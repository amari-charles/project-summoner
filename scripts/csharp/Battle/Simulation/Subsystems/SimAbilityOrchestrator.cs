using System;
using Fateforged.Data.Projectiles;
using Fateforged.Simulation.Combat;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Units;

namespace Fateforged.Simulation.Subsystems;

/// <summary>
/// Simulation-owned ability runtime orchestrator.
/// Executes non-basic unit abilities deterministically from UnitData.Abilities.
/// </summary>
public static class SimAbilityOrchestrator
{
    public static void Tick(MatchState state, float fixedDelta, System.Collections.Generic.List<SimEvent> events)
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
        System.Collections.Generic.List<SimEvent> events
    )
    {
        if (ability.CooldownTimer > 0f)
            ability.CooldownTimer = MathF.Max(0f, ability.CooldownTimer - fixedDelta);

        if (ability.CooldownTimer > 0f)
            return;

        bool activated = ability.Kind switch
        {
            UnitAbilityKind.HealerProjectile => TryActivateHealerProjectile(
                state,
                source,
                ability,
                events
            ),
            UnitAbilityKind.TauntPulse => TryActivateTauntPulse(state, source, ability, events),
            UnitAbilityKind.CleansePulse => TryActivateCleansePulse(state, source, ability, events),
            _ => false,
        };

        if (activated)
            ability.CooldownTimer = MathF.Max(ability.CooldownSeconds, 0f);
    }

    private static bool TryActivateHealerProjectile(
        MatchState state,
        UnitData source,
        UnitAbilityState ability,
        System.Collections.Generic.List<SimEvent> events
    )
    {
        var target = ResolveHealerTarget(state, source, ability);
        if (target == null)
            return false;

        if (!TryResolveProjectileData(source, ability, out var projectileData))
            return false;

        var targetPos = target.Position;
        var startPos = source.Position;
        if (projectileData.SpawnAtTargetHeight)
            startPos = new SimVector3(startPos.X, targetPos.Y, startPos.Z);

        SimProjectile.Spawn(
            state,
            sourceUnitId: source.UnitId,
            targetUnitId: target.UnitId,
            team: source.Team,
            damage: ability.Value,
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
            impactKind: ProjectileImpactKind.Heal
        );

        events.Add(
            new AbilityActivatedEvent(source.UnitId, ability.AbilityId, target.UnitId, source.Position)
        );
        return true;
    }

    private static UnitData? ResolveHealerTarget(
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

        return best;
    }

    private static bool TryActivateTauntPulse(
        MatchState state,
        UnitData source,
        UnitAbilityState ability,
        System.Collections.Generic.List<SimEvent> events
    )
    {
        int enemyTeam = MatchState.GetEnemyTeam((int)source.Team);
        float radius = ability.Radius > 0f ? ability.Radius : source.AttackRange;
        float radiusSq = radius * radius;
        int applied = 0;

        foreach (var candidate in state.GetAliveActiveUnitsForTeam(enemyTeam))
        {
            float distSq = source.Position.DistanceSquaredTo(candidate.Position);
            if (distSq > radiusSq)
                continue;
            if (!ShouldApplySoftTaunt(candidate, source.UnitId))
                continue;

            candidate.ForcedTargetUnitId = source.UnitId;
            candidate.ForcedTargetTimer = MathF.Max(candidate.ForcedTargetTimer, ability.DurationSeconds);
            events.Add(
                new StatusAppliedEvent(
                    source.UnitId,
                    candidate.UnitId,
                    StatusEffectKind.Taunt,
                    1,
                    ability.DurationSeconds
                )
            );
            applied++;
        }

        if (applied <= 0)
            return false;

        events.Add(new AbilityActivatedEvent(source.UnitId, ability.AbilityId, null, source.Position));
        return true;
    }

    private static bool TryActivateCleansePulse(
        MatchState state,
        UnitData source,
        UnitAbilityState ability,
        System.Collections.Generic.List<SimEvent> events
    )
    {
        float radius = ability.Radius > 0f ? ability.Radius : source.AttackRange;
        float radiusSq = radius * radius;
        int applied = 0;

        foreach (var ally in state.GetAliveActiveUnitsForTeam((int)source.Team))
        {
            float distSq = source.Position.DistanceSquaredTo(ally.Position);
            if (distSq > radiusSq)
                continue;

            SimEffects.ApplyEffect(
                state,
                EffectType.Cleanse,
                0f,
                0f,
                DamageType.Magic,
                ally,
                source.UnitId,
                source.Team,
                events
            );

            if (ability.Value > 0f)
            {
                SimEffects.ApplyEffect(
                    state,
                    EffectType.Heal,
                    ability.Value,
                    0f,
                    DamageType.Magic,
                    ally,
                    source.UnitId,
                    source.Team,
                    events
                );
            }
            applied++;
        }

        if (applied <= 0)
            return false;

        events.Add(new AbilityActivatedEvent(source.UnitId, ability.AbilityId, null, source.Position));
        return true;
    }

    private static bool ShouldApplySoftTaunt(UnitData candidate, int taunterUnitId)
    {
        if (candidate.ForcedTargetTimer <= 0f)
            return true;
        if (!candidate.ForcedTargetUnitId.HasValue)
            return true;
        return candidate.ForcedTargetUnitId.Value == taunterUnitId;
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
