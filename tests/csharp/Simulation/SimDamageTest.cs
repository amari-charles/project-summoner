namespace Fateforged.Tests.Simulation;

using System.Collections.Generic;
using Fateforged.Simulation;
using Fateforged.Simulation.Combat;
using Fateforged.Units;
using GdUnit4;
using Fateforged.Cards;
using static GdUnit4.Assertions;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Subsystems;

[TestSuite]
public class SimDamageTest
{
    private MatchState _state = null!;

    [BeforeTest]
    public void Setup()
    {
        _state = SimTestHelper.CreateBattleState();
    }

    // =========================================================================
    // Defense Multiplier Curve
    // =========================================================================

    [TestCase]
    public void CalculateDefenseMultiplier_ZeroDefense_ReturnsOne()
    {
        AssertThat(SimDamage.CalculateDefenseMultiplier(0f)).IsEqual(1f);
    }

    [TestCase]
    public void CalculateDefenseMultiplier_100Defense_ReturnsHalf()
    {
        AssertThat(SimDamage.CalculateDefenseMultiplier(100f)).IsEqual(0.5f);
    }

    [TestCase]
    public void CalculateDefenseMultiplier_NegativeDefense_ClampsToOne()
    {
        AssertThat(SimDamage.CalculateDefenseMultiplier(-50f)).IsEqual(1f);
    }

    // =========================================================================
    // Damage Type Routing
    // =========================================================================

