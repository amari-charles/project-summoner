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
    private const float VeerReferenceDistance = 10f;
    private const float VeerMinDistance = 3f;
    private const float VeerPitchRatio = 0.45f;
    private const float VeerCounterYawRatio = 0.85f;
    private const float VeerCounterPitchRatio = 0.6f;
    private const float HomingWeaveSettleDistance = 10.0f;
    private const float HomingWeaveYawRatio = 0.55f;
    private const float HomingWeavePitchRatio = 0.28f;
    private const float HomingWeaveFrequency = 8.5f;
    private const float HomingWeavePitchFrequency = 6.2f;
    private const float HomingFarSteerScale = 0.38f;
    private const float HomingFinalLockDistance = 6.0f;
    private const float HomingFinalLockTime = 0.55f;

    // Arc path constants matching ArcPath.cs
    private const float ArcFullArcDistance = 5.0f;

    // Ballistic constants matching BallisticPath.cs
    private const float BallisticDefaultGravity = 9.8f;
    private const float BallisticMinTime = 0.01f;
    private const float GeometryEpsilon = 0.000001f;

    private readonly struct PendingHit
    {
        public PendingHit(UnitData unit, float segmentT, float distanceSq)
        {
            Unit = unit;
            SegmentT = segmentT;
            DistanceSq = distanceSq;
        }

        public UnitData Unit { get; }
        public float SegmentT { get; }
        public float DistanceSq { get; }
    }

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
        float hitRadius = 2.5f, ProjectileHitSpace hitSpace = ProjectileHitSpace.GroundCylinder, float steerStrength = 180f,
        float veerDelay = 0.15f, float veerAngle = 25f, float veerDuration = 0.25f,
        string projectileCatalogId = "",
        float acceleration = 0f, float minSpeed = 1f,
        float? speedStart = null, float? speedEnd = null,
        float speedTransitionDuration = 1f, SpeedEasingType speedEasing = SpeedEasingType.Linear,
        float speedEaseExponent = 2f)
    {
        int id = state.NextProjectileId();
        bool useSpeedEasing = speedStart.HasValue || speedEnd.HasValue;
        float resolvedSpeedStart = speedStart ?? speed;
        float resolvedSpeedEnd = speedEnd ?? speed;

        var proj = new SimProjectileData
        {
            ProjectileId = id,
            ProjectileCatalogId = projectileCatalogId,
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
            // Delay simulation by one tick so newly-spawned projectiles render at least one frame.
            TimeAlive = -1f,
            Speed = useSpeedEasing ? resolvedSpeedStart : speed,
            Acceleration = acceleration,
            MinSpeed = minSpeed,
            UseSpeedEasing = useSpeedEasing,
            SpeedStart = resolvedSpeedStart,
            SpeedEnd = resolvedSpeedEnd,
            SpeedTransitionDuration = speedTransitionDuration,
            SpeedEasing = speedEasing,
            SpeedEaseExponent = speedEaseExponent,
            Lifetime = lifetime,
            ArcHeight = arcHeight,
            PierceRemaining = pierceCount,
            AoeRadius = aoeRadius,
            HitRadius = hitRadius,
            HitSpace = hitSpace,
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

            // First frame after spawn: keep projectile alive and visible for render sync.
            if (proj.TimeAlive < 0f)
            {
                proj.TimeAlive = 0f;
                continue;
            }

            // Save last position for line-segment hit detection
            proj.LastPosition = proj.CurrentPosition;
            proj.TimeAlive += delta;
            TickSpeed(proj, delta);

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
                if (target != null && !proj.HitUnitIds.Contains(target.UnitId))
                {
                    if (CanHitUnitAtPoint(proj, target, proj.CurrentPosition))
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

    private static void TickSpeed(SimProjectileData proj, float delta)
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

    private static float EvaluateSpeedEasing(float t, SpeedEasingType easingType, float exponent)
    {
        float clampedT = SimMath.Clamp(t, 0f, 1f);
        float safeExponent = MathF.Max(exponent, 0.0001f);
        return easingType switch
        {
            SpeedEasingType.EaseIn => MathF.Pow(clampedT, safeExponent),
            SpeedEasingType.EaseOut => 1f - MathF.Pow(1f - clampedT, safeExponent),
            SpeedEasingType.EaseInOut => (1f - MathF.Cos(clampedT * MathF.PI)) * 0.5f,
            _ => clampedT
        };
    }

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
                    proj.WeavingPhase = WeavingPhase.VeeringOut;
                    proj.PhaseTimer = 0f;
                }
                else
                {
                    var toTarget = (proj.TargetPosition - proj.CurrentPosition);
                    if (toTarget.LengthSquared() > 0.001f)
                        proj.Direction = toTarget.Normalized();
                }
                break;

            case WeavingPhase.VeeringOut:
                if (proj.PhaseTimer >= proj.ScaledVeerDuration)
                {
                    proj.WeavingPhase = WeavingPhase.VeeringBack;
                    proj.PhaseTimer = 0f;
                }
                else
                {
                    var outTarget = BlendWithTarget(proj, proj.VeerDirection, 0.06f);
                    SteerToward(proj, outTarget, delta);
                }
                break;

            case WeavingPhase.VeeringBack:
                if (proj.PhaseTimer >= proj.ScaledCounterVeerDuration)
                {
                    proj.WeavingPhase = WeavingPhase.Homing;
                    proj.PhaseTimer = 0f;
                }
                else
                {
                    var backTarget = BlendWithTarget(proj, proj.CounterVeerDirection, 0.12f);
                    SteerToward(proj, backTarget, delta);
                }
                break;

            case WeavingPhase.Homing:
                var toTarget2 = (proj.TargetPosition - proj.CurrentPosition);
                if (toTarget2.LengthSquared() > 0.001f)
                {
                    float distanceToTarget = toTarget2.Length();
                    bool finalLock = distanceToTarget <= HomingFinalLockDistance || proj.PhaseTimer >= HomingFinalLockTime;
                    var homingDirection = finalLock ? toTarget2.Normalized() : ApplyHomingWeave(proj, toTarget2);
                    float settle = SimMath.Clamp(distanceToTarget / HomingWeaveSettleDistance, 0f, 1f);
                    float steerScale = finalLock
                        ? 1f
                        : (HomingFarSteerScale + ((1f - HomingFarSteerScale) * (1f - settle)));
                    SteerToward(proj, homingDirection, delta, steerScale);
                }

                // Direct hit check
                if (target != null && !proj.HitUnitIds.Contains(target.UnitId))
                {
                    if (CanHitUnitAtPoint(proj, target, proj.CurrentPosition))
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

        // Refresh direction from actual frame travel so visuals bank with weave motion.
        var frameTravel = proj.CurrentPosition - proj.LastPosition;
        if (frameTravel.LengthSquared() > 0.001f)
            proj.Direction = frameTravel.Normalized();
    }

    /// <summary>
    /// Gradually steer the projectile toward a target direction.
    /// Mirrors Projectile3D.SteerToward().
    /// </summary>
    private static void SteerToward(SimProjectileData proj, SimVector3 targetDirection, float delta, float steerScale = 1f)
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
    // HIT DETECTION
    // =========================================================================

    /// <summary>
    /// Check for hits against enemy units using line-segment distance check.
    /// </summary>
    private static void CheckHits(SimProjectileData proj, MatchState state, List<SimEvent> events)
    {
        var pendingHits = new List<PendingHit>();

        foreach (var kvp in state.Units)
        {
            var unit = kvp.Value;
            if (!unit.IsAlive) continue;
            if (unit.Team == proj.Team) continue; // Don't hit friendly units
            if (unit.UnitId == proj.SourceUnitId) continue; // Don't hit source
            if (proj.HitUnitIds.Contains(unit.UnitId)) continue; // Never hit same unit twice

            float hitThreshold = MathF.Max(0f, proj.HitRadius + unit.SeparationRadius);
            if (TryGetSegmentDistanceAndT(proj, unit, proj.LastPosition, proj.CurrentPosition,
                    out float distSq, out float segmentT) &&
                distSq <= hitThreshold * hitThreshold)
            {
                pendingHits.Add(new PendingHit(unit, segmentT, distSq));
            }
        }

        if (pendingHits.Count == 0)
            return;

        pendingHits.Sort((a, b) =>
        {
            int byT = a.SegmentT.CompareTo(b.SegmentT);
            if (byT != 0)
                return byT;

            int byDist = a.DistanceSq.CompareTo(b.DistanceSq);
            if (byDist != 0)
                return byDist;

            return a.Unit.UnitId.CompareTo(b.Unit.UnitId);
        });

        foreach (var hit in pendingHits)
        {
            var unit = hit.Unit;
            if (!unit.IsAlive) continue;
            if (proj.HitUnitIds.Contains(unit.UnitId)) continue;
            if (unit.Team == proj.Team) continue;
            if (unit.UnitId == proj.SourceUnitId) continue;

            var impactPoint = proj.LastPosition.Lerp(proj.CurrentPosition, hit.SegmentT);
            ApplyHit(proj, unit, state, events);

            if (proj.PierceRemaining <= 0)
            {
                proj.CurrentPosition = impactPoint;

                // AoE on hit
                if (proj.AoeRadius > 0)
                    ApplyAoE(proj, impactPoint, state, events);

                proj.IsDead = true;
                return;
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

        proj.HitUnitIds.Add(target.UnitId);
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
        var sourceUnit = state.Units.TryGetValue(proj.SourceUnitId, out var src) ? src : null;
        SummonerData? attackerSummoner = sourceUnit != null ? state.Summoners[(int)sourceUnit.Team] : null;

        foreach (var kvp in state.Units)
        {
            var unit = kvp.Value;
            if (!unit.IsAlive) continue;
            if (unit.Team == proj.Team) continue;
            if (proj.HitUnitIds.Contains(unit.UnitId)) continue;

            float radius = MathF.Max(0f, proj.AoeRadius + unit.SeparationRadius);
            if (!CanHitUnitInRadius(proj, unit, center, radius))
                continue;

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
            proj.ScaledCounterVeerDuration = 0f;
            proj.WeavingPhase = WeavingPhase.Homing;
        }
        else
        {
            proj.ScaledVeerDelay = veerDelay * distanceScale;
            float weaveDuration = veerDuration * distanceScale;
            proj.ScaledVeerDuration = weaveDuration;
            // Slightly shorter counter-phase gives a tighter "S" before homing.
            proj.ScaledCounterVeerDuration = weaveDuration * 0.85f;
            float scaledVeerAngle = veerAngle * distanceScale;

            // Random left/right veer using deterministic RNG
            float veerSign = (rng != null && rng.NextFloat() > 0.5f) ? 1f : -1f;
            float pitchSign = (rng != null && rng.NextFloat() > 0.5f) ? 1f : -1f;
            float veerYawRadians = SimMath.DegToRad(scaledVeerAngle) * veerSign;
            float veerPitchRadians = SimMath.DegToRad(scaledVeerAngle * VeerPitchRatio) * pitchSign;

            var rightAxis = GetStableRightAxis(proj.Direction);
            var outDir = RotateAround(proj.Direction, SimVector3.Up, veerYawRadians);
            outDir = RotateAround(outDir, rightAxis, veerPitchRadians);

            var backDir = RotateAround(proj.Direction, SimVector3.Up, -veerYawRadians * VeerCounterYawRatio);
            backDir = RotateAround(backDir, rightAxis, -veerPitchRadians * VeerCounterPitchRatio);

            proj.VeerDirection = outDir.Normalized();
            proj.CounterVeerDirection = backDir.Normalized();
        }

        // WeavingHoming doesn't use PathLength (velocity-based, not progress-based)
        proj.PathLength = 0f;
    }

    // =========================================================================
    // GEOMETRY HELPERS
    // =========================================================================

    private static SimVector3 BlendWithTarget(SimProjectileData proj, SimVector3 weaveDirection, float targetWeight)
    {
        var toTarget = proj.TargetPosition - proj.CurrentPosition;
        if (toTarget.LengthSquared() <= 0.001f)
            return weaveDirection;

        var desired = (weaveDirection * (1f - targetWeight)) + (toTarget.Normalized() * targetWeight);
        if (desired.LengthSquared() <= 0.001f)
            return weaveDirection;

        return desired.Normalized();
    }

    private static SimVector3 ApplyHomingWeave(SimProjectileData proj, SimVector3 toTarget)
    {
        var targetDirection = toTarget.Normalized();
        float targetDistance = toTarget.Length();
        if (targetDistance <= GeometryEpsilon)
            return targetDirection;

        float arc = MathF.Max(0f, proj.ArcHeight);
        if (arc <= GeometryEpsilon)
            return targetDirection;

        float settle = SimMath.Clamp(targetDistance / HomingWeaveSettleDistance, 0f, 1f);
        float yawAmplitude = SimMath.DegToRad(proj.ScaledCounterVeerDuration > 0f
            ? proj.ScaledCounterVeerDuration * 220f
            : 0f);
        if (yawAmplitude <= GeometryEpsilon)
            yawAmplitude = SimMath.DegToRad(MathF.Max(22f, arc * 16f));

        float phase = (proj.TimeAlive * HomingWeaveFrequency) + (proj.ProjectileId * 0.37f);
        float yawOffset = MathF.Sin(phase) * yawAmplitude * HomingWeaveYawRatio * settle;
        float pitchOffset = MathF.Cos(phase * (HomingWeavePitchFrequency / HomingWeaveFrequency))
                            * yawAmplitude * HomingWeavePitchRatio * settle;

        var rightAxis = GetStableRightAxis(targetDirection);
        var woven = RotateAround(targetDirection, SimVector3.Up, yawOffset);
        woven = RotateAround(woven, rightAxis, pitchOffset);
        return woven.Normalized();
    }

    private static bool CanHitUnitAtPoint(SimProjectileData proj, UnitData unit, SimVector3 point)
    {
        float radius = MathF.Max(0f, proj.HitRadius + unit.SeparationRadius);
        return CanHitUnitInRadius(proj, unit, point, radius);
    }

    private static bool CanHitUnitInRadius(SimProjectileData proj, UnitData unit, SimVector3 center, float radius)
    {
        float radiusSq = radius * radius;
        return UseGroundCylinder(proj, unit)
            ? DistanceSquaredXZ(center, unit.Position) <= radiusSq
            : center.DistanceSquaredTo(unit.Position) <= radiusSq;
    }

    private static bool TryGetSegmentDistanceAndT(
        SimProjectileData proj, UnitData unit, SimVector3 segA, SimVector3 segB,
        out float distanceSq, out float segmentT)
    {
        if (UseGroundCylinder(proj, unit))
            return TryGetPointToSegmentDistanceSqXZ(unit.Position, segA, segB, out distanceSq, out segmentT);

        return TryGetPointToSegmentDistanceSq(unit.Position, segA, segB, out distanceSq, out segmentT);
    }

    private static bool UseGroundCylinder(SimProjectileData proj, UnitData unit)
    {
        if (proj.HitSpace == ProjectileHitSpace.Sphere3D)
            return false;

        // Ground-cylinder gameplay only applies to grounded targets.
        return unit.MovementLayer == MovementLayer.Ground;
    }

    /// <summary>
    /// Compute squared distance and segment-t for a 3D point-to-segment query.
    /// </summary>
    private static bool TryGetPointToSegmentDistanceSq(
        SimVector3 point, SimVector3 segA, SimVector3 segB, out float distanceSq, out float segmentT)
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

    /// <summary>
    /// Compute squared distance and segment-t for a 2D XZ point-to-segment query.
    /// </summary>
    private static bool TryGetPointToSegmentDistanceSqXZ(
        SimVector3 point, SimVector3 segA, SimVector3 segB, out float distanceSq, out float segmentT)
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

    private static float DistanceSquaredXZ(SimVector3 a, SimVector3 b)
    {
        float dx = a.X - b.X;
        float dz = a.Z - b.Z;
        return (dx * dx) + (dz * dz);
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

    private static SimVector3 GetStableRightAxis(SimVector3 forward)
    {
        var right = forward.Cross(SimVector3.Up);
        if (right.LengthSquared() < GeometryEpsilon)
            right = forward.Cross(SimVector3.Forward);
        if (right.LengthSquared() < GeometryEpsilon)
            right = SimVector3.Right;
        return right.Normalized();
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
