using Godot;

namespace ProjectSummoner.Projectiles.Paths;

/// <summary>
/// Quadratic Bézier arc path from start to end with configurable height.
/// Used for arcing projectiles like mana bolts.
/// Supports dynamic endpoint updates for tracking moving targets.
/// </summary>
public class ArcPath : IProjectilePath
{
    private readonly Vector3 _start;
    private Vector3 _end;
    private Vector3 _control;
    private readonly float _arcHeight;
    private float _cachedLength;

    /// <summary>
    /// Minimum distance to prevent degenerate arcs.
    /// </summary>
    private const float MinDistance = 0.1f;

    /// <summary>
    /// Distance at which full arc height is used.
    /// Shorter distances scale down the arc proportionally.
    /// </summary>
    private const float FullArcDistance = 5.0f;

    public ArcPath(Vector3 start, Vector3 end, float arcHeight)
    {
        _start = start;
        _end = end;
        _arcHeight = arcHeight;
        RecalculateControlPoint();
        _cachedLength = EstimateLength();
    }

    public Vector3 GetPosition(float progress)
    {
        float t = Mathf.Clamp(progress, 0f, 1f);

        // Quadratic Bézier: B(t) = (1-t)²P0 + 2(1-t)tP1 + t²P2
        float u = 1f - t;
        return (u * u * _start) + (2f * u * t * _control) + (t * t * _end);
    }

    public void UpdateTarget(Vector3 newTarget)
    {
        _end = newTarget;
        RecalculateControlPoint();
        _cachedLength = EstimateLength();
    }

    public float GetLength()
    {
        return _cachedLength;
    }

    public Vector3 GetDirection(float progress)
    {
        float t = Mathf.Clamp(progress, 0f, 1f);

        // Derivative of quadratic Bézier: B'(t) = 2(1-t)(P1-P0) + 2t(P2-P1)
        float u = 1f - t;
        Vector3 tangent = (2f * u * (_control - _start)) + (2f * t * (_end - _control));

        return tangent.LengthSquared() > 0.001f ? tangent.Normalized() : Vector3.Forward;
    }

    /// <summary>
    /// Recalculate control point when endpoint changes.
    /// Control point is at the midpoint, raised by scaled arc height.
    /// </summary>
    private void RecalculateControlPoint()
    {
        float distance = _start.DistanceTo(_end);

        // Scale arc height based on distance
        float arcScale = Mathf.Clamp(distance / FullArcDistance, 0f, 1f);
        float effectiveArcHeight = _arcHeight * arcScale;

        // Control point at midpoint, raised by arc height
        _control = (_start + _end) / 2f + Vector3.Up * effectiveArcHeight;
    }

    /// <summary>
    /// Estimate path length using line segments.
    /// Uses 8 segments for reasonable accuracy without excessive computation.
    /// </summary>
    private float EstimateLength()
    {
        const int segments = 8;
        float length = 0f;
        Vector3 prev = GetPosition(0f);

        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 current = GetPosition(t);
            length += prev.DistanceTo(current);
            prev = current;
        }

        return Mathf.Max(length, MinDistance);
    }
}
