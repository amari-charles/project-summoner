using System.Collections.Generic;
using Fateforged.Projectiles;
using Fateforged.Vfx;

namespace Fateforged.Data.Projectiles;

/// <summary>
/// Central registry of all projectile definitions.
/// Use ProjectileDefinitions.Get() to retrieve projectiles by ID.
/// </summary>
public static class ProjectileDefinitions
{
    // =========================================================================
    // PROJECTILE DEFINITIONS
    // =========================================================================

    /// <summary>Standard arrow projectile with arc trajectory.</summary>
    public static readonly ProjectileData Arrow = new()
    {
        ProjectileId = ProjectileIds.Arrow,
        ProjectileName = "Arrow",
        ModelScenePath = "res://scenes/battle/projectiles/arrow_visual.tscn",
        MovementType = ProjectileMovementType.Arc,
        Speed = 15.0f,
        Lifetime = 3.0f,
        RotateToDirection = true,
        ArcHeight = 1.5f,
        Gravity = -9.8f,
    };

    /// <summary>Small fire projectile from fire wisps.</summary>
    public static readonly ProjectileData Ember = new()
    {
        ProjectileId = ProjectileIds.Ember,
        ProjectileName = "Ember",
        ModelScenePath = "res://scenes/battle/projectiles/ember_visual.tscn",
        MovementType = ProjectileMovementType.Straight,
        Speed = 12.0f,
        Lifetime = 2.5f,
        RotateToDirection = false,
    };

    /// <summary>Large fire spell projectile with AOE impact.</summary>
    public static readonly ProjectileData Fireball = new()
    {
        ProjectileId = ProjectileIds.Fireball,
        ProjectileName = "Fireball",
        ModelScenePath = "res://scenes/battle/projectiles/fireball_visual.tscn",
        HitVfx = VfxIds.FireballSpell,
        MovementType = ProjectileMovementType.Arc,
        Speed = 22.0f,
        Lifetime = 4.0f,
        RotateToDirection = false,
        ArcHeight = 0.5f,
        AoeRadius = 8.0f,
    };

    /// <summary>Arcane bolt that weaves briefly before locking onto its target.</summary>
    public static readonly ProjectileData ManaBolt = new()
    {
        ProjectileId = ProjectileIds.ManaBolt,
        ProjectileName = "Mana Bolt",
        ModelScenePath = "res://scenes/battle/projectiles/mana_bolt_visual.tscn",
        MovementType = ProjectileMovementType.WeavingHoming,
        SpeedStart = 18.0f,
        SpeedEnd = 42.0f,
        SpeedTransitionDuration = 0.85f,
        SpeedEasing = SpeedEasingType.EaseInOut,
        SpeedEaseExponent = 2.35f,
        VeerDelay = 0.02f,
        VeerAngle = 58f,
        VeerDuration = 0.34f,
        SteerStrength = 330f,
        ArcHeight = 3.0f,
        Lifetime = 6.0f,
        HitRadius = 0.75f,
        RotateToDirection = true,
        SpawnAtTargetHeight = false,
    };

    /// <summary>Fast straight projectile for wind units.</summary>
    public static readonly ProjectileData WindPuff = new()
    {
        ProjectileId = ProjectileIds.WindPuff,
        ProjectileName = "Wind Puff",
        ModelScenePath = "res://scenes/battle/projectiles/wind_puff_visual.tscn",
        MovementType = ProjectileMovementType.Straight,
        Tracking = true,
        Speed = 18.0f,
        MinSpeed = 18.0f,
        Lifetime = 2.0f,
        // Keep contact radius tight so puffs don't register implausible long-range hits.
        HitRadius = 0.45f,
        RotateToDirection = false,
        FadeInDuration = 0.15f,
        FadeOnHit = true,
        FadeDuration = 0.1f,
    };

    /// <summary>Fire spider web attack projectile.</summary>
    public static readonly ProjectileData FireWeb = new()
    {
        ProjectileId = ProjectileIds.FireWeb,
        ProjectileName = "Fire Web",
        ModelScenePath = "res://scenes/battle/projectiles/fire_web_visual.tscn",
        MovementType = ProjectileMovementType.Straight,
        Speed = 16.0f,
        Lifetime = 3.0f,
        RotateToDirection = false,
    };

    /// <summary>Lobbed rock projectile with tracking arc.</summary>
    public static readonly ProjectileData Rock = new()
    {
        ProjectileId = ProjectileIds.Rock,
        ProjectileName = "Rock",
        ModelScenePath = "res://scenes/battle/projectiles/rock_visual.tscn",
        MovementType = ProjectileMovementType.Arc,
        Tracking = true,
        Speed = 10.0f,
        Lifetime = 4.0f,
        RotateToDirection = true,
        ArcHeight = 4.0f,
        Gravity = -12.0f,
    };

    /// <summary>Weaving homing bolt with serpentine motion (Cult of the Lamb style).</summary>
    public static readonly ProjectileData WeavingBolt = new()
    {
        ProjectileId = ProjectileIds.WeavingBolt,
        ProjectileName = "Weaving Bolt",
        ModelScenePath = "res://scenes/battle/projectiles/mana_bolt_visual.tscn",
        MovementType = ProjectileMovementType.WeavingHoming,
        SpeedStart = 28.0f,
        SpeedEnd = 60.0f,
        SpeedTransitionDuration = 1.0f,
        SpeedEasing = SpeedEasingType.EaseIn,
        SpeedEaseExponent = 2.5f,
        VeerDelay = 0.3f,
        VeerAngle = 55f,
        VeerDuration = 0.5f,
        SteerStrength = 360f,
        ArcHeight = 0.8f,
        Lifetime = 4.0f,
        RotateToDirection = true,
        SpawnAtTargetHeight = true,
    };

    // =========================================================================
    // LOOKUP
    // =========================================================================

    private static readonly Dictionary<string, ProjectileData> _lookup = new()
    {
        [ProjectileIds.Arrow] = Arrow,
        [ProjectileIds.Ember] = Ember,
        [ProjectileIds.Fireball] = Fireball,
        [ProjectileIds.ManaBolt] = ManaBolt,
        [ProjectileIds.WindPuff] = WindPuff,
        [ProjectileIds.FireWeb] = FireWeb,
        [ProjectileIds.Rock] = Rock,
        [ProjectileIds.WeavingBolt] = WeavingBolt,
    };

    /// <summary>Get a projectile definition by ID. Returns null if not found.</summary>
    public static ProjectileData? Get(ProjectileId id) => _lookup.GetValueOrDefault(id);

    /// <summary>Get a projectile definition by string ID. Returns null if not found.</summary>
    public static ProjectileData? Get(string id) => _lookup.GetValueOrDefault(id);

    /// <summary>Check if a projectile exists.</summary>
    public static bool Has(ProjectileId id) => _lookup.ContainsKey(id);

    /// <summary>Check if a projectile exists by string ID.</summary>
    public static bool Has(string id) => _lookup.ContainsKey(id);

    /// <summary>Get all projectile definitions.</summary>
    public static IEnumerable<ProjectileData> All => _lookup.Values;

    /// <summary>Get all projectile IDs.</summary>
    public static IEnumerable<string> AllIds => _lookup.Keys;

    /// <summary>Get projectile count.</summary>
    public static int Count => _lookup.Count;
}
