using System.Collections.Generic;
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
/// Centralized converters for Godot.Collections.Dictionary ↔ Domain model conversions.
/// All ProfileRepository conversion logic is consolidated here for consistency and testability.
/// </summary>
public static class DtoConverters
{
    // =========================================================================
    // SummonerInstance
    // =========================================================================

    /// <summary>Convert SummonerInstance to Godot Dictionary for GDScript.</summary>
    public static Godot.Collections.Dictionary ToDict(SummonerInstance instance)
    {
        var equippedDict = new Godot.Collections.Dictionary();
        foreach (var (slot, itemId) in instance.EquippedItems)
        {
            equippedDict[EnumSerializers.Serialize(slot)] = itemId ?? "";
        }

        return new Godot.Collections.Dictionary
        {
            ["summoner_id"] = instance.SummonerId,
            ["level"] = instance.Level,
            ["xp"] = instance.Xp,
            ["acquired_boon_ids"] = ToGodotArray(instance.AcquiredBoonIds),
            ["equipped_items"] = equippedDict
        };
    }

    /// <summary>
    /// Convert Godot Dictionary to SummonerInstance.
    /// Returns null if dict is empty or missing required fields.
    /// </summary>
    public static SummonerInstance? FromSummonerDict(Godot.Collections.Dictionary? dict)
    {
        if (dict == null || dict.Count == 0) return null;

        var summonerId = GetRequiredString(dict, "summoner_id");
        if (summonerId == null) return null;

        var boons = new List<string>();
        if (dict.TryGetValue("acquired_boon_ids", out var boonsVar))
        {
            var boonsArr = boonsVar.AsGodotArray();
            foreach (var b in boonsArr)
            {
                boons.Add(b.AsString());
            }
        }

        // Deserialize equipped_items (string keys from GDScript → ItemSlot enum keys)
        var equippedItems = new Dictionary<ItemSlot, string?>
        {
            [ItemSlot.Weapon] = null,
            [ItemSlot.Ring1] = null,
            [ItemSlot.Ring2] = null,
            [ItemSlot.Vestments] = null
        };
        if (dict.TryGetValue("equipped_items", out var equippedVar) && equippedVar.VariantType == Variant.Type.Dictionary)
        {
            var equippedDict = equippedVar.AsGodotDictionary();
            foreach (var key in equippedDict.Keys)
            {
                var slotStr = key.AsString();
                var itemId = equippedDict[key].VariantType != Variant.Type.Nil ? equippedDict[key].AsString() : null;
                if (string.IsNullOrEmpty(itemId)) itemId = null;

                // Use EnumSerializers for consistent deserialization
                var slot = EnumSerializers.DeserializeSlot(slotStr);
                if (slot.HasValue)
                {
                    equippedItems[slot.Value] = itemId;
                }
            }
        }

        return new SummonerInstance
        {
            SummonerId = summonerId,
            Level = GetInt(dict, "level", 1),
            Xp = GetInt(dict, "xp", 0),
            AcquiredBoonIds = boons,
            EquippedItems = equippedItems
        };
    }

    // =========================================================================
    // CardInstance
    // =========================================================================

    /// <summary>Convert CardInstance to Godot Dictionary for GDScript.</summary>
    public static Godot.Collections.Dictionary ToDict(CardInstance card)
    {
        var dict = new Godot.Collections.Dictionary
        {
            ["id"] = card.Id,
            ["catalog_id"] = card.CatalogId,
            ["profile_id"] = card.ProfileId,
            ["rarity"] = card.Rarity,
            ["level"] = card.Level,
            ["xp"] = card.Xp,
            ["upgrades"] = ToGodotArray(card.Upgrades),
            ["created_at"] = card.CreatedAt,
            ["binding"] = EnumSerializers.Serialize(card.Binding)
        };

        if (card.RollJson != null)
            dict["roll_json"] = card.RollJson;

        if (card.BoundToSummonerId != null)
            dict["bound_to"] = card.BoundToSummonerId;

        return dict;
    }

