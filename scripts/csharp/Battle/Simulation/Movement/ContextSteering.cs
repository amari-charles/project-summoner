using System;
using System.Collections.Generic;
using Fateforged.Simulation.Combat;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Units;

namespace Fateforged.Simulation.Movement;

/// <summary>
/// 12-slot direction map used by context steering.
/// Each slot represents a 30° direction in the XZ plane.
/// Interest = "I want to go this way", Danger = "I must avoid this direction".
/// Final direction = weighted sum of interest slots masked by danger.
/// </summary>
public struct ContextMap
{
    public const int NumSlots = 12;
    private const float SlotAngle = 360f / NumSlots; // 30°

    public readonly float[] Interest;
    public readonly float[] Danger;

    private ContextMap(float[] interest, float[] danger)
    {
        Interest = interest;
        Danger = danger;
    }

    public static ContextMap Create(float[] interest, float[] danger)
    {
        if (interest.Length < NumSlots)
            throw new ArgumentException("Interest buffer is smaller than NumSlots.", nameof(interest));
        if (danger.Length < NumSlots)
            throw new ArgumentException("Danger buffer is smaller than NumSlots.", nameof(danger));
        return new ContextMap(interest, danger);
    }

    public void Clear()
    {
        Array.Clear(Interest, 0, NumSlots);
        Array.Clear(Danger, 0, NumSlots);
    }

    /// <summary>
    /// Resolve the best movement direction from interest masked by danger.
    /// Returns a normalized direction or Zero if no viable direction.
    /// </summary>
    public SimVector3 ResolveDirection()
    {
        // Weighted sum of interest directions, masking out slots where danger >= interest
        var result = SimVector3.Zero;
        float totalWeight = 0f;

        for (int i = 0; i < NumSlots; i++)
        {
            float interest = Interest[i];
            if (interest <= 0f) continue;

            // Mask: subtract danger from interest
            float effective = interest - Danger[i];
            if (effective <= 0f) continue;

            var dir = SlotDirection(i);
            result += dir * effective;
            totalWeight += effective;
        }

        if (totalWeight < 0.001f || result.LengthSquared() < 0.0001f)
            return SimVector3.Zero;

        return result.Normalized();
    }

    /// <summary>
    /// Get the slot index closest to a given XZ direction.
    /// </summary>
    public static int DirectionToSlot(SimVector3 dir)
    {
        float angle = MathF.Atan2(dir.Z, dir.X); // radians
        if (angle < 0) angle += SimMath.Tau;
        int slot = (int)MathF.Round(angle / SimMath.DegToRad(SlotAngle)) % NumSlots;
        return slot;
    }

    /// <summary>
    /// Cached unit direction vector for a given slot index.
    /// </summary>
    public static SimVector3 SlotDirection(int slotIndex)
    {
        float angle = SimMath.DegToRad(slotIndex * SlotAngle);
        return new SimVector3(MathF.Cos(angle), 0f, MathF.Sin(angle));
    }
}

/// <summary>
/// Context steering resolver — computes a preferred movement direction from desire profiles.
/// Each movement type (melee chase, ranged kite, forward march, strafe) fills
/// interest/danger maps differently. The resolver picks the best composite direction.
/// </summary>
public static class ContextSteering
{
    [ThreadStatic] private static float[]? _interestBuffer;
    [ThreadStatic] private static float[]? _dangerBuffer;
    [ThreadStatic] private static List<UnitData>? _crowdNeighbors;
    [ThreadStatic] private static List<float>? _crowdNeighborDistancesSq;
    private const float CrowdDangerRadiusMultiplier = 3.25f;
    private const float CrowdDangerMinRadius = 1.2f;
    private const float CrowdDangerFrontFloor = 0.35f;
    private const float CrowdDangerFrontWeight = 0.65f;
    private const float CrowdDangerSideBleed = 0.55f;
    private const int MaxCrowdNeighbors = 20;

