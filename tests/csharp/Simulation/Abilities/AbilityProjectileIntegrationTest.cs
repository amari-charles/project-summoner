namespace Fateforged.Tests.Simulation.Abilities;

using System.Collections.Generic;
using System.Linq;
using Fateforged.Projectiles;
using Fateforged.Simulation;
using Fateforged.Simulation.Combat;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Effects;
using Fateforged.Simulation.Enums;
using Fateforged.Tests.Simulation;
using Fateforged.Units;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class AbilityProjectileIntegrationTest
{
    [TestCase]
    public void RockThrower_ArcProjectile_LandsAfterFlight()
    {
        var state = SimTestHelper.CreateBattleState();
        var events = new List<SimEvent>();

        var attacker = SimTestHelper.CreateRangedUnit(
            state,
            0,
            x: 0f,
            z: 0f,
            damage: 18f,
            attackSpeed: 1f,
            attackRange: 24f,
            projectileDelay: 0f,
            catalogId: "earth_rock_thrower"
        );
        attacker.ProjectileCatalogId = (string)ProjectileIds.Rock;
        attacker.Engagement.TargetUnitId = null;

        var target = SimTestHelper.CreateMeleeUnit(state, 1, x: 16f, z: 0f, hp: 140f);
        attacker.Engagement.TargetUnitId = target.UnitId;

        SimBehavior.TickBehavior(attacker, state, Simulation.FixedDeltaSeconds, events);
        SimBehavior.ResolvePendingAttackCommit(attacker, state, events);
        AssertThat(state.Projectiles.Count).IsEqual(1);
        var projectile = state.Projectiles.Values.First();
        AssertThat(projectile.MovementType).IsEqual(ProjectileMovementType.Arc);

        float hpAfterAttackFrame = target.CurrentHp;
        SimProjectile.TickAll(state, Simulation.FixedDeltaSeconds, events);
        AssertThat(target.CurrentHp).IsEqual(hpAfterAttackFrame);

        for (int i = 0; i < 360 && state.Projectiles.Count > 0; i++)
            SimProjectile.TickAll(state, Simulation.FixedDeltaSeconds, events);

        AssertThat(target.CurrentHp).IsLess(hpAfterAttackFrame);
    }

    [TestCase]
    public void LegacyMeleeAttack_NoAbilityLoadout_StillDamagesTarget()
    {
        var state = SimTestHelper.CreateBattleState();
        var events = new List<SimEvent>();
        var attacker = SimTestHelper.CreateMeleeUnit(state, 0, x: 0f, z: 0f, damage: 12f, attackSpeed: 1f, attackRange: 3f);
        var target = SimTestHelper.CreateMeleeUnit(state, 1, x: 1f, z: 0f, hp: 100f);
        attacker.Engagement.TargetUnitId = target.UnitId;

        float before = target.CurrentHp;
        SimBehavior.TickBehavior(attacker, state, Simulation.FixedDeltaSeconds, events);
        SimBehavior.ResolvePendingAttackCommit(attacker, state, events);
        AssertThat(target.CurrentHp).IsLess(before);
    }

    [TestCase]
    public void ProjectileImpact_RoutesDamageThroughEffectSpecAndCombatTriggers()
    {
        var state = SimTestHelper.CreateBattleState();
        var events = new List<SimEvent>();
        var attacker = SimTestHelper.CreateRangedUnit(state, 0, x: 0f, z: 0f, damage: 0f);
        attacker.AttackType = DamageType.True;
        attacker.CurrentHp = 50f;
        attacker.Abilities.Add(
            new UnitAbilityState
            {
                AbilityId = "projectile_on_hit_heal",
                Trigger = UnitAbilityTrigger.OnHit,
                Targeting = UnitAbilityTargeting.Self,
                Delivery = UnitAbilityDelivery.Instant,
                Effects =
                [
                    new UnitAbilityEffectState
                    {
                        EffectType = EffectType.Heal,
                        Value = 7f,
                    },
                ],
            }
        );

        var target = SimTestHelper.CreateMeleeUnit(state, 1, x: 6f, z: 0f, hp: 100f);
        target.Abilities.Add(
            new UnitAbilityState
            {
                AbilityId = "projectile_on_damaged_heal",
                Trigger = UnitAbilityTrigger.OnDamaged,
                Targeting = UnitAbilityTargeting.Self,
                Delivery = UnitAbilityDelivery.Instant,
                Effects =
                [
                    new UnitAbilityEffectState
                    {
                        EffectType = EffectType.Heal,
                        Value = 3f,
                    },
                ],
            }
        );

        SimProjectile.ResolveInstantLine(
            state,
            attacker.UnitId,
            target.UnitId,
            attacker.Team,
            damage: 20f,
            sourceElementId: attacker.ElementId,
            startPos: attacker.Position,
            endPos: target.Position,
            hitRadius: 0.5f,
            pierceCount: 0,
            aoeRadius: 0f,
            hitSpace: ProjectileHitSpace.GroundCylinder,
            projectileCatalogId: (string)ProjectileIds.Fireball,
            targetAffinity: AbilityTargetAffinity.Enemies,
            impactKind: ProjectileImpactKind.Damage,
            statusKind: StatusEffectKind.None,
            statusDuration: 0f,
            statusTickInterval: 0f,
            statusPotencyPerStack: 0f,
            statusMaxStacks: 1,
            beamDurationSeconds: 0.1f,
            events
        );

        AssertThat(attacker.CurrentHp).IsEqual(57f);
        AssertThat(target.CurrentHp).IsEqual(83f);
        AssertThat(events.OfType<EffectCueEvent>().Any(e => e.Phase == EffectCuePhase.Executed))
            .IsTrue();
    }

    [TestCase]
    public void ProjectileAoE_UsesEffectSpecButSkipsRecipientOnDamagedTriggers()
    {
        var state = SimTestHelper.CreateBattleState();
        var events = new List<SimEvent>();
        var attacker = SimTestHelper.CreateRangedUnit(state, 0, x: 0f, z: 0f, damage: 0f);
        attacker.AttackType = DamageType.True;
        var primary = SimTestHelper.CreateMeleeUnit(state, 1, x: 6f, z: 0f, hp: 100f);
        var splashTarget = SimTestHelper.CreateMeleeUnit(state, 1, x: 6.5f, z: 0.5f, hp: 100f);
        splashTarget.Abilities.Add(
            new UnitAbilityState
            {
                AbilityId = "splash_on_damaged_heal",
                Trigger = UnitAbilityTrigger.OnDamaged,
                Targeting = UnitAbilityTargeting.Self,
                Delivery = UnitAbilityDelivery.Instant,
                Effects =
                [
                    new UnitAbilityEffectState
                    {
                        EffectType = EffectType.Heal,
                        Value = 50f,
                    },
                ],
            }
        );

        SimProjectile.ResolveInstantLine(
            state,
            attacker.UnitId,
            primary.UnitId,
            attacker.Team,
            damage: 20f,
            sourceElementId: attacker.ElementId,
            startPos: attacker.Position,
            endPos: primary.Position,
            hitRadius: 0.5f,
            pierceCount: 0,
            aoeRadius: 2f,
            hitSpace: ProjectileHitSpace.GroundCylinder,
            projectileCatalogId: (string)ProjectileIds.Fireball,
            targetAffinity: AbilityTargetAffinity.Enemies,
            impactKind: ProjectileImpactKind.Damage,
            statusKind: StatusEffectKind.None,
            statusDuration: 0f,
            statusTickInterval: 0f,
            statusPotencyPerStack: 0f,
            statusMaxStacks: 1,
            beamDurationSeconds: 0.1f,
            events
        );

        AssertThat(primary.CurrentHp).IsEqual(80f);
        AssertThat(splashTarget.CurrentHp).IsEqual(80f);
        AssertThat(
                events.OfType<AbilityActivatedEvent>().Any(e =>
                    e.AbilityId == "splash_on_damaged_heal"
                )
            )
            .IsFalse();
    }
}
