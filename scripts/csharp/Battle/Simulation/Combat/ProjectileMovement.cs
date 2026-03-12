using System;
using Fateforged.Projectiles;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;

namespace Fateforged.Simulation.Combat;

/// <summary>
/// Shared deterministic projectile movement helpers.
/// Used by both SimProjectile (host simulation) and ClientSession (client-side visual extrapolation)
/// to eliminate duplicated movement code and ensure identical behavior.
/// </summary>
public static class ProjectileMovement
{
    // Arc path constants matching ArcPath.cs
    private const float ArcFullArcDistance = 5.0f;

    // Ballistic constants matching BallisticPath.cs
    private const float BallisticDefaultGravity = 9.8f;
    private const float BallisticMinTime = 0.01f;
    private const float GeometryEpsilon = 0.000001f;

    // Per-projectile phase offset for homing weave — irrational-ish value
    // ensures adjacent projectile IDs produce visually distinct sine patterns.
    private const float HomingWeavePhaseOffset = 0.37f;

    // =========================================================================
    // SPEED
    // =========================================================================

    public static void TickSpeed(SimProjectileData proj, float delta)
    {
        if (proj.UseSpeedEasing)
        {
            float duration = MathF.Max(proj.SpeedTransitionDuration, 0.0001f);
            float t = SimMath.Clamp(proj.TimeAlive / duration, 0f, 1f);
            float eased = EvaluateSpeedEasing(t, proj.SpeedEasing, proj.SpeedEaseExponent);
            proj.Speed = proj.SpeedStart + ((proj.SpeedEnd - proj.SpeedStart) * eased);
            return;
        }

        if (MathF.Abs(proj.Acceleration) < 0.0001f)
            return;

        proj.Speed += proj.Acceleration * delta;
        if (proj.Acceleration < 0f && proj.Speed < proj.MinSpeed)
            proj.Speed = proj.MinSpeed;
    }

    public static float EvaluateSpeedEasing(float t, SpeedEasingType easingType, float exponent)
    {
        float clampedT = SimMath.Clamp(t, 0f, 1f);
        float safeExponent = MathF.Max(exponent, 0.0001f);
        return easingType switch
        {
            SpeedEasingType.EaseIn => MathF.Pow(clampedT, safeExponent),
            SpeedEasingType.EaseOut => 1f - MathF.Pow(1f - clampedT, safeExponent),
            SpeedEasingType.EaseInOut => (1f - MathF.Cos(clampedT * MathF.PI)) * 0.5f,
            _ => clampedT,
        };
    }

    // =========================================================================
    // PATH MOVEMENT
    // =========================================================================

    /// <summary>
    /// Advance a straight-line projectile. Optionally updates TargetPosition when tracking a living target.
    /// </summary>
    /// <param name="getTargetPosition">If non-null and tracking, called to get live target position.</param>
    public static void TickStraight(
        SimProjectileData proj,
        float delta,
        Func<int, SimVector3?>? getTargetPosition = null
    )
    {
        if (proj.Tracking && getTargetPosition != null)
        {
            var targetPos = getTargetPosition(proj.TargetUnitId);
            if (targetPos.HasValue)
            {
                proj.TargetPosition = targetPos.Value;
                proj.PathLength = proj.StartPosition.DistanceTo(proj.TargetPosition);
            }
        }

        if (proj.PathLength < 0.01f)
        {
            proj.Progress = 1f;
            return;
        }

        proj.Progress += (proj.Speed * delta) / proj.PathLength;
        proj.Progress = MathF.Min(proj.Progress, 1f);

        proj.CurrentPosition = proj.StartPosition.Lerp(proj.TargetPosition, proj.Progress);
        proj.Direction = (proj.TargetPosition - proj.StartPosition);
        if (proj.Direction.LengthSquared() > 0.001f)
            proj.Direction = proj.Direction.Normalized();
    }

    /// <summary>
    /// Advance an arc (quadratic Bézier) projectile. Optionally updates TargetPosition when tracking.
    /// </summary>
    public static void TickArc(
        SimProjectileData proj,
        float delta,
        Func<int, SimVector3?>? getTargetPosition = null
    )
    {
        if (proj.Tracking && getTargetPosition != null)
        {
            var targetPos = getTargetPosition(proj.TargetUnitId);
            if (targetPos.HasValue)
            {
                proj.TargetPosition = targetPos.Value;
                proj.PathLength = EstimateArcLength(
                    proj.StartPosition,
                    proj.TargetPosition,
                    proj.ArcHeight
                );
            }
        }

        if (proj.PathLength < 0.01f)
        {
            proj.Progress = 1f;
            return;
        }

        proj.Progress += (proj.Speed * delta) / proj.PathLength;
        proj.Progress = MathF.Min(proj.Progress, 1f);

        float t = proj.Progress;
        var controlPoint = ComputeArcControlPoint(
            proj.StartPosition,
            proj.TargetPosition,
            proj.ArcHeight
        );

        float u = 1f - t;
        proj.CurrentPosition =
            (u * u * proj.StartPosition)
            + (2f * u * t * controlPoint)
            + (t * t * proj.TargetPosition);

        var tangent =
            (2f * u * (controlPoint - proj.StartPosition))
            + (2f * t * (proj.TargetPosition - controlPoint));
        if (tangent.LengthSquared() > 0.001f)
            proj.Direction = tangent.Normalized();
    }

