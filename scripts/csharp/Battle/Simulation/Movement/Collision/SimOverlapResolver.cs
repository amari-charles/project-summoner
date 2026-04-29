using System;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Geometry;
using Fateforged.Units;

namespace Fateforged.Simulation.Movement;

/// <summary>
/// Deep-overlap correction ownership for target-commit combat.
/// Allows light overlap and only applies correction for significant penetration.
/// </summary>
public static class SimOverlapResolver
{
    private const float DeepPenetrationRatio = 0.30f;

    public static void Correct(UnitData unit, MatchState state)
    {
        if (!HasDeepOverlap(unit, state))
            return;

        OverlapCorrection.Correct(unit, state);
    }

    private static bool HasDeepOverlap(UnitData unit, MatchState state)
    {
        float unitRadius = CombatGeometry.GetNavigationRadius(unit);

        foreach (var other in state.Units.Values)
        {
            if (other.UnitId == unit.UnitId)
                continue;
            if (!other.IsAlive)
                continue;
            if (other.ActivationState != ActivationState.Active)
                continue;
            if (other.MovementLayer != unit.MovementLayer)
                continue;
            if (MeleeClumpContext.IsSameTargetCloseMeleePair(unit, other, state))
                continue;

            float otherRadius = CombatGeometry.GetNavigationRadius(other);
            float minDist = unitRadius + otherRadius;

            var diff = unit.Position - other.Position;
            diff.Y = 0f;
            float distSq = diff.LengthSquared();
            if (distSq <= 0.000001f)
                return true;

            float minDistSq = minDist * minDist;
            if (distSq >= minDistSq)
                continue;

            float dist = MathF.Sqrt(distSq);
            float penetration = minDist - dist;
            if (penetration >= (minDist * DeepPenetrationRatio))
                return true;
        }

        return false;
    }
}
