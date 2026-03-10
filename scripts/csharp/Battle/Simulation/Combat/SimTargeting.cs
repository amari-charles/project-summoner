using System;
using Fateforged.Simulation;
using Fateforged.Units;
using Fateforged.Simulation.Data;

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

    /// <summary>
    /// Acquire a target using the unit's configured targeting policy.
    /// </summary>
    public static int? AcquireTarget(UnitData unit, MatchState state)
    {
        var policy = Targeting.TargetPolicyRegistry.Resolve(unit.TargetPolicyId);
        return policy.SelectTarget(unit, state);
    }

    /// <summary>
    /// Baseline target acquisition: score-only selection without attackable-now preference.
    /// </summary>
    public static int? AcquireTargetLegacy(UnitData unit, MatchState state)
        => AcquireTargetCore(unit, state, prioritizeAttackableNow: false);

    /// <summary>
    /// Target acquisition that prefers currently attackable candidates, then falls back
    /// to baseline score-only selection.
    /// </summary>
    public static int? AcquireTargetPreferAttackable(UnitData unit, MatchState state)
        => AcquireTargetCore(unit, state, prioritizeAttackableNow: true);

    /// <summary>
    /// Find the best target for a unit from all alive active enemy units.
    /// Group-aware: if unit has a LeaderId, copies leader's target.
    /// Returns the UnitId of the best target, or null if none found.
    /// </summary>
    private static int? AcquireTargetCore(UnitData unit, MatchState state, bool prioritizeAttackableNow)
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
        float bestScore = float.MinValue;
        float bestAttackableScore = float.MinValue;
        int? bestId = null;
        int? bestAttackableId = null;

        foreach (var kvp in state.Units)
        {
            var candidate = kvp.Value;

            // Basic filters
            if (!candidate.IsAlive) continue;
            if (candidate.ActivationState != ActivationState.Active) continue;
            if ((int)candidate.Team != enemyTeam) continue;

            // Distance filter (aggro radius)
            float distSq = unit.Position.DistanceSquaredTo(candidate.Position);
            if (distSq > unit.AggroRadius * unit.AggroRadius) continue;
            float dist = MathF.Sqrt(distSq);

            int candidateLane = VirtualLanes.GetLaneIndex(candidate.Position.Z);
            int laneDistance = VirtualLanes.LaneDistance(attackerLane, candidateLane);

            // Virtual lane guard: far cross-lane candidates are ignored to reduce center pull.
            if (laneDistance > 0 && dist > unit.AggroRadius * CrossLaneAggroDistanceScale) continue;

            // Layer filter
            if (!PassesLayerFilter(unit, candidate)) continue;

            // Reachability (cone constraint)
            if (unit.HasConeConstraint && !CanEverReach(unit, candidate)) continue;
            if (ShouldIgnoreForRole(unit, attackerLane, candidateLane, laneDistance, dist)) continue;

            // Score the candidate
            float score = ScoreTarget(unit, candidate, dist);
            score += ScoreLaneAffinity(unit, attackerLane, candidateLane, laneDistance);

            if (prioritizeAttackableNow &&
                dist <= unit.AttackRange &&
                CanAttack(unit, candidate) &&
                score > bestAttackableScore)
            {
                bestAttackableScore = score;
                bestAttackableId = candidate.UnitId;
            }

            if (score > bestScore)
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
        if (!targetPosition.HasValue || !IsWithinAttackRange(unit, targetPosition.Value))
            return false;

        if (MatchState.IsSummonerTarget(targetId))
            return CanAttackPosition(unit, targetPosition.Value);

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
            _ => true
        };
    }

    /// <summary>
    /// Check if a target can ever be reached by the unit's cone constraint.
    /// For ground units attacking air units with a cone: checks if the vertical angle is within the cone.
    /// </summary>
    private static bool CanEverReach(UnitData unit, UnitData candidate)
    {
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
        UnitData unit, int attackerLane, int candidateLane, int laneDistance, float distance)
    {
        if (unit.TacticalRole == TacticalRole.Flanker &&
            VirtualLanes.IsSideLane(attackerLane) &&
            candidateLane == VirtualLanes.CenterLane &&
            distance > FlankerCenterIgnoreDistance)
        {
            return true;
        }

        if (unit.TacticalRole == TacticalRole.Backliner &&
            laneDistance > 1 &&
            distance > unit.AttackRange * 1.2f)
        {
            return true;
        }

        return false;
    }

    private static float ScoreLaneAffinity(
        UnitData unit, int attackerLane, int candidateLane, int laneDistance)
    {
        float score = laneDistance == 0
            ? SameLaneScoreBonus
            : -CrossLaneScorePenaltyPerLane * laneDistance;

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

    /// <summary>
    /// Check if a unit can attack a target at the given position (cone constraint satisfied).
    /// Overload for summoner targets that don't have UnitData.
    /// </summary>
    public static bool CanAttackPosition(UnitData unit, SimVector3 targetPosition)
    {
        if (!unit.HasConeConstraint)
            return true;

        var toTarget = targetPosition - unit.Position;

        if (toTarget.Length() < unit.CloseRangeThreshold)
            return true;

        float hDx = toTarget.X;
        float hDz = toTarget.Z;
        float horizontalLen = MathF.Sqrt(hDx * hDx + hDz * hDz);
        if (horizontalLen < unit.CloseRangeThreshold)
            return true;

        float angleToTarget = SimMath.RadToDeg(MathF.Atan2(hDz, hDx));
        float facingAngle = unit.IsFacingRight ? 0f : 180f;

        float angleDiff = angleToTarget - facingAngle;
        while (angleDiff > 180f) angleDiff -= 360f;
        while (angleDiff < -180f) angleDiff += 360f;

        return MathF.Abs(angleDiff) <= unit.ConeHalfAngle;
    }

    /// <summary>
    /// Check if a unit can attack a target (cone constraint satisfied).
    /// Used by SimBehavior to decide between attacking and fallback movement.
    /// Delegates to CanAttackPosition using the target's current position.
    /// </summary>
    public static bool CanAttack(UnitData unit, UnitData target)
        => CanAttackPosition(unit, target.Position);

    private static bool IsWithinAttackRange(UnitData unit, SimVector3 targetPosition)
    {
        float dx = unit.Position.X - targetPosition.X;
        float dz = unit.Position.Z - targetPosition.Z;
        float horizontalDistance = MathF.Sqrt(dx * dx + dz * dz);
        return horizontalDistance <= unit.AttackRange;
    }
}
