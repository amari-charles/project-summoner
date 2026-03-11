using System;
using System.Collections.Generic;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Geometry;
using Fateforged.Units;

namespace Fateforged.Simulation.Movement;

/// <summary>
/// Resolves movement-target positions.
/// For summoner targets, returns an orbit point so units can wrap around
/// blocked fronts instead of funneling into a single lane.
/// </summary>
public static class MovementTargetResolver
{
    [ThreadStatic] private static List<UnitData>? _orbitNeighbors;
    [ThreadStatic] private static List<float>? _orbitNeighborDistancesSq;

    private const float OrbitAttackRangeBuffer = 0.15f;
    private const int MinOrbitSlots = 10;
    private const int MaxOrbitSlots = 48;
    private const float BlockedOrbitThresholdSeconds = 0.18f;
    private const float OrbitFallbackDistanceSq = 0.01f;
    private const int MaxOrbitNeighbors = 24;
    private const float OrbitNeighborScanRadiusMin = 2.5f;
    private const float OrbitNeighborScanRadiusMultiplier = 3.5f;
    private const float OrbitDensitySlotScale = 1.45f;
    private const float OrbitOccupancyHardRadiusMultiplier = 1.25f;
    private const float OrbitOccupancySoftRadiusMultiplier = 2.60f;
    private const float OrbitOccupancyHardWeight = 8.0f;
    private const float OrbitOccupancySoftWeight = 2.2f;
    private const float OrbitDirectionalStepWeight = 0.22f;
    private const float OrbitAbsoluteStepWeight = 0.08f;
    private const float OrbitScoreTieEpsilon = 0.001f;

    public static SimVector3? Resolve(UnitData unit, int? targetId, MatchState state)
    {
        var baseTargetPosition = SimUtils.ResolveTargetPosition(targetId, state);
        if (!baseTargetPosition.HasValue)
            return null;

        if (!MatchState.IsSummonerTarget(targetId))
            return baseTargetPosition.Value;

        return ResolveSummonerOrbitPoint(unit, baseTargetPosition.Value, state);
    }

    private static SimVector3 ResolveSummonerOrbitPoint(UnitData unit, SimVector3 summonerPosition, MatchState state)
    {
        // Keep orbit points inside attack range so units can continue damaging summoners
        // after wrapping around crowded fronts.
        float orbitRadius = MathF.Max(0.1f, unit.AttackRange - OrbitAttackRangeBuffer);
        float unitRadius = CombatGeometry.GetNavigationRadius(unit);
        bool shouldWrap =
            unit.NavigationBlockedTime >= BlockedOrbitThresholdSeconds ||
            unit.NavigationYieldTimer > 0f ||
            unit.NavigationEscapeTimer > 0f;
        int localNeighborCount = 1;

        _orbitNeighbors ??= new List<UnitData>(MaxOrbitNeighbors);
        _orbitNeighborDistancesSq ??= new List<float>(MaxOrbitNeighbors);
        if (shouldWrap)
        {
            float orbitNeighborScanRadius = MathF.Max(
                OrbitNeighborScanRadiusMin,
                orbitRadius + (unitRadius * OrbitNeighborScanRadiusMultiplier)
            );
            MovementNeighborQuery.FillNearestNeighbors(
                unit,
                state,
                orbitNeighborScanRadius,
                MaxOrbitNeighbors,
                _orbitNeighbors,
                _orbitNeighborDistancesSq
            );
            localNeighborCount += _orbitNeighbors.Count;
        }
        else
        {
            _orbitNeighbors.Clear();
            _orbitNeighborDistancesSq.Clear();
        }

        int slotCount = ComputeSlotCount(orbitRadius, unitRadius, localNeighborCount);
        int frontSlot = AngleToSlot(unit.Team == Team.Player ? MathF.PI : 0f, slotCount);

        int selectedSlot;
        if (shouldWrap)
        {
            int signedOffset = ComputeSignedOrbitOffset(unit.UnitId, slotCount);
            if (unit.NavigationEscapeDirectionSign < 0)
                signedOffset = -signedOffset;

            selectedSlot = SelectLeastCrowdedSlot(
                unit,
                summonerPosition,
                orbitRadius,
                slotCount,
                frontSlot,
                signedOffset,
                _orbitNeighbors
            );
        }
        else
        {
            var fromSummoner = unit.Position - summonerPosition;
            fromSummoner.Y = 0f;
            if (fromSummoner.LengthSquared() < OrbitFallbackDistanceSq)
            {
                selectedSlot = frontSlot;
            }
            else
            {
                selectedSlot = DirectionToSlot(fromSummoner.Normalized(), slotCount);
            }
        }

        var orbitDirection = SlotDirection(selectedSlot, slotCount);
        return new SimVector3(
            summonerPosition.X + orbitDirection.X * orbitRadius,
            unit.Position.Y,
            summonerPosition.Z + orbitDirection.Z * orbitRadius
        );
    }

