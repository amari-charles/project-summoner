using System;
using System.Collections.Generic;
using Fateforged.Simulation;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Geometry;

namespace Fateforged.Simulation.Combat.Slots;

/// <summary>
/// Target-owned melee slot authority for commit-slot combat.
/// </summary>
public static class SimMeleeSlotManager
{
    private const float SlotSpacingMin = 0.9f;
    private const int MinSlotsDefault = 3;
    private const float FrontShare = 0.60f;
    private const float SideShare = 0.30f;
    private const float AxisRefreshAngleDeg = 30f;
    private const float AxisRefreshDisplacementRadiusScale = 0.5f;
    private const float SummonerTargetRadius = 1.8f;
    private const int SummonerMinSlots = 12;

    public static TargetSlotState GetOrCreateTargetState(
        MatchState state,
        int targetId,
        UnitData attacker,
        int minSlots = MinSlotsDefault)
    {
        if (!state.TargetSlotStates.TryGetValue(targetId, out var slotState))
        {
            slotState = new TargetSlotState { TargetId = targetId };
            state.TargetSlotStates[targetId] = slotState;
        }

        EnsureSlotTopology(slotState, targetId, state, attacker, minSlots);
        RefreshLayoutAxis(slotState, targetId, state, attacker.Position);
        return slotState;
    }

    public static bool TryReserveSlot(
        UnitData unit,
        MatchState state,
        int targetId,
        out int reservedSlotId,
        int minSlots = MinSlotsDefault)
    {
        reservedSlotId = -1;
        var slotState = GetOrCreateTargetState(state, targetId, unit, minSlots);

        int bestSlot = -1;
        float bestDistSq = float.MaxValue;
        foreach (var slot in slotState.Slots)
        {
            if (slot.OccupancyState != SlotOccupancyState.Free)
                continue;

            var worldPos = ResolveSlotWorldPosition(targetId, slotState.LayoutAxis, slot.SlotOffset, state);
            if (!worldPos.HasValue)
                continue;

            float distSq = DistanceSquaredXZ(unit.Position, worldPos.Value);
            if (distSq > bestDistSq)
                continue;

            if (MathF.Abs(distSq - bestDistSq) <= 0.0001f && slot.SlotId > bestSlot)
                continue;

            bestDistSq = distSq;
            bestSlot = slot.SlotId;
        }

        if (bestSlot < 0)
            return false;

        var entry = slotState.Slots[bestSlot];
        entry.OccupancyState = SlotOccupancyState.Reserved;
        entry.ReservedUnitId = unit.UnitId;
        entry.ReservationDistanceSq = bestDistSq;
        entry.ReservationUnitId = unit.UnitId;

        unit.SlotTargetId = targetId;
        unit.ReservedSlotId = bestSlot;
        unit.LastReservationDistanceSq = bestDistSq;
        reservedSlotId = bestSlot;
        return true;
    }

    public static void SetOccupied(UnitData unit, MatchState state)
    {
        if (!unit.SlotTargetId.HasValue || !unit.ReservedSlotId.HasValue)
            return;
        if (!state.TargetSlotStates.TryGetValue(unit.SlotTargetId.Value, out var slotState))
            return;
        if (unit.ReservedSlotId.Value < 0 || unit.ReservedSlotId.Value >= slotState.Slots.Count)
            return;

        var entry = slotState.Slots[unit.ReservedSlotId.Value];
        entry.ReservedUnitId = unit.UnitId;
        entry.OccupiedUnitId = unit.UnitId;
        entry.OccupancyState = SlotOccupancyState.Occupied;
        unit.OccupiedSlotId = unit.ReservedSlotId;
    }

    public static void ReleaseUnitSlots(UnitData unit, MatchState state)
    {
        if (!unit.SlotTargetId.HasValue || !state.TargetSlotStates.TryGetValue(unit.SlotTargetId.Value, out var slotState))
        {
            ClearUnitSlotRefs(unit);
            return;
        }

        foreach (var entry in slotState.Slots)
        {
            if (entry.ReservedUnitId == unit.UnitId)
                entry.ReservedUnitId = null;
            if (entry.OccupiedUnitId == unit.UnitId)
                entry.OccupiedUnitId = null;

            if (!entry.ReservedUnitId.HasValue && !entry.OccupiedUnitId.HasValue)
            {
                entry.OccupancyState = SlotOccupancyState.Free;
                entry.ReservationDistanceSq = float.MaxValue;
                entry.ReservationUnitId = int.MaxValue;
            }
            else if (entry.OccupiedUnitId.HasValue)
            {
                entry.OccupancyState = SlotOccupancyState.Occupied;
            }
            else
            {
                entry.OccupancyState = SlotOccupancyState.Reserved;
            }
        }

        ClearUnitSlotRefs(unit);
    }

