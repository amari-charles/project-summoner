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

    private enum BuffRemovalReason
    {
        Expired,
        ShieldBreak,
        OwnerDeath,
    }

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
        var units = new List<UnitData>(state.GetAliveActiveUnits());
        foreach (var unit in units)
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
                        RemoveBuff(state, unit, i, events, BuffRemovalReason.Expired);
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
        List<SimEvent> events,
        StatusEffectKind statusKind = StatusEffectKind.None,
        float statusTickInterval = 1f,
        float statusPotencyPerStack = 0f,
        int statusMaxStacks = 1,
        SimVector3? sourcePosition = null,
        BuffRemovalEffectConfig? removalEffect = null
    )
    {
        var lifetime = EffectLifetimeResolver.Resolve(EffectLifetime.Timed(0f), duration);
        ApplyEffect(
            state,
            new EffectApplicationSpec
            {
                EffectType = effectType,
                Value = value,
                Duration = duration,
                Lifetime = lifetime,
                DamageType = damageType,
                StatusKind = statusKind,
                StatusTickInterval = statusTickInterval,
                StatusPotencyPerStack = statusPotencyPerStack,
                StatusMaxStacks = statusMaxStacks,
                RemovalEffect = removalEffect,
                Context = new EffectApplicationContext
                {
                    SourceUnitId = sourceUnitId,
                    SourceTeam = sourceTeam,
                    SourcePosition = sourcePosition,
                },
            },
            target,
            events
        );
    }

    /// <summary>
    /// Apply a fully resolved runtime effect spec to a target unit.
    /// </summary>
    public static bool ApplyEffect(
        MatchState state,
        EffectApplicationSpec spec,
        UnitData target,
        List<SimEvent> events
    )
    {
        if (!target.IsAlive)
        {
            LogEffectSkipped(state, spec, target, "target_dead");
            return false;
        }
        if (!CanApplyEffect(state, spec, target))
        {
            LogEffectSkipped(state, spec, target, "requirements_failed");
            return false;
        }

        var context = spec.Context;
        float duration = spec.ResolvedDuration;
        var targetBefore = CombatDebugFormatter.Capture(target);
        switch (spec.EffectType)
        {
            case EffectType.Damage:
                if (!ApplyDirectDamage(
                    state,
                    target,
                    spec.Value,
                    spec.DamageType,
                    context.SourceUnitId,
                    context.SourceTeam,
                    events,
                    spec.RemovalEffect,
                    context.TriggerSourceOnHit,
                    context.TriggerTargetOnDamaged,
                    context.UseAttackDamageProfile
                ))
                    return false;
                EmitCue(spec, target, EffectCuePhase.Executed, events);
                break;

            case EffectType.Heal:
                ApplyHeal(target, spec.Value, events);
                EmitCue(spec, target, EffectCuePhase.Executed, events);
                break;

            case EffectType.Shield:
                ApplyShield(
                    state,
                    target,
                    spec.Value,
                    duration,
                    context.SourceUnitId,
                    context.SourceTeam,
                    spec.RemovalEffect,
                    spec,
                    events
                );
                break;

            case EffectType.AreaDamage:
                // AreaDamage is handled by the caller (FireTriggers) which resolves targets
                // If called directly, treat as single-target damage
                if (!ApplyDirectDamage(
                    state,
                    target,
                    spec.Value,
                    spec.DamageType,
                    context.SourceUnitId,
                    context.SourceTeam,
                    events,
                    spec.RemovalEffect,
                    context.TriggerSourceOnHit,
                    context.TriggerTargetOnDamaged,
                    context.UseAttackDamageProfile
                ))
                    return false;
                EmitCue(spec, target, EffectCuePhase.Executed, events);
                break;

            case EffectType.Cleanse:
                ApplyCleanse(state, target, events);
                EmitCue(spec, target, EffectCuePhase.Executed, events);
                break;

            case EffectType.Knockback:
                ApplyKnockback(
                    state,
                    target,
                    spec.Value,
                    context.SourceUnitId,
                    context.SourceTeam,
                    context.SourcePosition
                );
                EmitCue(spec, target, EffectCuePhase.Executed, events);
                break;

            case EffectType.Displacement:
                ApplyDisplacement(
                    state,
                    target,
                    spec.Value,
                    context.SourceUnitId,
                    context.SourceTeam,
                    context.SourcePosition
                );
                EmitCue(spec, target, EffectCuePhase.Executed, events);
                break;

            case EffectType.SourceLungeToTarget:
                ApplySourceLungeToTarget(state, target, spec.Value, context.SourceUnitId);
                EmitCue(spec, target, EffectCuePhase.Executed, events);
                break;

            case EffectType.Taunt:
                ApplyTaunt(target, context.SourceUnitId, duration, events, spec);
                break;

            case EffectType.StatusApply:
                ApplyStackingStatus(
                    state,
                    target,
                    context.SourceUnitId,
                    context.SourceTeam,
                    spec.StatusKind,
                    duration,
                    spec.StatusTickInterval,
                    spec.StatusPotencyPerStack > 0f ? spec.StatusPotencyPerStack : spec.Value,
                    spec.StatusMaxStacks,
                    spec.DamageType,
                    events,
                    spec
                );
                break;

            case EffectType.StatusConsume:
                ConsumeStatus(
                    state,
                    target,
                    context.SourceUnitId,
                    context.SourceTeam,
                    spec.StatusKind == StatusEffectKind.None ? StatusEffectKind.Burn : spec.StatusKind,
                    spec.Value > 0f ? spec.Value : 1f,
                    spec.DamageType,
                    events
                );
                EmitCue(spec, target, EffectCuePhase.Executed, events);
                break;

            case EffectType.Slow:
            case EffectType.Stun:
            case EffectType.Root:
            case EffectType.Haste:
            case EffectType.DamageBoost:
            case EffectType.StatModifier:
            case EffectType.EvasionModifier:
            case EffectType.AttackSpeedModifier:
            case EffectType.FlatDamageReduction:
            case EffectType.AccuracyModifier:
            case EffectType.RangedDamageModifier:
            case EffectType.ReviveOnDeath:
                ApplyBuff(
                    state,
                    target,
                    spec.EffectType,
                    spec.Value,
                    duration,
                    spec.DamageType,
                    context.SourceUnitId,
                    context.SourceTeam,
                    events,
                    spec.RemovalEffect,
                    spec
                );
                break;
        }

        LogEffectApplied(state, spec, target, targetBefore);
        return true;
    }

    private static bool CanApplyEffect(MatchState state, EffectApplicationSpec spec, UnitData target)
    {
        if (spec.RequiredTargetElementId >= 0 && target.ElementId != spec.RequiredTargetElementId)
            return false;

        var requirements = spec.TagRequirements;
        if (requirements.IsEmpty)
            return true;

        state.Units.TryGetValue(spec.Context.SourceUnitId, out var source);
        var sourceTags = CombatTagSet.GetOwnedTags(source);
        var targetTags = CombatTagSet.GetOwnedTags(target);

        return CombatTagSet.HasAll(sourceTags, requirements.RequiredSourceTags)
            && !CombatTagSet.HasAny(sourceTags, requirements.BlockedSourceTags)
            && CombatTagSet.HasAll(targetTags, requirements.RequiredTargetTags)
            && !CombatTagSet.HasAny(targetTags, requirements.BlockedTargetTags);
    }

    private static void EmitCue(
        EffectApplicationSpec spec,
        UnitData target,
        EffectCuePhase phase,
        List<SimEvent>? events
    )
    {
        if (events == null || string.IsNullOrWhiteSpace(spec.CueId))
            return;

        events.Add(
            new EffectCueEvent(
                spec.CueId,
                phase,
                spec.EffectType,
                spec.Context.SourceUnitId,
                target.UnitId,
                spec.Context.SourcePosition ?? target.Position
            )
        );
    }

    private static void LogEffectApplied(
        MatchState state,
        EffectApplicationSpec spec,
        UnitData target,
        UnitDebugSnapshot targetBefore
    )
    {
        if (!Simulation.DebugAbilityLogsEnabled)
            return;
        if (
            !string.IsNullOrWhiteSpace(spec.Context.AbilityId)
            || !string.IsNullOrWhiteSpace(spec.Context.CardCatalogId)
        )
            return;

        Simulation.DebugAbilityLog(CombatDebugFormatter.FormatEffectApplied(state, spec, target, targetBefore));
    }

    private static void LogEffectSkipped(
        MatchState state,
        EffectApplicationSpec spec,
        UnitData target,
        string reason
    )
    {
        if (!Simulation.DebugAbilityLogsEnabled)
            return;
        if (!string.IsNullOrWhiteSpace(spec.Context.CardCatalogId))
            return;

        Simulation.DebugAbilityLog(CombatDebugFormatter.FormatEffectSkipped(state, spec, target, reason));
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
        SimAbilityOrchestrator.TryActivateOnDeathEffects(state, dyingUnit, killer, events);

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
        List<SimEvent> events,
        EffectApplicationSpec? spec = null
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
            if (spec != null)
            {
                buff.GrantedTags = new List<string>(spec.GrantedTags);
                buff.StackKey = spec.ResolvedStackKey;
                buff.CueId = spec.CueId;
                EmitCue(spec, target, EffectCuePhase.Active, events);
            }
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
            if (spec != null && string.IsNullOrWhiteSpace(existing.CueId))
                existing.CueId = spec.CueId;
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
        float duration,
        int sourceUnitId,
        Team sourceTeam,
        BuffRemovalEffectConfig? removalEffect = null,
        EffectApplicationSpec? spec = null,
        List<SimEvent>? events = null
    )
    {
        if (TryApplyStackPolicy(target, spec, shieldHp, duration, state, events))
            return;

        var lifetime = EffectLifetimeResolver.Resolve(EffectLifetime.Timed(0f), duration);
        float resolvedDuration = lifetime.ToLegacyDuration();
        target.ActiveBuffs.Add(
            new ActiveBuff
            {
                BuffId = state.NextBuffId(),
                EffectType = EffectType.Shield,
                ShieldHp = shieldHp,
                Duration = resolvedDuration,
                Lifetime = lifetime,
                SourceUnitId = sourceUnitId,
                SourceTeam = sourceTeam,
                RemovalEffect = removalEffect,
                OwnerHpAtApply = target.CurrentHp,
                GrantedTags = spec != null ? new List<string>(spec.GrantedTags) : new List<string>(),
                StackKey = spec?.ResolvedStackKey ?? "",
                CueId = spec?.CueId ?? "",
            }
        );
        events?.Add(new BuffAppliedEvent(target.UnitId, EffectType.Shield, shieldHp, resolvedDuration));
        if (spec != null)
            EmitCue(spec, target, EffectCuePhase.Active, events);
    }

    public static void ApplyShield(
        MatchState state,
        UnitData target,
        float shieldHp,
        int sourceUnitId,
        Team sourceTeam
    ) => ApplyShield(state, target, shieldHp, -1f, sourceUnitId, sourceTeam);

    /// <summary>
    /// Consume shields on a unit, oldest first. Returns remaining damage after absorption.
    /// </summary>
    public static float AbsorbWithShields(
        MatchState? state,
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
                RemoveBuff(state, target, i, events, BuffRemovalReason.ShieldBreak);
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

    public static float AbsorbWithShields(
        UnitData target,
        float incomingDamage,
        List<SimEvent>? events
    ) => AbsorbWithShields(null, target, incomingDamage, events);

    public static void TriggerBuffRemovalEffectsForOwnerDeath(
        MatchState state,
        UnitData owner,
        List<SimEvent> events
    )
    {
        for (int i = owner.ActiveBuffs.Count - 1; i >= 0; i--)
        {
            if (ShouldFireRemovalEffect(owner.ActiveBuffs[i], BuffRemovalReason.OwnerDeath))
                RemoveBuff(state, owner, i, events, BuffRemovalReason.OwnerDeath);
        }
    }

    // =========================================================================
    // INTERNAL HELPERS
    // =========================================================================

    private static void RemoveBuff(
        MatchState? state,
        UnitData owner,
        int buffIndex,
        List<SimEvent>? events,
        BuffRemovalReason reason
    )
    {
        if (buffIndex < 0 || buffIndex >= owner.ActiveBuffs.Count)
            return;

        var buff = owner.ActiveBuffs[buffIndex];
        owner.ActiveBuffs.RemoveAt(buffIndex);
        events?.Add(new BuffExpiredEvent(owner.UnitId, buff.BuffId, buff.EffectType));
        if (events != null && !string.IsNullOrWhiteSpace(buff.CueId))
        {
            events.Add(
                new EffectCueEvent(
                    buff.CueId,
                    EffectCuePhase.Removed,
                    buff.EffectType,
                    buff.SourceUnitId,
                    owner.UnitId,
                    owner.Position
                )
            );
        }

        if (state != null && events != null && owner.IsAlive)
            SimAbilityOrchestrator.TryActivateOnBuffRemovedEffects(state, owner, buff, events);

        if (state == null || events == null || !ShouldFireRemovalEffect(buff, reason))
            return;

        FireRemovalEffect(state, owner, buff, events);
    }

    private static bool ShouldFireRemovalEffect(ActiveBuff buff, BuffRemovalReason reason)
    {
        var removal = buff.RemovalEffect;
        if (removal == null)
            return false;

        return reason switch
        {
            BuffRemovalReason.Expired => removal.TriggerOnExpire,
            BuffRemovalReason.ShieldBreak => removal.TriggerOnShieldBreak,
            BuffRemovalReason.OwnerDeath => removal.TriggerOnOwnerDeath,
            _ => false,
        };
    }

    private static void FireRemovalEffect(
        MatchState state,
        UnitData owner,
        ActiveBuff buff,
        List<SimEvent> events
    )
    {
        var removal = buff.RemovalEffect;
        if (removal == null)
            return;

        float value = removal.Value;
        if (removal.ScaleValueByOwnerHpAtApply)
            value += buff.OwnerHpAtApply * removal.OwnerHpAtApplyMultiplier;

        float radius = MathF.Max(0f, removal.Radius);
        var targets = ResolveRemovalEffectTargets(state, owner, buff.SourceTeam, removal.Affinity, radius);
        float duration = EffectLifetimeResolver.ResolveDuration(removal.Lifetime, removal.Duration);
        foreach (var target in targets)
        {
            ApplyEffect(
                state,
                removal.EffectType,
                value,
                duration,
                removal.DamageType,
                target,
                buff.SourceUnitId,
                buff.SourceTeam,
                events,
                sourcePosition: owner.Position
            );
        }
    }

    private static List<UnitData> ResolveRemovalEffectTargets(
        MatchState state,
        UnitData owner,
        Team sourceTeam,
        SpellAffinity affinity,
        float radius
    )
    {
        int sourceTeamId = (int)sourceTeam;
        int? teamFilter = affinity switch
        {
            SpellAffinity.Enemies => MatchState.GetEnemyTeam(sourceTeamId),
            SpellAffinity.Allies => sourceTeamId,
            _ => null,
        };

        var targets = new List<UnitData>();
        foreach (var candidate in state.GetAliveActiveUnits())
        {
            if (candidate.UnitId == owner.UnitId)
                continue;
            if (teamFilter.HasValue && (int)candidate.Team != teamFilter.Value)
                continue;
            if (radius > 0f && owner.Position.DistanceSquaredTo(candidate.Position) > radius * radius)
                continue;
            targets.Add(candidate);
        }

        targets.Sort((a, b) => a.UnitId.CompareTo(b.UnitId));
        return targets;
    }

    private static bool ApplyDirectDamage(
        MatchState state,
        UnitData target,
        float baseDamage,
        DamageType damageType,
        int sourceUnitId,
        Team sourceTeam,
        List<SimEvent> events,
        BuffRemovalEffectConfig? removalEffect = null,
        bool triggerSourceOnHit = false,
        bool triggerTargetOnDamaged = true,
        bool useAttackDamageProfile = false
    )
    {
        // Get source unit for attacker stats (may be dead for delayed effects)
        UnitData? attacker = state.Units.TryGetValue(sourceUnitId, out var a) ? a : null;
        var attackerSummoner = state.Summoners[(int)sourceTeam];
        var targetSummoner =
            (int)target.Team >= 0 && (int)target.Team <= 1
                ? state.Summoners[(int)target.Team]
                : null;

        var (damage, isCrit, wasEvaded) =
            useAttackDamageProfile && attacker != null
                ? SimDamage.CalculateAttack(
                    baseDamage,
                    attacker,
                    target,
                    attackerSummoner,
                    targetSummoner,
                    state.Rng,
                    events,
                    state
                )
                : SimDamage.Calculate(
                    baseDamage,
                    damageType,
                    attacker,
                    target,
                    attackerSummoner,
                    targetSummoner,
                    state.Rng,
                    events: events,
                    state: state
                );
        if (wasEvaded)
            return false;

        target.CurrentHp -= damage;
        events.Add(new UnitDamagedEvent(target.UnitId, sourceUnitId, damage, isCrit));
        if (triggerSourceOnHit && attacker != null && target.UnitId != sourceUnitId)
            SimAbilityOrchestrator.TryActivateOnHitEffects(state, attacker, target, events);
        if (triggerTargetOnDamaged && target.CurrentHp > 0f && target.UnitId != sourceUnitId)
            SimAbilityOrchestrator.TryActivateOnDamagedEffects(state, target, attacker, events);

        if (target.CurrentHp <= 0)
        {
            if (SimUtils.KillUnit(state, target, sourceUnitId, events))
            {
                // Fire death triggers on the killed unit
                FireDeathTriggers(state, target, attacker, events);
            }
        }

        return true;
    }

    private static void ApplyHeal(UnitData target, float amount, List<SimEvent> events)
    {
        target.CurrentHp = MathF.Min(target.CurrentHp + amount, target.MaxHp);
    }

    private static bool TryApplyStackPolicy(
        UnitData target,
        EffectApplicationSpec? spec,
        float value,
        float duration,
        MatchState state,
        List<SimEvent>? events
    )
    {
        if (spec == null || spec.StackPolicy == EffectStackPolicy.Independent)
            return false;

        string stackKey = spec.ResolvedStackKey;
        if (string.IsNullOrWhiteSpace(stackKey))
            return false;

        foreach (var existing in target.ActiveBuffs)
        {
            if (existing.EffectType != spec.EffectType)
                continue;
            if (existing.StackKey != stackKey)
                continue;

            var lifetime = EffectLifetimeResolver.Resolve(existing.Lifetime, existing.Duration);
            float currentDuration = lifetime.ToLegacyDuration();
            float refreshedDuration =
                currentDuration < 0f || duration < 0f ? -1f : MathF.Max(currentDuration, duration);

            if (spec.StackPolicy == EffectStackPolicy.StackAndRefreshDuration)
            {
                existing.StackCount = Math.Max(1, existing.StackCount) + 1;
                existing.Value += value;
                if (existing.EffectType == EffectType.Shield)
                    existing.ShieldHp += value;
            }
            else
            {
                existing.Value = value;
                if (existing.EffectType == EffectType.Shield)
                    existing.ShieldHp = MathF.Max(existing.ShieldHp, value);
            }

            existing.Duration = refreshedDuration;
            existing.Lifetime = EffectLifetimeResolver.Resolve(
                refreshedDuration < 0f ? EffectLifetime.Persistent() : EffectLifetime.Timed(refreshedDuration),
                refreshedDuration
            );
            existing.SourceUnitId = spec.Context.SourceUnitId;
            existing.SourceTeam = spec.Context.SourceTeam;
            if (existing.GrantedTags.Count == 0 && spec.GrantedTags.Count > 0)
                existing.GrantedTags = new List<string>(spec.GrantedTags);
            if (string.IsNullOrWhiteSpace(existing.CueId))
                existing.CueId = spec.CueId;

            events?.Add(new BuffAppliedEvent(target.UnitId, spec.EffectType, existing.Value, refreshedDuration));
            EmitCue(spec, target, EffectCuePhase.Active, events);
            return true;
        }

        return false;
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
        List<SimEvent> events,
        BuffRemovalEffectConfig? removalEffect = null,
        EffectApplicationSpec? spec = null
    )
    {
        if (TryApplyStackPolicy(target, spec, value, duration, state, events))
            return;

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
            RemovalEffect = removalEffect,
            OwnerHpAtApply = target.CurrentHp,
            GrantedTags = spec != null ? new List<string>(spec.GrantedTags) : new List<string>(),
            StackKey = spec?.ResolvedStackKey ?? "",
            CueId = spec?.CueId ?? "",
        };
        target.ActiveBuffs.Add(buff);
        events.Add(new BuffAppliedEvent(target.UnitId, effectType, value, resolvedDuration));
        if (spec != null)
            EmitCue(spec, target, EffectCuePhase.Active, events);
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

    private static void ConsumeStatus(
        MatchState state,
        UnitData target,
        int sourceUnitId,
        Team sourceTeam,
        StatusEffectKind statusKind,
        float multiplier,
        DamageType damageType,
        List<SimEvent> events
    )
    {
        if (statusKind == StatusEffectKind.None)
            return;

        float consumedDamage = 0f;
        for (int i = target.ActiveBuffs.Count - 1; i >= 0; i--)
        {
            var buff = target.ActiveBuffs[i];
            if (
                buff.EffectType != EffectType.Damage
                || buff.TickInterval <= 0f
                || buff.StatusKind != statusKind
            )
            {
                continue;
            }

            float duration = EffectLifetimeResolver.ResolveDuration(buff.Lifetime, buff.Duration);
            float remainingTicks = buff.TickInterval > 0f
                ? MathF.Ceiling(MathF.Max(duration, 0f) / buff.TickInterval)
                : 0f;
            consumedDamage += buff.Value * remainingTicks;
            target.ActiveBuffs.RemoveAt(i);
            events.Add(new BuffExpiredEvent(target.UnitId, buff.BuffId, buff.EffectType));
        }

        if (consumedDamage <= 0f)
            return;

        ApplyDirectDamage(
            state,
            target,
            consumedDamage * multiplier,
            damageType,
            sourceUnitId,
            sourceTeam,
            events
        );
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
                SourcePosition = source.Position,
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

        var targets = SpellTargetResolver.Resolve(state, effect);
        var spec = SpellEffectSpecFactory.FromDelayedEffect(effect);
        SpellEffectExecutor.Apply(state, effect.CardCatalogId, spec, targets, events, delayed: true);
    }

    private static void ApplyCleanse(MatchState state, UnitData target, List<SimEvent> events)
    {
        for (int i = target.ActiveBuffs.Count - 1; i >= 0; i--)
        {
            var buff = target.ActiveBuffs[i];
            if (!IsNegativeBuffForCleanse(buff))
                continue;

            RemoveBuff(state, target, i, events, BuffRemovalReason.Expired);
        }

        target.Engagement.ForcedTargetUnitId = null;
        target.Engagement.ForcedTargetTimer = 0f;
    }

    private static void ApplyTaunt(
        UnitData target,
        int sourceUnitId,
        float duration,
        List<SimEvent> events,
        EffectApplicationSpec? spec = null
    )
    {
        if (sourceUnitId < 0 || duration <= 0f)
            return;
        if (!ShouldApplySoftTaunt(target, sourceUnitId))
            return;

        target.Engagement.ForcedTargetUnitId = sourceUnitId;
        target.Engagement.ForcedTargetTimer = MathF.Max(
            target.Engagement.ForcedTargetTimer,
            duration
        );
        events.Add(
            new StatusAppliedEvent(sourceUnitId, target.UnitId, StatusEffectKind.Taunt, 1, duration)
        );
        if (spec != null)
            EmitCue(spec, target, EffectCuePhase.Active, events);
    }

    private static bool ShouldApplySoftTaunt(UnitData target, int sourceUnitId)
    {
        if (target.Engagement.ForcedTargetTimer <= 0f)
            return true;
        if (!target.Engagement.ForcedTargetUnitId.HasValue)
            return true;
        return target.Engagement.ForcedTargetUnitId.Value == sourceUnitId;
    }

    private static bool IsNegativeBuffForCleanse(ActiveBuff buff)
    {
        if (
            buff.EffectType == EffectType.Slow
            || buff.EffectType == EffectType.Stun
            || buff.EffectType == EffectType.Root
            || buff.EffectType == EffectType.AccuracyModifier
            || buff.EffectType == EffectType.RangedDamageModifier
        )
        {
            return true;
        }

        if (buff.EffectType != EffectType.Damage || buff.TickInterval <= 0f)
            return false;

        return buff.StatusKind == StatusEffectKind.Burn || buff.StatusKind == StatusEffectKind.Poison;
    }

    private static void ApplyKnockback(
        MatchState state,
        UnitData target,
        float distance,
        int sourceUnitId,
        Team sourceTeam,
        SimVector3? sourcePosition
    )
    {
        if (distance <= 0f || !target.IsAlive)
            return;
        if (target.UnitId == sourceUnitId)
            return;

        var sourcePos = sourcePosition ?? ResolveSourcePosition(state, sourceUnitId, sourceTeam, target.Position);
        ApplyForcedDisplacement(target, distance, sourcePos, sourceTeam, pushAway: true);
    }

    private static void ApplyDisplacement(
        MatchState state,
        UnitData target,
        float distance,
        int sourceUnitId,
        Team sourceTeam,
        SimVector3? sourcePosition
    )
    {
        if (MathF.Abs(distance) <= 0f || !target.IsAlive)
            return;

        var sourcePos = sourcePosition ?? ResolveSourcePosition(state, sourceUnitId, sourceTeam, target.Position);
        ApplyForcedDisplacement(target, MathF.Abs(distance), sourcePos, sourceTeam, pushAway: distance >= 0f);
    }

    private static void ApplySourceLungeToTarget(
        MatchState state,
        UnitData target,
        float standoffDistance,
        int sourceUnitId
    )
    {
        if (!target.IsAlive)
            return;
        if (!state.Units.TryGetValue(sourceUnitId, out var source) || !source.IsAlive)
            return;
        if (source.UnitId == target.UnitId)
            return;

        float desiredStandoff =
            standoffDistance > 0f ? standoffDistance : MathF.Max(0.6f, source.AttackRange * 0.7f);
        float dx = target.Position.X - source.Position.X;
        float dz = target.Position.Z - source.Position.Z;
        float distanceSq = dx * dx + dz * dz;
        float closeEnough = MathF.Max(source.AttackRange * 0.85f, desiredStandoff);
        if (distanceSq <= closeEnough * closeEnough)
            return;

        if (distanceSq <= 0.0001f)
        {
            dx = source.Team == Team.Player ? 1f : -1f;
            dz = 0f;
            distanceSq = 1f;
        }

        float invDistance = 1f / MathF.Sqrt(distanceSq);
        var direction = new SimVector3(dx * invDistance, 0f, dz * invDistance);
        source.Position = new SimVector3(
            target.Position.X - direction.X * desiredStandoff,
            source.Position.Y,
            target.Position.Z - direction.Z * desiredStandoff
        );
    }

    private static void ApplyForcedDisplacement(
        UnitData target,
        float distance,
        SimVector3 sourcePos,
        Team sourceTeam,
        bool pushAway
    )
    {
        var direction = pushAway
            ? new SimVector3(target.Position.X - sourcePos.X, 0f, target.Position.Z - sourcePos.Z)
            : new SimVector3(sourcePos.X - target.Position.X, 0f, sourcePos.Z - target.Position.Z);
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
