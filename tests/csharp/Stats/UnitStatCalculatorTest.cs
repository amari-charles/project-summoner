namespace Fateforged.Tests.Stats;

using System.Collections.Generic;
using GdUnit4;
using Fateforged.Cards;
using Fateforged.Stats;
using static GdUnit4.Assertions;

/// <summary>
/// Tests for UnitStatCalculator.
/// </summary>
[TestSuite]
public class UnitStatCalculatorTest
{
    private static CardDefinition CreateTestCard(
        float maxHp = 100f,
        float attackDamage = 10f,
        float attackSpeed = 1f,
        float moveSpeed = 3f,
        float attackRange = 2f,
        float aggroRadius = 20f)
    {
        return new CardDefinition
        {
            Id = new CardId("test_card"),
            Name = "Test Card",
            Description = "A test card",
            Rarity = Rarity.Common,
            Type = CardType.Summon,
            ManaCost = 3,
            MaxHp = maxHp,
            AttackDamage = attackDamage,
            AttackSpeed = attackSpeed,
            MoveSpeed = moveSpeed,
            AttackRange = attackRange,
            AggroRadius = aggroRadius
        };
    }

    [TestCase]
    public void Calculate_WithOnlyBaseStats_ReturnsCardValues()
    {
        var card = CreateTestCard(maxHp: 150f, attackDamage: 25f);

        var stats = UnitStatCalculator.Calculate(card);

        AssertThat(stats.MaxHp).IsEqual(150f);
        AssertThat(stats.AttackDamage).IsEqual(25f);
    }

    [TestCase]
    public void Calculate_WithUpgradeMultipliers_AppliesMultiplicative()
    {
        var card = CreateTestCard(maxHp: 100f);
        var upgrades = new Dictionary<string, float>
        {
            ["max_hp"] = 1.2f // +20%
        };

        var stats = UnitStatCalculator.Calculate(card, upgrades);

        AssertThat(stats.MaxHp).IsEqualApprox(120f, 0.01f);
    }

    [TestCase]
    public void Calculate_WithModifiers_AppliesAfterUpgrades()
    {
        var card = CreateTestCard(maxHp: 100f);
        var upgrades = new Dictionary<string, float>
        {
            ["max_hp"] = 1.2f // 100 * 1.2 = 120
        };
        var modifiers = new List<StatModifier>
        {
            new StatModifier
            {
                StatAdds = new Dictionary<StatKey, float> { [StatKey.MaxHp] = 10f }
            },
            new StatModifier
            {
                StatMults = new Dictionary<StatKey, float> { [StatKey.MaxHp] = 1.1f }
            }
        };

        var stats = UnitStatCalculator.Calculate(card, upgrades, modifiers);

        // (120 + 10) * 1.1 = 143
        AssertThat(stats.MaxHp).IsEqual(143f);
    }

    [TestCase]
    public void Calculate_WithOverrides_ReplacesAfterModifiers()
    {
        var card = CreateTestCard(maxHp: 100f);
        var modifiers = new List<StatModifier>
        {
            new StatModifier
            {
                StatMults = new Dictionary<StatKey, float> { [StatKey.MaxHp] = 2.0f }
            }
        };
        var overrides = new Dictionary<string, float>
        {
            ["max_hp"] = 500f
        };

        var stats = UnitStatCalculator.Calculate(card, null, modifiers, overrides);

        // Override replaces regardless of modifier result
        AssertThat(stats.MaxHp).IsEqual(500f);
    }

    [TestCase]
    public void Calculate_FullPipeline_AppliesInCorrectOrder()
    {
        // Base: 100
        // Upgrade: 100 * 1.2 = 120
        // Add: 120 + 10 = 130
        // Mult: 130 * 1.1 = 143
        // Override: (none)

        var card = CreateTestCard(maxHp: 100f);
        var upgrades = new Dictionary<string, float>
        {
            ["max_hp"] = 1.2f
        };
        var modifiers = new List<StatModifier>
        {
            new StatModifier
            {
                StatAdds = new Dictionary<StatKey, float> { [StatKey.MaxHp] = 10f },
                StatMults = new Dictionary<StatKey, float> { [StatKey.MaxHp] = 1.1f }
            }
        };

        var stats = UnitStatCalculator.Calculate(card, upgrades, modifiers);

        AssertThat(stats.MaxHp).IsEqual(143f);
    }

