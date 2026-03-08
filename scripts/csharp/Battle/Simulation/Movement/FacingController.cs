using System;
using Fateforged.Simulation;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Units;

namespace Fateforged.Simulation.Movement;

/// <summary>
/// Deterministic facing controller with hysteresis.
/// Keeps orientation stable under close-contact velocity jitter.
/// </summary>
public static class FacingController
{
    private const float FacingSwitchHoldSeconds = 0.12f;
    private const float FacingXDeadZone = 0.10f;
    private const float StrafeTargetXDeadZone = 0.12f;
    private const float FacingDirectionThreshold = 0.001f;

    public static void Tick(UnitData unit, float delta)
    {
        if (unit.FacingLockTimer <= 0f)
            return;

        unit.FacingLockTimer = MathF.Max(0f, unit.FacingLockTimer - delta);
    }

    public static void Update(UnitData unit, MovementIntent intent, MatchState state)
    {
        bool desiredFacing = unit.IsFacingRight;

        switch (intent.Mode)
        {
            case MovementResult.Forward:
                desiredFacing = unit.Team == Team.Player;
                unit.IsFacingRight = desiredFacing;
                unit.FacingLockTimer = 0f;
                return;

            case MovementResult.Strafe:
                desiredFacing = ResolveStrafeFacing(unit, intent, state);
                break;

            case MovementResult.TowardTarget:
                desiredFacing = ResolveDirectionalFacing(unit, intent.DesiredFacingDirection);
                break;

            default:
                return;
        }

        if (desiredFacing == unit.IsFacingRight)
            return;
        if (unit.FacingLockTimer > 0f)
            return;

        unit.IsFacingRight = desiredFacing;
        unit.FacingLockTimer = FacingSwitchHoldSeconds;
    }

    private static bool ResolveDirectionalFacing(UnitData unit, SimVector3 facingDirection)
    {
        if (facingDirection.LengthSquared() < FacingDirectionThreshold)
            return unit.IsFacingRight;

        if (MathF.Abs(facingDirection.X) < FacingXDeadZone)
            return unit.IsFacingRight;

        return facingDirection.X > 0f;
    }

    private static bool ResolveStrafeFacing(UnitData unit, MovementIntent intent, MatchState state)
    {
        var targetPos = SimUtils.ResolveTargetPosition(intent.TargetId, state);
        if (!targetPos.HasValue)
            return ResolveDirectionalFacing(unit, intent.DesiredFacingDirection);

        var toTarget = targetPos.Value - unit.Position;
        if (MathF.Abs(toTarget.X) < StrafeTargetXDeadZone)
            return unit.IsFacingRight;

        return toTarget.X > 0f;
    }
}
