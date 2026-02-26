using System;
using System.Collections.Generic;

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
    // Behavior state constants (matches UnitData.BehaviorState)
    public const int NoTarget = 0;
    public const int Chasing = 1;
    public const int InRange = 2;
    public const int Attacking = 3;

    // Unit type constants
    private const int UnitTypeMelee = 0;
    private const int UnitTypeRanged = 1;

    // Fallback movement constants (matches UnitData.FallbackMovement)
    private const int FallbackMoveToward = 0;
    private const int FallbackStrafe = 1;
    private const int FallbackIdle = 2;

    private const float TargetLockDuration = 0.5f;

    /// <summary>
    /// Result of a behavior tick. Tells the caller what movement to perform
    /// and what events occurred.
    /// </summary>
    public struct BehaviorResult
    {
        public int Movement; // 0=None, 1=Forward, 2=TowardTarget, 3=Strafe
        public int? MoveTargetId; // UnitId to move toward (for TowardTarget/Strafe)
    }

    // Movement result constants
    public const int MoveNone = 0;
    public const int MoveForward = 1;
    public const int MoveTowardTarget = 2;
    public const int MoveStrafe = 3;

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

        // Re-acquire target if lock expired or current target invalid
        if (unit.TargetLockTimer <= 0 || !IsValidTarget(unit.TargetUnitId, state))
        {
            unit.TargetUnitId = SimTargeting.AcquireTarget(unit, state);
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
        // Resolve target position — works for both unit and summoner targets
        var targetPos = ResolveTargetPosition(unit.TargetUnitId, state);
        if (!targetPos.HasValue)
        {
            unit.BehaviorState = NoTarget;
            return new BehaviorResult { Movement = MoveForward };
        }

        bool isSummonerTarget = MatchState.IsSummonerTarget(unit.TargetUnitId);
        UnitData? target = isSummonerTarget ? null : state.GetAliveUnit(unit.TargetUnitId!.Value);
        SimVector3 tPos = targetPos.Value;
        int targetId = unit.TargetUnitId!.Value;

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
                unit.BehaviorState = InRange;
                return unit.FallbackMovement switch
                {
                    FallbackStrafe => new BehaviorResult
                    {
                        Movement = MoveStrafe,
                        MoveTargetId = targetId
                    },
                    FallbackIdle => new BehaviorResult { Movement = MoveNone },
                    _ => new BehaviorResult
                    {
                        Movement = MoveTowardTarget,
                        MoveTargetId = targetId
                    }
                };
            }

            // In range, constraint OK — attack if cooldown ready
            if (unit.AttackCooldown <= 0 && unit.AttackSpeed > 0)
            {
                unit.BehaviorState = Attacking;

                if (isSummonerTarget)
                {
                    // Attacking a summoner
                    ApplyDamageToSummoner(unit, targetId, state, delta, events);
                }
                else if (unit.UnitType == UnitTypeMelee)
                {
                    // Melee: immediate damage via SimDamage
                    ApplyMeleeDamageToUnit(unit, target!, state, events);
                }
                else if (unit.UnitType == UnitTypeRanged)
                {
                    // Ranged: delayed damage (pending damage timer simulates projectile travel)
                    if (unit.ProjectileDelay > 0)
                    {
                        unit.PendingDamageTimer = unit.ProjectileDelay;
                        unit.PendingDamageTargetId = targetId;
                        unit.PendingDamageAmount = unit.AttackDamage;
                    }
                    else
                    {
                        // Zero delay — instant damage
                        ApplyMeleeDamageToUnit(unit, target!, state, events);
                    }
                }

                unit.AttackCooldown = 1.0f / unit.AttackSpeed;
                unit.AttackAnimationTimer = 0.5f; // Default attack animation duration
                events.Add(new UnitAttackedEvent(unit.UnitId, targetId));

                return new BehaviorResult { Movement = MoveNone };
            }

            // In range, waiting for cooldown
            unit.BehaviorState = InRange;
            return new BehaviorResult { Movement = MoveNone };
        }

        // Out of range — chase
        unit.BehaviorState = Chasing;
        return new BehaviorResult
        {
            Movement = MoveTowardTarget,
            MoveTargetId = targetId
        };
    }

    /// <summary>
    /// Apply melee/instant damage to a unit target.
    /// </summary>
    private static void ApplyMeleeDamageToUnit(
        UnitData attacker, UnitData target, MatchState state, List<SimEvent> events)
    {
        var attackerSummoner = state.Summoners[attacker.Team];
        var targetSummoner = state.Summoners[target.Team];
        var (damage, isCrit) = SimDamage.Calculate(
            attacker.AttackDamage, attacker, target, attackerSummoner, targetSummoner, state.Rng);

        target.CurrentHp -= damage;
        events.Add(new UnitDamagedEvent(target.UnitId, attacker.UnitId, damage, isCrit));

        if (target.CurrentHp <= 0)
        {
            target.CurrentHp = 0;
            target.IsAlive = false;
            state.KillCount++;
            events.Add(new UnitDiedSimEvent(target.UnitId, attacker.UnitId));
        }
    }

    /// <summary>
    /// Apply damage to a summoner target. Handles both melee (immediate) and
    /// ranged (zero-delay) attacks. For ranged with projectile delay, the caller
    /// sets PendingDamageTargetId instead and TickPendingDamage handles it.
    /// </summary>
    private static void ApplyDamageToSummoner(
        UnitData attacker, int summonerTargetId, MatchState state, float delta, List<SimEvent> events)
    {
        int summonerTeam = MatchState.GetSummonerTeamFromTargetId(summonerTargetId);
        var summoner = state.Summoners[summonerTeam];
        if (!summoner.IsAlive) return;

        // For ranged with projectile delay, queue pending damage instead
        if (attacker.UnitType == UnitTypeRanged && attacker.ProjectileDelay > 0)
        {
            attacker.PendingDamageTimer = attacker.ProjectileDelay;
            attacker.PendingDamageTargetId = summonerTargetId;
            attacker.PendingDamageAmount = attacker.AttackDamage;
            return;
        }

        // Immediate damage (melee or zero-delay ranged)
        float damage = attacker.AttackDamage;
        var attackerSummoner = state.Summoners[attacker.Team];

        // Apply summoner damage bonus
        if (attackerSummoner.DamageBonus > 0f)
            damage *= 1f + attackerSummoner.DamageBonus / 100f;

        // Apply summoner damage reduction
        if (summoner.DamageReduction > 0f)
            damage = System.MathF.Max(damage - summoner.DamageReduction, 0f);

        damage = System.MathF.Round(damage * 10f) / 10f;

        summoner.CurrentHp -= damage;
        events.Add(new SummonerHpChangedEvent(summonerTeam, summoner.CurrentHp, summoner.MaxHp));

        if (summoner.CurrentHp <= 0)
        {
            summoner.CurrentHp = 0;
            summoner.IsAlive = false;
            int winnerTeam = attacker.Team;
            events.Add(new GameOverEvent(winnerTeam, "Summoner destroyed"));
        }
    }

    /// <summary>
    /// Process pending ranged damage (projectile travel simulation).
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
                    float damage = unit.PendingDamageAmount;
                    var attackerSummoner = state.Summoners[unit.Team];

                    if (attackerSummoner.DamageBonus > 0f)
                        damage *= 1f + attackerSummoner.DamageBonus / 100f;
                    if (summoner.DamageReduction > 0f)
                        damage = System.MathF.Max(damage - summoner.DamageReduction, 0f);
                    damage = System.MathF.Round(damage * 10f) / 10f;

                    summoner.CurrentHp -= damage;
                    events.Add(new SummonerHpChangedEvent(summonerTeam, summoner.CurrentHp, summoner.MaxHp));

                    if (summoner.CurrentHp <= 0)
                    {
                        summoner.CurrentHp = 0;
                        summoner.IsAlive = false;
                        events.Add(new GameOverEvent(unit.Team, "Summoner destroyed"));
                    }
                }
            }
            else
            {
                // Pending damage against a unit
                var target = state.GetAliveUnit(pendingTargetId);
                if (target != null)
                {
                    var attackerSummoner = state.Summoners[unit.Team];
                    var targetSummoner = state.Summoners[target.Team];
                    var (damage, isCrit) = SimDamage.Calculate(
                        unit.PendingDamageAmount, unit, target, attackerSummoner, targetSummoner, state.Rng);

                    target.CurrentHp -= damage;
                    events.Add(new UnitDamagedEvent(target.UnitId, unit.UnitId, damage, isCrit));

                    if (target.CurrentHp <= 0)
                    {
                        target.CurrentHp = 0;
                        target.IsAlive = false;
                        state.KillCount++;
                        events.Add(new UnitDiedSimEvent(target.UnitId, unit.UnitId));
                    }
                }
            }

            unit.PendingDamageTargetId = null;
            unit.PendingDamageAmount = 0;
        }
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
    /// Resolve target position for either a unit or summoner target ID.
    /// Returns null if target is invalid/dead.
    /// </summary>
    private static SimVector3? ResolveTargetPosition(int? targetId, MatchState state)
    {
        if (!targetId.HasValue) return null;

        if (MatchState.IsSummonerTarget(targetId))
        {
            int team = MatchState.GetSummonerTeamFromTargetId(targetId.Value);
            if (team >= 0 && team <= 1 && state.Summoners[team].IsAlive)
                return state.Summoners[team].Position;
            return null;
        }

        var unit = state.GetAliveUnit(targetId.Value);
        return unit?.Position;
    }
}
