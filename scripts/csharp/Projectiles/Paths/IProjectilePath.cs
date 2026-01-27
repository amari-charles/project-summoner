using Godot;

namespace ProjectSummoner.Projectiles.Paths;

/// <summary>
/// Interface for projectile path strategies.
/// Paths compute positions along a curve from start (progress=0) to end (progress=1).
/// Supports dynamic endpoint updates for tracking moving targets.
/// </summary>
public interface IProjectilePath
{
    /// <summary>
    /// Get position along the path.
    /// </summary>
    /// <param name="progress">Progress from 0 (start) to 1 (end).</param>
    /// <returns>World position at this progress point.</returns>
    Vector3 GetPosition(float progress);

    /// <summary>
    /// Update the endpoint to track a moving target.
    /// </summary>
    /// <param name="newTarget">New target position.</param>
    void UpdateTarget(Vector3 newTarget);

    /// <summary>
    /// Get approximate total path length for speed calculations.
    /// </summary>
    float GetLength();

    /// <summary>
    /// Get the direction of travel at a given progress point.
    /// Used for projectile rotation/orientation.
    /// </summary>
    /// <param name="progress">Progress from 0 to 1.</param>
    /// <returns>Normalized direction vector.</returns>
    Vector3 GetDirection(float progress);
}