    [TestCase]
    public void FromCardDefinition_ExtractsAllStats()
    {
        var card = CreateTestCard(
            maxHp: 150f,
            attackDamage: 25f,
            attackSpeed: 1.5f,
            moveSpeed: 4f,
            attackRange: 3f,
            aggroRadius: 25f
        );

        var stats = UnitStatCalculator.FromCardDefinition(card);

        AssertThat(stats.MaxHp).IsEqual(150f);
        AssertThat(stats.AttackDamage).IsEqual(25f);
        AssertThat(stats.AttackSpeed).IsEqual(1.5f);
        AssertThat(stats.MoveSpeed).IsEqual(4f);
        AssertThat(stats.AttackRange).IsEqual(3f);
        AssertThat(stats.AggroRadius).IsEqual(25f);
    }

    [TestCase]
    public void FromCardDefinition_UsesDefaultsForZeroValues()
    {
        var card = new CardDefinition
        {
            Id = new CardId("test"),
            Name = "Test",
            Description = "Test",
            Rarity = Rarity.Common,
            Type = CardType.Summon,
            ManaCost = 1,
            // MaxHp defaults to 0 if not set
            MaxHp = 0f,
            AttackDamage = 0f
        };

        var stats = UnitStatCalculator.FromCardDefinition(card);

        // Should use defaults when 0
        AssertThat(stats.MaxHp).IsEqual(100f);
        AssertThat(stats.AttackDamage).IsEqual(10f);
    }

    [TestCase]
    public void CalculateFromDictionary_WorksWithEffectiveStats()
    {
        // Simulates stats coming from PlayerCardService.get_effective_stats()
        var effectiveStats = new Dictionary<string, float>
        {
            ["max_hp"] = 120f,  // Already includes upgrades
            ["attack_damage"] = 15f
        };

        var stats = UnitStatCalculator.CalculateFromDictionary(effectiveStats);

        AssertThat(stats.MaxHp).IsEqualApprox(120f, 0.01f);
        AssertThat(stats.AttackDamage).IsEqualApprox(15f, 0.01f);
    }

    [TestCase]
    public void CalculateFromDictionary_AppliesModifiers()
    {
        var effectiveStats = new Dictionary<string, float>
        {
            ["max_hp"] = 100f
        };
        var modifiers = new List<StatModifier>
        {
            new StatModifier
            {
                StatMults = new Dictionary<StatKey, float> { [StatKey.MaxHp] = 1.5f }
            }
        };

        var stats = UnitStatCalculator.CalculateFromDictionary(effectiveStats, modifiers);

        AssertThat(stats.MaxHp).IsEqual(150f);
    }

    [TestCase]
    public void ValidateStatsDictionary_ReturnsWarningsForMissingKeys()
    {
        var dict = new Dictionary<string, float>
        {
            ["max_hp"] = 100f
            // Missing other stat keys
        };

        var warnings = UnitStatCalculator.ValidateStatsDictionary(dict);

        AssertThat(warnings.Count).IsGreater(0);
        AssertThat(string.Join(" ", warnings)).Contains("attack_damage");
    }

    [TestCase]
    public void ValidateStatsDictionary_ReturnsWarningsForUnknownKeys()
    {
        var dict = new Dictionary<string, float>
        {
            ["max_hp"] = 100f,
            ["unknown_stat"] = 50f
        };

        var warnings = UnitStatCalculator.ValidateStatsDictionary(dict);

        AssertThat(string.Join(" ", warnings)).Contains("unknown_stat");
    }

    // =========================================================================
    // ApplyModifiers tests (relocated from ModifierServiceTest)
    // =========================================================================

