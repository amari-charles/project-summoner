using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectSummoner.Systems.Modifiers;

namespace ProjectSummoner.Data.Traits;

/// <summary>
/// Central registry of all trait definitions.
/// Provides type-safe trait lookup and query methods.
/// GDScript can call this via the TraitCatalogBridge autoload.
/// </summary>
public static class TraitCatalog
{
    // =========================================================================
    // TRAIT DEFINITIONS
    // =========================================================================

    private static readonly Dictionary<string, TraitDefinition> _traits = new()
    {
        // =====================================================================
        // INNATE TRAITS - Fire Summoner
        // =====================================================================

        [TraitId.FireAffinity] = new TraitDefinition
        {
            Id = TraitId.FireAffinity,
            NameKey = "trait.fire_affinity.name",
            DescriptionKey = "trait.fire_affinity.description",
            Category = "elemental",
            IsInnate = true,
            Modifiers =
            [
                // Summoner stat modifier
                new TraitModifier { Stat = "fire_damage_bonus", Type = "percent", Value = 10.0f },
                // Unit modifier - buffs all fire units
                new TraitModifier
                {
                    Target = "unit",
                    Source = TraitId.FireAffinity,
                    Conditions = new() { ["elemental_affinity"] = "fire" },
                    StatMults = new() { ["attack_damage"] = 1.10f }
                }
            ]
        },

        [TraitId.BurningSpirit] = new TraitDefinition
        {
            Id = TraitId.BurningSpirit,
            NameKey = "trait.burning_spirit.name",
            DescriptionKey = "trait.burning_spirit.description",
            Category = "combat",
            IsInnate = true,
            Modifiers =
            [
                new TraitModifier { Stat = "fire_damage_bonus", Type = "percent", Value = 5.0f }
            ]
        },

        // =====================================================================
        // INNATE TRAITS - Water Summoner
        // =====================================================================

        [TraitId.WaterAffinity] = new TraitDefinition
        {
            Id = TraitId.WaterAffinity,
            NameKey = "trait.water_affinity.name",
            DescriptionKey = "trait.water_affinity.description",
            Category = "elemental",
            IsInnate = true,
            Modifiers =
            [
                new TraitModifier { Stat = "water_damage_bonus", Type = "percent", Value = 10.0f },
                new TraitModifier
                {
                    Target = "unit",
                    Source = TraitId.WaterAffinity,
                    Conditions = new() { ["elemental_affinity"] = "water" },
                    StatMults = new() { ["attack_damage"] = 1.10f }
                }
            ]
        },

        [TraitId.TidalResilience] = new TraitDefinition
        {
            Id = TraitId.TidalResilience,
            NameKey = "trait.tidal_resilience.name",
            DescriptionKey = "trait.tidal_resilience.description",
            Category = "defense",
            IsInnate = true,
            Modifiers =
            [
                new TraitModifier { Stat = "max_health", Type = "percent", Value = 10.0f }
            ]
        },

        // =====================================================================
        // INNATE TRAITS - Wind Summoner
        // =====================================================================

        [TraitId.WindAffinity] = new TraitDefinition
        {
            Id = TraitId.WindAffinity,
            NameKey = "trait.wind_affinity.name",
            DescriptionKey = "trait.wind_affinity.description",
            Category = "elemental",
            IsInnate = true,
            Modifiers =
            [
                new TraitModifier { Stat = "wind_damage_bonus", Type = "percent", Value = 10.0f },
                new TraitModifier
                {
                    Target = "unit",
                    Source = TraitId.WindAffinity,
                    Conditions = new() { ["elemental_affinity"] = "wind" },
                    StatMults = new() { ["attack_damage"] = 1.10f }
                }
            ]
        },

        [TraitId.SwiftCasting] = new TraitDefinition
        {
            Id = TraitId.SwiftCasting,
            NameKey = "trait.swift_casting.name",
            DescriptionKey = "trait.swift_casting.description",
            Category = "utility",
            IsInnate = true,
            Modifiers =
            [
                new TraitModifier { Stat = "cast_speed", Type = "percent", Value = 10.0f }
            ]
        },

        // =====================================================================
        // INNATE TRAITS - Earth Summoner
        // =====================================================================

        [TraitId.EarthAffinity] = new TraitDefinition
        {
            Id = TraitId.EarthAffinity,
            NameKey = "trait.earth_affinity.name",
            DescriptionKey = "trait.earth_affinity.description",
            Category = "elemental",
            IsInnate = true,
            Modifiers =
            [
                new TraitModifier { Stat = "earth_damage_bonus", Type = "percent", Value = 10.0f },
                new TraitModifier
                {
                    Target = "unit",
                    Source = TraitId.EarthAffinity,
                    Conditions = new() { ["elemental_affinity"] = "earth" },
                    StatMults = new() { ["attack_damage"] = 1.10f }
                }
            ]
        },

        [TraitId.StoneFortitude] = new TraitDefinition
        {
            Id = TraitId.StoneFortitude,
            NameKey = "trait.stone_fortitude.name",
            DescriptionKey = "trait.stone_fortitude.description",
            Category = "defense",
            IsInnate = true,
            Modifiers =
            [
                new TraitModifier { Stat = "damage_reduction", Type = "flat", Value = 5.0f }
            ]
        },

        // =====================================================================
        // INNATE TRAITS - Lightning Summoner
        // =====================================================================

        [TraitId.LightningAffinity] = new TraitDefinition
        {
            Id = TraitId.LightningAffinity,
            NameKey = "trait.lightning_affinity.name",
            DescriptionKey = "trait.lightning_affinity.description",
            Category = "elemental",
            IsInnate = true,
            Modifiers =
            [
                new TraitModifier { Stat = "lightning_damage_bonus", Type = "percent", Value = 15.0f },
                new TraitModifier
                {
                    Target = "unit",
                    Source = TraitId.LightningAffinity,
                    Conditions = new() { ["elemental_affinity"] = "lightning" },
                    StatMults = new() { ["attack_damage"] = 1.15f }
                }
            ]
        },

        // =====================================================================
        // INNATE TRAITS - Life Summoner
        // =====================================================================

        [TraitId.LifeAffinity] = new TraitDefinition
        {
            Id = TraitId.LifeAffinity,
            NameKey = "trait.life_affinity.name",
            DescriptionKey = "trait.life_affinity.description",
            Category = "elemental",
            IsInnate = true,
            Modifiers =
            [
                new TraitModifier { Stat = "healing_bonus", Type = "percent", Value = 15.0f },
                new TraitModifier
                {
                    Target = "unit",
                    Source = TraitId.LifeAffinity,
                    Conditions = new() { ["elemental_affinity"] = "life" },
                    StatMults = new() { ["max_health"] = 1.10f }
                }
            ]
        },

        // =====================================================================
        // INNATE TRAITS - Death Summoner
        // =====================================================================

        [TraitId.DeathAffinity] = new TraitDefinition
        {
            Id = TraitId.DeathAffinity,
            NameKey = "trait.death_affinity.name",
            DescriptionKey = "trait.death_affinity.description",
            Category = "elemental",
            IsInnate = true,
            Modifiers =
            [
                new TraitModifier { Stat = "death_damage_bonus", Type = "percent", Value = 10.0f },
                new TraitModifier { Stat = "lifesteal", Type = "percent", Value = 5.0f }
            ]
        },

        // =====================================================================
        // ACQUIRED BOONS
        // =====================================================================

        [TraitId.BoonVeteran] = new TraitDefinition
        {
            Id = TraitId.BoonVeteran,
            NameKey = "trait.veteran.name",
            DescriptionKey = "trait.veteran.description",
            Category = "milestone",
            IsInnate = false,
            Modifiers =
            [
                new TraitModifier { Stat = "max_health", Type = "flat", Value = 100.0f }
            ]
        },

        [TraitId.BoonManaWell] = new TraitDefinition
        {
            Id = TraitId.BoonManaWell,
            NameKey = "trait.mana_well.name",
            DescriptionKey = "trait.mana_well.description",
            Category = "milestone",
            IsInnate = false,
            Modifiers =
            [
                new TraitModifier { Stat = "max_mana", Type = "flat", Value = 2.0f }
            ]
        },

        [TraitId.BoonBattleHardened] = new TraitDefinition
        {
            Id = TraitId.BoonBattleHardened,
            NameKey = "trait.battle_hardened.name",
            DescriptionKey = "trait.battle_hardened.description",
            Category = "milestone",
            IsInnate = false,
            Modifiers =
            [
                new TraitModifier { Stat = "damage_bonus", Type = "percent", Value = 5.0f }
            ]
        },

        [TraitId.BoonFortuneFavors] = new TraitDefinition
        {
            Id = TraitId.BoonFortuneFavors,
            NameKey = "trait.fortune_favors.name",
            DescriptionKey = "trait.fortune_favors.description",
            Category = "special",
            IsInnate = false,
            Modifiers =
            [
                new TraitModifier { Stat = "gold_bonus", Type = "percent", Value = 10.0f }
            ]
        },

        [TraitId.FortuneFavorsBold] = new TraitDefinition
        {
            Id = TraitId.FortuneFavorsBold,
            NameKey = "trait.fortune_favors_bold.name",
            DescriptionKey = "trait.fortune_favors_bold.description",
            Category = "special",
            IsInnate = false,
            Modifiers =
            [
                new TraitModifier { Stat = "max_health", Type = "flat", Value = 50.0f }
            ]
        },

        // =====================================================================
        // LEVEL-UP BOONS (selected when leveling up)
        // =====================================================================

        [TraitId.BoonIronWill] = new TraitDefinition
        {
            Id = TraitId.BoonIronWill,
            NameKey = "trait.iron_will.name",
            DescriptionKey = "trait.iron_will.description",
            Category = "defense",
            IsInnate = false,
            Modifiers =
            [
                new TraitModifier { Stat = "damage_reduction", Type = "flat", Value = 5.0f }
            ]
        },

        [TraitId.BoonQuickRecovery] = new TraitDefinition
        {
            Id = TraitId.BoonQuickRecovery,
            NameKey = "trait.quick_recovery.name",
            DescriptionKey = "trait.quick_recovery.description",
            Category = "utility",
            IsInnate = false,
            Modifiers =
            [
                new TraitModifier { Stat = "mana_regen", Type = "percent", Value = 10.0f }
            ]
        },

        [TraitId.BoonVitalityBoost] = new TraitDefinition
        {
            Id = TraitId.BoonVitalityBoost,
            NameKey = "trait.vitality_boost.name",
            DescriptionKey = "trait.vitality_boost.description",
            Category = "defense",
            IsInnate = false,
            Modifiers =
            [
                new TraitModifier { Stat = "max_health", Type = "flat", Value = 100.0f }
            ]
        },

        [TraitId.BoonElementalMastery] = new TraitDefinition
        {
            Id = TraitId.BoonElementalMastery,
            NameKey = "trait.elemental_mastery.name",
            DescriptionKey = "trait.elemental_mastery.description",
            Category = "combat",
            IsInnate = false,
            Modifiers =
            [
                new TraitModifier { Stat = "fire_damage_bonus", Type = "percent", Value = 5.0f },
                new TraitModifier { Stat = "water_damage_bonus", Type = "percent", Value = 5.0f },
                new TraitModifier { Stat = "wind_damage_bonus", Type = "percent", Value = 5.0f },
                new TraitModifier { Stat = "earth_damage_bonus", Type = "percent", Value = 5.0f }
            ]
        },

        [TraitId.BoonSwiftStrike] = new TraitDefinition
        {
            Id = TraitId.BoonSwiftStrike,
            NameKey = "trait.swift_strike.name",
            DescriptionKey = "trait.swift_strike.description",
            Category = "combat",
            IsInnate = false,
            Modifiers =
            [
                new TraitModifier
                {
                    Target = "unit",
                    Source = TraitId.BoonSwiftStrike,
                    StatMults = new() { ["attack_speed"] = 1.10f }
                }
            ]
        },

        [TraitId.BoonTacticalMind] = new TraitDefinition
        {
            Id = TraitId.BoonTacticalMind,
            NameKey = "trait.tactical_mind.name",
            DescriptionKey = "trait.tactical_mind.description",
            Category = "utility",
            IsInnate = false,
            Modifiers =
            [
                new TraitModifier { Stat = "starting_hand_bonus", Type = "flat", Value = 1.0f }
            ]
        }
    };

