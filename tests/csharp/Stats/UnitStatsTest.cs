namespace ProjectSummoner.Tests.Stats;

using System.Collections.Generic;
using GdUnit4;
using ProjectSummoner.Stats;
using static GdUnit4.Assertions;

/// <summary>
/// Tests for UnitStats class.
/// </summary>
[TestSuite]
public class UnitStatsTest
{
    [TestCase]
    public void Default_HasCorrectValues()
    {
        var stats = UnitStats.Default;

        AssertThat(stats.MaxHp).IsEqual(100f);
        AssertThat(stats.AttackDamage).IsEqual(10f);
        AssertThat(stats.AttackSpeed).IsEqual(1f);
        AssertThat(stats.MoveSpeed).IsEqual(3f);
        AssertThat(stats.AttackRange).IsEqual(2f);
        AssertThat(stats.AggroRadius).IsEqual(20f);
        AssertThat(stats.CritChance).IsEqual(0f);
        AssertThat(stats.CritDamage).IsEqual(1.5f);
        AssertThat(stats.Armor).IsEqual(0f);
        AssertThat(stats.MagicResist).IsEqual(0f);
    }

    [TestCase]
    public void Get_ReturnsCorrectValue()
    {
        var stats = new UnitStats
        {
            MaxHp = 150f,
            AttackDamage = 25f
        };

        AssertThat(stats.Get(StatKey.MaxHp)).IsEqual(150f);
        AssertThat(stats.Get(StatKey.AttackDamage)).IsEqual(25f);
    }

    [TestCase]
    public void With_CreatesNewInstanceWithModifiedValue()
    {
        var original = UnitStats.Default;
        var modified = original.With(StatKey.MaxHp, 200f);

        // Original unchanged
        AssertThat(original.MaxHp).IsEqual(100f);
        // New instance has new value
        AssertThat(modified.MaxHp).IsEqual(200f);
        // Other stats unchanged
        AssertThat(modified.AttackDamage).IsEqual(10f);
    }

    [TestCase]
    public void WithMultiplied_MultipliesCorrectly()
    {
        var stats = new UnitStats { MaxHp = 100f };
        var multiplied = stats.WithMultiplied(StatKey.MaxHp, 1.5f);

        AssertThat(multiplied.MaxHp).IsEqual(150f);
    }

    [TestCase]
    public void WithAdded_AddsCorrectly()
    {
        var stats = new UnitStats { MaxHp = 100f };
        var added = stats.WithAdded(StatKey.MaxHp, 50f);

        AssertThat(added.MaxHp).IsEqual(150f);
    }

    [TestCase]
    public void FromDictionary_ParsesSnakeCaseKeys()
    {
        var dict = new Dictionary<string, float>
        {
            ["max_hp"] = 200f,
            ["attack_damage"] = 30f,
            ["attack_speed"] = 1.5f,
            ["move_speed"] = 4f,
            ["attack_range"] = 3f,
            ["aggro_radius"] = 25f,
            ["crit_chance"] = 0.25f,
            ["crit_damage"] = 2.0f,
            ["armor"] = 10f,
            ["magic_resist"] = 5f
        };

        var stats = UnitStats.FromDictionary(dict);

        AssertThat(stats.MaxHp).IsEqual(200f);
        AssertThat(stats.AttackDamage).IsEqual(30f);
        AssertThat(stats.AttackSpeed).IsEqual(1.5f);
        AssertThat(stats.MoveSpeed).IsEqual(4f);
        AssertThat(stats.AttackRange).IsEqual(3f);
        AssertThat(stats.AggroRadius).IsEqual(25f);
        AssertThat(stats.CritChance).IsEqual(0.25f);
        AssertThat(stats.CritDamage).IsEqual(2.0f);
        AssertThat(stats.Armor).IsEqual(10f);
        AssertThat(stats.MagicResist).IsEqual(5f);
    }

    [TestCase]
    public void FromDictionary_UsesDefaultsForMissingKeys()
    {
        var dict = new Dictionary<string, float>
        {
            ["max_hp"] = 200f
        };

        var stats = UnitStats.FromDictionary(dict);

        AssertThat(stats.MaxHp).IsEqual(200f);
        AssertThat(stats.AttackDamage).IsEqual(10f); // Default
        AssertThat(stats.AttackSpeed).IsEqual(1f);   // Default
    }

    [TestCase]
    public void FromDictionary_HandlesNullGracefully()
    {
        var stats = UnitStats.FromDictionary(null);

        AssertThat(stats.MaxHp).IsEqual(100f);
        AssertThat(stats.AttackDamage).IsEqual(10f);
    }


    [TestCase]
    public void WithModifiers_AppliesAddsBeforeMults()
    {
        var stats = new UnitStats { MaxHp = 100f };

        var modifiers = new List<StatModifier>
        {
            new StatModifier
            {
                StatAdds = new Dictionary<StatKey, float> { [StatKey.MaxHp] = 20f }
            },
            new StatModifier
            {
                StatMults = new Dictionary<StatKey, float> { [StatKey.MaxHp] = 1.5f }
            }
        };

        var modified = stats.WithModifiers(modifiers);

        // (100 + 20) * 1.5 = 180
        AssertThat(modified.MaxHp).IsEqual(180f);
    }

