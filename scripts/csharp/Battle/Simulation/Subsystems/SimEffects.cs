using System;
using System.Collections.Generic;
using Fateforged.Constants;
using Fateforged.Simulation.Combat;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Effects;
using Fateforged.Simulation.Enums;
using Fateforged.Units;

namespace Fateforged.Simulation.Subsystems;

/// <summary>
/// Effect system for buffs, debuffs, triggers, and delayed effects.
/// Operates on UnitData.ActiveBuffs and UnitData.Triggers.
/// No Godot dependencies — pure simulation.
/// </summary>
public static class SimEffects
{
    private const float KnockbackDurationSeconds = 0.22f;

    // =========================================================================
    // TICK METHODS (called from Simulation.Tick steps 7-8)
    // =========================================================================

    /// <summary>
    /// Tick all active buffs on all alive units:
    /// - Decrement durations
    /// - Apply periodic effects (DoT, HoT)
    /// - Remove expired buffs
    /// </summary>
    public static void TickBuffs(MatchState state, float fixedDelta, List<SimEvent> events)
    {
        foreach (var unit in state.GetAliveActiveUnits())
        {
            for (int i = unit.ActiveBuffs.Count - 1; i >= 0; i--)
            {
                var buff = unit.ActiveBuffs[i];

                // Periodic tick (DoT, HoT)
                if (buff.TickInterval > 0)
                {
                    buff.TickTimer -= fixedDelta;
                    if (buff.TickTimer <= 0)
                    {
                        buff.TickTimer += buff.TickInterval;
                        ApplyPeriodicTick(state, unit, buff, events);
                    }
                }

                // Duration countdown (skip persistent buffs).
                var lifetime = EffectLifetimeResolver.Resolve(buff.Lifetime, buff.Duration);
                buff.Lifetime = lifetime;
                buff.Duration = lifetime.ToLegacyDuration();
                if (buff.Duration > 0)
                {
                    buff.Duration -= fixedDelta;
                    if (buff.Duration <= 0)
                    {
                        events.Add(new BuffExpiredEvent(unit.UnitId, buff.BuffId, buff.EffectType));
                        unit.ActiveBuffs.RemoveAt(i);
                    }
                    else
                    {
                        buff.Lifetime = EffectLifetime.Timed(buff.Duration);
                    }
                }
            }

            // Tick periodic triggers (Periodic type fires on interval)
            TickPeriodicTriggers(state, unit, fixedDelta, events);

            // Check HP threshold triggers
            CheckHpThresholdTriggers(state, unit, events);
        }
    }

    /// <summary>
    /// Tick delayed effects (death explosions, timed AoE, etc.).
    /// Processes effects that were queued with a delay.
    /// </summary>
    public static void TickDelayedEffects(MatchState state, float fixedDelta, List<SimEvent> events)
    {
        for (int i = state.DelayedEffects.Count - 1; i >= 0; i--)
        {
            var effect = state.DelayedEffects[i];
            effect.Timer -= fixedDelta;

            if (effect.Timer <= 0)
            {
                ExecuteDelayedEffect(state, effect, events);
                state.DelayedEffects.RemoveAt(i);
            }
        }
    }

    // =========================================================================
    // APPLY EFFECT (central dispatch)
    // =========================================================================

    /// <summary>
    /// Apply an effect to a target unit.
    /// Central dispatch for all effect types (damage, heal, stat modifier, etc.).
    /// </summary>
    public static void ApplyEffect(
        MatchState state,
        EffectType effectType,
        float value,
        float duration,
        DamageType damageType,
        UnitData target,
        int sourceUnitId,
        Team sourceTeam,
        List<SimEvent> events
    )
    {
        if (!target.IsAlive)
            return;

        switch (effectType)
        {
            case EffectType.Damage:
                ApplyDirectDamage(
                    state,
                    target,
                    value,
                    damageType,
                    sourceUnitId,
                    sourceTeam,
                    events
                );
                break;

            case EffectType.Heal:
                ApplyHeal(target, value, events);
                break;

            case EffectType.Shield:
                ApplyShield(state, target, value, sourceUnitId, sourceTeam);
                events.Add(new BuffAppliedEvent(target.UnitId, EffectType.Shield, value, -1));
                break;

            case EffectType.AreaDamage:
                // AreaDamage is handled by the caller (FireTriggers) which resolves targets
                // If called directly, treat as single-target damage
                ApplyDirectDamage(
                    state,
                    target,
                    value,
                    damageType,
                    sourceUnitId,
                    sourceTeam,
                    events
                );
                break;

            case EffectType.Cleanse:
                ApplyCleanse(target, events);
                break;

            case EffectType.Knockback:
                ApplyKnockback(state, target, value, sourceUnitId, sourceTeam);
                break;

            case EffectType.Slow:
            case EffectType.Stun:
            case EffectType.Haste:
            case EffectType.DamageBoost:
            case EffectType.StatModifier:
            case EffectType.EvasionModifier:
            case EffectType.AttackSpeedModifier:
            case EffectType.FlatDamageReduction:
                ApplyBuff(
                    state,
                    target,
                    effectType,
                    value,
                    duration,
                    damageType,
                    sourceUnitId,
                    sourceTeam,
                    events
                );
                break;
        }
    }

