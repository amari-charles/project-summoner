using System.Collections.Generic;

namespace ProjectSummoner.Data.Traits;

/// <summary>
/// Defines a summoner trait or boon.
/// Traits are passive abilities that modify summoner stats or provide unit buffs.
/// </summary>
public class TraitDefinition
{
    /// <summary>Unique identifier (e.g., "trait_fire_affinity").</summary>
    public required string Id { get; init; }

    /// <summary>Localization key for display name.</summary>
    public required string NameKey { get; init; }

    /// <summary>Localization key for description.</summary>
    public required string DescriptionKey { get; init; }

    /// <summary>Category for filtering (elemental, combat, defense, utility, milestone, special).</summary>
    public required string Category { get; init; }

    /// <summary>True if this is an innate trait (comes with summoner), false if acquirable boon.</summary>
    public bool IsInnate { get; init; }

    /// <summary>List of modifiers this trait provides.</summary>
    public List<TraitModifier> Modifiers { get; init; } = [];
}

/// <summary>
/// A single modifier provided by a trait.
/// Can be either a summoner stat modifier or a unit modifier.
/// </summary>
public class TraitModifier
{
    // =========================================================================
    // SUMMONER STAT MODIFIER (when Target is null)
    // =========================================================================

    /// <summary>Stat to modify (e.g., "fire_damage_bonus", "max_health").</summary>
    public string? Stat { get; init; }

    /// <summary>Modifier type: "flat" for additive, "percent" for multiplicative.</summary>
    public string Type { get; init; } = "flat";

    /// <summary>Numeric value of the modifier.</summary>
    public float Value { get; init; }

    // =========================================================================
    // UNIT MODIFIER (when Target = "unit")
    // =========================================================================

    /// <summary>Target type. If "unit", this modifier affects spawned units.</summary>
    public string? Target { get; init; }

    /// <summary>Source identifier for tracking.</summary>
    public string? Source { get; init; }

    /// <summary>Conditions that must match for this modifier to apply.</summary>
    public Dictionary<string, object>? Conditions { get; init; }

    /// <summary>Multiplicative stat bonuses (e.g., {"attack_damage": 1.10f}).</summary>
    public Dictionary<string, float>? StatMults { get; init; }

    /// <summary>Additive stat bonuses.</summary>
    public Dictionary<string, float>? StatAdds { get; init; }

    /// <summary>Returns true if this is a unit modifier (target="unit").</summary>
    public bool IsUnitModifier => Target == "unit";
}