    [TestCase]
    public void WithModifiers_CombinesMultipleAddsMults()
    {
        var stats = new UnitStats { MaxHp = 100f };

        var modifiers = new List<StatModifier>
        {
            new StatModifier
            {
                StatAdds = new Dictionary<StatKey, float> { [StatKey.MaxHp] = 10f },
                StatMults = new Dictionary<StatKey, float> { [StatKey.MaxHp] = 1.1f }
            },
            new StatModifier
            {
                StatAdds = new Dictionary<StatKey, float> { [StatKey.MaxHp] = 10f },
                StatMults = new Dictionary<StatKey, float> { [StatKey.MaxHp] = 1.1f }
            }
        };

        var modified = stats.WithModifiers(modifiers);

        // (100 + 10 + 10) * (1.1 * 1.1) = 120 * 1.21 = 145.2
        AssertThat(modified.MaxHp).IsEqualApprox(145.2f, 0.01f);
    }

    [TestCase]
    public void WithModifiers_HandlesNullGracefully()
    {
        var stats = new UnitStats { MaxHp = 100f };
        var modified = stats.WithModifiers(null);

        AssertThat(modified.MaxHp).IsEqual(100f);
    }

    [TestCase]
    public void WithModifiers_HandlesEmptyListGracefully()
    {
        var stats = new UnitStats { MaxHp = 100f };
        var modified = stats.WithModifiers(new List<StatModifier>());

        AssertThat(modified.MaxHp).IsEqual(100f);
    }

    [TestCase]
    public void WithUpgradeMultipliers_AppliesCorrectly()
    {
        var stats = new UnitStats
        {
            MaxHp = 100f,
            AttackDamage = 10f
        };

        var multipliers = new Dictionary<string, float>
        {
            ["max_hp"] = 1.2f,
            ["attack_damage"] = 1.5f
        };

        var upgraded = stats.WithUpgradeMultipliers(multipliers);

        AssertThat(upgraded.MaxHp).IsEqualApprox(120f, 0.01f);
        AssertThat(upgraded.AttackDamage).IsEqualApprox(15f, 0.01f);
    }

    [TestCase]
    public void WithOverrides_ReplacesValues()
    {
        var stats = new UnitStats { MaxHp = 100f };

        var overrides = new Dictionary<string, float>
        {
            ["max_hp"] = 500f
        };

        var overridden = stats.WithOverrides(overrides);

        AssertThat(overridden.MaxHp).IsEqual(500f);
    }

    [TestCase]
    public void ToString_FormatsNicely()
    {
        var stats = new UnitStats
        {
            MaxHp = 100f,
            AttackDamage = 10f
        };

        var str = stats.ToString();

        AssertThat(str).Contains("HP=100");
        AssertThat(str).Contains("ATK=10");
    }

    [TestCase]
    public void CritStats_GetReturnsCorrectValues()
    {
        var stats = new UnitStats
        {
            CritChance = 0.25f,
            CritDamage = 2.5f
        };

        AssertThat(stats.Get(StatKey.CritChance)).IsEqual(0.25f);
        AssertThat(stats.Get(StatKey.CritDamage)).IsEqual(2.5f);
    }

    [TestCase]
    public void CritStats_WithCreatesModifiedInstance()
    {
        var original = UnitStats.Default;
        var withCritChance = original.With(StatKey.CritChance, 0.5f);
        var withCritDamage = original.With(StatKey.CritDamage, 3.0f);

        AssertThat(original.CritChance).IsEqual(0f);
        AssertThat(original.CritDamage).IsEqual(1.5f);
        AssertThat(withCritChance.CritChance).IsEqual(0.5f);
        AssertThat(withCritDamage.CritDamage).IsEqual(3.0f);
    }

    [TestCase]
    public void DefensiveStats_GetReturnsCorrectValues()
    {
        var stats = new UnitStats
        {
            Armor = 15f,
            MagicResist = 20f
        };

        AssertThat(stats.Get(StatKey.Armor)).IsEqual(15f);
        AssertThat(stats.Get(StatKey.MagicResist)).IsEqual(20f);
    }

    [TestCase]
    public void DefensiveStats_WithCreatesModifiedInstance()
    {
        var original = UnitStats.Default;
        var withArmor = original.With(StatKey.Armor, 25f);
        var withMagicResist = original.With(StatKey.MagicResist, 30f);

        AssertThat(original.Armor).IsEqual(0f);
        AssertThat(original.MagicResist).IsEqual(0f);
        AssertThat(withArmor.Armor).IsEqual(25f);
        AssertThat(withMagicResist.MagicResist).IsEqual(30f);
    }

    [TestCase]
    public void ToString_IncludesCritStats()
    {
        var stats = new UnitStats
        {
            CritChance = 0.25f,
            CritDamage = 2.0f
        };

        var str = stats.ToString();

        AssertThat(str).Contains("CRIT=25%/2.0x");
    }

    [TestCase]
    public void ToString_IncludesDefensiveStats()
    {
        var stats = new UnitStats
        {
            Armor = 10f,
            MagicResist = 15f
        };

        var str = stats.ToString();

        AssertThat(str).Contains("ARM=10");
        AssertThat(str).Contains("MR=15");
    }
}
