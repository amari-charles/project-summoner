using System;
using System.Collections.Generic;
using Fateforged.Data.Projectiles;
using Fateforged.Projectiles;
using Fateforged.Simulation;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Spatial;
using Fateforged.Simulation.Subsystems;
using Fateforged.Units;

namespace Fateforged.Simulation.Combat;

/// <summary>
/// Pure deterministic unit behavior state machine operating on UnitData.
/// Mirrors Unit3D.UpdateBehavior() logic: target acquisition, range checking,
/// constraint resolution, attack execution, and movement delegation.
///
/// State flow:
///   NoTarget → move forward
///   Chasing  → move toward target
///   InRange  → idle (waiting for cooldown or constraint)
///   Attacking → queue pending attack, reset cooldown
/// </summary>
public static class SimBehavior
{
    private const float BacklinerMaxCrossLaneChaseDistanceMultiplier = 1.35f;
    private const float DefaultHitscanBeamDurationSeconds = 0.12f;

    /// <summary>
    /// Result of a behavior tick. Tells the caller what movement to perform
    /// and what events occurred.
    /// </summary>
    public struct BehaviorResult
    {
        public MovementResult Movement;
        public int? MoveTargetId; // UnitId to move toward (for TowardTarget/Strafe)
    }

    /// <summary>
    /// Tick cooldowns for a unit (attack cooldown, target lock, forced target, attack animation).
    /// </summary>
    public static void TickCooldowns(UnitData unit, float delta)
    {
        if (unit.AttackCooldown > 0)
            unit.AttackCooldown -= delta;

        if (unit.Engagement.ForcedTargetTimer > 0)
        {
            unit.Engagement.ForcedTargetTimer -= delta;
            if (unit.Engagement.ForcedTargetTimer <= 0)
                unit.Engagement.ForcedTargetUnitId = null;
        }

        if (unit.Action.AttackAnimationTimer > 0)
            unit.Action.AttackAnimationTimer -= delta;
    }

