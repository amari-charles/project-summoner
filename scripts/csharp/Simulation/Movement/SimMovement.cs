using System;
using Fateforged.Simulation.Combat;
using ProjectSummoner.Constants;
using ProjectSummoner.Units;

namespace Fateforged.Simulation.Movement;

/// <summary>
/// Pure deterministic movement calculations operating on UnitData.
/// Mirrors UnitMovement.cs: forward, toward-target, strafe, and position application.
/// No Godot node dependencies — operates on MatchState data only.
/// </summary>
public static class SimMovement
{
    // Minimum squared length for treating a direction vector as non-zero
    private const float DirectionThreshold = 0.001f;

    // Minimum horizontal displacement for strafe calculations
    private const float MinHorizontalDisplacement = 0.01f;

    // Half-plane angle threshold for facing direction during strafe
    private const float StrafeFacingHalfAngle = 90f;

    /// <summary>
    /// Execute a full movement tick for a unit based on its behavior result.
    /// Applies velocity, clamps to bounds, corrects overlaps, and updates facing.
    /// </summary>
    public static void Tick(UnitData unit, SimBehavior.BehaviorResult behavior, MatchState state, float delta)
    {
        SimVector3 velocity;

        switch (behavior.Movement)
        {
            case SimBehavior.MoveForward:
                velocity = CalculateForward(unit, state);
                UpdateFacing(unit, new SimVector3(unit.Team == 0 ? 1f : -1f, 0, 0));
                break;

            case SimBehavior.MoveTowardTarget:
            {
                var targetPos = SimUtils.ResolveTargetPosition(behavior.MoveTargetId, state);
                if (!targetPos.HasValue)
                {
                    velocity = CalculateForward(unit, state);
                    UpdateFacing(unit, new SimVector3(unit.Team == 0 ? 1f : -1f, 0, 0));
                }
                else
                {
                    // For unit targets, exclude target from separation and apply flanking
                    int? excludeId = IsUnitTarget(behavior.MoveTargetId) ? behavior.MoveTargetId : null;
                    UnitData? targetUnit = excludeId.HasValue ? state.GetAliveUnit(excludeId.Value) : null;
                    velocity = CalculateTowardPosition(unit, targetPos.Value, excludeId, targetUnit, state, delta);
                    var dir = targetPos.Value - unit.Position;
                    dir.Y = 0;
                    if (dir.LengthSquared() > DirectionThreshold)
                        UpdateFacing(unit, dir.Normalized());
                }
                break;
            }

            case SimBehavior.MoveStrafe:
            {
                var targetPos = SimUtils.ResolveTargetPosition(behavior.MoveTargetId, state);
                if (!targetPos.HasValue)
                {
                    velocity = CalculateForward(unit, state);
                    UpdateFacing(unit, new SimVector3(unit.Team == 0 ? 1f : -1f, 0, 0));
                }
                else
                {
                    int? excludeId = IsUnitTarget(behavior.MoveTargetId) ? behavior.MoveTargetId : null;
                    velocity = CalculateStrafePosition(unit, targetPos.Value, excludeId, state);
                    // Strafe sets facing explicitly based on target half-plane
                    var toTarget = targetPos.Value - unit.Position;
                    float hx = toTarget.X;
                    float hz = toTarget.Z;
                    float horizontalLenSq = hx * hx + hz * hz;
                    if (horizontalLenSq > MinHorizontalDisplacement * MinHorizontalDisplacement)
                    {
                        float angleToTarget = SimMath.RadToDeg(MathF.Atan2(hz, hx));
                        unit.IsFacingRight = MathF.Abs(angleToTarget) <= StrafeFacingHalfAngle;
                    }
                }
                break;
            }

            default: // MoveNone
                unit.Velocity = SimVector3.Zero;
                SimSteering.CorrectOverlaps(unit, state);
                return;
        }

        // Apply velocity (track distance traveled for charge ability)
        unit.Velocity = velocity;
        var newPos = unit.Position + velocity * delta;
        float moveDist = (velocity * delta).Length();
        if (moveDist > DirectionThreshold)
            unit.DistanceTraveled += moveDist;

        // Preserve altitude for flying units
        if (unit.MovementLayer == MovementLayer.Air)
            newPos.Y = unit.FlightAltitude;
        else
            newPos.Y = unit.Position.Y;

        // Clamp to battlefield bounds
        newPos = BattlefieldBounds.ClampToBounds(newPos);
        unit.Position = newPos;

        // Update blocked state for flanking
        SimSteering.UpdateBlockedState(unit, delta);

        // Correct overlaps after position update
        SimSteering.CorrectOverlaps(unit, state);
    }

