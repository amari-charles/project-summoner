using System.Collections.Generic;
using Godot;

namespace Fateforged.Multiplayer.Client;

/// <summary>
/// Interpolates entity positions between server snapshots for smooth visuals.
/// </summary>
public class StateInterpolator
{
    /// <summary>
    /// Interpolation speed factor.
    /// Higher = snappier but less smooth.
    /// Lower = smoother but may lag behind.
    /// </summary>
    public float InterpolationSpeed { get; set; } = 15.0f;

    /// <summary>
    /// Maximum distance before snapping instead of interpolating.
    /// Prevents weird slow movement on teleports.
    /// </summary>
    public float SnapThreshold { get; set; } = 5.0f;

    private readonly Dictionary<int, InterpolationTarget> _targets = new();

    /// <summary>
    /// Set the target position for an entity.
    /// Called when a state snapshot is received.
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
                CurrentPosition = targetPosition // Start at target
            };
        }
    }

    /// <summary>
    /// Get the interpolated position for an entity.
    /// </summary>
    public Vector3? GetPosition(int networkId)
    {
        return _targets.TryGetValue(networkId, out var target) ? target.CurrentPosition : null;
    }

    /// <summary>
    /// Update interpolation for all entities.
    /// </summary>
    public void Update(double delta)
    {
        var dt = (float)delta;

        foreach (var target in _targets.Values)
        {
            var distance = target.CurrentPosition.DistanceTo(target.TargetPosition);

            if (distance > SnapThreshold)
            {
                // Snap if too far
                target.CurrentPosition = target.TargetPosition;
            }
            else if (distance > 0.01f)
            {
                // Interpolate toward target
                target.CurrentPosition = target.CurrentPosition.Lerp(
                    target.TargetPosition,
                    Mathf.Min(1.0f, InterpolationSpeed * dt)
                );
            }
        }
    }

    /// <summary>
    /// Remove an entity from interpolation (e.g., on death).
    /// </summary>
    public void Remove(int networkId)
    {
        _targets.Remove(networkId);
    }

    /// <summary>
    /// Clear all interpolation targets.
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
