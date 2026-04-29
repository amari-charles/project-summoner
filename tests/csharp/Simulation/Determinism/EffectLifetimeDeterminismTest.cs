namespace Fateforged.Tests.Simulation.Determinism;

using System.Collections.Generic;
using Fateforged.Simulation;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Subsystems;
using Fateforged.Tests.Simulation;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class EffectLifetimeDeterminismTest
{
    [TestCase]
    public void MixedLifetimeEvents_Deterministic()
    {
        var a = RunScenario(seed: 1001);
        var b = RunScenario(seed: 1001);

        AssertThat(a.finalHp).IsEqual(b.finalHp);
        AssertThat(a.buffCount).IsEqual(b.buffCount);
        AssertThat(a.damageEvents).IsEqual(b.damageEvents);
        AssertThat(a.buffAppliedEvents).IsEqual(b.buffAppliedEvents);
    }

    private static (float finalHp, int buffCount, int damageEvents, int buffAppliedEvents) RunScenario(
        uint seed
    )
    {
        var state = SimTestHelper.CreateBattleState(seed);
        var events = new List<SimEvent>();
        var source = SimTestHelper.CreateMeleeUnit(state, 0, x: 0f, z: 0f);
        var target = SimTestHelper.CreateMeleeUnit(state, 1, x: 0.5f, z: 0f, hp: 100f);

        // Timed periodic damage effect.
        target.ActiveBuffs.Add(
            new ActiveBuff
            {
                BuffId = state.NextBuffId(),
                EffectType = EffectType.Damage,
                Value = 5f,
                Duration = 0.5f,
                Lifetime = EffectLifetime.Timed(0.5f),
                TickInterval = 0.1f,
                TickTimer = 0.1f,
                SourceUnitId = source.UnitId,
                SourceTeam = source.Team,
            }
        );

        // Delayed persistent defensive effect.
        state.DelayedEffects.Add(
            new DelayedEffect
            {
                Timer = 0.2f,
                EffectType = EffectType.FlatDamageReduction,
                Value = 2f,
                Duration = 0f,
                Lifetime = EffectLifetime.Persistent(),
                DamageType = DamageType.Magic,
                AoeRadius = 2f,
                AreaShape = SpellAreaShape.Circle,
                Position = source.Position,
                SourceUnitId = source.UnitId,
                SourceTeam = source.Team,
                Affinity = SpellAffinity.Enemies,
                TargetingMode = SpellTargetingMode.Position,
            }
        );

        for (int i = 0; i < 40; i++)
        {
            SimEffects.TickBuffs(state, 1f / 60f, events);
            SimEffects.TickDelayedEffects(state, 1f / 60f, events);
        }

        int damageEvents = events.FindAll(e => e is UnitDamagedEvent).Count;
        int buffAppliedEvents = events.FindAll(e => e is BuffAppliedEvent).Count;
        return (target.CurrentHp, target.ActiveBuffs.Count, damageEvents, buffAppliedEvents);
    }
}
