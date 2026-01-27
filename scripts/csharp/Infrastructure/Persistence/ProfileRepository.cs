using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectSummoner.Domain.Profile;
using ProjectSummoner.Domain.Profile.Account;
using ProjectSummoner.Domain.Profile.Campaign;
using ProjectSummoner.Domain.Profile.Collection;
using ProjectSummoner.Domain.Profile.Decks;
using ProjectSummoner.Domain.Profile.Enums;
using ProjectSummoner.Domain.Profile.Inventory;
using ProjectSummoner.Domain.Profile.Shop;
using ProjectSummoner.Domain.Profile.Summoners;

namespace ProjectSummoner.Infrastructure.Persistence;

/// <summary>
/// Repository that wraps the GDScript ProfileRepo autoload.
/// Provides typed C# access to profile operations while delegating to existing GDScript implementation.
/// This allows gradual migration - C# code gets type safety now, full C# impl can come later.
/// </summary>
public partial class ProfileRepository : Node, IProfileRepository
{
    public static ProfileRepository? Instance { get; private set; }

    private Node? _gdProfileRepo;

    // Events
    public event Action<string>? ProfileLoaded;
    public event Action<string>? ProfileSaved;
    public event Action<string>? SaveFailed;
    public event Action? DataChanged;

    public override void _Ready()
    {
        Instance = this;
        ConnectToGdScript();
    }

    private void ConnectToGdScript()
    {
        _gdProfileRepo = GetNode<Node>("/root/ProfileRepo");
        if (_gdProfileRepo == null)
        {
            GD.PushError("ProfileRepository: ProfileRepo autoload not found");
            return;
        }

        // Connect GDScript signals to C# events
        _gdProfileRepo.Connect("profile_loaded", Callable.From<string>(OnProfileLoaded));
        _gdProfileRepo.Connect("profile_saved", Callable.From<string>(OnProfileSaved));
        _gdProfileRepo.Connect("save_failed", Callable.From<string>(OnSaveFailed));
        _gdProfileRepo.Connect("data_changed", Callable.From(OnDataChanged));

        GD.Print("ProfileRepository: Connected to GDScript ProfileRepo");
    }

    private void OnProfileLoaded(string profileId) => ProfileLoaded?.Invoke(profileId);
    private void OnProfileSaved(string profileId) => ProfileSaved?.Invoke(profileId);
    private void OnSaveFailed(string error) => SaveFailed?.Invoke(error);
    private void OnDataChanged() => DataChanged?.Invoke();

    /// <summary>Check if the GDScript repository is connected and log error if not.</summary>
    private bool EnsureConnected(string methodName)
    {
        if (_gdProfileRepo != null) return true;
        GD.PushError($"ProfileRepository.{methodName}: GDScript ProfileRepo not connected");
        return false;
    }

    // =========================================================================
    // PROFILE OPERATIONS
    // =========================================================================

    public bool LoadProfile(string profileId)
    {
        if (!EnsureConnected(nameof(LoadProfile))) return false;
        return (bool)_gdProfileRepo!.Call("load_profile", profileId);
    }

    public void SaveProfile(bool immediate = false)
    {
        if (!EnsureConnected(nameof(SaveProfile))) return;
        _gdProfileRepo!.Call("save_profile", immediate);
    }

    public string GetCurrentProfileId()
    {
        if (!EnsureConnected(nameof(GetCurrentProfileId))) return "";
        return (string)_gdProfileRepo!.Call("get_current_profile_id");
    }

    public void ResetProfile()
    {
        if (!EnsureConnected(nameof(ResetProfile))) return;
        _gdProfileRepo!.Call("reset_profile");
    }

    /// <summary>
    /// Get a partial snapshot of the profile.
    /// NOTE: This returns a partial ProfileData with only basic fields populated.
    /// For full data, use the individual accessor methods (GetResources, ListCards, etc.).
    /// </summary>
    public ProfileData? GetProfileSnapshot()
    {
        if (_gdProfileRepo == null) return null;
        var dict = _gdProfileRepo.Call("snapshot").AsGodotDictionary();
        return DtoConverters.FromProfileDict(dict);
    }

