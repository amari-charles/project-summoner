using System;
using Fateforged.Constants;

namespace Fateforged.Simulation.Spatial;

/// <summary>
/// Virtual lane helpers for simulation-only behavior shaping.
/// Lanes are logical partitions of the battlefield depth (Z axis) with no physical barriers.
/// </summary>
public static class VirtualLanes
{
    public const int LaneCount = 3;
    public const int CenterLane = 1;

    private static float LaneWidth => (BattlefieldBounds.MaxZ - BattlefieldBounds.MinZ) / LaneCount;

    public static int GetLaneIndex(float z)
    {
        float normalized = (z - BattlefieldBounds.MinZ) / LaneWidth;
        int lane = (int)MathF.Floor(normalized);
        return Math.Clamp(lane, 0, LaneCount - 1);
    }

    public static float GetLaneCenterZ(int lane)
    {
        lane = Math.Clamp(lane, 0, LaneCount - 1);
        return BattlefieldBounds.MinZ + (lane + 0.5f) * LaneWidth;
    }

    public static bool IsSideLane(int lane)
        => lane >= 0 && lane < LaneCount && lane != CenterLane;

    public static int LaneDistance(int a, int b)
        => Math.Abs(a - b);
}
