using System;
using System.Collections.Generic;
using Fateforged.Projectiles;
using Fateforged.Units;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Subsystems;

namespace Fateforged.Simulation.Combat;

/// <summary>
/// Pure deterministic projectile simulation operating on SimProjectileData.
/// Mirrors Projectile3D path-based movement and hit detection.
/// No Godot node dependencies — operates on MatchState data only.
/// </summary>
public static class SimProjectile
{
    // Constants matching Projectile3D
    private const float DirectHitDistanceThreshold = 2.5f;
    private const float VeerReferenceDistance = 25f;
    private const float VeerMinDistance = 12f;

    // Arc path constants matching ArcPath.cs
    private const float ArcFullArcDistance = 5.0f;

    // Ballistic constants matching BallisticPath.cs
    private const float BallisticDefaultGravity = 9.8f;
    private const float BallisticMinTime = 0.01f;

    /// <summary>
    /// Spawn a new projectile in MatchState.
    /// Called by SimBehavior when a ranged unit attacks.
    /// </summary>
    public static int Spawn(
        MatchState state,
        int sourceUnitId, int targetUnitId, Team team,
        float damage, int sourceElementId,
        ProjectileMovementType movementType, float speed, float lifetime,
        SimVector3 startPos, SimVector3 targetPos,
        float arcHeight = 0f, int pierceCount = 0, float aoeRadius = 0f,
        float hitRadius = 2.5f, float steerStrength = 180f,
        float veerDelay = 0.15f, float veerAngle = 25f, float veerDuration = 0.25f)
    {
        int id = state.NextProjectileId();

        var proj = new SimProjectileData
        {
            ProjectileId = id,
            SourceUnitId = sourceUnitId,
            TargetUnitId = targetUnitId,
            Team = team,
            Damage = damage,
            SourceElementId = sourceElementId,
            MovementType = movementType,
            StartPosition = startPos,
            TargetPosition = targetPos,
            CurrentPosition = startPos,
            LastPosition = startPos,
            Speed = speed,
            Lifetime = lifetime,
            ArcHeight = arcHeight,
            PierceRemaining = pierceCount,
            AoeRadius = aoeRadius,
            HitRadius = hitRadius,
            SteerStrength = steerStrength,
        };

        // Initialize direction
        var dir = (targetPos - startPos);
        if (dir.LengthSquared() > 0.001f)
            proj.Direction = dir.Normalized();
        else
            proj.Direction = SimVector3.Forward;

        // Movement-type-specific initialization
        switch (movementType)
        {
            case ProjectileMovementType.Straight:
                proj.PathLength = startPos.DistanceTo(targetPos);
                break;

            case ProjectileMovementType.Homing:
                proj.PathLength = startPos.DistanceTo(targetPos);
                break;

            case ProjectileMovementType.Arc:
                proj.PathLength = EstimateArcLength(startPos, targetPos, arcHeight);
                break;

            case ProjectileMovementType.Ballistic:
                InitBallistic(proj, startPos, targetPos, speed);
                break;

            case ProjectileMovementType.WeavingHoming:
                InitWeavingHoming(proj, startPos, targetPos, speed,
                    veerDelay, veerAngle, veerDuration, state.Rng);
                break;
        }

        state.Projectiles[id] = proj;
        return id;
    }

