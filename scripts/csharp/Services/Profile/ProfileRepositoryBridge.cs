using System;
using System.Collections.Generic;
using Godot;
using ProjectSummoner.Data.Profile;

namespace ProjectSummoner.Services.Profile;

/// <summary>
/// Bridge that wraps the GDScript ProfileRepo autoload.
/// Provides typed C# access to profile operations while delegating to existing GDScript implementation.
/// This allows gradual migration - C# code gets type safety now, full C# impl can come later.
/// </summary>
public partial class ProfileRepositoryBridge : Node, IProfileRepository
{
    public static ProfileRepositoryBridge? Instance { get; private set; }

    private Node? _gdProfileRepo;

    // Events
    public event Action<string>? ProfileLoaded;
    public event Action<string>? ProfileSaved;
    public event Action<string>? SaveFailed;
    public event Action? DataChanged;

    public override void _Ready()
    {
        Instance = this;
        CallDeferred(nameof(ConnectToGdScript));
    }

    private void ConnectToGdScript()
    {
        _gdProfileRepo = GetNode<Node>("/root/ProfileRepo");
        if (_gdProfileRepo == null)
        {
            GD.PushError("ProfileRepositoryBridge: ProfileRepo autoload not found");
            return;
        }

        // Connect GDScript signals to C# events
        _gdProfileRepo.Connect("profile_loaded", Callable.From<string>(OnProfileLoaded));
        _gdProfileRepo.Connect("profile_saved", Callable.From<string>(OnProfileSaved));
        _gdProfileRepo.Connect("save_failed", Callable.From<string>(OnSaveFailed));
        _gdProfileRepo.Connect("data_changed", Callable.From(OnDataChanged));

        GD.Print("ProfileRepositoryBridge: Connected to GDScript ProfileRepo");
    }

    private void OnProfileLoaded(string profileId) => ProfileLoaded?.Invoke(profileId);
    private void OnProfileSaved(string profileId) => ProfileSaved?.Invoke(profileId);
    private void OnSaveFailed(string error) => SaveFailed?.Invoke(error);
    private void OnDataChanged() => DataChanged?.Invoke();

    /// <summary>Check if the GDScript repository is connected and log error if not.</summary>
    private bool EnsureConnected(string methodName)
    {
        if (_gdProfileRepo != null) return true;
        GD.PushError($"ProfileRepositoryBridge.{methodName}: GDScript ProfileRepo not connected");
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
        return DictToProfileData(dict);
    }

    // =========================================================================
    // RESOURCE OPERATIONS
    // =========================================================================

    public ResourceData GetResources()
    {
        if (!EnsureConnected(nameof(GetResources))) return new ResourceData();
        var dict = _gdProfileRepo!.Call("get_resources").AsGodotDictionary();
        return new ResourceData
        {
            Gold = dict.TryGetValue("gold", out var gold) ? (int)gold : 0,
            Gems = dict.TryGetValue("gems", out var gems) ? (int)gems : 0,
            Essence = dict.TryGetValue("essence", out var essence) ? (int)essence : 0,
            Fragments = dict.TryGetValue("fragments", out var fragments) ? (int)fragments : 0
        };
    }

