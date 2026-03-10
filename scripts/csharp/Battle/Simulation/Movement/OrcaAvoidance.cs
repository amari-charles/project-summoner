using System;
using System.Collections.Generic;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Units;

namespace Fateforged.Simulation.Movement;

/// <summary>
/// Half-plane constraint in 2D velocity space (XZ plane).
/// Defines a boundary line: velocities on the left side of (Point + t*Direction) are safe.
/// </summary>
public struct OrcaLine
{
    public float PointX, PointZ;
    public float DirectionX, DirectionZ;
}

/// <summary>
/// ORCA (Optimal Reciprocal Collision Avoidance) velocity obstacle solver.
/// Takes a preferred velocity and returns the closest safe velocity that avoids
/// collisions with nearby units. Uses the RVO2 incremental linear programming approach.
/// </summary>
public static class OrcaAvoidance
{
    private const float TimeHorizon = 1.5f;
    private const int MaxNeighbors = 16;
    private const float NeighborSearchRadiusMultiplier = 6.0f;
    private const float Epsilon = 0.00001f;
    private const float OverlapInvDelta = 60f; // Assumes 60fps fixed timestep
    private const float GoldenAngle = 2.39996f; // 2π/φ² — spreads overlapping units evenly

    // Reusable list to avoid allocations per frame
    [ThreadStatic] private static List<OrcaLine>? _orcaLines;
    [ThreadStatic] private static List<UnitData>? _neighbors;
    [ThreadStatic] private static List<float>? _neighborDistancesSq;

    /// <summary>
    /// Compute the closest safe velocity to preferredVelocity that avoids collisions.
    /// </summary>
    public static SimVector3 ComputeSafeVelocity(
        UnitData unit, SimVector3 preferredVelocity, MatchState state)
    {
        _orcaLines ??= new List<OrcaLine>(MaxNeighbors);
        _neighbors ??= new List<UnitData>(MaxNeighbors);
        _neighborDistancesSq ??= new List<float>(MaxNeighbors);

        _orcaLines.Clear();
        _neighbors.Clear();
        _neighborDistancesSq.Clear();

        float searchRadius = unit.SeparationRadius * NeighborSearchRadiusMultiplier;
        MovementNeighborQuery.FillNearestNeighbors(
            unit,
            state,
            searchRadius,
            MaxNeighbors,
            _neighbors,
            _neighborDistancesSq,
            sortByDistance: false,
            includeBoundary: false
        );

        float invTimeHorizon = 1.0f / TimeHorizon;

        // Build ORCA half-plane constraints
        foreach (var neighbor in _neighbors)
        {
            var line = ComputeOrcaLine(unit, neighbor, invTimeHorizon);
            _orcaLines.Add(line);
        }

        // Solve: find closest velocity to preferred that satisfies all constraints
        float maxSpeed = new SimVector3(preferredVelocity.X, 0, preferredVelocity.Z).Length();
        if (maxSpeed < 0.001f)
            maxSpeed = 0.001f;

        float vx = preferredVelocity.X;
        float vz = preferredVelocity.Z;

        if (_orcaLines.Count > 0)
        {
            if (!LinearProgram2(_orcaLines, maxSpeed, ref vx, ref vz))
            {
                LinearProgram3(_orcaLines, maxSpeed, ref vx, ref vz);
            }
        }

        return new SimVector3(vx, 0f, vz);
    }