    /// <summary>
    /// Convert Godot Dictionary to CardInstance.
    /// Returns null if dict is empty or missing required fields.
    /// </summary>
    public static CardInstance? FromCardDict(Godot.Collections.Dictionary? dict)
    {
        if (dict == null || dict.Count == 0) return null;

        var id = GetRequiredString(dict, "id");
        var catalogId = GetRequiredString(dict, "catalog_id");
        if (id == null || catalogId == null) return null;

        var upgrades = new List<string>();
        if (dict.TryGetValue("upgrades", out var upgradesVar))
        {
            var upgradesArr = upgradesVar.AsGodotArray();
            foreach (var u in upgradesArr)
            {
                upgrades.Add(u.AsString());
            }
        }

        // Parse binding with validation
        var binding = ContentBinding.AccountWide;
        if (dict.TryGetValue("binding", out var bindingVar))
        {
            binding = EnumSerializers.DeserializeBinding(bindingVar.AsInt32());
        }

        return new CardInstance
        {
            Id = id,
            CatalogId = catalogId,
            ProfileId = GetString(dict, "profile_id", ""),
            Rarity = GetString(dict, "rarity", "common"),
            Level = GetInt(dict, "level", 1),
            Xp = GetInt(dict, "xp", 0),
            Upgrades = upgrades,
            RollJson = GetNullableString(dict, "roll_json"),
            CreatedAt = GetLong(dict, "created_at", 0),
            Binding = binding,
            BoundToSummonerId = GetNullableString(dict, "bound_to")
        };
    }

    // =========================================================================
    // ItemInstance
    // =========================================================================

    /// <summary>Convert ItemInstance to Godot Dictionary for GDScript.</summary>
    public static Godot.Collections.Dictionary ToDict(ItemInstance item)
    {
        return new Godot.Collections.Dictionary
        {
            ["id"] = item.Id,
            ["catalog_id"] = item.CatalogId,
            ["equipped_by"] = item.EquippedBySummonerId ?? "",
            ["bound_to"] = item.BoundToSummonerId ?? "",
            ["slot"] = item.EquippedSlot.HasValue ? EnumSerializers.Serialize(item.EquippedSlot.Value) : ""
        };
    }

    /// <summary>
    /// Convert Godot Dictionary to ItemInstance.
    /// Returns null if dict is empty or missing required fields.
    /// </summary>
    public static ItemInstance? FromItemDict(Godot.Collections.Dictionary? dict)
    {
        if (dict == null || dict.Count == 0) return null;

        var id = GetRequiredString(dict, "id");
        var catalogId = GetRequiredString(dict, "catalog_id");
        if (id == null || catalogId == null) return null;

        return new ItemInstance
        {
            Id = id,
            CatalogId = catalogId,
            EquippedBySummonerId = GetNullableString(dict, "equipped_by"),
            BoundToSummonerId = GetNullableString(dict, "bound_to"),
            EquippedSlot = EnumSerializers.DeserializeSlot(GetNullableString(dict, "slot"))
        };
    }

    // =========================================================================
    // Deck
    // =========================================================================

    /// <summary>Convert Deck to Godot Dictionary for GDScript.</summary>
    public static Godot.Collections.Dictionary ToDict(Deck deck)
    {
        return new Godot.Collections.Dictionary
        {
            ["id"] = deck.Id,
            ["profile_id"] = deck.ProfileId,
            ["summoner_id"] = deck.SummonerId,
            ["name"] = deck.Name,
            ["slot"] = deck.Slot,
            ["is_active"] = deck.IsActive,
            ["card_instance_ids"] = ToGodotArray(deck.CardInstanceIds),
            ["updated_at"] = deck.UpdatedAt
        };
    }

    /// <summary>
    /// Convert Godot Dictionary to Deck.
    /// Returns null if dict is empty or missing required fields.
    /// </summary>
    public static Deck? FromDeckDict(Godot.Collections.Dictionary? dict)
    {
        if (dict == null || dict.Count == 0) return null;

        var id = GetRequiredString(dict, "id");
        var summonerId = GetRequiredString(dict, "summoner_id");
        if (id == null || summonerId == null) return null;

        var cardIds = new List<string>();
        if (dict.TryGetValue("card_instance_ids", out var cardsVar))
        {
            var cardsArr = cardsVar.AsGodotArray();
            foreach (var c in cardsArr)
            {
                cardIds.Add(c.AsString());
            }
        }

        return new Deck
        {
            Id = id,
            ProfileId = GetString(dict, "profile_id", ""),
            SummonerId = summonerId,
            Name = GetString(dict, "name", "Deck"),
            Slot = GetInt(dict, "slot", 0),
            IsActive = GetBool(dict, "is_active", false),
            CardInstanceIds = cardIds,
            UpdatedAt = GetLong(dict, "updated_at", 0)
        };
    }

