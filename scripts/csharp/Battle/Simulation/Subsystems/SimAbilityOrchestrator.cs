using System;
using System.Collections.Generic;
using System.Linq;
using Fateforged.Data.Projectiles;
using Fateforged.Simulation.Combat;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Effects;
using Fateforged.Simulation.Enums;
using Fateforged.Units;

namespace Fateforged.Simulation.Subsystems;

/// <summary>
/// Simulation-owned ability runtime orchestrator.
/// Executes unit abilities from primitive trigger/target/delivery/effect specs.
/// </summary>
public static class SimAbilityOrchestrator
{
    public static void Tick(MatchState state, float fixedDelta, List<SimEvent> events)
    {
        foreach (var unit in state.GetAliveActiveUnits())
        {
            if (unit.Abilities.Count == 0)
                continue;

            foreach (var ability in unit.Abilities)
            {
                TickAbility(state, unit, ability, fixedDelta, events);
            }
        }
    }

    private static void TickAbility(
        MatchState state,
        UnitData source,
        UnitAbilityState ability,
        float fixedDelta,
        List<SimEvent> events
    )
    {
        if (ability.CooldownTimer > 0f)
            ability.CooldownTimer = MathF.Max(0f, ability.CooldownTimer - fixedDelta);

        if (ability.Trigger == UnitAbilityTrigger.OnSpawn && ability.HasApplied)
            return;
        if (ability.Trigger != UnitAbilityTrigger.OnSpawn && ability.Trigger != UnitAbilityTrigger.Periodic)
            return;
        if (ability.CooldownTimer > 0f)
            return;

        bool activated = TryActivateAbility(state, source, ability, null, events);
        if (!activated)
            return;

        if (ability.Trigger == UnitAbilityTrigger.OnSpawn)
            ability.HasApplied = true;
        else
            ability.CooldownTimer = MathF.Max(ability.CooldownSeconds, 0f);
    }

    public static void TryActivateOnHitEffects(
        MatchState state,
        UnitData source,
        UnitData target,
        List<SimEvent> events
    )
    {
        if (!source.IsAlive || !target.IsAlive || source.UnitId == target.UnitId)
            return;

        foreach (var ability in source.Abilities)
        {
            if (ability.Trigger != UnitAbilityTrigger.OnHit)
                continue;
            if (ability.CooldownTimer > 0f)
                continue;
            if (!CanApplyToTarget(source, target, ability.TargetAffinity))
                continue;
            if (!TryActivateAbility(state, source, ability, target, events))
                continue;

            ability.CooldownTimer = MathF.Max(ability.CooldownSeconds, 0f);
        }
    }

    public static void TryActivateOnDamagedEffects(
        MatchState state,
        UnitData source,
        UnitData? attacker,
        List<SimEvent> events
    )
    {
        if (!source.IsAlive)
            return;

        foreach (var ability in source.Abilities)
        {
            if (ability.Trigger != UnitAbilityTrigger.OnDamaged)
                continue;
            if (ability.CooldownTimer > 0f)
                continue;
            if (
                attacker != null
                && !CanApplyToTarget(source, attacker, ability.TargetAffinity)
                && ability.Targeting == UnitAbilityTargeting.HitTarget
            )
            {
                continue;
            }
            if (!TryActivateAbility(state, source, ability, attacker, events))
                continue;

            ability.CooldownTimer = MathF.Max(ability.CooldownSeconds, 0f);
        }
    }

    public static void TryActivateOnDeathEffects(
        MatchState state,
        UnitData source,
        UnitData? killer,
        List<SimEvent> events
    )
    {
        foreach (var ability in source.Abilities)
        {
            if (ability.Trigger != UnitAbilityTrigger.OnDeath)
                continue;
            if (ability.HasApplied)
                continue;
            if (!TryActivateAbility(state, source, ability, killer, events))
                continue;

            ability.HasApplied = true;
        }
    }