    /// <summary>
    /// Build one ORCA half-plane constraint for a unit-neighbor pair.
    /// The constraint represents the boundary between safe and unsafe velocities.
    /// </summary>
    private static OrcaLine ComputeOrcaLine(
        UnitData unit, UnitData neighbor, float invTimeHorizon)
    {
        // Relative position and velocity (in XZ)
        float relPosX = neighbor.Position.X - unit.Position.X;
        float relPosZ = neighbor.Position.Z - unit.Position.Z;
        float relVelX = unit.Velocity.X - neighbor.Velocity.X;
        float relVelZ = unit.Velocity.Z - neighbor.Velocity.Z;

        float distSq = relPosX * relPosX + relPosZ * relPosZ;
        float combinedRadius = unit.SeparationRadius + neighbor.SeparationRadius;
        float combinedRadiusSq = combinedRadius * combinedRadius;

        float avoidanceWeight = GetAvoidanceWeight(unit, neighbor);

        OrcaLine line;

        if (distSq > combinedRadiusSq)
        {
            // No collision — project velocity onto the velocity obstacle boundary
            float cutoffX = relPosX * invTimeHorizon - relVelX;
            float cutoffZ = relPosZ * invTimeHorizon - relVelZ;

            // Vector from cutoff center to relative velocity
            float wX = relVelX - relPosX * invTimeHorizon;
            float wZ = relVelZ - relPosZ * invTimeHorizon;
            float wLenSq = wX * wX + wZ * wZ;

            float dotProduct = wX * relPosX + wZ * relPosZ;

            if (dotProduct < 0f && dotProduct * dotProduct > combinedRadiusSq * wLenSq)
            {
                // Project on cutoff circle
                float wLen = MathF.Sqrt(wLenSq);
                if (wLen < Epsilon)
                {
                    line.DirectionX = -relPosZ;
                    line.DirectionZ = relPosX;
                    float len = MathF.Sqrt(line.DirectionX * line.DirectionX +
                                          line.DirectionZ * line.DirectionZ);
                    if (len > Epsilon)
                    {
                        line.DirectionX /= len;
                        line.DirectionZ /= len;
                    }
                    line.PointX = unit.Velocity.X + wX * avoidanceWeight;
                    line.PointZ = unit.Velocity.Z + wZ * avoidanceWeight;
                    return line;
                }

                float unitWX = wX / wLen;
                float unitWZ = wZ / wLen;

                line.DirectionX = unitWZ;
                line.DirectionZ = -unitWX;

                float uDot = (relVelX - relPosX * invTimeHorizon) * unitWX +
                             (relVelZ - relPosZ * invTimeHorizon) * unitWZ;
                float u = uDot - combinedRadius * invTimeHorizon;
                line.PointX = unit.Velocity.X + unitWX * u * avoidanceWeight;
                line.PointZ = unit.Velocity.Z + unitWZ * u * avoidanceWeight;
            }
            else
            {
                // Project on legs
                float dist = MathF.Sqrt(distSq);
                float leg = MathF.Sqrt(MathF.Max(distSq - combinedRadiusSq, 0f));

                if (relPosX * (relVelZ) - relPosZ * (relVelX) > 0f)
                {
                    // Left leg
                    line.DirectionX = (relPosX * leg - relPosZ * combinedRadius) / distSq;
                    line.DirectionZ = (relPosX * combinedRadius + relPosZ * leg) / distSq;
                }
                else
                {
                    // Right leg
                    line.DirectionX = -(relPosX * leg + relPosZ * combinedRadius) / distSq;
                    line.DirectionZ = -(-relPosX * combinedRadius + relPosZ * leg) / distSq;
                }

                float dotLeg = relVelX * line.DirectionX + relVelZ * line.DirectionZ;
                line.PointX = unit.Velocity.X + (relVelX - dotLeg * line.DirectionX) * avoidanceWeight;
                line.PointZ = unit.Velocity.Z + (relVelZ - dotLeg * line.DirectionZ) * avoidanceWeight;
            }
        }
        else
        {
            // Already overlapping — push apart with high priority
            float invDelta = OverlapInvDelta;
            float wX = relVelX - relPosX * invDelta;
            float wZ = relVelZ - relPosZ * invDelta;
            float wLen = MathF.Sqrt(wX * wX + wZ * wZ);

            if (wLen < Epsilon)
            {
                // Units at exact same position — push based on unit ID
                float angle = (unit.UnitId * GoldenAngle) % SimMath.Tau;
                line.DirectionX = MathF.Cos(angle);
                line.DirectionZ = MathF.Sin(angle);
                line.PointX = unit.Velocity.X;
                line.PointZ = unit.Velocity.Z;
                return line;
            }

            float unitWX = wX / wLen;
            float unitWZ = wZ / wLen;

            line.DirectionX = unitWZ;
            line.DirectionZ = -unitWX;
            line.PointX = unit.Velocity.X + unitWX * (wLen * avoidanceWeight);
            line.PointZ = unit.Velocity.Z + unitWZ * (wLen * avoidanceWeight);
        }

        return line;
    }

