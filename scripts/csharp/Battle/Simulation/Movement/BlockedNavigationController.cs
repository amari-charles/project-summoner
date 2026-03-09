using System;
using Fateforged.Simulation.Combat;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Subsystems;
using Fateforged.Units;

namespace Fateforged.Simulation.Movement;

/// <summary>
/// Detects prolonged low-progress movement and injects temporary
/// yield/escape intents to break clump pushback loops.
/// </summary>
public static class BlockedNavigationController
{
    private const float BlockedThresholdSeconds = 0.22f;
    private const float YieldDurationSeconds = 0.22f;
    private const float EscapeDurationSeconds = 0.55f;
    private const float MinProgressAbsolute = 0.005f;
    private const float MinProgressSpeedRatio = 0.10f;
    private const float BlockedRecoveryRate = 2.0f;
    private const float EscapeNeighborRadius = 2.2f;
    private const float EscapeLateralWeight = 0.90f;
    private const float EscapeForwardWeight = 0.35f;
    private const float EscapeSpeedScale = 1.00f;
    private const float DirectionThreshold = 0.0001f;

    public static MovementIntent Apply(
        UnitData unit,
        SimBehavior.BehaviorResult behavior,
        MatchState state,
        MovementIntent baseIntent,
        float delta)
    {
        if (!IsSupportedMovement(behavior.Movement))
        {
            Reset(unit);
            return baseIntent;
        }

        var targetPos = MovementTargetResolver.Resolve(unit, behavior.MoveTargetId, state);
        if (!targetPos.HasValue)
        {
            Reset(unit);
            return baseIntent;
        }

        EnsureTargetTracking(unit, behavior.MoveTargetId);

        var toTarget = targetPos.Value - unit.Position;
        toTarget.Y = 0f;
        float targetDistance = toTarget.Length();
        var targetDirection = targetDistance > DirectionThreshold
            ? toTarget / targetDistance
            : baseIntent.DesiredFacingDirection;

        TickTimers(unit, delta);

        if (unit.NavigationYieldTimer > 0f)
            return BuildYieldIntent(baseIntent, targetDirection);

        if (unit.NavigationEscapeTimer > 0f)
            return BuildEscapeIntent(unit, baseIntent, targetDirection);

        UpdateBlockedProgress(unit, targetDistance, baseIntent, delta);
        unit.NavigationLastTargetDistance = targetDistance;

        if (unit.NavigationBlockedTime < BlockedThresholdSeconds)
            return baseIntent;

        unit.NavigationBlockedTime = 0f;
        unit.NavigationYieldTimer = YieldDurationSeconds;
        unit.NavigationEscapeQueued = true;
        unit.NavigationEscapeDirectionSign = ChooseEscapeDirectionSign(unit, targetDirection, state);
        return BuildYieldIntent(baseIntent, targetDirection);
    }

    /// <summary>
    /// Clears blocked-navigation transient state when a unit is not in a movement mode
    /// that can accumulate blocked progress (e.g. idle/attacking/stunned).
    /// </summary>
    public static void ResetState(UnitData unit) => Reset(unit);

    private static bool IsSupportedMovement(MovementResult movement)
        => movement == MovementResult.TowardTarget || movement == MovementResult.Strafe;

    private static void EnsureTargetTracking(UnitData unit, int? targetId)
    {
        if (unit.NavigationTargetId == targetId)
            return;

        unit.NavigationTargetId = targetId;
        unit.NavigationLastTargetDistance = -1f;
        unit.NavigationBlockedTime = 0f;
        unit.NavigationYieldTimer = 0f;
        unit.NavigationEscapeTimer = 0f;
        unit.NavigationEscapeQueued = false;
    }

    private static void TickTimers(UnitData unit, float delta)
    {
        if (unit.NavigationYieldTimer > 0f)
        {
            unit.NavigationYieldTimer = MathF.Max(0f, unit.NavigationYieldTimer - delta);
            if (unit.NavigationYieldTimer <= 0f && unit.NavigationEscapeQueued)
            {
                unit.NavigationEscapeQueued = false;
                unit.NavigationEscapeTimer = EscapeDurationSeconds;
            }
        }

        if (unit.NavigationEscapeTimer > 0f)
            unit.NavigationEscapeTimer = MathF.Max(0f, unit.NavigationEscapeTimer - delta);
    }