    // =========================================================================
    // TRIGGER SYSTEM
    // =========================================================================

    /// <summary>
    /// Fire triggers of a specific type on a unit.
    /// Called by SimBehavior at combat moments (OnHit, OnDamaged, OnKill, OnDeath).
    /// </summary>
    public static void FireTriggers(
        MatchState state,
        UnitData unit,
        TriggerType triggerType,
        UnitData? target,
        List<SimEvent> events
    )
    {
        foreach (var trigger in unit.Triggers)
        {
            if (trigger.TriggerType != triggerType)
                continue;

            // One-shot check (HpThreshold, OnDeath fire once)
            if (
                trigger.HasFired
                && (triggerType == TriggerType.HpThreshold || triggerType == TriggerType.OnDeath)
            )
                continue;

            if (trigger.Delay > 0 && triggerType == TriggerType.OnDeath)
            {
                // Queue delayed effect (e.g., death explosion)
                QueueDelayedEffect(state, trigger, unit);
                trigger.HasFired = true;
                continue;
            }

            // Resolve effect targets
            if (trigger.AoeRadius > 0)
            {
                // Area effect — damage all enemies in radius
                ApplyAreaEffect(state, unit, trigger, events);
            }
            else if (target != null)
            {
                // Single target effect
                ApplyEffect(
                    state,
                    trigger.EffectType,
                    trigger.Value,
                    EffectLifetimeResolver.ResolveDuration(trigger.Lifetime, trigger.Duration),
                    trigger.DamageType,
                    target,
                    unit.UnitId,
                    unit.Team,
                    events
                );
            }

            trigger.HasFired = true;
        }
    }

    /// <summary>
    /// Fire OnDeath triggers for a unit, including LeaderDeath on group members.
    /// </summary>
    public static void FireDeathTriggers(
        MatchState state,
        UnitData dyingUnit,
        UnitData? killer,
        List<SimEvent> events
    )
    {
        // Fire OnDeath triggers on the dying unit
        FireTriggers(state, dyingUnit, TriggerType.OnDeath, killer, events);

        // Fire LeaderDeath triggers on group members if this was a leader
        if (dyingUnit.GroupId.HasValue)
        {
            foreach (var kvp in state.Units)
            {
                var member = kvp.Value;
                if (!member.IsAlive)
                    continue;
                if (member.UnitId == dyingUnit.UnitId)
                    continue;
                if (member.LeaderId != dyingUnit.UnitId)
                    continue;

                FireTriggers(state, member, TriggerType.LeaderDeath, null, events);
            }
        }
    }

    // =========================================================================
    // STAT QUERY HELPERS (used by SimBehavior/SimMovement)
    // =========================================================================

    /// <summary>
    /// Get effective move speed accounting for Slow and Haste buffs.
    /// </summary>
    public static float GetEffectiveMoveSpeed(UnitData unit)
    {
        return EffectStatResolver.GetEffectiveMoveSpeed(unit);
    }

    /// <summary>
    /// Get effective attack damage accounting for DamageBoost buffs.
    /// </summary>
    public static float GetEffectiveAttackDamage(UnitData unit)
    {
        return EffectStatResolver.GetEffectiveAttackDamage(unit);
    }

    /// <summary>
    /// Get effective attack speed accounting for AttackSpeedModifier buffs.
    /// </summary>
    public static float GetEffectiveAttackSpeed(UnitData unit)
    {
        return EffectStatResolver.GetEffectiveAttackSpeed(unit);
    }

    /// <summary>
    /// Get cumulative flat damage reduction from active buffs.
    /// </summary>
    public static float GetFlatDamageReduction(UnitData unit)
    {
        return EffectStatResolver.GetFlatDamageReduction(unit);
    }

