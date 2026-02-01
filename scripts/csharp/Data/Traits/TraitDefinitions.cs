using System.Collections.Generic;

namespace ProjectSummoner.Data.Traits;

/// <summary>
/// Central registry of all trait definitions as static readonly fields.
/// Provides type-safe trait definitions and lookup methods.
/// Follows the same pattern as UnitDefinitions/CardDefinitions for consistency.
/// </summary>
public static class TraitDefinitions
{
    // =========================================================================
    // INNATE TRAITS - Fire Summoner
    // =========================================================================

    public static readonly TraitDefinition FireAffinity = new()
    {
        Id = TraitIds.FireAffinity,
        NameKey = "trait.fire_affinity.name",
        DescriptionKey = "trait.fire_affinity.description",
        Category = "elemental",
        IsInnate = true,
        Tags = [TraitTags.Summoner, TraitTags.Fire],
        Modifiers =
        [
            new TraitModifier { Stat = "fire_damage_bonus", Type = "percent", Value = 10.0f },
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.FireAffinity,
                Conditions = new() { ["elemental_affinity"] = "fire" },
                StatMults = new() { ["attack_damage"] = 1.10f }
            }
        ]
    };

    public static readonly TraitDefinition BurningSpirit = new()
    {
        Id = TraitIds.BurningSpirit,
        NameKey = "trait.burning_spirit.name",
        DescriptionKey = "trait.burning_spirit.description",
        Category = "combat",
        IsInnate = true,
        Tags = [TraitTags.Summoner, TraitTags.Fire],
        Modifiers =
        [
            new TraitModifier { Stat = "fire_damage_bonus", Type = "percent", Value = 5.0f }
        ]
    };

    // =========================================================================
    // INNATE TRAITS - Water Summoner
    // =========================================================================

    public static readonly TraitDefinition WaterAffinity = new()
    {
        Id = TraitIds.WaterAffinity,
        NameKey = "trait.water_affinity.name",
        DescriptionKey = "trait.water_affinity.description",
        Category = "elemental",
        IsInnate = true,
        Tags = [TraitTags.Summoner, TraitTags.Water],
        Modifiers =
        [
            new TraitModifier { Stat = "water_damage_bonus", Type = "percent", Value = 10.0f },
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.WaterAffinity,
                Conditions = new() { ["elemental_affinity"] = "water" },
                StatMults = new() { ["attack_damage"] = 1.10f }
            }
        ]
    };

    public static readonly TraitDefinition TidalResilience = new()
    {
        Id = TraitIds.TidalResilience,
        NameKey = "trait.tidal_resilience.name",
        DescriptionKey = "trait.tidal_resilience.description",
        Category = "defense",
        IsInnate = true,
        Tags = [TraitTags.Summoner, TraitTags.Water],
        Modifiers =
        [
            new TraitModifier { Stat = "max_health", Type = "percent", Value = 10.0f }
        ]
    };

    // =========================================================================
    // INNATE TRAITS - Wind Summoner
    // =========================================================================

    public static readonly TraitDefinition WindAffinity = new()
    {
        Id = TraitIds.WindAffinity,
        NameKey = "trait.wind_affinity.name",
        DescriptionKey = "trait.wind_affinity.description",
        Category = "elemental",
        IsInnate = true,
        Tags = [TraitTags.Summoner, TraitTags.Wind],
        Modifiers =
        [
            new TraitModifier { Stat = "wind_damage_bonus", Type = "percent", Value = 10.0f },
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.WindAffinity,
                Conditions = new() { ["elemental_affinity"] = "wind" },
                StatMults = new() { ["attack_damage"] = 1.10f }
            }
        ]
    };

    public static readonly TraitDefinition SwiftCasting = new()
    {
        Id = TraitIds.SwiftCasting,
        NameKey = "trait.swift_casting.name",
        DescriptionKey = "trait.swift_casting.description",
        Category = "utility",
        IsInnate = true,
        Tags = [TraitTags.Summoner, TraitTags.Wind],
        Modifiers =
        [
            new TraitModifier { Stat = "cast_speed", Type = "percent", Value = 10.0f }
        ]
    };

    // =========================================================================
    // INNATE TRAITS - Earth Summoner
    // =========================================================================

    public static readonly TraitDefinition EarthAffinity = new()
    {
        Id = TraitIds.EarthAffinity,
        NameKey = "trait.earth_affinity.name",
        DescriptionKey = "trait.earth_affinity.description",
        Category = "elemental",
        IsInnate = true,
        Tags = [TraitTags.Summoner, TraitTags.Earth],
        Modifiers =
        [
            new TraitModifier { Stat = "earth_damage_bonus", Type = "percent", Value = 10.0f },
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.EarthAffinity,
                Conditions = new() { ["elemental_affinity"] = "earth" },
                StatMults = new() { ["attack_damage"] = 1.10f }
            }
        ]
    };

    public static readonly TraitDefinition StoneFortitude = new()
    {
        Id = TraitIds.StoneFortitude,
        NameKey = "trait.stone_fortitude.name",
        DescriptionKey = "trait.stone_fortitude.description",
        Category = "defense",
        IsInnate = true,
        Tags = [TraitTags.Summoner, TraitTags.Earth],
        Modifiers =
        [
            new TraitModifier { Stat = "damage_reduction", Type = "flat", Value = 5.0f }
        ]
    };

    // =========================================================================
    // INNATE TRAITS - Lightning Summoner
    // =========================================================================

    public static readonly TraitDefinition LightningAffinity = new()
    {
        Id = TraitIds.LightningAffinity,
        NameKey = "trait.lightning_affinity.name",
        DescriptionKey = "trait.lightning_affinity.description",
        Category = "elemental",
        IsInnate = true,
        Tags = [TraitTags.Summoner, TraitTags.Lightning],
        Modifiers =
        [
            new TraitModifier { Stat = "lightning_damage_bonus", Type = "percent", Value = 15.0f },
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.LightningAffinity,
                Conditions = new() { ["elemental_affinity"] = "lightning" },
                StatMults = new() { ["attack_damage"] = 1.15f }
            }
        ]
    };

    // =========================================================================
    // INNATE TRAITS - Life Summoner
    // =========================================================================

    public static readonly TraitDefinition LifeAffinity = new()
    {
        Id = TraitIds.LifeAffinity,
        NameKey = "trait.life_affinity.name",
        DescriptionKey = "trait.life_affinity.description",
        Category = "elemental",
        IsInnate = true,
        Tags = [TraitTags.Summoner, TraitTags.Life],
        Modifiers =
        [
            new TraitModifier { Stat = "healing_bonus", Type = "percent", Value = 15.0f },
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.LifeAffinity,
                Conditions = new() { ["elemental_affinity"] = "life" },
                StatMults = new() { ["max_health"] = 1.10f }
            }
        ]
    };

    // =========================================================================
    // INNATE TRAITS - Death Summoner
    // =========================================================================

    public static readonly TraitDefinition DeathAffinity = new()
    {
        Id = TraitIds.DeathAffinity,
        NameKey = "trait.death_affinity.name",
        DescriptionKey = "trait.death_affinity.description",
        Category = "elemental",
        IsInnate = true,
        Tags = [TraitTags.Summoner, TraitTags.Death],
        Modifiers =
        [
            new TraitModifier { Stat = "death_damage_bonus", Type = "percent", Value = 10.0f },
            new TraitModifier { Stat = "lifesteal", Type = "percent", Value = 5.0f }
        ]
    };

    // =========================================================================
    // ACQUIRABLE TRAITS - Global Summoner Pool
    // =========================================================================

    public static readonly TraitDefinition IronWill = new()
    {
        Id = TraitIds.IronWill,
        NameKey = "trait.iron_will.name",
        DescriptionKey = "trait.iron_will.description",
        Category = "defense",
        IsInnate = false,
        Tags = [TraitTags.Summoner, TraitTags.Global],
        MinLevel = 2,
        Modifiers =
        [
            new TraitModifier { Stat = "damage_reduction", Type = "flat", Value = 5.0f }
        ]
    };

    public static readonly TraitDefinition QuickRecovery = new()
    {
        Id = TraitIds.QuickRecovery,
        NameKey = "trait.quick_recovery.name",
        DescriptionKey = "trait.quick_recovery.description",
        Category = "utility",
        IsInnate = false,
        Tags = [TraitTags.Summoner, TraitTags.Global],
        MinLevel = 2,
        Modifiers =
        [
            new TraitModifier { Stat = "mana_regen", Type = "percent", Value = 10.0f }
        ]
    };

    public static readonly TraitDefinition VitalityBoost = new()
    {
        Id = TraitIds.VitalityBoost,
        NameKey = "trait.vitality_boost.name",
        DescriptionKey = "trait.vitality_boost.description",
        Category = "defense",
        IsInnate = false,
        Tags = [TraitTags.Summoner, TraitTags.Global],
        MinLevel = 2,
        Modifiers =
        [
            new TraitModifier { Stat = "max_health", Type = "flat", Value = 100.0f }
        ]
    };

    public static readonly TraitDefinition SwiftStrike = new()
    {
        Id = TraitIds.SwiftStrike,
        NameKey = "trait.swift_strike.name",
        DescriptionKey = "trait.swift_strike.description",
        Category = "combat",
        IsInnate = false,
        Tags = [TraitTags.Summoner, TraitTags.Global],
        MinLevel = 3,
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.SwiftStrike,
                StatMults = new() { ["attack_speed"] = 1.10f }
            }
        ]
    };

    // =========================================================================
    // ACQUIRABLE TRAITS - Triggered (conditional effects for summoners)
    // =========================================================================

    public static readonly TraitDefinition Berserker = new()
    {
        Id = TraitIds.Berserker,
        NameKey = "trait.berserker.name",
        DescriptionKey = "trait.berserker.description",
        Category = "combat",
        IsInnate = false,
        Tags = [TraitTags.Summoner, TraitTags.Global],
        MinLevel = 3,
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.Berserker,
                StatMults = new() { ["attack_damage"] = 1.20f },
                Trigger = "BelowHpPercent",
                TriggerThreshold = 0.5f
            }
        ]
    };

    public static readonly TraitDefinition Vengeful = new()
    {
        Id = TraitIds.Vengeful,
        NameKey = "trait.vengeful.name",
        DescriptionKey = "trait.vengeful.description",
        Category = "combat",
        IsInnate = false,
        Tags = [TraitTags.Summoner, TraitTags.Global],
        MinLevel = 4,
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.Vengeful,
                StatMults = new() { ["attack_speed"] = 1.10f },
                Trigger = "OnTakeHit",
                TriggerDuration = 5.0f,
                TriggerCooldown = 1.0f
            }
        ]
    };

    public static readonly TraitDefinition SoulHarvest = new()
    {
        Id = TraitIds.SoulHarvest,
        NameKey = "trait.soul_harvest.name",
        DescriptionKey = "trait.soul_harvest.description",
        Category = "combat",
        IsInnate = false,
        Tags = [TraitTags.Summoner, TraitTags.Global],
        MinLevel = 4,
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.SoulHarvest,
                StatAdds = new() { ["heal_on_kill"] = 5.0f },
                Trigger = "OnKill"
            }
        ]
    };

    // =========================================================================
    // ACQUIRABLE TRAITS - Element-Exclusive Summoner Traits
    // =========================================================================

    public static readonly TraitDefinition InfernoMastery = new()
    {
        Id = TraitIds.InfernoMastery,
        NameKey = "trait.inferno_mastery.name",
        DescriptionKey = "trait.inferno_mastery.description",
        Category = "elemental",
        IsInnate = false,
        Tags = [TraitTags.Summoner, TraitTags.Fire],
        MinLevel = 5,
        Prerequisites = [TraitIds.FireAffinity],
        Modifiers =
        [
            new TraitModifier { Stat = "fire_damage_bonus", Type = "percent", Value = 15.0f },
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.InfernoMastery,
                Conditions = new() { ["elemental_affinity"] = "fire" },
                StatMults = new() { ["attack_damage"] = 1.15f }
            }
        ]
    };

    public static readonly TraitDefinition TidalMastery = new()
    {
        Id = TraitIds.TidalMastery,
        NameKey = "trait.tidal_mastery.name",
        DescriptionKey = "trait.tidal_mastery.description",
        Category = "elemental",
        IsInnate = false,
        Tags = [TraitTags.Summoner, TraitTags.Water],
        MinLevel = 5,
        Prerequisites = [TraitIds.WaterAffinity],
        Modifiers =
        [
            new TraitModifier { Stat = "water_damage_bonus", Type = "percent", Value = 15.0f },
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.TidalMastery,
                Conditions = new() { ["elemental_affinity"] = "water" },
                StatMults = new() { ["max_health"] = 1.15f }
            }
        ]
    };

    // =========================================================================
    // SUMMON TRAITS - Global Pool (available to all summons)
    // =========================================================================

    public static readonly TraitDefinition Fortitude = new()
    {
        Id = TraitIds.Fortitude,
        NameKey = "trait.fortitude.name",
        DescriptionKey = "trait.fortitude.description",
        Category = "defense",
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 2,
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.Fortitude,
                StatMults = new() { ["max_hp"] = 1.08f }
            }
        ]
    };

    public static readonly TraitDefinition Power = new()
    {
        Id = TraitIds.Power,
        NameKey = "trait.power.name",
        DescriptionKey = "trait.power.description",
        Category = "combat",
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 2,
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.Power,
                StatMults = new() { ["attack_damage"] = 1.06f }
            }
        ]
    };

    public static readonly TraitDefinition Swiftness = new()
    {
        Id = TraitIds.Swiftness,
        NameKey = "trait.swiftness.name",
        DescriptionKey = "trait.swiftness.description",
        Category = "combat",
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 2,
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.Swiftness,
                StatMults = new() { ["attack_speed"] = 1.05f }
            }
        ]
    };

    public static readonly TraitDefinition Agility = new()
    {
        Id = TraitIds.Agility,
        NameKey = "trait.agility.name",
        DescriptionKey = "trait.agility.description",
        Category = "utility",
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 2,
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.Agility,
                StatMults = new() { ["move_speed"] = 1.05f }
            }
        ]
    };

    // =========================================================================
    // LOOKUP
    // =========================================================================

    private static readonly Dictionary<string, TraitDefinition> _lookup = new()
    {
        // Innate - Fire
        [TraitIds.FireAffinity] = FireAffinity,
        [TraitIds.BurningSpirit] = BurningSpirit,

        // Innate - Water
        [TraitIds.WaterAffinity] = WaterAffinity,
        [TraitIds.TidalResilience] = TidalResilience,

        // Innate - Wind
        [TraitIds.WindAffinity] = WindAffinity,
        [TraitIds.SwiftCasting] = SwiftCasting,

        // Innate - Earth
        [TraitIds.EarthAffinity] = EarthAffinity,
        [TraitIds.StoneFortitude] = StoneFortitude,

        // Innate - Lightning
        [TraitIds.LightningAffinity] = LightningAffinity,

        // Innate - Life
        [TraitIds.LifeAffinity] = LifeAffinity,

        // Innate - Death
        [TraitIds.DeathAffinity] = DeathAffinity,

        // Acquirable - Global
        [TraitIds.IronWill] = IronWill,
        [TraitIds.QuickRecovery] = QuickRecovery,
        [TraitIds.VitalityBoost] = VitalityBoost,
        [TraitIds.SwiftStrike] = SwiftStrike,

        // Acquirable - Triggered
        [TraitIds.Berserker] = Berserker,
        [TraitIds.Vengeful] = Vengeful,
        [TraitIds.SoulHarvest] = SoulHarvest,

        // Acquirable - Element Mastery
        [TraitIds.InfernoMastery] = InfernoMastery,
        [TraitIds.TidalMastery] = TidalMastery,

        // Summon Traits
        [TraitIds.Fortitude] = Fortitude,
        [TraitIds.Power] = Power,
        [TraitIds.Swiftness] = Swiftness,
        [TraitIds.Agility] = Agility
    };

    /// <summary>Get a trait definition by ID. Returns null if not found.</summary>
    public static TraitDefinition? Get(TraitId id) => _lookup.GetValueOrDefault(id);

    /// <summary>Get a trait definition by string ID. Returns null if not found.</summary>
    public static TraitDefinition? Get(string id) => _lookup.GetValueOrDefault(id);

    /// <summary>Try to get a trait definition by ID.</summary>
    public static bool TryGet(TraitId id, out TraitDefinition? definition)
    {
        return _lookup.TryGetValue(id, out definition);
    }

    /// <summary>Try to get a trait definition by string ID.</summary>
    public static bool TryGet(string id, out TraitDefinition? definition)
    {
        return _lookup.TryGetValue(id, out definition);
    }

    /// <summary>Check if a trait exists.</summary>
    public static bool Has(TraitId id) => _lookup.ContainsKey(id);

    /// <summary>Check if a trait exists by string ID.</summary>
    public static bool Has(string id) => _lookup.ContainsKey(id);

    /// <summary>Get all trait definitions.</summary>
    public static IReadOnlyCollection<TraitDefinition> All => _lookup.Values;

    /// <summary>Get all trait IDs.</summary>
    public static IReadOnlyCollection<string> AllIds => _lookup.Keys;

    /// <summary>Get trait count.</summary>
    public static int Count => _lookup.Count;
}
