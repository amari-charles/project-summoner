namespace ProjectSummoner.Tests.Stats;

using GdUnit4;
using ProjectSummoner.Stats;
using static GdUnit4.Assertions;

/// <summary>
/// Tests for StatKey enum and extensions.
/// </summary>
[TestSuite]
public class StatKeyTest
{
    [TestCase]
    public void ToSnakeCase_ConvertsCorrectly()
    {
        AssertThat(StatKey.MaxHp.ToSnakeCase()).IsEqual("max_hp");
        AssertThat(StatKey.AttackDamage.ToSnakeCase()).IsEqual("attack_damage");
        AssertThat(StatKey.AttackSpeed.ToSnakeCase()).IsEqual("attack_speed");
        AssertThat(StatKey.MoveSpeed.ToSnakeCase()).IsEqual("move_speed");
        AssertThat(StatKey.AttackRange.ToSnakeCase()).IsEqual("attack_range");
        AssertThat(StatKey.AggroRadius.ToSnakeCase()).IsEqual("aggro_radius");
    }

    [TestCase]
    public void ToPascalCase_ConvertsCorrectly()
    {
        AssertThat(StatKey.MaxHp.ToPascalCase()).IsEqual("MaxHp");
        AssertThat(StatKey.AttackDamage.ToPascalCase()).IsEqual("AttackDamage");
        AssertThat(StatKey.MoveSpeed.ToPascalCase()).IsEqual("MoveSpeed");
    }

    [TestCase]
    public void FromString_ParsesSnakeCase()
    {
        AssertThat(StatKeyExtensions.FromString("max_hp")).IsEqual(StatKey.MaxHp);
        AssertThat(StatKeyExtensions.FromString("attack_damage")).IsEqual(StatKey.AttackDamage);
        AssertThat(StatKeyExtensions.FromString("move_speed")).IsEqual(StatKey.MoveSpeed);
    }

    [TestCase]
    public void FromString_ParsesPascalCase()
    {
        AssertThat(StatKeyExtensions.FromString("MaxHp")).IsEqual(StatKey.MaxHp);
        AssertThat(StatKeyExtensions.FromString("AttackDamage")).IsEqual(StatKey.AttackDamage);
    }

    [TestCase]
    public void FromString_ReturnsNullForInvalidKey()
    {
        AssertThat(StatKeyExtensions.FromString("invalid_key")).IsNull();
        AssertThat(StatKeyExtensions.FromString("")).IsNull();
        AssertThat(StatKeyExtensions.FromString(null)).IsNull();
    }

    [TestCase]
    public void IsValidStatKey_ReturnsTrueForValidKeys()
    {
        AssertThat(StatKeyExtensions.IsValidStatKey("max_hp")).IsTrue();
        AssertThat(StatKeyExtensions.IsValidStatKey("attack_damage")).IsTrue();
        AssertThat(StatKeyExtensions.IsValidStatKey("MaxHp")).IsTrue();
    }

    [TestCase]
    public void IsValidStatKey_ReturnsFalseForInvalidKeys()
    {
        AssertThat(StatKeyExtensions.IsValidStatKey("invalid")).IsFalse();
        AssertThat(StatKeyExtensions.IsValidStatKey("")).IsFalse();
        AssertThat(StatKeyExtensions.IsValidStatKey(null)).IsFalse();
    }

    [TestCase]
    public void GetDefault_ReturnsCorrectDefaults()
    {
        AssertThat(StatKey.MaxHp.GetDefault()).IsEqual(100f);
        AssertThat(StatKey.AttackDamage.GetDefault()).IsEqual(10f);
        AssertThat(StatKey.AttackSpeed.GetDefault()).IsEqual(1f);
        AssertThat(StatKey.MoveSpeed.GetDefault()).IsEqual(3f);
        AssertThat(StatKey.AttackRange.GetDefault()).IsEqual(2f);
        AssertThat(StatKey.AggroRadius.GetDefault()).IsEqual(20f);
    }

    [TestCase]
    public void All_ContainsAllStatKeys()
    {
        var all = StatKeyExtensions.All;

        AssertThat(all.Length).IsEqual(6);
        AssertThat(all).Contains(StatKey.MaxHp);
        AssertThat(all).Contains(StatKey.AttackDamage);
        AssertThat(all).Contains(StatKey.AttackSpeed);
        AssertThat(all).Contains(StatKey.MoveSpeed);
        AssertThat(all).Contains(StatKey.AttackRange);
        AssertThat(all).Contains(StatKey.AggroRadius);
    }

    [TestCase]
    public void RoundTrip_SnakeCaseToEnumToSnakeCase()
    {
        foreach (var key in StatKeyExtensions.All)
        {
            var snakeCase = key.ToSnakeCase();
            var parsed = StatKeyExtensions.FromString(snakeCase);
            AssertThat(parsed).IsEqual(key);
        }
    }
}
