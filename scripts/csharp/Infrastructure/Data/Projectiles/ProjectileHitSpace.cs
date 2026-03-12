namespace Fateforged.Projectiles;

/// <summary>
/// Gameplay hit-space used for projectile overlap checks.
/// </summary>
public enum ProjectileHitSpace
{
    /// <summary>
    /// Ground units use XZ circle checks; air units fall back to 3D sphere checks.
    /// </summary>
    GroundCylinder = 0,

    /// <summary>
    /// Use full 3D sphere checks for all targets.
    /// </summary>
    Sphere3D = 1,
}
