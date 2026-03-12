using System;
using System.Collections.Generic;
using Fateforged.Simulation.Data;
using Fateforged.Units;

namespace Fateforged.Simulation.Combat;

/// <summary>
/// Deterministic recipient resolution for vector-based melee attacks.
/// </summary>
public static class AttackRecipientResolver
{
    private const float GeometryEpsilon = 0.00001f;

    public static List<UnitData> ResolveRecipients(UnitData attacker, UnitData primaryTarget, MatchState state)
    {
        return attacker.Attack.Selection.Mode switch
        {
            AttackSelectionMode.Single => BuildPrimaryOnly(primaryTarget),
            AttackSelectionMode.AreaCollect => ResolveAreaRecipients(attacker, primaryTarget, state),
            AttackSelectionMode.LineCollect => ResolveLineRecipients(attacker, primaryTarget, state),
            AttackSelectionMode.ChainHops => ResolveChainRecipients(attacker, primaryTarget, state),
            _ => BuildPrimaryOnly(primaryTarget)
        };
    }

    private static List<UnitData> BuildPrimaryOnly(UnitData primaryTarget)
    {
        if (!primaryTarget.IsAlive)
            return new List<UnitData>();
        return new List<UnitData> { primaryTarget };
    }

    private static List<UnitData> ResolveAreaRecipients(UnitData attacker, UnitData primaryTarget, MatchState state)
    {
        bool includePrimary = PassesLayerFilter(attacker, primaryTarget) &&
                              IsAreaMatch(attacker, primaryTarget, primaryTarget);
        var secondaries = new List<UnitData>();
        foreach (var candidate in EnumerateEnemyCandidates(attacker, state))
        {
            if (candidate.UnitId == primaryTarget.UnitId)
                continue;
            if (!PassesLayerFilter(attacker, candidate))
                continue;
            if (!IsAreaMatch(attacker, primaryTarget, candidate))
                continue;
            secondaries.Add(candidate);
        }

        secondaries.Sort((a, b) =>
        {
            float da = DistanceSquaredXZ(primaryTarget.Position, a.Position);
            float db = DistanceSquaredXZ(primaryTarget.Position, b.Position);
            int distanceCompare = da.CompareTo(db);
            if (distanceCompare != 0)
                return distanceCompare;
            return a.UnitId.CompareTo(b.UnitId);
        });

        return FinalizeRecipients(attacker, primaryTarget, secondaries, includePrimary);
    }

    private static List<UnitData> ResolveLineRecipients(UnitData attacker, UnitData primaryTarget, MatchState state)
    {
        var secondaries = new List<UnitData>();
        var line = BuildLineQuery(attacker, primaryTarget);
        bool includePrimary = PassesLayerFilter(attacker, primaryTarget) &&
                              IsInsideLineCorridor(primaryTarget.Position, line.Origin, line.Direction, line.Length, line.HalfWidth);

        foreach (var candidate in EnumerateEnemyCandidates(attacker, state))
        {
            if (candidate.UnitId == primaryTarget.UnitId)
                continue;
            if (!PassesLayerFilter(attacker, candidate))
                continue;

            if (!IsInsideLineCorridor(candidate.Position, line.Origin, line.Direction, line.Length, line.HalfWidth))
                continue;

            secondaries.Add(candidate);
        }

        secondaries.Sort((a, b) =>
        {
            float ta = ProjectAlongLine(a.Position, line.Origin, line.Direction);
            float tb = ProjectAlongLine(b.Position, line.Origin, line.Direction);
            int tCompare = ta.CompareTo(tb);
            if (tCompare != 0)
                return tCompare;

            float pa = DistanceSquaredFromLine(a.Position, line.Origin, line.Direction);
            float pb = DistanceSquaredFromLine(b.Position, line.Origin, line.Direction);
            int perpendicularCompare = pa.CompareTo(pb);
            if (perpendicularCompare != 0)
                return perpendicularCompare;

            return a.UnitId.CompareTo(b.UnitId);
        });

        return FinalizeRecipients(attacker, primaryTarget, secondaries, includePrimary);
    }

    private static List<UnitData> ResolveChainRecipients(UnitData attacker, UnitData primaryTarget, MatchState state)
    {
        if (!primaryTarget.IsAlive)
            return new List<UnitData>();

        var recipients = new List<UnitData> { primaryTarget };
        int recipientLimit = ResolveRecipientLimit(attacker.Attack.Selection.TargetLimit);
        if (recipientLimit == 1)
            return recipients;

        int maxJumps = Math.Max(attacker.Attack.Propagation.ChainMaxJumps, 0);
        float jumpRadius = attacker.Attack.Propagation.ChainJumpRadius;
        if (maxJumps <= 0 || jumpRadius <= 0f)
            return recipients;

        bool allowRepeats = attacker.Attack.Rules.AllowRepeatHits;
        var visited = new HashSet<int> { primaryTarget.UnitId };
        var current = primaryTarget;
        for (int hop = 0; hop < maxJumps && recipients.Count < recipientLimit; hop++)
        {
            var next = FindNextChainRecipient(attacker, current, state, visited, allowRepeats, jumpRadius);
            if (next == null)
                break;

            recipients.Add(next);
            if (!allowRepeats)
                visited.Add(next.UnitId);
            current = next;
        }

        return recipients;
    }

