using System;
using Fateforged.Simulation;
using Fateforged.Simulation.Combat.Slots;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Geometry;
using Fateforged.Simulation.Spatial;
using Fateforged.Units;

namespace Fateforged.Simulation.Combat;

/// <summary>
/// Pure deterministic target acquisition operating on UnitData arrays.
/// Mirrors the TargetingConfig Filter→Scorer pipeline from the presentation layer.
/// No Godot node dependencies — operates on MatchState data only.
/// </summary>
public static class SimTargeting
{
    private const float CrossLaneAggroDistanceScale = 0.55f;
    private const float SameLaneScoreBonus = 2.5f;
    private const float CrossLaneScorePenaltyPerLane = 3.0f;
    private const float FlankerOffLanePenalty = 4.0f;
    private const float BacklinerCrossLanePenalty = 5.0f;
    private const float FlankerCenterIgnoreDistance = 8.0f;
    private const float GeometryEpsilon = 0.00001f;
    private const float CommitStickinessBonus = 6.0f;
    private const float CommitCongestionWeight = 12.0f;
    private const float CommitFrontageArcDegrees = 135.0f;
    private const float CommitSummonerAcquireDistanceScale = 1.5f;
    private const float CommitSummonerAcquireDistanceMin = 20.0f;
    private const float ScoreTieEpsilon = 0.0001f;

    /// <summary>
    /// Acquire a target using the unit's configured targeting policy.
    /// </summary>
    public static int? AcquireTarget(UnitData unit, MatchState state)
    {
        var policy = Targeting.TargetPolicyRegistry.Resolve(unit.TargetPolicyId);
        return policy.SelectTarget(unit, state);
    }

    /// <summary>
    /// Commit-target acquisition used by commit-slot lifecycle.
    /// Summoner is always a valid candidate and congestion penalty is applied for melee vs unit targets.
    /// </summary>
    public static int? AcquireTargetCommit(
        UnitData unit,
        MatchState state,
        int? currentTargetId,
        int? droppedTargetId,
        float droppedTargetCooldownTimer
    )
    {
        int enemyTeam = MatchState.GetEnemyTeam((int)unit.Team);
        int attackerLane = ResolvePreferredLane(unit);
        EngageShape engageShape = ResolveEngageShape(unit);

        float bestScore = float.MinValue;
        int? bestId = null;
        bool anyEnemyUnitAlive = false;
        bool hadInAggroCandidate = false;
        bool sawSaturatedInAggroCandidate = false;

        foreach (var kvp in state.Units)
        {
            var candidate = kvp.Value;
            if (!candidate.IsAlive)
                continue;
            if (candidate.ActivationState != ActivationState.Active)
                continue;
            if ((int)candidate.Team != enemyTeam)
                continue;
            anyEnemyUnitAlive = true;
            if (
                droppedTargetCooldownTimer > 0f
                && droppedTargetId.HasValue
                && candidate.UnitId == droppedTargetId.Value
            )
            {
                continue;
            }

            float distSq = unit.Position.DistanceSquaredTo(candidate.Position);
            if (distSq > unit.AggroRadius * unit.AggroRadius)
                continue;
            float dist = MathF.Sqrt(distSq);

            int candidateLane = VirtualLanes.GetLaneIndex(candidate.Position.Z);
            int laneDistance = VirtualLanes.LaneDistance(attackerLane, candidateLane);
            if (laneDistance > 0 && dist > unit.AggroRadius * CrossLaneAggroDistanceScale)
                continue;
            if (!PassesLayerFilter(unit, candidate))
                continue;
            if (engageShape == EngageShape.Cone && !CanEverReach(unit, candidate))
                continue;
            if (ShouldIgnoreForRole(unit, attackerLane, candidateLane, laneDistance, dist))
                continue;
            hadInAggroCandidate = true;

            if (IsTargetSlotSaturatedForAttacker(unit, candidate, state))
            {
                sawSaturatedInAggroCandidate = true;
                continue;
            }

            float score = ScoreTarget(unit, candidate, dist);
            score += ScoreLaneAffinity(unit, attackerLane, candidateLane, laneDistance);
            if (currentTargetId.HasValue && currentTargetId.Value == candidate.UnitId)
                score += CommitStickinessBonus;
            score -= ComputeCongestionPenalty(unit, candidate, state);

            if (IsBetterScoredCandidate(score, candidate.UnitId, bestScore, bestId))
            {
                bestScore = score;
                bestId = candidate.UnitId;
            }
        }

        if (bestId.HasValue)
            return bestId;

        var enemySummoner = state.GetAliveEnemySummoner((int)unit.Team);
        if (enemySummoner == null)
            return null;

        int summonerTargetId = MatchState.GetSummonerTargetId((int)enemySummoner.Team);
        if (
            droppedTargetCooldownTimer > 0f
            && droppedTargetId.HasValue
            && droppedTargetId.Value == summonerTargetId
        )
        {
            return null;
        }

        float summonerDistance = DistanceXZ(unit.Position, enemySummoner.Position);
        float summonerAcquireDistance = MathF.Max(
            CommitSummonerAcquireDistanceMin,
            unit.AggroRadius * CommitSummonerAcquireDistanceScale
        );

        // If there were in-aggro unit candidates but they were all saturated,
        // allow fallback to summoner immediately to avoid deadlock.
        if (hadInAggroCandidate && sawSaturatedInAggroCandidate)
            return summonerTargetId;

        // Otherwise, avoid locking summoner from too far away.
        if (summonerDistance > summonerAcquireDistance && anyEnemyUnitAlive)
            return null;

        return summonerTargetId;
    }