    /// <summary>
    /// Calculate forward movement velocity (no target — move toward enemy base).
    /// Team 0 moves in +X, team 1 moves in -X.
    /// </summary>
    private static SimVector3 CalculateForward(UnitData unit, MatchState state)
    {
        float direction = unit.Team == 0 ? 1.0f : -1.0f;
        var moveDir = new SimVector3(direction, 0, 0);
        float effectiveSpeed = SimEffects.GetEffectiveMoveSpeed(unit);

        var separation = SimSteering.CalculateSeparation(unit, null, moveDir, state);
        var finalDir = (moveDir + separation);
        if (finalDir.LengthSquared() > DirectionThreshold)
            finalDir = finalDir.Normalized();

        var velocity = finalDir * effectiveSpeed;
        velocity.Y = 0;

        return velocity;
    }

    /// <summary>
    /// Check if a target ID refers to a unit (non-negative ID).
    /// </summary>
    private static bool IsUnitTarget(int? targetId)
    {
        return targetId.HasValue && targetId.Value >= 0;
    }

    /// <summary>
    /// Calculate movement toward a target position with separation and flanking.
    /// excludeUnitId: unit target to exclude from separation (null for summoner targets).
    /// targetUnit: the UnitData for flanking calculations (null for summoner targets).
    /// </summary>
    private static SimVector3 CalculateTowardPosition(
        UnitData unit, SimVector3 targetPos, int? excludeUnitId, UnitData? targetUnit,
        MatchState state, float delta)
    {
        // For ground units, ignore Y difference
        if (unit.MovementLayer == 0) // Ground
            targetPos.Y = unit.Position.Y;

        var direction = (targetPos - unit.Position);
        if (direction.LengthSquared() < DirectionThreshold)
            return SimVector3.Zero;

        direction = direction.Normalized();

        float effectiveSpeed = SimEffects.GetEffectiveMoveSpeed(unit);
        var separation = SimSteering.CalculateSeparation(unit, excludeUnitId, direction, state);
        var flank = targetUnit != null ? SimSteering.CalculateFlankForce(unit, targetUnit) : SimVector3.Zero;
        var finalDir = (direction * effectiveSpeed + separation + flank);
        if (finalDir.LengthSquared() > DirectionThreshold)
            finalDir = finalDir.Normalized();

        var velocity = finalDir * effectiveSpeed;
        velocity.Y = 0;

        return velocity;
    }

    /// <summary>
    /// Calculate strafe movement around a target position.
    /// Used by ranged units when they can't get target in attack cone.
    /// excludeUnitId: unit target to exclude from separation (null for summoner targets).
    /// </summary>
    private static SimVector3 CalculateStrafePosition(
        UnitData unit, SimVector3 targetPos, int? excludeUnitId, MatchState state)
    {
        var toTarget = targetPos - unit.Position;
        var horizontalToTarget = new SimVector3(toTarget.X, 0, toTarget.Z);

        if (horizontalToTarget.LengthSquared() < MinHorizontalDisplacement * MinHorizontalDisplacement)
            return SimVector3.Zero;

        // Calculate angle to target
        float angleToTarget = SimMath.RadToDeg(MathF.Atan2(horizontalToTarget.Z, horizontalToTarget.X));

        // Determine optimal facing
        bool shouldFaceRight = MathF.Abs(angleToTarget) <= StrafeFacingHalfAngle;

        // Calculate angle difference from facing
        float facingAngle = shouldFaceRight ? 0f : 180f;
        float angleDiff = angleToTarget - facingAngle;
        while (angleDiff > 180f) angleDiff -= 360f;
        while (angleDiff < -180f) angleDiff += 360f;

        // Get perpendicular strafe direction
        var strafeDir = new SimVector3(-horizontalToTarget.Z, 0, horizontalToTarget.X).Normalized();

        // Choose strafe direction that reduces |angleDiff|
        if (angleDiff < 0)
            strafeDir = -strafeDir;

        float effectiveSpeed = SimEffects.GetEffectiveMoveSpeed(unit);
        var separation = SimSteering.CalculateSeparation(unit, excludeUnitId, strafeDir, state);
        var finalDir = (strafeDir + separation);
        if (finalDir.LengthSquared() > DirectionThreshold)
            finalDir = finalDir.Normalized();

        var velocity = finalDir * effectiveSpeed;
        velocity.Y = 0;

        return velocity;
    }

    /// <summary>
    /// Update facing direction based on movement direction.
    /// </summary>
    private static void UpdateFacing(UnitData unit, SimVector3 direction)
    {
        if (direction.LengthSquared() > DirectionThreshold)
            unit.IsFacingRight = direction.X > 0;
    }
}
