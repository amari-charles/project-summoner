using System;
using Fateforged.Simulation.Combat;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Subsystems;
using Fateforged.Units;

namespace Fateforged.Simulation.Movement;

/// <summary>
/// Direct geometric movement intent generator.
/// Computes desired velocity/facing from behavior intent and target geometry only.
/// </summary>
public sealed class DirectIntentGenerator : IMovementIntentGenerator
{
    private const float DirectionThreshold = 0.001f;
    private const float MinHorizontalDisplacement = 0.01f;
    private const float StrafeFacingHalfAngle = 90f;

    public MovementIntent Generate(
        UnitData unit,
        SimBehavior.BehaviorResult behavior,
        MatchState state,
        float delta
    )
    {
        float speed = SimEffects.GetEffectiveMoveSpeed(unit);

        return behavior.Movement switch
        {
            MovementResult.Forward => BuildForwardIntent(unit, state, speed),
            MovementResult.TowardTarget => BuildTowardTargetIntent(
                unit,
                behavior.MoveTargetId,
                state,
                speed
            ),
            MovementResult.Strafe => BuildStrafeIntent(unit, behavior.MoveTargetId, state, speed),
            _ => MovementIntent.None(behavior.Movement, behavior.MoveTargetId),
        };
    }

    private static MovementIntent BuildForwardIntent(UnitData unit, MatchState state, float speed)
    {
        var forwardDir = MovementTargetResolver.ResolveObjectiveAdvanceDirection(unit, state);
        return new MovementIntent
        {
            Mode = MovementResult.Forward,
            TargetId = null,
            DesiredVelocity = forwardDir * speed,
            DesiredFacingDirection = forwardDir,
        };
    }

    private static MovementIntent BuildTowardTargetIntent(
        UnitData unit,
        int? targetId,
        MatchState state,
        float speed
    )
    {
        var targetPos = MovementTargetResolver.Resolve(unit, targetId, state);
        if (!targetPos.HasValue)
        {
            var fallback = BuildForwardIntent(unit, state, speed);
            return new MovementIntent
            {
                Mode = MovementResult.TowardTarget,
                TargetId = targetId,
                DesiredVelocity = fallback.DesiredVelocity,
                DesiredFacingDirection = fallback.DesiredFacingDirection,
            };
        }

        var toTarget = targetPos.Value - unit.Position;
        toTarget.Y = 0f;
        if (toTarget.LengthSquared() <= DirectionThreshold)
            return MovementIntent.None(MovementResult.TowardTarget, targetId);

        var dir = toTarget.Normalized();
        return new MovementIntent
        {
            Mode = MovementResult.TowardTarget,
            TargetId = targetId,
            DesiredVelocity = dir * speed,
            DesiredFacingDirection = dir,
        };
    }

    private static MovementIntent BuildStrafeIntent(
        UnitData unit,
        int? targetId,
        MatchState state,
        float speed
    )
    {
        var targetPos = MovementTargetResolver.Resolve(unit, targetId, state);
        if (!targetPos.HasValue)
        {
            var fallback = BuildForwardIntent(unit, state, speed);
            return new MovementIntent
            {
                Mode = MovementResult.Strafe,
                TargetId = targetId,
                DesiredVelocity = fallback.DesiredVelocity,
                DesiredFacingDirection = fallback.DesiredFacingDirection,
            };
        }

        var toTarget = targetPos.Value - unit.Position;
        var horizontalToTarget = new SimVector3(toTarget.X, 0f, toTarget.Z);
        if (
            horizontalToTarget.LengthSquared()
            < MinHorizontalDisplacement * MinHorizontalDisplacement
        )
            return MovementIntent.None(MovementResult.Strafe, targetId);

        horizontalToTarget = horizontalToTarget.Normalized();

        float angleToTarget = SimMath.RadToDeg(
            MathF.Atan2(horizontalToTarget.Z, horizontalToTarget.X)
        );
        bool shouldFaceRight = MathF.Abs(angleToTarget) <= StrafeFacingHalfAngle;
        float facingAngle = shouldFaceRight ? 0f : 180f;
        float angleDiff = angleToTarget - facingAngle;
        while (angleDiff > 180f)
            angleDiff -= 360f;
        while (angleDiff < -180f)
            angleDiff += 360f;

        var strafeDir = new SimVector3(-horizontalToTarget.Z, 0f, horizontalToTarget.X);
        if (angleDiff < 0f)
            strafeDir = -strafeDir;

        if (strafeDir.LengthSquared() <= DirectionThreshold)
            return MovementIntent.None(MovementResult.Strafe, targetId);

        strafeDir = strafeDir.Normalized();
        return new MovementIntent
        {
            Mode = MovementResult.Strafe,
            TargetId = targetId,
            DesiredVelocity = strafeDir * speed,
            DesiredFacingDirection = horizontalToTarget,
        };
    }
}
