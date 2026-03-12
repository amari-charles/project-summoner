namespace Fateforged.Data.Traits;

/// <summary>
/// Type of stat modifier (how the value is applied).
/// </summary>
public enum ModifierType
{
    /// <summary>Additive modifier (value is added directly).</summary>
    Flat,

    /// <summary>Multiplicative modifier (value is a percentage).</summary>
    Percent,
}

/// <summary>
/// Extension methods for ModifierType.
/// </summary>
public static class ModifierTypeExtensions
{
    /// <summary>Convert enum to string value for serialization/interop.</summary>
    public static string ToStringValue(this ModifierType type) =>
        type switch
        {
            ModifierType.Flat => "flat",
            ModifierType.Percent => "percent",
            _ => "flat",
        };

    /// <summary>Parse string to ModifierType.</summary>
    public static ModifierType ParseModifierType(string value) =>
        value switch
        {
            "flat" => ModifierType.Flat,
            "percent" => ModifierType.Percent,
            _ => ModifierType.Flat,
        };
}
