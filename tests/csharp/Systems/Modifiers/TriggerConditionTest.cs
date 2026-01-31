namespace ProjectSummoner.Tests.Systems.Modifiers;

using System;
using GdUnit4;
using ProjectSummoner.Systems.Modifiers;
using static GdUnit4.Assertions;

/// <summary>
/// Tests for TriggerCondition enum.
/// </summary>
[TestSuite]
public class TriggerConditionTest
{
    [TestCase]
    public void TriggerCondition_HasExpectedValues()
    {
        // Verify all expected trigger conditions exist
        AssertThat(Enum.IsDefined(typeof(TriggerCondition), TriggerCondition.Always)).IsTrue();
        AssertThat(Enum.IsDefined(typeof(TriggerCondition), TriggerCondition.OnHit)).IsTrue();
        AssertThat(Enum.IsDefined(typeof(TriggerCondition), TriggerCondition.OnTakeHit)).IsTrue();
        AssertThat(Enum.IsDefined(typeof(TriggerCondition), TriggerCondition.OnKill)).IsTrue();
        AssertThat(Enum.IsDefined(typeof(TriggerCondition), TriggerCondition.OnDeath)).IsTrue();
        AssertThat(Enum.IsDefined(typeof(TriggerCondition), TriggerCondition.BelowHpPercent)).IsTrue();
        AssertThat(Enum.IsDefined(typeof(TriggerCondition), TriggerCondition.AboveHpPercent)).IsTrue();
        AssertThat(Enum.IsDefined(typeof(TriggerCondition), TriggerCondition.Periodic)).IsTrue();
    }

    [TestCase]
    public void TriggerCondition_AlwaysIsDefault()
    {
        TriggerCondition condition = default;

        AssertThat(condition).IsEqual(TriggerCondition.Always);
    }

    [TestCase]
    public void TriggerCondition_ParsesFromString()
    {
        var parsed = Enum.Parse<TriggerCondition>("BelowHpPercent", ignoreCase: true);

        AssertThat(parsed).IsEqual(TriggerCondition.BelowHpPercent);
    }

    [TestCase]
    public void TriggerCondition_ParsesFromString_CaseInsensitive()
    {
        var parsed = Enum.Parse<TriggerCondition>("onkill", ignoreCase: true);

        AssertThat(parsed).IsEqual(TriggerCondition.OnKill);
    }

    [TestCase]
    public void TriggerCondition_ConvertsToString()
    {
        var str = TriggerCondition.OnTakeHit.ToString();

        AssertThat(str).IsEqual("OnTakeHit");
    }
}