    // =========================================================================
    // RESOURCE OPERATIONS
    // =========================================================================

    public Resources GetResources()
    {
        if (!EnsureConnected(nameof(GetResources))) return new Resources();
        var dict = _gdProfileRepo!.Call("get_resources").AsGodotDictionary();
        return DtoConverters.FromResourcesDict(dict);
    }

    public void UpdateResources(Dictionary<ResourceType, int> delta)
    {
        if (!EnsureConnected(nameof(UpdateResources))) return;
        var gdDelta = new Godot.Collections.Dictionary();
        foreach (var kvp in delta)
        {
            gdDelta[EnumSerializers.Serialize(kvp.Key)] = kvp.Value;
        }
        _gdProfileRepo!.Call("update_resources", gdDelta);
    }

    // =========================================================================
    // SUMMONER OPERATIONS
    // =========================================================================

    public string[] GetUnlockedSummoners()
    {
        if (!EnsureConnected(nameof(GetUnlockedSummoners))) return [];
        var arr = _gdProfileRepo!.Call("get_unlocked_summoners").AsGodotArray();
        var result = new List<string>();
        foreach (var item in arr)
        {
            result.Add(item.AsString());
        }
        return [.. result];
    }

    public bool IsSummonerUnlocked(string summonerId)
    {
        if (!EnsureConnected(nameof(IsSummonerUnlocked))) return false;
        return (bool)_gdProfileRepo!.Call("is_summoner_unlocked", summonerId);
    }

    public bool UnlockSummoner(string summonerId)
    {
        if (!EnsureConnected(nameof(UnlockSummoner))) return false;
        return (bool)_gdProfileRepo!.Call("unlock_summoner", summonerId);
    }

    public bool SetStartingSummoner(string summonerId, bool chosenRandom)
    {
        if (!EnsureConnected(nameof(SetStartingSummoner))) return false;
        return (bool)_gdProfileRepo!.Call("set_starting_summoner", summonerId, chosenRandom);
    }

    public SummonerInstance? GetSummonerInstance(string summonerId)
    {
        if (!EnsureConnected(nameof(GetSummonerInstance))) return null;
        var dict = _gdProfileRepo!.Call("get_summoner_instance", summonerId).AsGodotDictionary();
        return DtoConverters.FromSummonerDict(dict);
    }

    public SummonerInstance[] GetAllSummonerInstances()
    {
        if (!EnsureConnected(nameof(GetAllSummonerInstances))) return [];
        var arr = _gdProfileRepo!.Call("get_summoner_instances").AsGodotArray();
        var result = new List<SummonerInstance>();
        foreach (var item in arr)
        {
            var dict = item.AsGodotDictionary();
            var instance = DtoConverters.FromSummonerDict(dict);
            if (instance != null) result.Add(instance);
        }
        return [.. result];
    }

    public bool SaveSummonerInstance(SummonerInstance instance)
    {
        if (!EnsureConnected(nameof(SaveSummonerInstance))) return false;
        var dict = DtoConverters.ToDict(instance);
        return (bool)_gdProfileRepo!.Call("save_summoner_instance_dict", dict);
    }

    // =========================================================================
    // CARD COLLECTION OPERATIONS
    // =========================================================================

    public string[] GrantCards(IEnumerable<(string catalogId, string rarity)> cards)
    {
        // Delegate to the full overload with default AccountWide binding
        return GrantCards(cards.Select(c => (c.catalogId, c.rarity, ContentBinding.AccountWide, (string?)null)));
    }

