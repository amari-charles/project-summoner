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
        unit.Engagement.LifecycleState = CombatLifecycleState.ApproachTarget;
        unit.Engagement.ForcedTargetUnitId = 99;

        AssertThat(unit.Engagement.TargetUnitId.HasValue).IsTrue();
        AssertThat(unit.Engagement.TargetUnitId!.Value).IsEqual(42);
        AssertThat(unit.Engagement.LifecycleState).IsEqual(CombatLifecycleState.ApproachTarget);
        AssertThat(unit.Engagement.ForcedTargetUnitId.HasValue).IsTrue();
        AssertThat(unit.Engagement.ForcedTargetUnitId!.Value).IsEqual(99);
    }

    [TestCase]
    public void EngagementDefaults_ExposePriorTimeoutContracts()
    {
        var unit = new UnitData();

        AssertThat(unit.Engagement.UnreachableTimeoutSeconds).IsEqual(1.2f);
        AssertThat(unit.Engagement.DroppedTargetCooldownSeconds).IsEqual(0.75f);
    }
}
