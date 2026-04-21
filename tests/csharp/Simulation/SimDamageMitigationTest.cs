namespace Fateforged.Tests.Simulation;

using System.Collections.Generic;
using Fateforged.Simulation;
using Fateforged.Simulation.Combat;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Subsystems;
using Fateforged.Units;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class SimDamageMitigationTest
{
    [TestCase]
    public void FlatDamageReduction_DirectHit_Clamped()
    {
        var state = SimTestHelper.CreateBattleState();
        var attacker = SimTestHelper.CreateMeleeUnit(state, 0);
        var target = SimTestHelper.CreateMeleeUnit(state, 1, hp: 100f);
        target.ActiveBuffs.Add(
            new ActiveBuff
            {
                BuffId = state.NextBuffId(),
                EffectType = EffectType.FlatDamageReduction,
                Value = 4f,
                Duration = -1f,
                Lifetime = EffectLifetime.Persistent(),
                SourceUnitId = target.UnitId,
                SourceTeam = target.Team,
            }
        );

        var (damage, _, wasEvaded) = SimDamage.Calculate(
            baseDamage: 10f,
            damageType: DamageType.Physical,
            attacker: attacker,
            target: target,
            attackerSummoner: null,
            targetSummoner: null,
            rng: null
        );

        AssertThat(wasEvaded).IsFalse();
        AssertThat(damage).IsEqual(6f);

        var (clamped, _, _) = SimDamage.Calculate(
            baseDamage: 3f,
            damageType: DamageType.Physical,
            attacker: attacker,
            target: target,
            attackerSummoner: null,
            targetSummoner: null,
            rng: null
        );
        AssertThat(clamped).IsEqual(0f);
    }

    [TestCase]
    public void FlatDamageReduction_PeriodicHit_Clamped()
    {
        var state = SimTestHelper.CreateBattleState();
        var events = new List<SimEvent>();
        var target = SimTestHelper.CreateMeleeUnit(state, 1, hp: 100f);
        float hpBefore = target.CurrentHp;

        target.ActiveBuffs.Add(
            new ActiveBuff
            {
                BuffId = state.NextBuffId(),
                EffectType = EffectType.FlatDamageReduction,
                Value = 3f,
                Duration = -1f,
                Lifetime = EffectLifetime.Persistent(),
                SourceUnitId = target.UnitId,
                SourceTeam = target.Team,
            }
        );

        target.ActiveBuffs.Add(
            new ActiveBuff
            {
                BuffId = state.NextBuffId(),
                EffectType = EffectType.Damage,
                Value = 5f,
                Duration = 1f,
                Lifetime = EffectLifetime.Timed(1f),
                TickInterval = 0.1f,
                TickTimer = 0.01f,
                SourceUnitId = 999,
                SourceTeam = Team.Player,
            }
        );

        SimEffects.TickBuffs(state, 0.02f, events);

        AssertThat(target.CurrentHp).IsEqual(hpBefore - 2f);
    }
}
