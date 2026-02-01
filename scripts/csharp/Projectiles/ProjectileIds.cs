namespace ProjectSummoner.Projectiles;

/// <summary>
/// All known projectile type IDs. Use these instead of raw strings.
/// Example: ProjectileIds.FireWeb instead of "fire_web"
/// </summary>
public static class ProjectileIds
{
    // =========================================================================
    // GENERIC PROJECTILES
    // =========================================================================

    public static readonly ProjectileId Arrow = new("arrow");
    public static readonly ProjectileId Ember = new("ember");
    public static readonly ProjectileId Fireball = new("fireball");
    public static readonly ProjectileId ManaBolt = new("mana_bolt");

    // =========================================================================
    // UNIT-SPECIFIC PROJECTILES
    // =========================================================================

    /// <summary>Fire Spider's web projectile - applies slow on hit.</summary>
    public static readonly ProjectileId FireWeb = new("fire_web");

    /// <summary>Rock Thrower's rock projectile - high damage, slow travel.</summary>
    public static readonly ProjectileId Rock = new("rock");

    /// <summary>Puff/Duckling's wind projectile.</summary>
    public static readonly ProjectileId WindPuff = new("wind_puff");
}
