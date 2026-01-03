using Godot;

namespace ProjectSummoner.Capabilities;

/// <summary>
/// Interface for units that can attack at range with projectiles.
/// Implement this for archers, mages, or any unit with ranged attacks.
/// </summary>
public interface IRangedAttacker
{
    /// <summary>ID of the projectile to spawn (from ProjectileManager).</summary>
    string ProjectileId { get; }

    /// <summary>Delay before spawning projectile (for charge-up attacks).</summary>
    float ProjectileDelay { get; }

    /// <summary>Whether this unit uses delayed projectile spawning.</summary>
    bool IsDelayedProjectile { get; }

    /// <summary>
    /// Spawn a projectile toward the target.
    /// </summary>
    /// <param name="target">The target to attack.</param>
    void SpawnProjectile(Node3D target);

    /// <summary>
    /// Get the position where projectiles should spawn from.
    /// </summary>
    /// <returns>World position for projectile spawn.</returns>
    Vector3 GetProjectileSpawnPosition();
}