    /// <summary>
    /// Advance a ballistic (physics-based parabola) projectile.
    /// </summary>
    public static void TickBallistic(SimProjectileData proj, float delta)
    {
        if (proj.TotalTime < BallisticMinTime)
        {
            proj.Progress = 1f;
            return;
        }

        proj.Progress += (proj.Speed * delta) / proj.PathLength;
        proj.Progress = MathF.Min(proj.Progress, 1f);

        float time = proj.Progress * proj.TotalTime;

        var pos = proj.StartPosition + proj.HorizontalVelocity * time;
        pos.Y =
            proj.StartPosition.Y
            + proj.InitialVerticalVelocity * time
            - 0.5f * proj.Gravity * time * time;

        proj.CurrentPosition = pos;

        var velocity = proj.HorizontalVelocity;
        velocity.Y = proj.InitialVerticalVelocity - proj.Gravity * time;
        if (velocity.LengthSquared() > 0.001f)
            proj.Direction = velocity.Normalized();
    }

    // =========================================================================
    // STEERING
    // =========================================================================

    /// <summary>
    /// Gradually steer the projectile toward a target direction.
    /// </summary>
    public static void SteerToward(
        SimProjectileData proj,
        SimVector3 targetDirection,
        float delta,
        float steerScale = 1f
    )
    {
        if (targetDirection.LengthSquared() < 0.001f)
            return;

        targetDirection = targetDirection.Normalized();

        float dot = proj.Direction.Dot(targetDirection);
        dot = SimMath.Clamp(dot, -1f, 1f);
        float angleBetween = MathF.Acos(dot);

        float clampedScale = SimMath.Clamp(steerScale, 0f, 1f);
        float maxRotation = SimMath.DegToRad(proj.SteerStrength * clampedScale) * delta;

        if (angleBetween <= maxRotation)
        {
            proj.Direction = targetDirection;
        }
        else
        {
            var rotationAxis = proj.Direction.Cross(targetDirection);
            if (rotationAxis.LengthSquared() < 0.001f)
            {
                rotationAxis = proj.Direction.Cross(SimVector3.Up);
                if (rotationAxis.LengthSquared() < 0.001f)
                    rotationAxis = proj.Direction.Cross(SimVector3.Right);
            }
            rotationAxis = rotationAxis.Normalized();

            proj.Direction = RotateAround(proj.Direction, rotationAxis, maxRotation).Normalized();
        }
    }

    // =========================================================================
    // HOMING WEAVE
    // =========================================================================

    /// <summary>
    /// Apply sinusoidal weave to homing direction during WeavingHoming's Homing phase.
    /// Creates organic serpentine motion rather than a straight line to target.
    /// </summary>
    public static SimVector3 ApplyHomingWeave(SimProjectileData proj, SimVector3 toTarget)
    {
        var targetDirection = toTarget.Normalized();
        float targetDistance = toTarget.Length();
        if (targetDistance <= GeometryEpsilon)
            return targetDirection;

        float arc = MathF.Max(0f, proj.ArcHeight);
        if (arc <= GeometryEpsilon)
            return targetDirection;

        float settle = SimMath.Clamp(
            targetDistance / WeavingHomingTuning.HomingWeaveSettleDistance,
            0f,
            1f
        );
        float yawAmplitude = SimMath.DegToRad(
            proj.ScaledCounterVeerDuration > 0f
                ? proj.ScaledCounterVeerDuration * WeavingHomingTuning.HomingYawFromDurationScale
                : 0f
        );
        if (yawAmplitude <= GeometryEpsilon)
            yawAmplitude = SimMath.DegToRad(
                MathF.Max(
                    WeavingHomingTuning.HomingYawFallbackMinDegrees,
                    arc * WeavingHomingTuning.HomingYawFallbackArcMultiplier
                )
            );

        float phase =
            (proj.TimeAlive * WeavingHomingTuning.HomingWeaveFrequency)
            + (proj.ProjectileId * HomingWeavePhaseOffset);
        float yawOffset =
            MathF.Sin(phase) * yawAmplitude * WeavingHomingTuning.HomingWeaveYawRatio * settle;
        float pitchOffset =
            MathF.Cos(
                phase
                    * (
                        WeavingHomingTuning.HomingWeavePitchFrequency
                        / WeavingHomingTuning.HomingWeaveFrequency
                    )
            )
            * yawAmplitude
            * WeavingHomingTuning.HomingWeavePitchRatio
            * settle;

        var rightAxis = GetStableRightAxis(targetDirection);
        var woven = RotateAround(targetDirection, SimVector3.Up, yawOffset);
        woven = RotateAround(woven, rightAxis, pitchOffset);
        return woven.Normalized();
    }

