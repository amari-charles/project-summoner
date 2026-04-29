namespace Fateforged.Tests.Simulation.Abilities;

using System.Collections.Generic;
using System.Linq;
using Fateforged.Projectiles;
using Fateforged.Simulation;
using Fateforged.Simulation.Combat;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Subsystems;
using Fateforged.Tests.Simulation;
using Fateforged.Units;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class AbilityAllyTargetingTest
{
    [TestCase]
    public void HealerProjectile_TargetsLowestHpAlly_AndHealsOnHit()
    {
        var state = SimTestHelper.CreateBattleState();
        var events = new List<SimEvent>();

        var healer = SimTestHelper.CreateRangedUnit(state, 0, x: 0f, z: 0f, damage: 0f, attackSpeed: 0f);
        healer.Abilities.Add(
            new UnitAbilityState
            {
                AbilityId = "healer_bullet",
                Kind = UnitAbilityKind.HealerProjectile,
                CooldownSeconds = 1f,
                Range = 20f,
                Value = 12f,
                ProjectileCatalogId = (string)ProjectileIds.HealingBolt,
                TargetAffinity = AbilityTargetAffinity.Allies,
            }
        );

        var lowAlly = SimTestHelper.CreateMeleeUnit(state, 0, x: 5f, hp: 100f);
        lowAlly.CurrentHp = 30f;
        var highAlly = SimTestHelper.CreateMeleeUnit(state, 0, x: 6f, hp: 100f);
        highAlly.CurrentHp = 70f;

        SimAbilityOrchestrator.Tick(state, Simulation.FixedDeltaSeconds, events);

        AssertThat(state.Projectiles.Count).IsEqual(1);
        var projectile = state.Projectiles.Values.First();
        AssertThat(projectile.TargetUnitId).IsEqual(lowAlly.UnitId);
        AssertThat(projectile.TargetAffinity).IsEqual(AbilityTargetAffinity.Allies);
        AssertThat(projectile.ImpactKind).IsEqual(ProjectileImpactKind.Heal);
        AssertThat(events.OfType<AbilityActivatedEvent>().Any()).IsTrue();

        float beforeLowHp = lowAlly.CurrentHp;
        for (int i = 0; i < 240 && state.Projectiles.Count > 0; i++)
            SimProjectile.TickAll(state, Simulation.FixedDeltaSeconds, events);

        AssertThat(lowAlly.CurrentHp).IsGreater(beforeLowHp);
        AssertThat(highAlly.CurrentHp).IsEqual(70f);
    }

    [TestCase]
    public void CleansePulse_CleansesAndHealsNearbyAllies()
    {
        var state = SimTestHelper.CreateBattleState();
        var events = new List<SimEvent>();

        var support = SimTestHelper.CreateRangedUnit(state, 0, x: 0f, z: 0f, damage: 0f, attackSpeed: 0f);
        support.Abilities.Add(
            new UnitAbilityState
            {
                AbilityId = "cleanse_pulse",
                Kind = UnitAbilityKind.CleansePulse,
                CooldownSeconds = 2f,
                Radius = 7f,
                Value = 12f,
                TargetAffinity = AbilityTargetAffinity.Allies,
            }
        );

        var ally = SimTestHelper.CreateMeleeUnit(state, 0, x: 4f, z: 0f, hp: 100f);
        ally.CurrentHp = 50f;
        ally.ActiveBuffs.Add(
            new ActiveBuff
            {
                BuffId = 7,
                EffectType = EffectType.Slow,
                Value = 0.3f,
                Duration = 3f,
            }
        );
        ally.ActiveBuffs.Add(
            new ActiveBuff
            {
                BuffId = 8,
                EffectType = EffectType.Damage,
                Value = 3f,
                Duration = 3f,
                TickInterval = 1f,
                TickTimer = 1f,
                StatusKind = StatusEffectKind.Burn,
            }
        );
        ally.Engagement.ForcedTargetUnitId = 123;
        ally.Engagement.ForcedTargetTimer = 2f;

        var distantAlly = SimTestHelper.CreateMeleeUnit(state, 0, x: 20f, z: 0f, hp: 100f);
        distantAlly.ActiveBuffs.Add(
            new ActiveBuff
            {
                BuffId = 9,
                EffectType = EffectType.Slow,
                Value = 0.3f,
                Duration = 4f,
            }
        );

        SimAbilityOrchestrator.Tick(state, Simulation.FixedDeltaSeconds, events);

        AssertThat(ally.CurrentHp).IsGreater(50f);
        AssertThat(ally.ActiveBuffs.Any(b => b.EffectType == EffectType.Slow)).IsFalse();
        AssertThat(ally.ActiveBuffs.Any(b => b.StatusKind == StatusEffectKind.Burn)).IsFalse();
        AssertThat(ally.Engagement.ForcedTargetUnitId.HasValue).IsFalse();
        AssertThat(ally.Engagement.ForcedTargetTimer).IsEqual(0f);

        AssertThat(distantAlly.ActiveBuffs.Any(b => b.EffectType == EffectType.Slow)).IsTrue();
        AssertThat(events.OfType<AbilityActivatedEvent>().Any()).IsTrue();
    }
}