    /// <summary>
    /// Determine behavior for a unit this tick.
    /// Returns what movement the caller should perform and emits combat events.
    /// </summary>
    public static BehaviorResult TickBehavior(
        UnitData unit,
        MatchState state,
        float delta,
        List<SimEvent> events
    )
    {
        // Stunned units can't act
        if (SimEffects.IsStunned(unit))
        {
            unit.BehaviorState = BehaviorState.InRange;
            return new BehaviorResult { Movement = MovementResult.None };
        }

        // Resolve target position — works for both unit and summoner targets
        var targetPos = SimUtils.ResolveTargetPosition(unit.Engagement.TargetUnitId, state);
        if (!targetPos.HasValue)
        {
            unit.BehaviorState = BehaviorState.NoTarget;
            return new BehaviorResult { Movement = MovementResult.Forward };
        }

        bool isSummonerTarget = MatchState.IsSummonerTarget(unit.Engagement.TargetUnitId);
        UnitData? target = isSummonerTarget ? null : state.GetAliveUnit(unit.Engagement.TargetUnitId!.Value);
        SimVector3 tPos = targetPos.Value;
        if (isSummonerTarget)
            tPos = SimTargeting.ResolveSummonerEngagePosition(unit, tPos);
        int targetId = unit.Engagement.TargetUnitId!.Value;

        // If the target unit died between position resolution and this lookup, re-target next tick
        if (!isSummonerTarget && target == null)
        {
            unit.BehaviorState = BehaviorState.NoTarget;
            return new BehaviorResult { Movement = MovementResult.Forward };
        }

        // Use XZ distance for lane-role chase shaping.
        float dx = unit.Position.X - tPos.X;
        float dz = unit.Position.Z - tPos.Z;
        float dist = MathF.Sqrt(dx * dx + dz * dz);

        bool inEngageDistance = SimTargeting.IsWithinEngageDistance(unit, tPos);
        if (inEngageDistance)
        {
            // In engage distance — check engage shape constraint.
            bool canAttack = isSummonerTarget
                ? SimTargeting.CanAttackPosition(unit, tPos)
                : (target != null && SimTargeting.CanAttack(unit, target));

            if (!canAttack)
            {
                // Constraint not satisfied — use fallback movement
                unit.BehaviorState = BehaviorState.InRange;
                return unit.FallbackMovement switch
                {
                    FallbackMovement.Strafe => new BehaviorResult
                    {
                        Movement = MovementResult.Strafe,
                        MoveTargetId = targetId,
                    },
                    FallbackMovement.Idle => new BehaviorResult { Movement = MovementResult.None },
                    _ => new BehaviorResult
                    {
                        Movement = MovementResult.TowardTarget,
                        MoveTargetId = targetId,
                    },
                };
            }

            // In range, constraint OK — attack if cooldown ready
            float effectiveAttackSpeed = SimEffects.GetEffectiveAttackSpeed(unit);
            if (unit.AttackCooldown <= 0 && effectiveAttackSpeed > 0)
            {
                unit.BehaviorState = BehaviorState.Attacking;
                QueuePendingAttack(
                    unit,
                    targetId,
                    MatchState.IsSummonerTarget(targetId),
                    SimEffects.GetEffectiveAttackDamage(unit)
                );

                unit.AttackCooldown = 1.0f / effectiveAttackSpeed;
                unit.Action.AttackAnimationTimer = SimAttackLoop.ResolveAttackAnimationDuration(unit);
                events.Add(new UnitAttackedEvent(unit.UnitId, targetId));

                return new BehaviorResult { Movement = MovementResult.None };
            }

            // In range, waiting for cooldown
            unit.BehaviorState = BehaviorState.InRange;
            return new BehaviorResult { Movement = MovementResult.None };
        }

        // Out of range — role/lane guards may keep unit on lane objective instead of hard-chasing.
        // Summoner targets are exempt so endgame pressure cannot deadlock on side-lane rules.
        if (!isSummonerTarget && ShouldHoldLaneInsteadOfChasing(unit, tPos, dist))
        {
            unit.BehaviorState = BehaviorState.NoTarget;
            return new BehaviorResult { Movement = MovementResult.Forward };
        }

        // Out of range — chase
        unit.BehaviorState = BehaviorState.Chasing;
        return new BehaviorResult
        {
            Movement = MovementResult.TowardTarget,
            MoveTargetId = targetId,
        };
    }

    public static void ResolvePendingAttackCommit(UnitData unit, MatchState state, List<SimEvent> events)
    {
        if (!unit.Action.PendingAttackTargetId.HasValue)
            return;

        int targetId = unit.Action.PendingAttackTargetId.Value;
        float baseDamage = unit.Action.PendingAttackBaseDamage;
        bool targetsSummoner =
            unit.Action.PendingAttackTargetsSummoner || MatchState.IsSummonerTarget(targetId);
        ClearPendingAttack(unit);

        if (targetsSummoner)
        {
            int summonerTeam = MatchState.GetSummonerTeamFromTargetId(targetId);
            var summoner = state.Summoners[summonerTeam];
            if (!summoner.IsAlive)
                return;

            if (unit.UnitType == UnitType.Ranged)
            {
                SpawnProjectileToSummonerOrApplyDirect(unit, targetId, baseDamage, state, events);
            }
            else
            {
                DealSummonerDamage(
                    state,
                    summoner,
                    summonerTeam,
                    baseDamage,
                    unit.Team,
                    unit.UnitId,
                    events
                );
            }

            return;
        }

        var target = state.GetAliveUnit(targetId);
        if (target == null)
            return;

        if (unit.UnitType == UnitType.Ranged)
        {
            SpawnProjectileOrApplyDirect(unit, target, baseDamage, state, events);
            return;
        }

        ApplyMeleeDamageToUnit(unit, target, baseDamage, state, events);
    }

    public static void ClearPendingAttack(UnitData unit)
    {
        unit.Action.PendingAttackTargetId = null;
        unit.Action.PendingAttackBaseDamage = 0f;
        unit.Action.PendingAttackTargetsSummoner = false;
    }