    // =========================================================================
    // GEOMETRY HELPERS
    // =========================================================================

    public static SimVector3 BlendWithTarget(
        SimProjectileData proj,
        SimVector3 weaveDirection,
        float targetWeight
    )
    {
        var toTarget = proj.TargetPosition - proj.CurrentPosition;
        if (toTarget.LengthSquared() <= 0.001f)
            return weaveDirection;

        var desired =
            (weaveDirection * (1f - targetWeight)) + (toTarget.Normalized() * targetWeight);
        if (desired.LengthSquared() <= 0.001f)
            return weaveDirection;

        return desired.Normalized();
    }

    public static SimVector3 ComputeArcControlPoint(
        SimVector3 start,
        SimVector3 end,
        float arcHeight
    )
    {
        float distance = start.DistanceTo(end);
        float arcScale = SimMath.Clamp(distance / ArcFullArcDistance, 0f, 1f);
        float effectiveArcHeight = arcHeight * arcScale;

        return (start + end) / 2f + SimVector3.Up * effectiveArcHeight;
    }

    public static float EstimateArcLength(SimVector3 start, SimVector3 end, float arcHeight)
    {
        const int segments = 8;
        var control = ComputeArcControlPoint(start, end, arcHeight);
        float length = 0f;

        var prev = start;
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            float u = 1f - t;
            var current = (u * u * start) + (2f * u * t * control) + (t * t * end);
            length += prev.DistanceTo(current);
            prev = current;
        }

        return MathF.Max(length, 0.1f);
    }

    public static float EstimateBallisticLength(SimProjectileData proj)
    {
        const int segments = 16;
        float length = 0f;

        var prev = proj.StartPosition;
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            float time = t * proj.TotalTime;

            var pos = proj.StartPosition + proj.HorizontalVelocity * time;
            pos.Y =
                proj.StartPosition.Y
                + proj.InitialVerticalVelocity * time
                - 0.5f * proj.Gravity * time * time;

            length += prev.DistanceTo(pos);
            prev = pos;
        }

        return MathF.Max(length, 0.1f);
    }

    /// <summary>
    /// Rotate a vector around an axis by the given angle (radians).
    /// Uses Rodrigues' rotation formula.
    /// </summary>
    public static SimVector3 RotateAround(SimVector3 v, SimVector3 axis, float angle)
    {
        axis = axis.Normalized();
        float cos = MathF.Cos(angle);
        float sin = MathF.Sin(angle);
        return v * cos + axis.Cross(v) * sin + axis * (axis.Dot(v) * (1f - cos));
    }

    public static SimVector3 GetStableRightAxis(SimVector3 forward)
    {
        var right = forward.Cross(SimVector3.Up);
        if (right.LengthSquared() < GeometryEpsilon)
            right = forward.Cross(SimVector3.Forward);
        if (right.LengthSquared() < GeometryEpsilon)
            right = SimVector3.Right;
        return right.Normalized();
    }

    /// <summary>
    /// Initialize ballistic path parameters for a projectile.
    /// </summary>
    public static void InitBallistic(
        SimProjectileData proj,
        SimVector3 start,
        SimVector3 end,
        float speed
    )
    {
        var displacement = end - start;
        float horizontalDist = MathF.Sqrt(
            displacement.X * displacement.X + displacement.Z * displacement.Z
        );
        float verticalDist = displacement.Y;

        proj.TotalTime = MathF.Max(horizontalDist / speed, BallisticMinTime);

        proj.InitialVerticalVelocity =
            (verticalDist + 0.5f * proj.Gravity * proj.TotalTime * proj.TotalTime) / proj.TotalTime;

        var horizontalDir = new SimVector3(displacement.X, 0, displacement.Z);
        if (horizontalDir.LengthSquared() > 0.001f)
            horizontalDir = horizontalDir.Normalized();
        proj.HorizontalVelocity = horizontalDir * speed;

        proj.PathLength = EstimateBallisticLength(proj);
    }
}
