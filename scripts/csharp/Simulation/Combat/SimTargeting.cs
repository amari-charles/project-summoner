using System;
using ProjectSummoner.Units;

namespace Fateforged.Simulation.Combat;

/// <summary>
/// Pure deterministic target acquisition operating on UnitData arrays.
/// Mirrors the TargetingConfig Filter→Scorer pipeline from the presentation layer.
/// No Godot node dependencies — operates on MatchState data only.
/// </summary>
public static class SimTargeting
{

    /// <summary>
    /// Find the best target for a unit from all alive active enemy units.
    /// Group-aware: if unit has a LeaderId, copies leader's target.
    /// Returns the UnitId of the best target, or null if none found.
    /// </summary>
    public static int? AcquireTarget(UnitData unit, MatchState state)
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
        float bestScore = float.MinValue;
        int? bestId = null;

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

            // Layer filter
            if (!PassesLayerFilter(unit, candidate)) continue;

            // Reachability (cone constraint)
            if (unit.HasConeConstraint && !CanEverReach(unit, candidate)) continue;

            // Score the candidate
            float dist = MathF.Sqrt(distSq);
            float score = ScoreTarget(unit, candidate, dist);

            if (score > bestScore)
            {
                bestScore = score;
                bestId = candidate.UnitId;
            }
        }

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
    /// </summary>
    public static bool CanAttack(UnitData unit, UnitData target)
    {
        if (!unit.HasConeConstraint)
            return true;

        var toTarget = target.Position - unit.Position;

        // Very close — always OK
        if (toTarget.Length() < unit.CloseRangeThreshold)
            return true;

        // Horizontal cone check (XZ plane only)
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
}
