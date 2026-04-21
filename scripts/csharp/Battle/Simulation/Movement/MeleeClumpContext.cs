using System;
using Fateforged.Simulation.Combat;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Geometry;
using Fateforged.Units;

namespace Fateforged.Simulation.Movement;

/// <summary>
/// Shared melee clump predicates used by movement subsystems.
/// Keeps same-target close-combat gating consistent across steering/ORCA/recovery.
/// </summary>
internal static class MeleeClumpContext
{
    private const float CloseEngageDistanceScale = 1.35f;
    private const float NearbyAllyDistanceMultiplier = 2.2f;
    private const float NearbyAllyDistanceMin = 1.2f;

    public static bool IsSameTargetCloseMeleePair(UnitData unit, UnitData other, MatchState state)
    {
        if (!HasSharedNonSummonerTarget(unit, other))
            return false;
        if (!IsTowardTargetOrEngaging(unit) || !IsTowardTargetOrEngaging(other))
            return false;

        var sharedTargetPosition = SimUtils.ResolveTargetPosition(unit.Engagement.TargetUnitId, state);
        if (!sharedTargetPosition.HasValue)
            return false;

        return IsCloseEngageContext(unit, sharedTargetPosition.Value)
            && IsCloseEngageContext(other, sharedTargetPosition.Value);
    }

    public static bool IsTowardTargetCloseMeleeClump(
        UnitData unit,
        MovementResult movement,
        int? targetId,
        MatchState state,
        SimVector3 targetPosition
    )
    {
        if (
            unit.UnitType != UnitType.Melee
            || movement != MovementResult.TowardTarget
            || !targetId.HasValue
            || MatchState.IsSummonerTarget(targetId)
        )
        {
            return false;
        }

        if (!unit.Engagement.TargetUnitId.HasValue || unit.Engagement.TargetUnitId.Value != targetId.Value)
            return false;
        if (!IsCloseEngageContext(unit, targetPosition))
            return false;

        foreach (var ally in state.GetAliveActiveUnitsForTeam((int)unit.Team))
        {
            if (ally.UnitId == unit.UnitId)
                continue;
            if (!HasSharedNonSummonerTarget(unit, ally))
                continue;

            float pairDistance = (ally.Position - unit.Position).Length();
            float nearbyThreshold = MathF.Max(
                NearbyAllyDistanceMin,
                (
                    CombatGeometry.GetNavigationRadius(unit)
                    + CombatGeometry.GetNavigationRadius(ally)
                ) * NearbyAllyDistanceMultiplier
            );
            if (pairDistance <= nearbyThreshold)
                return true;
        }

        return false;
    }

    private static bool HasSharedNonSummonerTarget(UnitData unit, UnitData other)
    {
        if (unit.Team != other.Team)
            return false;
        if (unit.UnitType != UnitType.Melee || other.UnitType != UnitType.Melee)
            return false;
        if (!unit.Engagement.TargetUnitId.HasValue || !other.Engagement.TargetUnitId.HasValue)
            return false;
        if (unit.Engagement.TargetUnitId.Value != other.Engagement.TargetUnitId.Value)
            return false;
        return !MatchState.IsSummonerTarget(unit.Engagement.TargetUnitId);
    }

    private static bool IsCloseEngageContext(UnitData unit, SimVector3 targetPosition)
    {
        float engageProximityThreshold = MathF.Max(
            unit.AttackRange * CloseEngageDistanceScale,
            unit.AttackRange + CombatGeometry.GetNavigationRadius(unit)
        );
        float distanceToTarget = (targetPosition - unit.Position).Length();
        return distanceToTarget <= engageProximityThreshold;
    }

    private static bool IsTowardTargetOrEngaging(UnitData unit)
    {
        return unit.BehaviorState == BehaviorState.Chasing
            || unit.BehaviorState == BehaviorState.InRange
            || unit.BehaviorState == BehaviorState.Attacking;
    }
}