    private static bool IsTargetSlotSaturatedForAttacker(
        UnitData attacker,
        UnitData target,
        MatchState state
    )
    {
        if (attacker.UnitType != UnitType.Melee)
            return false;

        if (
            attacker.SlotTargetId.HasValue
            && attacker.SlotTargetId.Value == target.UnitId
            && attacker.ReservedSlotId.HasValue
        )
        {
            return false;
        }

        if (!state.TargetSlotStates.TryGetValue(target.UnitId, out var slotState))
            return false;

        foreach (var slot in slotState.Slots)
        {
            if (slot.ReservedUnitId == attacker.UnitId || slot.OccupiedUnitId == attacker.UnitId)
                return false;
            if (slot.OccupancyState == SlotOccupancyState.Free)
                return false;
        }

        return slotState.Slots.Count > 0;
    }

    /// <summary>
    /// Target acquisition that prefers currently attackable candidates, then falls back
    /// to baseline score-only selection.
    /// </summary>
    public static int? AcquireTargetPreferAttackable(UnitData unit, MatchState state) =>
        AcquireTargetCore(unit, state, prioritizeAttackableNow: true);

    /// <summary>
    /// Find the best target for a unit from all alive active enemy units.
    /// Group-aware: if unit has a LeaderId, copies leader's target.
    /// Returns the UnitId of the best target, or null if none found.
    /// </summary>
    private static int? AcquireTargetCore(
        UnitData unit,
        MatchState state,
        bool prioritizeAttackableNow
    )
    {
        // Group targeting: follow leader's target if available
        if (unit.LeaderId.HasValue)
        {
            var leader = state.GetAliveUnit(unit.LeaderId.Value);
            if (leader?.TargetUnitId != null)
                return leader.TargetUnitId;
            // Leader dead or no target — fall through to normal targeting
        }

        int enemyTeam = MatchState.GetEnemyTeam((int)unit.Team);
        int attackerLane = ResolvePreferredLane(unit);
        EngageShape engageShape = ResolveEngageShape(unit);
        float bestScore = float.MinValue;
        float bestAttackableScore = float.MinValue;
        int? bestId = null;
        int? bestAttackableId = null;

        foreach (var kvp in state.Units)
        {
            var candidate = kvp.Value;

            // Basic filters
            if (!candidate.IsAlive)
                continue;
            if (candidate.ActivationState != ActivationState.Active)
                continue;
            if ((int)candidate.Team != enemyTeam)
                continue;

            // Distance filter (aggro radius)
            float distSq = unit.Position.DistanceSquaredTo(candidate.Position);
            if (distSq > unit.AggroRadius * unit.AggroRadius)
                continue;
            float dist = MathF.Sqrt(distSq);

            int candidateLane = VirtualLanes.GetLaneIndex(candidate.Position.Z);
            int laneDistance = VirtualLanes.LaneDistance(attackerLane, candidateLane);

            // Virtual lane guard: far cross-lane candidates are ignored to reduce center pull.
            if (laneDistance > 0 && dist > unit.AggroRadius * CrossLaneAggroDistanceScale)
                continue;

            // Layer filter
            if (!PassesLayerFilter(unit, candidate))
                continue;

            // Reachability (cone constraint)
            if (engageShape == EngageShape.Cone && !CanEverReach(unit, candidate))
                continue;
            if (ShouldIgnoreForRole(unit, attackerLane, candidateLane, laneDistance, dist))
                continue;

            // Score the candidate
            float score = ScoreTarget(unit, candidate, dist);
            score += ScoreLaneAffinity(unit, attackerLane, candidateLane, laneDistance);

            if (
                prioritizeAttackableNow
                && IsWithinEngageDistance(unit, candidate.Position)
                && CanAttack(unit, candidate)
                && IsBetterScoredCandidate(
                    score,
                    candidate.UnitId,
                    bestAttackableScore,
                    bestAttackableId
                )
            )
            {
                bestAttackableScore = score;
                bestAttackableId = candidate.UnitId;
            }

            if (IsBetterScoredCandidate(score, candidate.UnitId, bestScore, bestId))
            {
                bestScore = score;
                bestId = candidate.UnitId;
            }
        }

        if (bestAttackableId.HasValue)
            return bestAttackableId;

        if (bestId.HasValue)
            return bestId;

        // No enemy units found — fall back to enemy summoner
        var enemySummoner = state.GetAliveEnemySummoner((int)unit.Team);
        if (enemySummoner != null)
        {
            return MatchState.GetSummonerTargetId((int)enemySummoner.Team);
        }

        return null;
    }

