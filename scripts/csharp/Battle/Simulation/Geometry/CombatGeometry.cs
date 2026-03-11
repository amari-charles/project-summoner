using System;
using Fateforged.Projectiles;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;

namespace Fateforged.Simulation.Geometry;

/// <summary>
/// Shared combat-geometry helpers.
/// Pass 2 wiring keeps legacy behavior by default while exposing split
/// channels for movement footprint (navigation) and combat contact (hurtbox).
/// </summary>
public static class CombatGeometry
{
    private const float DefaultRadius = 0.5f;
    private const float GeometryEpsilon = 0.000001f;

    public static float GetNavigationRadius(UnitData unit)
    {
        if (unit.NavigationRadius > 0f)
            return unit.NavigationRadius;
        if (unit.SeparationRadius > 0f)
            return unit.SeparationRadius;
        return DefaultRadius;
    }

    public static float GetHurtboxRadius(UnitData unit)
    {
        if (unit.HurtboxRadius > 0f)
            return unit.HurtboxRadius;
        return GetNavigationRadius(unit);
    }

    public static bool UseGroundCylinder(ProjectileHitSpace hitSpace, UnitData unit)
    {
        if (hitSpace == ProjectileHitSpace.Sphere3D)
            return false;
        return unit.MovementLayer == Units.MovementLayer.Ground;
    }

    public static bool CanHitUnitInRadius(
        ProjectileHitSpace hitSpace, UnitData unit, SimVector3 center, float radius)
    {
        float radiusSq = radius * radius;
        return UseGroundCylinder(hitSpace, unit)
            ? DistanceSquaredXZ(center, unit.Position) <= radiusSq
            : center.DistanceSquaredTo(unit.Position) <= radiusSq;
    }

    public static bool TryGetSegmentDistanceAndT(
        ProjectileHitSpace hitSpace,
        UnitData unit,
        SimVector3 segA,
        SimVector3 segB,
        out float distanceSq,
        out float segmentT)
    {
        if (UseGroundCylinder(hitSpace, unit))
            return TryGetPointToSegmentDistanceSqXZ(unit.Position, segA, segB, out distanceSq, out segmentT);
        return TryGetPointToSegmentDistanceSq(unit.Position, segA, segB, out distanceSq, out segmentT);
    }

    public static float DistanceSquaredXZ(SimVector3 a, SimVector3 b)
    {
        float dx = a.X - b.X;
        float dz = a.Z - b.Z;
        return (dx * dx) + (dz * dz);
    }

    public static bool TryGetPointToSegmentDistanceSq(
        SimVector3 point,
        SimVector3 segA,
        SimVector3 segB,
        out float distanceSq,
        out float segmentT)
    {
        var ab = segB - segA;
        float abLenSq = ab.LengthSquared();
        if (abLenSq < GeometryEpsilon)
        {
            segmentT = 0f;
            distanceSq = point.DistanceSquaredTo(segA);
            return true;
        }

        segmentT = SimMath.Clamp(ab.Dot(point - segA) / abLenSq, 0f, 1f);
        var closest = segA + ab * segmentT;
        distanceSq = closest.DistanceSquaredTo(point);
        return true;
    }

    public static bool TryGetPointToSegmentDistanceSqXZ(
        SimVector3 point,
        SimVector3 segA,
        SimVector3 segB,
        out float distanceSq,
        out float segmentT)
    {
        float abX = segB.X - segA.X;
        float abZ = segB.Z - segA.Z;
        float abLenSq = (abX * abX) + (abZ * abZ);
        if (abLenSq < GeometryEpsilon)
        {
            segmentT = 0f;
            float dx0 = point.X - segA.X;
            float dz0 = point.Z - segA.Z;
            distanceSq = (dx0 * dx0) + (dz0 * dz0);
            return true;
        }

        float apX = point.X - segA.X;
        float apZ = point.Z - segA.Z;
        segmentT = SimMath.Clamp(((abX * apX) + (abZ * apZ)) / abLenSq, 0f, 1f);

        float closestX = segA.X + abX * segmentT;
        float closestZ = segA.Z + abZ * segmentT;
        float dx = point.X - closestX;
        float dz = point.Z - closestZ;
        distanceSq = (dx * dx) + (dz * dz);
        return true;
    }
}