    public static SimVector3? GetReservedSlotWorldPosition(UnitData unit, MatchState state)
    {
        if (!unit.SlotTargetId.HasValue || !unit.ReservedSlotId.HasValue)
            return null;
        if (!state.TargetSlotStates.TryGetValue(unit.SlotTargetId.Value, out var slotState))
            return null;
        if (unit.ReservedSlotId.Value < 0 || unit.ReservedSlotId.Value >= slotState.Slots.Count)
            return null;

        RefreshLayoutAxis(slotState, unit.SlotTargetId.Value, state, unit.Position);
        var slot = slotState.Slots[unit.ReservedSlotId.Value];
        return ResolveSlotWorldPosition(unit.SlotTargetId.Value, slotState.LayoutAxis, slot.SlotOffset, state);
    }

    public static float ResolveSlotArrivalDistance(UnitData unit)
    {
        float radius = CombatGeometry.GetNavigationRadius(unit);
        return MathF.Max(0.10f, radius * 0.65f);
    }

    private static void EnsureSlotTopology(
        TargetSlotState slotState,
        int targetId,
        MatchState state,
        UnitData attacker,
        int minSlots)
    {
        float targetRadius = ResolveTargetRadius(targetId, state);
        float attackerRadius = MathF.Max(0.1f, CombatGeometry.GetNavigationRadius(attacker));
        float slotSpacing = MathF.Max(SlotSpacingMin, attackerRadius * 2f);
        int computedSlots = (int)MathF.Floor((SimMath.Tau * targetRadius) / slotSpacing);
        int slotCount = Math.Max(Math.Max(minSlots, 1), computedSlots);
        if (MatchState.IsSummonerTarget(targetId))
            slotCount = Math.Max(slotCount, SummonerMinSlots);

        if (slotState.Slots.Count == slotCount)
            return;

        // Rebuild topology deterministically.
        slotState.Slots.Clear();
        float desiredOrbitRadius = MathF.Max(targetRadius + (attackerRadius * 0.9f), 0.2f);
        if (attacker.EngageShape == EngageShape.ForwardRect && attacker.EngageRectForwardOffset > 0f)
        {
            // Forward-rect units with positive forward offset must reserve slots far enough
            // ahead that their engage shape can actually include the target.
            desiredOrbitRadius = MathF.Max(desiredOrbitRadius, attacker.EngageRectForwardOffset + 0.05f);
        }
        // Slots must sit within practical attack reach so reserved attackers can
        // actually enter attack loop (important for large targets like summoners).
        float maxReachableOrbitRadius = MathF.Max(0.2f, attacker.AttackRange * 0.92f);
        float orbitRadius = MathF.Min(desiredOrbitRadius, maxReachableOrbitRadius);
        var offsets = BuildSlotOffsets(slotCount, orbitRadius);
        for (int i = 0; i < offsets.Count; i++)
        {
            slotState.Slots.Add(new MeleeSlotEntry
            {
                SlotId = i,
                SlotOffset = offsets[i]
            });
        }
    }

    private static List<SimVector3> BuildSlotOffsets(int slotCount, float radius)
    {
        var angles = new List<float>(slotCount);

        // Small slot sets should stay fully attacker-facing to avoid
        // "run past target" paths in 2-4 unit melee engagements.
        if (slotCount <= 4)
        {
            AppendArcAngles(angles, -70f, 70f, slotCount);
            return BuildOffsetsFromAngles(angles, radius);
        }

        int frontCount = Math.Max(1, (int)MathF.Round(slotCount * FrontShare));
        int sideCount = Math.Max(0, (int)MathF.Round(slotCount * SideShare));
        int rearCount = Math.Max(0, slotCount - frontCount - sideCount);

        AppendArcAngles(angles, -60f, 60f, frontCount);

        int sideLeft = (sideCount + 1) / 2;
        int sideRight = sideCount / 2;
        AppendArcAngles(angles, 95f, 145f, sideLeft);
        AppendArcAngles(angles, -95f, -145f, sideRight);

        AppendArcAngles(angles, 160f, 200f, rearCount);

        while (angles.Count < slotCount)
            angles.Add(180f);
        if (angles.Count > slotCount)
            angles.RemoveRange(slotCount, angles.Count - slotCount);

        return BuildOffsetsFromAngles(angles, radius);
    }

    private static List<SimVector3> BuildOffsetsFromAngles(List<float> angles, float radius)
    {
        var result = new List<SimVector3>(angles.Count);
        foreach (float angleDeg in angles)
        {
            float angleRad = SimMath.DegToRad(angleDeg);
            result.Add(new SimVector3(MathF.Cos(angleRad) * radius, 0f, MathF.Sin(angleRad) * radius));
        }

        return result;
    }

