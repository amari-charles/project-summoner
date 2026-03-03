namespace Fateforged.Combat;

/// <summary>
/// Type of damage dealt by attacks and abilities.
/// </summary>
public enum DamageType
{
    /// <summary>Physical damage (default).</summary>
    Physical,

    /// <summary>Magical damage.</summary>
    Magical,

    /// <summary>True damage (ignores armor).</summary>
    True,

    /// <summary>Fire elemental damage.</summary>
    Fire,

    /// <summary>Ice elemental damage.</summary>
    Ice,

    /// <summary>Poison damage.</summary>
    Poison
}

/// <summary>
/// Extension methods for DamageType.
/// </summary>
public static class DamageTypeExtensions
{
    /// <summary>Convert enum to string value for serialization/interop.</summary>
    public static string ToStringValue(this DamageType type) => type switch
    {
        DamageType.Physical => "physical",
        DamageType.Magical => "magical",
        DamageType.True => "true",
        DamageType.Fire => "fire",
        DamageType.Ice => "ice",
        DamageType.Poison => "poison",
        _ => "physical"
    };

    /// <summary>Parse string to DamageType.</summary>
    public static DamageType ParseDamageType(string value) => value switch
    {
        "physical" => DamageType.Physical,
        "magical" => DamageType.Magical,
        "true" => DamageType.True,
        "fire" => DamageType.Fire,
        "ice" => DamageType.Ice,
        "poison" => DamageType.Poison,
        _ => DamageType.Physical
    };

    /// <summary>Get the base damage multiplier for this type.</summary>
    public static float GetBaseMultiplier(this DamageType type) => 1.0f;

    /// <summary>Check if this damage type ignores armor.</summary>
    public static bool IgnoresArmor(this DamageType type) => type == DamageType.True;

    /// <summary>Get the stat bonus key for this damage type (e.g., "fire_damage_bonus").</summary>
    public static string GetBonusStatKey(this DamageType type) =>
        type.ToStringValue() + "_damage_bonus";
}

/// <summary>
/// String constants for damage types (for backwards compatibility).
/// Use these instead of raw string literals.
/// </summary>
public static class DamageTypes
{
    public const string Physical = "physical";
    public const string Magical = "magical";
    public const string True = "true";
    public const string Fire = "fire";
    public const string Ice = "ice";
    public const string Poison = "poison";
}