    /// <summary>
    /// 1D optimization on a single ORCA line — find closest point to (vx,vz) on the line
    /// within the speed disc of radius maxSpeed.
    /// </summary>
    private static bool LinearProgram1(
        List<OrcaLine> lines, int lineIndex, float maxSpeed,
        ref float vx, ref float vz)
    {
        var line = lines[lineIndex];
        float dotProduct = line.PointX * line.DirectionX + line.PointZ * line.DirectionZ;
        float discriminant = dotProduct * dotProduct + maxSpeed * maxSpeed -
                            (line.PointX * line.PointX + line.PointZ * line.PointZ);

        if (discriminant < 0f)
            return false; // Max speed disc doesn't intersect line

        float sqrtDisc = MathF.Sqrt(discriminant);
        float tLeft = -dotProduct - sqrtDisc;
        float tRight = -dotProduct + sqrtDisc;

        // Constrain by previous lines
        for (int i = 0; i < lineIndex; i++)
        {
            var other = lines[i];
            float denom = line.DirectionX * other.DirectionZ - line.DirectionZ * other.DirectionX;
            float numer = other.DirectionX * (line.PointZ - other.PointZ) -
                         other.DirectionZ * (line.PointX - other.PointX);

            if (MathF.Abs(denom) <= Epsilon)
            {
                // Lines are parallel
                if (numer < 0f)
                    return false;
                continue;
            }

            float t = numer / denom;

            if (denom >= 0f)
                tRight = MathF.Min(tRight, t);
            else
                tLeft = MathF.Max(tLeft, t);

            if (tLeft > tRight)
                return false;
        }

        // Project optimal velocity onto valid range
        float tOpt = line.DirectionX * (vx - line.PointX) +
                     line.DirectionZ * (vz - line.PointZ);
        tOpt = MathF.Max(tLeft, MathF.Min(tRight, tOpt));

        vx = line.PointX + tOpt * line.DirectionX;
        vz = line.PointZ + tOpt * line.DirectionZ;

        return true;
    }

    /// <summary>
    /// 2D LP: incrementally satisfy all constraints.
    /// Returns true if feasible, false if constraints are contradictory.
    /// </summary>
    private static bool LinearProgram2(
        List<OrcaLine> lines, float maxSpeed, ref float vx, ref float vz)
    {
        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            // Check if current velocity satisfies this constraint
            float det = line.DirectionX * (line.PointZ - vz) -
                       line.DirectionZ * (line.PointX - vx);

            if (det > 0f)
            {
                // Violated — project onto this line
                if (!LinearProgram1(lines, i, maxSpeed, ref vx, ref vz))
                    return false; // Infeasible
            }
        }

