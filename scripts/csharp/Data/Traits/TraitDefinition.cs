using System.Collections.Generic;
using ProjectSummoner.Stats;

namespace ProjectSummoner.Data.Traits;

/// <summary>
/// Defines a summoner trait.
/// Traits are passive abilities that modify summoner stats or provide unit buffs.
/// </summary>
public class TraitDefinition
{
    /// <summary>Unique identifier (e.g., "trait_fire_affinity").</summary>
    public required TraitId Id { get; init; }

    /// <summary>Localization key for display name.</summary>
    public required string NameKey { get; init; }

    /// <summary>Localization key for description.</summary>
    public required string DescriptionKey { get; init; }

    /// <summary>Category for filtering (elemental, combat, defense, utility, milestone, special).</summary>
    public required TraitCategory Category { get; init; }

    /// <summary>True if this is an innate trait (comes with summoner).</summary>
    public bool IsInnate { get; init; }

    /// <summary>List of modifiers this trait provides.</summary>
    public List<TraitModifier> Modifiers { get; init; } = [];

    // =========================================================================
    // TRAIT OFFERING SYSTEM (for level-up selection)
    // =========================================================================

    /// <summary>
    /// Minimum summoner level required to be offered this trait.
    /// Default is 2 (first level-up opportunity).
    /// </summary>
    public int MinLevel { get; init; } = 2;

    /// <summary>
    /// Maximum summoner level this trait can be offered at.
    /// 0 means no maximum (available at any level once MinLevel is reached).
    /// </summary>
    public int MaxLevel { get; init; } = 0;

    /// <summary>
    /// Trait IDs that must be acquired before this trait becomes available.
    /// Creates a trait tree structure where early choices unlock later options.
    /// </summary>
    public string[] Prerequisites { get; init; } = [];

    // =========================================================================
    // TAG-BASED ELIGIBILITY SYSTEM
    // =========================================================================

    /// <summary>
    /// Tags that determine eligibility. Entity must have ANY of these tags.
    /// Use TraitTags constants for type safety.
    /// Example: [TraitTags.Summoner, TraitTags.Global] = available to all summoners
    /// Example: [TraitTags.Fire, TraitTags.Cole] = available to fire entities OR Cole
    /// </summary>
    public string[] Tags { get; init; } = [TraitTags.Summoner, TraitTags.Global];

    /// <summary>
    /// Additional required tags. Entity must have ALL of these tags.
    /// Used for more restrictive filtering after Tags check passes.
    /// Example: RequiredTags = [TraitTags.Elemental] = only elemental creatures
    /// </summary>
    public string[] RequiredTags { get; init; } = [];
}

/// <summary>
/// A single modifier provided by a trait.
///
/// TraitModifier operates in two distinct modes based on the Target property:
///
/// 1. SUMMONER STAT MODIFIER (Target = null or empty):
///    - Modifies the summoner's own stats directly
///    - Uses Stat/Type/Value properties
///    - Example: +10% fire_damage_bonus, +100 flat max_health
///    - Applied to the summoner character, not spawned units
///
/// 2. UNIT MODIFIER (Target = "unit"):
///    - Affects all units spawned by the summoner
///    - Uses StatMults/StatAdds/Conditions properties
///    - Can have trigger conditions (Berserker below 50% HP, etc.)
///    - Converted to StatModifier and provided via SummonerModifierProvider
///
/// Check IsUnitModifier property to determine which mode is active.
/// </summary>
public class TraitModifier
{
    // =========================================================================
    // SUMMONER STAT MODIFIER (when Target is null)
    // These modify the summoner character directly (not spawned units)
    // =========================================================================

    /// <summary>Stat to modify (type-safe).</summary>
    public StatKey? Stat { get; init; }

    /// <summary>Modifier type: Flat for additive, Percent for multiplicative.</summary>
    public ModifierType Type { get; init; } = ModifierType.Flat;

    /// <summary>Numeric value of the modifier.</summary>
    public float Value { get; init; }

    // =========================================================================
    // UNIT MODIFIER (when Target = "unit")
    // These affect all units spawned by the summoner
    // =========================================================================

    /// <summary>
    /// Target type. If "unit", this modifier affects spawned units.
    /// If null/empty, this is a summoner stat modifier.
    /// </summary>
    public string? Target { get; init; }

    /// <summary>Source identifier for tracking.</summary>
    public string? Source { get; init; }

    /// <summary>Conditions that must match for this modifier to apply.</summary>
    public Dictionary<string, object>? Conditions { get; init; }

    /// <summary>Multiplicative stat bonuses (e.g., {StatKey.AttackDamage: 1.10f}).</summary>
    public Dictionary<StatKey, float>? StatMults { get; init; }

    /// <summary>Additive stat bonuses.</summary>
    public Dictionary<StatKey, float>? StatAdds { get; init; }

    // =========================================================================
    // TRIGGER FIELDS (for conditional unit modifiers)
    // =========================================================================

    /// <summary>
    /// Trigger condition for when this modifier activates.
    /// Uses string values matching TriggerCondition enum.
    /// </summary>
    public string? Trigger { get; init; }

    /// <summary>
    /// Threshold value for HP-based triggers (0.0 - 1.0 representing percentage).
    /// </summary>
    public float TriggerThreshold { get; init; }

    /// <summary>
    /// How long the effect lasts after activation, in seconds.
    /// </summary>
    public float TriggerDuration { get; init; }

    /// <summary>
    /// Minimum time between activations, in seconds.
    /// </summary>
    public float TriggerCooldown { get; init; }

    /// <summary>Returns true if this is a unit modifier (target="unit").</summary>
    public bool IsUnitModifier => TraitTargetTypeExtensions.IsUnitTarget(Target);

    /// <summary>Returns true if this modifier has a summoner stat to modify.</summary>
    public bool HasSummonerStat => Stat.HasValue;

    /// <summary>Returns true if this modifier has a trigger condition.</summary>
    public bool HasTrigger => !string.IsNullOrEmpty(Trigger);
}