    private static void UpdateBlockedProgress(
        UnitData unit, float targetDistance, MovementIntent baseIntent, float delta)
    {
        if (unit.NavigationLastTargetDistance < 0f)
            return;

        float progress = unit.NavigationLastTargetDistance - targetDistance;
        float desiredSpeed = baseIntent.DesiredVelocity.Length();
        if (desiredSpeed < 0.05f)
            desiredSpeed = SimEffects.GetEffectiveMoveSpeed(unit);

        float minProgress = MathF.Max(MinProgressAbsolute, desiredSpeed * delta * MinProgressSpeedRatio);
        if (progress < minProgress)
        {
            unit.NavigationBlockedTime += delta;
        }
        else
        {
            unit.NavigationBlockedTime = MathF.Max(0f, unit.NavigationBlockedTime - (delta * BlockedRecoveryRate));
        }
    }

    private static MovementIntent BuildYieldIntent(MovementIntent baseIntent, SimVector3 targetDirection)
    {
        return new MovementIntent
        {
            Mode = baseIntent.Mode,
            TargetId = baseIntent.TargetId,
            DesiredVelocity = SimVector3.Zero,
            DesiredFacingDirection = targetDirection
        };
    }

    private static MovementIntent BuildEscapeIntent(
        UnitData unit, MovementIntent baseIntent, SimVector3 targetDirection)
    {
        if (targetDirection.LengthSquared() < DirectionThreshold)
            return baseIntent;

        var left = new SimVector3(-targetDirection.Z, 0f, targetDirection.X);
        var lateral = left * unit.NavigationEscapeDirectionSign;
        var blended = (lateral * EscapeLateralWeight) + (targetDirection * EscapeForwardWeight);
        if (blended.LengthSquared() < DirectionThreshold)
            return baseIntent;

        blended = blended.Normalized();
        float speed = baseIntent.DesiredVelocity.Length();
        if (speed < 0.05f)
            speed = SimEffects.GetEffectiveMoveSpeed(unit);

        return new MovementIntent
        {
            Mode = baseIntent.Mode,
            TargetId = baseIntent.TargetId,
            DesiredVelocity = blended * (speed * EscapeSpeedScale),
            DesiredFacingDirection = targetDirection
        };
    }

    private static int ChooseEscapeDirectionSign(UnitData unit, SimVector3 targetDirection, MatchState state)
    {
        if (targetDirection.LengthSquared() < DirectionThreshold)
            return (unit.UnitId % 2 == 0) ? 1 : -1;

        var left = new SimVector3(-targetDirection.Z, 0f, targetDirection.X);
        float radiusSq = EscapeNeighborRadius * EscapeNeighborRadius;
        int leftCount = 0;
        int rightCount = 0;

        foreach (var other in state.Units.Values)
        {
            if (other.UnitId == unit.UnitId) continue;
            if (!other.IsAlive) continue;
            if (other.ActivationState != ActivationState.Active) continue;
            if (other.MovementLayer != unit.MovementLayer) continue;

            var diff = other.Position - unit.Position;
            diff.Y = 0f;
            if (diff.LengthSquared() > radiusSq) continue;

            float side = diff.Dot(left);
            if (side > 0f) leftCount++;
            else if (side < 0f) rightCount++;
        }

        if (leftCount < rightCount) return 1;
        if (rightCount < leftCount) return -1;
        return (unit.UnitId % 2 == 0) ? 1 : -1;
    }

    private static void Reset(UnitData unit)
    {
        unit.NavigationTargetId = null;
        unit.NavigationLastTargetDistance = -1f;
        unit.NavigationBlockedTime = 0f;
        unit.NavigationYieldTimer = 0f;
        unit.NavigationEscapeTimer = 0f;
        unit.NavigationEscapeQueued = false;
    }
}