    private static UnitData? FindNextChainRecipient(
        UnitData attacker,
        UnitData fromUnit,
        MatchState state,
        HashSet<int> visited,
        bool allowRepeats,
        float jumpRadius)
    {
        UnitData? best = null;
        float bestDistSq = float.MaxValue;
        float radiusSq = jumpRadius * jumpRadius;
        foreach (var candidate in EnumerateEnemyCandidates(attacker, state))
        {
            if (candidate.UnitId == fromUnit.UnitId)
                continue;
            if (!PassesLayerFilter(attacker, candidate))
                continue;
            if (!allowRepeats && visited.Contains(candidate.UnitId))
                continue;

            float distSq = DistanceSquaredXZ(fromUnit.Position, candidate.Position);
            if (distSq > radiusSq + GeometryEpsilon)
                continue;

            if (best == null ||
                distSq < bestDistSq - GeometryEpsilon ||
                (MathF.Abs(distSq - bestDistSq) <= GeometryEpsilon && candidate.UnitId < best.UnitId))
            {
                best = candidate;
                bestDistSq = distSq;
            }
        }

        return best;
    }

    private static List<UnitData> FinalizeRecipients(
        UnitData attacker,
        UnitData primaryTarget,
        List<UnitData> orderedSecondaries,
        bool includePrimary)
    {
        if (!primaryTarget.IsAlive || !includePrimary)
            return new List<UnitData>();

        var ordered = new List<UnitData> { primaryTarget };
        int recipientLimit = ResolveRecipientLimit(attacker.Attack.Selection.TargetLimit);
        if (recipientLimit == 1)
            return ordered;

        var seen = new HashSet<int> { primaryTarget.UnitId };
        foreach (var secondary in orderedSecondaries)
        {
            if (!secondary.IsAlive)
                continue;
            if (!seen.Add(secondary.UnitId))
                continue;

            ordered.Add(secondary);
            if (ordered.Count >= recipientLimit)
                break;
        }
        return ordered;
    }

    private static int ResolveRecipientLimit(int configuredLimit)
    {
        // 0 means unlimited in attack vector config.
        return configuredLimit <= 0 ? int.MaxValue : configuredLimit;
    }

    private static List<UnitData> EnumerateEnemyCandidates(UnitData attacker, MatchState state)
    {
        int enemyTeam = MatchState.GetEnemyTeam((int)attacker.Team);
        return state.GetAliveActiveUnitsForTeam(enemyTeam);
    }

    private static bool PassesLayerFilter(UnitData attacker, UnitData candidate)
    {
        return attacker.TargetLayerFilter switch
        {
            TargetLayer.GroundOnly => candidate.MovementLayer == MovementLayer.Ground,
            TargetLayer.AirOnly => candidate.MovementLayer == MovementLayer.Air,
            _ => true
        };
    }

    private static bool IsAreaMatch(UnitData attacker, UnitData primaryTarget, UnitData candidate)
    {
        return attacker.Attack.Area.Shape switch
        {
            AttackAreaShape.Sphere => IsInsideSphere(primaryTarget.Position, candidate.Position, attacker.Attack.Area.Size),
            AttackAreaShape.Box => IsInsideForwardBox(attacker, candidate.Position, attacker.Attack.Area.Size),
            AttackAreaShape.Capsule => IsInsideCapsule(attacker.Position, primaryTarget.Position, candidate.Position, attacker.Attack.Area.Size),
            AttackAreaShape.Line => IsInsideLineFromArea(attacker, primaryTarget, candidate.Position),
            _ => IsInsideSphere(primaryTarget.Position, candidate.Position, attacker.Attack.Area.Size)
        };
    }

    private static bool IsInsideSphere(SimVector3 center, SimVector3 point, SimVector3 size)
    {
        float radius = MathF.Max(size.X, 0f);
        return DistanceSquaredXZ(center, point) <= (radius * radius) + GeometryEpsilon;
    }

    private static bool IsInsideForwardBox(UnitData attacker, SimVector3 point, SimVector3 size)
    {
        float closeRadius = MathF.Max(attacker.EngageCloseRadius, 0f);
        if (closeRadius > 0f &&
            DistanceSquaredXZ(attacker.Position, point) <= (closeRadius * closeRadius) + GeometryEpsilon)
        {
            return true;
        }

        var forward = GetFacingDirection(attacker);
        var right = new SimVector3(-forward.Z, 0f, forward.X);
        var delta = point - attacker.Position;

        float forwardLength = size.X > 0f ? size.X : MathF.Max(attacker.AttackRange, 0.5f);
        float forwardOffset = MathF.Max(attacker.Attack.Area.ForwardOffset, 0f);
        float halfWidth = size.Z > 0f ? size.Z : 0.5f;
        float projectedForward = DotXZ(delta, forward);
        float projectedRight = MathF.Abs(DotXZ(delta, right));

        return projectedForward >= forwardOffset - GeometryEpsilon &&
               projectedForward <= (forwardOffset + forwardLength) + GeometryEpsilon &&
               projectedRight <= halfWidth + GeometryEpsilon;
    }

