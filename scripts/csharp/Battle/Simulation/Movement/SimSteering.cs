using System;
using Fateforged.Constants;
using Fateforged.Units;
using Fateforged.Simulation.Data;

namespace Fateforged.Simulation.Movement;

/// <summary>
/// Pure deterministic steering calculations operating on UnitData.
/// Mirrors UnitSteering.cs: separation, flanking, and overlap correction.
/// No Godot node dependencies — operates on MatchState data only.
/// </summary>
public static class SimSteering
{
    // =========================================================================
    // SEPARATION CONSTANTS (mirrors UnitSteering.cs)
    // =========================================================================

    private const float SeparationMultiplier = 1.5f;
    private const float SeparationStrength = 2.0f;
    private const float LateralSeparationBoost = 3.0f;
    private const int MaxNeighborsToProcess = 8;

    // =========================================================================
    // FLANKING CONSTANTS
    // =========================================================================

    private const float BlockedThreshold = 0.3f;
    private const float BlockedMoveThreshold = 0.1f;
    private const float FlankStrength = 1.2f;
    private const float FlankAngleMin = 90f;
    private const float FlankAngleMax = 135f;
    private const float FlankAngleStep = 15f;
    private const float FlankProgressInterval = 0.5f;


    /// <summary>
    /// Calculate separation force to prevent unit stacking.
    /// Pushes away from nearby units on the same movement layer.
    /// </summary>
    public static SimVector3 CalculateSeparation(UnitData unit, int? targetUnitId, SimVector3 moveDirection, MatchState state)
    {
        var force = SimVector3.Zero;
        float separationDist = unit.SeparationRadius * SeparationMultiplier * 2f;
        int processed = 0;

        foreach (var kvp in state.Units)
        {
            if (processed >= MaxNeighborsToProcess) break;

            var other = kvp.Value;
            if (other.UnitId == unit.UnitId) continue;
            if (!other.IsAlive) continue;
            if (other.ActivationState != ActivationState.Active) continue;

            // Skip current target — don't separate from what we're attacking
            if (targetUnitId.HasValue && other.UnitId == targetUnitId.Value) continue;

            // Only separate from same movement layer
            if (other.MovementLayer != unit.MovementLayer) continue;

            var diff = unit.Position - other.Position;
            diff.Y = 0; // XZ plane only
            float distSq = diff.LengthSquared();

            float combinedSep = (unit.SeparationRadius + other.SeparationRadius) * SeparationMultiplier;
            if (distSq >= combinedSep * combinedSep || distSq < 0.001f) continue;

            processed++;

            float dist = MathF.Sqrt(distSq);
            var pushDir = diff / dist;
            float strength = (1f - dist / combinedSep) * SeparationStrength;
            force += pushDir * strength;

            // Lateral boost when chasing same direction
            if (moveDirection.LengthSquared() > 0.01f)
            {
                var lateralDir = new SimVector3(-moveDirection.Z, 0, moveDirection.X);
                float forwardAlignment = MathF.Abs(pushDir.Dot(moveDirection));
                // Alternate lateral push direction by unit ID to spread units evenly.
                // Intentionally opposite sign from FlankDirection (line 104) — separation
                // spreads units while moving normally; flanking goes around a blocked target.
                int lateralSign = (unit.UnitId % 2 == 0) ? 1 : -1;
                force += lateralDir * lateralSign * forwardAlignment * strength * LateralSeparationBoost;
            }
        }

        return force;
    }

