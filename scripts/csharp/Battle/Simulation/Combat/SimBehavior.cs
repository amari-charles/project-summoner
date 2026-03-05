using System;
using System.Collections.Generic;
using Fateforged.Data.Projectiles;
using Fateforged.Projectiles;
using Fateforged.Units;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Combat.Targeting;
using Fateforged.Simulation.Subsystems;

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
///   Attacking → apply damage, reset cooldown
/// </summary>
public static class SimBehavior
{
    private const float AttackAnimationDuration = 0.5f;
    private const float TargetLockDuration = 0.5f;

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

        if (unit.TargetLockTimer > 0)
            unit.TargetLockTimer -= delta;

        if (unit.ForcedTargetTimer > 0)
        {
            unit.ForcedTargetTimer -= delta;
            if (unit.ForcedTargetTimer <= 0)
                unit.ForcedTargetUnitId = null;
        }

        if (unit.AttackAnimationTimer > 0)
            unit.AttackAnimationTimer -= delta;
    }

    /// <summary>
    /// Update targeting for a unit: forced target, target lock, re-acquisition.
    /// </summary>
    public static void TickTargeting(UnitData unit, MatchState state)
    {
        var policy = TargetPolicyRegistry.Resolve(unit.TargetPolicyId);

        // Use forced target if available
        if (unit.ForcedTargetUnitId.HasValue)
        {
            if (IsValidTarget(unit.ForcedTargetUnitId, state))
            {
                unit.TargetUnitId = unit.ForcedTargetUnitId.Value;
                return;
            }
            unit.ForcedTargetUnitId = null;
        }

        bool currentTargetIsValid = IsValidTarget(unit.TargetUnitId, state);

        // Keep current target if policy allows and it's still attackable now.
        // This avoids unnecessary target churn when lock expires.
        if (unit.TargetLockTimer <= 0 &&
            currentTargetIsValid &&
            policy.ShouldKeepCurrentTarget(unit, state, unit.TargetUnitId))
        {
            unit.TargetLockTimer = TargetLockDuration;
            return;
        }

        // Re-acquire target if lock expired or current target invalid
        if (unit.TargetLockTimer <= 0 || !currentTargetIsValid)
        {
            unit.TargetUnitId = policy.SelectTarget(unit, state);
            if (unit.TargetUnitId.HasValue)
                unit.TargetLockTimer = TargetLockDuration;
        }
    }

    /// <summary>
    /// Determine behavior for a unit this tick.
    /// Returns what movement the caller should perform and emits combat events.
    /// </summary>
    public static BehaviorResult TickBehavior(
        UnitData unit, MatchState state, float delta, List<SimEvent> events)
    {
        // Stunned units can't act
        if (SimEffects.IsStunned(unit))
        {
            unit.BehaviorState = BehaviorState.InRange;
            return new BehaviorResult { Movement = MovementResult.None };
        }

        // Resolve target position — works for both unit and summoner targets
        var targetPos = SimUtils.ResolveTargetPosition(unit.TargetUnitId, state);
        if (!targetPos.HasValue)
        {
            unit.BehaviorState = BehaviorState.NoTarget;
            return new BehaviorResult { Movement = MovementResult.Forward };
        }

        bool isSummonerTarget = MatchState.IsSummonerTarget(unit.TargetUnitId);
        UnitData? target = isSummonerTarget ? null : state.GetAliveUnit(unit.TargetUnitId!.Value);
        SimVector3 tPos = targetPos.Value;
        int targetId = unit.TargetUnitId!.Value;

        // If the target unit died between position resolution and this lookup, re-target next tick
        if (!isSummonerTarget && target == null)
        {
            unit.BehaviorState = BehaviorState.NoTarget;
            return new BehaviorResult { Movement = MovementResult.Forward };
        }

        // Use XZ distance for range check (consistent with movement which ignores Y)
        float dx = unit.Position.X - tPos.X;
        float dz = unit.Position.Z - tPos.Z;
        float dist = MathF.Sqrt(dx * dx + dz * dz);

        if (dist <= unit.AttackRange)
        {
            // In range — check cone constraint
            bool canAttack = isSummonerTarget
                ? SimTargeting.CanAttackPosition(unit, tPos)
                : (target != null && SimTargeting.CanAttack(unit, target));

            if (unit.HasConeConstraint && !canAttack)
            {
                // Constraint not satisfied — use fallback movement
                unit.BehaviorState = BehaviorState.InRange;
                return unit.FallbackMovement switch
                {
                    FallbackMovement.Strafe => new BehaviorResult
                    {
                        Movement = MovementResult.Strafe,
                        MoveTargetId = targetId
                    },
                    FallbackMovement.Idle => new BehaviorResult { Movement = MovementResult.None },
                    _ => new BehaviorResult
                    {
                        Movement = MovementResult.TowardTarget,
                        MoveTargetId = targetId
                    }
                };
            }

            // In range, constraint OK — attack if cooldown ready
            if (unit.AttackCooldown <= 0 && unit.AttackSpeed > 0)
            {
                unit.BehaviorState = BehaviorState.Attacking;

                if (isSummonerTarget)
                {
                    // Attacking a summoner
                    ApplyDamageToSummoner(unit, targetId, state, events);
                }
                else if (unit.UnitType == UnitType.Melee)
                {
                    // Melee: immediate damage via SimDamage
                    ApplyMeleeDamageToUnit(unit, target!, state, events);
                }
                else if (unit.UnitType == UnitType.Ranged)
                {
                    float baseDamage = SimEffects.GetEffectiveAttackDamage(unit);

                    // Ranged: optional windup, then spawn an authoritative projectile.
                    if (unit.ProjectileDelay > 0)
                    {
                        unit.PendingDamageTimer = unit.ProjectileDelay;
                        unit.PendingDamageTargetId = targetId;
                        unit.PendingDamageAmount = baseDamage;
                    }
                    else
                    {
                        SpawnProjectileOrApplyDirect(unit, target!, baseDamage, state, events);
                    }
                }

                unit.AttackCooldown = 1.0f / unit.AttackSpeed;
                unit.AttackAnimationTimer = AttackAnimationDuration;
                events.Add(new UnitAttackedEvent(unit.UnitId, targetId));

                return new BehaviorResult { Movement = MovementResult.None };
            }

            // In range, waiting for cooldown
            unit.BehaviorState = BehaviorState.InRange;
            return new BehaviorResult { Movement = MovementResult.None };
        }

        // Out of range — chase
        unit.BehaviorState = BehaviorState.Chasing;
        return new BehaviorResult
        {
            Movement = MovementResult.TowardTarget,
            MoveTargetId = targetId
        };
    }

    /// <summary>
    /// Apply melee/instant damage to a unit target.
    /// Uses effective attack damage (includes DamageBoost buffs and charge bonus).
    /// Fires OnHit/OnDamaged triggers, and OnKill/OnDeath on kill.
    /// </summary>
    private static void ApplyMeleeDamageToUnit(
        UnitData attacker, UnitData target, MatchState state, List<SimEvent> events)
    {
        float baseDamage = SimEffects.GetEffectiveAttackDamage(attacker);
        ApplyUnitDamage(attacker, target, baseDamage, state, events);

        // Reset charge distance after attacking
        attacker.DistanceTraveled = 0f;
    }

    /// <summary>
    /// Shared damage pipeline for unit-vs-unit combat.
    /// Calculates damage via SimDamage, applies HP reduction, emits events,
    /// fires triggers (OnHit, OnDamaged, OnKill, OnDeath).
    /// Used by both immediate melee and delayed ranged (pending damage) paths.
    /// </summary>
    private static void ApplyUnitDamage(
        UnitData attacker, UnitData target, float baseDamage, MatchState state, List<SimEvent> events)
    {
        var attackerSummoner = state.Summoners[(int)attacker.Team];
        var targetSummoner = state.Summoners[(int)target.Team];
        var (damage, isCrit) = SimDamage.Calculate(
            baseDamage, attacker, target, attackerSummoner, targetSummoner, state.Rng);

        target.CurrentHp -= damage;
        events.Add(new UnitDamagedEvent(target.UnitId, attacker.UnitId, damage, isCrit));

        // Fire OnHit triggers on attacker
        SimEffects.FireTriggers(state, attacker, TriggerType.OnHit, target, events);

        // Fire OnDamaged triggers on target (if still alive)
        if (target.IsAlive)
            SimEffects.FireTriggers(state, target, TriggerType.OnDamaged, attacker, events);

        if (target.CurrentHp <= 0)
        {
            SimUtils.KillUnit(state, target, attacker.UnitId, events);

            // Fire OnKill triggers on attacker, OnDeath + LeaderDeath on target
            SimEffects.FireTriggers(state, attacker, TriggerType.OnKill, target, events);
            SimEffects.FireDeathTriggers(state, target, attacker, events);
        }
    }

    /// <summary>
    /// Apply damage to a summoner target.
    /// Summoner damage intentionally bypasses SimDamage.Calculate() — summoners are
    /// not units and don't have evasion, crit interaction, elemental matchups, defense,
    /// or shields. Only summoner-level modifiers (DamageBonus, DamageReduction) apply.
    /// For ranged with projectile delay, the caller sets PendingDamageTargetId instead.
    /// </summary>
    private static void ApplyDamageToSummoner(
        UnitData attacker, int summonerTargetId, MatchState state, List<SimEvent> events)
    {
        int summonerTeam = MatchState.GetSummonerTeamFromTargetId(summonerTargetId);
        var summoner = state.Summoners[summonerTeam];
        if (!summoner.IsAlive) return;

        // For ranged with projectile delay, queue pending damage instead
        if (attacker.UnitType == UnitType.Ranged && attacker.ProjectileDelay > 0)
        {
            attacker.PendingDamageTimer = attacker.ProjectileDelay;
            attacker.PendingDamageTargetId = summonerTargetId;
            attacker.PendingDamageAmount = SimEffects.GetEffectiveAttackDamage(attacker);
            return;
        }

        // Immediate damage (melee or zero-delay ranged)
        DealSummonerDamage(
            state,
            summoner,
            summonerTeam,
            SimEffects.GetEffectiveAttackDamage(attacker),
            attacker.Team,
            attacker.UnitId,
            events);
    }

    /// <summary>
    /// Process delayed ranged outcomes after attack windup.
    /// Unit targets spawn projectiles; summoner targets apply delayed direct damage.
    /// Called after all units have moved for the tick.
    /// </summary>
    public static void TickPendingDamage(UnitData unit, MatchState state, float delta, List<SimEvent> events)
    {
        if (unit.PendingDamageTimer <= 0) return;

        unit.PendingDamageTimer -= delta;
        if (unit.PendingDamageTimer > 0) return;

        unit.PendingDamageTimer = 0;

        if (unit.PendingDamageTargetId.HasValue)
        {
            int pendingTargetId = unit.PendingDamageTargetId.Value;

            if (MatchState.IsSummonerTarget(pendingTargetId))
            {
                // Pending damage against a summoner
                int summonerTeam = MatchState.GetSummonerTeamFromTargetId(pendingTargetId);
                var summoner = state.Summoners[summonerTeam];
                if (summoner.IsAlive)
                {
                    DealSummonerDamage(state, summoner, summonerTeam, unit.PendingDamageAmount, unit.Team, unit.UnitId, events);
                }
            }
            else
            {
                // Delayed ranged attack on unit: spawn projectile (or direct fallback).
                var target = state.GetAliveUnit(pendingTargetId);
                if (target != null)
                {
                    SpawnProjectileOrApplyDirect(unit, target, unit.PendingDamageAmount, state, events);
                }
            }

            unit.PendingDamageTargetId = null;
            unit.PendingDamageAmount = 0;
        }
    }

    /// <summary>
    /// Apply damage to a summoner with modifiers and emit HP changed + damaged events.
    /// Shared by ApplyDamageToSummoner (immediate) and TickPendingDamage (delayed).
    /// </summary>
    private static void DealSummonerDamage(
        MatchState state, SummonerData summoner, int summonerTeam,
        float baseDamage, Team attackerTeam, int attackerUnitId, List<SimEvent> events)
    {
        float damage = baseDamage;
        var attackerSummoner = state.Summoners[(int)attackerTeam];
        damage = ApplySummonerDamageModifiers(damage, attackerSummoner, summoner);

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
        UnitData attacker, UnitData target, float baseDamage, MatchState state, List<SimEvent> events)
    {
        if (!TryResolveProjectileData(attacker, out var projectileData))
        {
            // Fallback for missing ranged definitions in tests or incomplete data.
            ApplyUnitDamage(attacker, target, baseDamage, state, events);
            return;
        }

        var startPos = attacker.Position;
        var targetPos = target.Position;
        if (projectileData.SpawnAtTargetHeight)
            startPos = new SimVector3(startPos.X, targetPos.Y, startPos.Z);

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
            projectileCatalogId: (string)projectileData.ProjectileId,
            acceleration: projectileData.Acceleration,
            minSpeed: projectileData.MinSpeed,
            speedStart: projectileData.SpeedStart,
            speedEnd: projectileData.SpeedEnd,
            speedTransitionDuration: projectileData.SpeedTransitionDuration,
            speedEasing: projectileData.SpeedEasing,
            speedEaseExponent: projectileData.SpeedEaseExponent
        );
    }

    private static bool TryResolveProjectileData(UnitData attacker, out ProjectileData projectileData)
    {
        projectileData = null!;

        if (string.IsNullOrEmpty(attacker.CatalogId))
            return false;

        var unitDef = UnitDefinitions.Get(attacker.CatalogId);
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
        if (!targetId.HasValue) return false;

        if (MatchState.IsSummonerTarget(targetId))
        {
            int team = MatchState.GetSummonerTeamFromTargetId(targetId.Value);
            return team >= 0 && team <= 1 && state.Summoners[team].IsAlive;
        }

        return state.GetAliveUnit(targetId.Value) != null;
    }

    /// <summary>
    /// Apply summoner-level damage modifiers (damage bonus from attacker, damage reduction from target).
    /// Rounds to one decimal place for deterministic results.
    /// </summary>
    private static float ApplySummonerDamageModifiers(float damage, SummonerData attacker, SummonerData target)
    {
        if (attacker.DamageBonus > 0f)
            damage *= 1f + attacker.DamageBonus / 100f;
        if (target.DamageReduction > 0f)
            damage = System.MathF.Max(damage - target.DamageReduction, 0f);
        return SimUtils.RoundToOneDecimal(damage);
    }
}
