namespace Fateforged.Tests.Simulation.Abilities;

using System.Collections.Generic;
using System.Linq;
using Fateforged.Simulation;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Subsystems;
using Fateforged.Tests.Simulation;
using Fateforged.Units;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class AbilityTargetedKnockbackTest
{
    [TestCase]
    public void TargetedKnockback_OnHitAppliesEvenWithShortAbilityRange()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);
        var events = new List<SimEvent>();

        var caster = SimTestHelper.CreateRangedUnit(
            state,
            0,
            x: 0f,
            z: 0f,
            damage: 0f,
            attackSpeed: 0f,
            moveSpeed: 0f
        );
        caster.Abilities.Add(
            new UnitAbilityState
            {
                AbilityId = "targeted_knockback",
                Trigger = UnitAbilityTrigger.OnHit,
                Targeting = UnitAbilityTargeting.HitTarget,
                Delivery = UnitAbilityDelivery.Instant,
                CooldownSeconds = 3f,
                Range = 0.5f,
                Effects =
                [
                    new UnitAbilityEffectState
                    {
                        EffectType = EffectType.Knockback,
                        Value = 3f,
                    },
                ],
            }
        );

        var target = SimTestHelper.CreateMeleeUnit(
            state,
            1,
            x: 10f,
            z: 0f,
            hp: 100f,
            damage: 0f,
            attackSpeed: 0f,
            moveSpeed: 0f
        );
        float before = target.Position.X;

        SimAbilityOrchestrator.TryActivateOnHitEffects(state, caster, target, events);
        for (int i = 0; i < 10; i++)
            sim.Tick(Simulation.FixedDeltaSeconds);

        AssertThat(target.Position.X).IsGreater(before);
        AssertThat(
                events.OfType<AbilityActivatedEvent>().Any(e =>
                    e.SourceUnitId == caster.UnitId && e.TargetUnitId == target.UnitId
                )
            )
            .IsTrue();
    }

    [TestCase]
    public void TargetedKnockback_OnHitHasCooldown_AndDisplacesOverTime()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);
        var events = new List<SimEvent>();

        var caster = SimTestHelper.CreateRangedUnit(
            state,
            0,
            x: 0f,
            z: 0f,
            damage: 0f,
            attackSpeed: 0f,
            moveSpeed: 0f
        );
        caster.Abilities.Add(
            new UnitAbilityState
            {
                AbilityId = "targeted_knockback",
                Trigger = UnitAbilityTrigger.OnHit,
                Targeting = UnitAbilityTargeting.HitTarget,
                Delivery = UnitAbilityDelivery.Instant,
                CooldownSeconds = 3f,
                Range = 12f,
                Effects =
                [
                    new UnitAbilityEffectState
                    {
                        EffectType = EffectType.Knockback,
                        Value = 3f,
                    },
                ],
            }
        );

        var target = SimTestHelper.CreateMeleeUnit(
            state,
            1,
            x: 4f,
            z: 0f,
            hp: 100f,
            damage: 0f,
            attackSpeed: 0f,
            moveSpeed: 0f
        );

        float casterBefore = caster.Position.X;
        float targetBefore = target.Position.X;

        SimAbilityOrchestrator.TryActivateOnHitEffects(state, caster, target, events);
        float afterApplySameFrame = target.Position.X;
        SimAbilityOrchestrator.TryActivateOnHitEffects(state, caster, target, events); // cooldown blocks
        sim.Tick(Simulation.FixedDeltaSeconds);
        float afterFirstTick = target.Position.X;

        AssertThat(caster.Position.X).IsEqual(casterBefore);
        AssertThat(afterApplySameFrame).IsEqual(targetBefore); // no teleport
        AssertThat(afterFirstTick).IsGreater(targetBefore); // starts moving on tick
        AssertThat(afterFirstTick).IsLess(targetBefore + 3f); // displacement is gradual
        AssertThat(
                events.OfType<AbilityActivatedEvent>().Count(e =>
                    e.SourceUnitId == caster.UnitId && e.TargetUnitId == target.UnitId
                )
            )
            .IsEqual(1);
    }
}