    /// <summary>
    /// Main entry: compute preferred direction for a unit based on its behavior result.
    /// </summary>
    public static SimVector3 Resolve(
        UnitData unit, SimBehavior.BehaviorResult behavior, MatchState state)
    {
        _interestBuffer ??= new float[ContextMap.NumSlots];
        _dangerBuffer ??= new float[ContextMap.NumSlots];

        var map = ContextMap.Create(_interestBuffer, _dangerBuffer);
        map.Clear();

        switch (behavior.Movement)
        {
            case MovementResult.Forward:
                FillForwardProfile(unit, state, ref map);
                break;

            case MovementResult.TowardTarget:
            {
                var targetPos = ResolveTargetPosition(unit, behavior.MoveTargetId, state);
                if (!targetPos.HasValue)
                {
                    FillForwardProfile(unit, state, ref map);
                }
                else if (unit.UnitType == UnitType.Ranged)
                {
                    FillRangedProfile(unit, targetPos.Value, state, ref map);
                }
                else
                {
                    FillMeleeProfile(unit, targetPos.Value, state, ref map);
                }
                break;
            }

            case MovementResult.Strafe:
            {
                var targetPos = ResolveTargetPosition(unit, behavior.MoveTargetId, state);
                if (!targetPos.HasValue)
                    FillForwardProfile(unit, state, ref map);
                else
                    FillStrafeProfile(unit, targetPos.Value, state, ref map);
                break;
            }

            default:
                return SimVector3.Zero;
        }

        return map.ResolveDirection();
    }

    /// <summary>
    /// Melee profile: strong interest toward target position.
    /// Interest falls off with angular distance from target direction.
    /// </summary>
    private static void FillMeleeProfile(
        UnitData unit, SimVector3 targetPos, MatchState state, ref ContextMap map)
    {
        var toTarget = targetPos - unit.Position;
        toTarget.Y = 0;
        if (toTarget.LengthSquared() < 0.0625f)
        {
            AddCrowdDanger(unit, state, ref map, SimVector3.Zero);
            return; // Stop steering when within 0.25 units
        }

        var targetDir = toTarget.Normalized();

        // Fill interest: peak at target direction, linear cosine falloff
        for (int i = 0; i < ContextMap.NumSlots; i++)
        {
            var slotDir = ContextMap.SlotDirection(i);
            float dot = targetDir.Dot(slotDir);

            // Interest only in the forward hemisphere toward target
            if (dot > 0f)
            {
                map.Interest[i] = dot;
            }
        }

        AddCrowdDanger(unit, state, ref map, targetDir);
    }

    /// <summary>
    /// Ranged profile: interest toward target but also comfortable at range.
    /// If already within attack range, less interest in closing further.
    /// </summary>
    private static void FillRangedProfile(
        UnitData unit, SimVector3 targetPos, MatchState state, ref ContextMap map)
    {
        // Ranged units chasing a target behave like melee — they need to get in range
        FillMeleeProfile(unit, targetPos, state, ref map);
    }

    /// <summary>
    /// Forward profile: march toward enemy base.
    /// Team 0 moves in +X, Team 1 moves in -X.
    /// </summary>
    private static void FillForwardProfile(
        UnitData unit, MatchState state, ref ContextMap map)
    {
        float direction = unit.Team == Team.Player ? 1.0f : -1.0f;
        var forwardDir = new SimVector3(direction, 0f, 0f);

        // Strong interest in the forward direction, mild spread
        for (int i = 0; i < ContextMap.NumSlots; i++)
        {
            var slotDir = ContextMap.SlotDirection(i);
            float dot = forwardDir.Dot(slotDir);
            if (dot > 0f)
                map.Interest[i] = dot;
        }

        AddCrowdDanger(unit, state, ref map, forwardDir);
    }

