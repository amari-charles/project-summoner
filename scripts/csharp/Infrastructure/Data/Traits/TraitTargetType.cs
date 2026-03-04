namespace Fateforged.Data.Traits;

/// <summary>
/// Target type for trait modifiers.
/// Determines what entity the modifier affects.
/// </summary>
public enum TraitTargetType
{
    /// <summary>Modifier affects the summoner character directly.</summary>
    Summoner,

    /// <summary>Modifier affects all units spawned by the summoner.</summary>
    Unit
}

/// <summary>
/// Extension methods for TraitTargetType.
/// </summary>
public static class TraitTargetTypeExtensions
{
    /// <summary>Convert enum to string value for serialization/interop.</summary>
    public static string ToStringValue(this TraitTargetType type) => type switch
    {
        TraitTargetType.Summoner => "summoner",
        TraitTargetType.Unit => "unit",
        _ => "summoner"
    };

    /// <summary>Parse string to TraitTargetType.</summary>
    public static TraitTargetType ParseTraitTargetType(string? value) => value switch
    {
        "unit" => TraitTargetType.Unit,
        "summoner" => TraitTargetType.Summoner,
        null or "" => TraitTargetType.Summoner, // Default when Target is null/empty
        _ => TraitTargetType.Summoner
    };

    /// <summary>Check if value represents a unit modifier target.</summary>
    public static bool IsUnitTarget(string? value) => value == "unit";
}
