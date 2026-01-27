using Godot;

namespace ProjectSummoner.Projectiles.Paths;

/// <summary>
/// Pre-computed parabolic arc path for ballistic projectiles.
/// Simulates gravity-based trajectory from start to end.
/// Does NOT support dynamic target updates (trajectory is fixed at creation).
/// </summary>
public class BallisticPath : IProjectilePath
{
    private readonly Vector3 _start;
    private readonly Vector3 _end;
    private readonly float _totalTime;
    private readonly Vector3 _horizontalVelocity;
    private readonly float _initialVerticalVelocity;
    private readonly float _gravity;
    private readonly float _cachedLength;

    /// <summary>
    /// Default gravity constant.
    /// </summary>
    private const float DefaultGravity = 9.8f;

    /// <summary>
    /// Minimum time to prevent division by zero.
    /// </summary>
    private const float MinTime = 0.01f;

    public BallisticPath(Vector3 start, Vector3 end, float speed, float gravity = DefaultGravity)
    {
        _start = start;
        _end = end;
        _gravity = gravity;

        // Calculate horizontal distance and direction
        var displacement = end - start;
        float horizontalDist = new Vector2(displacement.X, displacement.Z).Length();
        float verticalDist = displacement.Y;

        // Time to reach target at given horizontal speed
        _totalTime = Mathf.Max(horizontalDist / speed, MinTime);

        // Calculate initial vertical velocity to land at target
        // Using: y = v0*t - 0.5*g*t² => v0 = (y + 0.5*g*t²) / t
        _initialVerticalVelocity = (verticalDist + 0.5f * _gravity * _totalTime * _totalTime) / _totalTime;

        // Horizontal velocity (constant throughout flight)
        var horizontalDir = new Vector3(displacement.X, 0, displacement.Z).Normalized();
        _horizontalVelocity = horizontalDir * speed;

        _cachedLength = EstimateLength();
    }

    public Vector3 GetPosition(float progress)
    {
        float t = Mathf.Clamp(progress, 0f, 1f);
        float time = t * _totalTime;

        // Horizontal position: start + velocity * time
        Vector3 pos = _start + _horizontalVelocity * time;

        // Vertical position: y0 + v0*t - 0.5*g*t²
        pos.Y = _start.Y + _initialVerticalVelocity * time - 0.5f * _gravity * time * time;

        return pos;
    }

    public void UpdateTarget(Vector3 newTarget)
    {
        // Ballistic paths do not support dynamic target updates
        // The trajectory is pre-computed and fixed
        GD.PushWarning("BallisticPath: UpdateTarget called but ballistic trajectories are fixed at creation.");
    }

    public float GetLength()
    {
        return _cachedLength;
    }

    public Vector3 GetDirection(float progress)
    {
        float t = Mathf.Clamp(progress, 0f, 1f);
        float time = t * _totalTime;

        // Velocity at time t: v = v0 - g*t (vertical) + horizontal (constant)
        Vector3 velocity = _horizontalVelocity;
        velocity.Y = _initialVerticalVelocity - _gravity * time;

        return velocity.LengthSquared() > 0.001f ? velocity.Normalized() : Vector3.Forward;
    }

    /// <summary>
    /// Estimate path length using line segments.
    /// </summary>
    private float EstimateLength()
    {
        const int segments = 16; // More segments for curved ballistic arc
        float length = 0f;
        Vector3 prev = GetPosition(0f);

        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 current = GetPosition(t);
            length += prev.DistanceTo(current);
            prev = current;
        }

        return Mathf.Max(length, 0.1f);
    }
}
