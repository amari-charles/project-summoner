using System;
using Fateforged.Constants;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Geometry;
using Fateforged.Units;

namespace Fateforged.Simulation.Movement;

/// <summary>
/// Lightweight post-movement overlap correction.
/// Pushes apart units that still overlap after ORCA velocity resolution.
/// Same-layer only, mass-proportional, skips mutual target pairs.
/// </summary>
public static class OverlapCorrection
{
    private const float DefaultCorrectionStrength = 0.30f;

    /// <summary>
    /// Correct remaining overlaps for a single unit against all others.
    /// This is a safety net — ORCA should prevent most overlaps, but
    /// edge cases (spawning, teleportation, simultaneous movement) can cause them.
    /// </summary>
    public static void Correct(UnitData unit, MatchState state)
    {
        foreach (var kvp in state.Units)
        {
            var other = kvp.Value;
            if (other.UnitId == unit.UnitId)
                continue;
            if (!other.IsAlive)
                continue;
            if (other.ActivationState != ActivationState.Active)
                continue;
            if (other.MovementLayer != unit.MovementLayer)
                continue;

            // Skip current target — prevents infinite chase -> overlap -> push loops
            if (unit.Engagement.TargetUnitId.HasValue && other.UnitId == unit.Engagement.TargetUnitId.Value)
                continue;
            if (other.Engagement.TargetUnitId.HasValue && other.Engagement.TargetUnitId.Value == unit.UnitId)
                continue;
            if (MeleeClumpContext.IsSameTargetCloseMeleePair(unit, other, state))
                continue;

            float unitRadius = CombatGeometry.GetNavigationRadius(unit);
            float otherRadius = CombatGeometry.GetNavigationRadius(other);
            float minDist = unitRadius + otherRadius;
            var diff = unit.Position - other.Position;
            diff.Y = 0;
            float distSq = diff.LengthSquared();

            if (distSq >= minDist * minDist || distSq < 0.000001f)
                continue;

            float dist = MathF.Sqrt(distSq);
            float overlap = minDist - dist;
            var pushDir = diff / dist;

            // Push proportional to relative mass (navigationRadius^3)
            float unitMass = unitRadius * unitRadius * unitRadius;
            float otherMass = otherRadius * otherRadius * otherRadius;
            float pushRatio = otherMass / (unitMass + otherMass);

            var newPos = unit.Position + pushDir * overlap * pushRatio * DefaultCorrectionStrength;
            newPos = BattlefieldBounds.ClampToBounds(newPos);
            unit.Position = new SimVector3(newPos.X, unit.Position.Y, newPos.Z);
        }
    }
}
