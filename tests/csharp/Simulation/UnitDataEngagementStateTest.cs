namespace Fateforged.Tests.Simulation;

using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class UnitDataEngagementStateTest
{
    [TestCase]
    public void LegacyCompatibilityProperties_MapToEngagementState()
    {
        var unit = new UnitData();

        unit.Engagement.TargetUnitId = 42;
        unit.Engagement.LifecycleState = CombatLifecycleState.MoveToSlot;
        unit.Engagement.SlotTargetId = 88;
        unit.Engagement.ForcedTargetUnitId = 99;

        AssertThat(unit.Engagement.TargetUnitId.HasValue).IsTrue();
        AssertThat(unit.Engagement.TargetUnitId!.Value).IsEqual(42);
        AssertThat(unit.Engagement.LifecycleState).IsEqual(CombatLifecycleState.MoveToSlot);
        AssertThat(unit.Engagement.SlotTargetId.HasValue).IsTrue();
        AssertThat(unit.Engagement.SlotTargetId!.Value).IsEqual(88);
        AssertThat(unit.Engagement.ForcedTargetUnitId.HasValue).IsTrue();
        AssertThat(unit.Engagement.ForcedTargetUnitId!.Value).IsEqual(99);
    }

    [TestCase]
    public void EngagementDefaults_ExposePriorTimeoutContracts()
    {
        var unit = new UnitData();

        AssertThat(unit.Engagement.UnreachableTimeoutSeconds).IsEqual(1.2f);
        AssertThat(unit.Engagement.SlotWaitTimeoutSeconds).IsEqual(0.7f);
        AssertThat(unit.Engagement.DroppedTargetCooldownSeconds).IsEqual(0.75f);
    }
}
