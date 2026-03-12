using System.Collections.Generic;
using System.Linq;
using Fateforged.Stats;
using Godot;

namespace Fateforged.Data.Traits;

/// <summary>
/// Central registry of all trait definitions.
/// Provides type-safe trait lookup and query methods.
/// GDScript can call this via the TraitCatalogBridge autoload.
///
/// Trait definitions are stored in TraitDefinitions.cs as static readonly fields.
/// This class provides lookup and query methods over those definitions.
/// </summary>
public static class TraitCatalog
{
    // =========================================================================
    // LOOKUP METHODS
    // =========================================================================

    /// <summary>Get a trait definition by ID. Returns null if not found.</summary>
    public static TraitDefinition? GetTrait(string id) => TraitDefinitions.Get(id);

    /// <summary>Get a trait definition by typed TraitId. Returns null if not found.</summary>
    public static TraitDefinition? GetTrait(TraitId id) => TraitDefinitions.Get(id);

    /// <summary>Check if a trait exists in the catalog.</summary>
    public static bool HasTrait(string id) => TraitDefinitions.Has(id);

    /// <summary>Check if a trait exists in the catalog by typed TraitId.</summary>
    public static bool HasTrait(TraitId id) => TraitDefinitions.Has(id);

    /// <summary>Get all trait IDs.</summary>
    public static string[] GetAllTraitIds() => [.. TraitDefinitions.AllIds];

    /// <summary>Get all trait definitions.</summary>
    public static TraitDefinition[] GetAllTraits() => [.. TraitDefinitions.All];

    /// <summary>Get trait count.</summary>
    public static int Count => TraitDefinitions.Count;

    // =========================================================================
    // QUERY METHODS
    // =========================================================================

    /// <summary>Get traits by category (accepts string for GDScript interop).</summary>
    public static TraitDefinition[] GetTraitsByCategory(string category)
    {
        var parsedCategory = TraitCategoryExtensions.ParseCategory(category);
        return TraitDefinitions.All.Where(t => t.Category == parsedCategory).ToArray();
    }

    /// <summary>Get traits by category (typed).</summary>
    public static TraitDefinition[] GetTraitsByCategory(TraitCategory category)
    {
        return TraitDefinitions.All.Where(t => t.Category == category).ToArray();
    }

    /// <summary>Get only innate traits.</summary>
    public static TraitDefinition[] GetInnateTraits()
    {
        return TraitDefinitions.All.Where(t => t.IsInnate).ToArray();
    }

    /// <summary>Get traits by acquisition mode.</summary>
    public static TraitDefinition[] GetTraitsByAcquisitionMode(TraitAcquisitionMode mode)
    {
        return TraitDefinitions.All.Where(t => t.AcquisitionMode == mode).ToArray();
    }

    /// <summary>Get traits by acquisition mode (accepts string for GDScript interop).</summary>
    public static TraitDefinition[] GetTraitsByAcquisitionMode(string mode)
    {
        var parsedMode = TraitAcquisitionModeExtensions.Parse(mode);
        return GetTraitsByAcquisitionMode(parsedMode);
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
        int count = 3
    )
    {
        var acquired = new HashSet<string>(acquiredTraitIds);
        var entityTagSet = new HashSet<string>(entityTags);

        var eligible = new List<TraitDefinition>();

        foreach (var trait in TraitDefinitions.All)
        {
            // Skip innate traits (they come with entities, not offered)
            if (trait.IsInnate)
                continue;
            if (trait.AcquisitionMode != TraitAcquisitionMode.LevelUpOffer)
                continue;

            // Skip already acquired
            if (acquired.Contains((string)trait.Id))
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
                bool hasAllPrereqs = trait.Prerequisites.All(prereqId =>
                    acquired.Contains(prereqId)
                );
                if (!hasAllPrereqs)
                    continue;
            }

            eligible.Add(trait);
        }

