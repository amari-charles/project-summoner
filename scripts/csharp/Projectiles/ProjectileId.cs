namespace ProjectSummoner.Projectiles;

/// <summary>
/// Strongly-typed identifier for projectile types.
/// Prevents typos and enables IDE autocomplete via ProjectileIds static class.
/// </summary>
public readonly record struct ProjectileId(string Value)
{
    /// <summary>Returns the underlying string value.</summary>
    public override string ToString() => Value;

    /// <summary>Implicit conversion to string for interop with existing systems.</summary>
    public static implicit operator string(ProjectileId id) => id.Value;

    /// <summary>Check if this ID has a value (not empty).</summary>
    public bool HasValue => !string.IsNullOrEmpty(Value);

    /// <summary>Empty/unset projectile ID.</summary>
    public static readonly ProjectileId None = new("");
}

/// <summary>
/// All known projectile IDs. Use these instead of raw strings.
/// Example: ProjectileIds.Fireball instead of "fireball"
/// </summary>
public static class ProjectileIds
{
    // =========================================================================
    // BASIC PROJECTILES
    // =========================================================================

    /// <summary>Standard arrow for ranged units.</summary>
    public static readonly ProjectileId Arrow = new("arrow");

    /// <summary>Fire elemental projectile from fire wisps.</summary>
    public static readonly ProjectileId Ember = new("ember");

    /// <summary>Fire spell projectile with AOE impact.</summary>
    public static readonly ProjectileId Fireball = new("fireball");

    /// <summary>Homing magical bolt.</summary>
    public static readonly ProjectileId ManaBolt = new("mana_bolt");

    /// <summary>Fast straight projectile for wind puff units.</summary>
    public static readonly ProjectileId WindPuff = new("wind_puff");

    /// <summary>Fire spider web attack.</summary>
    public static readonly ProjectileId FireWeb = new("fire_web");

    /// <summary>Lobbed rock projectile for rock throwers.</summary>
    public static readonly ProjectileId Rock = new("rock");

    /// <summary>Weaving homing bolt with serpentine motion.</summary>
    public static readonly ProjectileId WeavingBolt = new("weaving_bolt");
}