    /// <summary>
    /// Update blocked state for flanking behavior.
    /// Detects when a unit is stuck and computes flank direction/angle.
    /// </summary>
    public static void UpdateBlockedState(UnitData unit, float delta)
    {
        float speed = unit.Position.DistanceTo(unit.LastPosition) / MathF.Max(delta, 0.001f);

        if (speed < BlockedMoveThreshold)
        {
            unit.BlockedTime += delta;

            // Phase 1: Choose flank direction
            if (unit.BlockedTime > BlockedThreshold && unit.FlankDirection == FlankDirection.None)
            {
                // Alternate flank direction by unit ID so units go around from both sides.
                // Intentionally opposite sign from separation lateral (line 81).
                unit.FlankDirection = (unit.UnitId % 2 == 0) ? FlankDirection.Left : FlankDirection.Right;
                unit.FlankAngle = FlankAngleMin;
                unit.FlankProgressTimer = 0;
            }

            // Phase 2: Progressive angle increase
            if (unit.BlockedTime > BlockedThreshold + FlankProgressInterval)
            {
                unit.FlankProgressTimer += delta;
                if (unit.FlankProgressTimer >= FlankProgressInterval)
                {
                    unit.FlankProgressTimer = 0;
                    unit.FlankAngle = MathF.Min(unit.FlankAngle + FlankAngleStep, FlankAngleMax);
                }
            }
        }
        else
        {
            // Moving normally — reset flanking
            unit.BlockedTime = 0;
            unit.FlankDirection = FlankDirection.None;
            unit.FlankAngle = FlankAngleMin;
            unit.FlankProgressTimer = 0;
        }

        unit.LastPosition = unit.Position;
    }

    /// <summary>
    /// Calculate flanking force when unit is blocked.
    /// Returns Zero if not blocked.
    /// </summary>
    public static SimVector3 CalculateFlankForce(UnitData unit, UnitData target)
    {
        if (unit.BlockedTime < BlockedThreshold || unit.FlankDirection == FlankDirection.None)
            return SimVector3.Zero;

        var toTarget = (target.Position - unit.Position);
        toTarget.Y = 0;
        if (toTarget.LengthSquared() < 0.01f)
            return SimVector3.Zero;

        toTarget = toTarget.Normalized();

        float angleRad = SimMath.DegToRad(unit.FlankAngle);
        float lateralComponent = MathF.Sin(angleRad);
        float forwardComponent = MathF.Cos(angleRad);

        SimVector3 lateralDir;
        if (unit.FlankDirection == FlankDirection.Left)
            lateralDir = new SimVector3(-toTarget.Z, 0, toTarget.X);
        else
            lateralDir = new SimVector3(toTarget.Z, 0, -toTarget.X);

        var flankDir = (toTarget * forwardComponent + lateralDir * lateralComponent).Normalized();
        float strength = MathF.Min((unit.BlockedTime - BlockedThreshold) * 2f, 1f);

        return flankDir * strength * unit.MoveSpeed * FlankStrength;
    }

    /// <summary>
    /// Correct overlapping positions between units.
    /// Pushes overlapping units apart by half the overlap distance.
    /// Skips the unit's current target to prevent chase→push→chase loops.
    /// </summary>
    public static void CorrectOverlaps(UnitData unit, MatchState state)
    {
        foreach (var kvp in state.Units)
        {
            var other = kvp.Value;
            if (other.UnitId == unit.UnitId) continue;
            if (!other.IsAlive) continue;
            if (other.ActivationState != ActivationState.Active) continue;
            if (other.MovementLayer != unit.MovementLayer) continue;

            // Skip current target — prevents infinite chase→overlap→push loops
            if (unit.TargetUnitId.HasValue && other.UnitId == unit.TargetUnitId.Value) continue;
            if (other.TargetUnitId.HasValue && other.TargetUnitId.Value == unit.UnitId) continue;

            float minDist = unit.SeparationRadius + other.SeparationRadius;
            var diff = unit.Position - other.Position;
            diff.Y = 0;
            float distSq = diff.LengthSquared();

            if (distSq >= minDist * minDist || distSq < 0.000001f) continue;

            float dist = MathF.Sqrt(distSq);
            float overlap = minDist - dist;
            var pushDir = diff / dist;

            // Push proportional to relative mass (SeparationRadius^3)
            float unitMass = unit.SeparationRadius * unit.SeparationRadius * unit.SeparationRadius;
            float otherMass = other.SeparationRadius * other.SeparationRadius * other.SeparationRadius;
            float pushRatio = otherMass / (unitMass + otherMass);

            var newPos = unit.Position + pushDir * overlap * pushRatio;
            newPos = BattlefieldBounds.ClampToBounds(newPos);
            unit.Position = new SimVector3(newPos.X, unit.Position.Y, newPos.Z);
        }
    }
}