    /// <summary>
    /// Tick all projectiles: advance movement, check hits, remove dead.
    /// </summary>
    public static void TickAll(MatchState state, float delta, List<SimEvent> events)
    {
        var toRemove = new List<int>();

        foreach (var kvp in state.Projectiles)
        {
            var proj = kvp.Value;
            if (proj.IsDead)
            {
                toRemove.Add(kvp.Key);
                continue;
            }

            // Save last position for line-segment hit detection
            proj.LastPosition = proj.CurrentPosition;
            proj.TimeAlive += delta;

            // Check lifetime
            if (proj.TimeAlive >= proj.Lifetime)
            {
                proj.IsDead = true;
                toRemove.Add(kvp.Key);
                continue;
            }

            // Advance movement
            switch (proj.MovementType)
            {
                case ProjectileMovementType.Straight:
                    TickStraight(proj, delta);
                    break;
                case ProjectileMovementType.Homing:
                    TickHoming(proj, state, delta);
                    break;
                case ProjectileMovementType.Arc:
                    TickArc(proj, delta);
                    break;
                case ProjectileMovementType.Ballistic:
                    TickBallistic(proj, delta);
                    break;
                case ProjectileMovementType.WeavingHoming:
                    TickWeavingHoming(proj, state, delta, events);
                    break;
            }

            // Hit detection (line segment from LastPosition to CurrentPosition)
            if (!proj.IsDead)
            {
                CheckHits(proj, state, events);
            }

            // Path completion check (for path-based types)
            if (!proj.IsDead && proj.MovementType != ProjectileMovementType.WeavingHoming
                && proj.MovementType != ProjectileMovementType.Homing
                && proj.Progress >= 1f)
            {
                // Check direct hit on target at path end
                var target = state.GetAliveUnit(proj.TargetUnitId);
                if (target != null)
                {
                    float dist = proj.CurrentPosition.DistanceTo(target.Position);
                    if (dist < DirectHitDistanceThreshold)
                    {
                        ApplyHit(proj, target, state, events);
                    }
                }

                // AoE on expire
                if (proj.AoeRadius > 0)
                    ApplyAoE(proj, proj.CurrentPosition, state, events);

                proj.IsDead = true;
            }

            if (proj.IsDead)
                toRemove.Add(kvp.Key);
        }

        // Remove dead projectiles
        foreach (var id in toRemove)
            state.Projectiles.Remove(id);
    }

    // =========================================================================
    // PATH MOVEMENT
    // =========================================================================

    private static void TickStraight(SimProjectileData proj, float delta)
    {
        if (proj.PathLength < 0.01f)
        {
            proj.Progress = 1f;
            return;
        }

        proj.Progress += (proj.Speed * delta) / proj.PathLength;
        proj.Progress = MathF.Min(proj.Progress, 1f);

        // Linear interpolation
        proj.CurrentPosition = proj.StartPosition.Lerp(proj.TargetPosition, proj.Progress);
        proj.Direction = (proj.TargetPosition - proj.StartPosition);
        if (proj.Direction.LengthSquared() > 0.001f)
            proj.Direction = proj.Direction.Normalized();
    }

    private static void TickArc(SimProjectileData proj, float delta)
    {
        if (proj.PathLength < 0.01f)
        {
            proj.Progress = 1f;
            return;
        }

        proj.Progress += (proj.Speed * delta) / proj.PathLength;
        proj.Progress = MathF.Min(proj.Progress, 1f);

        // Quadratic Bézier: B(t) = (1-t)²P0 + 2(1-t)tP1 + t²P2
        float t = proj.Progress;
        var controlPoint = ComputeArcControlPoint(proj.StartPosition, proj.TargetPosition, proj.ArcHeight);

        float u = 1f - t;
        proj.CurrentPosition = (u * u * proj.StartPosition) + (2f * u * t * controlPoint) + (t * t * proj.TargetPosition);

        // Direction from Bézier derivative
        var tangent = (2f * u * (controlPoint - proj.StartPosition)) + (2f * t * (proj.TargetPosition - controlPoint));
        if (tangent.LengthSquared() > 0.001f)
            proj.Direction = tangent.Normalized();
    }

    private static void TickHoming(SimProjectileData proj, MatchState state, float delta)
    {
        var target = state.GetAliveUnit(proj.TargetUnitId);
        if (target != null)
            proj.TargetPosition = target.Position;

        var toTarget = proj.TargetPosition - proj.CurrentPosition;
        if (toTarget.LengthSquared() > 0.001f)
        {
            var desiredDirection = toTarget.Normalized();
            SteerToward(proj, desiredDirection, delta);
        }

        proj.CurrentPosition += proj.Direction * (proj.Speed * delta);
    }