    // =========================================================================
    // LOOKUP METHODS
    // =========================================================================

    /// <summary>Get a trait definition by ID. Returns null if not found.</summary>
    public static TraitDefinition? GetTrait(string id)
    {
        return _traits.GetValueOrDefault(id);
    }

    /// <summary>Check if a trait exists in the catalog.</summary>
    public static bool HasTrait(string id)
    {
        return _traits.ContainsKey(id);
    }

    /// <summary>Get all trait IDs.</summary>
    public static string[] GetAllTraitIds()
    {
        return [.. _traits.Keys];
    }

    /// <summary>Get all trait definitions.</summary>
    public static TraitDefinition[] GetAllTraits()
    {
        return [.. _traits.Values];
    }

    /// <summary>Get trait count.</summary>
    public static int Count => _traits.Count;

    // =========================================================================
    // QUERY METHODS
    // =========================================================================

    /// <summary>Get traits by category.</summary>
    public static TraitDefinition[] GetTraitsByCategory(string category)
    {
        return _traits.Values.Where(t => t.Category == category).ToArray();
    }

    /// <summary>Get only innate traits.</summary>
    public static TraitDefinition[] GetInnateTraits()
    {
        return _traits.Values.Where(t => t.IsInnate).ToArray();
    }

    /// <summary>Get only acquirable boons (non-innate).</summary>
    public static TraitDefinition[] GetAcquirableBoons()
    {
        return _traits.Values.Where(t => !t.IsInnate).ToArray();
    }