    // =========================================================================
    // CampaignProgress
    // =========================================================================

    /// <summary>Convert CampaignProgress to Godot Dictionary for GDScript.</summary>
    public static Godot.Collections.Dictionary ToDict(CampaignProgress progress)
    {
        var dict = new Godot.Collections.Dictionary
        {
            ["completed_battles"] = ToGodotArray(progress.CompletedBattles),
            ["current_battle"] = progress.CurrentBattle ?? "",
            ["gold"] = progress.Gold
        };

        // Add pending_reward if present
        if (progress.PendingReward != null)
        {
            var rewardDict = new Godot.Collections.Dictionary();
            foreach (var (key, value) in progress.PendingReward)
            {
                rewardDict[key] = Variant.From(value);
            }
            dict["pending_reward"] = rewardDict;
        }

        // Add story_arcs if present
        if (progress.StoryArcs.Count > 0)
        {
            var arcsDict = new Godot.Collections.Dictionary();
            foreach (var (arcId, arcProgress) in progress.StoryArcs)
            {
                arcsDict[arcId] = ToDict(arcProgress);
            }
            dict["story_arcs"] = arcsDict;
        }

        return dict;
    }

    /// <summary>Convert StoryArcProgress to Godot Dictionary for GDScript.</summary>
    public static Godot.Collections.Dictionary ToDict(StoryArcProgress arcProgress)
    {
        var flagsDict = new Godot.Collections.Dictionary();
        foreach (var (key, value) in arcProgress.Flags)
        {
            flagsDict[key] = Variant.From(value);
        }

        return new Godot.Collections.Dictionary
        {
            ["completed_events"] = ToGodotArray(arcProgress.CompletedEvents),
            ["current_event"] = arcProgress.CurrentEvent ?? "",
            ["flags"] = flagsDict
        };
    }

    /// <summary>
    /// Convert Godot Dictionary to CampaignProgress.
    /// Returns null if dict is null (but empty dict returns default data).
    /// </summary>
    public static CampaignProgress? FromCampaignDict(Godot.Collections.Dictionary? dict)
    {
        if (dict == null) return null;
        if (dict.Count == 0) return new CampaignProgress();

        var completed = new List<string>();
        if (dict.TryGetValue("completed_battles", out var completedVar))
        {
            var completedArr = completedVar.AsGodotArray();
            foreach (var c in completedArr)
            {
                completed.Add(c.AsString());
            }
        }

        // Parse pending_reward if present
        Dictionary<string, object>? pendingReward = null;
        if (dict.TryGetValue("pending_reward", out var rewardVar) && rewardVar.VariantType == Variant.Type.Dictionary)
        {
            pendingReward = new Dictionary<string, object>();
            var rewardDict = rewardVar.AsGodotDictionary();
            foreach (var key in rewardDict.Keys)
            {
                pendingReward[key.AsString()] = rewardDict[key].Obj ?? rewardDict[key].AsString();
            }
        }

        // Parse story_arcs if present
        var storyArcs = new Dictionary<string, StoryArcProgress>();
        if (dict.TryGetValue("story_arcs", out var arcsVar) && arcsVar.VariantType == Variant.Type.Dictionary)
        {
            var arcsDict = arcsVar.AsGodotDictionary();
            foreach (var key in arcsDict.Keys)
            {
                var arcDict = arcsDict[key].AsGodotDictionary();
                var arcProgress = FromStoryArcDict(arcDict);
                if (arcProgress != null)
                {
                    storyArcs[key.AsString()] = arcProgress;
                }
            }
        }

        return new CampaignProgress
        {
            CompletedBattles = completed,
            CurrentBattle = GetNullableString(dict, "current_battle"),
            Gold = GetInt(dict, "gold", 0),
            PendingReward = pendingReward,
            StoryArcs = storyArcs
        };
    }

