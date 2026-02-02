namespace ProjectSummoner.Data.Traits;

/// <summary>
/// Category of trait for filtering and organization.
/// </summary>
public enum TraitCategory
{
    /// <summary>Element-related traits (affinities, elemental bonuses).</summary>
    Elemental,

    /// <summary>Combat-focused traits (damage, attack speed).</summary>
    Combat,

    /// <summary>Defensive traits (HP, damage reduction).</summary>
    Defense,

    /// <summary>Utility traits (mana regen, cast speed, economy).</summary>
    Utility,

    /// <summary>Milestone traits (progression rewards).</summary>
    Milestone,

    /// <summary>Special traits (granted by specific events).</summary>
    Special
}

/// <summary>
/// Extension methods for TraitCategory.
/// </summary>
public static class TraitCategoryExtensions
{
    /// <summary>Convert enum to lowercase string value for serialization/interop.</summary>
    public static string ToStringValue(this TraitCategory category) => category switch
    {
        TraitCategory.Elemental => "elemental",
        TraitCategory.Combat => "combat",
        TraitCategory.Defense => "defense",
        TraitCategory.Utility => "utility",
        TraitCategory.Milestone => "milestone",
        TraitCategory.Special => "special",
        _ => "utility"
    };

    /// <summary>Parse string to TraitCategory.</summary>
    public static TraitCategory ParseCategory(string value) => value.ToLowerInvariant() switch
    {
        "elemental" => TraitCategory.Elemental,
        "combat" => TraitCategory.Combat,
        "defense" => TraitCategory.Defense,
        "utility" => TraitCategory.Utility,
        "milestone" => TraitCategory.Milestone,
        "special" => TraitCategory.Special,
        _ => TraitCategory.Utility
    };
}
