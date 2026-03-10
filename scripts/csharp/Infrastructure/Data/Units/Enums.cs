namespace Fateforged.Units;

/// <summary>
/// Team affiliation for units.
/// </summary>
public enum Team
{
    Player = 0,
    Enemy = 1
}

/// <summary>
/// Type of unit determining attack behavior.
/// </summary>
public enum UnitType
{
    Melee,
    Ranged
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
    Backliner = 3
}

/// <summary>
/// Movement layer for targeting and collision.
/// </summary>
public enum MovementLayer
{
    Ground,
    Air
}

/// <summary>
/// Which movement layers this unit can target.
/// </summary>
public enum TargetLayer
{
    GroundOnly,
    AirOnly,
    Both
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
    FlyingConeStrafe = 4
}

/// <summary>
/// Activation state for spawn reveal system.
/// </summary>
public enum ActivationState
{
    Inactive,
    Active
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
    Adaptive
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
    Explode
}