    private static int ComputeSlotCount(float orbitRadius, float navigationRadius, int localNeighborCount)
    {
        float spacing = MathF.Max(0.4f, navigationRadius * 2.0f);
        float circumference = SimMath.Tau * orbitRadius;
        int baseSlots = (int)MathF.Ceiling(circumference / spacing);
        int densitySlots = (int)MathF.Ceiling(localNeighborCount * OrbitDensitySlotScale);
        int slots = Math.Max(baseSlots, densitySlots);
        return Math.Clamp(slots, MinOrbitSlots, MaxOrbitSlots);
    }

    private static int SelectLeastCrowdedSlot(
        UnitData unit,
        SimVector3 summonerPosition,
        float orbitRadius,
        int slotCount,
        int frontSlot,
        int signedOffset,
        List<UnitData> neighbors)
    {
        int preferredSlot = PositiveModulo(frontSlot + signedOffset, slotCount);
        int preferredDirectionSign = signedOffset >= 0 ? 1 : -1;

        int bestSlot = preferredSlot;
        float bestScore = float.MaxValue;
        int bestAbsoluteDistance = slotCount;

        for (int slot = 0; slot < slotCount; slot++)
        {
            var orbitDirection = SlotDirection(slot, slotCount);
            var candidatePosition = new SimVector3(
                summonerPosition.X + orbitDirection.X * orbitRadius,
                unit.Position.Y,
                summonerPosition.Z + orbitDirection.Z * orbitRadius
            );

            float occupancyPenalty = ComputeSlotOccupancyPenalty(candidatePosition, neighbors, CombatGeometry.GetNavigationRadius(unit));
            int directionalDistance = preferredDirectionSign >= 0
                ? PositiveModulo(slot - preferredSlot, slotCount)
                : PositiveModulo(preferredSlot - slot, slotCount);
            int absoluteDistance = CircularDistance(slot, preferredSlot, slotCount);
            float score =
                occupancyPenalty +
                (directionalDistance * OrbitDirectionalStepWeight) +
                (absoluteDistance * OrbitAbsoluteStepWeight);

            bool isBetter = score < bestScore - OrbitScoreTieEpsilon;
            bool isTie = MathF.Abs(score - bestScore) <= OrbitScoreTieEpsilon;
            if (!isBetter && (!isTie || absoluteDistance > bestAbsoluteDistance))
                continue;

            if (isTie && absoluteDistance == bestAbsoluteDistance && slot >= bestSlot)
                continue;

            bestScore = score;
            bestSlot = slot;
            bestAbsoluteDistance = absoluteDistance;
        }

        return bestSlot;
    }

    private static float ComputeSlotOccupancyPenalty(
        SimVector3 candidatePosition, List<UnitData> neighbors, float navigationRadius)
    {
        float hardRadius = MathF.Max(0.5f, navigationRadius * OrbitOccupancyHardRadiusMultiplier);
        float softRadius = MathF.Max(hardRadius + 0.25f, navigationRadius * OrbitOccupancySoftRadiusMultiplier);
        float hardRadiusSq = hardRadius * hardRadius;
        float softRadiusSq = softRadius * softRadius;

        float penalty = 0f;
        foreach (var neighbor in neighbors)
        {
            var diff = neighbor.Position - candidatePosition;
            diff.Y = 0f;
            float distSq = diff.LengthSquared();
            if (distSq <= hardRadiusSq)
            {
                penalty += OrbitOccupancyHardWeight * (1f - (distSq / hardRadiusSq));
                continue;
            }

            if (distSq <= softRadiusSq)
                penalty += OrbitOccupancySoftWeight * (1f - (distSq / softRadiusSq));
        }

        return penalty;
    }

    private static int ComputeSignedOrbitOffset(int unitId, int slotCount)
    {
        if (slotCount <= 1)
            return 0;

        uint mixed = (uint)unitId * 2654435761u;
        int raw = (int)(mixed % (uint)slotCount);
        int centered = raw <= slotCount / 2 ? raw : raw - slotCount;
        if (centered == 0)
            centered = (unitId & 1) == 0 ? 1 : -1;
        return centered;
    }

    private static int DirectionToSlot(SimVector3 direction, int slotCount)
    {
        float angle = MathF.Atan2(direction.Z, direction.X);
        if (angle < 0f)
            angle += SimMath.Tau;
        int slot = (int)MathF.Round((angle / SimMath.Tau) * slotCount) % slotCount;
        return slot;
    }

    private static int AngleToSlot(float angle, int slotCount)
    {
        float wrapped = angle % SimMath.Tau;
        if (wrapped < 0f)
            wrapped += SimMath.Tau;
        return (int)MathF.Round((wrapped / SimMath.Tau) * slotCount) % slotCount;
    }

    private static int CircularDistance(int a, int b, int modulo)
    {
        int forward = PositiveModulo(a - b, modulo);
        int backward = PositiveModulo(b - a, modulo);
        return Math.Min(forward, backward);
    }

    private static SimVector3 SlotDirection(int slotIndex, int slotCount)
    {
        float angle = (slotIndex / (float)slotCount) * SimMath.Tau;
        return new SimVector3(MathF.Cos(angle), 0f, MathF.Sin(angle));
    }

    private static int PositiveModulo(int value, int modulo)
    {
        int result = value % modulo;
        return result < 0 ? result + modulo : result;
    }
}
