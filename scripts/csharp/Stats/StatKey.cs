namespace ProjectSummoner.Stats;

/// <summary>
/// Type-safe enumeration of all unit stat keys.
/// Use this instead of string keys for compile-time validation.
/// </summary>
public enum StatKey
{
    MaxHp,
    AttackDamage,
    AttackSpeed,
    MoveSpeed,
    AttackRange,
    AggroRadius
}

/// <summary>
/// Extension methods for StatKey conversion and validation.
/// </summary>
public static class StatKeyExtensions
{
    /// <summary>
    /// Converts a StatKey to its snake_case string representation.
    /// Used for GDScript interop and dictionary keys.
    /// </summary>
    public static string ToSnakeCase(this StatKey key) => key switch
    {
        StatKey.MaxHp => "max_hp",
        StatKey.AttackDamage => "attack_damage",
        StatKey.AttackSpeed => "attack_speed",
        StatKey.MoveSpeed => "move_speed",
        StatKey.AttackRange => "attack_range",
        StatKey.AggroRadius => "aggro_radius",
        _ => key.ToString().ToLowerInvariant()
    };

    /// <summary>
    /// Converts a StatKey to its PascalCase property name.
    /// Used for C# property access.
    /// </summary>
    public static string ToPascalCase(this StatKey key) => key switch
    {
        StatKey.MaxHp => "MaxHp",
        StatKey.AttackDamage => "AttackDamage",
        StatKey.AttackSpeed => "AttackSpeed",
        StatKey.MoveSpeed => "MoveSpeed",
        StatKey.AttackRange => "AttackRange",
        StatKey.AggroRadius => "AggroRadius",
        _ => key.ToString()
    };

    /// <summary>
    /// Parses a string to a StatKey. Returns null if the string is not a valid stat key.
    /// Accepts both snake_case and PascalCase formats.
    /// </summary>
    public static StatKey? FromString(string? s)
    {
        if (string.IsNullOrEmpty(s))
            return null;

        return s.ToLowerInvariant().Replace("_", "") switch
        {
            "maxhp" => StatKey.MaxHp,
            "attackdamage" => StatKey.AttackDamage,
            "attackspeed" => StatKey.AttackSpeed,
            "movespeed" => StatKey.MoveSpeed,
            "attackrange" => StatKey.AttackRange,
            "aggroradius" => StatKey.AggroRadius,
            _ => null
        };
    }

    /// <summary>
    /// Checks if a string represents a valid stat key.
    /// </summary>
    public static bool IsValidStatKey(string? s) => FromString(s) != null;

    /// <summary>
    /// Gets the default value for a stat key.
    /// </summary>
    public static float GetDefault(this StatKey key) => key switch
    {
        StatKey.MaxHp => 100f,
        StatKey.AttackDamage => 10f,
        StatKey.AttackSpeed => 1f,
        StatKey.MoveSpeed => 3f,
        StatKey.AttackRange => 2f,
        StatKey.AggroRadius => 20f,
        _ => 0f
    };

    /// <summary>
    /// Gets all stat keys as an array.
    /// </summary>
    public static StatKey[] All => new[]
    {
        StatKey.MaxHp,
        StatKey.AttackDamage,
        StatKey.AttackSpeed,
        StatKey.MoveSpeed,
        StatKey.AttackRange,
        StatKey.AggroRadius
    };
}