    public string[] GrantCards(IEnumerable<(string catalogId, string rarity, ContentBinding binding, string? boundTo)> cards)
    {
        if (!EnsureConnected(nameof(GrantCards))) return [];
        var gdCards = new Godot.Collections.Array();
        foreach (var (catalogId, rarity, binding, boundTo) in cards)
        {
            var cardDict = new Godot.Collections.Dictionary
            {
                ["catalog_id"] = catalogId,
                ["rarity"] = rarity,
                ["binding"] = EnumSerializers.Serialize(binding)
            };
            if (binding == ContentBinding.SummonerBound && !string.IsNullOrEmpty(boundTo))
            {
                cardDict["bound_to"] = boundTo;
            }
            gdCards.Add(cardDict);
        }

        var result = _gdProfileRepo!.Call("grant_cards", gdCards).AsGodotArray();
        var ids = new List<string>();
        foreach (var item in result)
        {
            ids.Add(item.AsString());
        }
        return [.. ids];
    }

    public bool RemoveCard(string cardInstanceId)
    {
        if (!EnsureConnected(nameof(RemoveCard))) return false;
        return (bool)_gdProfileRepo!.Call("remove_card", cardInstanceId);
    }

    public CardInstance[] ListCards()
    {
        if (!EnsureConnected(nameof(ListCards))) return [];
        var arr = _gdProfileRepo!.Call("list_cards").AsGodotArray();
        var result = new List<CardInstance>();
        foreach (var item in arr)
        {
            var dict = item.AsGodotDictionary();
            var card = DtoConverters.FromCardDict(dict);
            if (card != null) result.Add(card);
        }
        return [.. result];
    }

    public int GetCardCount(string catalogId)
    {
        if (!EnsureConnected(nameof(GetCardCount))) return 0;
        return (int)_gdProfileRepo!.Call("get_card_count", catalogId);
    }

    public CardInstance? GetCard(string cardInstanceId)
    {
        if (!EnsureConnected(nameof(GetCard))) return null;
        var dict = _gdProfileRepo!.Call("get_card", cardInstanceId).AsGodotDictionary();
        return DtoConverters.FromCardDict(dict);
    }

    public bool UpdateCard(string cardInstanceId, Dictionary<string, object> updates)
    {
        if (!EnsureConnected(nameof(UpdateCard))) return false;
        var gdUpdates = new Godot.Collections.Dictionary();
        foreach (var kvp in updates)
        {
            gdUpdates[kvp.Key] = DtoConverters.ObjectToVariant(kvp.Value);
        }
        return (bool)_gdProfileRepo!.Call("update_card", cardInstanceId, gdUpdates);
    }

    // =========================================================================
    // DECK OPERATIONS
    // =========================================================================

    public string UpsertDeck(Deck deck)
    {
        if (!EnsureConnected(nameof(UpsertDeck))) return "";
        var gdDeck = DtoConverters.ToDict(deck);
        return (string)_gdProfileRepo!.Call("upsert_deck", gdDeck);
    }

    public bool DeleteDeck(string deckId)
    {
        if (!EnsureConnected(nameof(DeleteDeck))) return false;
        return (bool)_gdProfileRepo!.Call("delete_deck", deckId);
    }

    public Deck[] ListDecks()
    {
        if (!EnsureConnected(nameof(ListDecks))) return [];
        var arr = _gdProfileRepo!.Call("list_decks").AsGodotArray();
        var result = new List<Deck>();
        foreach (var item in arr)
        {
            var dict = item.AsGodotDictionary();
            var deck = DtoConverters.FromDeckDict(dict);
            if (deck != null) result.Add(deck);
        }
        return [.. result];
    }

    public Deck? GetDeck(string deckId)
    {
        if (!EnsureConnected(nameof(GetDeck))) return null;
        var dict = _gdProfileRepo!.Call("get_deck", deckId).AsGodotDictionary();
        return DtoConverters.FromDeckDict(dict);
    }

    // =========================================================================
    // CAMPAIGN OPERATIONS
    // =========================================================================

