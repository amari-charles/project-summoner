namespace ProjectSummoner.Projectiles;

/// <summary>
/// Defines how a projectile moves through space.
/// </summary>
public enum ProjectileMovementType
{
    /// <summary>
    /// Travels in a straight line at constant direction.
    /// </summary>
    Straight,

    /// <summary>
    /// Tracks and turns toward target.
    /// </summary>
    Homing,

    /// <summary>
    /// Follows a parabolic arc based on arc_height.
    /// </summary>
    Arc,

    /// <summary>
    /// Physics-based trajectory with gravity.
    /// </summary>
    Ballistic,

    /// <summary>
    /// Homing arc with sinusoidal weaving motion perpendicular to travel.
    /// Creates dynamic, serpentine visual effect (Cult of the Lamb style).
    /// </summary>
    WeavingHoming
}
