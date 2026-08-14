namespace Fateforged.Data.Events;

/// <summary>
/// Strongly-typed identifier for biome types.
/// Prevents typos and enables IDE autocomplete via BiomeIds static class.
/// </summary>
public readonly record struct BiomeId(string Value)
{
    /// <summary>Returns the underlying string value.</summary>
    public override string ToString() => Value;

    /// <summary>Implicit conversion to string for interop with existing systems.</summary>
    public static implicit operator string(BiomeId id) => id.Value;

    /// <summary>Check if this ID has a value (not empty).</summary>
    public bool HasValue => !string.IsNullOrEmpty(Value);

    /// <summary>Empty/unset biome ID.</summary>
    public static readonly BiomeId None = new("");
}

/// <summary>
/// All known biome IDs. Use these instead of raw strings.
/// Example: BiomeIds.SummerPlains instead of "summer_plains"
/// </summary>
public static class BiomeIds
{
    /// <summary>Starting biome - grassy plains environment</summary>
    public static readonly BiomeId SummerPlains = new("summer_plains");

    /// <summary>Grass island surrounded by animated water.</summary>
    public static readonly BiomeId IslandWater = new("island_water");

    /// <summary>Default biome used as fallback</summary>
    public static readonly BiomeId Default = SummerPlains;

    /// <summary>Returns whether the biome has a corresponding authored resource.</summary>
    public static bool IsValid(BiomeId biome) =>
        biome == SummerPlains || biome == IslandWater;
}
