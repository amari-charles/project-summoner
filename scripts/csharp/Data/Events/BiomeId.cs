namespace ProjectSummoner.Data.Events;

/// <summary>
/// Biome ID constants - type-safe biome references.
/// Mirrors GDScript BiomeIDs for C# code.
/// </summary>
public static class BiomeId
{
    /// <summary>Starting biome - grassy plains environment</summary>
    public const string SummerPlains = "summer_plains";

    /// <summary>Default biome used as fallback</summary>
    public const string Default = SummerPlains;
}
