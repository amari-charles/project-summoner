namespace Fateforged.Tests.Simulation.Abilities;

using System.Collections.Generic;
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
    public void FireSpells_ProjectileMarkAndFlareShieldWork()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);
        var ally = SimTestHelper.CreateMeleeUnit(state, 0, x: 0f, hp: 100f);
        var fireballTarget = SimTestHelper.CreateMeleeUnit(state, 1, x: -5f, hp: 160f);
        var markedEnemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 12f, hp: 120f);
        var nearbyEnemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 1f, hp: 120f);

        CastSpell(state, sim, CardDefinitions.Fireball, new SimVector3(-5f, 0f, 0f));
        Advance(sim, seconds: 2.5f);

        AssertThat(fireballTarget.CurrentHp).IsLess(160f);

        float markedHp = markedEnemy.CurrentHp;
        CastSpell(state, sim, CardDefinitions.IgnitionMark, SimVector3.Zero, markedEnemy.UnitId);

        AssertThat(markedEnemy.ActiveBuffs.Any(b => b.StatusKind == StatusEffectKind.Burn)).IsTrue();

        Advance(sim, seconds: 4.2f);

        AssertThat(markedEnemy.CurrentHp).IsLess(markedHp);

        var shieldState = SimTestHelper.CreateBattleState();
        var shieldSim = new Fateforged.Simulation.Simulation(shieldState);
        var shieldedAlly = SimTestHelper.CreateMeleeUnit(shieldState, 0, x: 0f, hp: 100f);
        var flareEnemy = SimTestHelper.CreateMeleeUnit(shieldState, 1, x: 1f, hp: 120f);
        CastSpell(shieldState, shieldSim, CardDefinitions.FlareShield, SimVector3.Zero);

        AssertThat(shieldedAlly.ActiveBuffs.Any(b => b.EffectType == EffectType.Shield)).IsTrue();

        float enemyHpBeforeBreak = flareEnemy.CurrentHp;
        SimEffects.ApplyEffect(
            shieldState,
            EffectType.Damage,
            80f,
            0f,
            DamageType.True,
            shieldedAlly,
            MatchState.GetSummonerTargetId(1),
            Team.Enemy,
            []
        );

        AssertThat(flareEnemy.CurrentHp).IsLess(enemyHpBeforeBreak);
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

        Advance(sim, seconds: 2.45f);

        AssertThat(enemy.CurrentHp).IsLess(enemyHpBeforeWhirlpool);
    }

    [TestCase]
    public void WaterSpells_CleanseWaterJetAndRainFieldWork()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);
        var ally = SimTestHelper.CreateMeleeUnit(state, 0, x: 0f, hp: 100f);
        ally.CurrentHp = 50f;
        ally.ActiveBuffs.Add(
            new ActiveBuff
            {
                BuffId = 42,
                EffectType = EffectType.Slow,
                Value = 0.3f,
                Duration = 5f,
                Lifetime = EffectLifetime.Timed(5f),
            }
        );
        var enemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 2f, hp: 120f);

        CastSpell(state, sim, CardDefinitions.Cleanse, SimVector3.Zero);

        AssertThat(ally.CurrentHp).IsGreater(50f);
        AssertThat(ally.ActiveBuffs.Any(b => b.EffectType == EffectType.Slow)).IsFalse();

        float enemyHpBeforeJet = enemy.CurrentHp;
        CastSpell(state, sim, CardDefinitions.WaterJet, SimVector3.Zero, enemy.UnitId);

        AssertThat(enemy.CurrentHp).IsLess(enemyHpBeforeJet);
        AssertThat(enemy.KnockbackRemainingDistance).IsGreater(0f);

        enemy.KnockbackRemainingDistance = 0f;
        float enemyHpBeforeRain = enemy.CurrentHp;
        CastSpell(state, sim, CardDefinitions.RainField, SimVector3.Zero);

        AssertThat(enemy.ActiveBuffs.Any(b => b.EffectType == EffectType.Slow)).IsTrue();

        Advance(sim, seconds: 3.2f);

        AssertThat(enemy.CurrentHp).IsLess(enemyHpBeforeRain);
    }

    [TestCase]
    public void EarthSpells_DamageControlAndReviveWork()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);
        var ally = SimTestHelper.CreateMeleeUnit(state, 0, x: 0f, hp: 100f);
        ally.ElementId = (int)Element.Earth;
        var nonEarthAlly = SimTestHelper.CreateMeleeUnit(state, 0, x: 1f, hp: 100f);
        nonEarthAlly.ElementId = (int)Element.Water;
        var enemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 0f, hp: 150f);
        var flyingEnemy = SimTestHelper.CreateFlyingUnit(state, 1, x: 0f, hp: 150f);

        CastSpell(state, sim, CardDefinitions.Quake, SimVector3.Zero);

        AssertThat(enemy.CurrentHp).IsLess(150f);
        AssertThat(enemy.ActiveBuffs.Any(b => b.EffectType == EffectType.Stun)).IsTrue();
        AssertThat(flyingEnemy.CurrentHp).IsEqual(150f);
        AssertThat(flyingEnemy.ActiveBuffs.Any(b => b.EffectType == EffectType.Stun)).IsFalse();

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
        AssertThat(nonEarthAlly.ActiveBuffs.Any(b => b.EffectType == EffectType.ReviveOnDeath))
            .IsFalse();
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
    public void EarthSpells_FortifyAndStoneSpikeWork()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);
        var ally = SimTestHelper.CreateMeleeUnit(state, 0, x: 0f, hp: 100f);
        var enemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 2f, hp: 120f);

        CastSpell(state, sim, CardDefinitions.Fortify, SimVector3.Zero);

        AssertThat(ally.ActiveBuffs.Any(b => b.EffectType == EffectType.FlatDamageReduction))
            .IsTrue();

        CastSpell(state, sim, CardDefinitions.StoneSpike, SimVector3.Zero, enemy.UnitId);

        AssertThat(enemy.CurrentHp).IsLess(120f);
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

    [TestCase]
    public void WindSpells_TailWindTornadoAndEvacuateWork()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);
        var ally = SimTestHelper.CreateMeleeUnit(state, 0, x: 0f, hp: 100f);
        var enemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 2f, hp: 160f);
        var upperEnemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 3f, hp: 160f);
        var lateEnemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 12f, hp: 160f);

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

        float enemyHpBeforeTornado = enemy.CurrentHp;
        var positionBeforeTornado = enemy.Position;
        CastSpell(state, sim, CardDefinitions.Tornado, SimVector3.Zero);
        AssertThat(enemy.ActiveBuffs.Any(b => b.EffectType == EffectType.TornadoCarry)).IsTrue();
        AssertThat(upperEnemy.ActiveBuffs.Any(b => b.EffectType == EffectType.TornadoCarry))
            .IsTrue();
        AssertThat(SimEffects.IsStunned(enemy)).IsTrue();

        lateEnemy.Position = new SimVector3(1f, 0f, 0f);
        Advance(sim, seconds: 0.65f);

        AssertThat(enemy.CurrentHp).IsLess(enemyHpBeforeTornado);
        AssertThat(enemy.Position.Y).IsGreater(2.5f);
        AssertThat(upperEnemy.Position.Y).IsGreater(enemy.Position.Y);
        var enemyCarry = enemy.ActiveBuffs.First(b => b.EffectType == EffectType.TornadoCarry);
        var upperCarry = upperEnemy.ActiveBuffs.First(b => b.EffectType == EffectType.TornadoCarry);
        AssertThat(upperCarry.TornadoOrbitRadius).IsGreater(enemyCarry.TornadoOrbitRadius);
        AssertThat(enemy.Position.X).IsNotEqual(positionBeforeTornado.X);
        AssertThat(lateEnemy.ActiveBuffs.Any(b => b.EffectType == EffectType.TornadoCarry))
            .IsTrue();

        Advance(sim, seconds: 2.8f);

        AssertThat(enemy.ActiveBuffs.Any(b => b.EffectType == EffectType.TornadoCarry)).IsFalse();
        AssertThat(enemy.ActiveBuffs.Any(b => b.EffectType == EffectType.TornadoFall)).IsTrue();
        AssertThat(SimEffects.IsStunned(enemy)).IsTrue();
        AssertThat(enemy.Position.Y).IsGreater(0f);
        AssertThat(enemy.Velocity.Y).IsLess(0.001f);

        Advance(sim, seconds: 1.0f);

        AssertThat(enemy.ActiveBuffs.Any(b => b.EffectType == EffectType.TornadoFall)).IsFalse();
        AssertThat(enemy.Position.Y).IsEqualApprox(0f, 0.001f);

        enemy.Position = new SimVector3(2f, 0f, 0f);
        enemy.KnockbackRemainingDistance = 0f;
        CastSpell(state, sim, CardDefinitions.Evacuate, SimVector3.Zero);

        AssertThat(enemy.KnockbackRemainingDistance).IsGreater(0f);
        AssertThat(enemy.KnockbackDirection.X).IsGreater(0f);
    }

    [TestCase]
    public void Tornado_FlyingUnit_LiftsAboveAndFallsBackToFlightAltitude()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);
        const float flightAltitude = 2.2f;
        var flyer = SimTestHelper.CreateFlyingUnit(
            state,
            team: 1,
            x: 2f,
            hp: 300f,
            altitude: flightAltitude
        );

        CastSpell(state, sim, CardDefinitions.Tornado, SimVector3.Zero);

        AssertThat(flyer.ActiveBuffs.Any(b => b.EffectType == EffectType.TornadoCarry)).IsTrue();
        AssertThat(flyer.Position.Y).IsGreater(flightAltitude - 0.001f);

        Advance(sim, seconds: 0.3f);

        AssertThat(flyer.Position.Y).IsGreater(flightAltitude + 2.0f);

        Advance(sim, seconds: 2.8f);

        AssertThat(flyer.ActiveBuffs.Any(b => b.EffectType == EffectType.TornadoCarry)).IsFalse();
        AssertThat(flyer.ActiveBuffs.Any(b => b.EffectType == EffectType.TornadoFall)).IsTrue();
        AssertThat(SimEffects.IsStunned(flyer)).IsTrue();
        AssertThat(flyer.Position.Y).IsGreater(flightAltitude);

        Advance(sim, seconds: 1.0f);

        AssertThat(flyer.ActiveBuffs.Any(b => b.EffectType == EffectType.TornadoFall)).IsFalse();
        AssertThat(flyer.Position.Y).IsEqualApprox(flightAltitude, 0.001f);
    }

    [TestCase]
    public void SpellDebugLogs_ReportAppliedDelayedAndZeroTargetOutcomes()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);
        var enemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 0f, hp: 120f);
        var logs = new List<string>();
        var oldEnabled = Fateforged.Simulation.Simulation.DebugAbilityLogsEnabled;
        var oldLog = Fateforged.Simulation.Simulation.Log;

        try
        {
            Fateforged.Simulation.Simulation.DebugAbilityLogsEnabled = true;
            Fateforged.Simulation.Simulation.Log = logs.Add;

            CastSpell(state, sim, CardDefinitions.FireAreaBurn, SimVector3.Zero);
            CastSpell(state, sim, CardDefinitions.BurnCashout, SimVector3.Zero);
            CastSpell(state, sim, CardDefinitions.RainField, SimVector3.Zero);
            Advance(sim, seconds: 0.8f);

            enemy.IsAlive = false;
            CastSpell(state, sim, CardDefinitions.WaterJet, SimVector3.Zero, enemy.UnitId);
        }
        finally
        {
            Fateforged.Simulation.Simulation.DebugAbilityLogsEnabled = oldEnabled;
            Fateforged.Simulation.Simulation.Log = oldLog;
        }

        AssertThat(logs.Any(line => line.Contains("Fire Area Burn applied Burn to 1/1 enemy")))
            .IsTrue();
        AssertThat(logs.Any(line => line.Contains("Burn Cashout applied Burn cashout to 1/1 enemy")))
            .IsTrue();
        AssertThat(logs.Any(line => line.Contains("Rain Field queued damage"))).IsTrue();
        AssertThat(logs.Any(line => line.Contains("Rain Field resolved delayed damage"))).IsTrue();
        AssertThat(logs.Any(line => line.Contains("Water Jet applied damage but found no valid enemies")))
            .IsTrue();
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
