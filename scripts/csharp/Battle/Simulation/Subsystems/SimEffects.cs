using System;
using System.Collections.Generic;
using Fateforged.Simulation.Combat;
using Fateforged.Units;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;

namespace Fateforged.Simulation.Subsystems;

/// <summary>
/// Effect system for buffs, debuffs, triggers, and delayed effects.
/// Operates on UnitData.ActiveBuffs and UnitData.Triggers.
/// No Godot dependencies — pure simulation.
/// </summary>
public static class SimEffects
{

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

                // Duration countdown (skip permanent buffs: Duration == -1)
                if (buff.Duration > 0)
                {
                    buff.Duration -= fixedDelta;
                    if (buff.Duration <= 0)
                    {
                        events.Add(new BuffExpiredEvent(unit.UnitId, buff.BuffId, buff.EffectType));
                        unit.ActiveBuffs.RemoveAt(i);
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
        List<SimEvent> events)
    {
        if (!target.IsAlive) return;

        switch (effectType)
        {
            case EffectType.Damage:
                ApplyDirectDamage(state, target, value, damageType, sourceUnitId, sourceTeam, events);
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
                ApplyDirectDamage(state, target, value, damageType, sourceUnitId, sourceTeam, events);
                break;

            case EffectType.Slow:
            case EffectType.Stun:
            case EffectType.Haste:
            case EffectType.DamageBoost:
            case EffectType.StatModifier:
                ApplyBuff(state, target, effectType, value, duration, damageType, sourceUnitId, sourceTeam, events);
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
        List<SimEvent> events)
    {
        foreach (var trigger in unit.Triggers)
        {
            if (trigger.TriggerType != triggerType) continue;

            // One-shot check (HpThreshold, OnDeath fire once)
            if (trigger.HasFired && (triggerType == TriggerType.HpThreshold || triggerType == TriggerType.OnDeath))
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
                ApplyEffect(state, trigger.EffectType, trigger.Value, trigger.Duration,
                    trigger.DamageType, target, unit.UnitId, unit.Team, events);
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
        List<SimEvent> events)
    {
        // Fire OnDeath triggers on the dying unit
        FireTriggers(state, dyingUnit, TriggerType.OnDeath, killer, events);

        // Fire LeaderDeath triggers on group members if this was a leader
        if (dyingUnit.GroupId.HasValue)
        {
            foreach (var kvp in state.Units)
            {
                var member = kvp.Value;
                if (!member.IsAlive) continue;
                if (member.UnitId == dyingUnit.UnitId) continue;
                if (member.LeaderId != dyingUnit.UnitId) continue;

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
        float speed = unit.MoveSpeed;
        foreach (var buff in unit.ActiveBuffs)
        {
            if (buff.EffectType == EffectType.Slow)
                speed *= (1f - buff.Value); // Value is proportion (0.3 = 30% slow)
            else if (buff.EffectType == EffectType.Haste)
                speed *= (1f + buff.Value);
        }
        return MathF.Max(speed, 0f);
    }

    /// <summary>
    /// Get effective attack damage accounting for DamageBoost buffs.
    /// </summary>
    public static float GetEffectiveAttackDamage(UnitData unit)
    {
        float damage = unit.AttackDamage;
        foreach (var buff in unit.ActiveBuffs)
        {
            if (buff.EffectType == EffectType.DamageBoost)
                damage *= (1f + buff.Value);
        }
        return damage;
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

    // =========================================================================
    // SHIELD (existing)
    // =========================================================================

    /// <summary>
    /// Apply a shield to a unit. Shields are consumed oldest-first during damage calculation.
    /// </summary>
    public static void ApplyShield(MatchState state, UnitData target, float shieldHp, int sourceUnitId, Team sourceTeam)
    {
        target.ActiveBuffs.Add(new ActiveBuff
        {
            BuffId = state.NextBuffId(),
            EffectType = EffectType.Shield,
            ShieldHp = shieldHp,
            Duration = -1, // Permanent until consumed
            SourceUnitId = sourceUnitId,
            SourceTeam = sourceTeam
        });
    }

    /// <summary>
    /// Consume shields on a unit, oldest first. Returns remaining damage after absorption.
    /// </summary>
    public static float AbsorbWithShields(UnitData target, float incomingDamage, List<SimEvent>? events)
    {
        float remaining = incomingDamage;

        for (int i = 0; i < target.ActiveBuffs.Count && remaining > 0; i++)
        {
            var buff = target.ActiveBuffs[i];
            if (buff.EffectType != EffectType.Shield) continue;

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
        MatchState state, UnitData target, float baseDamage, DamageType damageType,
        int sourceUnitId, Team sourceTeam, List<SimEvent> events)
    {
        // Get source unit for attacker stats (may be dead for delayed effects)
        UnitData? attacker = state.Units.TryGetValue(sourceUnitId, out var a) ? a : null;
        var attackerSummoner = state.Summoners[(int)sourceTeam];
        var targetSummoner = (int)target.Team >= 0 && (int)target.Team <= 1 ? state.Summoners[(int)target.Team] : null;

        var (damage, isCrit, _) = SimDamage.Calculate(
            baseDamage, damageType, attacker, target, attackerSummoner, targetSummoner, state.Rng, events);

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
        float previousHp = target.CurrentHp;
        target.CurrentHp = MathF.Min(target.CurrentHp + amount, target.MaxHp);
        float actualHeal = target.CurrentHp - previousHp;

        if (actualHeal > 0)
        {
            // Could add a HealEvent here if needed for visual feedback
        }
    }

    private static void ApplyBuff(
        MatchState state, UnitData target, EffectType effectType, float value, float duration,
        DamageType damageType, int sourceUnitId, Team sourceTeam, List<SimEvent> events)
    {
        var buff = new ActiveBuff
        {
            BuffId = state.NextBuffId(),
            EffectType = effectType,
            Value = value,
            Duration = duration,
            DamageType = damageType,
            SourceUnitId = sourceUnitId,
            SourceTeam = sourceTeam
        };
        target.ActiveBuffs.Add(buff);
        events.Add(new BuffAppliedEvent(target.UnitId, effectType, value, duration));
    }

    private static void ApplyPeriodicTick(
        MatchState state, UnitData unit, ActiveBuff buff, List<SimEvent> events)
    {
        switch (buff.EffectType)
        {
            case EffectType.Damage:
                // DoT intentionally bypasses SimDamage.Calculate — DoT effects apply flat
                // damage that ignores defense, crit, and shields. This matches the design
                // intent where DoT represents guaranteed damage over time.
                unit.CurrentHp -= buff.Value;
                events.Add(new UnitDamagedEvent(unit.UnitId, buff.SourceUnitId, buff.Value, false));
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
        MatchState state, UnitData unit, float fixedDelta, List<SimEvent> events)
    {
        foreach (var trigger in unit.Triggers)
        {
            if (trigger.TriggerType != TriggerType.Periodic) continue;

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
        MatchState state, UnitData unit, List<SimEvent> events)
    {
        if (unit.MaxHp <= 0) return;

        float hpPercent = unit.CurrentHp / unit.MaxHp;

        foreach (var trigger in unit.Triggers)
        {
            if (trigger.TriggerType != TriggerType.HpThreshold) continue;
            if (trigger.HasFired) continue;

            if (hpPercent <= trigger.Threshold)
            {
                // Fire the trigger effect on self
                ApplyEffect(state, trigger.EffectType, trigger.Value, trigger.Duration,
                    trigger.DamageType, unit, unit.UnitId, unit.Team, events);
                trigger.HasFired = true;
            }
        }
    }

    private static void ApplyAreaEffect(
        MatchState state, UnitData source, TriggerConfig trigger, List<SimEvent> events)
    {
        int enemyTeam = MatchState.GetEnemyTeam((int)source.Team);
        float radiusSq = trigger.AoeRadius * trigger.AoeRadius;

        foreach (var kvp in state.Units)
        {
            var candidate = kvp.Value;
            if (!candidate.IsAlive) continue;
            if ((int)candidate.Team != enemyTeam) continue;

            float distSq = source.Position.DistanceSquaredTo(candidate.Position);
            if (distSq > radiusSq) continue;

            ApplyEffect(state, trigger.EffectType, trigger.Value, trigger.Duration,
                trigger.DamageType, candidate, source.UnitId, source.Team, events);
        }
    }

    private static void QueueDelayedEffect(
        MatchState state, TriggerConfig trigger, UnitData source)
    {
        state.DelayedEffects.Add(new DelayedEffect
        {
            Timer = trigger.Delay,
            EffectType = trigger.EffectType,
            Value = trigger.Value,
            DamageType = trigger.DamageType,
            AoeRadius = trigger.AoeRadius,
            Position = source.Position,
            SourceUnitId = source.UnitId,
            SourceTeam = source.Team
        });
    }

    private static void ExecuteDelayedEffect(
        MatchState state, DelayedEffect effect, List<SimEvent> events)
    {
        events.Add(new DelayedEffectFiredEvent(effect.Position, effect.EffectType, effect.AoeRadius));

        if (effect.AoeRadius > 0)
        {
            // Area effect — damage all enemies in radius
            int enemyTeam = MatchState.GetEnemyTeam((int)effect.SourceTeam);
            float radiusSq = effect.AoeRadius * effect.AoeRadius;

            foreach (var kvp in state.Units)
            {
                var candidate = kvp.Value;
                if (!candidate.IsAlive) continue;
                if ((int)candidate.Team != enemyTeam) continue;

                float distSq = effect.Position.DistanceSquaredTo(candidate.Position);
                if (distSq > radiusSq) continue;

                ApplyDirectDamage(state, candidate, effect.Value, effect.DamageType,
                    effect.SourceUnitId, effect.SourceTeam, events);
            }
        }
    }
}
