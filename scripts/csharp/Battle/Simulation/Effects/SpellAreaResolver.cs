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
        float radius,
        SimVector3? origin = null
    )
    {
        if (radius <= 0f)
            return candidate.DistanceSquaredTo(center) <= 0f;

        switch (areaShape)
        {
            case SpellAreaShape.Square:
                return MathF.Abs(candidate.X - center.X) <= radius
                    && MathF.Abs(candidate.Z - center.Z) <= radius;

            case SpellAreaShape.Line:
                var lineOrigin = origin ?? center;
                float endpointLength = DistanceXZ(lineOrigin, center);
                return IsInsideLine(
                    lineOrigin,
                    center,
                    candidate,
                    MathF.Max(radius, endpointLength),
                    1.25f
                );

            case SpellAreaShape.Cone:
                return IsInsideCone(origin ?? center, center, candidate, radius, 45f);

            default:
                return candidate.DistanceSquaredTo(center) <= radius * radius;
        }
    }

    private static bool IsInsideLine(
        SimVector3 origin,
        SimVector3 target,
        SimVector3 candidate,
        float length,
        float halfWidth
    )
    {
        var direction = NormalizeXZ(target - origin);
        if (direction.LengthSquared() <= 0.0001f)
            direction = new SimVector3(1f, 0f, 0f);

        var rel = candidate - origin;
        float projected = rel.X * direction.X + rel.Z * direction.Z;
        if (projected < 0f || projected > length)
            return false;

        float perpX = rel.X - direction.X * projected;
        float perpZ = rel.Z - direction.Z * projected;
        return (perpX * perpX + perpZ * perpZ) <= halfWidth * halfWidth;
    }

    private static float DistanceXZ(SimVector3 a, SimVector3 b)
    {
        float dx = b.X - a.X;
        float dz = b.Z - a.Z;
        return MathF.Sqrt(dx * dx + dz * dz);
    }

    private static bool IsInsideCone(
        SimVector3 origin,
        SimVector3 target,
        SimVector3 candidate,
        float radius,
        float halfAngleDegrees
    )
    {
        var direction = NormalizeXZ(target - origin);
        if (direction.LengthSquared() <= 0.0001f)
            direction = new SimVector3(1f, 0f, 0f);

        var rel = candidate - origin;
        float distSq = rel.X * rel.X + rel.Z * rel.Z;
        if (distSq > radius * radius)
            return false;
        if (distSq <= 0.0001f)
            return true;

        float invDist = 1f / MathF.Sqrt(distSq);
        float dot = direction.X * rel.X * invDist + direction.Z * rel.Z * invDist;
        float minDot = MathF.Cos(halfAngleDegrees * MathF.PI / 180f);
        return dot >= minDot;
    }

    private static SimVector3 NormalizeXZ(SimVector3 value)
    {
        float lengthSq = value.X * value.X + value.Z * value.Z;
        if (lengthSq <= 0.0001f)
            return SimVector3.Zero;
        float inv = 1f / MathF.Sqrt(lengthSq);
        return new SimVector3(value.X * inv, 0f, value.Z * inv);
    }
}
