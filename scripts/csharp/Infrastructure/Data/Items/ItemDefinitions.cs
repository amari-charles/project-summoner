using System.Collections.Generic;
using Fateforged.Data.Traits;
using Fateforged.Stats;

namespace Fateforged.Data.Items;

/// <summary>
/// Central registry of all item definitions.
/// Use ItemDefinitions.Get() to retrieve items by ID.
/// </summary>
public static class ItemDefinitions
{
    // =========================================================================
    // RARE EQUIPMENT
    // =========================================================================

    public static readonly ItemDefinition VeteransMedal = new()
    {
        Id = ItemIds.VeteransMedal,
        NameKey = "item.veterans_medal.name",
        DescriptionKey = "item.veterans_medal.description",
        Slot = ItemSlot.Robes,
        Binding = ItemBinding.AccountWide,
        IsEventExclusive = true,
        Rarity = "rare",
        Modifiers =
        [
            new TraitModifier
            {
                Stat = StatKey.MaxHealth,
                Type = ModifierType.Flat,
                Value = 100.0f,
            },
        ],
    };

    public static readonly ItemDefinition BattleHardenedBadge = new()
    {
        Id = ItemIds.BattleHardenedBadge,
        NameKey = "item.battle_hardened_badge.name",
        DescriptionKey = "item.battle_hardened_badge.description",
        Slot = ItemSlot.Wand,
        Binding = ItemBinding.SummonerBound,
        Rarity = "rare",
        Modifiers =
        [
            new TraitModifier
            {
                Stat = StatKey.DamageBonus,
                Type = ModifierType.Percent,
                Value = 5.0f,
            },
        ],
    };

    public static readonly ItemDefinition FortunesCharm = new()
    {
        Id = ItemIds.FortunesCharm,
        NameKey = "item.fortunes_charm.name",
        DescriptionKey = "item.fortunes_charm.description",
        Slot = ItemSlot.Ring1,
        Binding = ItemBinding.SummonerBound,
        Rarity = "uncommon",
        Modifiers =
        [
            new TraitModifier
            {
                Stat = StatKey.GoldBonus,
                Type = ModifierType.Percent,
                Value = 10.0f,
            },
        ],
    };

    public static readonly ItemDefinition BoldFortuneAmulet = new()
    {
        Id = ItemIds.BoldFortuneAmulet,
        NameKey = "item.bold_fortune_amulet.name",
        DescriptionKey = "item.bold_fortune_amulet.description",
        Slot = ItemSlot.Robes,
        Binding = ItemBinding.SummonerBound,
        Rarity = "uncommon",
        Modifiers =
        [
            new TraitModifier
            {
                Stat = StatKey.MaxHealth,
                Type = ModifierType.Flat,
                Value = 50.0f,
            },
        ],
    };

    // =========================================================================
    // STARTER ITEMS
    // Basic items available from the start, one for each slot.
    // Slots: Wand, Ring1, Ring2, Robes
    // =========================================================================

    public static readonly ItemDefinition TrainingBlade = new()
    {
        Id = ItemIds.TrainingBlade,
        NameKey = "item.training_blade.name",
        DescriptionKey = "item.training_blade.description",
        Slot = ItemSlot.Wand,
        Binding = ItemBinding.SummonerBound,
        Rarity = "common",
        Modifiers =
        [
            new TraitModifier
            {
                Stat = StatKey.DamageBonus,
                Type = ModifierType.Percent,
                Value = 2.0f,
            },
        ],
    };

    public static readonly ItemDefinition SimpleRing = new()
    {
        Id = ItemIds.SimpleRing,
        NameKey = "item.simple_ring.name",
        DescriptionKey = "item.simple_ring.description",
        Slot = ItemSlot.Ring1,
        Binding = ItemBinding.SummonerBound,
        Rarity = "common",
        Modifiers =
        [
            new TraitModifier
            {
                Stat = StatKey.GoldBonus,
                Type = ModifierType.Percent,
                Value = 5.0f,
            },
        ],
    };

    public static readonly ItemDefinition LuckyBand = new()
    {
        Id = ItemIds.LuckyBand,
        NameKey = "item.lucky_band.name",
        DescriptionKey = "item.lucky_band.description",
        Slot = ItemSlot.Ring2,
        Binding = ItemBinding.SummonerBound,
        Rarity = "common",
        Modifiers =
        [
            new TraitModifier
            {
                Stat = StatKey.XpBonus,
                Type = ModifierType.Percent,
                Value = 5.0f,
            },
        ],
    };

    public static readonly ItemDefinition TravelersCloak = new()
    {
        Id = ItemIds.TravelersCloak,
        NameKey = "item.travelers_cloak.name",
        DescriptionKey = "item.travelers_cloak.description",
        Slot = ItemSlot.Robes,
        Binding = ItemBinding.SummonerBound,
        Rarity = "common",
        Modifiers =
        [
            new TraitModifier
            {
                Stat = StatKey.MaxHealth,
                Type = ModifierType.Flat,
                Value = 25.0f,
            },
        ],
    };

    // =========================================================================
    // LOOKUP
    // =========================================================================

    private static readonly Dictionary<string, ItemDefinition> _lookup = new()
    {
        [ItemIds.VeteransMedal] = VeteransMedal,
        [ItemIds.BattleHardenedBadge] = BattleHardenedBadge,
        [ItemIds.FortunesCharm] = FortunesCharm,
        [ItemIds.BoldFortuneAmulet] = BoldFortuneAmulet,
        [ItemIds.TrainingBlade] = TrainingBlade,
        [ItemIds.SimpleRing] = SimpleRing,
        [ItemIds.LuckyBand] = LuckyBand,
        [ItemIds.TravelersCloak] = TravelersCloak,
    };

    /// <summary>Get an item definition by ID. Returns null if not found.</summary>
    public static ItemDefinition? Get(ItemId id) => _lookup.GetValueOrDefault(id);

    /// <summary>Get an item definition by string ID. Returns null if not found.</summary>
    public static ItemDefinition? Get(string id) => _lookup.GetValueOrDefault(id);

    /// <summary>Check if an item exists.</summary>
    public static bool Has(ItemId id) => _lookup.ContainsKey(id);

    /// <summary>Check if an item exists by string ID.</summary>
    public static bool Has(string id) => _lookup.ContainsKey(id);

    /// <summary>Get all item definitions.</summary>
    public static IEnumerable<ItemDefinition> All => _lookup.Values;

    /// <summary>Get item count.</summary>
    public static int Count => _lookup.Count;
}
