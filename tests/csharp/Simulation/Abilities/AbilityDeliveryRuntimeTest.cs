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
public class AbilityDeliveryRuntimeTest
{
    [TestCase]
    public void DelayedDelivery_AppliesAfterDelay()
    {
        var state = SimTestHelper.CreateBattleState();
        var source = SimTestHelper.CreateMeleeUnit(state, 0, x: 0f, damage: 0f);
        var target = SimTestHelper.CreateMeleeUnit(state, 1, x: 2f, hp: 100f, damage: 0f);
        source.Abilities.Add(
            new UnitAbilityState
            {
                AbilityId = "delayed_strike",
                Trigger = UnitAbilityTrigger.OnHit,
                Targeting = UnitAbilityTargeting.HitTarget,
                Delivery = UnitAbilityDelivery.Delayed,
                DeliveryDelaySeconds = 0.2f,
                Effects =
                [
                    new UnitAbilityEffectState
                    {
                        EffectType = EffectType.Damage,
                        Value = 15f,
                        DamageType = DamageType.True,
                    },
                ],
            }
        );
        var events = new List<SimEvent>();

        SimAbilityOrchestrator.TryActivateOnHitEffects(state, source, target, events);

        AssertThat(target.CurrentHp).IsEqual(100f);
        AssertThat(state.DelayedEffects.Count).IsEqual(1);

        SimEffects.TickDelayedEffects(state, 0.1f, events);
        AssertThat(target.CurrentHp).IsEqual(100f);

        SimEffects.TickDelayedEffects(state, 0.1f, events);
        AssertThat(target.CurrentHp).IsEqual(85f);
    }

    [TestCase]
    public void RepeatedAreaDelivery_AppliesImmediateAndQueuedPulses()
    {
        var state = SimTestHelper.CreateBattleState();
        var source = SimTestHelper.CreateMeleeUnit(state, 0, x: 0f, damage: 0f);
        var target = SimTestHelper.CreateMeleeUnit(state, 1, x: 2f, hp: 100f, damage: 0f);
        source.Abilities.Add(
            new UnitAbilityState
            {
                AbilityId = "repeated_area",
                Trigger = UnitAbilityTrigger.Periodic,
                Targeting = UnitAbilityTargeting.EnemiesInRadius,
                Delivery = UnitAbilityDelivery.RepeatedArea,
                Radius = 5f,
                CooldownSeconds = 5f,
                RepeatCount = 2,
                RepeatIntervalSeconds = 0.1f,
                Effects =
                [
                    new UnitAbilityEffectState
                    {
                        EffectType = EffectType.Damage,
                        Value = 10f,
                        DamageType = DamageType.True,
                    },
                ],
            }
        );
        var events = new List<SimEvent>();

        SimAbilityOrchestrator.Tick(state, 0.1f, events);

        AssertThat(target.CurrentHp).IsEqual(90f);
        AssertThat(state.DelayedEffects.Count).IsEqual(2);

        SimEffects.TickDelayedEffects(state, 0.1f, events);
        SimEffects.TickDelayedEffects(state, 0.1f, events);

        AssertThat(target.CurrentHp).IsEqual(70f);
        AssertThat(
                events.OfType<AbilityActivatedEvent>().Count(e =>
                    e.SourceUnitId == source.UnitId && e.AbilityId == "repeated_area"
                )
            )
            .IsEqual(1);
    }
}
