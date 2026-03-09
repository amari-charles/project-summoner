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
/// Movement/steering/geometry helpers are delegated to ProjectileMovement (shared with client).
/// No Godot node dependencies — operates on MatchState data only.
/// </summary>
public static class SimProjectile
{
    private const float GeometryEpsilon = 0.000001f;

    // Static reusable buffers to avoid per-frame heap allocations.
    // NOT thread-safe — TickAll must only be called from the physics thread.
    private static readonly List<int> _toRemoveBuffer = new();
    private static readonly List<PendingHit> _pendingHitsBuffer = new();

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
        SimProjectileCatalogId projectileCatalogId = default,
        float acceleration = 0f, float minSpeed = 1f,
        float? speedStart = null, float? speedEnd = null,
        float speedTransitionDuration = 1f, SpeedEasingType speedEasing = SpeedEasingType.Linear,
        float speedEaseExponent = 2f,
        bool tracking = false)
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
            Tracking = tracking,
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
                proj.PathLength = ProjectileMovement.EstimateArcLength(startPos, targetPos, arcHeight);
                break;

            case ProjectileMovementType.Ballistic:
                ProjectileMovement.InitBallistic(proj, startPos, targetPos, speed);
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
        _toRemoveBuffer.Clear();

        foreach (var kvp in state.Projectiles)
        {
            var proj = kvp.Value;
            if (proj.IsDead)
            {
                _toRemoveBuffer.Add(kvp.Key);
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
            ProjectileMovement.TickSpeed(proj, delta);

            // Check lifetime
            if (proj.TimeAlive >= proj.Lifetime)
            {
                proj.IsDead = true;
                _toRemoveBuffer.Add(kvp.Key);
                continue;
            }

            // Advance movement
            switch (proj.MovementType)
            {
                case ProjectileMovementType.Straight:
                    TickStraight(proj, state, delta);
                    break;
                case ProjectileMovementType.Homing:
                    TickHoming(proj, state, delta);
                    break;
                case ProjectileMovementType.Arc:
                    TickArc(proj, state, delta);
                    break;
                case ProjectileMovementType.Ballistic:
                    ProjectileMovement.TickBallistic(proj, delta);
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
                _toRemoveBuffer.Add(kvp.Key);
        }

        // Remove dead projectiles
        foreach (var id in _toRemoveBuffer)
            state.Projectiles.Remove(id);
    }

    // =========================================================================
    // PATH MOVEMENT (simulation-specific wrappers with MatchState access)
    // =========================================================================

    private static void TickStraight(SimProjectileData proj, MatchState state, float delta)
    {
        ProjectileMovement.TickStraight(proj, delta, unitId =>
        {
            var target = state.GetAliveUnit(unitId);
            return target?.Position;
        });
    }

    private static void TickArc(SimProjectileData proj, MatchState state, float delta)
    {
        ProjectileMovement.TickArc(proj, delta, unitId =>
        {
            var target = state.GetAliveUnit(unitId);
            return target?.Position;
        });
    }

    private static void TickHoming(SimProjectileData proj, MatchState state, float delta)
    {
        var target = state.GetAliveUnit(proj.TargetUnitId);
        if (target != null)
        {
            proj.TargetPosition = target.Position;
            proj.TargetLost = false;
        }
        else if (!proj.TargetLost)
        {
            proj.TargetLost = true;
            proj.PreviousDistanceToTarget = proj.CurrentPosition.DistanceTo(proj.TargetPosition);
        }

        var toTarget = proj.TargetPosition - proj.CurrentPosition;
        if (toTarget.LengthSquared() > 0.001f)
        {
            var desiredDirection = toTarget.Normalized();
            ProjectileMovement.SteerToward(proj, desiredDirection, delta);
        }

        proj.CurrentPosition += proj.Direction * (proj.Speed * delta);

        // Kill homing projectile after it passes the dead target's last known position
        if (proj.TargetLost)
        {
            float currentDist = proj.CurrentPosition.DistanceTo(proj.TargetPosition);
            if (currentDist > proj.PreviousDistanceToTarget)
            {
                proj.IsDead = true;
                return;
            }
            proj.PreviousDistanceToTarget = currentDist;
        }
    }

    private static void TickWeavingHoming(SimProjectileData proj, MatchState state, float delta, List<SimEvent> events)
    {
        proj.PhaseTimer += delta;

        // Update target position if target is still alive
        var target = state.GetAliveUnit(proj.TargetUnitId);
        if (target != null)
        {
            proj.TargetPosition = target.Position;
            proj.TargetLost = false;
        }
        else if (!proj.TargetLost)
        {
            proj.TargetLost = true;
            proj.PreviousDistanceToTarget = proj.CurrentPosition.DistanceTo(proj.TargetPosition);
        }

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
                    var outTarget = ProjectileMovement.BlendWithTarget(proj, proj.VeerDirection, WeavingHomingTuning.BlendOutTargetWeight);
                    ProjectileMovement.SteerToward(proj, outTarget, delta);
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
                    var backTarget = ProjectileMovement.BlendWithTarget(proj, proj.CounterVeerDirection, WeavingHomingTuning.BlendBackTargetWeight);
                    ProjectileMovement.SteerToward(proj, backTarget, delta);
                }
                break;

            case WeavingPhase.Homing:
            {
                var toTarget = (proj.TargetPosition - proj.CurrentPosition);
                if (toTarget.LengthSquared() > 0.001f)
                {
                    float distanceToTarget = toTarget.Length();
                    bool finalLock = distanceToTarget <= WeavingHomingTuning.HomingFinalLockDistance
                                     || proj.PhaseTimer >= WeavingHomingTuning.HomingFinalLockTime;
                    var homingDirection = finalLock ? toTarget.Normalized() : ProjectileMovement.ApplyHomingWeave(proj, toTarget);
                    float settle = SimMath.Clamp(distanceToTarget / WeavingHomingTuning.HomingWeaveSettleDistance, 0f, 1f);
                    float steerScale = finalLock
                        ? 1f
                        : (WeavingHomingTuning.HomingFarSteerScale
                           + ((1f - WeavingHomingTuning.HomingFarSteerScale) * (1f - settle)));
                    ProjectileMovement.SteerToward(proj, homingDirection, delta, steerScale);
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
        }

        // Apply velocity
        proj.Velocity = proj.Direction * proj.Speed;
        proj.CurrentPosition += proj.Velocity * delta;

        // Refresh direction from actual frame travel so visuals bank with weave motion.
        var frameTravel = proj.CurrentPosition - proj.LastPosition;
        if (frameTravel.LengthSquared() > 0.001f)
            proj.Direction = frameTravel.Normalized();

        // Kill weaving projectile after it passes the dead target's last known position
        if (proj.TargetLost && proj.WeavingPhase == WeavingPhase.Homing)
        {
            float currentDist = proj.CurrentPosition.DistanceTo(proj.TargetPosition);
            if (currentDist > proj.PreviousDistanceToTarget)
            {
                proj.IsDead = true;
                return;
            }
            proj.PreviousDistanceToTarget = currentDist;
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
        _pendingHitsBuffer.Clear();

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
                _pendingHitsBuffer.Add(new PendingHit(unit, segmentT, distSq));
            }
        }

        if (_pendingHitsBuffer.Count == 0)
            return;

        _pendingHitsBuffer.Sort((a, b) =>
        {
            int byT = a.SegmentT.CompareTo(b.SegmentT);
            if (byT != 0)
                return byT;

            int byDist = a.DistanceSq.CompareTo(b.DistanceSq);
            if (byDist != 0)
                return byDist;

            return a.Unit.UnitId.CompareTo(b.Unit.UnitId);
        });

        foreach (var hit in _pendingHitsBuffer)
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

    private static void InitWeavingHoming(
        SimProjectileData proj, SimVector3 start, SimVector3 target, float speed,
        float veerDelay, float veerAngle, float veerDuration,
        DeterministicRng? rng)
    {
        proj.WeavingPhase = WeavingPhase.Straight;
        proj.PhaseTimer = 0f;
        proj.Velocity = proj.Direction * speed;

        float distance = start.DistanceTo(target);
        float distanceScale = SimMath.Clamp(distance / WeavingHomingTuning.VeerReferenceDistance, 0f, 1f);

        if (distance < WeavingHomingTuning.VeerMinDistance)
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
            proj.ScaledCounterVeerDuration = weaveDuration * WeavingHomingTuning.CounterVeerDurationRatio;
            float scaledVeerAngle = veerAngle * distanceScale;

            // Random left/right veer using deterministic RNG
            float veerSign = (rng != null && rng.NextFloat() > 0.5f) ? 1f : -1f;
            float pitchSign = (rng != null && rng.NextFloat() > 0.5f) ? 1f : -1f;
            float veerYawRadians = SimMath.DegToRad(scaledVeerAngle) * veerSign;
            float veerPitchRadians = SimMath.DegToRad(scaledVeerAngle * WeavingHomingTuning.VeerPitchRatio) * pitchSign;

            var rightAxis = ProjectileMovement.GetStableRightAxis(proj.Direction);
            var outDir = ProjectileMovement.RotateAround(proj.Direction, SimVector3.Up, veerYawRadians);
            outDir = ProjectileMovement.RotateAround(outDir, rightAxis, veerPitchRadians);

            var backDir = ProjectileMovement.RotateAround(proj.Direction, SimVector3.Up, -veerYawRadians * WeavingHomingTuning.VeerCounterYawRatio);
            backDir = ProjectileMovement.RotateAround(backDir, rightAxis, -veerPitchRadians * WeavingHomingTuning.VeerCounterPitchRatio);

            proj.VeerDirection = outDir.Normalized();
            proj.CounterVeerDirection = backDir.Normalized();
        }

        // WeavingHoming doesn't use PathLength (velocity-based, not progress-based)
        proj.PathLength = 0f;
    }

    // =========================================================================
    // SIM-ONLY HELPERS (hit detection geometry)
    // =========================================================================

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

        return unit.MovementLayer == MovementLayer.Ground;
    }

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
}