    /// <summary>
    /// Returns true if target is currently attackable by this unit:
    /// within attack range and satisfying cone constraint.
    /// </summary>
    public static bool IsTargetAttackableNow(UnitData unit, int targetId, MatchState state)
    {
        var targetPosition = SimUtils.ResolveTargetPosition(targetId, state);
        if (!targetPosition.HasValue)
            return false;

        if (MatchState.IsSummonerTarget(targetId))
        {
            var engagePosition = ResolveSummonerEngagePosition(unit, targetPosition.Value);
            return IsWithinEngageDistance(unit, engagePosition) &&
                   CanAttackPosition(unit, engagePosition);
        }

        if (!IsWithinEngageDistance(unit, targetPosition.Value))
            return false;

        var target = state.GetAliveUnit(targetId);
        return target != null && CanAttack(unit, target);
    }

    /// <summary>
    /// Check if candidate passes the unit's layer filter.
    /// </summary>
    private static bool PassesLayerFilter(UnitData unit, UnitData candidate)
    {
        return unit.TargetLayerFilter switch
        {
            TargetLayer.GroundOnly => candidate.MovementLayer == MovementLayer.Ground,
            TargetLayer.AirOnly => candidate.MovementLayer == MovementLayer.Air,
            _ => true,
        };
    }

    private static bool IsBetterScoredCandidate(
        float candidateScore,
        int candidateId,
        float bestScore,
        int? bestId
    )
    {
        if (!bestId.HasValue)
            return true;

        if (candidateScore > bestScore + ScoreTieEpsilon)
            return true;
        if (candidateScore < bestScore - ScoreTieEpsilon)
            return false;

        // Stable tie-break for equal scores avoids selection churn between equivalent targets.
        return candidateId < bestId.Value;
    }

    /// <summary>
    /// Check if a target can ever be reached by the unit's cone constraint.
    /// For ground units attacking air units with a cone: checks if the vertical angle is within the cone.
    /// </summary>
    private static bool CanEverReach(UnitData unit, UnitData candidate)
    {
        if (ResolveEngageShape(unit) != EngageShape.Cone)
            return true;

        var toTarget = candidate.Position - unit.Position;

        // Very close — always reachable
        if (toTarget.Length() < unit.CloseRangeThreshold)
            return true;

        float horizontalDist = MathF.Sqrt(toTarget.X * toTarget.X + toTarget.Z * toTarget.Z);
        float verticalDist = MathF.Abs(toTarget.Y);

        if (horizontalDist < unit.CloseRangeThreshold)
            return toTarget.Length() < unit.CloseRangeThreshold;

        float angleToTarget = SimMath.RadToDeg(MathF.Atan2(verticalDist, horizontalDist));
        return angleToTarget <= unit.ConeHalfAngle;
    }

