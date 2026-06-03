namespace Fateforged.Tests.Simulation.Abilities;

using System;
using System.Linq;
using Fateforged.Cards;
using Fateforged.Simulation;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Effects;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Subsystems;
using Fateforged.Tests.Simulation;
using Fateforged.Units;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class RosterSpellDedicatedRuntimeTest
{
    [TestCase]
    public void Fireball_DamagesEnemiesInExplosionOnly()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);
        var ally = CreatePassiveMeleeUnit(state, 0, x: -5f, hp: 120f);
        var enemy = CreatePassiveMeleeUnit(state, 1, x: 5f, hp: 160f);
        var farEnemy = CreatePassiveMeleeUnit(state, 1, x: 30f, hp: 160f);

        CastSpell(state, sim, CardDefinitions.Fireball, SimVector3.Zero);
        Advance(sim, 2.5f);

        AssertThat(enemy.CurrentHp).IsLess(160f);
        AssertThat(farEnemy.CurrentHp).IsEqual(160f);
        AssertThat(ally.CurrentHp).IsEqual(120f);
    }

    [TestCase]
    public void FireAreaBurn_AppliesStackingBurnToEnemiesInAreaOnly()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);
        var ally = CreatePassiveMeleeUnit(state, 0, x: -3f, hp: 100f);
        var enemy = CreatePassiveMeleeUnit(state, 1, x: 3f, hp: 100f);

        CastSpell(state, sim, CardDefinitions.FireAreaBurn, SimVector3.Zero);
        CastSpell(state, sim, CardDefinitions.FireAreaBurn, SimVector3.Zero);

        var burn = enemy.ActiveBuffs.Single(b => b.StatusKind == StatusEffectKind.Burn);
        AssertThat(burn.StackCount).IsEqual(2);
        AssertThat(burn.Value).IsEqual(8f);
        AssertThat(ally.ActiveBuffs.Any(b => b.StatusKind == StatusEffectKind.Burn)).IsFalse();
    }

    [TestCase]
    public void BurnCashout_ConsumesBurnAndDealsRemainingBurnDamage()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);
        var enemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 0f, hp: 100f);

        CastSpell(state, sim, CardDefinitions.FireAreaBurn, SimVector3.Zero);
        float hpBeforeCashout = enemy.CurrentHp;
        CastSpell(state, sim, CardDefinitions.BurnCashout, SimVector3.Zero);

        AssertThat(enemy.ActiveBuffs.Any(b => b.StatusKind == StatusEffectKind.Burn)).IsFalse();
        AssertThat(enemy.CurrentHp).IsLess(hpBeforeCashout);
    }

    [TestCase]
    public void Overheat_BuffsAlliesThenDamagesThemAfterDelay()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);
        var ally = CreatePassiveMeleeUnit(state, 0, x: -3f, hp: 100f);
        var enemy = CreatePassiveMeleeUnit(state, 1, x: 3f, hp: 100f);

        CastSpell(state, sim, CardDefinitions.Overheat, SimVector3.Zero);

        AssertThat(ally.ActiveBuffs.Any(b => b.EffectType == EffectType.DamageBoost)).IsTrue();
        AssertThat(ally.ActiveBuffs.Any(b => b.EffectType == EffectType.AttackSpeedModifier)).IsTrue();
        AssertThat(enemy.ActiveBuffs.Any(b => b.EffectType == EffectType.DamageBoost)).IsFalse();

        Advance(sim, 5.2f);

        AssertThat(ally.CurrentHp).IsLess(100f);
    }

    [TestCase]
    public void IgnitionMark_BurnsTargetAndBurstsWhenMarkedTargetDies()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);
        var marked = SimTestHelper.CreateMeleeUnit(state, 1, x: 0f, hp: 120f);
        var nearbyEnemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 1f, hp: 120f);

        CastSpell(state, sim, CardDefinitions.IgnitionMark, SimVector3.Zero, marked.UnitId);

        AssertThat(marked.ActiveBuffs.Any(b => b.StatusKind == StatusEffectKind.Burn)).IsTrue();

        KillWithTrueDamage(state, marked, Team.Player);

        AssertThat(marked.IsAlive).IsFalse();
        AssertThat(nearbyEnemy.CurrentHp).IsLess(120f);
    }

    [TestCase]
    public void FlareShield_ShieldsAllyAndDamagesEnemiesWhenShieldBreaks()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);
        var ally = SimTestHelper.CreateMeleeUnit(state, 0, x: 0f, hp: 100f);
        var enemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 1f, hp: 120f);

        CastSpell(state, sim, CardDefinitions.FlareShield, SimVector3.Zero);

        AssertThat(ShieldAmount(ally)).IsEqual(35f);
        KillShieldWithTrueDamage(state, ally);

        AssertThat(enemy.CurrentHp).IsLess(120f);
    }

    [TestCase]
    public void Cleanse_HealsAndRemovesNegativeBuffsFromAlliesOnly()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);
        var ally = SimTestHelper.CreateMeleeUnit(state, 0, x: 0f, hp: 100f);
        ally.CurrentHp = 50f;
        AddSlow(ally);
        var enemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 0f, hp: 100f);
        AddSlow(enemy);

        CastSpell(state, sim, CardDefinitions.Cleanse, SimVector3.Zero);

        AssertThat(ally.CurrentHp).IsGreater(50f);
        AssertThat(ally.ActiveBuffs.Any(b => b.EffectType == EffectType.Slow)).IsFalse();
        AssertThat(enemy.ActiveBuffs.Any(b => b.EffectType == EffectType.Slow)).IsTrue();
    }

    [TestCase]
    public void WaterJet_DamagesAndKnocksBackSingleEnemyTarget()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);
        var target = SimTestHelper.CreateMeleeUnit(state, 1, x: 2f, hp: 120f);
        var bystander = SimTestHelper.CreateMeleeUnit(state, 1, x: 3f, hp: 120f);

        CastSpell(state, sim, CardDefinitions.WaterJet, SimVector3.Zero, target.UnitId);

        AssertThat(target.CurrentHp).IsLess(120f);
        AssertThat(target.KnockbackRemainingDistance).IsGreater(0f);
        AssertThat(bystander.CurrentHp).IsEqual(120f);
    }

    [TestCase]
    public void RainField_SlowsEnemiesImmediatelyAndDamagesRepeatedly()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);
        var ally = CreatePassiveMeleeUnit(state, 0, x: -3f, hp: 100f);
        var enemy = CreatePassiveMeleeUnit(state, 1, x: 3f, hp: 100f);

        CastSpell(state, sim, CardDefinitions.RainField, SimVector3.Zero);

        AssertThat(enemy.ActiveBuffs.Any(b => b.EffectType == EffectType.Slow)).IsTrue();
        AssertThat(ally.ActiveBuffs.Any(b => b.EffectType == EffectType.Slow)).IsFalse();
        AssertThat(state.DelayedEffects.All(e => e.Affinity == SpellAffinity.Enemies)).IsTrue();
        AssertThat(state.DelayedEffects.All(e => e.SourceTeam == Team.Player)).IsTrue();
        AssertThat(SpellTargetResolver.Resolve(state, state.DelayedEffects[0]).Select(u => u.UnitId))
            .ContainsExactly(enemy.UnitId);

        Advance(sim, 3.2f);

        AssertThat(enemy.CurrentHp).IsLess(100f);
        AssertThat(ally.CurrentHp).IsEqual(100f);
    }

    [TestCase]
    public void BubbleShield_AddsShieldToAlliesInAreaOnly()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);
        var ally = SimTestHelper.CreateMeleeUnit(state, 0, x: 0f, hp: 100f);
        var farAlly = SimTestHelper.CreateMeleeUnit(state, 0, x: 9f, hp: 100f);
        var enemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 0f, hp: 100f);

        CastSpell(state, sim, CardDefinitions.BubbleShield, SimVector3.Zero);

        AssertThat(ShieldAmount(ally)).IsEqual(45f);
        AssertThat(ShieldAmount(farAlly)).IsEqual(0f);
        AssertThat(ShieldAmount(enemy)).IsEqual(0f);
    }

    [TestCase]
    public void Whirlpool_PullsEnemiesTowardCenterAndDealsRepeatedDamage()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);
        var enemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 2f, hp: 100f);

        CastSpell(state, sim, CardDefinitions.Whirlpool, SimVector3.Zero);

        AssertThat(enemy.KnockbackRemainingDistance).IsGreater(0f);
        AssertThat(enemy.KnockbackDirection.X).IsLess(0f);

        Advance(sim, 2.45f);

        AssertThat(enemy.CurrentHp).IsLess(100f);
    }

    [TestCase]
    public void Flow_BuffsAlliesWithDodgeAndDamageOnly()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);
        var ally = SimTestHelper.CreateMeleeUnit(state, 0, x: 0f, hp: 100f);
        var enemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 0f, hp: 100f);

        CastSpell(state, sim, CardDefinitions.Flow, SimVector3.Zero);

        AssertThat(ally.ActiveBuffs.Any(b => b.EffectType == EffectType.EvasionModifier)).IsTrue();
        AssertThat(ally.ActiveBuffs.Any(b => b.EffectType == EffectType.DamageBoost)).IsTrue();
        AssertThat(enemy.ActiveBuffs.Any(b => b.EffectType == EffectType.EvasionModifier)).IsFalse();
        AssertThat(enemy.ActiveBuffs.Any(b => b.EffectType == EffectType.DamageBoost)).IsFalse();
    }

    [TestCase]
    public void Fortify_AddsFlatDamageReductionToAlliesWithoutHealing()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);
        var ally = SimTestHelper.CreateMeleeUnit(state, 0, x: 0f, hp: 100f);
        ally.CurrentHp = 60f;
        var enemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 0f, hp: 100f);

        CastSpell(state, sim, CardDefinitions.Fortify, SimVector3.Zero);

        AssertThat(ally.CurrentHp).IsEqual(60f);
        AssertThat(
                ally.ActiveBuffs.Any(b =>
                    b.EffectType == EffectType.FlatDamageReduction && b.Value == 4f
                )
            )
            .IsTrue();
        AssertThat(enemy.ActiveBuffs.Any(b => b.EffectType == EffectType.FlatDamageReduction))
            .IsFalse();
    }

    [TestCase]
    public void Quake_DamagesAndStunsGroundEnemiesOnly()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);
        var groundEnemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 0f, hp: 150f);
        var flyingEnemy = SimTestHelper.CreateFlyingUnit(state, 1, x: 0f, hp: 150f);

        CastSpell(state, sim, CardDefinitions.Quake, SimVector3.Zero);

        AssertThat(groundEnemy.CurrentHp).IsLess(150f);
        AssertThat(groundEnemy.ActiveBuffs.Any(b => b.EffectType == EffectType.Stun)).IsTrue();
        AssertThat(flyingEnemy.CurrentHp).IsEqual(150f);
        AssertThat(flyingEnemy.ActiveBuffs.Any(b => b.EffectType == EffectType.Stun)).IsFalse();
    }

    [TestCase]
    public void StoneSpike_DamagesOnlyExplicitEnemyTarget()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);
        var target = SimTestHelper.CreateMeleeUnit(state, 1, x: 0f, hp: 120f);
        var bystander = SimTestHelper.CreateMeleeUnit(state, 1, x: 1f, hp: 120f);

        CastSpell(state, sim, CardDefinitions.StoneSpike, SimVector3.Zero, target.UnitId);

        AssertThat(target.CurrentHp).IsLess(120f);
        AssertThat(bystander.CurrentHp).IsEqual(120f);
    }

    [TestCase]
    public void GravityWell_PullsEnemiesAndSlowsTheirAttackSpeed()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);
        var enemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 2f, hp: 120f);

        CastSpell(state, sim, CardDefinitions.GravityWell, SimVector3.Zero);

        AssertThat(enemy.KnockbackRemainingDistance).IsGreater(0f);
        AssertThat(enemy.KnockbackDirection.X).IsLess(0f);
        AssertThat(
                enemy.ActiveBuffs.Any(b =>
                    b.EffectType == EffectType.AttackSpeedModifier && b.Value < 0f
                )
            )
            .IsTrue();
    }

    [TestCase]
    public void ReformEarth_RevivesEarthAllyAtHalfHealth()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);
        var earthAlly = SimTestHelper.CreateMeleeUnit(state, 0, x: 0f, hp: 100f);
        earthAlly.ElementId = (int)Element.Earth;

        CastSpell(state, sim, CardDefinitions.ReformEarth, SimVector3.Zero);
        KillWithTrueDamage(state, earthAlly, Team.Enemy);

        AssertThat(earthAlly.IsAlive).IsTrue();
        AssertThat(earthAlly.CurrentHp).IsEqual(50f);
        AssertThat(earthAlly.ActiveBuffs.Any(b => b.EffectType == EffectType.ReviveOnDeath))
            .IsFalse();
    }

    [TestCase]
    public void ReformEarth_DoesNotReviveNonEarthAlly()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);
        var waterAlly = SimTestHelper.CreateMeleeUnit(state, 0, x: 0f, hp: 100f);
        waterAlly.ElementId = (int)Element.Water;

        CastSpell(state, sim, CardDefinitions.ReformEarth, SimVector3.Zero);
        KillWithTrueDamage(state, waterAlly, Team.Enemy);

        AssertThat(waterAlly.ActiveBuffs.Any(b => b.EffectType == EffectType.ReviveOnDeath))
            .IsFalse();
        AssertThat(waterAlly.IsAlive).IsFalse();
    }

    [TestCase]
    public void EarthenGrip_RootsAndDamagesOneEnemy()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);
        var target = SimTestHelper.CreateMeleeUnit(state, 1, x: 0f, hp: 100f);

        CastSpell(state, sim, CardDefinitions.EarthenGrip, SimVector3.Zero, target.UnitId);

        AssertThat(target.ActiveBuffs.Any(b => b.EffectType == EffectType.Root)).IsTrue();
        AssertThat(target.CurrentHp).IsLess(100f);
    }

    [TestCase]
    public void TailWind_BuffsAlliesAndDebuffsEnemiesInSquareZone()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);
        var ally = SimTestHelper.CreateMeleeUnit(state, 0, x: 0f, hp: 100f);
        var enemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 0f, hp: 100f);

        CastSpell(state, sim, CardDefinitions.TailWind, SimVector3.Zero);

        AssertThat(
                ally.ActiveBuffs.Any(b =>
                    b.EffectType == EffectType.AttackSpeedModifier && b.Value > 0f
                )
            )
            .IsTrue();
        AssertThat(
                enemy.ActiveBuffs.Any(b =>
                    b.EffectType == EffectType.AttackSpeedModifier && b.Value < 0f
                )
            )
            .IsTrue();
    }

    [TestCase]
    public void Tornado_LiftsCarriesDamagesAndDropsEnemy()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);
        var enemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 2f, hp: 180f);

        CastSpell(state, sim, CardDefinitions.Tornado, SimVector3.Zero);

        AssertThat(enemy.ActiveBuffs.Any(b => b.EffectType == EffectType.TornadoCarry)).IsTrue();

        Advance(sim, 0.65f);

        AssertThat(enemy.Position.Y).IsGreater(2.5f);
        AssertThat(enemy.CurrentHp).IsLess(180f);

        Advance(sim, 2.45f);

        AssertThat(enemy.ActiveBuffs.Any(b => b.EffectType == EffectType.TornadoCarry)).IsFalse();
        AssertThat(enemy.ActiveBuffs.Any(b => b.EffectType == EffectType.TornadoFall)).IsTrue();
    }

    [TestCase]
    public void Crosswind_ReducesEnemyRangedDamageOnly()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);
        var ally = SimTestHelper.CreateMeleeUnit(state, 0, x: 0f, hp: 100f);
        var enemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 0f, hp: 100f);

        CastSpell(state, sim, CardDefinitions.Crosswind, SimVector3.Zero);

        AssertThat(
                enemy.ActiveBuffs.Any(b =>
                    b.EffectType == EffectType.RangedDamageModifier && b.Value < 0f
                )
            )
            .IsTrue();
        AssertThat(ally.ActiveBuffs.Any(b => b.EffectType == EffectType.RangedDamageModifier))
            .IsFalse();
    }

    [TestCase]
    public void AirBullet_DamagesAndKnocksBackSingleEnemyTarget()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);
        var target = SimTestHelper.CreateMeleeUnit(state, 1, x: 2f, hp: 120f);

        CastSpell(state, sim, CardDefinitions.AirBullet, SimVector3.Zero, target.UnitId);

        AssertThat(target.CurrentHp).IsLess(120f);
        AssertThat(target.KnockbackRemainingDistance).IsGreater(0f);
        AssertThat(target.KnockbackDirection.X).IsGreater(0f);
    }

    [TestCase]
    public void Evacuate_PushesEnemiesAwayFromCastPoint()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);
        var enemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 2f, hp: 100f);

        CastSpell(state, sim, CardDefinitions.Evacuate, SimVector3.Zero);

        AssertThat(enemy.KnockbackRemainingDistance).IsGreater(0f);
        AssertThat(enemy.KnockbackDirection.X).IsGreater(0f);
    }

    [TestCase]
    public void WindShear_DamagesAndDisplacesEnemiesInLineOnly()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);
        var lineEnemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 5f, z: 0f, hp: 120f);
        var offLineEnemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 5f, z: 5f, hp: 120f);

        CastSpell(state, sim, CardDefinitions.WindShear, new SimVector3(10f, 0f, 0f));

        AssertThat(lineEnemy.CurrentHp).IsLess(120f);
        AssertThat(lineEnemy.KnockbackRemainingDistance).IsGreater(0f);
        AssertThat(offLineEnemy.CurrentHp).IsEqual(120f);
        AssertThat(offLineEnemy.KnockbackRemainingDistance).IsEqual(0f);
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

    private static UnitData CreatePassiveMeleeUnit(
        MatchState state,
        int team,
        float x = 0f,
        float z = 0f,
        float hp = 100f
    ) =>
        SimTestHelper.CreateMeleeUnit(
            state,
            team,
            x: x,
            z: z,
            hp: hp,
            damage: 0f,
            attackSpeed: 0f,
            attackRange: 0f,
            moveSpeed: 0f,
            aggroRadius: 0f
        );

    private static void Advance(Fateforged.Simulation.Simulation sim, float seconds)
    {
        int ticks = (int)(seconds / Simulation.FixedDeltaSeconds) + 1;
        for (int i = 0; i < ticks; i++)
            sim.Tick(Simulation.FixedDeltaSeconds);
    }

    private static float ShieldAmount(UnitData unit) =>
        unit.ActiveBuffs
            .Where(b => b.EffectType == EffectType.Shield)
            .Sum(b => MathF.Max(0f, b.ShieldHp));

    private static void AddSlow(UnitData unit)
    {
        unit.ActiveBuffs.Add(
            new ActiveBuff
            {
                BuffId = 123,
                EffectType = EffectType.Slow,
                Value = 0.3f,
                Duration = 5f,
                Lifetime = EffectLifetime.Timed(5f),
            }
        );
    }

    private static void KillShieldWithTrueDamage(MatchState state, UnitData target)
    {
        SimEffects.ApplyEffect(
            state,
            EffectType.Damage,
            80f,
            0f,
            DamageType.True,
            target,
            MatchState.GetSummonerTargetId(1),
            Team.Enemy,
            []
        );
    }

    private static void KillWithTrueDamage(MatchState state, UnitData target, Team sourceTeam)
    {
        SimEffects.ApplyEffect(
            state,
            EffectType.Damage,
            500f,
            0f,
            DamageType.True,
            target,
            MatchState.GetSummonerTargetId((int)sourceTeam),
            sourceTeam,
            []
        );
    }
}
