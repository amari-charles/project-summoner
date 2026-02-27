namespace ProjectSummoner.Tests.Systems.Modifiers;

using System.Collections.Generic;
using GdUnit4;
using ProjectSummoner.Stats;
using ProjectSummoner.Systems.Modifiers;
using static GdUnit4.Assertions;

/// <summary>
/// Tests for StatModifier, including trigger fields.
/// </summary>
// [TestSuite] — requires Godot runtime; run via editor's gdUnit4 panel
public class StatModifierTest
{
    [TestCase]
    public void StatModifier_DefaultsToAlwaysTrigger()
    {
        var mod = new StatModifier();

        AssertThat(mod.Trigger).IsEqual(TriggerCondition.Always);
        AssertThat(mod.IsTriggered).IsFalse();
    }

    [TestCase]
    public void StatModifier_IsTriggered_ReturnsTrueForNonAlwaysCondition()
    {
        var mod = new StatModifier
        {
            Trigger = TriggerCondition.OnHit
        };

        AssertThat(mod.IsTriggered).IsTrue();
    }

    [TestCase]
    public void StatModifier_ToDictionary_IncludesTriggerFields()
    {
        var mod = new StatModifier
        {
            Source = "test_source",
            Trigger = TriggerCondition.BelowHpPercent,
            TriggerThreshold = 0.5f,
            TriggerDuration = 10.0f,
            TriggerCooldown = 2.0f
        };

        var dict = mod.ToDictionary();

        AssertThat(dict["source"].AsString()).IsEqual("test_source");
        AssertThat(dict["trigger"].AsString()).IsEqual("BelowHpPercent");
        AssertThat(dict["trigger_threshold"].AsSingle()).IsEqual(0.5f);
        AssertThat(dict["trigger_duration"].AsSingle()).IsEqual(10.0f);
        AssertThat(dict["trigger_cooldown"].AsSingle()).IsEqual(2.0f);
    }

    [TestCase]
    public void StatModifier_ToDictionary_OmitsTriggerFieldsWhenDefault()
    {
        var mod = new StatModifier
        {
            Source = "test_source",
            StatAdds = new Dictionary<StatKey, float> { [StatKey.AttackDamage] = 5.0f }
        };

        var dict = mod.ToDictionary();

        AssertThat(dict.ContainsKey("trigger")).IsFalse();
        AssertThat(dict.ContainsKey("trigger_threshold")).IsFalse();
        AssertThat(dict.ContainsKey("trigger_duration")).IsFalse();
        AssertThat(dict.ContainsKey("trigger_cooldown")).IsFalse();
    }

    [TestCase]
    public void StatModifier_FromDictionary_ParsesTriggerFields()
    {
        var dict = new Godot.Collections.Dictionary
        {
            ["source"] = "test_source",
            ["trigger"] = "OnKill",
            ["trigger_threshold"] = 0.25f,
            ["trigger_duration"] = 5.0f,
            ["trigger_cooldown"] = 1.0f
        };

        var mod = StatModifier.FromDictionary(dict);

        AssertThat(mod.Trigger).IsEqual(TriggerCondition.OnKill);
        AssertThat(mod.TriggerThreshold).IsEqual(0.25f);
        AssertThat(mod.TriggerDuration).IsEqual(5.0f);
        AssertThat(mod.TriggerCooldown).IsEqual(1.0f);
    }

    [TestCase]
    public void StatModifier_FromDictionary_HandlesInvalidTriggerGracefully()
    {
        var dict = new Godot.Collections.Dictionary
        {
            ["source"] = "test_source",
            ["trigger"] = "InvalidTrigger"
        };

        var mod = StatModifier.FromDictionary(dict);

        // Should stay at default (Always) when parse fails
        AssertThat(mod.Trigger).IsEqual(TriggerCondition.Always);
    }

    [TestCase]
    public void StatModifier_RoundTrips_WithAllFields()
    {
        var original = new StatModifier
        {
            Source = "test_source",
            CardInstanceId = "card_123",
            Tags = new List<string> { "fire", "damage" },
            Conditions = new Dictionary<string, object> { [ConditionKeys.ElementalAffinity] = "fire" },
            StatAdds = new Dictionary<StatKey, float> { [StatKey.AttackDamage] = 10.0f },
            StatMults = new Dictionary<StatKey, float> { [StatKey.AttackSpeed] = 1.2f },
            Flags = new Dictionary<string, bool> { ["immune_slow"] = true },
            Trigger = TriggerCondition.OnTakeHit,
            TriggerDuration = 5.0f,
            TriggerCooldown = 1.5f
        };

        var dict = original.ToDictionary();
        var restored = StatModifier.FromDictionary(dict);

        AssertThat(restored.Source).IsEqual(original.Source);
        AssertThat(restored.CardInstanceId).IsEqual(original.CardInstanceId);
        AssertThat(restored.Trigger).IsEqual(original.Trigger);
        AssertThat(restored.TriggerDuration).IsEqual(original.TriggerDuration);
        AssertThat(restored.TriggerCooldown).IsEqual(original.TriggerCooldown);
        // Verify stat keys round-trip correctly
        AssertThat(restored.StatAdds.ContainsKey(StatKey.AttackDamage)).IsTrue();
        AssertThat(restored.StatMults.ContainsKey(StatKey.AttackSpeed)).IsTrue();
    }
}
