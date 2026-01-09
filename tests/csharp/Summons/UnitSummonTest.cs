namespace ProjectSummoner.Tests.Summons;

using GdUnit4;
using static GdUnit4.Assertions;

/// <summary>
/// Tests for UnitSummon class.
/// Note: UnitSummon inherits from RefCounted which requires Godot runtime.
/// These tests verify compile-time contracts and API signatures.
/// Full runtime tests should be run through Godot editor.
/// </summary>
[TestSuite]
public class UnitSummonTest
{
    [TestCase]
    public void UnitSummon_HasExpectedProperties()
    {
        // Verify properties exist via reflection (compile-time contract check)
        var type = typeof(ProjectSummoner.Summons.UnitSummon);

        AssertThat(type.GetProperty("CatalogId")).IsNotNull();
        AssertThat(type.GetProperty("InstanceId")).IsNotNull();
        AssertThat(type.GetProperty("Team")).IsNotNull();
        AssertThat(type.GetProperty("CreatedAtMsec")).IsNotNull();
        AssertThat(type.GetProperty("AliveCount")).IsNotNull();
        AssertThat(type.GetProperty("DeadCount")).IsNotNull();
        AssertThat(type.GetProperty("TotalCount")).IsNotNull();
        AssertThat(type.GetProperty("HasAliveUnits")).IsNotNull();
        AssertThat(type.GetProperty("AllDead")).IsNotNull();
    }

    [TestCase]
    public void UnitSummon_HasExpectedMethods()
    {
        var type = typeof(ProjectSummoner.Summons.UnitSummon);

        AssertThat(type.GetMethod("AddUnit")).IsNotNull();
        AssertThat(type.GetMethod("RemoveUnit")).IsNotNull();
        AssertThat(type.GetMethod("GetAliveUnits")).IsNotNull();
        AssertThat(type.GetMethod("GetDeadUnits")).IsNotNull();
        AssertThat(type.GetMethod("GetAllUnits")).IsNotNull();
        AssertThat(type.GetMethod("GetAliveUnitsArray")).IsNotNull();
        AssertThat(type.GetMethod("KillAll")).IsNotNull();
    }

    [TestCase]
    public void UnitSummon_HasSignals()
    {
        // Verify signal delegates exist
        var type = typeof(ProjectSummoner.Summons.UnitSummon);

        AssertThat(type.GetNestedType("UnitAddedEventHandler")).IsNotNull();
        AssertThat(type.GetNestedType("UnitDiedEventHandler")).IsNotNull();
        AssertThat(type.GetNestedType("AllUnitsDiedEventHandler")).IsNotNull();
    }

    [TestCase]
    public void UnitSummon_HasConstructor()
    {
        var type = typeof(ProjectSummoner.Summons.UnitSummon);

        // Should have constructor with (string, string, int) params
        var constructor = type.GetConstructor(new[] { typeof(string), typeof(string), typeof(int) });
        AssertThat(constructor).IsNotNull();
    }
}
