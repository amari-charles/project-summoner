namespace Fateforged.Units;

/// <summary>
/// Team affiliation for units.
/// </summary>
public enum Team
{
    Player = 0,
    Enemy = 1,
}

/// <summary>
/// Type of unit determining attack behavior.
/// </summary>
public enum UnitType
{
    Melee,
    Ranged,
}

/// <summary>
/// Authoring-friendly preset for vector-based melee attack behavior.
/// Presets map to concrete vector defaults in UnitDefinitions.BuildSimTemplate.
/// </summary>
public enum AttackPreset
{
    SingleTarget = 0,
    LegacySingleTarget = SingleTarget,
    AreaCleave = 1,
    LinePierce = 2,
    Chain = 3,
    Custom = 99,
}

/// <summary>
/// How attack recipients are selected after choosing the primary target.
/// </summary>
public enum AttackSelectionMode
{
    Single = 0,
    AreaCollect = 1,
    LineCollect = 2,
    ChainHops = 3,
}

/// <summary>
/// Attack area shape used by area/line recipient selection.
/// </summary>
public enum AttackAreaShape
{
    Sphere = 0,
    Box = 1,
    Capsule = 2,
    Line = 3,
}

/// <summary>
/// How attack effects propagate beyond the primary target.
/// </summary>
public enum AttackPropagationMode
{
    None = 0,
    Pierce = 1,
    Chain = 2,
}

/// <summary>
/// Delivery mode for attack resolution.
/// </summary>
public enum AttackDeliveryMode
{
    Instant = 0,
    Projectile = 1,
    PersistentZone = 2,
}

/// <summary>
/// Trigger policy when multiple recipients are hit.
/// </summary>
public enum AttackTriggerMode
{
    PrimaryOnly = 0,
    EveryRecipient = 1,
}

/// <summary>
/// Team affinity used by ability targeting and projectile impact filters.
/// </summary>
public enum AbilityTargetAffinity
{
    Enemies = 0,
    Allies = 1,
    Both = 2,
}

/// <summary>
/// High-level projectile impact behavior for unit-driven attacks/abilities.
/// </summary>
public enum ProjectileImpactKind
{
    Damage = 0,
    Heal = 1,
}

/// <summary>
/// Status kinds that can be applied by ability/projectile payloads.
/// </summary>
public enum StatusEffectKind
{
    None = 0,
    Poison = 1,
    Burn = 2,
    Taunt = 3,
}

/// <summary>
/// Ability runtime behavior kind.
/// </summary>
public enum UnitAbilityKind
{
    HealerProjectile = 0,
    TauntPulse = 1,
    CleansePulse = 2,
    ApplySelfEffect = 3,
    TargetedKnockback = 4,
}

/// <summary>
/// High-level tactical role used by simulation behavior shaping.
/// </summary>
public enum TacticalRole
{
    /// <summary>
    /// Resolve role from unit definition heuristics.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Default line-holder role.
    /// </summary>
    Frontliner = 1,

    /// <summary>
    /// Side-pressure role with stronger lane adherence.
    /// </summary>
    Flanker = 2,

    /// <summary>
    /// Safer-range role with reduced cross-lane chase.
    /// </summary>
    Backliner = 3,
}

/// <summary>
/// Movement layer for targeting and collision.
/// </summary>
public enum MovementLayer
{
    Ground,
    Air,
}

/// <summary>
/// Which movement layers this unit can target.
/// </summary>
public enum TargetLayer
{
    GroundOnly,
    AirOnly,
    Both,
}

/// <summary>
/// High-level targeting profile used to build simulation targeting fields.
/// </summary>
public enum UnitTargetingProfile
{
    /// <summary>
    /// Passive/non-combat unit. No chasing or attacking behavior.
    /// </summary>
    Passive = 0,

    /// <summary>
    /// Ground melee profile: move toward enemies, prioritize ground.
    /// </summary>
    MeleeGround = 1,

    /// <summary>
    /// Ground ranged profile: move toward enemies when out of range.
    /// </summary>
    RangedGround = 2,

    /// <summary>
    /// Ranged strafe profile: uses strafe fallback without cone gating.
    /// </summary>
    RangedStrafe = 3,

    /// <summary>
    /// Flying cone profile: strafe fallback with cone attack constraint.
    /// </summary>
    FlyingConeStrafe = 4,
}

/// <summary>
/// Activation state for spawn reveal system.
/// </summary>
public enum ActivationState
{
    Inactive,
    Active,
}

/// <summary>
/// Attack behavior for flying units when engaging targets.
/// </summary>
public enum FlyingAttackStyle
{
    /// <summary>Stay at altitude, attack from above.</summary>
    Hover,

    /// <summary>Descend when entering combat, stay grounded.</summary>
    LandOnEngage,

    /// <summary>Dive to attack, return to altitude after.</summary>
    Swoop,

    /// <summary>Switch between air/ground based on target type.</summary>
    Adaptive,
}

/// <summary>
/// Death behavior for flying units.
/// </summary>
public enum FlyingDeathStyle
{
    /// <summary>Body falls with gravity, shadow tracks body.</summary>
    Fall,

    /// <summary>Fade out at altitude.</summary>
    Fade,

    /// <summary>Explosion VFX at altitude.</summary>
    Explode,
}
