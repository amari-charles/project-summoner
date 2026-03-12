using System;
using System.Collections.Generic;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Units;

namespace Fateforged.Simulation.Movement;

/// <summary>
/// Shared neighbor query utility for movement subsystems.
/// Collects nearest valid neighbors within a radius and sorts by distance
/// (then UnitId) for deterministic iteration.
/// </summary>
public static class MovementNeighborQuery
{
    private const float DistanceTieEpsilon = 0.0001f;

    public static void FillNearestNeighbors(
        UnitData unit,
        MatchState state,
        float radius,
        int maxNeighbors,
        List<UnitData> neighbors,
        List<float> neighborDistancesSq,
        bool sortByDistance = true,
        bool includeBoundary = true
    )
    {
        if (neighbors is null)
            throw new ArgumentNullException(nameof(neighbors));
        if (neighborDistancesSq is null)
            throw new ArgumentNullException(nameof(neighborDistancesSq));
        if (maxNeighbors <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(maxNeighbors),
                "maxNeighbors must be positive."
            );

        neighbors.Clear();
        neighborDistancesSq.Clear();

        if (radius <= 0f)
            return;

        float radiusSq = radius * radius;
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

            var diff = other.Position - unit.Position;
            diff.Y = 0f;
            float distSq = diff.LengthSquared();
            if (includeBoundary)
            {
                if (distSq > radiusSq)
                    continue;
            }
            else
            {
                if (distSq >= radiusSq)
                    continue;
            }

            if (neighbors.Count < maxNeighbors)
            {
                neighbors.Add(other);
                neighborDistancesSq.Add(distSq);
                continue;
            }

            int farthestIndex = 0;
            float farthestDistSq = neighborDistancesSq[0];
            int farthestUnitId = neighbors[0].UnitId;
            for (int i = 1; i < neighbors.Count; i++)
            {
                float candidateDistSq = neighborDistancesSq[i];
                int candidateUnitId = neighbors[i].UnitId;
                if (IsFurther(candidateDistSq, candidateUnitId, farthestDistSq, farthestUnitId))
                {
                    farthestDistSq = candidateDistSq;
                    farthestUnitId = candidateUnitId;
                    farthestIndex = i;
                }
            }

            if (!IsCloser(distSq, other.UnitId, farthestDistSq, farthestUnitId))
                continue;

            neighbors[farthestIndex] = other;
            neighborDistancesSq[farthestIndex] = distSq;
        }

        if (sortByDistance)
            SortByDistanceThenUnitId(neighbors, neighborDistancesSq);
    }

    private static bool IsCloser(float distSq, int unitId, float otherDistSq, int otherUnitId)
    {
        if (distSq < otherDistSq - DistanceTieEpsilon)
            return true;
        if (distSq > otherDistSq + DistanceTieEpsilon)
            return false;
        return unitId < otherUnitId;
    }

    private static bool IsFurther(float distSq, int unitId, float otherDistSq, int otherUnitId)
    {
        if (distSq > otherDistSq + DistanceTieEpsilon)
            return true;
        if (distSq < otherDistSq - DistanceTieEpsilon)
            return false;
        return unitId > otherUnitId;
    }

    private static void SortByDistanceThenUnitId(List<UnitData> neighbors, List<float> distancesSq)
    {
        // Small bounded lists (<= MaxNeighbors) make insertion sort cheaper than full comparer allocs.
        for (int i = 1; i < neighbors.Count; i++)
        {
            var neighbor = neighbors[i];
            float distance = distancesSq[i];
            int j = i - 1;
            while (j >= 0)
            {
                bool previousAfterCurrent =
                    distancesSq[j] > distance + DistanceTieEpsilon
                    || (
                        MathF.Abs(distancesSq[j] - distance) <= DistanceTieEpsilon
                        && neighbors[j].UnitId > neighbor.UnitId
                    );
                if (!previousAfterCurrent)
                    break;

                neighbors[j + 1] = neighbors[j];
                distancesSq[j + 1] = distancesSq[j];
                j--;
            }

            neighbors[j + 1] = neighbor;
            distancesSq[j + 1] = distance;
        }
    }
}
