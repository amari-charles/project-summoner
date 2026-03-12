using System.Collections.Generic;
using Godot;

namespace Fateforged.View;

/// <summary>
/// View-layer position interpolation for remote units.
/// Consumes authoritative snapshot positions and outputs render-space smoothing only.
/// </summary>
public class StateInterpolator
{
    /// <summary>
    /// Interpolation speed factor.
    /// Higher = snappier but less smooth.
    /// Lower = smoother but more visual latency.
    /// </summary>
    public float InterpolationSpeed { get; set; } = 15.0f;

    /// <summary>
    /// Maximum distance before snapping instead of interpolating.
    /// Prevents slow drift on teleports or large corrections.
    /// </summary>
    public float SnapThreshold { get; set; } = 5.0f;

    private readonly Dictionary<int, InterpolationTarget> _targets = new();

    /// <summary>
    /// Set the latest authoritative target position for an entity.
    /// </summary>
    public void SetTarget(int networkId, Vector3 targetPosition)
    {
        if (_targets.TryGetValue(networkId, out var existing))
        {
            existing.TargetPosition = targetPosition;
        }
        else
        {
            _targets[networkId] = new InterpolationTarget
            {
                TargetPosition = targetPosition,
                CurrentPosition = targetPosition,
            };
        }
    }

    /// <summary>
    /// Get the current interpolated position for an entity.
    /// Returns null when no target exists yet.
    /// </summary>
    public Vector3? GetPosition(int networkId)
    {
        return _targets.TryGetValue(networkId, out var target) ? target.CurrentPosition : null;
    }

    /// <summary>
    /// Update interpolation for all tracked entities.
    /// </summary>
    public void Update(double delta)
    {
        var dt = (float)delta;

        foreach (var target in _targets.Values)
        {
            var distance = target.CurrentPosition.DistanceTo(target.TargetPosition);

            if (distance > SnapThreshold)
            {
                target.CurrentPosition = target.TargetPosition;
            }
            else if (distance > 0.01f)
            {
                target.CurrentPosition = target.CurrentPosition.Lerp(
                    target.TargetPosition,
                    Mathf.Min(1.0f, InterpolationSpeed * dt)
                );
            }
        }
    }

    /// <summary>
    /// Stop tracking one entity.
    /// </summary>
    public void Remove(int networkId)
    {
        _targets.Remove(networkId);
    }

    /// <summary>
    /// Clear all tracked interpolation state.
    /// </summary>
    public void Clear()
    {
        _targets.Clear();
    }

    private class InterpolationTarget
    {
        public Vector3 TargetPosition;
        public Vector3 CurrentPosition;
    }
}