        // Shuffle and return requested count
        if (count > 0 && eligible.Count > count)
        {
            return eligible.OrderBy(_ => System.Random.Shared.Next()).Take(count).ToArray();
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
        IEnumerable<TraitId> acquiredTraitIds,
        int count = 3
    )
    {
        // Include innate traits in acquired set (can't re-acquire innate traits)
        // Convert TraitId to string for matching
        var acquired = new HashSet<string>(acquiredTraitIds.Select(id => (string)id));
        foreach (var innateId in summonerDef.InnateTraitIds)
        {
            acquired.Add(innateId);
        }

        return GetAvailableTraitsForLevelUp(
            summonerDef.TraitEligibilityTags,
            currentLevel,
            acquired,
            count
        );
    }

    /// <summary>
    /// Get all traits with the Global tag for the specified entity type.
    /// </summary>
    public static TraitDefinition[] GetGlobalPoolTraits(string entityType = TraitTags.Summoner)
    {
        return TraitDefinitions
            .All.Where(t =>
                !t.IsInnate
                && t.AcquisitionMode == TraitAcquisitionMode.LevelUpOffer
                && t.Tags.Contains(TraitTags.Global)
                && t.Tags.Contains(entityType)
            )
            .ToArray();
    }

    /// <summary>
    /// Check if an entity meets the prerequisites for a specific trait.
    /// </summary>
    public static bool MeetsPrerequisites(string traitId, IEnumerable<string> acquiredTraitIds)
    {
        var trait = GetTrait(traitId);
        if (trait == null)
            return false;
        if (trait.Prerequisites.Length == 0)
            return true;

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
        if (trait == null)
            return [];

        var result = new List<StatModifier>();
        foreach (var mod in trait.Modifiers.Where(m => m.IsUnitModifier))
        {
            var statMod = new StatModifier
            {
                Source = mod.Source ?? traitId,
                Conditions = mod.Conditions ?? [],
                StatMults = mod.StatMults ?? [],
                StatAdds = mod.StatAdds ?? [],
            };

            // Copy trigger fields if present
            if (mod.HasTrigger)
            {
                if (
                    System.Enum.TryParse<TriggerCondition>(
                        mod.Trigger,
                        ignoreCase: true,
                        out var trigger
                    )
                )
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
            if (mod.HasSummonerStat)
            {
                modDict["stat"] = mod.Stat!.Value.ToSnakeCase();
                modDict["type"] = mod.Type.ToStringValue();
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
                    conditionsDict[kvp.Key] = kvp.Value switch
                    {
                        string s => s,
                        int i => i,
                        float f => f,
                        double d => (float)d,
                        bool b => b,
                        _ => kvp.Value?.ToString() ?? "",
                    };
                }
                modDict["conditions"] = conditionsDict;
            }
            if (mod.StatMults != null && mod.StatMults.Count > 0)
            {
                var statMultsDict = new Godot.Collections.Dictionary();
                foreach (var kvp in mod.StatMults)
                {
                    statMultsDict[kvp.Key.ToSnakeCase()] = kvp.Value;
                }
                modDict["stat_mults"] = statMultsDict;
            }
            if (mod.StatAdds != null && mod.StatAdds.Count > 0)
            {
                var statAddsDict = new Godot.Collections.Dictionary();
                foreach (var kvp in mod.StatAdds)
                {
                    statAddsDict[kvp.Key.ToSnakeCase()] = kvp.Value;
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
            ["id"] = (string)trait.Id,
            ["name_key"] = trait.NameKey,
            ["description_key"] = trait.DescriptionKey,
            ["category"] = trait.Category.ToStringValue(),
            ["is_innate"] = trait.IsInnate,
            ["acquisition_mode"] = trait.AcquisitionMode.ToStringValue(),
            ["tags"] = tagsArray,
            ["required_tags"] = requiredTagsArray,
            ["min_level"] = trait.MinLevel,
            ["max_level"] = trait.MaxLevel,
            ["prerequisites"] = prerequisitesArray,
            ["modifiers"] = modifiersArray,
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
        foreach (var trait in TraitDefinitions.All)
        {
            result.Add(ToDictionary(trait));
        }
        return result;
    }

    /// <summary>Get unit modifiers for a trait as dictionaries for GDScript.</summary>
    public static Godot.Collections.Array<Godot.Collections.Dictionary> GetUnitModifiersForTraitAsDict(
        string traitId
    )
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var modifier in GetUnitModifiersForTrait(traitId))
        {
            result.Add(modifier.ToDictionary());
        }
        return result;
    }
}