    public static void TryActivateOnBuffRemovedEffects(
        MatchState state,
        UnitData source,
        ActiveBuff removedBuff,
        List<SimEvent> events
    )
    {
        if (!source.IsAlive)
            return;

        foreach (var ability in source.Abilities)
        {
            if (ability.Trigger != UnitAbilityTrigger.OnBuffRemoved)
                continue;
            if (ability.CooldownTimer > 0f)
                continue;
            if (!TryActivateAbility(state, source, ability, null, events))
                continue;

            ability.CooldownTimer = MathF.Max(ability.CooldownSeconds, 0f);
        }
    }

    private static bool TryActivateAbility(
        MatchState state,
        UnitData source,
        UnitAbilityState ability,
        UnitData? contextTarget,
        List<SimEvent> events
    )
    {
        var targets = ResolveTargets(state, source, ability, contextTarget);
        if (targets.Count == 0)
            return false;

        return ability.Delivery switch
        {
            UnitAbilityDelivery.Projectile => TryDeliverProjectile(
                state,
                source,
                ability,
                contextTarget,
                targets[0],
                events
            ),
            UnitAbilityDelivery.Delayed => TryDeliverScheduled(
                state,
                source,
                ability,
                contextTarget,
                targets,
                includeImmediate: false,
                events
            ),
            UnitAbilityDelivery.RepeatedArea => TryDeliverScheduled(
                state,
                source,
                ability,
                contextTarget,
                targets,
                includeImmediate: true,
                events
            ),
            _ => TryDeliverInstant(state, source, ability, contextTarget, targets, events),
        };
    }

    private static bool TryDeliverInstant(
        MatchState state,
        UnitData source,
        UnitAbilityState ability,
        UnitData? contextTarget,
        List<UnitData> targets,
        List<SimEvent> events
    )
    {
        var effects = ResolveEffects(ability);
        var before = CombatDebugFormatter.CaptureUnits(BuildDebugSnapshotUnits(source, contextTarget, targets));
        int applied = 0;

        foreach (var effect in effects.Where(e => e.EffectType == EffectType.TransferHealth))
            applied += ApplyHealthRedistribution(source, targets, effect, events);

        foreach (var target in targets)
        {
            foreach (var effect in effects.Where(e => e.EffectType != EffectType.TransferHealth))
            {
                var spec = BuildEffectSpec(source, ability, effect);
                if (SimEffects.ApplyEffect(state, spec, target, events))
                    applied++;
            }
        }

        if (applied <= 0)
            return false;

        int? eventTarget = targets.Count == 1 ? targets[0].UnitId : null;
        LogAbilityActivation(state, source, ability, contextTarget, targets, effects, applied, before);
        events.Add(new AbilityActivatedEvent(source.UnitId, ability.AbilityId, eventTarget, source.Position));
        return true;
    }

    private static bool TryDeliverScheduled(
        MatchState state,
        UnitData source,
        UnitAbilityState ability,
        UnitData? contextTarget,
        List<UnitData> targets,
        bool includeImmediate,
        List<SimEvent> events
    )
    {
        var effects = ResolveEffects(ability);
        if (effects.Count == 0)
            return false;

        int scheduled = 0;
        bool emittedImmediateActivation = false;
        float baseDelay = MathF.Max(0f, ability.DeliveryDelaySeconds);
        if (baseDelay <= 0f && !includeImmediate)
            baseDelay = MathF.Max(0f, ability.WindupSeconds);
        int applications = Math.Max(1, ability.RepeatCount + 1);
        float interval = MathF.Max(0f, ability.RepeatIntervalSeconds);

        for (int applicationIndex = 0; applicationIndex < applications; applicationIndex++)
        {
            float delay = baseDelay + applicationIndex * interval;
            if (delay <= 0f && includeImmediate && applicationIndex == 0)
            {
                if (TryDeliverInstant(state, source, ability, contextTarget, targets, events))
                {
                    scheduled++;
                    emittedImmediateActivation = true;
                }
                continue;
            }

            foreach (var effect in effects)
            {
                if (effect.EffectType == EffectType.TransferHealth)
                    continue;

                QueueDelayedAbilityEffect(state, source, ability, effect, targets, delay);
                scheduled++;
            }
        }

        if (scheduled <= 0)
            return false;

        if (!emittedImmediateActivation)
        {
            int? eventTarget = targets.Count == 1 ? targets[0].UnitId : null;
            var before = CombatDebugFormatter.CaptureUnits(
                BuildDebugSnapshotUnits(source, contextTarget, targets)
            );
            LogAbilityActivation(state, source, ability, contextTarget, targets, effects, scheduled, before);
            events.Add(
                new AbilityActivatedEvent(source.UnitId, ability.AbilityId, eventTarget, source.Position)
            );
        }
        return true;
    }

