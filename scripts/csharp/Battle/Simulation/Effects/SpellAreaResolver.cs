using System;
using Fateforged.Simulation.Enums;

namespace Fateforged.Simulation.Effects;

/// <summary>
/// Canonical spell/delayed-effect area inclusion checks.
/// </summary>
public static class SpellAreaResolver
{
    public static bool IsWithinArea(
        SpellAreaShape areaShape,
        SimVector3 center,
        SimVector3 candidate,
        float radius
    )
    {
        if (radius <= 0f)
            return candidate.DistanceSquaredTo(center) <= 0f;

        if (areaShape == SpellAreaShape.Square)
        {
            return MathF.Abs(candidate.X - center.X) <= radius
                && MathF.Abs(candidate.Z - center.Z) <= radius;
        }

        return candidate.DistanceSquaredTo(center) <= radius * radius;
    }
}