    private static void QueuePendingAttack(
        UnitData unit,
        int targetId,
        bool targetsSummoner,
        float baseDamage
    )
    {
        unit.Action.PendingDamageTimer = 0f;
        unit.Action.PendingDamageTargetId = null;
        unit.Action.PendingDamageAmount = 0f;

        unit.Action.PendingAttackTargetId = targetId;
        unit.Action.PendingAttackTargetsSummoner = targetsSummoner;
        unit.Action.PendingAttackBaseDamage = baseDamage;
    }

    private static bool ShouldHoldLaneInsteadOfChasing(
        UnitData unit,
        SimVector3 targetPos,
        float distance
    )
    {
        int unitLane =
            unit.AssignedLane >= 0 ? unit.AssignedLane : VirtualLanes.GetLaneIndex(unit.Position.Z);
        int targetLane = VirtualLanes.GetLaneIndex(targetPos.Z);
        int laneDistance = VirtualLanes.LaneDistance(unitLane, targetLane);

        if (
            unit.TacticalRole == TacticalRole.Flanker
            && VirtualLanes.IsSideLane(unitLane)
            && targetLane == VirtualLanes.CenterLane
        )
        {
            return true;
        }

        if (
            unit.TacticalRole == TacticalRole.Backliner
            && laneDistance > 0
            && distance > unit.AttackRange * BacklinerMaxCrossLaneChaseDistanceMultiplier
        )
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Apply melee/instant damage to a unit target.
    /// Uses effective attack damage (includes DamageBoost buffs and charge bonus).
    /// Fires OnHit/OnDamaged triggers, and OnKill/OnDeath on kill.
    /// </summary>
    private static void ApplyMeleeDamageToUnit(
        UnitData attacker,
        UnitData target,
        float baseDamage,
        MatchState state,
        List<SimEvent> events
    )
    {
        var recipients = AttackRecipientResolver.ResolveRecipients(attacker, target, state);
        if (recipients.Count == 0)
        {
            attacker.DistanceTraveled = 0f;
            return;
        }

        // Primary recipient preserves legacy trigger semantics.
        ApplyUnitDamage(attacker, recipients[0], baseDamage, state, events);

        bool triggerPerRecipient =
            attacker.Attack.Rules.TriggerMode == AttackTriggerMode.EveryRecipient;
        for (int i = 1; i < recipients.Count; i++)
        {
            if (triggerPerRecipient)
                ApplyUnitDamage(attacker, recipients[i], baseDamage, state, events);
            else
                ApplySecondaryUnitDamage(attacker, recipients[i], baseDamage, state, events);
        }

        // Reset charge distance after attacking
        attacker.DistanceTraveled = 0f;
    }

    private static void ApplySecondaryUnitDamage(
        UnitData attacker,
        UnitData target,
        float baseDamage,
        MatchState state,
        List<SimEvent> events
    )
    {
        var attackerSummoner = state.Summoners[(int)attacker.Team];
        var targetSummoner = state.Summoners[(int)target.Team];
        var (damage, isCrit, wasEvaded) = SimDamage.CalculateAttack(
            baseDamage,
            attacker,
            target,
            attackerSummoner,
            targetSummoner,
            state.Rng,
            events,
            state
        );
        if (wasEvaded)
            return;

        target.CurrentHp -= damage;
        events.Add(new UnitDamagedEvent(target.UnitId, attacker.UnitId, damage, isCrit));
        if (target.CurrentHp > 0f)
            SimAbilityOrchestrator.TryActivateOnDamagedEffects(state, target, attacker, events);

        if (target.CurrentHp <= 0)
        {
            if (SimUtils.KillUnit(state, target, attacker.UnitId, events))
                SimEffects.FireDeathTriggers(state, target, attacker, events);
        }
    }

    /// <summary>
    /// Shared damage pipeline for unit-vs-unit combat.
    /// Calculates damage via SimDamage, applies HP reduction, emits events,
    /// fires triggers (OnHit, OnDamaged, OnKill, OnDeath).
    /// Used by both immediate melee and delayed ranged (pending damage) paths.
    /// </summary>
    private static void ApplyUnitDamage(
        UnitData attacker,
        UnitData target,
        float baseDamage,
        MatchState state,
        List<SimEvent> events
    )
    {
        var attackerSummoner = state.Summoners[(int)attacker.Team];
        var targetSummoner = state.Summoners[(int)target.Team];
        var (damage, isCrit, wasEvaded) = SimDamage.CalculateAttack(
            baseDamage,
            attacker,
            target,
            attackerSummoner,
            targetSummoner,
            state.Rng,
            events,
            state
        );
        if (wasEvaded)
            return;

        target.CurrentHp -= damage;
        events.Add(new UnitDamagedEvent(target.UnitId, attacker.UnitId, damage, isCrit));
        SimAbilityOrchestrator.TryActivateOnHitEffects(state, attacker, target, events);

        // Fire OnHit triggers on attacker
        SimEffects.FireTriggers(state, attacker, TriggerType.OnHit, target, events);

        // Fire OnDamaged triggers on target (if still alive)
        if (target.CurrentHp > 0f)
        {
            SimAbilityOrchestrator.TryActivateOnDamagedEffects(state, target, attacker, events);
            SimEffects.FireTriggers(state, target, TriggerType.OnDamaged, attacker, events);
        }

        if (target.CurrentHp <= 0)
        {
            if (SimUtils.KillUnit(state, target, attacker.UnitId, events))
            {
                // Fire OnKill triggers on attacker, OnDeath + LeaderDeath on target
                SimEffects.FireTriggers(state, attacker, TriggerType.OnKill, target, events);
                SimEffects.FireDeathTriggers(state, target, attacker, events);
            }
        }
    }

    /// <summary>
    /// Process delayed ranged outcomes after attack windup.
    /// Ranged delayed outcomes spawn projectiles for both unit and summoner targets.
    /// Melee-only pending damage against summoners still resolves directly.
    /// Legacy path kept for compatibility with existing tests and edge cases.
    /// </summary>
    public static void TickPendingDamage(
        UnitData unit,
        MatchState state,
        float delta,
        List<SimEvent> events
    )
    {
        if (unit.Action.PendingDamageTimer <= 0)
            return;

        unit.Action.PendingDamageTimer -= delta;
        if (unit.Action.PendingDamageTimer > 0)
            return;

        unit.Action.PendingDamageTimer = 0;

        if (unit.Action.PendingDamageTargetId.HasValue)
        {
            int pendingTargetId = unit.Action.PendingDamageTargetId.Value;

            if (MatchState.IsSummonerTarget(pendingTargetId))
            {
                if (unit.UnitType == UnitType.Ranged)
                {
                    SpawnProjectileToSummonerOrApplyDirect(
                        unit,
                        pendingTargetId,
                        unit.Action.PendingDamageAmount,
                        state,
                        events
                    );
                }
                else
                {
                    // Pending damage against a summoner
                    int summonerTeam = MatchState.GetSummonerTeamFromTargetId(pendingTargetId);
                    var summoner = state.Summoners[summonerTeam];
                    if (summoner.IsAlive)
                    {
                        DealSummonerDamage(
                            state,
                            summoner,
                            summonerTeam,
                            unit.Action.PendingDamageAmount,
                            unit.Team,
                            unit.UnitId,
                            events
                        );
                    }
                }
            }
            else
            {
                // Delayed ranged attack on unit: spawn projectile (or direct fallback).
                var target = state.GetAliveUnit(pendingTargetId);
                if (target != null)
                {
                    SpawnProjectileOrApplyDirect(
                        unit,
                        target,
                        unit.Action.PendingDamageAmount,
                        state,
                        events
                    );
                }
            }

            unit.Action.PendingDamageTargetId = null;
            unit.Action.PendingDamageAmount = 0;
        }
    }

    /// <summary>
    /// Apply damage to a summoner with modifiers and emit HP changed + damaged events.
    /// Used by windup-commit melee hits and legacy delayed paths.
    /// </summary>
    private static void DealSummonerDamage(
        MatchState state,
        SummonerData summoner,
        int summonerTeam,
        float baseDamage,
        Team attackerTeam,
        int attackerUnitId,
        List<SimEvent> events
    )
    {
        float damage = baseDamage;
        var attackerSummoner = state.Summoners[(int)attackerTeam];
        float soulStrength = 0f;
        if (state.Units.TryGetValue(attackerUnitId, out var attackerUnit))
            soulStrength = attackerUnit.SoulStrength;

        damage = ApplySummonerDamageModifiers(damage, attackerSummoner, summoner, soulStrength);

        summoner.CurrentHp -= damage;
        bool wasDestroyed = false;
        if (summoner.CurrentHp <= 0)
        {
            summoner.CurrentHp = 0;
            summoner.IsAlive = false;
            wasDestroyed = true;
        }
        events.Add(new SummonerHpChangedEvent(summonerTeam, summoner.CurrentHp, summoner.MaxHp));
        events.Add(new SummonerDamagedEvent(summonerTeam, damage, attackerUnitId));
        if (wasDestroyed)
            events.Add(new SummonerDestroyedEvent(summonerTeam, attackerUnitId));
    }

    private static void SpawnProjectileOrApplyDirect(
        UnitData attacker,
        UnitData target,
        float baseDamage,
        MatchState state,
        List<SimEvent> events
    )
    {
        if (!TryResolveProjectileData(attacker, out var projectileData))
        {
            Simulation.Log?.Invoke(
                $"[SimBehavior] Missing projectile data for ranged attacker unitId={attacker.UnitId} catalogId={attacker.CatalogId.Value}; skipping attack resolution."
            );
            return;
        }

        var startPos = ResolveProjectileStartPosition(attacker);
        if (projectileData.SpawnAtTargetHeight)
            startPos = new SimVector3(startPos.X, target.Position.Y, startPos.Z);
        var targetPos = ResolveProjectileTargetPosition(startPos, target.Position, projectileData);

        if (projectileData.InstantHitScan)
        {
            SimProjectile.ResolveInstantLine(
                state,
                sourceUnitId: attacker.UnitId,
                targetUnitId: target.UnitId,
                team: attacker.Team,
                damage: baseDamage,
                sourceElementId: attacker.ElementId,
                startPos: startPos,
                endPos: targetPos,
                hitRadius: projectileData.HitRadius,
                pierceCount: projectileData.PierceCount,
                aoeRadius: projectileData.AoeRadius,
                hitSpace: projectileData.HitSpace,
                projectileCatalogId: new SimProjectileCatalogId(projectileData.ProjectileId),
                targetAffinity: attacker.ProjectileTargetAffinity,
                impactKind: attacker.ProjectileImpactKind,
                statusKind: attacker.ProjectileStatusKind,
                statusDuration: attacker.ProjectileStatusDuration,
                statusTickInterval: attacker.ProjectileStatusTickInterval,
                statusPotencyPerStack: attacker.ProjectileStatusPotencyPerStack,
                statusMaxStacks: attacker.ProjectileStatusMaxStacks,
                beamDurationSeconds: ResolveHitscanBeamDurationSeconds(projectileData),
                events: events
            );
            return;
        }

        SimProjectile.Spawn(
            state,
            sourceUnitId: attacker.UnitId,
            targetUnitId: target.UnitId,
            team: attacker.Team,
            damage: baseDamage,
            sourceElementId: attacker.ElementId,
            movementType: projectileData.MovementType,
            speed: projectileData.Speed,
            lifetime: projectileData.Lifetime,
            startPos: startPos,
            targetPos: targetPos,
            arcHeight: projectileData.ArcHeight,
            pierceCount: projectileData.PierceCount,
            aoeRadius: projectileData.AoeRadius,
            hitRadius: projectileData.HitRadius,
            hitSpace: projectileData.HitSpace,
            steerStrength: projectileData.SteerStrength,
            veerDelay: projectileData.VeerDelay,
            veerAngle: projectileData.VeerAngle,
            veerDuration: projectileData.VeerDuration,
            projectileCatalogId: new SimProjectileCatalogId(projectileData.ProjectileId),
            acceleration: projectileData.Acceleration,
            minSpeed: projectileData.MinSpeed,
            speedStart: projectileData.SpeedStart,
            speedEnd: projectileData.SpeedEnd,
            speedTransitionDuration: projectileData.SpeedTransitionDuration,
            speedEasing: projectileData.SpeedEasing,
            speedEaseExponent: projectileData.SpeedEaseExponent,
            tracking: projectileData.Tracking,
            targetAffinity: attacker.ProjectileTargetAffinity,
            impactKind: attacker.ProjectileImpactKind,
            statusKind: attacker.ProjectileStatusKind,
            statusDuration: attacker.ProjectileStatusDuration,
            statusTickInterval: attacker.ProjectileStatusTickInterval,
            statusPotencyPerStack: attacker.ProjectileStatusPotencyPerStack,
            statusMaxStacks: attacker.ProjectileStatusMaxStacks
        );
    }

    private static void SpawnProjectileToSummonerOrApplyDirect(
        UnitData attacker,
        int summonerTargetId,
        float baseDamage,
        MatchState state,
        List<SimEvent> events
    )
    {
        int summonerTeam = MatchState.GetSummonerTeamFromTargetId(summonerTargetId);
        var summoner = state.Summoners[summonerTeam];
        if (!summoner.IsAlive)
            return;

        if (!TryResolveProjectileData(attacker, out var projectileData))
        {
            Simulation.Log?.Invoke(
                $"[SimBehavior] Missing projectile data for ranged summoner attack unitId={attacker.UnitId} catalogId={attacker.CatalogId.Value}; skipping attack resolution."
            );
            return;
        }

        var startPos = ResolveProjectileStartPosition(attacker);
        var summonerTargetPos = summoner.TargetPointPosition;
        if (projectileData.SpawnAtTargetHeight)
            startPos = new SimVector3(startPos.X, summonerTargetPos.Y, startPos.Z);
        var targetPos = ResolveProjectileTargetPosition(startPos, summonerTargetPos, projectileData);

        if (projectileData.InstantHitScan)
        {
            SimProjectile.ResolveInstantLine(
                state,
                sourceUnitId: attacker.UnitId,
                targetUnitId: summonerTargetId,
                team: attacker.Team,
                damage: baseDamage,
                sourceElementId: attacker.ElementId,
                startPos: startPos,
                endPos: targetPos,
                hitRadius: projectileData.HitRadius,
                pierceCount: projectileData.PierceCount,
                aoeRadius: projectileData.AoeRadius,
                hitSpace: projectileData.HitSpace,
                projectileCatalogId: new SimProjectileCatalogId(projectileData.ProjectileId),
                targetAffinity: attacker.ProjectileTargetAffinity,
                impactKind: attacker.ProjectileImpactKind,
                statusKind: attacker.ProjectileStatusKind,
                statusDuration: attacker.ProjectileStatusDuration,
                statusTickInterval: attacker.ProjectileStatusTickInterval,
                statusPotencyPerStack: attacker.ProjectileStatusPotencyPerStack,
                statusMaxStacks: attacker.ProjectileStatusMaxStacks,
                beamDurationSeconds: ResolveHitscanBeamDurationSeconds(projectileData),
                events: events
            );
            return;
        }

        SimProjectile.Spawn(
            state,
            sourceUnitId: attacker.UnitId,
            targetUnitId: summonerTargetId,
            team: attacker.Team,
            damage: baseDamage,
            sourceElementId: attacker.ElementId,
            movementType: projectileData.MovementType,
            speed: projectileData.Speed,
            lifetime: projectileData.Lifetime,
            startPos: startPos,
            targetPos: targetPos,
            arcHeight: projectileData.ArcHeight,
            pierceCount: projectileData.PierceCount,
            aoeRadius: projectileData.AoeRadius,
            hitRadius: projectileData.HitRadius,
            hitSpace: projectileData.HitSpace,
            steerStrength: projectileData.SteerStrength,
            veerDelay: projectileData.VeerDelay,
            veerAngle: projectileData.VeerAngle,
            veerDuration: projectileData.VeerDuration,
            projectileCatalogId: new SimProjectileCatalogId(projectileData.ProjectileId),
            acceleration: projectileData.Acceleration,
            minSpeed: projectileData.MinSpeed,
            speedStart: projectileData.SpeedStart,
            speedEnd: projectileData.SpeedEnd,
            speedTransitionDuration: projectileData.SpeedTransitionDuration,
            speedEasing: projectileData.SpeedEasing,
            speedEaseExponent: projectileData.SpeedEaseExponent,
            tracking: projectileData.Tracking,
            targetAffinity: attacker.ProjectileTargetAffinity,
            impactKind: attacker.ProjectileImpactKind,
            statusKind: attacker.ProjectileStatusKind,
            statusDuration: attacker.ProjectileStatusDuration,
            statusTickInterval: attacker.ProjectileStatusTickInterval,
            statusPotencyPerStack: attacker.ProjectileStatusPotencyPerStack,
            statusMaxStacks: attacker.ProjectileStatusMaxStacks
        );
    }

    private static SimVector3 ResolveProjectileStartPosition(UnitData attacker)
    {
        var startPos = attacker.Position;
        if (!attacker.CatalogId.HasValue)
            return startPos;

        var unitDef = UnitDefinitions.Get(attacker.CatalogId.Value);
        if (unitDef == null)
            return startPos;

        // Use per-unit target-point offset as a projectile muzzle offset.
        // Mirror X for left-facing units so both teams spawn from their "front" side.
        var offset = unitDef.Visual.TargetPointOffset;
        float mirroredOffsetX = attacker.IsFacingRight ? offset.X : -offset.X;
        return new SimVector3(
            startPos.X + mirroredOffsetX,
            startPos.Y + offset.Y,
            startPos.Z + offset.Z
        );
    }

    private static SimVector3 ResolveProjectileTargetPosition(
        SimVector3 startPos,
        SimVector3 intendedTargetPos,
        ProjectileData projectileData
    )
    {
        if (
            projectileData.MovementType != ProjectileMovementType.Straight
            || projectileData.Tracking
            || projectileData.FixedTravelDistance <= 0f
        )
        {
            return intendedTargetPos;
        }

        var toTarget = intendedTargetPos - startPos;
        if (toTarget.LengthSquared() <= 0.0001f)
            return intendedTargetPos;

        return startPos + (toTarget.Normalized() * projectileData.FixedTravelDistance);
    }

    private static float ResolveHitscanBeamDurationSeconds(ProjectileData projectileData)
    {
        if (projectileData.FadeDuration > 0f)
            return projectileData.FadeDuration;
        return DefaultHitscanBeamDurationSeconds;
    }

    private static bool TryResolveProjectileData(
        UnitData attacker,
        out ProjectileData projectileData
    )
    {
        projectileData = null!;

        if (attacker.ProjectileCatalogId.HasValue)
        {
            var fromUnit = ProjectileDefinitions.Get(attacker.ProjectileCatalogId.Value);
            if (fromUnit != null)
            {
                projectileData = fromUnit;
                return true;
            }
        }

        if (!attacker.CatalogId.HasValue)
            return false;

        var unitDef = UnitDefinitions.Get(attacker.CatalogId.Value);
        if (unitDef?.Ranged == null)
            return false;

        var resolved = ProjectileDefinitions.Get(unitDef.Ranged.ProjectileId);
        if (resolved == null)
            return false;

        projectileData = resolved;
        return true;
    }

    private static bool IsValidTarget(int? targetId, MatchState state)
    {
        if (!targetId.HasValue)
            return false;

        if (MatchState.IsSummonerTarget(targetId))
        {
            int team = MatchState.GetSummonerTeamFromTargetId(targetId.Value);
            return team >= 0 && team <= 1 && state.Summoners[team].IsAlive;
        }

        return state.GetAliveUnit(targetId.Value) != null;
    }

    /// <summary>
    /// Apply summoner-level damage modifiers:
    /// damage bonus from attacker summoner, soul strength from attacker unit, and
    /// reduction from target summoner soul strength.
    /// Rounds to one decimal place for deterministic results.
    /// </summary>
    private static float ApplySummonerDamageModifiers(
        float damage,
        SummonerData attacker,
        SummonerData target,
        float soulStrength = 0f
    )
    {
        if (attacker.DamageBonus > 0f)
            damage *= 1f + attacker.DamageBonus / 100f;
        if (soulStrength > 0f)
            damage += soulStrength;
        if (target.SoulStrength > 0f)
            damage = System.MathF.Max(damage - target.SoulStrength, 0f);
        return SimUtils.RoundToOneDecimal(damage);
    }
}