    public void UpdateResources(Dictionary<ResourceType, int> delta)
    {
        if (!EnsureConnected(nameof(UpdateResources))) return;
        var gdDelta = new Godot.Collections.Dictionary();
        foreach (var kvp in delta)
        {
            gdDelta[kvp.Key.ToKey()] = kvp.Value;
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

    public SummonerInstanceData? GetSummonerInstance(string summonerId)
    {
        if (!EnsureConnected(nameof(GetSummonerInstance))) return null;
        var dict = _gdProfileRepo!.Call("get_summoner_instance", summonerId).AsGodotDictionary();
        if (dict == null || dict.Count == 0) return null;
        return DictToSummonerInstance(dict);
    }

    public SummonerInstanceData[] GetAllSummonerInstances()
    {
        if (!EnsureConnected(nameof(GetAllSummonerInstances))) return [];
        var arr = _gdProfileRepo!.Call("get_summoner_instances").AsGodotArray();
        var result = new List<SummonerInstanceData>();
        foreach (var item in arr)
        {
            var dict = item.AsGodotDictionary();
            var instance = DictToSummonerInstance(dict);
            if (instance != null) result.Add(instance);
        }
        return [.. result];
    }

    public bool SaveSummonerInstance(SummonerInstanceData instance)
    {
        if (!EnsureConnected(nameof(SaveSummonerInstance))) return false;
        // This requires passing a SummonerInstance GDScript object
        // For now, we'll call save_summoner_instance_data which accepts a Dictionary
        var dict = new Godot.Collections.Dictionary
        {
            ["summoner_id"] = instance.SummonerId,
            ["level"] = instance.Level,
            ["xp"] = instance.Xp,
            ["acquired_boon_ids"] = ToGodotArray(instance.AcquiredBoonIds)
        };

        // Use the internal method if available, or create through SummonerInstance
        if (_gdProfileRepo!.HasMethod("save_summoner_instance_dict"))
        {
            return (bool)_gdProfileRepo.Call("save_summoner_instance_dict", dict);
        }

        // Fallback - this won't work without a SummonerInstance object
        GD.PushWarning("ProfileRepositoryBridge: save_summoner_instance_dict not available");
        return false;
    }

    // =========================================================================
    // CARD COLLECTION OPERATIONS
    // =========================================================================

    public string[] GrantCards(IEnumerable<(string catalogId, string rarity)> cards)
    {
        if (!EnsureConnected(nameof(GrantCards))) return [];
        var gdCards = new Godot.Collections.Array();
        foreach (var (catalogId, rarity) in cards)
        {
            gdCards.Add(new Godot.Collections.Dictionary
            {
                ["catalog_id"] = catalogId,
                ["rarity"] = rarity
            });
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

    public CardInstanceData[] ListCards()
    {
        if (!EnsureConnected(nameof(ListCards))) return [];
        var arr = _gdProfileRepo!.Call("list_cards").AsGodotArray();
        var result = new List<CardInstanceData>();
        foreach (var item in arr)
        {
            var dict = item.AsGodotDictionary();
            var card = DictToCardInstance(dict);
            if (card != null) result.Add(card);
        }
        return [.. result];
    }

    public int GetCardCount(string catalogId)
    {
        if (!EnsureConnected(nameof(GetCardCount))) return 0;
        return (int)_gdProfileRepo!.Call("get_card_count", catalogId);
    }

    public CardInstanceData? GetCard(string cardInstanceId)
    {
        if (!EnsureConnected(nameof(GetCard))) return null;
        var dict = _gdProfileRepo!.Call("get_card", cardInstanceId).AsGodotDictionary();
        if (dict == null || dict.Count == 0) return null;
        return DictToCardInstance(dict);
    }

    public bool UpdateCard(string cardInstanceId, Dictionary<string, object> updates)
    {
        if (!EnsureConnected(nameof(UpdateCard))) return false;
        var gdUpdates = new Godot.Collections.Dictionary();
        foreach (var kvp in updates)
        {
            gdUpdates[kvp.Key] = Variant.From(kvp.Value);
        }
        return (bool)_gdProfileRepo!.Call("update_card", cardInstanceId, gdUpdates);
    }

    // =========================================================================
    // DECK OPERATIONS
    // =========================================================================

    public string UpsertDeck(DeckData deck)
    {
        if (!EnsureConnected(nameof(UpsertDeck))) return "";
        var gdDeck = new Godot.Collections.Dictionary
        {
            ["id"] = deck.Id,
            ["name"] = deck.Name,
            ["summoner_id"] = deck.SummonerId,
            ["card_instance_ids"] = ToGodotArray(deck.CardInstanceIds)
        };
        return (string)_gdProfileRepo!.Call("upsert_deck", gdDeck);
    }

    public bool DeleteDeck(string deckId)
    {
        if (!EnsureConnected(nameof(DeleteDeck))) return false;
        return (bool)_gdProfileRepo!.Call("delete_deck", deckId);
    }

    public DeckData[] ListDecks()
    {
        if (!EnsureConnected(nameof(ListDecks))) return [];
        var arr = _gdProfileRepo!.Call("list_decks").AsGodotArray();
        var result = new List<DeckData>();
        foreach (var item in arr)
        {
            var dict = item.AsGodotDictionary();
            var deck = DictToDeckData(dict);
            if (deck != null) result.Add(deck);
        }
        return [.. result];
    }

    public DeckData? GetDeck(string deckId)
    {
        if (!EnsureConnected(nameof(GetDeck))) return null;
        var dict = _gdProfileRepo!.Call("get_deck", deckId).AsGodotDictionary();
        if (dict == null || dict.Count == 0) return null;
        return DictToDeckData(dict);
    }

    // =========================================================================
    // CAMPAIGN OPERATIONS
    // =========================================================================

    public CampaignProgressData GetCampaignProgress(string summonerId)
    {
        if (!EnsureConnected(nameof(GetCampaignProgress))) return new CampaignProgressData();
        var dict = _gdProfileRepo!.Call("get_campaign_progress_for_summoner", summonerId).AsGodotDictionary();
        return DictToCampaignProgress(dict) ?? new CampaignProgressData();
    }

    public void UpdateCampaignProgress(string summonerId, CampaignProgressData progress)
    {
        if (!EnsureConnected(nameof(UpdateCampaignProgress))) return;
        var gdProgress = CampaignProgressToDict(progress);
        _gdProfileRepo!.Call("update_campaign_progress_for_summoner", summonerId, gdProgress);
    }

    public CampaignProgressData GetSharedCampaignProgress()
    {
        if (!EnsureConnected(nameof(GetSharedCampaignProgress))) return new CampaignProgressData();
        var dict = _gdProfileRepo!.Call("get_shared_campaign_progress").AsGodotDictionary();
        return DictToCampaignProgress(dict) ?? new CampaignProgressData();
    }

    public void UpdateSharedCampaignProgress(CampaignProgressData progress)
    {
        if (!EnsureConnected(nameof(UpdateSharedCampaignProgress))) return;
        var gdProgress = CampaignProgressToDict(progress);
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

    public (int epoch, string lastRefreshAt) GetShopRefreshState(string shopId)
    {
        if (!EnsureConnected(nameof(GetShopRefreshState))) return (0, "");
        var dict = _gdProfileRepo!.Call("get_shop_refresh_state", shopId).AsGodotDictionary();
        var epoch = dict.TryGetValue("refresh_epoch", out var e) ? (int)e : 0;
        var lastRefresh = dict.TryGetValue("last_refresh_at", out var lr) ? lr.AsString() : "";
        return (epoch, lastRefresh);
    }

    public bool IncrementShopRefreshEpoch(string shopId)
    {
        if (!EnsureConnected(nameof(IncrementShopRefreshEpoch))) return false;
        return (bool)_gdProfileRepo!.Call("increment_shop_refresh_epoch", shopId);
    }

    // =========================================================================
    // SETTINGS OPERATIONS
    // =========================================================================

    public SettingsData GetSettings()
    {
        if (!EnsureConnected(nameof(GetSettings))) return new SettingsData();
        var dict = _gdProfileRepo!.Call("get_settings").AsGodotDictionary();
        return new SettingsData
        {
            SfxVolume = dict.TryGetValue("sfx_volume", out var sfx) ? (float)sfx : 1.0f,
            MusicVolume = dict.TryGetValue("music_volume", out var music) ? (float)music : 1.0f,
            Lang = dict.TryGetValue("lang", out var lang) ? lang.AsString() : "en"
        };
    }

    public void UpdateSettings(SettingsData settings)
    {
        if (!EnsureConnected(nameof(UpdateSettings))) return;
        var gdSettings = new Godot.Collections.Dictionary
        {
            ["sfx_volume"] = settings.SfxVolume,
            ["music_volume"] = settings.MusicVolume,
            ["lang"] = settings.Lang
        };
        _gdProfileRepo!.Call("update_settings", gdSettings);
    }

    // =========================================================================
    // CONVERSION HELPERS
    // =========================================================================

    private static Godot.Collections.Array ToGodotArray(IEnumerable<string> items)
    {
        var arr = new Godot.Collections.Array();
        foreach (var item in items)
        {
            arr.Add(item);
        }
        return arr;
    }

    /// <summary>
    /// Converts GDScript profile dictionary to ProfileData.
    /// NOTE: This is a partial conversion. For complete data, use individual accessor methods.
    /// Populated fields: Version, ProfileId, UpdatedAt, CatalogVersion, Resources, UnlockedSummoners.
    /// </summary>
    private static ProfileData? DictToProfileData(Godot.Collections.Dictionary dict)
    {
        if (dict == null || dict.Count == 0) return null;

        var profileData = new ProfileData
        {
            Version = dict.TryGetValue("version", out var v) ? (int)v : ProfileData.CurrentVersion,
            ProfileId = dict.TryGetValue("profile_id", out var pid) ? pid.AsString() : "",
            UpdatedAt = dict.TryGetValue("updated_at", out var updated) ? ConvertToLong(updated) : 0,
            CatalogVersion = dict.TryGetValue("catalog_version", out var cv) ? cv.AsString() : "1.0.0"
        };

        // Convert resources if present
        if (dict.TryGetValue("resources", out var resourcesVar) && resourcesVar.VariantType == Variant.Type.Dictionary)
        {
            var resourcesDict = resourcesVar.AsGodotDictionary();
            profileData.Resources = new ResourceData
            {
                Gold = resourcesDict.TryGetValue("gold", out var gold) ? (int)gold : 0,
                Gems = resourcesDict.TryGetValue("gems", out var gems) ? (int)gems : 0,
                Essence = resourcesDict.TryGetValue("essence", out var essence) ? (int)essence : 0,
                Fragments = resourcesDict.TryGetValue("fragments", out var fragments) ? (int)fragments : 0
            };
        }

        // Convert unlocked summoners if present
        if (dict.TryGetValue("unlocked_summoners", out var summonersVar) && summonersVar.VariantType == Variant.Type.Array)
        {
            var summonersArr = summonersVar.AsGodotArray();
            foreach (var s in summonersArr)
            {
                profileData.UnlockedSummoners.Add(s.AsString());
            }
        }

        return profileData;
    }

    /// <summary>Safely converts a Variant to long, handling both int and float types.</summary>
    private static long ConvertToLong(Variant value)
    {
        return value.VariantType switch
        {
            Variant.Type.Int => (long)(int)value,
            Variant.Type.Float => (long)(double)value,
            _ => 0
        };
    }

    private static SummonerInstanceData? DictToSummonerInstance(Godot.Collections.Dictionary dict)
    {
        if (dict == null || dict.Count == 0) return null;

        var summonerId = dict.TryGetValue("summoner_id", out var sid) ? sid.AsString() : "";
        if (string.IsNullOrEmpty(summonerId)) return null;

        var boons = new List<string>();
        if (dict.TryGetValue("acquired_boon_ids", out var boonsVar))
        {
            var boonsArr = boonsVar.AsGodotArray();
            foreach (var b in boonsArr)
            {
                boons.Add(b.AsString());
            }
        }

        return new SummonerInstanceData
        {
            SummonerId = summonerId,
            Level = dict.TryGetValue("level", out var lvl) ? (int)lvl : 1,
            Xp = dict.TryGetValue("xp", out var xp) ? (int)xp : 0,
            AcquiredBoonIds = boons
        };
    }

    private static CardInstanceData? DictToCardInstance(Godot.Collections.Dictionary dict)
    {
        if (dict == null || dict.Count == 0) return null;

        var id = dict.TryGetValue("id", out var idVar) ? idVar.AsString() : "";
        var catalogId = dict.TryGetValue("catalog_id", out var cid) ? cid.AsString() : "";
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(catalogId)) return null;

        var upgrades = new List<string>();
        if (dict.TryGetValue("upgrades", out var upgradesVar))
        {
            var upgradesArr = upgradesVar.AsGodotArray();
            foreach (var u in upgradesArr)
            {
                upgrades.Add(u.AsString());
            }
        }

        return new CardInstanceData
        {
            Id = id,
            CatalogId = catalogId,
            ProfileId = dict.TryGetValue("profile_id", out var pid) ? pid.AsString() : "",
            Rarity = dict.TryGetValue("rarity", out var rarity) ? rarity.AsString() : "common",
            Level = dict.TryGetValue("level", out var lvl) ? (int)lvl : 1,
            Xp = dict.TryGetValue("xp", out var xp) ? (int)xp : 0,
            Upgrades = upgrades,
            CreatedAt = dict.TryGetValue("created_at", out var created) ? ConvertToLong(created) : 0
        };
    }

    private static DeckData? DictToDeckData(Godot.Collections.Dictionary dict)
    {
        if (dict == null || dict.Count == 0) return null;

        var id = dict.TryGetValue("id", out var idVar) ? idVar.AsString() : "";
        if (string.IsNullOrEmpty(id)) return null;

        var cardIds = new List<string>();
        if (dict.TryGetValue("card_instance_ids", out var cardsVar))
        {
            var cardsArr = cardsVar.AsGodotArray();
            foreach (var c in cardsArr)
            {
                cardIds.Add(c.AsString());
            }
        }

        return new DeckData
        {
            Id = id,
            SummonerId = dict.TryGetValue("summoner_id", out var sid) ? sid.AsString() : "",
            Name = dict.TryGetValue("name", out var name) ? name.AsString() : "Deck",
            CardInstanceIds = cardIds
        };
    }

    private static CampaignProgressData? DictToCampaignProgress(Godot.Collections.Dictionary? dict)
    {
        if (dict == null || dict.Count == 0) return null;

        var completed = new List<string>();
        if (dict.TryGetValue("completed_battles", out var completedVar))
        {
            var completedArr = completedVar.AsGodotArray();
            foreach (var c in completedArr)
            {
                completed.Add(c.AsString());
            }
        }

        return new CampaignProgressData
        {
            CompletedBattles = completed,
            CurrentBattle = dict.TryGetValue("current_battle", out var cb) ? cb.AsString() : null
        };
    }

    private static Godot.Collections.Dictionary CampaignProgressToDict(CampaignProgressData progress)
    {
        return new Godot.Collections.Dictionary
        {
            ["completed_battles"] = ToGodotArray(progress.CompletedBattles),
            ["current_battle"] = progress.CurrentBattle ?? ""
        };
    }
}
