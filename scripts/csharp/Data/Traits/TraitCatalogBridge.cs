using Godot;
using ProjectSummoner.Stats;

namespace ProjectSummoner.Data.Traits;

/// <summary>
/// Bridge node for GDScript to access C# TraitCatalog.
/// Registered as autoload "TraitCatalogCS" in project.godot.
/// Wraps static TraitCatalog methods as instance methods for GDScript compatibility.
/// Note: GDScript can call these PascalCase methods directly - Godot 4 auto-converts snake_case calls.
/// </summary>
public partial class TraitCatalogBridge : Node
{
    public static TraitCatalogBridge? Instance { get; private set; }

    private Node? _loc;

    public override void _Ready()
    {
        Instance = this;
        CallDeferred(nameof(CacheDependencies));
        GD.Print($"TraitCatalogBridge: Initialized with {TraitCatalog.Count} traits");
    }

    private void CacheDependencies()
    {
        _loc = GetNodeOrNull<Node>("/root/Loc");
    }

    // =========================================================================
    // LOOKUP METHODS
    // =========================================================================

    /// <summary>Get a trait as dictionary by ID. Returns empty dict if not found.</summary>
    public Godot.Collections.Dictionary GetTrait(string traitId)
    {
        return TraitCatalog.GetTraitAsDict(traitId);
    }

    /// <summary>Check if a trait exists in the catalog.</summary>
    public bool HasTrait(string traitId)
    {
        return TraitCatalog.HasTrait(traitId);
    }

    /// <summary>Get all trait IDs.</summary>
    public string[] GetAllTraitIds()
    {
        return TraitCatalog.GetAllTraitIds();
    }

    /// <summary>Get trait count.</summary>
    public int GetTraitCount()
    {
        return TraitCatalog.Count;
    }

    // =========================================================================
    // QUERY METHODS
    // =========================================================================

    /// <summary>Get all traits as dictionaries.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetAllTraits()
    {
        return TraitCatalog.GetAllTraitsAsDict();
    }

    /// <summary>Get traits by category as dictionaries.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetTraitsByCategory(string category)
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var trait in TraitCatalog.GetTraitsByCategory(category))
        {
            result.Add(TraitCatalog.ToDictionary(trait));
        }
        return result;
    }

    /// <summary>Get only innate traits as dictionaries.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetInnateTraits()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var trait in TraitCatalog.GetInnateTraits())
        {
            result.Add(TraitCatalog.ToDictionary(trait));
        }
        return result;
    }

    // =========================================================================
    // UNIT MODIFIER METHODS (for SummonerModifierProvider)
    // =========================================================================

    /// <summary>Get unit modifiers for a trait as dictionaries.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetUnitModifiersForTrait(string traitId)
    {
        return TraitCatalog.GetUnitModifiersForTraitAsDict(traitId);
    }

    // =========================================================================
    // TRAIT OFFERING SYSTEM (for level-up selection)
    // =========================================================================

    /// <summary>
    /// Get traits available for level-up using tag-based eligibility.
    /// Works for summoners, summons, and spells.
    /// </summary>
    /// <param name="entityTags">Tags from the entity (summoner, summon, or spell)</param>
    /// <param name="currentLevel">Entity's current level</param>
    /// <param name="acquiredTraitIds">Trait IDs already acquired</param>
    /// <param name="count">Maximum number of traits to return (0 = all eligible)</param>
    /// <returns>Array of eligible trait dictionaries</returns>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetAvailableTraitsForLevelUp(
        Godot.Collections.Array<string> entityTags,
        int currentLevel,
        Godot.Collections.Array<string> acquiredTraitIds,
        int count = 3)
    {
        var tags = new string[entityTags.Count];
        for (int i = 0; i < entityTags.Count; i++)
            tags[i] = entityTags[i];

        var acquired = new string[acquiredTraitIds.Count];
        for (int i = 0; i < acquiredTraitIds.Count; i++)
            acquired[i] = acquiredTraitIds[i];

        var traits = TraitCatalog.GetAvailableTraitsForLevelUp(tags, currentLevel, acquired, count);

        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var trait in traits)
        {
            result.Add(TraitCatalog.ToDictionary(trait));
        }
        return result;
    }

    /// <summary>
    /// Get traits in the global pool for a specific entity type.
    /// </summary>
    /// <param name="entityType">Entity type tag (e.g., "summoner", "summon", or "spell")</param>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetGlobalPoolTraits(string entityType = TraitTags.Summoner)
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var trait in TraitCatalog.GetGlobalPoolTraits(entityType))
        {
            result.Add(TraitCatalog.ToDictionary(trait));
        }
        return result;
    }

    /// <summary>
    /// Check if an entity meets the prerequisites for a specific trait.
    /// </summary>
    public bool MeetsPrerequisites(string traitId, Godot.Collections.Array<string> acquiredTraitIds)
    {
        var acquired = new string[acquiredTraitIds.Count];
        for (int i = 0; i < acquiredTraitIds.Count; i++)
            acquired[i] = acquiredTraitIds[i];

        return TraitCatalog.MeetsPrerequisites(traitId, acquired);
    }

    // =========================================================================
    // DISPLAY HELPERS
    // =========================================================================

    /// <summary>Get localized trait name.</summary>
    public string GetTraitName(string traitId)
    {
        var trait = TraitCatalog.GetTrait(traitId);
        if (trait == null) return traitId;

        if (_loc != null && _loc.HasMethod("t"))
        {
            return (string)_loc.Call("t", trait.NameKey);
        }
        return trait.NameKey;
    }

    /// <summary>Get localized trait description.</summary>
    public string GetTraitDescription(string traitId)
    {
        var trait = TraitCatalog.GetTrait(traitId);
        if (trait == null) return "";

        if (_loc != null && _loc.HasMethod("t"))
        {
            return (string)_loc.Call("t", trait.DescriptionKey);
        }
        return trait.DescriptionKey;
    }

    /// <summary>Get formatted modifier text for a trait.</summary>
    public string GetTraitModifierText(string traitId)
    {
        var trait = TraitCatalog.GetTrait(traitId);
        if (trait == null) return "";

        var texts = new System.Collections.Generic.List<string>();

        foreach (var mod in trait.Modifiers)
        {
            if (!mod.HasSummonerStat) continue;

            var sign = mod.Value >= 0 ? "+" : "";
            var suffix = mod.Type == ModifierType.Percent ? "%" : "";
            // Convert StatKey to readable name
            var statSnake = mod.Stat!.Value.ToSnakeCase();
            var statName = ToTitleCase(statSnake.Replace("_", " "));

            texts.Add($"{sign}{mod.Value}{suffix} {statName}");
        }

        return string.Join(", ", texts);
    }

    /// <summary>Simple title case conversion.</summary>
    private static string ToTitleCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var words = input.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                words[i] = char.ToUpperInvariant(words[i][0]) + words[i][1..].ToLowerInvariant();
            }
        }
        return string.Join(" ", words);
    }
}