    private static void QueueDelayedAbilityEffect(
        MatchState state,
        UnitData source,
        UnitAbilityState ability,
        UnitAbilityEffectState effect,
        List<UnitData> targets,
        float delay
    )
    {
        var spec = BuildEffectSpec(source, ability, effect);
        bool areaTargeting =
            ability.Targeting == UnitAbilityTargeting.AlliesInRadius
            || ability.Targeting == UnitAbilityTargeting.EnemiesInRadius;

        if (areaTargeting)
        {
            state.DelayedEffects.Add(
                BuildDelayedEffect(source, ability, effect, spec, delay, null, source.Position)
            );
            return;
        }

        foreach (var target in targets)
        {
            state.DelayedEffects.Add(
                BuildDelayedEffect(source, ability, effect, spec, delay, target.UnitId, target.Position)
            );
        }
    }

    private static DelayedEffect BuildDelayedEffect(
        UnitData source,
        UnitAbilityState ability,
        UnitAbilityEffectState effect,
        EffectApplicationSpec spec,
        float delay,
        int? targetUnitId,
        SimVector3 position
    )
    {
        float radius = ability.Radius > 0f ? ability.Radius : 0f;
        return new DelayedEffect
        {
            Timer = MathF.Max(0f, delay),
            EffectType = effect.EffectType,
            Value = effect.Value,
            Duration = spec.ResolvedDuration,
            Lifetime = effect.Lifetime,
            DamageType = effect.DamageType,
            AoeRadius = targetUnitId.HasValue ? 0f : radius,
            Position = position,
            SourceUnitId = source.UnitId,
            SourceTeam = source.Team,
            Affinity = ToSpellAffinity(ability.TargetAffinity),
            TargetingMode = targetUnitId.HasValue
                ? SpellTargetingMode.NearestEnemy
                : SpellTargetingMode.Position,
            TargetUnitId = targetUnitId,
            StatusKind = effect.StatusKind,
            StatusTickInterval = effect.StatusTickInterval,
            StatusPotencyPerStack = effect.StatusPotencyPerStack,
            StatusMaxStacks = effect.StatusMaxStacks,
            TagRequirements = spec.TagRequirements.DeepClone(),
            GrantedTags = new List<string>(spec.GrantedTags),
            StackPolicy = spec.StackPolicy,
            StackKey = spec.StackKey,
            CueId = spec.CueId,
        };
    }

    private static bool TryDeliverProjectile(
        MatchState state,
        UnitData source,
        UnitAbilityState ability,
        UnitData? contextTarget,
        UnitData target,
        List<SimEvent> events
    )
    {
        if (!TryResolveProjectileData(source, ability, out var projectileData))
            return false;

        var effect = ResolvePrimaryEffect(ability);
        var targetPos = target.Position;
        var startPos = source.Position;
        if (projectileData.SpawnAtTargetHeight)
            startPos = new SimVector3(startPos.X, targetPos.Y, startPos.Z);

        SimProjectile.Spawn(
            state,
            sourceUnitId: source.UnitId,
            targetUnitId: target.UnitId,
            team: source.Team,
            damage: effect.Value,
            sourceElementId: source.ElementId,
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
            targetAffinity: ability.TargetAffinity,
            impactKind: ResolveProjectileImpact(effect),
            statusKind: effect.StatusKind,
            statusDuration: effect.StatusDuration,
            statusTickInterval: effect.StatusTickInterval,
            statusPotencyPerStack: effect.StatusPotencyPerStack,
            statusMaxStacks: effect.StatusMaxStacks
        );

        events.Add(
            new AbilityActivatedEvent(source.UnitId, ability.AbilityId, target.UnitId, source.Position)
        );
        var before = CombatDebugFormatter.CaptureUnits(
            BuildDebugSnapshotUnits(source, contextTarget, new List<UnitData> { target })
        );
        LogAbilityActivation(
            state,
            source,
            ability,
            contextTarget,
            new List<UnitData> { target },
            new List<UnitAbilityEffectState> { effect },
            1,
            before
        );
        return true;
    }