        return true;
    }

    /// <summary>
    /// Fallback when LP2 is infeasible: sequentially process each violated line,
    /// searching along intersections with prior lines to find velocities that
    /// satisfy multiple constraints simultaneously. Falls back to simple projection
    /// when no intersection is feasible.
    /// </summary>
    private static void LinearProgram3(
        List<OrcaLine> lines, float maxSpeed, ref float vx, ref float vz)
    {
        int numLines = lines.Count;

        for (int i = 0; i < numLines; i++)
        {
            var lineI = lines[i];
            float det = lineI.DirectionX * (lineI.PointZ - vz) -
                       lineI.DirectionZ * (lineI.PointX - vx);

            if (det <= 0f)
                continue; // Not violated

            // Project onto line i
            float tProj = lineI.DirectionX * (vx - lineI.PointX) +
                         lineI.DirectionZ * (vz - lineI.PointZ);
            float projX = lineI.PointX + tProj * lineI.DirectionX;
            float projZ = lineI.PointZ + tProj * lineI.DirectionZ;

            // Re-check all prior lines against the projected point
            bool resolved = false;
            for (int j = 0; j < i; j++)
            {
                var lineJ = lines[j];
                float detJ = lineJ.DirectionX * (lineJ.PointZ - projZ) -
                            lineJ.DirectionZ * (lineJ.PointX - projX);

                if (detJ <= 0f)
                    continue; // Prior line not violated

                // Lines i and j conflict — find intersection direction
                float denom = lineI.DirectionX * lineJ.DirectionZ -
                             lineI.DirectionZ * lineJ.DirectionX;

                if (MathF.Abs(denom) <= Epsilon)
                    continue; // Parallel — skip this pair, try next j

                // Intersection direction (difference of line directions, normalized)
                float crossX = lineJ.DirectionX - lineI.DirectionX;
                float crossZ = lineJ.DirectionZ - lineI.DirectionZ;
                float crossLen = MathF.Sqrt(crossX * crossX + crossZ * crossZ);

                if (crossLen < Epsilon)
                    continue;

                crossX /= crossLen;
                crossZ /= crossLen;

                // Ensure direction is on the feasible side of line i
                float checkI = crossX * lineI.DirectionZ - crossZ * lineI.DirectionX;
                if (checkI < 0f)
                {
                    crossX = -crossX;
                    crossZ = -crossZ;
                }

                // Intersection point of lines i and j
                float numer = lineJ.DirectionX * (lineI.PointZ - lineJ.PointZ) -
                             lineJ.DirectionZ * (lineI.PointX - lineJ.PointX);
                float tInt = numer / denom;
                float intX = lineI.PointX + tInt * lineI.DirectionX;
                float intZ = lineI.PointZ + tInt * lineI.DirectionZ;

                // Search along intersection direction, clamped to speed disc
                float tSearch = crossX * (vx - intX) + crossZ * (vz - intZ);

                float b = 2f * (intX * crossX + intZ * crossZ);
                float c = intX * intX + intZ * intZ - maxSpeed * maxSpeed;
                float discrim = b * b - 4f * c; // a=1 (normalized)

                if (discrim < 0f)
                    continue; // Speed disc doesn't intersect — try next j

                float sqrtDisc = MathF.Sqrt(discrim);
                float tMin = (-b - sqrtDisc) * 0.5f;
                float tMax = (-b + sqrtDisc) * 0.5f;

                tSearch = MathF.Max(tMin, MathF.Min(tMax, tSearch));

                projX = intX + tSearch * crossX;
                projZ = intZ + tSearch * crossZ;
                resolved = true;
                break; // Use the first valid intersection
            }

            // Accept the projected/intersected velocity
            vx = projX;
            vz = projZ;

            // If we didn't resolve via intersection, the simple projection onto line i
            // is already in projX/projZ. Either way, clamp to speed disc.
            if (!resolved)
            {
                float speedSq = vx * vx + vz * vz;
                if (speedSq > maxSpeed * maxSpeed)
                {
                    float scale = maxSpeed / MathF.Sqrt(speedSq);
                    vx *= scale;
                    vz *= scale;
                }
            }
        }

        // Final clamp to speed disc
        float finalSpeedSq = vx * vx + vz * vz;
        if (finalSpeedSq > maxSpeed * maxSpeed)
        {
            float scale = maxSpeed / MathF.Sqrt(finalSpeedSq);
            vx *= scale;
            vz *= scale;
        }
    }

    /// <summary>
    /// Determine avoidance responsibility split.
    /// Stationary/attacking units get high priority (others dodge them).
    /// Moving units share avoidance burden equally.
    /// </summary>
    private static float GetAvoidanceWeight(UnitData unit, UnitData neighbor)
    {
        bool neighborStationary = neighbor.BehaviorState == BehaviorState.Attacking ||
                                  neighbor.BehaviorState == BehaviorState.InRange;
        bool unitStationary = unit.BehaviorState == BehaviorState.Attacking ||
                              unit.BehaviorState == BehaviorState.InRange;

        if (neighborStationary && !unitStationary)
            return 0.9f; // We dodge almost entirely
        if (unitStationary && !neighborStationary)
            return 0.1f; // Neighbor should dodge us
        return 0.5f; // Equal share
    }
}
