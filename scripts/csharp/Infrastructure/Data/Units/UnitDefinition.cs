using System.Collections.Generic;
using Fateforged.Constants;
using Fateforged.Projectiles;
using Fateforged.Stats;
using Godot;

namespace Fateforged.Units;

/// <summary>
/// Complete definition of a unit type - the single source of truth for unit configuration.
/// Scene files define VISUALS. UnitDefinition defines BEHAVIOR.
/// </summary>
public record UnitDefinition
{
    // =========================================================================
    // IDENTITY
    // =========================================================================

    /// <summary>Unique identifier for this unit type.</summary>
    public required UnitId Id { get; init; }

    /// <summary>Display name for UI/debugging.</summary>
    public required string DisplayName { get; init; }

    // =========================================================================
    // CORE CONFIGURATION
    // =========================================================================

    /// <summary>Base combat stats for this unit type.</summary>
    public required UnitStats Stats { get; init; }

    /// <summary>Whether this unit is Melee or Ranged.</summary>
    public required UnitType UnitType { get; init; }

    /// <summary>
    /// Tactical role used by virtual-lane behavior shaping.
    /// Auto uses simulation heuristics.
    /// </summary>
    public TacticalRole TacticalRole { get; init; } = TacticalRole.Auto;

    /// <summary>Whether this unit is Ground or Air.</summary>
    public MovementLayer MovementLayer { get; init; } = MovementLayer.Ground;

    /// <summary>Targeting profile used when building simulation templates.</summary>
    public required UnitTargetingProfile TargetingProfile { get; init; }

    /// <summary>Distance scoring weight for target selection.</summary>
    public float TargetingDistanceScorerWeight { get; init; } = 1f;

    /// <summary>Health scoring weight for target selection.</summary>
    public float TargetingHealthScorerWeight { get; init; } = 10f;

    /// <summary>Optional preference used after normal target filters pass.</summary>
    public UnitTargetPriority TargetPriority { get; init; } = UnitTargetPriority.Default;

    /// <summary>Layer filter for target acquisition.</summary>
    public TargetLayer TargetingLayerFilter { get; init; } = TargetLayer.GroundOnly;

    /// <summary>Cone half-angle used when the profile has cone constraints.</summary>
    public float TargetingConeHalfAngle { get; init; } = 30f;

    /// <summary>
    /// Cone center offset in degrees (XZ plane).
    /// Negative rotates cone center toward -Z, positive toward +Z.
    /// </summary>
    public float TargetingConeCenterOffsetDegrees { get; init; }

    /// <summary>Close-range threshold used for cone edge behavior.</summary>
    public float TargetingCloseRangeThreshold { get; init; } = 0.5f;

    // =========================================================================
    // ATTACK VECTOR CONFIGURATION
    // =========================================================================

    /// <summary>Grouped vector-based attack behavior config.</summary>
    public AttackVectorConfig Attack { get; init; } = AttackVectorConfig.Default;

    /// <summary>Optional simulation-owned ability loadout for this unit.</summary>
    public List<UnitAbilityConfig> Abilities { get; init; } = [];

    /// <summary>Combat tags used by effect requirements and immunity rules.</summary>
    public string[] CombatTags { get; init; } = [];

    // =========================================================================
    // RANGED CONFIGURATION (null = not a ranged unit)
    // =========================================================================

    /// <summary>
    /// Configuration for ranged units (projectile settings).
    /// Null for melee units.
    /// </summary>
    public RangedConfig? Ranged { get; init; }

    // =========================================================================
    // FLYING CONFIGURATION (null = ground unit)
    // =========================================================================

    /// <summary>
    /// Configuration for flying units (altitude, attack style).
    /// Null for ground units.
    /// </summary>
    public FlyingConfig? Flying { get; init; }

    // =========================================================================
    // VISUAL CONFIGURATION
    // =========================================================================