    /// <summary>
    /// Score a candidate target. Higher = more attractive.
    /// Mirrors DistanceScorer + HealthScorer from the targeting pipeline.
    /// </summary>
    private static float ScoreTarget(UnitData unit, UnitData candidate, float distance)
    {
        float score = 0f;

        // Distance scorer: closer = higher score (mirrors DistanceScorer)
        score += (unit.AggroRadius - distance) * unit.DistanceScorerWeight;

        // Health scorer: lower HP % = higher score (mirrors HealthScorer)
        if (unit.HealthScorerWeight > 0f && candidate.MaxHp > 0f)
        {
            float hpPercent = candidate.CurrentHp / candidate.MaxHp;
            score += (1f - hpPercent) * unit.HealthScorerWeight;
        }

        return score;
    }

    private static int ResolvePreferredLane(UnitData unit)
    {
        if (unit.AssignedLane >= 0)
            return unit.AssignedLane;
        return VirtualLanes.GetLaneIndex(unit.Position.Z);
    }

    private static bool ShouldIgnoreForRole(
        UnitData unit,
        int attackerLane,
        int candidateLane,
        int laneDistance,
        float distance
    )
    {
        if (
            unit.TacticalRole == TacticalRole.Flanker
            && VirtualLanes.IsSideLane(attackerLane)
            && candidateLane == VirtualLanes.CenterLane
            && distance > FlankerCenterIgnoreDistance
        )
        {
            return true;
        }

        if (
            unit.TacticalRole == TacticalRole.Backliner
            && laneDistance > 1
            && distance > unit.AttackRange * 1.2f
        )
        {
            return true;
        }

        return false;
    }

    private static float ScoreLaneAffinity(
        UnitData unit,
        int attackerLane,
        int candidateLane,
        int laneDistance
    )
    {
        float score =
            laneDistance == 0 ? SameLaneScoreBonus : -CrossLaneScorePenaltyPerLane * laneDistance;

        switch (unit.TacticalRole)
        {
            case TacticalRole.Flanker:
                if (VirtualLanes.IsSideLane(attackerLane) && candidateLane != attackerLane)
                    score -= FlankerOffLanePenalty;
                break;

            case TacticalRole.Backliner:
                if (laneDistance > 0)
                    score -= BacklinerCrossLanePenalty * laneDistance;
                break;
        }

        return score;
    }

    private static float ComputeCongestionPenalty(
        UnitData attacker,
        UnitData target,
        MatchState state
    )
    {
        if (attacker.UnitType != UnitType.Melee)
            return 0f;

        float attackerRadius = MathF.Max(0.1f, CombatGeometry.GetNavigationRadius(attacker));
        float targetRadius = MathF.Max(0.1f, CombatGeometry.GetNavigationRadius(target));
        float frontageRadius = targetRadius + attackerRadius;
        float spacing = MathF.Max(0.25f, attackerRadius * 1.8f);
        float arcLength = frontageRadius * SimMath.DegToRad(CommitFrontageArcDegrees);
        int capacity = Math.Clamp((int)MathF.Floor(arcLength / spacing), 1, 6);

        int assigned = 0;
        foreach (var ally in state.GetAliveActiveUnitsForTeam((int)attacker.Team))
        {
            if (ally.UnitType != UnitType.Melee)
                continue;
            int? allyTarget = ally.LockedTargetUnitId ?? ally.TargetUnitId;
            if (allyTarget.HasValue && allyTarget.Value == target.UnitId)
                assigned++;
        }

        float saturation = (assigned + 1f) / capacity;
        if (saturation <= 1f)
            return 0f;

        float over = saturation - 1f;
        return CommitCongestionWeight * over * over;
    }

    /// <summary>
    /// Returns true when the target position is within this unit's engage distance envelope.
    /// Shape orientation checks are applied by CanAttackPosition.
    /// </summary>
    public static bool IsWithinEngageDistance(UnitData unit, SimVector3 targetPosition)
    {
        float horizontalDistance = DistanceXZ(unit.Position, targetPosition);
        EngageShape engageShape = ResolveEngageShape(unit);

        if (engageShape != EngageShape.ForwardRect)
            return horizontalDistance <= unit.AttackRange;

        float reachFromRect = unit.EngageRectForwardOffset + unit.EngageRectLength;
        float maxReach = MathF.Max(
            unit.AttackRange,
            MathF.Max(reachFromRect, unit.EngageCloseRadius)
        );
        return horizontalDistance <= maxReach + GeometryEpsilon;
    }