    private static void TickBallistic(SimProjectileData proj, float delta)
    {
        if (proj.TotalTime < BallisticMinTime)
        {
            proj.Progress = 1f;
            return;
        }

        proj.Progress += (proj.Speed * delta) / proj.PathLength;
        proj.Progress = MathF.Min(proj.Progress, 1f);

        float time = proj.Progress * proj.TotalTime;

        // Horizontal: start + velocity * time
        var pos = proj.StartPosition + proj.HorizontalVelocity * time;

        // Vertical: y0 + v0*t - 0.5*g*t²
        pos.Y = proj.StartPosition.Y + proj.InitialVerticalVelocity * time - 0.5f * proj.Gravity * time * time;

        proj.CurrentPosition = pos;

        // Direction from velocity at time t
        var velocity = proj.HorizontalVelocity;
        velocity.Y = proj.InitialVerticalVelocity - proj.Gravity * time;
        if (velocity.LengthSquared() > 0.001f)
            proj.Direction = velocity.Normalized();
    }

    private static void TickWeavingHoming(SimProjectileData proj, MatchState state, float delta, List<SimEvent> events)
    {
        proj.PhaseTimer += delta;

        // Update target position if target is still alive
        var target = state.GetAliveUnit(proj.TargetUnitId);
        if (target != null)
            proj.TargetPosition = target.Position;

        switch (proj.WeavingPhase)
        {
            case WeavingPhase.Straight:
                if (proj.PhaseTimer >= proj.ScaledVeerDelay)
                {
                    proj.WeavingPhase = WeavingPhase.Veering;
                    proj.PhaseTimer = 0f;
                }
                else
                {
                    var toTarget = (proj.TargetPosition - proj.CurrentPosition);
                    if (toTarget.LengthSquared() > 0.001f)
                        proj.Direction = toTarget.Normalized();
                }
                break;

            case WeavingPhase.Veering:
                if (proj.PhaseTimer >= proj.ScaledVeerDuration)
                {
                    proj.WeavingPhase = WeavingPhase.Homing;
                    proj.PhaseTimer = 0f;
                }
                else
                {
                    SteerToward(proj, proj.VeerDirection, delta);
                }
                break;

            case WeavingPhase.Homing:
                var toTarget2 = (proj.TargetPosition - proj.CurrentPosition);
                if (toTarget2.LengthSquared() > 0.001f)
                    SteerToward(proj, toTarget2.Normalized(), delta);

                // Direct hit check
                if (target != null)
                {
                    float dist = proj.CurrentPosition.DistanceTo(target.Position);
                    if (dist < DirectHitDistanceThreshold)
                    {
                        ApplyHit(proj, target, state, events);
                        proj.IsDead = true;
                        return;
                    }
                }
                break;
        }

        // Apply velocity
        proj.Velocity = proj.Direction * proj.Speed;
        proj.CurrentPosition += proj.Velocity * delta;
    }

