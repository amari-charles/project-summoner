using Godot;

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

    /// <summary>Get only acquirable boons as dictionaries.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetAcquirableBoons()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var trait in TraitCatalog.GetAcquirableBoons())
        {
            result.Add(TraitCatalog.ToDictionary(trait));
        }
        return result;
    }

    /// <summary>
    /// Get a pool of traits for level-up selection.
    /// Returns random acquirable traits excluding those already acquired.
    /// </summary>
    /// <param name="excludedIds">Trait IDs to exclude (already acquired)</param>
    /// <param name="count">Number of traits to return (default 3)</param>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetLevelUpTraitPool(
        Godot.Collections.Array<string> excludedIds,
        int count = 3)
    {
        var excluded = new System.Collections.Generic.List<string>();
        foreach (var id in excludedIds)
        {
            excluded.Add(id);
        }

        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var trait in TraitCatalog.GetLevelUpTraitPool(excluded, count))
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
            if (string.IsNullOrEmpty(mod.Stat)) continue;

            var sign = mod.Value >= 0 ? "+" : "";
            var suffix = mod.Type == "percent" ? "%" : "";
            // Simple title case without relying on CultureInfo
            var statName = ToTitleCase(mod.Stat.Replace("_", " "));

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
