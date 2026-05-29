namespace Fateforged.Tests.Simulation.Abilities;

using System.Collections.Generic;
using System.Linq;
using Fateforged.Constants;
using Fateforged.Simulation;
using Fateforged.Simulation.Combat;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Subsystems;
using Fateforged.Tests.Simulation;
using Fateforged.Units;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class RosterUnitAbilityRuntimeTest
{
    [TestCase]
    public void FireRoster_CinderAndChanneler_OnHitApplyBurn()
    {
        AssertOnHitAppliesStatus(UnitIds.CinderCaster, StatusEffectKind.Burn);
        AssertOnHitAppliesStatus(UnitIds.FlameChanneler, StatusEffectKind.Burn);
    }

    [TestCase]
    public void FireRoster_FireSpider_OnHitSlows()
    {
        AssertOnHitAppliesEffect(UnitIds.FireSpider, EffectType.Slow);
    }

    [TestCase]
    public void FireRoster_EmberBombCarrier_DeathBurstDamagesNearbyEnemies()
    {
        var state = SimTestHelper.CreateBattleState();
        var events = new List<SimEvent>();
        var carrier = CreateUnitFromDefinition(state, UnitIds.EmberBombCarrier, 0, x: 0f);
        var enemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 2f, hp: 100f);

        SimUtils.KillUnit(state, carrier, enemy.UnitId, events);
        SimEffects.FireDeathTriggers(state, carrier, enemy, events);

        AssertThat(enemy.CurrentHp).IsLess(100f);
    }

    [TestCase]
    public void FireRoster_OverheatBrawler_PeriodicBuffsAndSelfDamages()
    {
        var state = SimTestHelper.CreateBattleState();
        var events = new List<SimEvent>();
        var brawler = CreateUnitFromDefinition(state, UnitIds.OverheatBrawler, 0);
        float hpBefore = brawler.CurrentHp;

        SimAbilityOrchestrator.Tick(state, Simulation.FixedDeltaSeconds, events);

        AssertThat(brawler.CurrentHp).IsLess(hpBefore);
        AssertThat(brawler.ActiveBuffs.Any(b => b.EffectType == EffectType.DamageBoost)).IsTrue();
        AssertThat(brawler.ActiveBuffs.Any(b => b.EffectType == EffectType.AttackSpeedModifier)).IsTrue();
    }

    [TestCase]
    public void WaterRoster_RedistributorAndBarbedInflator_PeriodicAbilitiesWork()
    {
        var state = SimTestHelper.CreateBattleState();
        var events = new List<SimEvent>();
        CreateUnitFromDefinition(state, UnitIds.WaterRedistributor, 0, x: 0f);
        var donor = SimTestHelper.CreateMeleeUnit(state, 0, x: 2f, hp: 100f);
        donor.CurrentHp = 90f;
        var receiver = SimTestHelper.CreateMeleeUnit(state, 0, x: 3f, hp: 100f);
        receiver.CurrentHp = 30f;
        var inflator = CreateUnitFromDefinition(state, UnitIds.BarbedInflator, 0, x: 10f);
        var enemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 12f, hp: 100f);

        SimAbilityOrchestrator.Tick(state, Simulation.FixedDeltaSeconds, events);

        AssertThat(donor.CurrentHp).IsLess(90f);
        AssertThat(receiver.CurrentHp).IsGreater(30f);
        AssertThat(enemy.CurrentHp).IsLess(100f);
        AssertThat(inflator.ActiveBuffs.Any(b => b.EffectType == EffectType.Shield)).IsTrue();
    }

    [TestCase]
    public void WaterRoster_SlipperyMelee_OnSpawnAppliesPersistentEvasion()
    {
        var state = SimTestHelper.CreateBattleState();
        var events = new List<SimEvent>();
        var slippery = CreateUnitFromDefinition(state, UnitIds.SlipperyMelee, 0);

        SimAbilityOrchestrator.Tick(state, Simulation.FixedDeltaSeconds, events);

        AssertThat(slippery.ActiveBuffs.Any(b => b.EffectType == EffectType.EvasionModifier)).IsTrue();
    }

    [TestCase]
    public void EarthRoster_ShieldSupportAndBurrowAmbusher_AbilitiesWork()
    {
        var state = SimTestHelper.CreateBattleState();
        var events = new List<SimEvent>();
        CreateUnitFromDefinition(state, UnitIds.EarthShieldSupport, 0, x: 0f);
        var ally = SimTestHelper.CreateMeleeUnit(state, 0, x: 2f, hp: 100f);
        var ambusher = CreateUnitFromDefinition(state, UnitIds.BurrowAmbusher, 0, x: 8f);
        var enemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 10f, hp: 100f);
        float enemyHpBefore = enemy.CurrentHp;

        SimAbilityOrchestrator.Tick(state, Simulation.FixedDeltaSeconds, events);
        SimAbilityOrchestrator.TryActivateOnHitEffects(state, ambusher, enemy, events);

        AssertThat(ally.ActiveBuffs.Any(b => b.EffectType == EffectType.Shield)).IsTrue();
        AssertThat(enemy.CurrentHp).IsLess(enemyHpBefore);
        AssertThat(enemy.ActiveBuffs.Any(b => b.EffectType == EffectType.Stun)).IsTrue();
    }

    [TestCase]
    public void EarthRoster_EarthBullet_OnHitSlows()
    {
        AssertOnHitAppliesEffect(UnitIds.EarthBulletUnit, EffectType.Slow);
    }

    [TestCase]
    public void WindRoster_SupportAndDashAbilitiesWork()
    {
        var state = SimTestHelper.CreateBattleState();
        var events = new List<SimEvent>();
        CreateUnitFromDefinition(state, UnitIds.WindSpeedSupport, 0, x: 0f);
        var ally = SimTestHelper.CreateMeleeUnit(state, 0, x: 2f, hp: 100f);
        CreateUnitFromDefinition(state, UnitIds.WindMissSupport, 0, x: 10f);
        var enemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 12f, hp: 100f);
        var dash = CreateUnitFromDefinition(state, UnitIds.DashStriker, 0, x: 18f);
        var dashTarget = SimTestHelper.CreateMeleeUnit(state, 1, x: 20f, hp: 100f);

        SimAbilityOrchestrator.Tick(state, Simulation.FixedDeltaSeconds, events);
        SimAbilityOrchestrator.TryActivateOnHitEffects(state, dash, dashTarget, events);

        AssertThat(ally.ActiveBuffs.Any(b => b.EffectType == EffectType.AttackSpeedModifier)).IsTrue();
        AssertThat(enemy.ActiveBuffs.Any(b => b.EffectType == EffectType.AccuracyModifier)).IsTrue();
        AssertThat(dash.ActiveBuffs.Any(b => b.EffectType == EffectType.EvasionModifier)).IsTrue();
        AssertThat(dash.ActiveBuffs.Any(b => b.EffectType == EffectType.AttackSpeedModifier)).IsTrue();
    }

    [TestCase]
    public void WindRoster_PushbackAndEvasionTank_AbilitiesWork()
    {
        var state = SimTestHelper.CreateBattleState();
        var events = new List<SimEvent>();
        var tank = CreateUnitFromDefinition(state, UnitIds.WindEvasionTank, 0, x: 0f);
        var pushback = CreateUnitFromDefinition(state, UnitIds.WindPushbackUnit, 0, x: 4f);
        var enemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 6f, hp: 100f);

        SimAbilityOrchestrator.Tick(state, Simulation.FixedDeltaSeconds, events);
        SimAbilityOrchestrator.TryActivateOnHitEffects(state, pushback, enemy, events);

        AssertThat(tank.ActiveBuffs.Any(b => b.EffectType == EffectType.EvasionModifier)).IsTrue();
        AssertThat(enemy.KnockbackRemainingDistance).IsGreater(0f);
    }

    private static void AssertOnHitAppliesStatus(UnitId unitId, StatusEffectKind statusKind)
    {
        var state = SimTestHelper.CreateBattleState();
        var events = new List<SimEvent>();
        var source = CreateUnitFromDefinition(state, unitId, 0, x: 0f);
        var target = SimTestHelper.CreateMeleeUnit(state, 1, x: 2f, hp: 100f);

        SimAbilityOrchestrator.TryActivateOnHitEffects(state, source, target, events);

        AssertThat(target.ActiveBuffs.Any(b => b.StatusKind == statusKind)).IsTrue();
    }

    private static void AssertOnHitAppliesEffect(UnitId unitId, EffectType effectType)
    {
        var state = SimTestHelper.CreateBattleState();
        var events = new List<SimEvent>();
        var source = CreateUnitFromDefinition(state, unitId, 0, x: 0f);
        var target = SimTestHelper.CreateMeleeUnit(state, 1, x: 2f, hp: 100f);

        SimAbilityOrchestrator.TryActivateOnHitEffects(state, source, target, events);

        AssertThat(target.ActiveBuffs.Any(b => b.EffectType == effectType)).IsTrue();
    }

    private static UnitData CreateUnitFromDefinition(
        MatchState state,
        UnitId unitId,
        int team,
        float x = 0f,
        float z = 0f
    )
    {
        var template = UnitDefinitions.BuildSimTemplate(unitId, 1);
        int id = state.NextUnitId();
        var unit = new UnitData
        {
            UnitId = id,
            CatalogId = unitId.Value,
            Team = (Team)team,
            CurrentHp = template.MaxHp,
            MaxHp = template.MaxHp,
            IsAlive = true,
            Position = new SimVector3(x, 0f, z),
            AttackDamage = template.AttackDamage,
            AttackSpeed = template.AttackSpeed,
            MoveSpeed = template.MoveSpeed,
            AttackRange = template.AttackRange,
            AggroRadius = template.AggroRadius,
            UnitType = template.UnitType,
            MovementLayer = template.MovementLayer,
            ProjectileCatalogId = template.ProjectileCatalogId,
            ActivationState = ActivationState.Active,
            TargetLayerFilter = TargetLayer.Both,
            Abilities = template.Abilities.Select(ability => ability.DeepClone()).ToList(),
        };
        state.Units[id] = unit;
        return unit;
    }
}
