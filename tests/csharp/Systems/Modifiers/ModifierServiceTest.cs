namespace ProjectSummoner.Tests.Systems.Modifiers;

using System.Collections.Generic;
using GdUnit4;
using ProjectSummoner.Stats;
using ProjectSummoner.Systems.Modifiers;
using static GdUnit4.Assertions;

/// <summary>
/// Tests for ModifierService, including trigger partitioning.
/// </summary>
[TestSuite]
public class ModifierServiceTest
{
    /// <summary>
    /// Test provider that returns a fixed list of modifiers.
    /// </summary>
    private class TestModifierProvider : IModifierProvider
    {
        public string ProviderId { get; }
        public List<StatModifier> Modifiers { get; }

        public TestModifierProvider(string id, List<StatModifier> modifiers)
        {
            ProviderId = id;
            Modifiers = modifiers;
        }

        public List<StatModifier> GetModifiers() => Modifiers;
    }

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

        var result = ModifierService.ApplyModifiers(baseStats, modifiers);

        // HP: 100 + 50 = 150 (no mult)
        AssertThat(result.MaxHp).IsEqual(150f);
        // Damage: 10 * 1.2 = 12 (no add)
        AssertThat(result.AttackDamage).IsEqual(12f);
        // Unchanged
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

        var result = ModifierService.ApplyModifiers(baseStats, modifiers);

        // Damage: 10 * 1.1 * 1.2 = 13.2
        AssertThat(result.AttackDamage).IsEqual(13.2f);
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

        var result = ModifierService.ApplyModifiers(baseStats, modifiers);

        // HP: (100 + 100) * 1.5 = 300
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

        var result = ModifierService.ApplyModifiers(baseStats, modifiers);

        AssertThat(result.Flags.ContainsKey("immune_slow")).IsTrue();
        AssertThat(result.Flags["immune_slow"]).IsTrue();
        AssertThat(result.Flags.ContainsKey("deals_fire_damage")).IsTrue();
    }
}
