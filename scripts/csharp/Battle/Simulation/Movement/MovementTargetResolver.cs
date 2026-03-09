using System;
using Fateforged.Simulation.Data;
using Fateforged.Units;

namespace Fateforged.Simulation.Movement;

/// <summary>
/// Resolves movement-target positions.
/// For summoner targets, returns an orbit point so units can wrap around
/// blocked fronts instead of funneling into a single lane.
/// </summary>
public static class MovementTargetResolver
{
    private const float MinOrbitRadius = 1.2f;
    private const float OrbitPadding = 0.35f;
    private const int MinOrbitSlots = 10;
    private const int MaxOrbitSlots = 24;
    private const float BlockedOrbitThresholdSeconds = 0.18f;
    private const float OrbitFallbackDistanceSq = 0.01f;

    public static SimVector3? Resolve(UnitData unit, int? targetId, MatchState state)
    {
        var baseTargetPosition = SimUtils.ResolveTargetPosition(targetId, state);
        if (!baseTargetPosition.HasValue)
            return null;

        if (!MatchState.IsSummonerTarget(targetId))
            return baseTargetPosition.Value;

        return ResolveSummonerOrbitPoint(unit, baseTargetPosition.Value);
    }

    private static SimVector3 ResolveSummonerOrbitPoint(UnitData unit, SimVector3 summonerPosition)
    {
        float orbitRadius = MathF.Max(
            MinOrbitRadius,
            unit.AttackRange + unit.SeparationRadius + OrbitPadding
        );
        int slotCount = ComputeSlotCount(orbitRadius, unit.SeparationRadius);
        int frontSlot = AngleToSlot(unit.Team == Team.Player ? MathF.PI : 0f, slotCount);
        bool shouldWrap =
            unit.NavigationBlockedTime >= BlockedOrbitThresholdSeconds ||
            unit.NavigationYieldTimer > 0f ||
            unit.NavigationEscapeTimer > 0f;

        int selectedSlot;
        if (shouldWrap)
        {
            int signedOffset = ComputeSignedOrbitOffset(unit.UnitId, slotCount);
            if (unit.NavigationEscapeDirectionSign < 0)
                signedOffset = -signedOffset;

            selectedSlot = PositiveModulo(frontSlot + signedOffset, slotCount);
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

    private static int ComputeSlotCount(float orbitRadius, float separationRadius)
    {
        float spacing = MathF.Max(0.4f, separationRadius * 2.0f);
        float circumference = SimMath.Tau * orbitRadius;
        int slots = (int)MathF.Ceiling(circumference / spacing);
        return Math.Clamp(slots, MinOrbitSlots, MaxOrbitSlots);
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
