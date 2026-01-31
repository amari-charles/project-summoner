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
            Tags = [TraitTags.Summoner, TraitTags.Fire],
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
            Tags = [TraitTags.Summoner, TraitTags.Fire],
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
            Tags = [TraitTags.Summoner, TraitTags.Water],
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
            Tags = [TraitTags.Summoner, TraitTags.Water],
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
            Tags = [TraitTags.Summoner, TraitTags.Wind],
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
            Tags = [TraitTags.Summoner, TraitTags.Wind],
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
            Tags = [TraitTags.Summoner, TraitTags.Earth],
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
            Tags = [TraitTags.Summoner, TraitTags.Earth],
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
            Tags = [TraitTags.Summoner, TraitTags.Lightning],
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
            Tags = [TraitTags.Summoner, TraitTags.Life],
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
            Tags = [TraitTags.Summoner, TraitTags.Death],
            Modifiers =
            [
                new TraitModifier { Stat = "death_damage_bonus", Type = "percent", Value = 10.0f },
                new TraitModifier { Stat = "lifesteal", Type = "percent", Value = 5.0f }
            ]
        },

        // =====================================================================
        // ACQUIRABLE TRAITS - Global Summoner Pool
        // =====================================================================

        [TraitId.IronWill] = new TraitDefinition
        {
            Id = TraitId.IronWill,
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
        },

        [TraitId.QuickRecovery] = new TraitDefinition
        {
            Id = TraitId.QuickRecovery,
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
        },

        [TraitId.VitalityBoost] = new TraitDefinition
        {
            Id = TraitId.VitalityBoost,
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
        },

        [TraitId.SwiftStrike] = new TraitDefinition
        {
            Id = TraitId.SwiftStrike,
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
                    Source = TraitId.SwiftStrike,
                    StatMults = new() { ["attack_speed"] = 1.10f }
                }
            ]
        },

        // =====================================================================
        // ACQUIRABLE TRAITS - Triggered (conditional effects for summoners)
        // =====================================================================

        [TraitId.Berserker] = new TraitDefinition
        {
            Id = TraitId.Berserker,
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
                    Source = TraitId.Berserker,
                    StatMults = new() { ["attack_damage"] = 1.20f }, // +20% damage
                    Trigger = "BelowHpPercent",
                    TriggerThreshold = 0.5f // Below 50% HP
                }
            ]
        },

        [TraitId.Vengeful] = new TraitDefinition
        {
            Id = TraitId.Vengeful,
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
                    Source = TraitId.Vengeful,
                    StatMults = new() { ["attack_speed"] = 1.10f }, // +10% attack speed
                    Trigger = "OnTakeHit",
                    TriggerDuration = 5.0f, // Lasts 5 seconds
                    TriggerCooldown = 1.0f  // 1 second cooldown between activations
                }
            ]
        },

        [TraitId.SoulHarvest] = new TraitDefinition
        {
            Id = TraitId.SoulHarvest,
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
                    Source = TraitId.SoulHarvest,
                    StatAdds = new() { ["heal_on_kill"] = 5.0f }, // Heal 5 HP on kill
                    Trigger = "OnKill"
                }
            ]
        },

        // =====================================================================
        // ACQUIRABLE TRAITS - Element-Exclusive Summoner Traits
        // =====================================================================

        [TraitId.InfernoMastery] = new TraitDefinition
        {
            Id = TraitId.InfernoMastery,
            NameKey = "trait.inferno_mastery.name",
            DescriptionKey = "trait.inferno_mastery.description",
            Category = "elemental",
            IsInnate = false,
            Tags = [TraitTags.Summoner, TraitTags.Fire], // Only fire summoners
            MinLevel = 5,
            Prerequisites = [TraitId.FireAffinity],
            Modifiers =
            [
                new TraitModifier { Stat = "fire_damage_bonus", Type = "percent", Value = 15.0f },
                new TraitModifier
                {
                    Target = "unit",
                    Source = TraitId.InfernoMastery,
                    Conditions = new() { ["elemental_affinity"] = "fire" },
                    StatMults = new() { ["attack_damage"] = 1.15f }
                }
            ]
        },

        [TraitId.TidalMastery] = new TraitDefinition
        {
            Id = TraitId.TidalMastery,
            NameKey = "trait.tidal_mastery.name",
            DescriptionKey = "trait.tidal_mastery.description",
            Category = "elemental",
            IsInnate = false,
            Tags = [TraitTags.Summoner, TraitTags.Water], // Only water summoners
            MinLevel = 5,
            Prerequisites = [TraitId.WaterAffinity],
            Modifiers =
            [
                new TraitModifier { Stat = "water_damage_bonus", Type = "percent", Value = 15.0f },
                new TraitModifier
                {
                    Target = "unit",
                    Source = TraitId.TidalMastery,
                    Conditions = new() { ["elemental_affinity"] = "water" },
                    StatMults = new() { ["max_health"] = 1.15f }
                }
            ]
        },

        // =====================================================================
        // SUMMON TRAITS - Global Pool (available to all summons)
        // =====================================================================

        [TraitId.Fortitude] = new TraitDefinition
        {
            Id = TraitId.Fortitude,
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
                    Source = TraitId.Fortitude,
                    StatMults = new() { ["max_hp"] = 1.08f } // +8% HP
                }
            ]
        },

        [TraitId.Power] = new TraitDefinition
        {
            Id = TraitId.Power,
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
                    Source = TraitId.Power,
                    StatMults = new() { ["attack_damage"] = 1.06f } // +6% damage
                }
            ]
        },

        [TraitId.Swiftness] = new TraitDefinition
        {
            Id = TraitId.Swiftness,
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
                    Source = TraitId.Swiftness,
                    StatMults = new() { ["attack_speed"] = 1.05f } // +5% attack speed
                }
            ]
        },

        [TraitId.Agility] = new TraitDefinition
        {
            Id = TraitId.Agility,
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
                    Source = TraitId.Agility,
                    StatMults = new() { ["move_speed"] = 1.05f } // +5% move speed
                }
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

    // =========================================================================
    // TRAIT OFFERING SYSTEM (unified tag-based eligibility)
    // =========================================================================

    /// <summary>
    /// Get traits available for level-up selection using tag-based eligibility.
    /// Works for both summoners and cards/units.
    /// </summary>
    /// <param name="entityTags">Tags from the entity (summoner or card)</param>
    /// <param name="currentLevel">Entity's current level</param>
    /// <param name="acquiredTraitIds">Trait IDs already acquired (excluded from results)</param>
    /// <param name="count">Maximum number of traits to return (0 = all eligible)</param>
    /// <returns>Array of eligible traits, shuffled if count > 0</returns>
    public static TraitDefinition[] GetAvailableTraitsForLevelUp(
        string[] entityTags,
        int currentLevel,
        IEnumerable<string> acquiredTraitIds,
        int count = 3)
    {
        var acquired = new HashSet<string>(acquiredTraitIds);
        var entityTagSet = new HashSet<string>(entityTags);

        var eligible = new List<TraitDefinition>();

        foreach (var trait in _traits.Values)
        {
            // Skip innate traits (they come with entities, not offered)
            if (trait.IsInnate)
                continue;

            // Skip already acquired
            if (acquired.Contains(trait.Id))
                continue;

            // Check tag eligibility: (any of Tags) AND (all of RequiredTags)
            bool hasAnyTag = trait.Tags.Any(t => entityTagSet.Contains(t));
            bool hasAllRequired = trait.RequiredTags.All(t => entityTagSet.Contains(t));
            if (!hasAnyTag || !hasAllRequired)
                continue;

            // Check level requirements
            if (currentLevel < trait.MinLevel)
                continue;
            if (trait.MaxLevel > 0 && currentLevel > trait.MaxLevel)
                continue;

            // Check prerequisites (all must be acquired)
            if (trait.Prerequisites.Length > 0)
            {
                bool hasAllPrereqs = trait.Prerequisites.All(prereqId => acquired.Contains(prereqId));
                if (!hasAllPrereqs)
                    continue;
            }

            eligible.Add(trait);
        }

        // Shuffle and return requested count
        if (count > 0 && eligible.Count > count)
        {
            return eligible
                .OrderBy(_ => System.Random.Shared.Next())
                .Take(count)
                .ToArray();
        }

        return [.. eligible];
    }

    /// <summary>
    /// Get traits available for a summoner to choose at level-up.
    /// Uses the summoner's TraitTags for eligibility.
    /// </summary>
    /// <param name="summonerDef">The summoner definition</param>
    /// <param name="currentLevel">Summoner's current level</param>
    /// <param name="acquiredTraitIds">Trait IDs the summoner has already acquired</param>
    /// <param name="count">Maximum number of traits to return (0 = all eligible)</param>
    /// <returns>Array of eligible traits, shuffled if count > 0</returns>
    public static TraitDefinition[] GetAvailableTraitsForLevelUp(
        Summoners.SummonerDefinition summonerDef,
        int currentLevel,
        IEnumerable<string> acquiredTraitIds,
        int count = 3)
    {
        // Include innate traits in acquired set (can't re-acquire innate traits)
        var acquired = new HashSet<string>(acquiredTraitIds);
        foreach (var innateId in summonerDef.InnateTraitIds)
        {
            acquired.Add(innateId);
        }

        return GetAvailableTraitsForLevelUp(summonerDef.TraitEligibilityTags, currentLevel, acquired, count);
    }

    /// <summary>
    /// Get all traits with the Global tag for the specified entity type.
    /// </summary>
    public static TraitDefinition[] GetGlobalPoolTraits(string entityType = TraitTags.Summoner)
    {
        return _traits.Values.Where(t =>
            !t.IsInnate &&
            t.Tags.Contains(TraitTags.Global) &&
            t.Tags.Contains(entityType)
        ).ToArray();
    }

    /// <summary>
    /// Check if an entity meets the prerequisites for a specific trait.
    /// </summary>
    public static bool MeetsPrerequisites(string traitId, IEnumerable<string> acquiredTraitIds)
    {
        var trait = GetTrait(traitId);
        if (trait == null) return false;
        if (trait.Prerequisites.Length == 0) return true;

        var acquired = new HashSet<string>(acquiredTraitIds);
        return trait.Prerequisites.All(prereqId => acquired.Contains(prereqId));
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
            var statMod = new StatModifier
            {
                Source = mod.Source ?? traitId,
                Conditions = mod.Conditions ?? [],
                StatMults = mod.StatMults ?? [],
                StatAdds = mod.StatAdds ?? []
            };

            // Copy trigger fields if present
            if (mod.HasTrigger)
            {
                if (System.Enum.TryParse<Systems.Modifiers.TriggerCondition>(mod.Trigger, ignoreCase: true, out var trigger))
                {
                    statMod.Trigger = trigger;
                }
                statMod.TriggerThreshold = mod.TriggerThreshold;
                statMod.TriggerDuration = mod.TriggerDuration;
                statMod.TriggerCooldown = mod.TriggerCooldown;
            }

            result.Add(statMod);
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

        // Convert tags to Godot array
        var tagsArray = new Godot.Collections.Array<string>();
        foreach (var tag in trait.Tags)
        {
            tagsArray.Add(tag);
        }

        var requiredTagsArray = new Godot.Collections.Array<string>();
        foreach (var tag in trait.RequiredTags)
        {
            requiredTagsArray.Add(tag);
        }

        var prerequisitesArray = new Godot.Collections.Array<string>();
        foreach (var prereq in trait.Prerequisites)
        {
            prerequisitesArray.Add(prereq);
        }

        return new Godot.Collections.Dictionary
        {
            ["id"] = trait.Id,
            ["name_key"] = trait.NameKey,
            ["description_key"] = trait.DescriptionKey,
            ["category"] = trait.Category,
            ["is_innate"] = trait.IsInnate,
            ["tags"] = tagsArray,
            ["required_tags"] = requiredTagsArray,
            ["min_level"] = trait.MinLevel,
            ["max_level"] = trait.MaxLevel,
            ["prerequisites"] = prerequisitesArray,
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