    public CampaignProgress GetCampaignProgress(string summonerId)
    {
        if (!EnsureConnected(nameof(GetCampaignProgress))) return new CampaignProgress();
        var dict = _gdProfileRepo!.Call("get_campaign_progress", summonerId).AsGodotDictionary();
        return DtoConverters.FromCampaignDict(dict) ?? new CampaignProgress();
    }

    public void UpdateCampaignProgress(string summonerId, CampaignProgress progress)
    {
        if (!EnsureConnected(nameof(UpdateCampaignProgress))) return;
        var gdProgress = DtoConverters.ToDict(progress);
        _gdProfileRepo!.Call("update_campaign_progress", gdProgress, summonerId);
    }

    public CampaignProgress GetSharedCampaignProgress()
    {
        if (!EnsureConnected(nameof(GetSharedCampaignProgress))) return new CampaignProgress();
        var dict = _gdProfileRepo!.Call("get_shared_campaign_progress").AsGodotDictionary();
        return DtoConverters.FromCampaignDict(dict) ?? new CampaignProgress();
    }

    public void UpdateSharedCampaignProgress(CampaignProgress progress)
    {
        if (!EnsureConnected(nameof(UpdateSharedCampaignProgress))) return;
        var gdProgress = DtoConverters.ToDict(progress);
        _gdProfileRepo!.Call("update_shared_campaign_progress", gdProgress);
    }

    // =========================================================================
    // COSMETIC OPERATIONS
    // =========================================================================

    public string[] GetOwnedCosmetics()
    {
        if (!EnsureConnected(nameof(GetOwnedCosmetics))) return [];
        var arr = _gdProfileRepo!.Call("get_owned_cosmetics").AsGodotArray();
        var result = new List<string>();
        foreach (var item in arr)
        {
            result.Add(item.AsString());
        }
        return [.. result];
    }

    public bool IsCosmeticOwned(string cosmeticId)
    {
        if (!EnsureConnected(nameof(IsCosmeticOwned))) return false;
        return (bool)_gdProfileRepo!.Call("is_cosmetic_owned", cosmeticId);
    }

    public bool GrantCosmetic(string cosmeticId)
    {
        if (!EnsureConnected(nameof(GrantCosmetic))) return false;
        return (bool)_gdProfileRepo!.Call("grant_cosmetic", cosmeticId);
    }

    public Dictionary<string, string> GetEquippedCosmetics()
    {
        if (!EnsureConnected(nameof(GetEquippedCosmetics))) return new Dictionary<string, string>();
        var dict = _gdProfileRepo!.Call("get_equipped_cosmetics").AsGodotDictionary();
        var result = new Dictionary<string, string>();
        foreach (var key in dict.Keys)
        {
            result[key.AsString()] = dict[key].AsString();
        }
        return result;
    }

    public bool EquipCosmetic(string slot, string cosmeticId)
    {
        if (!EnsureConnected(nameof(EquipCosmetic))) return false;
        return (bool)_gdProfileRepo!.Call("equip_cosmetic", slot, cosmeticId);
    }

    public Dictionary<string, string> GetSummonerSkins()
    {
        if (!EnsureConnected(nameof(GetSummonerSkins))) return new Dictionary<string, string>();
        var dict = _gdProfileRepo!.Call("get_summoner_skins").AsGodotDictionary();
        var result = new Dictionary<string, string>();
        foreach (var key in dict.Keys)
        {
            result[key.AsString()] = dict[key].AsString();
        }
        return result;
    }

    public bool SetSummonerSkin(string summonerId, string skinId)
    {
        if (!EnsureConnected(nameof(SetSummonerSkin))) return false;
        return (bool)_gdProfileRepo!.Call("set_summoner_skin", summonerId, skinId);
    }

    // =========================================================================
    // EMOTE OPERATIONS
    // =========================================================================