    /// <summary>Visual configuration (separation radius, shadow, hurtbox).</summary>
    public VisualConfig Visual { get; init; } = VisualConfig.Default;

    // =========================================================================
    // DAMAGE PROFILE
    // =========================================================================

    /// <summary>
    /// Defines how this unit's damage is split between physical and elemental.
    /// Defaults to pure physical (100% physical, 0% elemental).
    /// </summary>
    public DamageProfile DamageProfile { get; init; } = DamageProfile.Physical;

    // =========================================================================
    // SCENE REFERENCE
    // =========================================================================

    /// <summary>Path to the unit's visual scene file.</summary>
    public required string ScenePath { get; init; }
}

/// <summary>
/// Configuration for ranged units (projectile spawning).
/// </summary>
public record RangedConfig
{
    /// <summary>ID of the projectile to spawn.</summary>
    public ProjectileId ProjectileId { get; init; }

    /// <summary>Delay before projectile spawns after attack starts (for charge-up).</summary>
    public float ProjectileDelay { get; init; } = 0f;

    /// <summary>Whether this is a delayed/charge-up projectile attack.</summary>
    public bool IsDelayedProjectile { get; init; } = false;

    /// <summary>Estimated projectile speed for intercept calculations.</summary>
    public float ProjectileSpeedEstimate { get; init; } = 15f;

    /// <summary>Optional impact behavior override (heal/status payloads, ally hits, etc.).</summary>
    public ProjectileImpactConfig Impact { get; init; } = ProjectileImpactConfig.DamageDefault;

    public RangedConfig(ProjectileId projectileId)
    {
        ProjectileId = projectileId;
    }
}

/// <summary>
/// Configuration for flying units (altitude, attack style).
/// </summary>
public record FlyingConfig
{
    /// <summary>Y position for flying units.</summary>
    public float Altitude { get; init; } = 2.5f;

    /// <summary>How the unit behaves when attacking.</summary>
    public FlyingAttackStyle AttackStyle { get; init; } = FlyingAttackStyle.Hover;

    /// <summary>Whether the unit can return to air after landing.</summary>
    public bool CanReturnToAir { get; init; } = true;

    /// <summary>Delay before returning to air.</summary>
    public float ReturnToAirDelay { get; init; } = 1.0f;

    /// <summary>Death behavior for flying units.</summary>
    public FlyingDeathStyle DeathStyle { get; init; } = FlyingDeathStyle.Fall;
}

/// <summary>
/// Visual configuration (collision, shadows, hurtbox).
/// </summary>
public record VisualConfig
{
    /// <summary>Separation radius for collision avoidance.</summary>
    public float SeparationRadius { get; init; } = 0.5f;

    /// <summary>Custom hurtbox configuration (null = use defaults).</summary>
    public HurtboxConfig? Hurtbox { get; init; }

    /// <summary>HP bar Y offset override. 0 = auto-calculate.</summary>
    public float HpBarOffsetY { get; init; } = 0f;

    /// <summary>Visual-only scale multiplier for shared placeholder scenes.</summary>
    public float DisplayScale { get; init; } = 1f;

    /// <summary>Offset from center mass for projectile targeting.</summary>
    public Vector3 TargetPointOffset { get; init; } = Vector3.Zero;

    public static VisualConfig Default => new();
}

/// <summary>
/// Custom hurtbox shape configuration.
/// </summary>
public record HurtboxConfig
{
    /// <summary>Whether the hurtbox capsule is horizontal (for wide, flat units).</summary>
    public bool Horizontal { get; init; } = false;

    /// <summary>Hurtbox height (or width when horizontal). 0 = auto.</summary>
    public float Height { get; init; } = 0f;

    /// <summary>Hurtbox radius. 0 = default (0.5).</summary>
    public float Radius { get; init; } = 0f;

    /// <summary>Offset for hurtbox position.</summary>
    public Vector3 Offset { get; init; } = Vector3.Zero;
}