    private static void AppendArcAngles(List<float> angles, float startDeg, float endDeg, int count)
    {
        if (count <= 0)
            return;
        if (count == 1)
        {
            angles.Add((startDeg + endDeg) * 0.5f);
            return;
        }

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)(count - 1);
            angles.Add(startDeg + ((endDeg - startDeg) * t));
        }
    }

    private static void RefreshLayoutAxis(
        TargetSlotState slotState,
        int targetId,
        MatchState state,
        SimVector3 fallbackAttackerPosition)
    {
        var targetPosOpt = SimUtils.ResolveTargetPosition(targetId, state);
        if (!targetPosOpt.HasValue)
            return;

        var targetPos = targetPosOpt.Value;
        float targetRadius = ResolveTargetRadius(targetId, state);

        bool hasCentroid = TryGetAttackerCentroid(slotState, state, out var centroid);
        if (!hasCentroid)
            centroid = fallbackAttackerPosition;

        // Front slots are authored around +X in local space.
        // Layout axis must point from target toward attackers so front slots stay
        // on the attacker-facing side instead of flipping behind the target.
        var desired = centroid - targetPos;
        desired.Y = 0f;
        if (desired.LengthSquared() < 0.000001f)
            return;
        desired = desired.Normalized();

        var current = slotState.LayoutAxis;
        current.Y = 0f;
        if (current.LengthSquared() < 0.000001f)
            current = desired;
        else
            current = current.Normalized();

        float dot = Math.Clamp(current.Dot(desired), -1f, 1f);
        float angleDeg = SimMath.RadToDeg(MathF.Acos(dot));
        float displacement = DistanceXZ(targetPos, slotState.LastAnchorPosition);
        float displacementThreshold = MathF.Max(0.1f, targetRadius * AxisRefreshDisplacementRadiusScale);

        bool firstInit = slotState.LastAnchorPosition.LengthSquared() < 0.000001f;
        if (firstInit || angleDeg > AxisRefreshAngleDeg || displacement > displacementThreshold)
        {
            slotState.LayoutAxis = desired;
            slotState.LastAnchorPosition = targetPos;
            slotState.LastAxisRefreshTime = state.MatchTime;
        }
    }

    private static bool TryGetAttackerCentroid(TargetSlotState slotState, MatchState state, out SimVector3 centroid)
    {
        centroid = SimVector3.Zero;
        int count = 0;

        foreach (var slot in slotState.Slots)
        {
            if (slot.ReservedUnitId.HasValue)
            {
                var unit = state.GetAliveUnit(slot.ReservedUnitId.Value);
                if (unit != null)
                {
                    centroid += unit.Position;
                    count++;
                }
            }

            if (slot.OccupiedUnitId.HasValue && slot.OccupiedUnitId != slot.ReservedUnitId)
            {
                var unit = state.GetAliveUnit(slot.OccupiedUnitId.Value);
                if (unit != null)
                {
                    centroid += unit.Position;
                    count++;
                }
            }
        }

        if (count <= 0)
            return false;

        centroid /= count;
        return true;
    }

    private static SimVector3? ResolveSlotWorldPosition(int targetId, SimVector3 layoutAxis, SimVector3 slotOffset, MatchState state)
    {
        var targetPos = SimUtils.ResolveTargetPosition(targetId, state);
        if (!targetPos.HasValue)
            return null;

        var axis = layoutAxis;
        axis.Y = 0f;
        if (axis.LengthSquared() < 0.000001f)
            axis = new SimVector3(1f, 0f, 0f);
        else
            axis = axis.Normalized();

        float yaw = MathF.Atan2(axis.Z, axis.X);
        float cos = MathF.Cos(yaw);
        float sin = MathF.Sin(yaw);

        float rotatedX = (slotOffset.X * cos) - (slotOffset.Z * sin);
        float rotatedZ = (slotOffset.X * sin) + (slotOffset.Z * cos);

        var pos = targetPos.Value;
        return new SimVector3(pos.X + rotatedX, pos.Y + slotOffset.Y, pos.Z + rotatedZ);
    }

    private static float ResolveTargetRadius(int targetId, MatchState state)
    {
        if (MatchState.IsSummonerTarget(targetId))
            return SummonerTargetRadius;

        var target = state.GetAliveUnit(targetId);
        if (target == null)
            return SummonerTargetRadius;

        float navigation = CombatGeometry.GetNavigationRadius(target);
        return MathF.Max(0.2f, navigation);
    }

    private static float DistanceXZ(SimVector3 a, SimVector3 b)
    {
        float dx = a.X - b.X;
        float dz = a.Z - b.Z;
        return MathF.Sqrt((dx * dx) + (dz * dz));
    }

    private static float DistanceSquaredXZ(SimVector3 a, SimVector3 b)
    {
        float dx = a.X - b.X;
        float dz = a.Z - b.Z;
        return (dx * dx) + (dz * dz);
    }

    private static void ClearUnitSlotRefs(UnitData unit)
    {
        unit.SlotTargetId = null;
        unit.ReservedSlotId = null;
        unit.OccupiedSlotId = null;
    }
}
