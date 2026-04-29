namespace Fateforged.Tests.Simulation;

using System.Collections.Generic;
using Fateforged.Simulation;
using Fateforged.Simulation.Combat;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class AttackSpeedModifierTest
{
    [TestCase]
    public void AttackSpeedModifier_Ally_IncreasesCadence()
    {
        var state = SimTestHelper.CreateBattleState();
        var events = new List<SimEvent>();
        var attacker = SimTestHelper.CreateMeleeUnit(state, 0, attackSpeed: 1f, x: 0f, z: 0f);
        var target = SimTestHelper.CreateMeleeUnit(state, 1, x: 1.2f, z: 0f);
        attacker.TargetUnitId = target.UnitId;

        attacker.ActiveBuffs.Add(
            new ActiveBuff
            {
                BuffId = state.NextBuffId(),
                EffectType = EffectType.AttackSpeedModifier,
                Value = 0.5f,
                Duration = 1f,
                Lifetime = EffectLifetime.Timed(1f),
                SourceUnitId = attacker.UnitId,
                SourceTeam = attacker.Team,
            }
        );

        SimBehavior.TickBehavior(
            attacker,
            state,
            global::Fateforged.Simulation.Simulation.FixedDeltaSeconds,
            events
        );

        AssertThat(attacker.AttackCooldown).IsEqual(1f / 1.5f);
    }

    [TestCase]
    public void AttackSpeedModifier_Enemy_DecreasesCadence()
    {
        var state = SimTestHelper.CreateBattleState();
        var events = new List<SimEvent>();
        var attacker = SimTestHelper.CreateMeleeUnit(state, 0, attackSpeed: 1f, x: 0f, z: 0f);
        var target = SimTestHelper.CreateMeleeUnit(state, 1, x: 1.2f, z: 0f);
        attacker.TargetUnitId = target.UnitId;

        attacker.ActiveBuffs.Add(
            new ActiveBuff
            {
                BuffId = state.NextBuffId(),
                EffectType = EffectType.AttackSpeedModifier,
                Value = -0.4f,
                Duration = 1f,
                Lifetime = EffectLifetime.Timed(1f),
                SourceUnitId = target.UnitId,
                SourceTeam = target.Team,
            }
        );

        SimBehavior.TickBehavior(
            attacker,
            state,
            global::Fateforged.Simulation.Simulation.FixedDeltaSeconds,
            events
        );

        AssertThat(attacker.AttackCooldown).IsEqual(1f / 0.6f);
    }
}
