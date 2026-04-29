namespace Fateforged.Tests.Simulation.Abilities;

using System.Collections.Generic;
using Fateforged.Simulation;
using Fateforged.Simulation.Effects;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Subsystems;
using Fateforged.Tests.Simulation;
using Fateforged.Units;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class AbilityPassiveSelfEffectTest
{
    [TestCase]
    public void ApplySelfEffect_Evasion_Persistent()
    {
        var state = SimTestHelper.CreateBattleState();
        var events = new List<SimEvent>();
        var unit = SimTestHelper.CreateMeleeUnit(state, 0);

        unit.Abilities.Add(
            new UnitAbilityState
            {
                AbilityId = "evasion_passive",
                Kind = UnitAbilityKind.ApplySelfEffect,
                CooldownSeconds = 2f,
                EffectType = EffectType.EvasionModifier,
                Value = 0.2f,
                Lifetime = EffectLifetime.Persistent(),
                DurationSeconds = -1f,
            }
        );

        SimAbilityOrchestrator.Tick(state, 0.1f, events);
        AssertThat(unit.ActiveBuffs.Count).IsEqual(1);
        AssertThat(unit.ActiveBuffs[0].Lifetime.IsPersistent).IsTrue();
        AssertThat(unit.ActiveBuffs[0].Duration).IsEqual(-1f);
        AssertThat(EffectStatResolver.GetEffectiveEvasion(unit)).IsEqual(0.2f);
    }

    [TestCase]
    public void ApplySelfEffect_FlatReduction_Persistent()
    {
        var state = SimTestHelper.CreateBattleState();
        var events = new List<SimEvent>();
        var unit = SimTestHelper.CreateMeleeUnit(state, 0);

        unit.Abilities.Add(
            new UnitAbilityState
            {
                AbilityId = "flat_reduction_passive",
                Kind = UnitAbilityKind.ApplySelfEffect,
                CooldownSeconds = 2f,
                EffectType = EffectType.FlatDamageReduction,
                Value = 3f,
                Lifetime = EffectLifetime.Persistent(),
                DurationSeconds = -1f,
            }
        );

        SimAbilityOrchestrator.Tick(state, 0.1f, events);
        SimAbilityOrchestrator.Tick(state, 2.1f, events);

        AssertThat(unit.ActiveBuffs.Count).IsEqual(1);
        AssertThat(unit.ActiveBuffs[0].EffectType).IsEqual(EffectType.FlatDamageReduction);
        AssertThat(unit.ActiveBuffs[0].Value).IsEqual(3f);
    }
}