    /// <summary>
    /// Convert Godot Dictionary to StoryArcProgress.
    /// </summary>
    public static StoryArcProgress? FromStoryArcDict(Godot.Collections.Dictionary? dict)
    {
        if (dict == null || dict.Count == 0) return null;

        var completedEvents = new List<string>();
        if (dict.TryGetValue("completed_events", out var eventsVar))
        {
            var eventsArr = eventsVar.AsGodotArray();
            foreach (var e in eventsArr)
            {
                completedEvents.Add(e.AsString());
            }
        }

        var flags = new Dictionary<string, object>();
        if (dict.TryGetValue("flags", out var flagsVar) && flagsVar.VariantType == Variant.Type.Dictionary)
        {
            var flagsDict = flagsVar.AsGodotDictionary();
            foreach (var key in flagsDict.Keys)
            {
                flags[key.AsString()] = flagsDict[key].Obj ?? flagsDict[key].AsString();
            }
        }

        return new StoryArcProgress
        {
            CompletedEvents = completedEvents,
            CurrentEvent = GetNullableString(dict, "current_event"),
            Flags = flags
        };
    }

    // =========================================================================
    // Resources
    // =========================================================================

    /// <summary>Convert Resources to Godot Dictionary for GDScript.</summary>
    public static Godot.Collections.Dictionary ToDict(Resources resources)
    {
        return new Godot.Collections.Dictionary
        {
            ["gold"] = resources.Gold,
            ["gems"] = resources.Gems,
            ["essence"] = resources.Essence,
            ["fragments"] = resources.Fragments
        };
    }

    /// <summary>
    /// Convert Godot Dictionary to Resources.
    /// Returns default Resources if dict is null or empty.
    /// </summary>
    public static Resources FromResourcesDict(Godot.Collections.Dictionary? dict)
    {
        if (dict == null || dict.Count == 0) return new Resources();

        return new Resources
        {
            Gold = GetInt(dict, "gold", 0),
            Gems = GetInt(dict, "gems", 0),
            Essence = GetInt(dict, "essence", 0),
            Fragments = GetInt(dict, "fragments", 0)
        };
    }

    // =========================================================================
    // Settings
    // =========================================================================

    /// <summary>Convert Settings to Godot Dictionary for GDScript.</summary>
    public static Godot.Collections.Dictionary ToDict(Settings settings)
    {
        return new Godot.Collections.Dictionary
        {
            ["sfx_volume"] = settings.SfxVolume,
            ["music_volume"] = settings.MusicVolume,
            ["lang"] = settings.Lang
        };
    }

    /// <summary>
    /// Convert Godot Dictionary to Settings.
    /// Returns default Settings if dict is null or empty.
    /// </summary>
    public static Settings FromSettingsDict(Godot.Collections.Dictionary? dict)
    {
        if (dict == null || dict.Count == 0) return new Settings();

        return new Settings
        {
            SfxVolume = GetFloat(dict, "sfx_volume", 1.0f),
            MusicVolume = GetFloat(dict, "music_volume", 1.0f),
            Lang = GetString(dict, "lang", "en")
        };
    }

    // =========================================================================
    // ShopRefreshState
    // =========================================================================

    /// <summary>Convert ShopRefreshState to Godot Dictionary for GDScript.</summary>
    public static Godot.Collections.Dictionary ToDict(ShopRefreshState state)
    {
        return new Godot.Collections.Dictionary
        {
            ["refresh_epoch"] = state.RefreshEpoch,
            ["last_refresh_at"] = state.LastRefreshAt
        };
    }

    /// <summary>
    /// Convert Godot Dictionary to ShopRefreshState.
    /// Returns default ShopRefreshState if dict is null or empty.
    /// </summary>
    public static ShopRefreshState FromShopRefreshStateDict(Godot.Collections.Dictionary? dict)
    {
        if (dict == null || dict.Count == 0) return new ShopRefreshState();

        return new ShopRefreshState
        {
            RefreshEpoch = GetInt(dict, "refresh_epoch", 0),
            LastRefreshAt = GetString(dict, "last_refresh_at", "")
        };
    }

    // =========================================================================
    // ProfileData (partial - for snapshot)
    // =========================================================================

