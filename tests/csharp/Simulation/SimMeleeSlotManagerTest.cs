namespace Fateforged.Tests.Simulation;

using System.Collections.Generic;
using Fateforged.Simulation;
using Fateforged.Simulation.Combat;
using Fateforged.Simulation.Combat.Slots;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class SimMeleeSlotManagerTest
{
    private const float Delta = 1f / 60f;
    private MatchState _state = null!;

    [BeforeTest]
    public void Setup()
    {
        _state = SimTestHelper.CreateBattleState();
    }

    [TestCase]
    public void SlotOverflow_WaitsThenReacquires_ByTimeoutOrder()
    {
        var lockedTarget = SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 2f, z: 0f, hp: 600f);
        lockedTarget.NavigationRadius = 0.2f;
        var fallbackTarget = SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 4f, z: 0f, hp: 600f);

        var blockerOne = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 0f, z: -0.5f);
        var blockerTwo = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 0f, z: 0f);
        var blockerThree = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 0f, z: 0.5f);
        AssertThat(SimMeleeSlotManager.TryReserveSlot(blockerOne, _state, lockedTarget.UnitId, out _)).IsTrue();
        AssertThat(SimMeleeSlotManager.TryReserveSlot(blockerTwo, _state, lockedTarget.UnitId, out _)).IsTrue();
        AssertThat(SimMeleeSlotManager.TryReserveSlot(blockerThree, _state, lockedTarget.UnitId, out _)).IsTrue();

        var seeker = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: -1.5f, z: 0f, attackRange: 2.5f, aggroRadius: 25f);
        seeker.CombatLifecycleState = CombatLifecycleState.AcquireTarget;
        seeker.LockedTargetUnitId = lockedTarget.UnitId;
        seeker.TargetUnitId = lockedTarget.UnitId;

        float switchedAtSeconds = -1f;
        for (int frame = 1; frame <= 120; frame++)
        {
            SimCombatStateMachine.Tick(seeker, _state, Delta, new List<SimEvent>());
            if (seeker.TargetUnitId.HasValue && seeker.TargetUnitId.Value != lockedTarget.UnitId)
            {
                switchedAtSeconds = frame * Delta;
                break;
            }
        }

        AssertThat(switchedAtSeconds > 0f).IsTrue();
        AssertThat(switchedAtSeconds).IsLess(seeker.UnreachableTimeoutSeconds);
        AssertThat(switchedAtSeconds).IsGreaterEqual(seeker.SlotWaitTimeoutSeconds);
        AssertThat(_state.CombatBlockedTimeoutRetargetCount).IsEqual(1);
        AssertThat(seeker.DroppedTargetUnitId.HasValue).IsTrue();
        AssertThat(seeker.DroppedTargetUnitId!.Value).IsEqual(lockedTarget.UnitId);
        AssertThat(seeker.TargetUnitId.HasValue).IsTrue();
        AssertThat(seeker.TargetUnitId!.Value).IsEqual(fallbackTarget.UnitId);
    }

    [TestCase]
    public void ReservedSlot_CannotBeStolen_ByAnotherUnit()
    {
        var target = SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 2f, z: 0f);
        target.NavigationRadius = 0.2f;
        var first = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 0f, z: 0f);
        var second = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 0.1f, z: 0f);

        bool reservedFirst = SimMeleeSlotManager.TryReserveSlot(first, _state, target.UnitId, out int firstSlot, minSlots: 1);
        bool reservedSecond = SimMeleeSlotManager.TryReserveSlot(second, _state, target.UnitId, out int _, minSlots: 1);

        AssertThat(reservedFirst).IsTrue();
        AssertThat(reservedSecond).IsFalse();

        var slotState = _state.TargetSlotStates[target.UnitId];
        var slot = slotState.Slots[firstSlot];
        AssertThat(slot.OccupancyState).IsEqual(SlotOccupancyState.Reserved);
        AssertThat(slot.ReservedUnitId.HasValue).IsTrue();
        AssertThat(slot.ReservedUnitId!.Value).IsEqual(first.UnitId);
    }

    [TestCase]
    public void SlotTieBreak_UsesDistanceThenUnitId()
    {
        var target = SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 2f, z: 0f);
        var unit = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 0f, z: 0f);

        bool reserved = SimMeleeSlotManager.TryReserveSlot(unit, _state, target.UnitId, out int slotId, minSlots: 1);

        AssertThat(reserved).IsTrue();
        var slot = _state.TargetSlotStates[target.UnitId].Slots[slotId];
        AssertThat(slot.ReservationUnitId).IsEqual(unit.UnitId);
        AssertThat(slot.ReservationDistanceSq < float.MaxValue).IsTrue();
    }
}
