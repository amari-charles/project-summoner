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