    private static bool IsInsideCapsule(
        SimVector3 segmentStart, SimVector3 segmentEnd, SimVector3 point, SimVector3 size)
    {
        float radius = size.Z > 0f ? size.Z : MathF.Max(size.X, 0f);
        if (radius <= 0f)
            return false;

        float distSq = PointToSegmentDistanceSqXZ(point, segmentStart, segmentEnd);
        return distSq <= (radius * radius) + GeometryEpsilon;
    }

    private static bool IsInsideLineFromArea(UnitData attacker, UnitData primaryTarget, SimVector3 point)
    {
        var line = BuildLineQuery(attacker, primaryTarget);
        return IsInsideLineCorridor(point, line.Origin, line.Direction, line.Length, line.HalfWidth);
    }

    private static (SimVector3 Origin, SimVector3 Direction, float Length, float HalfWidth) BuildLineQuery(
        UnitData attacker,
        UnitData primaryTarget)
    {
        var direction = GetDirectionFromAttacker(attacker, primaryTarget.Position);
        float forwardOffset = MathF.Max(attacker.Attack.Area.ForwardOffset, 0f);
        var origin = attacker.Position + (direction * forwardOffset);
        float targetDistance = MathF.Max(DistanceXZ(attacker.Position, primaryTarget.Position) - forwardOffset, 0f);
        float length = attacker.Attack.Area.LineLength > 0f
            ? attacker.Attack.Area.LineLength
            : targetDistance;
        if (length <= GeometryEpsilon)
            length = MathF.Max(attacker.AttackRange, 0.5f);
        float halfWidth = attacker.Attack.Area.LineHalfWidth > 0f
            ? attacker.Attack.Area.LineHalfWidth
            : 0.5f;

        return (origin, direction, length, halfWidth);
    }

    private static bool IsInsideLineCorridor(
        SimVector3 point,
        SimVector3 origin,
        SimVector3 direction,
        float length,
        float halfWidth)
    {
        float t = ProjectAlongLine(point, origin, direction);
        if (t < -GeometryEpsilon || t > length + GeometryEpsilon)
            return false;

        float distSq = DistanceSquaredFromLine(point, origin, direction);
        return distSq <= (halfWidth * halfWidth) + GeometryEpsilon;
    }

    private static SimVector3 GetFacingDirection(UnitData attacker)
    {
        return attacker.IsFacingRight
            ? new SimVector3(1f, 0f, 0f)
            : new SimVector3(-1f, 0f, 0f);
    }

    private static SimVector3 GetDirectionFromAttacker(UnitData attacker, SimVector3 targetPosition)
    {
        float dx = targetPosition.X - attacker.Position.X;
        float dz = targetPosition.Z - attacker.Position.Z;
        float lenSq = (dx * dx) + (dz * dz);
        if (lenSq <= GeometryEpsilon)
            return GetFacingDirection(attacker);

        float invLen = 1f / MathF.Sqrt(lenSq);
        return new SimVector3(dx * invLen, 0f, dz * invLen);
    }

    private static float DistanceXZ(SimVector3 a, SimVector3 b)
        => MathF.Sqrt(DistanceSquaredXZ(a, b));

    private static float DistanceSquaredXZ(SimVector3 a, SimVector3 b)
    {
        float dx = a.X - b.X;
        float dz = a.Z - b.Z;
        return (dx * dx) + (dz * dz);
    }

    private static float DotXZ(SimVector3 a, SimVector3 b)
        => (a.X * b.X) + (a.Z * b.Z);

    private static float ProjectAlongLine(SimVector3 point, SimVector3 origin, SimVector3 direction)
    {
        float dx = point.X - origin.X;
        float dz = point.Z - origin.Z;
        return (dx * direction.X) + (dz * direction.Z);
    }

    private static float DistanceSquaredFromLine(SimVector3 point, SimVector3 origin, SimVector3 direction)
    {
        float t = ProjectAlongLine(point, origin, direction);
        float closestX = origin.X + (direction.X * t);
        float closestZ = origin.Z + (direction.Z * t);
        float dx = point.X - closestX;
        float dz = point.Z - closestZ;
        return (dx * dx) + (dz * dz);
    }

    private static float PointToSegmentDistanceSqXZ(SimVector3 point, SimVector3 segA, SimVector3 segB)
    {
        float abX = segB.X - segA.X;
        float abZ = segB.Z - segA.Z;
        float abLenSq = (abX * abX) + (abZ * abZ);
        if (abLenSq <= GeometryEpsilon)
            return DistanceSquaredXZ(point, segA);

        float apX = point.X - segA.X;
        float apZ = point.Z - segA.Z;
        float t = SimMath.Clamp(((abX * apX) + (abZ * apZ)) / abLenSq, 0f, 1f);
        float closestX = segA.X + (abX * t);
        float closestZ = segA.Z + (abZ * t);
        float dx = point.X - closestX;
        float dz = point.Z - closestZ;
        return (dx * dx) + (dz * dz);
    }
}