    /// <summary>
    /// Get a pool of traits for level-up selection.
    /// Returns random acquirable traits excluding those already acquired.
    /// </summary>
    /// <param name="excludedIds">Trait IDs to exclude (already acquired)</param>
    /// <param name="count">Number of traits to return (default 3)</param>
    public static TraitDefinition[] GetLevelUpTraitPool(List<string> excludedIds, int count = 3)
    {
        return GetAcquirableBoons()
            .Where(t => !excludedIds.Contains(t.Id))
            .OrderBy(_ => System.Random.Shared.Next())
            .Take(count)
            .ToArray();
    }

    // =========================================================================
    // UNIT MODIFIERS (for SummonerModifierProvider)
    // =========================================================================

    /// <summary>
    /// Get unit modifiers for a trait.
    /// Returns modifiers where Target = "unit" - these affect spawned units.
    /// </summary>
    public static List<StatModifier> GetUnitModifiersForTrait(string traitId)
    {
        var trait = GetTrait(traitId);
        if (trait == null) return [];

        var result = new List<StatModifier>();
        foreach (var mod in trait.Modifiers.Where(m => m.IsUnitModifier))
        {
            result.Add(new StatModifier
            {
                Source = mod.Source ?? traitId,
                Conditions = mod.Conditions ?? [],
                StatMults = mod.StatMults ?? [],
                StatAdds = mod.StatAdds ?? []
            });
        }
        return result;
    }