    private static List<UnitData> ResolveTargets(
        MatchState state,
        UnitData source,
        UnitAbilityState ability,
        UnitData? contextTarget
    )
    {
        return ability.Targeting switch
        {
            UnitAbilityTargeting.Self => source.IsAlive ? new List<UnitData> { source } : new List<UnitData>(),
            UnitAbilityTargeting.HitTarget => contextTarget is { IsAlive: true }
                ? new List<UnitData> { contextTarget }
                : new List<UnitData>(),
            UnitAbilityTargeting.CurrentTarget => ResolveCurrentTarget(state, source),
            UnitAbilityTargeting.LowestHpAlly => ResolveLowestHpAlly(state, source, ability),
            UnitAbilityTargeting.AlliesInRadius => ResolveUnitsInRadius(
                state,
                source,
                ability,
                (int)source.Team
            ),
            UnitAbilityTargeting.EnemiesInRadius => ResolveUnitsInRadius(
                state,
                source,
                ability,
                MatchState.GetEnemyTeam((int)source.Team)
            ),
            UnitAbilityTargeting.HealthRedistributionPool => ResolveUnitsInRadius(
                state,
                source,
                ability,
                (int)source.Team
            ),
            _ => new List<UnitData>(),
        };
    }

    private static int ApplyHealthRedistribution(
        UnitData source,
        List<UnitData> candidates,
        UnitAbilityEffectState effect,
        List<SimEvent> events
    )
    {
        float amount = effect.Value > 0f ? effect.Value : 12f;
        float donorThreshold = 0.70f;
        float donorFloor = 0.60f;
        float receiverThreshold = 0.45f;
        float receiverCap = 0.80f;
        int applied = 0;

        var receivers = candidates
            .Where(u => u.MaxHp > 0f && u.CurrentHp / u.MaxHp < receiverThreshold)
            .OrderBy(u => u.CurrentHp / u.MaxHp)
            .ThenBy(u => u.UnitId)
            .ToList();
        var donors = candidates
            .Where(u =>
                u.UnitId != source.UnitId
                && u.MaxHp > 0f
                && u.CurrentHp / u.MaxHp > donorThreshold
            )
            .OrderByDescending(u => u.CurrentHp / u.MaxHp)
            .ThenBy(u => u.UnitId)
            .ToList();

        foreach (var receiver in receivers)
        {
            float receiverRoom = receiver.MaxHp * receiverCap - receiver.CurrentHp;
            if (receiverRoom <= 0f)
                continue;

            foreach (var donor in donors)
            {
                float donorAvailable = donor.CurrentHp - donor.MaxHp * donorFloor;
                if (donorAvailable <= 0f)
                    continue;

                float transfer = MathF.Min(amount, MathF.Min(receiverRoom, donorAvailable));
                if (transfer <= 0f)
                    continue;

                donor.CurrentHp -= transfer;
                receiver.CurrentHp = MathF.Min(receiver.CurrentHp + transfer, receiver.MaxHp);
                events.Add(new UnitDamagedEvent(donor.UnitId, source.UnitId, transfer, false));
                applied++;
                return applied;
            }
        }

        return applied;
    }

