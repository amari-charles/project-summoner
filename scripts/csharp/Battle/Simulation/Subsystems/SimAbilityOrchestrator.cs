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
            return;
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
                targets[0],
                events
            ),
            _ => TryDeliverInstant(state, source, ability, targets, events),
        };
    }

    private static bool TryDeliverInstant(
        MatchState state,
        UnitData source,
        UnitAbilityState ability,
        List<UnitData> targets,
        List<SimEvent> events
    )
    {
        var effects = ResolveEffects(ability);
        int applied = 0;

        foreach (var effect in effects.Where(e => e.EffectType == EffectType.TransferHealth))
            applied += ApplyHealthRedistribution(source, targets, effect, events);

        foreach (var target in targets)
        {
            foreach (var effect in effects.Where(e => e.EffectType != EffectType.TransferHealth))
            {
                SimEffects.ApplyEffect(
                    state,
                    effect.EffectType,
                    effect.Value,
                    EffectLifetimeResolver.ResolveDuration(
                        effect.Lifetime,
                        effect.DurationSeconds
                    ),
                    effect.DamageType,
                    target,
                    source.UnitId,
                    source.Team,
                    events,
                    effect.StatusKind,
                    effect.StatusTickInterval,
                    effect.StatusPotencyPerStack,
                    effect.StatusMaxStacks
                );
                applied++;
            }
        }

        if (applied <= 0)
            return false;

        int? eventTarget = targets.Count == 1 ? targets[0].UnitId : null;
        events.Add(new AbilityActivatedEvent(source.UnitId, ability.AbilityId, eventTarget, source.Position));
        return true;
    }

    private static bool TryDeliverProjectile(
        MatchState state,
        UnitData source,
        UnitAbilityState ability,
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
