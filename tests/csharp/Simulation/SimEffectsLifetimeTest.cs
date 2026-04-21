namespace Fateforged.Tests.Simulation;

using System.Collections.Generic;
using Fateforged.Simulation;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Subsystems;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class SimEffectsLifetimeTest
{
    [TestCase]
    public void TimedBuff_Expires_ByTypedLifetime()
    {
        var state = SimTestHelper.CreateBattleState();
        var events = new List<SimEvent>();
        var unit = SimTestHelper.CreateMeleeUnit(state, 0);

        unit.ActiveBuffs.Add(
            new ActiveBuff
            {
                BuffId = 1,
                EffectType = EffectType.AttackSpeedModifier,
                Value = 0.25f,
                Duration = 0f,
                Lifetime = EffectLifetime.Timed(0.05f),
                SourceUnitId = unit.UnitId,
                SourceTeam = unit.Team,
            }
        );

        SimEffects.TickBuffs(state, 0.1f, events);

        AssertThat(unit.ActiveBuffs.Count).IsEqual(0);
    }

    [TestCase]
    public void PersistentBuff_DoesNotExpire_ByTypedLifetime()
    {
        var state = SimTestHelper.CreateBattleState();
        var events = new List<SimEvent>();
        var unit = SimTestHelper.CreateMeleeUnit(state, 0);

        unit.ActiveBuffs.Add(
            new ActiveBuff
            {
                BuffId = 1,
                EffectType = EffectType.FlatDamageReduction,
                Value = 4f,
                Duration = -1f,
                Lifetime = EffectLifetime.Persistent(),
                SourceUnitId = unit.UnitId,
                SourceTeam = unit.Team,
            }
        );

        SimEffects.TickBuffs(state, 2.0f, events);

        AssertThat(unit.ActiveBuffs.Count).IsEqual(1);
        AssertThat(unit.ActiveBuffs[0].Lifetime.IsPersistent).IsTrue();
    }

    [TestCase]
    public void TriggerPayload_UsesTypedLifetime()
    {
        var state = SimTestHelper.CreateBattleState();
        var events = new List<SimEvent>();
        var source = SimTestHelper.CreateMeleeUnit(state, 0);
        var target = SimTestHelper.CreateMeleeUnit(state, 1, x: 1.5f);

        source.Triggers.Add(
            new TriggerConfig
            {
                TriggerType = TriggerType.OnHit,
                EffectType = EffectType.AttackSpeedModifier,
                Value = -0.3f,
                Duration = 0f,
                Lifetime = EffectLifetime.Persistent(),
                DamageType = DamageType.Magic,
            }
        );

        SimEffects.FireTriggers(state, source, TriggerType.OnHit, target, events);

        AssertThat(target.ActiveBuffs.Count).IsEqual(1);
        AssertThat(target.ActiveBuffs[0].Lifetime.IsPersistent).IsTrue();
        AssertThat(target.ActiveBuffs[0].Duration).IsEqual(-1f);
    }

    [TestCase]
    public void DelayedPayload_UsesTypedLifetime()
    {
        var state = SimTestHelper.CreateBattleState();
        var events = new List<SimEvent>();
        var source = SimTestHelper.CreateMeleeUnit(state, 0, x: 0f, z: 0f);
        var target = SimTestHelper.CreateMeleeUnit(state, 1, x: 0.5f, z: 0f);

        state.DelayedEffects.Add(
            new DelayedEffect
            {
                Timer = 0.01f,
                EffectType = EffectType.FlatDamageReduction,
                Value = 5f,
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

        SimEffects.TickDelayedEffects(state, 0.02f, events);

        AssertThat(target.ActiveBuffs.Count).IsEqual(1);
        AssertThat(target.ActiveBuffs[0].Lifetime.IsPersistent).IsTrue();
        AssertThat(target.ActiveBuffs[0].Duration).IsEqual(-1f);
    }
}
