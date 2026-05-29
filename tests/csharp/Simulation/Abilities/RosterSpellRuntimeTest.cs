namespace Fateforged.Tests.Simulation.Abilities;

using System.Linq;
using Fateforged.Cards;
using Fateforged.Simulation;
using Fateforged.Simulation.Combat;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Subsystems;
using Fateforged.Tests.Simulation;
using Fateforged.Units;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class RosterSpellRuntimeTest
{
    [TestCase]
    public void FireSpells_ApplyBurnConsumeBurnAndOverheatAllies()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);
        var ally = SimTestHelper.CreateMeleeUnit(state, 0, x: 0f, hp: 100f);
        var enemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 0f, hp: 100f);

        CastSpell(state, sim, CardDefinitions.FireAreaBurn, SimVector3.Zero);

        AssertThat(enemy.ActiveBuffs.Any(b => b.StatusKind == StatusEffectKind.Burn)).IsTrue();

        float enemyHpAfterBurn = enemy.CurrentHp;
        CastSpell(state, sim, CardDefinitions.BurnCashout, SimVector3.Zero);

        AssertThat(enemy.ActiveBuffs.Any(b => b.StatusKind == StatusEffectKind.Burn)).IsFalse();
        AssertThat(enemy.CurrentHp).IsLess(enemyHpAfterBurn);

        float allyHpBeforeOverheat = ally.CurrentHp;
        CastSpell(state, sim, CardDefinitions.Overheat, SimVector3.Zero);

        AssertThat(ally.ActiveBuffs.Any(b => b.EffectType == EffectType.DamageBoost)).IsTrue();
        AssertThat(ally.ActiveBuffs.Any(b => b.EffectType == EffectType.AttackSpeedModifier)).IsTrue();

        Advance(sim, seconds: 5.2f);

        AssertThat(ally.CurrentHp).IsLess(allyHpBeforeOverheat);
    }

    [TestCase]
    public void WaterSpells_ShieldFlowAndWhirlpoolApplyEffects()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);
        var ally = SimTestHelper.CreateMeleeUnit(state, 0, x: 0f, hp: 100f);
        var enemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 2f, hp: 100f);
        float enemyHpBeforeWhirlpool = enemy.CurrentHp;

        CastSpell(state, sim, CardDefinitions.BubbleShield, SimVector3.Zero);
        CastSpell(state, sim, CardDefinitions.Flow, SimVector3.Zero);
        CastSpell(state, sim, CardDefinitions.Whirlpool, SimVector3.Zero);

        AssertThat(ally.ActiveBuffs.Any(b => b.EffectType == EffectType.Shield)).IsTrue();
        AssertThat(ally.ActiveBuffs.Any(b => b.EffectType == EffectType.EvasionModifier)).IsTrue();
        AssertThat(ally.ActiveBuffs.Any(b => b.EffectType == EffectType.DamageBoost)).IsTrue();
        AssertThat(enemy.KnockbackRemainingDistance).IsGreater(0f);
        AssertThat(enemy.KnockbackDirection.X).IsLess(0f);

        Advance(sim, seconds: 2.8f);

        AssertThat(enemy.CurrentHp).IsLess(enemyHpBeforeWhirlpool);
    }

    [TestCase]
    public void EarthSpells_DamageControlAndReviveWork()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);
        var ally = SimTestHelper.CreateMeleeUnit(state, 0, x: 0f, hp: 100f);
        var enemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 0f, hp: 150f);

        CastSpell(state, sim, CardDefinitions.Quake, SimVector3.Zero);

        AssertThat(enemy.CurrentHp).IsLess(150f);
        AssertThat(enemy.ActiveBuffs.Any(b => b.EffectType == EffectType.Stun)).IsTrue();

        CastSpell(state, sim, CardDefinitions.EarthenGrip, SimVector3.Zero, enemy.UnitId);

        AssertThat(enemy.ActiveBuffs.Any(b => b.EffectType == EffectType.Root)).IsTrue();

        CastSpell(state, sim, CardDefinitions.GravityWell, SimVector3.Zero);

        AssertThat(
                enemy.ActiveBuffs.Any(b =>
                    b.EffectType == EffectType.AttackSpeedModifier && b.Value < 0f
                )
            )
            .IsTrue();

        CastSpell(state, sim, CardDefinitions.ReformEarth, SimVector3.Zero);
        SimEffects.ApplyEffect(
            state,
            EffectType.Damage,
            500f,
            0f,
            DamageType.True,
            ally,
            MatchState.GetSummonerTargetId(0),
            Team.Player,
            []
        );

        AssertThat(ally.IsAlive).IsTrue();
        AssertThat(ally.CurrentHp).IsEqual(50f);
    }

    [TestCase]
    public void WindSpells_PushDebuffAndLineDamageWork()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);
        var enemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 2f, hp: 160f);
        var lineEnemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 5f, z: 0f, hp: 120f);
        var offLineEnemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 5f, z: 5f, hp: 120f);

        CastSpell(state, sim, CardDefinitions.Crosswind, SimVector3.Zero);

        AssertThat(
                enemy.ActiveBuffs.Any(b =>
                    b.EffectType == EffectType.RangedDamageModifier && b.Value < 0f
                )
            )
            .IsTrue();

        CastSpell(state, sim, CardDefinitions.AirBullet, SimVector3.Zero, enemy.UnitId);

        AssertThat(enemy.CurrentHp).IsLess(160f);
        AssertThat(enemy.KnockbackRemainingDistance).IsGreater(0f);

        float lineHpBefore = lineEnemy.CurrentHp;
        float offLineHpBefore = offLineEnemy.CurrentHp;
        CastSpell(state, sim, CardDefinitions.WindShear, new SimVector3(10f, 0f, 0f));

        AssertThat(lineEnemy.CurrentHp).IsLess(lineHpBefore);
        AssertThat(offLineEnemy.CurrentHp).IsEqual(offLineHpBefore);
    }

    private static void CastSpell(
        MatchState state,
        Fateforged.Simulation.Simulation sim,
        CardDefinition cardDef,
        SimVector3 position,
        int? targetUnitId = null
    )
    {
        var cardId = (string)cardDef.Id;
        state.CardDataMap[cardId] = SimCardData.FromCardDefinition(cardDef);
        state.Summoners[0].Hand.Clear();
        state.Summoners[0].Hand.Add(cardId);
        state.Summoners[0].Mana = 99f;

        state.PendingCommandBuffer.Add(new PlayCardCommand(0, 0, position) { TargetUnitId = targetUnitId });
        sim.Tick(Simulation.FixedDeltaSeconds);
    }

    private static void Advance(Fateforged.Simulation.Simulation sim, float seconds)
    {
        int ticks = (int)(seconds / Simulation.FixedDeltaSeconds) + 1;
        for (int i = 0; i < ticks; i++)
            sim.Tick(Simulation.FixedDeltaSeconds);
    }
}