    private static List<UnitData> ResolveCurrentTarget(MatchState state, UnitData source)
    {
        if (!source.Engagement.TargetUnitId.HasValue)
            return new List<UnitData>();

        var target = state.GetAliveUnit(source.Engagement.TargetUnitId.Value);
        return target != null ? new List<UnitData> { target } : new List<UnitData>();
    }

    private static List<UnitData> ResolveLowestHpAlly(
        MatchState state,
        UnitData source,
        UnitAbilityState ability
    )
    {
        float rangeSq = ability.Range > 0f ? ability.Range * ability.Range : source.AggroRadius * source.AggroRadius;
        UnitData? best = null;
        float bestHpPct = float.MaxValue;
        float bestDistSq = float.MaxValue;

        foreach (var candidate in state.GetAliveActiveUnitsForTeam((int)source.Team))
        {
            if (candidate.UnitId == source.UnitId)
                continue;
            if (candidate.CurrentHp >= candidate.MaxHp)
                continue;

            float distSq = source.Position.DistanceSquaredTo(candidate.Position);
            if (distSq > rangeSq)
                continue;

            float hpPct = candidate.MaxHp > 0f ? candidate.CurrentHp / candidate.MaxHp : 1f;
            if (
                best == null
                || hpPct < bestHpPct
                || (MathF.Abs(hpPct - bestHpPct) < 0.0001f && distSq < bestDistSq)
                || (
                    MathF.Abs(hpPct - bestHpPct) < 0.0001f
                    && MathF.Abs(distSq - bestDistSq) < 0.0001f
                    && candidate.UnitId < best.UnitId
                )
            )
            {
                best = candidate;
                bestHpPct = hpPct;
                bestDistSq = distSq;
            }
        }

        return best != null ? new List<UnitData> { best } : new List<UnitData>();
    }

    private static List<UnitData> ResolveUnitsInRadius(
        MatchState state,
        UnitData source,
        UnitAbilityState ability,
        int team
    )
    {
        float radius = ability.Radius > 0f ? ability.Radius : source.AttackRange;
        float radiusSq = radius * radius;
        var targets = new List<UnitData>();

        foreach (var candidate in state.GetAliveActiveUnitsForTeam(team))
        {
            float distSq = source.Position.DistanceSquaredTo(candidate.Position);
            if (distSq <= radiusSq)
                targets.Add(candidate);
        }

        targets.Sort((a, b) => a.UnitId.CompareTo(b.UnitId));
        return targets;
    }

    private static bool CanApplyToTarget(
        UnitData source,
        UnitData target,
        AbilityTargetAffinity affinity
    )
    {
        return affinity switch
        {
            AbilityTargetAffinity.Enemies => target.Team != source.Team,
            AbilityTargetAffinity.Allies => target.Team == source.Team,
            AbilityTargetAffinity.Both => true,
            _ => false,
        };
    }

    private static SpellAffinity ToSpellAffinity(AbilityTargetAffinity affinity)
    {
        return affinity switch
        {
            AbilityTargetAffinity.Allies => SpellAffinity.Allies,
            AbilityTargetAffinity.Both => SpellAffinity.Both,
            _ => SpellAffinity.Enemies,
        };
    }

    private static ProjectileImpactKind ResolveProjectileImpact(UnitAbilityEffectState effect)
    {
        return effect.EffectType == EffectType.Heal
            ? ProjectileImpactKind.Heal
            : ProjectileImpactKind.Damage;
    }

    private static List<UnitAbilityEffectState> ResolveEffects(UnitAbilityState ability)
    {
        return ability.Effects.Count > 0
            ? ability.Effects
            : new List<UnitAbilityEffectState> { ResolvePrimaryEffect(ability) };
    }

    private static UnitAbilityEffectState ResolvePrimaryEffect(UnitAbilityState ability)
    {
        if (ability.Effects.Count > 0)
            return ability.Effects[0];

        return new UnitAbilityEffectState
        {
            EffectType = ability.EffectType,
            Value = ability.Value,
            DurationSeconds = ability.DurationSeconds,
            Lifetime = ability.Lifetime,
            DamageType = DamageType.Magic,
        };
    }