    /// <summary>
    /// Convert Godot Dictionary to partial ProfileData (for snapshot).
    /// NOTE: This is a partial conversion. For complete data, use individual accessor methods.
    /// Populated fields: Version, ProfileId, UpdatedAt, CatalogVersion, Resources, UnlockedSummoners.
    /// </summary>
    public static ProfileData? FromProfileDict(Godot.Collections.Dictionary? dict)
    {
        if (dict == null || dict.Count == 0) return null;

        var profileData = new ProfileData
        {
            Version = GetInt(dict, "version", ProfileData.CurrentVersion),
            ProfileId = GetString(dict, "profile_id", ""),
            UpdatedAt = GetLong(dict, "updated_at", 0),
            CatalogVersion = GetString(dict, "catalog_version", "1.0.0")
        };

        // Convert resources if present
        if (dict.TryGetValue("resources", out var resourcesVar) && resourcesVar.VariantType == Variant.Type.Dictionary)
        {
            var resourcesDict = resourcesVar.AsGodotDictionary();
            profileData.Resources = FromResourcesDict(resourcesDict);
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

    // =========================================================================
    // Helpers - Array Conversion
    // =========================================================================

    /// <summary>Convert IEnumerable of strings to Godot Array.</summary>
    public static Godot.Collections.Array ToGodotArray(IEnumerable<string> items)
    {
        var arr = new Godot.Collections.Array();
        foreach (var item in items)
        {
            arr.Add(item);
        }
        return arr;
    }

    // =========================================================================
    // Helpers - Dictionary Value Extraction
    // =========================================================================

    /// <summary>Get required string from dictionary, returns null and logs warning if missing/empty.</summary>
    private static string? GetRequiredString(Godot.Collections.Dictionary dict, string key)
    {
        if (!dict.TryGetValue(key, out var value))
        {
            GD.PushWarning($"DtoConverters: Missing required field '{key}'");
            return null;
        }
        if (value.VariantType == Variant.Type.Nil)
        {
            GD.PushWarning($"DtoConverters: Required field '{key}' is null");
            return null;
        }
        var str = value.AsString();
        if (string.IsNullOrEmpty(str))
        {
            GD.PushWarning($"DtoConverters: Required field '{key}' is empty");
            return null;
        }
        return str;
    }

    /// <summary>Get string from dictionary with default value.</summary>
    private static string GetString(Godot.Collections.Dictionary dict, string key, string defaultValue)
    {
        if (!dict.TryGetValue(key, out var value)) return defaultValue;
        if (value.VariantType == Variant.Type.Nil) return defaultValue;
        return value.AsString();
    }

    /// <summary>Get nullable string from dictionary, treating empty strings as null.</summary>
    private static string? GetNullableString(Godot.Collections.Dictionary dict, string key)
    {
        if (!dict.TryGetValue(key, out var value)) return null;
        if (value.VariantType == Variant.Type.Nil) return null;
        var str = value.AsString();
        return string.IsNullOrEmpty(str) ? null : str;
    }

    /// <summary>Get int from dictionary with default value.</summary>
    private static int GetInt(Godot.Collections.Dictionary dict, string key, int defaultValue)
    {
        if (!dict.TryGetValue(key, out var value)) return defaultValue;
        return value.VariantType switch
        {
            Variant.Type.Int => value.AsInt32(),
            Variant.Type.Float => (int)value.AsDouble(),
            _ => defaultValue
        };
    }

    /// <summary>Get long from dictionary with default value.</summary>
    private static long GetLong(Godot.Collections.Dictionary dict, string key, long defaultValue)
    {
        if (!dict.TryGetValue(key, out var value)) return defaultValue;
        return value.VariantType switch
        {
            Variant.Type.Int => (long)value.AsInt32(),
            Variant.Type.Float => (long)value.AsDouble(),
            _ => defaultValue
        };
    }

    /// <summary>Get float from dictionary with default value.</summary>
    private static float GetFloat(Godot.Collections.Dictionary dict, string key, float defaultValue)
    {
        if (!dict.TryGetValue(key, out var value)) return defaultValue;
        return value.VariantType switch
        {
            Variant.Type.Float => (float)value.AsDouble(),
            Variant.Type.Int => value.AsInt32(),
            _ => defaultValue
        };
    }

    /// <summary>Get bool from dictionary with default value.</summary>
    private static bool GetBool(Godot.Collections.Dictionary dict, string key, bool defaultValue)
    {
        if (!dict.TryGetValue(key, out var value)) return defaultValue;
        if (value.VariantType == Variant.Type.Bool) return value.AsBool();
        return defaultValue;
    }
}
