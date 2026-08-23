using System;
using System.Collections.Generic;
using System.Linq;
using Fateforged.Cards;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile;
using Fateforged.Domain.Profile.Account;
using Fateforged.Domain.Profile.Campaign;
using Fateforged.Domain.Profile.Collection;
using Fateforged.Domain.Profile.Decks;
using Fateforged.Domain.Profile.Enums;
using Fateforged.Domain.Profile.Inventory;
using Fateforged.Domain.Profile.Rewards;
using Fateforged.Domain.Profile.Shop;
using Fateforged.Domain.Profile.Summoners;
using Fateforged.Meta.Deck;
using Fateforged.Meta.Rewards;
using Fateforged.Meta.Shop;
using Fateforged.Meta.Summoner;
using Godot;
using GdArray = Godot.Collections.Array;
using GdDict = Godot.Collections.Dictionary;
using ItemSlot = Fateforged.Domain.Profile.Inventory.ItemSlot;

namespace Fateforged.Infrastructure.Persistence;

/// <summary>
/// Native C# profile repository. Owns all profile data in-memory as typed ProfileData.
/// Handles JSON persistence via JsonProfileStore, migrations via ProfileMigrator,
/// and debounced saves via Timer.
/// </summary>
public partial class ProfileRepository
    : Node,
        IProfileRepository,
        IRewardProfileStore,
        IProgressionProfileStore
{
    public static ProfileRepository? Instance { get; private set; }

    private const float AutosaveDelay = 0.5f;
    private const int WalMaxEntries = 100;

    private ProfileData _data = new();
    private string _currentProfileId = "";
    private readonly JsonProfileStore _store = new();
    private Timer? _saveTimer;
    private bool _pendingSave;
    private readonly object _rewardTransactionLock = new();
    private int _rewardRevision;

    // C# events (for C# consumers)
    public event Action<string>? ProfileLoaded;
    public event Action<string>? ProfileSaved;
    public event Action<string>? SaveFailed;
    public event Action? DataChanged;

    // Godot signals (for GDScript consumers to connect() to)
    [Signal]
    public delegate void DataChangedGodotEventHandler();

    [Signal]
    public delegate void ProfileLoadedGodotEventHandler(string profileId);

    [Signal]
    public delegate void ProfileSavedGodotEventHandler(string profileId);

    [Signal]
    public delegate void SaveFailedGodotEventHandler(string error);

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    public override void _Ready()
    {
        Instance = this;
        SetupSaveTimer();

        // Auto-load default profile
        var defaultId = GetOrCreateDefaultProfile();
        LoadProfile(new ProfileId(defaultId));
        GD.Print("ProfileRepository: Initialized with native C# persistence");
    }

    public override void _Notification(int what)
    {
        switch ((long)what)
        {
            case NotificationWMCloseRequest:
                SaveProfile(immediate: true);
                break;
            case NotificationApplicationPaused:
                SaveProfile(immediate: true);
                break;
            case NotificationApplicationFocusOut:
                if (_pendingSave)
                    SaveProfile(immediate: true);
                break;
        }
    }

    private void SetupSaveTimer()
    {
        _saveTimer = new Timer { OneShot = true, WaitTime = AutosaveDelay };
        _saveTimer.Timeout += OnSaveTimerTimeout;
        AddChild(_saveTimer);
    }

    private void OnSaveTimerTimeout()
    {
        if (_pendingSave)
            WriteSave();
    }

    // =========================================================================
    // PROFILE OPERATIONS
    // =========================================================================

    public bool LoadProfile(ProfileId profileId)
    {
        GD.Print($"ProfileRepository: Loading profile '{profileId}'...");
        _currentProfileId = (string)profileId;

        _store.EnsureProfileDir(_currentProfileId);
        var dict = _store.LoadProfile(_currentProfileId);

        if (dict != null)
        {
            ProfileMigrator.MigrateIfNeeded(dict);
            _data = ProfileDataMapper.FromDictionary(dict, _currentProfileId);
            GD.Print("ProfileRepository: Profile loaded successfully");
        }
        else
        {
            GD.Print("ProfileRepository: No save found, creating fresh profile");
            CreateFreshProfile();
            SaveProfile(immediate: true);
        }

        ProfileLoaded?.Invoke(_currentProfileId);
        EmitSignal(SignalName.ProfileLoadedGodot, _currentProfileId);
        EmitDataChanged();
        return true;
    }

    /// <summary>GDScript boundary overload — Godot can't resolve explicit conversions from string.</summary>
    public bool LoadProfile(string id) => LoadProfile(new ProfileId(id));

    public void SaveProfile(bool immediate = false)
    {
        _pendingSave = true;
        if (immediate)
        {
            _saveTimer?.Stop();
            WriteSave();
        }
        else
        {
            _saveTimer?.Start(AutosaveDelay);
        }
    }

    public ProfileId GetCurrentProfileId() => new(_currentProfileId);

    public void ResetProfile()
    {
        GD.Print("ProfileRepository: Resetting profile...");
        CreateFreshProfile();
        SaveProfile(immediate: true);
        EmitDataChanged();
    }

    public ProfileData? GetProfileMetadata() => _data;

    public RewardProfileState GetRewardState()
    {
        lock (_rewardTransactionLock)
        {
            return RewardStateMapper.FromDictionary(RewardStateMapper.ToDictionary(_data.Rewards));
        }
    }

    public ProfileData GetProgressionSnapshot()
    {
        lock (_rewardTransactionLock)
        {
            return CloneProfile(_data);
        }
    }

    public bool TryCommitProgression(
        IReadOnlyList<IRewardGrantMutation> mutations,
        out string error
    )
    {
        lock (_rewardTransactionLock)
        {
            var candidate = CloneProfile(_data);
            foreach (var mutation in mutations)
            {
                if (!mutation.TryApply(candidate, out error))
                    return false;
            }

            return TryPersistRewardCandidate(candidate, out error);
        }
    }

    public bool TryGetOrCreateRewardSeed(SummonerId summonerId, out ulong seed, out string error)
    {
        lock (_rewardTransactionLock)
        {
            var key = (string)summonerId;
            if (_data.Rewards.RewardSeedBySummoner.TryGetValue(key, out seed))
            {
                error = "";
                return true;
            }

            seed = BitConverter.ToUInt64(
                System.Security.Cryptography.RandomNumberGenerator.GetBytes(sizeof(ulong))
            );
            if (seed == 0)
                seed = 1;

            var candidate = CloneProfile(_data);
            candidate.Rewards.RewardSeedBySummoner[key] = seed;
            return TryPersistRewardCandidate(candidate, out error);
        }
    }

    public IReadOnlySet<string> GetOwnedRewardKeys(SummonerId summonerId)
    {
        lock (_rewardTransactionLock)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var card in _data.Collection)
                keys.Add($"card:{card.CatalogId}");
            foreach (var item in _data.Items)
                keys.Add($"item:{item.CatalogId}");
            foreach (var unlocked in _data.UnlockedSummoners)
                keys.Add($"summoner:{unlocked}");
            foreach (var cosmetic in _data.Cosmetics.Owned)
                keys.Add($"cosmetic:{cosmetic}");
            foreach (var emote in _data.Emotes.Owned)
                keys.Add($"emote:{emote}");

            var summoner = _data.SummonerInstances.FirstOrDefault(instance =>
                instance.SummonerId == summonerId
            );
            if (summoner != null)
            {
                foreach (var trait in summoner.AcquiredTraitIds)
                    keys.Add($"summoner_trait:{trait}");
            }

            if (_data.CampaignProgressMap.TryGetValue((string)summonerId, out var progress))
            {
                foreach (var flag in progress.Academy.RewardFlags.Keys)
                    keys.Add($"academy_flag:{flag}");
            }
            return keys;
        }
    }

    public bool TryStoreResolvedOffer(
        ResolvedRewardOfferSnapshot snapshot,
        PendingRewardSelection? pending,
        out string error
    )
    {
        lock (_rewardTransactionLock)
        {
            var claimId = snapshot.ClaimId.Value;
            if (_data.Rewards.ResolvedOffers.ContainsKey(claimId))
            {
                if (
                    pending == null
                    || _data.Rewards.PendingSelections.ContainsKey(claimId)
                    || _data.Rewards.ClaimReceipts.ContainsKey(claimId)
                )
                {
                    error = "";
                    return true;
                }

                var existingCandidate = CloneProfile(_data);
                existingCandidate.Rewards.PendingSelections[claimId] = pending;
                return TryPersistRewardCandidate(existingCandidate, out error);
            }

            var candidate = CloneProfile(_data);
            candidate.Rewards.ResolvedOffers[claimId] = snapshot;
            if (pending != null)
                candidate.Rewards.PendingSelections[claimId] = pending;
            return TryPersistRewardCandidate(candidate, out error);
        }
    }

    public IRewardGrantTransaction BeginRewardTransaction() =>
        new ProfileRewardGrantTransaction(this, _rewardRevision);

    internal RewardTransactionCommitResult TryCommitRewardTransaction(
        int expectedRevision,
        IReadOnlyList<IRewardGrantMutation> mutations,
        RewardClaimReceipt receipt
    )
    {
        lock (_rewardTransactionLock)
        {
            if (expectedRevision != _rewardRevision)
            {
                return RewardTransactionCommitResult.Unavailable(
                    "Profile changed while the reward claim was being prepared."
                );
            }

            if (_data.Rewards.ClaimReceipts.ContainsKey(receipt.ClaimId.Value))
                return new RewardTransactionCommitResult(true, "");

            var candidate = CloneProfile(_data);

            foreach (var mutation in mutations)
            {
                if (!mutation.TryApply(candidate, out var mutationError))
                    return RewardTransactionCommitResult.Unavailable(mutationError);
            }

            candidate.Rewards.ClaimReceipts[receipt.ClaimId.Value] = receipt;
            candidate.Rewards.PendingSelections.Remove(receipt.ClaimId.Value);
            return TryPersistRewardCandidate(candidate, out var persistenceError)
                ? new RewardTransactionCommitResult(true, "")
                : RewardTransactionCommitResult.Unavailable(persistenceError);
        }
    }

    private ProfileData CloneProfile(ProfileData profile)
    {
        var snapshot = ProfileDataMapper.ToDictionary(profile);
        return ProfileDataMapper.FromDictionary(snapshot, _currentProfileId);
    }

    private bool TryPersistRewardCandidate(ProfileData candidate, out string error)
    {
        candidate.UpdatedAt = (long)Time.GetUnixTimeFromSystem();
        var candidateDict = ProfileDataMapper.ToDictionary(candidate);
        if (!_store.SaveProfile(_currentProfileId, candidateDict))
        {
            error = "Failed to persist the reward transaction.";
            return false;
        }

        _data = candidate;
        _rewardRevision++;
        _pendingSave = false;
        ProfileSaved?.Invoke(_currentProfileId);
        EmitSignal(SignalName.ProfileSavedGodot, _currentProfileId);
        EmitDataChanged();
        error = "";
        return true;
    }

    // =========================================================================
    // RESOURCE OPERATIONS
    // =========================================================================

    public Resources GetResources() => _data.Resources;

    public void UpdateResources(Dictionary<ResourceType, int> delta)
    {
        foreach (var (resourceType, amount) in delta)
        {
            switch (resourceType)
            {
                case ResourceType.Gold:
                    _data.Resources.Gold = Math.Max(0, _data.Resources.Gold + amount);
                    break;
                case ResourceType.Gems:
                    _data.Resources.Gems = Math.Max(0, _data.Resources.Gems + amount);
                    break;
                case ResourceType.Essence:
                    _data.Resources.Essence = Math.Max(0, _data.Resources.Essence + amount);
                    break;
                case ResourceType.Fragments:
                    _data.Resources.Fragments = Math.Max(0, _data.Resources.Fragments + amount);
                    break;
            }
        }
        _data.Resources.UpdatedAt = (long)Time.GetUnixTimeFromSystem();

        AppendToWal("update_resources", delta);
        MarkDirty();
    }

    // =========================================================================
    // SUMMONER OPERATIONS
    // =========================================================================

    public SummonerId[] GetUnlockedSummoners() => [.. _data.UnlockedSummoners];

    public bool IsSummonerUnlocked(SummonerId summonerId) =>
        _data.UnlockedSummoners.Contains(summonerId);

    public bool UnlockSummoner(SummonerId summonerId)
    {
        if (_data.UnlockedSummoners.Contains(summonerId))
            return false;
        _data.UnlockedSummoners.Add(summonerId);
        AppendToWal("unlock_summoner", new { summoner_id = (string)summonerId });
        MarkDirty();
        return true;
    }

    /// <summary>GDScript boundary overload — Godot can't resolve explicit conversions from string.</summary>
    public bool UnlockSummoner(string id) => UnlockSummoner(new SummonerId(id));

    public bool SetStartingSummoner(SummonerId summonerId, bool chosenRandom)
    {
        if (_data.UnlockedSummoners.Count > 0)
        {
            GD.PushError(
                "ProfileRepository: Cannot set starting summoner - summoners already unlocked"
            );
            return false;
        }

        _data.UnlockedSummoners.Add(summonerId);
        AppendToWal(
            "set_starting_summoner",
            new { summoner_id = (string)summonerId, chosen_random = chosenRandom }
        );
        MarkDirty();
        return true;
    }

    public SummonerInstance? GetSummonerInstance(SummonerId summonerId) =>
        _data.SummonerInstances.FirstOrDefault(s => s.SummonerId == summonerId);

    public SummonerInstance[] GetAllSummonerInstances() => [.. _data.SummonerInstances];

    public bool SaveSummonerInstance(SummonerInstance instance)
    {
        var existing = _data.SummonerInstances.FindIndex(s => s.SummonerId == instance.SummonerId);
        if (existing >= 0)
            _data.SummonerInstances[existing] = instance;
        else
        {
            _data.SummonerInstances.Add(instance);
            // Also add to unlocked_summoners for compatibility
            if (!_data.UnlockedSummoners.Contains(instance.SummonerId))
                _data.UnlockedSummoners.Add(instance.SummonerId);
        }

        AppendToWal("save_summoner_instance", new { summoner_id = (string)instance.SummonerId });
        MarkDirty();
        return true;
    }

    // =========================================================================
    // CARD COLLECTION OPERATIONS
    // =========================================================================

    public CardInstanceId[] GrantCards(IEnumerable<(CardId catalogId, string rarity)> cards) =>
        GrantCards(
            cards.Select(c =>
                (c.catalogId, c.rarity, ContentBinding.AccountWide, (SummonerId?)null)
            )
        );

    public CardInstanceId[] GrantCards(
        IEnumerable<(
            CardId catalogId,
            string rarity,
            ContentBinding binding,
            SummonerId? boundTo
        )> cards
    )
    {
        var ids = new List<CardInstanceId>();
        foreach (var (catalogId, rarity, binding, boundTo) in cards)
        {
            var instanceId = new CardInstanceId(Guid.NewGuid().ToString());
            var card = new CardInstance
            {
                Id = instanceId,
                CatalogId = catalogId,
                ProfileId = new ProfileId(_currentProfileId),
                Rarity = rarity,
                Level = 1,
                Xp = 0,
                Traits = [],
                UnspentTraitPoints = 0,
                CreatedAt = (long)Time.GetUnixTimeFromSystem(),
                Binding = binding,
                BoundToSummonerId = binding == ContentBinding.SummonerBound ? boundTo : null,
            };
            _data.Collection.Add(card);
            ids.Add(instanceId);
        }

        AppendToWal("grant_cards", new { instance_ids = ids.Select(id => (string)id).ToArray() });
        MarkDirty();
        return [.. ids];
    }

    public bool RemoveCard(CardInstanceId cardInstanceId)
    {
        var index = _data.Collection.FindIndex(c => c.Id == cardInstanceId);
        if (index < 0)
        {
            GD.PushWarning($"ProfileRepository: Card instance '{cardInstanceId}' not found");
            return false;
        }

        _data.Collection.RemoveAt(index);
        AppendToWal("remove_card", new { card_instance_id = (string)cardInstanceId });
        MarkDirty();
        return true;
    }

    public CardInstance[] ListCards() => [.. _data.Collection];

    public int GetCardCount(CardId catalogId) =>
        _data.Collection.Count(c => c.CatalogId == catalogId);

    public CardInstance? GetCard(CardInstanceId cardInstanceId) =>
        _data.Collection.FirstOrDefault(c => c.Id == cardInstanceId);

    public bool UpdateCard(CardInstanceId cardInstanceId, CardUpdate updates)
    {
        var card = _data.Collection.FirstOrDefault(c => c.Id == cardInstanceId);
        if (card == null)
        {
            GD.PushWarning(
                $"ProfileRepository: Card instance '{cardInstanceId}' not found for update"
            );
            return false;
        }

        if (updates.Xp.HasValue)
            card.Xp = updates.Xp.Value;
        if (updates.Level.HasValue)
            card.Level = updates.Level.Value;
        if (updates.Traits != null)
            card.Traits = updates.Traits;
        if (updates.UnspentTraitPoints.HasValue)
            card.UnspentTraitPoints = updates.UnspentTraitPoints.Value;

        AppendToWal("update_card", new { card_instance_id = (string)cardInstanceId });
        MarkDirty();
        return true;
    }

    // =========================================================================
    // DECK OPERATIONS
    // =========================================================================

    public DeckId UpsertDeck(Deck deck)
    {
        if (!deck.Id.HasValue || string.IsNullOrEmpty((string)deck.Id))
        {
            // Create new deck
            var newId = new DeckId(Guid.NewGuid().ToString());
            deck.Id = newId;
            deck.ProfileId = new ProfileId(_currentProfileId);
            _data.Decks.Add(deck);
        }
        else
        {
            // Update existing
            var existing = _data.Decks.FindIndex(d => d.Id == deck.Id);
            if (existing >= 0)
            {
                _data.Decks[existing].Name = deck.Name;
                if (deck.SummonerId.HasValue)
                    _data.Decks[existing].SummonerId = deck.SummonerId;
                _data.Decks[existing].CardInstanceIds = deck.CardInstanceIds;
                _data.Decks[existing].UpdatedAt = deck.UpdatedAt;
            }
            else
            {
                GD.PushWarning($"ProfileRepository: Deck '{deck.Id}' not found for update");
                return DeckId.None;
            }
        }

        MarkDirty();
        return deck.Id;
    }

    public bool DeleteDeck(DeckId deckId)
    {
        var index = _data.Decks.FindIndex(d => d.Id == deckId);
        if (index < 0)
        {
            GD.PushWarning($"ProfileRepository: Deck '{deckId}' not found");
            return false;
        }

        _data.Decks.RemoveAt(index);
        MarkDirty();
        return true;
    }

    public Deck[] ListDecks() => [.. _data.Decks];

    public Deck? GetDeck(DeckId deckId) => _data.Decks.FirstOrDefault(d => d.Id == deckId);

    // =========================================================================
    // CAMPAIGN OPERATIONS
    // =========================================================================

    public CampaignProgress GetCampaignProgress(SummonerId summonerId)
    {
        var key = (string)summonerId;
        if (string.IsNullOrEmpty(key))
            key = GetActiveSummonerId();

        if (string.IsNullOrEmpty(key))
            return new CampaignProgress();

        return _data.CampaignProgressMap.TryGetValue(key, out var progress)
            ? progress
            : new CampaignProgress();
    }

    public void UpdateCampaignProgress(SummonerId summonerId, CampaignProgress progress)
    {
        var key = (string)summonerId;
        if (string.IsNullOrEmpty(key))
            key = GetActiveSummonerId();

        if (string.IsNullOrEmpty(key))
        {
            GD.PushWarning(
                "ProfileRepository: Cannot update campaign progress - no active summoner"
            );
            return;
        }

        _data.CampaignProgressMap[key] = progress;
        AppendToWal("update_campaign_progress", new { summoner_id = key });
        SaveProfile(immediate: true);
        EmitDataChanged();
    }

    public CampaignProgress GetSharedCampaignProgress() => _data.SharedCampaignProgress;

    public void UpdateSharedCampaignProgress(CampaignProgress progress)
    {
        _data.SharedCampaignProgress = progress;
        AppendToWal("update_shared_campaign_progress", null);
        SaveProfile(immediate: true);
        EmitDataChanged();
    }

    // =========================================================================
    // CARAVAN PURCHASE TRACKING
    // =========================================================================

    public string[] GetCaravanPurchases(SummonerId summonerId)
    {
        var progress = GetCampaignProgress(summonerId);
        return progress.CaravanPurchases.ToArray();
    }

    public void AddCaravanPurchase(string offeringId, SummonerId summonerId)
    {
        var key = (string)summonerId;
        if (string.IsNullOrEmpty(key))
            key = GetActiveSummonerId();

        if (string.IsNullOrEmpty(key))
            return;

        var typedId = new SummonerId(key);
        var progress = GetCampaignProgress(typedId);
        if (!progress.CaravanPurchases.Contains(offeringId))
        {
            progress.CaravanPurchases.Add(offeringId);
            UpdateCampaignProgress(typedId, progress);
        }
    }

    public void ClearCaravanPurchases(SummonerId summonerId)
    {
        var key = (string)summonerId;
        if (string.IsNullOrEmpty(key))
            key = GetActiveSummonerId();

        if (string.IsNullOrEmpty(key))
            return;

        var typedId = new SummonerId(key);
        var progress = GetCampaignProgress(typedId);
        if (progress.CaravanPurchases.Count > 0)
        {
            progress.CaravanPurchases.Clear();
            UpdateCampaignProgress(typedId, progress);
        }
    }

    // =========================================================================
    // COSMETIC OPERATIONS
    // =========================================================================

    public CosmeticId[] GetOwnedCosmetics() => [.. _data.Cosmetics.Owned];

    public bool IsCosmeticOwned(CosmeticId cosmeticId) =>
        _data.Cosmetics.Owned.Contains(cosmeticId);

    public bool GrantCosmetic(CosmeticId cosmeticId)
    {
        if (_data.Cosmetics.Owned.Contains(cosmeticId))
            return false;
        _data.Cosmetics.Owned.Add(cosmeticId);
        AppendToWal("grant_cosmetic", new { cosmetic_id = (string)cosmeticId });
        MarkDirty();
        return true;
    }

    /// <summary>GDScript boundary overload — Godot can't resolve explicit conversions from string.</summary>
    public bool GrantCosmetic(string id) => GrantCosmetic(new CosmeticId(id));

    public Dictionary<string, CosmeticId> GetEquippedCosmetics()
    {
        return new Dictionary<string, CosmeticId>
        {
            ["card_back"] = _data.Cosmetics.Equipped.CardBack,
            ["ui_theme"] = _data.Cosmetics.Equipped.UiTheme,
        };
    }

    public bool EquipCosmetic(string slot, CosmeticId cosmeticId)
    {
        if (cosmeticId.HasValue && !IsCosmeticOwned(cosmeticId))
        {
            GD.PushWarning($"ProfileRepository: Cannot equip cosmetic '{cosmeticId}' - not owned");
            return false;
        }

        switch (slot)
        {
            case "card_back":
                _data.Cosmetics.Equipped.CardBack = cosmeticId;
                break;
            case "ui_theme":
                _data.Cosmetics.Equipped.UiTheme = cosmeticId;
                break;
            default:
                GD.PushWarning($"ProfileRepository: Unknown cosmetic slot '{slot}'");
                return false;
        }

        AppendToWal("equip_cosmetic", new { slot, cosmetic_id = (string)cosmeticId });
        MarkDirty();
        return true;
    }

    public Dictionary<SummonerId, SkinId> GetSummonerSkins()
    {
        var result = new Dictionary<SummonerId, SkinId>();
        foreach (var (key, skinId) in _data.Cosmetics.SummonerSkins)
            result[new SummonerId(key)] = skinId;
        return result;
    }

    public bool SetSummonerSkin(SummonerId summonerId, SkinId skinId)
    {
        if (skinId.HasValue && !IsCosmeticOwned(new CosmeticId((string)skinId)))
        {
            GD.PushWarning($"ProfileRepository: Cannot set skin '{skinId}' - not owned");
            return false;
        }

        var key = (string)summonerId;
        if (!skinId.HasValue)
            _data.Cosmetics.SummonerSkins.Remove(key);
        else
            _data.Cosmetics.SummonerSkins[key] = skinId;

        AppendToWal(
            "set_summoner_skin",
            new { summoner_id = (string)summonerId, skin_id = (string)skinId }
        );
        MarkDirty();
        return true;
    }

    // =========================================================================
    // EMOTE OPERATIONS
    // =========================================================================

    public EmoteId[] GetOwnedEmotes() => [.. _data.Emotes.Owned];

    public bool IsEmoteOwned(EmoteId emoteId) => _data.Emotes.Owned.Contains(emoteId);

    public bool GrantEmote(EmoteId emoteId)
    {
        if (_data.Emotes.Owned.Contains(emoteId))
            return false;
        _data.Emotes.Owned.Add(emoteId);
        AppendToWal("grant_emote", new { emote_id = (string)emoteId });
        MarkDirty();
        return true;
    }

    /// <summary>GDScript boundary overload — Godot can't resolve explicit conversions from string.</summary>
    public bool GrantEmote(string id) => GrantEmote(new EmoteId(id));

    public EmoteId[] GetEquippedEmotes()
    {
        // Ensure exactly 4 slots
        while (_data.Emotes.Equipped.Count < 4)
            _data.Emotes.Equipped.Add(EmoteId.None);
        return [.. _data.Emotes.Equipped];
    }

    public bool EquipEmote(int slot, EmoteId emoteId)
    {
        if (slot < 0 || slot >= 4)
        {
            GD.PushWarning($"ProfileRepository: Invalid emote slot {slot} (must be 0-3)");
            return false;
        }

        if (emoteId.HasValue && !IsEmoteOwned(emoteId))
        {
            GD.PushWarning($"ProfileRepository: Cannot equip emote '{emoteId}' - not owned");
            return false;
        }

        while (_data.Emotes.Equipped.Count < 4)
            _data.Emotes.Equipped.Add(EmoteId.None);

        _data.Emotes.Equipped[slot] = emoteId;
        AppendToWal("equip_emote", new { slot, emote_id = (string)emoteId });
        MarkDirty();
        return true;
    }

    // =========================================================================
    // SHOP OPERATIONS
    // =========================================================================

    public int GetPurchaseCount(string purchaseKey) =>
        _data.ShopPurchases.TryGetValue(purchaseKey, out var count) ? count : 0;

    public bool IncrementPurchaseCount(string purchaseKey)
    {
        _data.ShopPurchases[purchaseKey] = _data.ShopPurchases.TryGetValue(
            purchaseKey,
            out var count
        )
            ? count + 1
            : 1;
        AppendToWal("shop_purchase", new { key = purchaseKey });
        MarkDirty();
        return true;
    }

    public ShopRefreshState GetShopRefreshState(ShopId shopId)
    {
        var key = (string)shopId;
        return _data.ShopRefreshStateMap.TryGetValue(key, out var state)
            ? state
            : new ShopRefreshState();
    }

    public bool IncrementShopRefreshEpoch(ShopId shopId)
    {
        var key = (string)shopId;
        if (!_data.ShopRefreshStateMap.TryGetValue(key, out var state))
            state = new ShopRefreshState();

        state.RefreshEpoch++;
        state.LastRefreshAt = Time.GetDatetimeStringFromSystem();
        _data.ShopRefreshStateMap[key] = state;

        AppendToWal("shop_refresh", new { shop_id = key });
        MarkDirty();
        return true;
    }

    // =========================================================================
    // META OPERATIONS
    // =========================================================================

    public void UpdateProfileMeta(MetaUpdate updates)
    {
        if (updates.SelectedDeck != null)
            _data.Meta.SelectedDeck = updates.SelectedDeck;
        if (updates.RankedDecksBySummoner != null)
            _data.Meta.RankedDecksBySummoner = new Dictionary<string, string>(
                updates.RankedDecksBySummoner
            );
        if (updates.SelectedSummoner != null)
            _data.Meta.SelectedSummoner = updates.SelectedSummoner;
        if (updates.SelectedCampaign != null)
            _data.Meta.SelectedCampaign = updates.SelectedCampaign;
        if (updates.AnalyticsOptIn.HasValue)
            _data.Meta.AnalyticsOptIn = updates.AnalyticsOptIn.Value;

        if (updates.TutorialFlags != null)
        {
            foreach (var (key, value) in updates.TutorialFlags)
                _data.Meta.TutorialFlags[key] = value;
        }

        if (updates.NarrativeFlags != null)
        {
            foreach (var (key, value) in updates.NarrativeFlags)
                _data.Meta.NarrativeFlags[key] = value;
        }

        if (updates.Achievements != null)
        {
            foreach (var (key, value) in updates.Achievements)
                _data.Meta.Achievements[key] = value;
        }

        MarkDirty();
    }

    // =========================================================================
    // SETTINGS OPERATIONS
    // =========================================================================

    public Settings GetSettings() => _data.Settings;

    public void UpdateSettings(Settings settings)
    {
        _data.Settings = settings;
        SaveProfile(immediate: true);
        EmitDataChanged();
    }

    public GdDict GetSettingsDict() => DtoConverters.ToDict(_data.Settings);

    public void UpdateSettingsDict(GdDict settingsDict)
    {
        var merged = DtoConverters.ToDict(_data.Settings);
        foreach (var key in settingsDict.Keys)
            merged[key] = settingsDict[key];
        _data.Settings = DtoConverters.FromSettingsDict(merged);
        SaveProfile(immediate: true);
        EmitDataChanged();
    }

    // =========================================================================
    // ITEM OPERATIONS
    // =========================================================================

    public List<ItemInstance> ListItems() => [.. _data.Items];

    public void SaveItems(List<ItemInstance> items)
    {
        _data.Items = [.. items];
        MarkDirty();
    }

    // =========================================================================
    // GDSCRIPT INTEROP METHODS (dict-returning for GDScript forwarder)
    // =========================================================================

    /// <summary>Get full profile as dictionary (read-only snapshot).</summary>
    public GdDict GetActiveProfileDict() => ProfileDataMapper.ToDictionary(_data);

    /// <summary>Get resources as dictionary for GDScript.</summary>
    public GdDict GetResourcesDict() => DtoConverters.ToDict(_data.Resources);

    /// <summary>Update resources from GDScript dictionary delta.</summary>
    public void UpdateResourcesDict(GdDict delta)
    {
        var typedDelta = new Dictionary<ResourceType, int>();
        foreach (var key in delta.Keys)
        {
            var keyStr = key.AsString();
            if (Enum.TryParse<ResourceType>(keyStr, ignoreCase: true, out var resourceType))
                typedDelta[resourceType] = delta[key].AsInt32();
        }
        UpdateResources(typedDelta);
    }

    /// <summary>Get shop purchases as dictionary for GDScript.</summary>
    public GdDict GetShopPurchasesDict()
    {
        var dict = new GdDict();
        foreach (var (key, count) in _data.ShopPurchases)
            dict[key] = count;
        return dict;
    }

    /// <summary>Get shop refresh state for a shop as dictionary for GDScript.</summary>
    public GdDict GetShopRefreshStateDict(string shopId) =>
        DtoConverters.ToDict(GetShopRefreshState(new ShopId(shopId)));

    /// <summary>Get caravan purchases as array for GDScript.</summary>
    public GdArray GetCaravanPurchasesArray(string summonerId)
    {
        var purchases = GetCaravanPurchases(
            string.IsNullOrEmpty(summonerId)
                ? new SummonerId(GetActiveSummonerId())
                : new SummonerId(summonerId)
        );
        var arr = new GdArray();
        foreach (var p in purchases)
            arr.Add(p);
        return arr;
    }

    /// <summary>Update profile meta from GDScript dictionary.</summary>
    public void UpdateProfileMetaDict(GdDict metaDict)
    {
        var update = new MetaUpdate();
        if (metaDict.ContainsKey("selected_deck"))
            update.SelectedDeck = metaDict["selected_deck"].AsString();
        if (
            metaDict.ContainsKey("ranked_decks_by_summoner")
            && metaDict["ranked_decks_by_summoner"].VariantType == Variant.Type.Dictionary
        )
        {
            update.RankedDecksBySummoner = [];
            foreach (var key in metaDict["ranked_decks_by_summoner"].AsGodotDictionary().Keys)
                update.RankedDecksBySummoner[key.AsString()] = metaDict[
                    "ranked_decks_by_summoner"
                ].AsGodotDictionary()[key].AsString();
        }
        if (metaDict.ContainsKey("selected_summoner"))
            update.SelectedSummoner = metaDict["selected_summoner"].AsString();
        if (metaDict.ContainsKey("selected_campaign"))
            update.SelectedCampaign = metaDict["selected_campaign"].AsString();
        if (metaDict.ContainsKey("analytics_opt_in"))
            update.AnalyticsOptIn = metaDict["analytics_opt_in"].AsBool();
        UpdateProfileMeta(update);
    }

    /// <summary>Update campaign progress from GDScript dictionary.</summary>
    public void UpdateCampaignProgressDict(GdDict progressDict, string summonerId = "")
    {
        var key = string.IsNullOrEmpty(summonerId) ? GetActiveSummonerId() : summonerId;
        if (string.IsNullOrEmpty(key))
        {
            GD.PushWarning("ProfileRepository: Cannot update campaign progress - no summoner");
            return;
        }

        var existing = GetCampaignProgress(new SummonerId(key));
        var existingDict = DtoConverters.ToDict(existing);

        // Merge fields from progressDict into existing
        foreach (var dictKey in progressDict.Keys)
            existingDict[dictKey] = progressDict[dictKey];

        var merged = DtoConverters.FromCampaignDict(existingDict) ?? new CampaignProgress();
        UpdateCampaignProgress(new SummonerId(key), merged);
    }

    /// <summary>Get full profile data snapshot as dictionary.</summary>
    public GdDict GetProfileDataSnapshot() => ProfileDataMapper.ToDictionary(_data);

    /// <summary>Load complete profile data from a dictionary snapshot.</summary>
    public void LoadProfileData(GdDict profileData)
    {
        if (profileData.Count == 0)
        {
            GD.PushError("ProfileRepository: Cannot load empty profile data");
            return;
        }

        GD.Print("ProfileRepository: Loading profile data from snapshot...");
        _data = ProfileDataMapper.FromDictionary(profileData, _currentProfileId);
        _data.ProfileId = new ProfileId(_currentProfileId);
        _data.Resources.ProfileId = new ProfileId(_currentProfileId);
        _data.UpdatedAt = (long)Time.GetUnixTimeFromSystem();

        SaveProfile(immediate: true);
        ProfileLoaded?.Invoke(_currentProfileId);
        EmitSignal(SignalName.ProfileLoadedGodot, _currentProfileId);
        EmitDataChanged();
        GD.Print("ProfileRepository: Profile data loaded successfully");
    }

    /// <summary>Check if onboarding is complete.</summary>
    public bool IsOnboardingComplete()
    {
        return _data.SharedCampaignProgress.CompletedBattles.Any(b =>
            (string)b == "event_caravan_tutorial"
        );
    }

    /// <summary>Get active deck as array of {catalog_id, count} for multiplayer.</summary>
    public GdArray GetActiveDeckArray()
    {
        return GetDeckArray(_data.Meta.SelectedDeck);
    }

    /// <summary>Get a deck as {catalog_id, count} entries for battle configuration.</summary>
    public GdArray GetDeckArray(string deckId)
    {
        if (string.IsNullOrEmpty(deckId))
            return [];

        var deck = _data.Decks.FirstOrDefault(d => (string)d.Id == deckId);
        if (deck == null)
            return [];

        // Convert card_instance_ids → {catalog_id, count} format
        var catalogCounts = new Dictionary<string, int>();
        foreach (var instanceId in deck.CardInstanceIds)
        {
            var card = _data.Collection.FirstOrDefault(c => c.Id == instanceId);
            if (card == null)
                continue;
            var catalogId = (string)card.CatalogId;
            if (!string.IsNullOrEmpty(catalogId))
                catalogCounts[catalogId] = catalogCounts.GetValueOrDefault(catalogId, 0) + 1;
        }

        var result = new GdArray();
        foreach (var (catalogId, count) in catalogCounts)
        {
            result.Add(new GdDict { ["catalog_id"] = catalogId, ["count"] = count });
        }
        return result;
    }

    /// <summary>Get unlocked summoner IDs as Godot Array for GDScript.</summary>
    public GdArray GetUnlockedSummonersArray()
    {
        var arr = new GdArray();
        foreach (var s in _data.UnlockedSummoners)
            arr.Add((string)s);
        return arr;
    }

    /// <summary>Get owned cosmetic IDs as Godot Array for GDScript.</summary>
    public GdArray GetOwnedCosmeticsArray()
    {
        var arr = new GdArray();
        foreach (var c in _data.Cosmetics.Owned)
            arr.Add((string)c);
        return arr;
    }

    /// <summary>Get equipped cosmetics as Godot Dictionary for GDScript.</summary>
    public GdDict GetEquippedCosmeticsDict()
    {
        return new GdDict
        {
            ["card_back"] = (string)_data.Cosmetics.Equipped.CardBack,
            ["ui_theme"] = (string)_data.Cosmetics.Equipped.UiTheme,
        };
    }

    /// <summary>Get summoner skins as Godot Dictionary for GDScript.</summary>
    public GdDict GetSummonerSkinsDict()
    {
        var dict = new GdDict();
        foreach (var (key, skinId) in _data.Cosmetics.SummonerSkins)
            dict[key] = (string)skinId;
        return dict;
    }

    /// <summary>Get owned emote IDs as Godot Array for GDScript.</summary>
    public GdArray GetOwnedEmotesArray()
    {
        var arr = new GdArray();
        foreach (var e in _data.Emotes.Owned)
            arr.Add((string)e);
        return arr;
    }

    /// <summary>Get equipped emotes as Godot Array for GDScript.</summary>
    public GdArray GetEquippedEmotesArray()
    {
        var equipped = GetEquippedEmotes();
        var arr = new GdArray();
        foreach (var e in equipped)
            arr.Add((string)e);
        return arr;
    }

    /// <summary>List all cards as Godot Array of Dictionaries for GDScript.</summary>
    public GdArray ListCardsArray()
    {
        var arr = new GdArray();
        foreach (var card in _data.Collection)
            arr.Add(DtoConverters.ToDict(card));
        return arr;
    }

    /// <summary>Get a card as Godot Dictionary for GDScript.</summary>
    public GdDict GetCardDict(string cardInstanceId)
    {
        var card = _data.Collection.FirstOrDefault(c => (string)c.Id == cardInstanceId);
        return card != null ? DtoConverters.ToDict(card) : new GdDict();
    }

    /// <summary>Grant cards from GDScript array of dicts. Returns array of instance IDs.</summary>
    public GdArray GrantCardsFromDicts(GdArray cards)
    {
        var typedCards =
            new List<(
                CardId catalogId,
                string rarity,
                ContentBinding binding,
                SummonerId? boundTo
            )>();
        foreach (var item in cards)
        {
            if (item.VariantType != Variant.Type.Dictionary)
                continue;
            var dict = item.AsGodotDictionary();
            var catalogId = GdGet(dict, "catalog_id", "").AsString();
            var rarity = GdGet(dict, "rarity", "common").AsString();
            var bindingInt = GdGet(dict, "binding", 0).AsInt32();
            var binding = Enum.IsDefined(typeof(ContentBinding), bindingInt)
                ? (ContentBinding)bindingInt
                : ContentBinding.AccountWide;
            var boundToStr = GdGet(dict, "bound_to", "").AsString();
            SummonerId? boundTo = string.IsNullOrEmpty(boundToStr)
                ? null
                : new SummonerId(boundToStr);

            typedCards.Add((new CardId(catalogId), rarity, binding, boundTo));
        }

        var ids = GrantCards(typedCards);
        var result = new GdArray();
        foreach (var id in ids)
            result.Add((string)id);
        return result;
    }

    /// <summary>Update card from GDScript dictionary. Returns bool.</summary>
    public bool UpdateCardFromDict(string cardInstanceId, GdDict updates)
    {
        var cardUpdate = new CardUpdate();
        if (updates.ContainsKey("xp"))
            cardUpdate.Xp = updates["xp"].AsInt32();
        if (updates.ContainsKey("level"))
            cardUpdate.Level = updates["level"].AsInt32();
        if (updates.ContainsKey("upgrades"))
        {
            var traits = new List<CardTraitId>();
            var arr = updates["upgrades"].AsGodotArray();
            foreach (var t in arr)
                traits.Add(new CardTraitId(t.AsString()));
            cardUpdate.Traits = traits;
        }
        if (updates.ContainsKey("unspent_trait_points"))
            cardUpdate.UnspentTraitPoints = updates["unspent_trait_points"].AsInt32();
        return UpdateCard(new CardInstanceId(cardInstanceId), cardUpdate);
    }

    /// <summary>Upsert deck from GDScript dictionary. Returns deck_id string.</summary>
    public string UpsertDeckFromDict(GdDict deckDict)
    {
        var deck = DtoConverters.FromDeckDict(deckDict);
        if (deck == null)
        {
            // Create new deck with the provided data
            deck = new Deck
            {
                Id = DeckId.None,
                SummonerId = new SummonerId(GdGet(deckDict, "summoner_id", "").AsString()),
                Name = GdGet(deckDict, "name", "Untitled Deck").AsString(),
            };

            // Parse card_instance_ids if provided
            if (
                deckDict.TryGetValue("card_instance_ids", out var cardsVar)
                && cardsVar.VariantType == Variant.Type.Array
            )
            {
                foreach (var c in cardsVar.AsGodotArray())
                    deck.CardInstanceIds.Add(new CardInstanceId(c.AsString()));
            }
        }

        var id = UpsertDeck(deck);
        return (string)id;
    }

    /// <summary>List all decks as Godot Array of Dictionaries for GDScript.</summary>
    public GdArray ListDecksArray()
    {
        var arr = new GdArray();
        foreach (var deck in _data.Decks)
            arr.Add(DtoConverters.ToDict(deck));
        return arr;
    }

    /// <summary>Get a deck as Godot Dictionary for GDScript.</summary>
    public GdDict GetDeckDict(string deckId)
    {
        var deck = _data.Decks.FirstOrDefault(d => (string)d.Id == deckId);
        return deck != null ? DtoConverters.ToDict(deck) : new GdDict();
    }

    /// <summary>Get profile meta as dictionary for GDScript.</summary>
    public GdDict GetProfileMetaDict() => DtoConverters.ToDict(_data.Meta);

    /// <summary>Get last match as dictionary for GDScript.</summary>
    public GdDict GetLastMatchDict()
    {
        var dict = new GdDict();
        dict["seed"] = _data.LastMatch.Seed.HasValue
            ? Variant.From(_data.LastMatch.Seed.Value)
            : new Variant();
        dict["result"] =
            _data.LastMatch.Result != null ? Variant.From(_data.LastMatch.Result) : new Variant();
        dict["duration_s"] = _data.LastMatch.DurationSeconds.HasValue
            ? Variant.From(_data.LastMatch.DurationSeconds.Value)
            : new Variant();
        return dict;
    }

    /// <summary>Update last match from GDScript dictionary.</summary>
    public void UpdateLastMatchDict(GdDict matchInfo)
    {
        if (
            matchInfo.TryGetValue("seed", out var seedVar)
            && seedVar.VariantType == Variant.Type.Int
        )
            _data.LastMatch.Seed = seedVar.AsInt64();
        if (
            matchInfo.TryGetValue("result", out var resultVar)
            && resultVar.VariantType == Variant.Type.String
        )
            _data.LastMatch.Result = resultVar.AsString();
        if (matchInfo.TryGetValue("duration_s", out var durVar))
        {
            if (durVar.VariantType == Variant.Type.Float)
                _data.LastMatch.DurationSeconds = (float)durVar.AsDouble();
            else if (durVar.VariantType == Variant.Type.Int)
                _data.LastMatch.DurationSeconds = durVar.AsInt32();
        }
        SaveProfile(immediate: true);
        EmitDataChanged();
    }

    /// <summary>Save summoner instance from GDScript dictionary (for test/interop).</summary>
    public bool SaveSummonerInstanceDict(GdDict summonerData)
    {
        var instance = DtoConverters.FromSummonerDict(summonerData);
        if (instance == null)
        {
            GD.PushError("ProfileRepository: SaveSummonerInstanceDict called with invalid data");
            return false;
        }
        return SaveSummonerInstance(instance);
    }

    /// <summary>Get summoner instance as dictionary for GDScript.</summary>
    public GdDict GetSummonerInstanceDict(string summonerId)
    {
        var instance = GetSummonerInstance(new SummonerId(summonerId));
        return instance != null ? DtoConverters.ToDict(instance) : new GdDict();
    }

    /// <summary>Get all summoner instances as array for GDScript.</summary>
    public GdArray GetSummonerInstancesArray()
    {
        var arr = new GdArray();
        foreach (var inst in _data.SummonerInstances)
            arr.Add(DtoConverters.ToDict(inst));
        return arr;
    }

    /// <summary>Get campaign progress as dictionary for GDScript.</summary>
    public GdDict GetCampaignProgressDict(string summonerId = "")
    {
        var progress = GetCampaignProgress(
            string.IsNullOrEmpty(summonerId)
                ? new SummonerId(GetActiveSummonerId())
                : new SummonerId(summonerId)
        );
        return DtoConverters.ToDict(progress);
    }

    /// <summary>Get shared campaign progress as dictionary for GDScript.</summary>
    public GdDict GetSharedCampaignProgressDict() =>
        DtoConverters.ToDict(_data.SharedCampaignProgress);

    /// <summary>Update shared campaign progress from GDScript dictionary.</summary>
    public void UpdateSharedCampaignProgressDict(GdDict progressDict)
    {
        var existing = DtoConverters.ToDict(_data.SharedCampaignProgress);
        foreach (var key in progressDict.Keys)
            existing[key] = progressDict[key];
        var merged = DtoConverters.FromCampaignDict(existing) ?? new CampaignProgress();
        UpdateSharedCampaignProgress(merged);
    }

    // =========================================================================
    // INTERNAL
    // =========================================================================

    private void MarkDirty()
    {
        SaveProfile();
        EmitDataChanged();
    }

    private void EmitDataChanged()
    {
        DataChanged?.Invoke();
        EmitSignal(SignalName.DataChangedGodot);
    }

    private void WriteSave()
    {
        if (!_pendingSave)
            return;
        _pendingSave = false;

        _data.UpdatedAt = (long)Time.GetUnixTimeFromSystem();
        var dict = ProfileDataMapper.ToDictionary(_data);

        if (_store.SaveProfile(_currentProfileId, dict))
        {
            ProfileSaved?.Invoke(_currentProfileId);
            EmitSignal(SignalName.ProfileSavedGodot, _currentProfileId);
        }
        else
        {
            var error = "Failed to write save file";
            SaveFailed?.Invoke(error);
            EmitSignal(SignalName.SaveFailedGodot, error);
            GD.PushError("ProfileRepository: Save failed!");
        }
    }

    private void CreateFreshProfile()
    {
        var now = (long)Time.GetUnixTimeFromSystem();
        _data = new ProfileData
        {
            Version = ProfileData.CurrentVersion,
            ProfileId = new ProfileId(_currentProfileId),
            UpdatedAt = now,
            CatalogVersion = "1.0.0",
            Resources = new Resources
            {
                Gold = 100,
                Gems = 0,
                Essence = 0,
                Fragments = 0,
                ProfileId = new ProfileId(_currentProfileId),
                UpdatedAt = now,
            },
            Meta = new AccountMeta
            {
                SelectedDeck = "",
                SelectedSummoner = "",
                TutorialFlags = [],
                Achievements = [],
                AnalyticsOptIn = false,
            },
            Settings = new Settings
            {
                SfxVolume = 1.0f,
                MusicVolume = 1.0f,
                Lang = "en",
            },
            Cosmetics = new Cosmetics
            {
                Owned = [],
                Equipped = new EquippedCosmetics
                {
                    CardBack = CosmeticId.None,
                    UiTheme = CosmeticId.None,
                },
                SummonerSkins = [],
            },
            Emotes = new Emotes
            {
                Owned = [],
                Equipped = [EmoteId.None, EmoteId.None, EmoteId.None, EmoteId.None],
            },
        };
    }

    private string GetOrCreateDefaultProfile()
    {
        var defaultId = "default";
        _store.EnsureProfileDir(defaultId);
        return defaultId;
    }

    private static string GetActiveSummonerId() =>
        SummonerSelectionService.Instance?.GetActiveSummonerId() ?? "";

    /// <summary>Safe dictionary access — Godot.Collections.Dictionary lacks GetValueOrDefault.</summary>
    private static Variant GdGet(GdDict dict, string key, Variant defaultValue) =>
        dict.TryGetValue(key, out var val) ? val : defaultValue;

    private void AppendToWal(string action, object? parameters)
    {
        var walEntry = new Dictionary<string, object>
        {
            ["op_id"] = Guid.NewGuid().ToString(),
            ["profile_id"] = _currentProfileId,
            ["action"] = action,
            ["timestamp"] = Time.GetUnixTimeFromSystem(),
        };
        if (parameters != null)
            walEntry["params"] = parameters;

        _data.Wal.Add(walEntry);

        // Trim WAL if too large
        if (_data.Wal.Count > WalMaxEntries)
            _data.Wal = _data.Wal.Skip(_data.Wal.Count - WalMaxEntries).ToList();
    }
}