    /// <summary>
    /// Check if a unit can attack a target at the given position (engage shape satisfied).
    /// Overload for summoner targets that don't have UnitData.
    /// </summary>
    public static bool CanAttackPosition(UnitData unit, SimVector3 targetPosition)
    {
        return ResolveEngageShape(unit) switch
        {
            EngageShape.Circle => true,
            EngageShape.Cone => IsInsideFacingCone(unit, targetPosition),
            EngageShape.ForwardRect => IsInsideForwardRect(unit, targetPosition),
            _ => true,
        };
    }

    /// <summary>
    /// Check if a unit can attack a target (cone constraint satisfied).
    /// Used by SimBehavior to decide between attacking and fallback movement.
    /// Delegates to CanAttackPosition using the target's current position.
    /// </summary>
    public static bool CanAttack(UnitData unit, UnitData target) =>
        CanAttackPosition(unit, target.Position);

    /// <summary>
    /// Summoner melee attackability is evaluated against the closest point on the
    /// shared summoner bubble, not the summoner center point.
    /// Non-melee units keep center-point behavior.
    /// </summary>
    public static SimVector3 ResolveSummonerEngagePosition(UnitData unit, SimVector3 summonerPosition)
    {
        if (unit.UnitType != UnitType.Melee)
            return summonerPosition;
        return SummonerMeleeBubble.ResolveClosestPoint(summonerPosition, unit.Position);
    }

    private static EngageShape ResolveEngageShape(UnitData unit)
    {
        if (unit.EngageShape != EngageShape.Circle)
            return unit.EngageShape;
        return unit.HasConeConstraint ? EngageShape.Cone : EngageShape.Circle;
    }

    private static bool IsInsideFacingCone(UnitData unit, SimVector3 targetPosition)
    {
        var toTarget = targetPosition - unit.Position;
        if (toTarget.Length() < unit.CloseRangeThreshold)
            return true;

        float horizontalLen = DistanceXZ(unit.Position, targetPosition);
        if (horizontalLen < unit.CloseRangeThreshold)
            return true;

        float angleToTarget = SimMath.RadToDeg(MathF.Atan2(toTarget.Z, toTarget.X));
        float facingAngle = (unit.IsFacingRight ? 0f : 180f) + unit.ConeCenterOffsetDegrees;
        float angleDiff = angleToTarget - facingAngle;
        while (angleDiff > 180f)
            angleDiff -= 360f;
        while (angleDiff < -180f)
            angleDiff += 360f;

        return MathF.Abs(angleDiff) <= unit.ConeHalfAngle;
    }

    private static bool IsInsideForwardRect(UnitData unit, SimVector3 targetPosition)
    {
        float closeRadius = MathF.Max(unit.EngageCloseRadius, 0.01f);
        if (DistanceXZ(unit.Position, targetPosition) <= closeRadius + GeometryEpsilon)
            return true;

        float length =
            unit.EngageRectLength > 0f
                ? unit.EngageRectLength
                : MathF.Max(unit.AttackRange * 0.9f, 0.1f);
        float halfWidth = unit.EngageRectHalfWidth > 0f ? unit.EngageRectHalfWidth : 0.45f;
        float forwardOffset = MathF.Max(unit.EngageRectForwardOffset, 0f);

        float forwardSign = unit.IsFacingRight ? 1f : -1f;
        float relX = targetPosition.X - unit.Position.X;
        float relZ = targetPosition.Z - unit.Position.Z;
        float projectedForward = relX * forwardSign;
        float projectedRight = MathF.Abs(relZ);

        return projectedForward >= forwardOffset - GeometryEpsilon
            && projectedForward <= (forwardOffset + length) + GeometryEpsilon
            && projectedRight <= halfWidth + GeometryEpsilon;
    }

    private static float DistanceXZ(SimVector3 a, SimVector3 b)
    {
        float dx = a.X - b.X;
        float dz = a.Z - b.Z;
        return MathF.Sqrt((dx * dx) + (dz * dz));
    }
}