    private static EffectApplicationSpec BuildEffectSpec(
        UnitData source,
        UnitAbilityState ability,
        UnitAbilityEffectState effect
    )
    {
        return new EffectApplicationSpec
        {
            EffectType = effect.EffectType,
            Value = effect.Value,
            Duration = EffectLifetimeResolver.ResolveDuration(effect.Lifetime, effect.DurationSeconds),
            Lifetime = effect.Lifetime,
            DamageType = effect.DamageType,
            StatusKind = effect.StatusKind,
            StatusTickInterval = effect.StatusTickInterval,
            StatusPotencyPerStack = effect.StatusPotencyPerStack,
            StatusMaxStacks = effect.StatusMaxStacks,
            TagRequirements = MergeRequirements(ability.TagRequirements, effect.TagRequirements),
            GrantedTags = new List<string>(effect.GrantedTags),
            StackPolicy = effect.StackPolicy,
            StackKey = effect.StackKey,
            CueId = ResolveCueId(ability, effect),
            Context = new EffectApplicationContext
            {
                SourceUnitId = source.UnitId,
                SourceTeam = source.Team,
                SourcePosition = source.Position,
                AbilityId = ability.AbilityId,
            },
        };
    }

    private static string ResolveCueId(UnitAbilityState ability, UnitAbilityEffectState effect)
    {
        if (!string.IsNullOrWhiteSpace(effect.CueId))
            return effect.CueId;
        if (!string.IsNullOrWhiteSpace(ability.CueId))
            return ability.CueId;
        return ability.AbilityId;
    }

    private static void LogAbilityActivation(
        MatchState state,
        UnitData source,
        UnitAbilityState ability,
        UnitData? contextTarget,
        List<UnitData> targets,
        List<UnitAbilityEffectState> effects,
        int appliedCount,
        IReadOnlyDictionary<int, UnitDebugSnapshot> before
    )
    {
        if (!Simulation.DebugAbilityLogsEnabled)
            return;

        Simulation.DebugAbilityLog(
            CombatDebugFormatter.FormatAbilityActivation(
                state,
                source,
                ability,
                contextTarget,
                targets,
                effects,
                appliedCount,
                before
            )
        );
    }

    private static IEnumerable<UnitData> BuildDebugSnapshotUnits(
        UnitData source,
        UnitData? contextTarget,
        List<UnitData> targets
    )
    {
        var seen = new HashSet<int>();
        if (seen.Add(source.UnitId))
            yield return source;
        if (contextTarget != null && seen.Add(contextTarget.UnitId))
            yield return contextTarget;
        foreach (var target in targets)
        {
            if (seen.Add(target.UnitId))
                yield return target;
        }
    }

    private static EffectTagRequirements MergeRequirements(
        EffectTagRequirements abilityRequirements,
        EffectTagRequirements effectRequirements
    )
    {
        var merged = abilityRequirements.DeepClone();
        merged.RequiredSourceTags.AddRange(effectRequirements.RequiredSourceTags);
        merged.BlockedSourceTags.AddRange(effectRequirements.BlockedSourceTags);
        merged.RequiredTargetTags.AddRange(effectRequirements.RequiredTargetTags);
        merged.BlockedTargetTags.AddRange(effectRequirements.BlockedTargetTags);
        return merged;
    }

    private static bool TryResolveProjectileData(
        UnitData source,
        UnitAbilityState ability,
        out Fateforged.Projectiles.ProjectileData projectileData
    )
    {
        projectileData = null!;
        var resolvedId = ability.ProjectileCatalogId;
        if (!resolvedId.HasValue)
            resolvedId = source.ProjectileCatalogId;
        if (!resolvedId.HasValue)
            return false;

        var resolved = ProjectileDefinitions.Get(resolvedId.Value);
        if (resolved == null)
            return false;

        projectileData = resolved;
        return true;
    }
}