    [TestCase]
    public void Calculate_PhysicalDamage_UsesPhysicalDefense()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        attacker.CritChance = 0f;
        attacker.ElementId = 0; // Neutral

        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f);
        target.PhysicalDefense = 100f;
        target.MagicDefense = 0f;
        target.Evasion = 0f;

        var (damage, _, _) = SimDamage.Calculate(
            100f, DamageType.Physical, attacker, target, null, null, _state.Rng);

        // 100 / (100 + 100) = 0.5 → 100 * 0.5 = 50
        AssertThat(damage).IsEqual(50f);
    }

    [TestCase]
    public void Calculate_MagicDamage_UsesMagicDefense()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        attacker.CritChance = 0f;
        attacker.ElementId = 0;

        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f);
        target.PhysicalDefense = 0f;
        target.MagicDefense = 100f;
        target.Evasion = 0f;

        var (damage, _, _) = SimDamage.Calculate(
            100f, DamageType.Magic, attacker, target, null, null, _state.Rng);

        AssertThat(damage).IsEqual(50f);
    }

    [TestCase]
    public void Calculate_TrueDamage_IgnoresAllDefense()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        attacker.CritChance = 0f;
        attacker.ElementId = 0;

        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f);
        target.PhysicalDefense = 200f;
        target.MagicDefense = 200f;
        target.Evasion = 0f;

        var (damage, _, _) = SimDamage.Calculate(
            100f, DamageType.True, attacker, target, null, null, _state.Rng);

        AssertThat(damage).IsEqual(100f);
    }

    // =========================================================================
    // Evasion
    // =========================================================================

    [TestCase]
    public void Calculate_HighEvasion_ReturnsZeroDamage()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        attacker.CritChance = 0f;
        attacker.ElementId = 0;

        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f);
        target.Evasion = 1.0f; // Guaranteed evasion

        var (damage, _, wasEvaded) = SimDamage.Calculate(
            100f, DamageType.Physical, attacker, target, null, null, _state.Rng);

        AssertThat(damage).IsEqual(0f);
        AssertThat(wasEvaded).IsTrue();
    }

    [TestCase]
    public void Calculate_ZeroEvasion_NeverEvades()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        attacker.CritChance = 0f;
        attacker.ElementId = 0;

        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f);
        target.Evasion = 0f;

        var (damage, _, wasEvaded) = SimDamage.Calculate(
            100f, DamageType.Physical, attacker, target, null, null, _state.Rng);

        AssertThat(damage).IsGreater(0f);
        AssertThat(wasEvaded).IsFalse();
    }

    // =========================================================================
    // Critical Hits
    // =========================================================================

    [TestCase]
    public void Calculate_GuaranteedCrit_MultipliesByCritDamage()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        attacker.CritChance = 1.0f;
        attacker.CritDamage = 2.0f;
        attacker.ElementId = 0;

        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f);
        target.Evasion = 0f;

        var (damage, isCrit, _) = SimDamage.Calculate(
            100f, DamageType.Physical, attacker, target, null, null, _state.Rng);

        AssertThat(isCrit).IsTrue();
        AssertThat(damage).IsEqual(200f);
    }

    [TestCase]
    public void Calculate_ZeroCritChance_NeverCrits()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        attacker.CritChance = 0f;
        attacker.ElementId = 0;

        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f);
        target.Evasion = 0f;

        var (damage, isCrit, _) = SimDamage.Calculate(
            100f, DamageType.Physical, attacker, target, null, null, _state.Rng);

        AssertThat(isCrit).IsFalse();
        AssertThat(damage).IsEqual(100f);
    }

    // =========================================================================
    // Elemental Matchups
    // =========================================================================

    [TestCase]
    public void Calculate_ElementAdvantage_IncreasedDamage()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        attacker.CritChance = 0f;
        attacker.ElementId = (int)Element.Fire;

        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f);
        target.ElementId = (int)Element.Wind; // Fire > Wind
        target.Evasion = 0f;

        var (damage, _, _) = SimDamage.Calculate(
            100f, DamageType.Physical, attacker, target, null, null, _state.Rng);

        // Fire vs Wind = 1.25x multiplier → 125
        AssertThat(damage).IsEqual(125f);
    }

    [TestCase]
    public void Calculate_ElementDisadvantage_DecreasedDamage()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        attacker.CritChance = 0f;
        attacker.ElementId = (int)Element.Fire;

        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f);
        target.ElementId = (int)Element.Water; // Fire < Water
        target.Evasion = 0f;

        var (damage, _, _) = SimDamage.Calculate(
            100f, DamageType.Physical, attacker, target, null, null, _state.Rng);

        // Fire vs Water = 0.8x multiplier → 80
        AssertThat(damage).IsEqual(80f);
    }

    [TestCase]
    public void Calculate_NeutralElement_NoBonusOrPenalty()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        attacker.CritChance = 0f;
        attacker.ElementId = (int)Element.Neutral;

        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f);
        target.ElementId = (int)Element.Neutral;
        target.Evasion = 0f;

        var (damage, _, _) = SimDamage.Calculate(
            100f, DamageType.Physical, attacker, target, null, null, _state.Rng);

        AssertThat(damage).IsEqual(100f);
    }

    // =========================================================================
    // Summoner Modifiers
    // =========================================================================

    [TestCase]
    public void Calculate_SummonerDamageBonus_IncreasesDamage()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        attacker.CritChance = 0f;
        attacker.ElementId = 0;

        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f);
        target.Evasion = 0f;

        _state.Summoners[0].DamageBonus = 20f; // 20% bonus

        var (damage, _, _) = SimDamage.Calculate(
            100f, DamageType.Physical, attacker, target, _state.Summoners[0], null, _state.Rng);

        // 100 * 1.20 = 120
        AssertThat(damage).IsEqual(120f);
    }

    [TestCase]
    public void Calculate_SummonerDamageReduction_DecreasesDamage()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        attacker.CritChance = 0f;
        attacker.ElementId = 0;

        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f);
        target.Evasion = 0f;

        _state.Summoners[1].DamageReduction = 10f; // Flat 10 reduction

        var (damage, _, _) = SimDamage.Calculate(
            100f, DamageType.Physical, attacker, target, null, _state.Summoners[1], _state.Rng);

        // 100 - 10 = 90
        AssertThat(damage).IsEqual(90f);
    }

    [TestCase]
    public void Calculate_SummonerDamageReduction_FloorAtZero()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        attacker.CritChance = 0f;
        attacker.ElementId = 0;

        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f);
        target.Evasion = 0f;

        _state.Summoners[1].DamageReduction = 200f; // More than damage

        var (damage, _, _) = SimDamage.Calculate(
            100f, DamageType.Physical, attacker, target, null, _state.Summoners[1], _state.Rng);

        AssertThat(damage).IsEqual(0f);
    }

    // =========================================================================
    // Shield Absorption
    // =========================================================================

    [TestCase]
    public void Calculate_ShieldAbsorbsAllDamage_ReturnsZero()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        attacker.CritChance = 0f;
        attacker.ElementId = 0;

        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f);
        target.Evasion = 0f;

        // Add a shield that can absorb all damage
        SimEffects.ApplyShield(_state, target, 200f, attacker.UnitId, Team.Player);

        var (damage, _, _) = SimDamage.Calculate(
            100f, DamageType.Physical, attacker, target, null, null, _state.Rng);

        AssertThat(damage).IsEqual(0f);
    }

    [TestCase]
    public void Calculate_ShieldAbsorbsPartial_ReturnsRemainder()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        attacker.CritChance = 0f;
        attacker.ElementId = 0;

        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f);
        target.Evasion = 0f;

        SimEffects.ApplyShield(_state, target, 30f, attacker.UnitId, Team.Player);

        var (damage, _, _) = SimDamage.Calculate(
            100f, DamageType.Physical, attacker, target, null, null, _state.Rng);

        AssertThat(damage).IsEqual(70f);
    }

    [TestCase]
    public void Calculate_MultipleShields_OldestConsumedFirst()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        attacker.CritChance = 0f;
        attacker.ElementId = 0;

        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f);
        target.Evasion = 0f;

        // Oldest shield added first (25 HP)
        SimEffects.ApplyShield(_state, target, 25f, attacker.UnitId, Team.Player);
        // Newer shield (50 HP)
        SimEffects.ApplyShield(_state, target, 50f, attacker.UnitId, Team.Player);

        var (damage, _, _) = SimDamage.Calculate(
            100f, DamageType.Physical, attacker, target, null, null, _state.Rng);

        // 100 - 25 (oldest) - 50 (next) = 25 remaining damage
        AssertThat(damage).IsEqual(25f);
    }

    // =========================================================================
    // Rounding
    // =========================================================================

    [TestCase]
    public void Calculate_DamageRoundedToOneDecimal()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        attacker.CritChance = 0f;
        attacker.ElementId = 0;

        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f);
        target.Evasion = 0f;
        target.PhysicalDefense = 30f; // 100 / 130 ≈ 0.769... → 100 * 0.769... = 76.923...

        var (damage, _, _) = SimDamage.Calculate(
            100f, DamageType.Physical, attacker, target, null, null, _state.Rng);

        // Should be rounded to 1 decimal: 76.9
        float expected = System.MathF.Round(100f * (100f / 130f) * 10f) / 10f;
        AssertThat(damage).IsEqual(expected);
    }

    // =========================================================================
    // Combined Pipeline
    // =========================================================================

    [TestCase]
    public void Calculate_FullPipeline_CorrectOrder()
    {
        // Test: crit → elemental → summoner bonus → defense → summoner reduction → shield → round
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        attacker.CritChance = 1.0f;
        attacker.CritDamage = 1.5f;
        attacker.ElementId = (int)Element.Fire;

        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f);
        target.Evasion = 0f;
        target.ElementId = (int)Element.Wind; // Fire > Wind = 1.25x
        target.PhysicalDefense = 100f; // 0.5x

        _state.Summoners[0].DamageBonus = 10f; // +10%
        _state.Summoners[1].DamageReduction = 5f; // -5 flat

        // Pipeline:
        // base=100, crit=100*1.5=150, elem=150*1.25=187.5, bonus=187.5*1.1=206.25
        // defense=206.25*0.5=103.125, reduction=103.125-5=98.125, round=98.1
        var (damage, isCrit, _) = SimDamage.Calculate(
            100f, attacker.AttackType, attacker, target, _state.Summoners[0], _state.Summoners[1], _state.Rng);

        AssertThat(isCrit).IsTrue();
        AssertThat(damage).IsEqual(98.1f);
    }

    // =========================================================================
    // Null Safety
    // =========================================================================

    [TestCase]
    public void Calculate_NullAttacker_StillCalculatesDamage()
    {
        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f);
        target.Evasion = 0f;

        var (damage, isCrit, _) = SimDamage.Calculate(
            50f, DamageType.Physical, null, target, null, null, _state.Rng);

        // No crit (null attacker), neutral element, no defense → 50
        AssertThat(isCrit).IsFalse();
        AssertThat(damage).IsEqual(50f);
    }

    [TestCase]
    public void Calculate_NullRng_SkipsEvasionAndCrit()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        attacker.CritChance = 1.0f; // Would crit with RNG
        attacker.ElementId = 0;

        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f);
        target.Evasion = 1.0f; // Would evade with RNG

        var (damage, isCrit, wasEvaded) = SimDamage.Calculate(
            100f, DamageType.Physical, attacker, target, null, null, null);

        // Both evasion and crit require RNG — skipped
        AssertThat(isCrit).IsFalse();
        AssertThat(wasEvaded).IsFalse();
        AssertThat(damage).IsEqual(100f);
    }

    // =========================================================================
    // DamageProfile + Summoner Modifier Coverage
    // =========================================================================

    [TestCase]
    public void Calculate_MixedProfile_SplitsAcrossDefenseLanes()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        attacker.CritChance = 0f;
        attacker.ElementId = (int)Element.Neutral;
        attacker.AttackType = DamageType.Physical;
        attacker.PhysicalDamageRatio = 0.6f;
        attacker.ElementalDamageRatio = 0.4f;

        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f);
        target.Evasion = 0f;
        target.PhysicalDefense = 100f; // 0.5x
        target.MagicDefense = 0f;      // 1.0x

        var (damage, _, _) = SimDamage.CalculateAttack(
            100f,
            attacker,
            target,
            null,
            null,
            _state.Rng);

        // 60 * 0.5 + 40 * 1.0 = 70
        AssertThat(damage).IsEqual(70f);
    }

    [TestCase]
    public void Calculate_ExplicitDamageType_DoesNotUseProfileSplitByDefault()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        attacker.CritChance = 0f;
        attacker.ElementId = (int)Element.Neutral;
        attacker.AttackType = DamageType.Physical;
        attacker.PhysicalDamageRatio = 0.5f;
        attacker.ElementalDamageRatio = 0.5f;

        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f);
        target.Evasion = 0f;
        target.PhysicalDefense = 0f;
        target.MagicDefense = 100f;

        // Explicit damageType call (used by effect paths) should honor the supplied lane.
        var (damage, _, _) = SimDamage.Calculate(
            100f,
            DamageType.Magic,
            attacker,
            target,
            null,
            null,
            _state.Rng);

        AssertThat(damage).IsEqual(50f);
    }

    [TestCase]
    public void Calculate_PureElementalProfile_UsesMagicLane()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        attacker.CritChance = 0f;
        attacker.ElementId = (int)Element.Neutral;
        attacker.AttackType = DamageType.Magic;
        attacker.PhysicalDamageRatio = 0f;
        attacker.ElementalDamageRatio = 1f;

        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f);
        target.Evasion = 0f;
        target.PhysicalDefense = 100f; // ignored
        target.MagicDefense = 50f;     // 100 / 150 = 0.666...

        var (damage, _, _) = SimDamage.Calculate(
            100f, DamageType.Magic, attacker, target, null, null, _state.Rng);

        AssertThat(damage).IsEqual(66.7f);
    }

    [TestCase]
    public void Calculate_SummonerElementalBonus_AppliesForMatchingElement()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        attacker.CritChance = 0f;
        attacker.ElementId = (int)Element.Fire;

        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f);
        target.Evasion = 0f;
        target.ElementId = (int)Element.Neutral;

        _state.Summoners[0].SetElementalDamageBonus(Element.Fire, 20f);

        var (damage, _, _) = SimDamage.Calculate(
            100f, DamageType.Physical, attacker, target, _state.Summoners[0], null, _state.Rng);

        AssertThat(damage).IsEqual(120f);
    }

    [TestCase]
    public void Calculate_SummonerElementalBonus_DoesNotApplyForNonMatchingElement()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        attacker.CritChance = 0f;
        attacker.ElementId = (int)Element.Water;

        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f);
        target.Evasion = 0f;
        target.ElementId = (int)Element.Neutral;

        _state.Summoners[0].SetElementalDamageBonus(Element.Fire, 20f);

        var (damage, _, _) = SimDamage.Calculate(
            100f, DamageType.Physical, attacker, target, _state.Summoners[0], null, _state.Rng);

        AssertThat(damage).IsEqual(100f);
    }

    [TestCase]
    public void Calculate_FullPipeline_MixedProfile_CorrectOrder()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, 0, x: -5f);
        attacker.CritChance = 0f;
        attacker.ElementId = (int)Element.Neutral;
        attacker.AttackType = DamageType.Physical;
        attacker.PhysicalDamageRatio = 0.5f;
        attacker.ElementalDamageRatio = 0.5f;

        var target = SimTestHelper.CreateMeleeUnit(_state, 1, x: 5f);
        target.Evasion = 0f;
        target.ElementId = (int)Element.Neutral;
        target.PhysicalDefense = 100f; // 0.5x
        target.MagicDefense = 300f;    // 0.25x

        _state.Summoners[0].DamageBonus = 10f;
        _state.Summoners[1].DamageReduction = 5f;

        // base=100 -> bonus=110
        // split: 55 physical -> 27.5 after armor
        //        55 elemental -> 13.75 after magic resist
        // subtotal=41.25 -> reduction=36.25 -> round=36.2 (midpoint-to-even)
        var (damage, isCrit, _) = SimDamage.CalculateAttack(
            100f,
            attacker,
            target,
            _state.Summoners[0],
            _state.Summoners[1],
            _state.Rng);

        AssertThat(isCrit).IsFalse();
        AssertThat(damage).IsEqual(36.2f);
    }
}