    [TestCase]
    public void ApplyModifiers_AppliesAdditiveAndMultiplicativeBonuses()
    {
        var baseStats = new BaseStats
        {
            MaxHp = 100f,
            AttackDamage = 10f,
            AttackSpeed = 1f,
            MoveSpeed = 3f
        };

        var modifiers = new List<StatModifier>
        {
            new StatModifier
            {
                Source = "test",
                StatAdds = new Dictionary<StatKey, float> { [StatKey.MaxHp] = 50f },
                StatMults = new Dictionary<StatKey, float> { [StatKey.AttackDamage] = 1.2f }
            }
        };

        var result = UnitStatCalculator.ApplyModifiers(baseStats, modifiers);

        AssertThat(result.MaxHp).IsEqual(150f);
        AssertThat(result.AttackDamage).IsEqual(12f);
        AssertThat(result.AttackSpeed).IsEqual(1f);
        AssertThat(result.MoveSpeed).IsEqual(3f);
    }

    [TestCase]
    public void ApplyModifiers_StacksMultipliers()
    {
        var baseStats = new BaseStats
        {
            MaxHp = 100f,
            AttackDamage = 10f,
            AttackSpeed = 1f,
            MoveSpeed = 3f
        };

        var modifiers = new List<StatModifier>
        {
            new StatModifier
            {
                Source = "mod1",
                StatMults = new Dictionary<StatKey, float> { [StatKey.AttackDamage] = 1.1f }
            },
            new StatModifier
            {
                Source = "mod2",
                StatMults = new Dictionary<StatKey, float> { [StatKey.AttackDamage] = 1.2f }
            }
        };

        var result = UnitStatCalculator.ApplyModifiers(baseStats, modifiers);

        AssertThat(result.AttackDamage).IsEqualApprox(13.2f, 0.01f);
    }

    [TestCase]
    public void ApplyModifiers_AddsBeforeMultiplies()
    {
        var baseStats = new BaseStats
        {
            MaxHp = 100f,
            AttackDamage = 10f,
            AttackSpeed = 1f,
            MoveSpeed = 3f
        };

        var modifiers = new List<StatModifier>
        {
            new StatModifier
            {
                Source = "test",
                StatAdds = new Dictionary<StatKey, float> { [StatKey.MaxHp] = 100f },
                StatMults = new Dictionary<StatKey, float> { [StatKey.MaxHp] = 1.5f }
            }
        };

        var result = UnitStatCalculator.ApplyModifiers(baseStats, modifiers);

        AssertThat(result.MaxHp).IsEqual(300f);
    }

    [TestCase]
    public void ApplyModifiers_CollectsFlags()
    {
        var baseStats = new BaseStats
        {
            MaxHp = 100f,
            AttackDamage = 10f,
            AttackSpeed = 1f,
            MoveSpeed = 3f
        };

        var modifiers = new List<StatModifier>
        {
            new StatModifier
            {
                Source = "test",
                Flags = new Dictionary<string, bool>
                {
                    ["immune_slow"] = true,
                    ["deals_fire_damage"] = true
                }
            }
        };

        var result = UnitStatCalculator.ApplyModifiers(baseStats, modifiers);

        AssertThat(result.Flags.ContainsKey("immune_slow")).IsTrue();
        AssertThat(result.Flags["immune_slow"]).IsTrue();
        AssertThat(result.Flags.ContainsKey("deals_fire_damage")).IsTrue();
    }

    [TestCase]
    public void ValidateStatsDictionary_IgnoresKnownNonStatKeys()
    {
        var dict = new Dictionary<string, float>
        {
            ["max_hp"] = 100f,
            ["attack_damage"] = 10f,
            ["attack_speed"] = 1f,
            ["move_speed"] = 3f,
            ["attack_range"] = 2f,
            ["aggro_radius"] = 20f,
            ["mana_cost"] = 3f,        // Known non-stat key
            ["spawn_count"] = 5f,       // Known non-stat key
            ["scale_multiplier"] = 1.5f // Known override key
        };

        var warnings = UnitStatCalculator.ValidateStatsDictionary(dict);

        // Should have no warnings about known keys
        foreach (var warning in warnings)
        {
            AssertThat(warning).IsNotEqual("Unknown stat key: mana_cost");
            AssertThat(warning).IsNotEqual("Unknown stat key: spawn_count");
            AssertThat(warning).IsNotEqual("Unknown stat key: scale_multiplier");
        }
    }
}
