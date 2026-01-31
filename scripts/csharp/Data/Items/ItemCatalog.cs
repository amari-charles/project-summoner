using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectSummoner.Data.Traits;

namespace ProjectSummoner.Data.Items;

/// <summary>
/// Item ID constants for type-safe references.
/// </summary>
public static class ItemId
{
    // Rare equipment
    public const string VeteransMedal = "item_veterans_medal";
    public const string BattleHardenedBadge = "item_battle_hardened_badge";
    public const string FortunesCharm = "item_fortunes_charm";
    public const string BoldFortuneAmulet = "item_bold_fortune_amulet";

    // Starter items (one per slot: Weapon, Ring1, Ring2, Vestments)
    public const string TrainingBlade = "item_training_blade";
    public const string SimpleRing = "item_simple_ring";
    public const string LuckyBand = "item_lucky_band";
    public const string TravelersCloak = "item_travelers_cloak";
}

/// <summary>
/// Central registry of all item definitions.
/// Provides type-safe item lookup and query methods.
/// </summary>
public static class ItemCatalog
{
    // =========================================================================
    // ITEM DEFINITIONS
    // =========================================================================

    private static readonly Dictionary<string, ItemDefinition> _items = new()
    {
        // =====================================================================
        // RARE EQUIPMENT
        // =====================================================================

        [ItemId.VeteransMedal] = new ItemDefinition
        {
            Id = ItemId.VeteransMedal,
            NameKey = "item.veterans_medal.name",
            DescriptionKey = "item.veterans_medal.description",
            Slot = ItemSlot.Vestments,
            Binding = ItemBinding.AccountWide,
            Rarity = "rare",
            Modifiers =
            [
                new TraitModifier { Stat = "max_health", Type = "flat", Value = 100.0f }
            ]
        },

        [ItemId.BattleHardenedBadge] = new ItemDefinition
        {
            Id = ItemId.BattleHardenedBadge,
            NameKey = "item.battle_hardened_badge.name",
            DescriptionKey = "item.battle_hardened_badge.description",
            Slot = ItemSlot.Weapon,
            Binding = ItemBinding.AccountWide,
            Rarity = "rare",
            Modifiers =
            [
                new TraitModifier { Stat = "damage_bonus", Type = "percent", Value = 5.0f }
            ]
        },

        [ItemId.FortunesCharm] = new ItemDefinition
        {
            Id = ItemId.FortunesCharm,
            NameKey = "item.fortunes_charm.name",
            DescriptionKey = "item.fortunes_charm.description",
            Slot = ItemSlot.Ring1,
            Binding = ItemBinding.AccountWide,
            Rarity = "uncommon",
            Modifiers =
            [
                new TraitModifier { Stat = "gold_bonus", Type = "percent", Value = 10.0f }
            ]
        },

        [ItemId.BoldFortuneAmulet] = new ItemDefinition
        {
            Id = ItemId.BoldFortuneAmulet,
            NameKey = "item.bold_fortune_amulet.name",
            DescriptionKey = "item.bold_fortune_amulet.description",
            Slot = ItemSlot.Vestments,
            Binding = ItemBinding.AccountWide,
            Rarity = "uncommon",
            Modifiers =
            [
                new TraitModifier { Stat = "max_health", Type = "flat", Value = 50.0f }
            ]
        },

        // =====================================================================
        // STARTER ITEMS
        // Basic items available from the start, one for each slot.
        // Slots: Weapon, Ring1, Ring2, Vestments
        // =====================================================================

        [ItemId.TrainingBlade] = new ItemDefinition
        {
            Id = ItemId.TrainingBlade,
            NameKey = "item.training_blade.name",
            DescriptionKey = "item.training_blade.description",
            Slot = ItemSlot.Weapon,
            Binding = ItemBinding.AccountWide,
            Rarity = "common",
            Modifiers =
            [
                new TraitModifier { Stat = "damage_bonus", Type = "percent", Value = 2.0f }
            ]
        },

        [ItemId.SimpleRing] = new ItemDefinition
        {
            Id = ItemId.SimpleRing,
            NameKey = "item.simple_ring.name",
            DescriptionKey = "item.simple_ring.description",
            Slot = ItemSlot.Ring1,
            Binding = ItemBinding.AccountWide,
            Rarity = "common",
            Modifiers =
            [
                new TraitModifier { Stat = "gold_bonus", Type = "percent", Value = 5.0f }
            ]
        },

        [ItemId.LuckyBand] = new ItemDefinition
        {
            Id = ItemId.LuckyBand,
            NameKey = "item.lucky_band.name",
            DescriptionKey = "item.lucky_band.description",
            Slot = ItemSlot.Ring2,
            Binding = ItemBinding.AccountWide,
            Rarity = "common",
            Modifiers =
            [
                new TraitModifier { Stat = "xp_bonus", Type = "percent", Value = 5.0f }
            ]
        },

        [ItemId.TravelersCloak] = new ItemDefinition
        {
            Id = ItemId.TravelersCloak,
            NameKey = "item.travelers_cloak.name",
            DescriptionKey = "item.travelers_cloak.description",
            Slot = ItemSlot.Vestments,
            Binding = ItemBinding.AccountWide,
            Rarity = "common",
            Modifiers =
            [
                new TraitModifier { Stat = "max_health", Type = "flat", Value = 25.0f }
            ]
        }
    };