    /// <summary>
    /// Gradually steer the projectile toward a target direction.
    /// Mirrors Projectile3D.SteerToward().
    /// </summary>
    private static void SteerToward(SimProjectileData proj, SimVector3 targetDirection, float delta)
    {
        if (targetDirection.LengthSquared() < 0.001f)
            return;

        targetDirection = targetDirection.Normalized();

        float dot = proj.Direction.Dot(targetDirection);
        dot = SimMath.Clamp(dot, -1f, 1f);
        float angleBetween = MathF.Acos(dot);

        float maxRotation = SimMath.DegToRad(proj.SteerStrength) * delta;

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
    // HIT DETECTION
    // =========================================================================

    /// <summary>
    /// Check for hits against enemy units using line-segment distance check.
    /// </summary>
    private static void CheckHits(SimProjectileData proj, MatchState state, List<SimEvent> events)
    {
        foreach (var kvp in state.Units)
        {
            var unit = kvp.Value;
            if (!unit.IsAlive) continue;
            if (unit.Team == proj.Team) continue; // Don't hit friendly units
            if (unit.UnitId == proj.SourceUnitId) continue; // Don't hit source

            float dist = PointToSegmentDistance(unit.Position, proj.LastPosition, proj.CurrentPosition);
            if (dist <= proj.HitRadius)
            {
                ApplyHit(proj, unit, state, events);

                if (proj.PierceRemaining <= 0)
                {
                    // AoE on hit
                    if (proj.AoeRadius > 0)
                        ApplyAoE(proj, proj.CurrentPosition, state, events);

                    proj.IsDead = true;
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Apply damage to a target unit from a projectile hit.
    /// Projectile hits intentionally skip OnHit/OnDamaged triggers — projectile damage
    /// goes through a separate pipeline (SimDamage.Calculate) and does not interact with
    /// the trigger system. Triggers are reserved for melee combat in SimBehavior.
    /// </summary>
    private static void ApplyHit(SimProjectileData proj, UnitData target, MatchState state, List<SimEvent> events)
    {
        var (sourceUnit, attackerSummoner, targetSummoner) = ResolveSourceAndSummoners(proj, target, state);

        var (damage, isCrit) = SimDamage.Calculate(
            proj.Damage, sourceUnit, target, attackerSummoner, targetSummoner, state.Rng);

        target.CurrentHp -= damage;
        events.Add(new UnitDamagedEvent(target.UnitId, proj.SourceUnitId, damage, isCrit));

        if (target.CurrentHp <= 0)
        {
            SimUtils.KillUnit(state, target, proj.SourceUnitId, events);
            SimEffects.FireDeathTriggers(state, target, sourceUnit, events);
        }

        proj.PierceRemaining--;
        events.Add(new ProjectileHitEvent(proj.ProjectileId, target.UnitId));
    }

    /// <summary>
    /// Apply AoE damage to all enemy units within radius.
    /// AoE effects intentionally skip per-unit OnHit/OnDamaged triggers to avoid
    /// trigger avalanches when many units are hit simultaneously.
    /// </summary>
    private static void ApplyAoE(SimProjectileData proj, SimVector3 center, MatchState state, List<SimEvent> events)
    {
        float radiusSq = proj.AoeRadius * proj.AoeRadius;
        var sourceUnit = state.Units.TryGetValue(proj.SourceUnitId, out var src) ? src : null;
        SummonerData? attackerSummoner = sourceUnit != null ? state.Summoners[(int)sourceUnit.Team] : null;

        foreach (var kvp in state.Units)
        {
            var unit = kvp.Value;
            if (!unit.IsAlive) continue;
            if (unit.Team == proj.Team) continue;

            float distSq = center.DistanceSquaredTo(unit.Position);
            if (distSq > radiusSq) continue;

            var targetSummoner = state.Summoners[(int)unit.Team];
            var (damage, isCrit) = SimDamage.Calculate(
                proj.Damage, sourceUnit, unit, attackerSummoner, targetSummoner, state.Rng);

            unit.CurrentHp -= damage;
            events.Add(new UnitDamagedEvent(unit.UnitId, proj.SourceUnitId, damage, isCrit));

            if (unit.CurrentHp <= 0)
            {
                SimUtils.KillUnit(state, unit, proj.SourceUnitId, events);
                SimEffects.FireDeathTriggers(state, unit, sourceUnit, events);
            }
        }
    }

    /// <summary>
    /// Resolve the source unit and both summoners for damage calculation.
    /// Shared by ApplyHit and ApplyAoE to eliminate duplicated lookup.
    /// </summary>
    private static (UnitData? sourceUnit, SummonerData? attackerSummoner, SummonerData? targetSummoner)
        ResolveSourceAndSummoners(SimProjectileData proj, UnitData target, MatchState state)
    {
        var sourceUnit = state.Units.TryGetValue(proj.SourceUnitId, out var src) ? src : null;
        SummonerData? attackerSummoner = sourceUnit != null ? state.Summoners[(int)sourceUnit.Team] : null;
        var targetSummoner = state.Summoners[(int)target.Team];
        return (sourceUnit, attackerSummoner, targetSummoner);
    }

    // =========================================================================
    // INITIALIZATION HELPERS
    // =========================================================================

    private static void InitBallistic(SimProjectileData proj, SimVector3 start, SimVector3 end, float speed)
    {
        var displacement = end - start;
        float horizontalDist = MathF.Sqrt(displacement.X * displacement.X + displacement.Z * displacement.Z);
        float verticalDist = displacement.Y;

        proj.TotalTime = MathF.Max(horizontalDist / speed, BallisticMinTime);

        // v0 = (y + 0.5*g*t²) / t
        proj.InitialVerticalVelocity = (verticalDist + 0.5f * proj.Gravity * proj.TotalTime * proj.TotalTime) / proj.TotalTime;

        var horizontalDir = new SimVector3(displacement.X, 0, displacement.Z);
        if (horizontalDir.LengthSquared() > 0.001f)
            horizontalDir = horizontalDir.Normalized();
        proj.HorizontalVelocity = horizontalDir * speed;

        proj.PathLength = EstimateBallisticLength(proj);
    }

    private static void InitWeavingHoming(
        SimProjectileData proj, SimVector3 start, SimVector3 target, float speed,
        float veerDelay, float veerAngle, float veerDuration,
        DeterministicRng? rng)
    {
        proj.WeavingPhase = WeavingPhase.Straight;
        proj.PhaseTimer = 0f;
        proj.Velocity = proj.Direction * speed;

        float distance = start.DistanceTo(target);
        float distanceScale = SimMath.Clamp(distance / VeerReferenceDistance, 0f, 1f);

        if (distance < VeerMinDistance)
        {
            proj.ScaledVeerDelay = 0f;
            proj.ScaledVeerDuration = 0f;
            proj.WeavingPhase = WeavingPhase.Homing;
        }
        else
        {
            proj.ScaledVeerDelay = veerDelay * distanceScale;
            proj.ScaledVeerDuration = veerDuration * distanceScale;
            float scaledVeerAngle = veerAngle * distanceScale;

            // Random left/right veer using deterministic RNG
            float veerSign = (rng != null && rng.NextFloat() > 0.5f) ? 1f : -1f;
            float veerRadians = SimMath.DegToRad(scaledVeerAngle) * veerSign;

            proj.VeerDirection = RotateAround(proj.Direction, SimVector3.Up, veerRadians).Normalized();
        }

        // WeavingHoming doesn't use PathLength (velocity-based, not progress-based)
        proj.PathLength = 0f;
    }

    // =========================================================================
    // GEOMETRY HELPERS
    // =========================================================================

    /// <summary>
    /// Compute the minimum distance from a point to a line segment.
    /// </summary>
    private static float PointToSegmentDistance(SimVector3 point, SimVector3 segA, SimVector3 segB)
    {
        var ab = segB - segA;
        float abLenSq = ab.LengthSquared();
        if (abLenSq < 0.000001f)
            return point.DistanceTo(segA);

        float t = SimMath.Clamp(ab.Dot(point - segA) / abLenSq, 0f, 1f);
        var closest = segA + ab * t;
        return point.DistanceTo(closest);
    }

    /// <summary>
    /// Compute arc control point (matches ArcPath.RecalculateControlPoint).
    /// </summary>
    private static SimVector3 ComputeArcControlPoint(SimVector3 start, SimVector3 end, float arcHeight)
    {
        float distance = start.DistanceTo(end);
        float arcScale = SimMath.Clamp(distance / ArcFullArcDistance, 0f, 1f);
        float effectiveArcHeight = arcHeight * arcScale;

        return (start + end) / 2f + SimVector3.Up * effectiveArcHeight;
    }

    /// <summary>
    /// Estimate arc path length using line segments (matches ArcPath.EstimateLength).
    /// </summary>
    private static float EstimateArcLength(SimVector3 start, SimVector3 end, float arcHeight)
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

    /// <summary>
    /// Rotate a vector around an axis by the given angle (radians).
    /// Uses Rodrigues' rotation formula. Replaces Godot Vector3.Rotated().
    /// </summary>
    private static SimVector3 RotateAround(SimVector3 v, SimVector3 axis, float angle)
    {
        axis = axis.Normalized();
        float cos = MathF.Cos(angle);
        float sin = MathF.Sin(angle);
        // Rodrigues: v*cos + (axis x v)*sin + axis*(axis . v)*(1 - cos)
        return v * cos + axis.Cross(v) * sin + axis * (axis.Dot(v) * (1f - cos));
    }

    /// <summary>
    /// Estimate ballistic path length using line segments (matches BallisticPath.EstimateLength).
    /// </summary>
    private static float EstimateBallisticLength(SimProjectileData proj)
    {
        const int segments = 16;
        float length = 0f;

        var prev = proj.StartPosition;
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            float time = t * proj.TotalTime;

            var pos = proj.StartPosition + proj.HorizontalVelocity * time;
            pos.Y = proj.StartPosition.Y + proj.InitialVerticalVelocity * time - 0.5f * proj.Gravity * time * time;

            length += prev.DistanceTo(pos);
            prev = pos;
        }

        return MathF.Max(length, 0.1f);
    }
}
