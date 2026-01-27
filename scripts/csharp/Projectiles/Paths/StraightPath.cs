using Godot;

namespace ProjectSummoner.Projectiles.Paths;

/// <summary>
/// Linear path from start to end.
/// Used for straight-line projectiles.
/// </summary>
public class StraightPath : IProjectilePath
{
    private readonly Vector3 _start;
    private Vector3 _end;
    private float _cachedLength;

    public StraightPath(Vector3 start, Vector3 end)
    {
        _start = start;
        _end = end;
        _cachedLength = start.DistanceTo(end);
    }

    public Vector3 GetPosition(float progress)
    {
        return _start.Lerp(_end, Mathf.Clamp(progress, 0f, 1f));
    }

    public void UpdateTarget(Vector3 newTarget)
    {
        _end = newTarget;
        _cachedLength = _start.DistanceTo(_end);
    }

    public float GetLength()
    {
        return _cachedLength;
    }

    public Vector3 GetDirection(float progress)
    {
        var dir = (_end - _start).Normalized();
        return dir.LengthSquared() > 0.001f ? dir : Vector3.Forward;
    }
}