    // =========================================================================
    // LOOKUP METHODS
    // =========================================================================

    /// <summary>Get an item definition by ID. Returns null if not found.</summary>
    public static ItemDefinition? GetItem(string id)
    {
        return _items.GetValueOrDefault(id);
    }

    /// <summary>Check if an item exists in the catalog.</summary>
    public static bool HasItem(string id)
    {
        return _items.ContainsKey(id);
    }

    /// <summary>Get all item IDs.</summary>
    public static string[] GetAllItemIds()
    {
        return [.. _items.Keys];
    }

    /// <summary>Get all item definitions.</summary>
    public static ItemDefinition[] GetAllItems()
    {
        return [.. _items.Values];
    }

    /// <summary>Get item count.</summary>
    public static int Count => _items.Count;

    // =========================================================================
    // QUERY METHODS
    // =========================================================================

    /// <summary>Get items by slot.</summary>
    public static ItemDefinition[] GetItemsBySlot(ItemSlot slot)
    {
        return _items.Values.Where(i => i.Slot == slot).ToArray();
    }

    /// <summary>Get items by rarity.</summary>
    public static ItemDefinition[] GetItemsByRarity(string rarity)
    {
        return _items.Values.Where(i => i.Rarity == rarity).ToArray();
    }

    /// <summary>Get items by binding type.</summary>
    public static ItemDefinition[] GetItemsByBinding(ItemBinding binding)
    {
        return _items.Values.Where(i => i.Binding == binding).ToArray();
    }

    // =========================================================================
    // GODOT DICTIONARY CONVERSION (for GDScript interop)
    // =========================================================================

    /// <summary>Convert an ItemDefinition to a Godot Dictionary for GDScript consumption.</summary>
    public static Godot.Collections.Dictionary ToDictionary(ItemDefinition item)
    {
        var modifiersArray = new Godot.Collections.Array();
        foreach (var mod in item.Modifiers)
        {
            var modDict = new Godot.Collections.Dictionary();
            if (!string.IsNullOrEmpty(mod.Stat))
            {
                modDict["stat"] = mod.Stat;
                modDict["type"] = mod.Type;
                modDict["value"] = mod.Value;
            }
            modifiersArray.Add(modDict);
        }

        var dict = new Godot.Collections.Dictionary
        {
            ["id"] = item.Id,
            ["name_key"] = item.NameKey,
            ["description_key"] = item.DescriptionKey,
            ["slot"] = item.Slot.ToString().ToLowerInvariant(),
            ["binding"] = item.Binding.ToString(),
            ["rarity"] = item.Rarity,
            ["modifiers"] = modifiersArray
        };

        if (!string.IsNullOrEmpty(item.IconPath))
            dict["icon_path"] = item.IconPath;

        return dict;
    }

    /// <summary>Get item as dictionary for GDScript. Returns empty dict if not found.</summary>
    public static Godot.Collections.Dictionary GetItemAsDict(string id)
    {
        var item = GetItem(id);
        return item != null ? ToDictionary(item) : new Godot.Collections.Dictionary();
    }

    /// <summary>Get all items as dictionaries for GDScript.</summary>
    public static Godot.Collections.Array<Godot.Collections.Dictionary> GetAllItemsAsDict()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var item in _items.Values)
        {
            result.Add(ToDictionary(item));
        }
        return result;
    }

    /// <summary>Get items by slot as dictionaries for GDScript.</summary>
    public static Godot.Collections.Array<Godot.Collections.Dictionary> GetItemsBySlotAsDict(string slotName)
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        if (System.Enum.TryParse<ItemSlot>(slotName, ignoreCase: true, out var slot))
        {
            foreach (var item in GetItemsBySlot(slot))
            {
                result.Add(ToDictionary(item));
            }
        }
        return result;
    }
}