    /// <summary>
    /// Check if a unit is stunned (has any active Stun buff).
    /// </summary>
    public static bool IsStunned(UnitData unit)
    {
        foreach (var buff in unit.ActiveBuffs)
        {
            if (buff.EffectType == EffectType.Stun)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Apply/stack a periodic status payload (poison/burn) on a target.
    /// Reapplications increase potency up to max stacks and refresh duration.
    /// </summary>
    public static void ApplyStackingStatus(
        MatchState state,
        UnitData target,
        int sourceUnitId,
        Team sourceTeam,
        StatusEffectKind statusKind,
        float durationSeconds,
        float tickIntervalSeconds,
        float potencyPerStack,
        int maxStacks,
        DamageType damageType,
        List<SimEvent> events
    )
    {
        if (statusKind == StatusEffectKind.None)
            return;
        if (durationSeconds <= 0f || tickIntervalSeconds <= 0f || potencyPerStack <= 0f)
            return;

        maxStacks = Math.Max(1, maxStacks);
        ActiveBuff? existing = null;
        foreach (var buff in target.ActiveBuffs)
        {
            if (
                buff.EffectType == EffectType.Damage
                && buff.TickInterval > 0f
                && buff.StatusKind == statusKind
            )
            {
                existing = buff;
                break;
            }
        }

        int stackCount;
        if (existing == null)
        {
            var buff = new ActiveBuff
            {
                BuffId = state.NextBuffId(),
                EffectType = EffectType.Damage,
                Value = potencyPerStack,
                Duration = durationSeconds,
                Lifetime = EffectLifetime.Timed(durationSeconds),
                TickInterval = tickIntervalSeconds,
                TickTimer = tickIntervalSeconds,
                SourceUnitId = sourceUnitId,
                SourceTeam = sourceTeam,
                DamageType = damageType,
                StatusKind = statusKind,
                StackCount = 1,
            };
            target.ActiveBuffs.Add(buff);
            stackCount = 1;
        }
        else
        {
            existing.StackCount = Math.Min(maxStacks, Math.Max(1, existing.StackCount) + 1);
            existing.Value = potencyPerStack * existing.StackCount;
            float existingDuration = EffectLifetimeResolver.ResolveDuration(
                existing.Lifetime,
                existing.Duration
            );
            float refreshedDuration = MathF.Max(existingDuration, durationSeconds);
            existing.Duration = refreshedDuration;
            existing.Lifetime = EffectLifetime.Timed(refreshedDuration);
            existing.TickInterval = tickIntervalSeconds;
            if (existing.TickTimer <= 0f || existing.TickTimer > tickIntervalSeconds)
                existing.TickTimer = tickIntervalSeconds;
            existing.SourceUnitId = sourceUnitId;
            existing.SourceTeam = sourceTeam;
            existing.DamageType = damageType;
            stackCount = existing.StackCount;
        }

        events.Add(
            new StatusAppliedEvent(
                sourceUnitId,
                target.UnitId,
                statusKind,
                stackCount,
                durationSeconds
            )
        );
    }

    // =========================================================================
    // SHIELD (existing)
    // =========================================================================

    /// <summary>
    /// Apply a shield to a unit. Shields are consumed oldest-first during damage calculation.
    /// </summary>
    public static void ApplyShield(
        MatchState state,
        UnitData target,
        float shieldHp,
        int sourceUnitId,
        Team sourceTeam
    )
    {
        target.ActiveBuffs.Add(
            new ActiveBuff
            {
                BuffId = state.NextBuffId(),
                EffectType = EffectType.Shield,
                ShieldHp = shieldHp,
                Duration = -1, // Permanent until consumed
                Lifetime = EffectLifetime.Persistent(),
                SourceUnitId = sourceUnitId,
                SourceTeam = sourceTeam,
            }
        );
    }

    /// <summary>
    /// Consume shields on a unit, oldest first. Returns remaining damage after absorption.
    /// </summary>
    public static float AbsorbWithShields(
        UnitData target,
        float incomingDamage,
        List<SimEvent>? events
    )
    {
        float remaining = incomingDamage;

        for (int i = 0; i < target.ActiveBuffs.Count && remaining > 0; i++)
        {
            var buff = target.ActiveBuffs[i];
            if (buff.EffectType != EffectType.Shield)
                continue;

            if (buff.ShieldHp <= remaining)
            {
                // Shield fully consumed
                remaining -= buff.ShieldHp;
                buff.ShieldHp = 0;
                target.ActiveBuffs.RemoveAt(i);
                i--; // Adjust index after removal
            }
            else
            {
                // Shield partially consumed
                buff.ShieldHp -= remaining;
                remaining = 0;
            }
        }

        return remaining;
    }

    // =========================================================================
    // INTERNAL HELPERS
    // =========================================================================

    private static void ApplyDirectDamage(
        MatchState state,
        UnitData target,
        float baseDamage,
        DamageType damageType,
        int sourceUnitId,
        Team sourceTeam,
        List<SimEvent> events
    )
    {
        // Get source unit for attacker stats (may be dead for delayed effects)
        UnitData? attacker = state.Units.TryGetValue(sourceUnitId, out var a) ? a : null;
        var attackerSummoner = state.Summoners[(int)sourceTeam];
        var targetSummoner =
            (int)target.Team >= 0 && (int)target.Team <= 1
                ? state.Summoners[(int)target.Team]
                : null;

        var (damage, isCrit, _) = SimDamage.Calculate(
            baseDamage,
            damageType,
            attacker,
            target,
            attackerSummoner,
            targetSummoner,
            state.Rng,
            events: events
        );

        target.CurrentHp -= damage;
        events.Add(new UnitDamagedEvent(target.UnitId, sourceUnitId, damage, isCrit));

        if (target.CurrentHp <= 0)
        {
            SimUtils.KillUnit(state, target, sourceUnitId, events);

            // Fire death triggers on the killed unit
            FireDeathTriggers(state, target, attacker, events);
        }
    }

    private static void ApplyHeal(UnitData target, float amount, List<SimEvent> events)
    {
        target.CurrentHp = MathF.Min(target.CurrentHp + amount, target.MaxHp);
    }

    private static void ApplyBuff(
        MatchState state,
        UnitData target,
        EffectType effectType,
        float value,
        float duration,
        DamageType damageType,
        int sourceUnitId,
        Team sourceTeam,
        List<SimEvent> events
    )
    {
        var lifetime = EffectLifetimeResolver.Resolve(EffectLifetime.Timed(0f), duration);
        float resolvedDuration = lifetime.ToLegacyDuration();
        var buff = new ActiveBuff
        {
            BuffId = state.NextBuffId(),
            EffectType = effectType,
            Value = value,
            Duration = resolvedDuration,
            Lifetime = lifetime,
            DamageType = damageType,
            SourceUnitId = sourceUnitId,
            SourceTeam = sourceTeam,
        };
        target.ActiveBuffs.Add(buff);
        events.Add(new BuffAppliedEvent(target.UnitId, effectType, value, resolvedDuration));
    }

    private static void ApplyPeriodicTick(
        MatchState state,
        UnitData unit,
        ActiveBuff buff,
        List<SimEvent> events
    )
    {
        switch (buff.EffectType)
        {
            case EffectType.Damage:
                // DoT intentionally bypasses SimDamage.Calculate — DoT effects apply flat
                // damage that ignores defense, crit, and shields. This matches the design
                // intent where DoT represents guaranteed damage over time.
                float periodicDamage = EffectStatResolver.ApplyFlatDamageReduction(unit, buff.Value);
                if (periodicDamage <= 0f)
                    break;

                unit.CurrentHp -= periodicDamage;
                events.Add(new UnitDamagedEvent(unit.UnitId, buff.SourceUnitId, periodicDamage, false));
                if (unit.CurrentHp <= 0)
                {
                    SimUtils.KillUnit(state, unit, buff.SourceUnitId, events);
                }
                break;

            case EffectType.Heal:
                // HoT — heal the unit
                ApplyHeal(unit, buff.Value, events);
                break;
        }
    }

    private static void TickPeriodicTriggers(
        MatchState state,
        UnitData unit,
        float fixedDelta,
        List<SimEvent> events
    )
    {
        foreach (var trigger in unit.Triggers)
        {
            if (trigger.TriggerType != TriggerType.Periodic)
                continue;

            trigger.PeriodicTimer -= fixedDelta;
            if (trigger.PeriodicTimer <= 0)
            {
                trigger.PeriodicTimer += trigger.Interval;

                if (trigger.AoeRadius > 0)
                {
                    ApplyAreaEffect(state, unit, trigger, events);
                }
            }
        }
    }

    private static void CheckHpThresholdTriggers(
        MatchState state,
        UnitData unit,
        List<SimEvent> events
    )
    {
        if (unit.MaxHp <= 0)
            return;

        float hpPercent = unit.CurrentHp / unit.MaxHp;

        foreach (var trigger in unit.Triggers)
        {
            if (trigger.TriggerType != TriggerType.HpThreshold)
                continue;
            if (trigger.HasFired)
                continue;

            if (hpPercent <= trigger.Threshold)
            {
                // Fire the trigger effect on self
                ApplyEffect(
                    state,
                    trigger.EffectType,
                    trigger.Value,
                    EffectLifetimeResolver.ResolveDuration(trigger.Lifetime, trigger.Duration),
                    trigger.DamageType,
                    unit,
                    unit.UnitId,
                    unit.Team,
                    events
                );
                trigger.HasFired = true;
            }
        }
    }

    private static void ApplyAreaEffect(
        MatchState state,
        UnitData source,
        TriggerConfig trigger,
        List<SimEvent> events
    )
    {
        int enemyTeam = MatchState.GetEnemyTeam((int)source.Team);
        float radiusSq = trigger.AoeRadius * trigger.AoeRadius;

        foreach (var kvp in state.Units)
        {
            var candidate = kvp.Value;
            if (!candidate.IsAlive)
                continue;
            if ((int)candidate.Team != enemyTeam)
                continue;

            float distSq = source.Position.DistanceSquaredTo(candidate.Position);
            if (distSq > radiusSq)
                continue;

            ApplyEffect(
                state,
                trigger.EffectType,
                trigger.Value,
                EffectLifetimeResolver.ResolveDuration(trigger.Lifetime, trigger.Duration),
                trigger.DamageType,
                candidate,
                source.UnitId,
                source.Team,
                events
            );
        }
    }

    private static void QueueDelayedEffect(MatchState state, TriggerConfig trigger, UnitData source)
    {
        var lifetime = EffectLifetimeResolver.Resolve(trigger.Lifetime, trigger.Duration);
        state.DelayedEffects.Add(
            new DelayedEffect
            {
                Timer = trigger.Delay,
                EffectType = trigger.EffectType,
                Value = trigger.Value,
                Duration = lifetime.ToLegacyDuration(),
                Lifetime = lifetime,
                DamageType = trigger.DamageType,
                AoeRadius = trigger.AoeRadius,
                Position = source.Position,
                SourceUnitId = source.UnitId,
                SourceTeam = source.Team,
            }
        );
    }

    private static void ExecuteDelayedEffect(
        MatchState state,
        DelayedEffect effect,
        List<SimEvent> events
    )
    {
        events.Add(
            new DelayedEffectFiredEvent(effect.Position, effect.EffectType, effect.AoeRadius)
        );

        var targets = ResolveDelayedTargets(state, effect);
        foreach (var target in targets)
        {
            ApplyEffect(
                state,
                effect.EffectType,
                effect.Value,
                EffectLifetimeResolver.ResolveDuration(effect.Lifetime, effect.Duration),
                effect.DamageType,
                target,
                effect.SourceUnitId,
                effect.SourceTeam,
                events
            );
        }
    }

    private static List<UnitData> ResolveDelayedTargets(MatchState state, DelayedEffect effect)
    {
        var targets = new List<UnitData>();
        int sourceTeam = (int)effect.SourceTeam;
        int? teamFilter = effect.Affinity switch
        {
            SpellAffinity.Enemies => MatchState.GetEnemyTeam(sourceTeam),
            SpellAffinity.Allies => sourceTeam,
            _ => null,
        };

        switch (effect.TargetingMode)
        {
            case SpellTargetingMode.Position:
            {
                float radius = effect.AoeRadius;
                foreach (var candidate in state.GetAliveActiveUnits())
                {
                    if (teamFilter.HasValue && (int)candidate.Team != teamFilter.Value)
                        continue;
                    if (
                        !SpellAreaResolver.IsWithinArea(
                            effect.AreaShape,
                            effect.Position,
                            candidate.Position,
                            radius
                        )
                    )
                        continue;
                    targets.Add(candidate);
                }
                break;
            }

            case SpellTargetingMode.NearestEnemy:
            {
                if (effect.TargetUnitId.HasValue)
                {
                    var pinned = state.GetAliveUnit(effect.TargetUnitId.Value);
                    if (pinned != null)
                        targets.Add(pinned);
                    break;
                }

                int enemyTeam = MatchState.GetEnemyTeam(sourceTeam);
                UnitData? best = null;
                float bestDistSq = float.MaxValue;

                foreach (var candidate in state.GetAliveActiveUnitsForTeam(enemyTeam))
                {
                    float distSq = candidate.Position.DistanceSquaredTo(effect.Position);
                    if (distSq >= bestDistSq)
                        continue;
                    best = candidate;
                    bestDistSq = distSq;
                }

                if (best != null)
                    targets.Add(best);
                break;
            }

            case SpellTargetingMode.AlliesInRadius:
            {
                float radius = effect.AoeRadius;
                foreach (var candidate in state.GetAliveActiveUnitsForTeam(sourceTeam))
                {
                    if (
                        !SpellAreaResolver.IsWithinArea(
                            effect.AreaShape,
                            effect.Position,
                            candidate.Position,
                            radius
                        )
                    )
                        continue;
                    targets.Add(candidate);
                }
                break;
            }
        }

        return targets;
    }

    private static void ApplyCleanse(UnitData target, List<SimEvent> events)
    {
        for (int i = target.ActiveBuffs.Count - 1; i >= 0; i--)
        {
            var buff = target.ActiveBuffs[i];
            if (!IsNegativeBuffForCleanse(buff))
                continue;

            target.ActiveBuffs.RemoveAt(i);
            events.Add(new BuffExpiredEvent(target.UnitId, buff.BuffId, buff.EffectType));
        }

        target.ForcedTargetUnitId = null;
        target.ForcedTargetTimer = 0f;
    }

    private static bool IsNegativeBuffForCleanse(ActiveBuff buff)
    {
        if (buff.EffectType == EffectType.Slow || buff.EffectType == EffectType.Stun)
            return true;

        if (buff.EffectType != EffectType.Damage || buff.TickInterval <= 0f)
            return false;

        return buff.StatusKind == StatusEffectKind.Burn || buff.StatusKind == StatusEffectKind.Poison;
    }

    private static void ApplyKnockback(
        MatchState state,
        UnitData target,
        float distance,
        int sourceUnitId,
        Team sourceTeam
    )
    {
        if (distance <= 0f || !target.IsAlive)
            return;
        if (target.UnitId == sourceUnitId)
            return;

        var sourcePos = ResolveSourcePosition(state, sourceUnitId, sourceTeam, target.Position);
        var direction = new SimVector3(target.Position.X - sourcePos.X, 0f, target.Position.Z - sourcePos.Z);
        float lengthSq = direction.X * direction.X + direction.Z * direction.Z;

        if (lengthSq <= 0.0001f)
        {
            // Deterministic fallback when source/target overlap.
            direction = sourceTeam == Team.Player ? new SimVector3(1f, 0f, 0f) : new SimVector3(-1f, 0f, 0f);
        }
        else
        {
            float invLen = 1f / MathF.Sqrt(lengthSq);
            direction = new SimVector3(direction.X * invLen, 0f, direction.Z * invLen);
        }

        float knockbackSpeed = distance / KnockbackDurationSeconds;
        float alignment =
            target.KnockbackDirection.X * direction.X + target.KnockbackDirection.Z * direction.Z;

        target.KnockbackDirection = direction;
        target.KnockbackSpeed = MathF.Max(target.KnockbackSpeed, knockbackSpeed);
        if (target.KnockbackRemainingDistance <= 0f)
        {
            target.KnockbackRemainingDistance = distance;
            return;
        }

        // Same-direction hits stack travel distance; opposing hits resolve to latest strongest push.
        target.KnockbackRemainingDistance = alignment >= 0.25f
            ? target.KnockbackRemainingDistance + distance
            : MathF.Max(target.KnockbackRemainingDistance, distance);
    }

    private static SimVector3 ResolveSourcePosition(
        MatchState state,
        int sourceUnitId,
        Team sourceTeam,
        SimVector3 fallback
    )
    {
        if (sourceUnitId >= 0 && state.Units.TryGetValue(sourceUnitId, out var sourceUnit))
            return sourceUnit.Position;

        if (MatchState.IsSummonerTarget(sourceUnitId))
        {
            int team = MatchState.GetSummonerTeamFromTargetId(sourceUnitId);
            if (team >= 0 && team < state.Summoners.Length)
                return state.Summoners[team].Position;
        }

        int sourceTeamIndex = (int)sourceTeam;
        if (sourceTeamIndex >= 0 && sourceTeamIndex < state.Summoners.Length)
            return state.Summoners[sourceTeamIndex].Position;

        return fallback;
    }
}