    /// <summary>
    /// Strafe profile: perpendicular movement around target.
    /// Used when unit has cone constraint and needs to reposition.
    /// </summary>
    private static void FillStrafeProfile(
        UnitData unit, SimVector3 targetPos, MatchState state, ref ContextMap map)
    {
        var toTarget = targetPos - unit.Position;
        var horizontalToTarget = new SimVector3(toTarget.X, 0f, toTarget.Z);
        if (horizontalToTarget.LengthSquared() < 0.0001f)
        {
            AddCrowdDanger(unit, state, ref map, SimVector3.Zero);
            return;
        }

        horizontalToTarget = horizontalToTarget.Normalized();

        // Calculate angle to target for facing decision
        float angleToTarget = SimMath.RadToDeg(MathF.Atan2(horizontalToTarget.Z, horizontalToTarget.X));
        bool shouldFaceRight = MathF.Abs(angleToTarget) <= 90f;

        // Calculate angle difference from facing
        float facingAngle = shouldFaceRight ? 0f : 180f;
        float angleDiff = angleToTarget - facingAngle;
        while (angleDiff > 180f) angleDiff -= 360f;
        while (angleDiff < -180f) angleDiff += 360f;

        // Perpendicular to target direction
        var strafeDir = new SimVector3(-horizontalToTarget.Z, 0f, horizontalToTarget.X);
        // Choose direction that reduces angle difference
        if (angleDiff < 0)
            strafeDir = -strafeDir;

        strafeDir = strafeDir.Normalized();

        // Fill interest toward strafe direction
        for (int i = 0; i < ContextMap.NumSlots; i++)
        {
            var slotDir = ContextMap.SlotDirection(i);
            float dot = strafeDir.Dot(slotDir);
            if (dot > 0f)
                map.Interest[i] = dot;
        }

        AddCrowdDanger(unit, state, ref map, strafeDir);
    }

    private static void AddCrowdDanger(
        UnitData unit, MatchState state, ref ContextMap map, SimVector3 preferredDirection)
    {
        float dangerRadius = MathF.Max(CrowdDangerMinRadius, unit.SeparationRadius * CrowdDangerRadiusMultiplier);
        _crowdNeighbors ??= new List<UnitData>(MaxCrowdNeighbors);
        _crowdNeighborDistancesSq ??= new List<float>(MaxCrowdNeighbors);
        MovementNeighborQuery.FillNearestNeighbors(
            unit,
            state,
            dangerRadius,
            MaxCrowdNeighbors,
            _crowdNeighbors,
            _crowdNeighborDistancesSq
        );

        bool hasPreferredDirection = preferredDirection.LengthSquared() >= 0.0001f;
        var preferredDir = hasPreferredDirection ? preferredDirection.Normalized() : SimVector3.Zero;

        foreach (var other in _crowdNeighbors)
        {
            var toNeighbor = other.Position - unit.Position;
            toNeighbor.Y = 0f;
            float distSq = toNeighbor.LengthSquared();
            if (distSq <= 0.000001f) continue;

            float distance = MathF.Sqrt(distSq);
            var neighborDir = toNeighbor / distance;
            float proximity = 1f - (distance / dangerRadius);
            if (proximity <= 0f) continue;

            float frontBias = 1f;
            if (hasPreferredDirection)
            {
                float alignment = MathF.Max(0f, preferredDir.Dot(neighborDir));
                frontBias = MathF.Max(CrowdDangerFrontFloor, CrowdDangerFrontFloor + alignment * CrowdDangerFrontWeight);
            }

            float danger = proximity * frontBias;
            int slot = ContextMap.DirectionToSlot(neighborDir);
            if (danger > map.Danger[slot])
                map.Danger[slot] = danger;

            float sideDanger = danger * CrowdDangerSideBleed;
            int leftSlot = (slot + ContextMap.NumSlots - 1) % ContextMap.NumSlots;
            int rightSlot = (slot + 1) % ContextMap.NumSlots;

            if (sideDanger > map.Danger[leftSlot])
                map.Danger[leftSlot] = sideDanger;
            if (sideDanger > map.Danger[rightSlot])
                map.Danger[rightSlot] = sideDanger;
        }
    }

    private static SimVector3? ResolveTargetPosition(UnitData unit, int? targetId, MatchState state)
    {
        return MovementTargetResolver.Resolve(unit, targetId, state);
    }
}