    // =========================================================================
    // GODOT DICTIONARY CONVERSION (for GDScript interop)
    // =========================================================================

    /// <summary>Convert a TraitDefinition to a Godot Dictionary for GDScript consumption.</summary>
    public static Godot.Collections.Dictionary ToDictionary(TraitDefinition trait)
    {
        var modifiersArray = new Godot.Collections.Array();
        foreach (var mod in trait.Modifiers)
        {
            var modDict = new Godot.Collections.Dictionary();

            // Summoner stat modifier properties
            if (!string.IsNullOrEmpty(mod.Stat))
            {
                modDict["stat"] = mod.Stat;
                modDict["type"] = mod.Type;
                modDict["value"] = mod.Value;
            }

            // Unit modifier properties
            if (!string.IsNullOrEmpty(mod.Target))
            {
                modDict["target"] = mod.Target;
            }
            if (!string.IsNullOrEmpty(mod.Source))
            {
                modDict["source"] = mod.Source;
            }
            if (mod.Conditions != null && mod.Conditions.Count > 0)
            {
                var conditionsDict = new Godot.Collections.Dictionary();
                foreach (var kvp in mod.Conditions)
                {
                    // Convert object to appropriate Variant type
                    conditionsDict[kvp.Key] = kvp.Value switch
                    {
                        string s => s,
                        int i => i,
                        float f => f,
                        double d => (float)d,
                        bool b => b,
                        _ => kvp.Value?.ToString() ?? ""
                    };
                }
                modDict["conditions"] = conditionsDict;
            }
            if (mod.StatMults != null && mod.StatMults.Count > 0)
            {
                var statMultsDict = new Godot.Collections.Dictionary();
                foreach (var kvp in mod.StatMults)
                {
                    statMultsDict[kvp.Key] = kvp.Value;
                }
                modDict["stat_mults"] = statMultsDict;
            }
            if (mod.StatAdds != null && mod.StatAdds.Count > 0)
            {
                var statAddsDict = new Godot.Collections.Dictionary();
                foreach (var kvp in mod.StatAdds)
                {
                    statAddsDict[kvp.Key] = kvp.Value;
                }
                modDict["stat_adds"] = statAddsDict;
            }

            modifiersArray.Add(modDict);
        }

        return new Godot.Collections.Dictionary
        {
            ["id"] = trait.Id,
            ["name_key"] = trait.NameKey,
            ["description_key"] = trait.DescriptionKey,
            ["category"] = trait.Category,
            ["is_innate"] = trait.IsInnate,
            ["modifiers"] = modifiersArray
        };
    }

    /// <summary>Get trait as dictionary for GDScript. Returns empty dict if not found.</summary>
    public static Godot.Collections.Dictionary GetTraitAsDict(string id)
    {
        var trait = GetTrait(id);
        return trait != null ? ToDictionary(trait) : new Godot.Collections.Dictionary();
    }

    /// <summary>Get all traits as dictionaries for GDScript.</summary>
    public static Godot.Collections.Array<Godot.Collections.Dictionary> GetAllTraitsAsDict()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var trait in _traits.Values)
        {
            result.Add(ToDictionary(trait));
        }
        return result;
    }

    /// <summary>Get unit modifiers for a trait as dictionaries for GDScript.</summary>
    public static Godot.Collections.Array<Godot.Collections.Dictionary> GetUnitModifiersForTraitAsDict(string traitId)
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var modifier in GetUnitModifiersForTrait(traitId))
        {
            result.Add(modifier.ToDictionary());
        }
        return result;
    }
}