    public string[] GetOwnedEmotes()
    {
        if (!EnsureConnected(nameof(GetOwnedEmotes))) return [];
        var arr = _gdProfileRepo!.Call("get_owned_emotes").AsGodotArray();
        var result = new List<string>();
        foreach (var item in arr)
        {
            result.Add(item.AsString());
        }
        return [.. result];
    }

    public bool IsEmoteOwned(string emoteId)
    {
        if (!EnsureConnected(nameof(IsEmoteOwned))) return false;
        return (bool)_gdProfileRepo!.Call("is_emote_owned", emoteId);
    }

    public bool GrantEmote(string emoteId)
    {
        if (!EnsureConnected(nameof(GrantEmote))) return false;
        return (bool)_gdProfileRepo!.Call("grant_emote", emoteId);
    }

    public string[] GetEquippedEmotes()
    {
        if (!EnsureConnected(nameof(GetEquippedEmotes))) return [];
        var arr = _gdProfileRepo!.Call("get_equipped_emotes").AsGodotArray();
        var result = new List<string>();
        foreach (var item in arr)
        {
            result.Add(item.AsString());
        }
        return [.. result];
    }

    public bool EquipEmote(int slot, string emoteId)
    {
        if (!EnsureConnected(nameof(EquipEmote))) return false;
        return (bool)_gdProfileRepo!.Call("equip_emote", slot, emoteId);
    }

    // =========================================================================
    // SHOP OPERATIONS
    // =========================================================================

    public int GetPurchaseCount(string purchaseKey)
    {
        if (!EnsureConnected(nameof(GetPurchaseCount))) return 0;
        return (int)_gdProfileRepo!.Call("get_purchase_count", purchaseKey);
    }

    public bool IncrementPurchaseCount(string purchaseKey)
    {
        if (!EnsureConnected(nameof(IncrementPurchaseCount))) return false;
        return (bool)_gdProfileRepo!.Call("increment_purchase_count", purchaseKey);
    }

    public ShopRefreshState GetShopRefreshState(string shopId)
    {
        if (!EnsureConnected(nameof(GetShopRefreshState))) return new ShopRefreshState();
        var dict = _gdProfileRepo!.Call("get_shop_refresh_state", shopId).AsGodotDictionary();
        return DtoConverters.FromShopRefreshStateDict(dict);
    }

    public bool IncrementShopRefreshEpoch(string shopId)
    {
        if (!EnsureConnected(nameof(IncrementShopRefreshEpoch))) return false;
        return (bool)_gdProfileRepo!.Call("increment_shop_refresh_epoch", shopId);
    }

    // =========================================================================
    // SETTINGS OPERATIONS
    // =========================================================================

    public Settings GetSettings()
    {
        if (!EnsureConnected(nameof(GetSettings))) return new Settings();
        var dict = _gdProfileRepo!.Call("get_settings").AsGodotDictionary();
        return DtoConverters.FromSettingsDict(dict);
    }

    public void UpdateSettings(Settings settings)
    {
        if (!EnsureConnected(nameof(UpdateSettings))) return;
        var gdSettings = DtoConverters.ToDict(settings);
        _gdProfileRepo!.Call("update_settings", gdSettings);
    }

    // =========================================================================
    // ITEM OPERATIONS
    // =========================================================================

    public List<ItemInstance> ListItems()
    {
        if (!EnsureConnected(nameof(ListItems))) return [];

        var arr = _gdProfileRepo!.Call("list_items").AsGodotArray();
        var result = new List<ItemInstance>();

        foreach (var item in arr)
        {
            var dict = item.AsGodotDictionary();
            var itemData = DtoConverters.FromItemDict(dict);
            if (itemData != null) result.Add(itemData);
        }

        return result;
    }

    public void SaveItems(List<ItemInstance> items)
    {
        if (!EnsureConnected(nameof(SaveItems))) return;

        var gdItems = new Godot.Collections.Array();
        foreach (var item in items)
        {
            gdItems.Add(DtoConverters.ToDict(item));
        }

        _gdProfileRepo!.Call("save_items", gdItems);
    }

}
