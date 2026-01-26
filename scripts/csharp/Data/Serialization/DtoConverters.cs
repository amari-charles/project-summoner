using System.Collections.Generic;
using Godot;
using ProjectSummoner.Data.Items;
using ProjectSummoner.Data.Profile;

namespace ProjectSummoner.Data.Serialization;

/// <summary>
/// Centralized converters for Godot.Collections.Dictionary ↔ Domain model conversions.
/// All ProfileRepositoryBridge conversion logic is consolidated here for consistency and testability.
/// </summary>
public static class DtoConverters
{
    // =========================================================================
    // SummonerInstanceData
    // =========================================================================

    /// <summary>Convert SummonerInstanceData to Godot Dictionary for GDScript.</summary>
    public static Godot.Collections.Dictionary ToDict(SummonerInstanceData instance)
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
    /// Convert Godot Dictionary to SummonerInstanceData.
    /// Returns null if dict is empty or missing required fields.
    /// </summary>
    public static SummonerInstanceData? FromSummonerDict(Godot.Collections.Dictionary? dict)
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

        return new SummonerInstanceData
        {
            SummonerId = summonerId,
            Level = GetInt(dict, "level", 1),
            Xp = GetInt(dict, "xp", 0),
            AcquiredBoonIds = boons,
            EquippedItems = equippedItems
        };
    }

    // =========================================================================
    // CardInstanceData
    // =========================================================================

    /// <summary>Convert CardInstanceData to Godot Dictionary for GDScript.</summary>
    public static Godot.Collections.Dictionary ToDict(CardInstanceData card)
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
    /// Convert Godot Dictionary to CardInstanceData.
    /// Returns null if dict is empty or missing required fields.
    /// </summary>
    public static CardInstanceData? FromCardDict(Godot.Collections.Dictionary? dict)
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

        return new CardInstanceData
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
    // ItemInstanceData
    // =========================================================================

    /// <summary>Convert ItemInstanceData to Godot Dictionary for GDScript.</summary>
    public static Godot.Collections.Dictionary ToDict(ItemInstanceData item)
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
    /// Convert Godot Dictionary to ItemInstanceData.
    /// Returns null if dict is empty or missing required fields.
    /// </summary>
    public static ItemInstanceData? FromItemDict(Godot.Collections.Dictionary? dict)
    {
        if (dict == null || dict.Count == 0) return null;

        var id = GetRequiredString(dict, "id");
        var catalogId = GetRequiredString(dict, "catalog_id");
        if (id == null || catalogId == null) return null;

        return new ItemInstanceData
        {
            Id = id,
            CatalogId = catalogId,
            EquippedBySummonerId = GetNullableString(dict, "equipped_by"),
            BoundToSummonerId = GetNullableString(dict, "bound_to"),
            EquippedSlot = EnumSerializers.DeserializeSlot(GetNullableString(dict, "slot"))
        };
    }

    // =========================================================================
    // DeckData
    // =========================================================================

    /// <summary>Convert DeckData to Godot Dictionary for GDScript.</summary>
    public static Godot.Collections.Dictionary ToDict(DeckData deck)
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
    /// Convert Godot Dictionary to DeckData.
    /// Returns null if dict is empty or missing required fields.
    /// </summary>
    public static DeckData? FromDeckDict(Godot.Collections.Dictionary? dict)
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

        return new DeckData
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
    // CampaignProgressData
    // =========================================================================

    /// <summary>Convert CampaignProgressData to Godot Dictionary for GDScript.</summary>
    public static Godot.Collections.Dictionary ToDict(CampaignProgressData progress)
    {
        return new Godot.Collections.Dictionary
        {
            ["completed_battles"] = ToGodotArray(progress.CompletedBattles),
            ["current_battle"] = progress.CurrentBattle ?? "",
            ["gold"] = progress.Gold
        };
    }

    /// <summary>
    /// Convert Godot Dictionary to CampaignProgressData.
    /// Returns null if dict is null (but empty dict returns default data).
    /// </summary>
    public static CampaignProgressData? FromCampaignDict(Godot.Collections.Dictionary? dict)
    {
        if (dict == null) return null;
        if (dict.Count == 0) return new CampaignProgressData();

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
            CurrentBattle = GetNullableString(dict, "current_battle"),
            Gold = GetInt(dict, "gold", 0)
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
            profileData.Resources = new ResourceData
            {
                Gold = GetInt(resourcesDict, "gold", 0),
                Gems = GetInt(resourcesDict, "gems", 0),
                Essence = GetInt(resourcesDict, "essence", 0),
                Fragments = GetInt(resourcesDict, "fragments", 0)
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

    /// <summary>Get bool from dictionary with default value.</summary>
    private static bool GetBool(Godot.Collections.Dictionary dict, string key, bool defaultValue)
    {
        if (!dict.TryGetValue(key, out var value)) return defaultValue;
        if (value.VariantType == Variant.Type.Bool) return value.AsBool();
        return defaultValue;
    }
}
