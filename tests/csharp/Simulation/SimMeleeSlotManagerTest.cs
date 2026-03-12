namespace Fateforged.Tests.Simulation;

using System;
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
        SummonerMeleeBubble.ClearOverrideRadius();
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
        AssertThat(
                SimMeleeSlotManager.TryReserveSlot(blockerOne, _state, lockedTarget.UnitId, out _)
            )
            .IsTrue();
        AssertThat(
                SimMeleeSlotManager.TryReserveSlot(blockerTwo, _state, lockedTarget.UnitId, out _)
            )
            .IsTrue();
        AssertThat(
                SimMeleeSlotManager.TryReserveSlot(blockerThree, _state, lockedTarget.UnitId, out _)
            )
            .IsTrue();

        var seeker = SimTestHelper.CreateMeleeUnit(
            _state,
            team: 0,
            x: -1.5f,
            z: 0f,
            attackRange: 2.5f,
            aggroRadius: 25f
        );
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

        bool reservedFirst = SimMeleeSlotManager.TryReserveSlot(
            first,
            _state,
            target.UnitId,
            out int firstSlot,
            minSlots: 1
        );
        bool reservedSecond = SimMeleeSlotManager.TryReserveSlot(
            second,
            _state,
            target.UnitId,
            out int _,
            minSlots: 1
        );

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

        bool reserved = SimMeleeSlotManager.TryReserveSlot(
            unit,
            _state,
            target.UnitId,
            out int slotId,
            minSlots: 1
        );

        AssertThat(reserved).IsTrue();
        var slot = _state.TargetSlotStates[target.UnitId].Slots[slotId];
        AssertThat(slot.ReservationUnitId).IsEqual(unit.UnitId);
        AssertThat(slot.ReservationDistanceSq < float.MaxValue).IsTrue();
    }

    [TestCase]
    public void ReservedSlot_StaysOnAttackerFacingSide()
    {
        var target = SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 4f, z: 0f);
        var attacker = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 0f, z: 0f);

        bool reserved = SimMeleeSlotManager.TryReserveSlot(attacker, _state, target.UnitId, out _);
        AssertThat(reserved).IsTrue();

        var slotPos = SimMeleeSlotManager.GetReservedSlotWorldPosition(attacker, _state);
        AssertThat(slotPos.HasValue).IsTrue();
        // Attacker is left of target, so attacker-facing frontage should also be left of target.
        AssertThat(slotPos!.Value.X).IsLess(target.Position.X);
    }

    [TestCase]
    public void SummonerSlotTopology_UsesSharedBubbleRadiusOverride()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 14f, z: 0f, attackRange: 6f);
        int summonerTarget = MatchState.GetSummonerTargetId(team: 1);

        var baselineState = SimMeleeSlotManager.GetOrCreateTargetState(_state, summonerTarget, attacker, minSlots: 1);
        int baselineSlotCount = baselineState.Slots.Count;

        SummonerMeleeBubble.SetOverrideRadius(7.2f);
        var overriddenState = SimMeleeSlotManager.GetOrCreateTargetState(_state, summonerTarget, attacker, minSlots: 1);

        AssertThat(overriddenState.Slots.Count).IsGreater(baselineSlotCount);
    }

    [TestCase]
    public void SummonerSlotTopology_RebuildsWhenBubbleRadiusChangesWithoutSlotCountDelta()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 14f, z: 0f, attackRange: 6f);
        attacker.NavigationRadius = 1.2f;
        int summonerTarget = MatchState.GetSummonerTargetId(team: 1);

        SummonerMeleeBubble.SetOverrideRadius(5.40f);
        var baselineState = SimMeleeSlotManager.GetOrCreateTargetState(_state, summonerTarget, attacker, minSlots: 1);
        int baselineSlotCount = baselineState.Slots.Count;
        float baselineRadius = baselineState.Slots[0].SlotOffset.Length();

        SummonerMeleeBubble.SetOverrideRadius(5.45f);
        var updatedState = SimMeleeSlotManager.GetOrCreateTargetState(_state, summonerTarget, attacker, minSlots: 1);
        float updatedRadius = updatedState.Slots[0].SlotOffset.Length();

        AssertThat(updatedState.Slots.Count).IsEqual(baselineSlotCount);
        AssertThat(updatedRadius).IsGreater(baselineRadius + 0.03f);
    }

    [TestCase]
    public void TryReserveSlot_ExcludedSlotId_PicksDifferentFreeSlot()
    {
        var target = SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 4f, z: 0f);
        var unit = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 0f, z: 0f);

        bool reserved = SimMeleeSlotManager.TryReserveSlot(unit, _state, target.UnitId, out int initialSlot, minSlots: 3);
        AssertThat(reserved).IsTrue();

        SimMeleeSlotManager.ReleaseUnitSlots(unit, _state);

        bool reservedDifferent = SimMeleeSlotManager.TryReserveSlot(
            unit,
            _state,
            target.UnitId,
            out int reboundSlot,
            minSlots: 3,
            excludedSlotId: initialSlot
        );
        AssertThat(reservedDifferent).IsTrue();
        AssertThat(reboundSlot).IsNotEqual(initialSlot);
    }

    [TestCase]
    public void SummonerSlots_UseDeterministicWorldAxis()
    {
        int summonerTarget = MatchState.GetSummonerTargetId(team: 1);
        var attackerA = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 14f, z: -3f, attackRange: 3f);
        var attackerB = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 17f, z: 4f, attackRange: 3f);

        AssertThat(SimMeleeSlotManager.TryReserveSlot(attackerA, _state, summonerTarget, out _)).IsTrue();
        AssertThat(SimMeleeSlotManager.TryReserveSlot(attackerB, _state, summonerTarget, out _)).IsTrue();

        var slotState = SimMeleeSlotManager.GetOrCreateTargetState(_state, summonerTarget, attackerA);
        AssertThat(MathF.Abs(slotState.LayoutAxis.X - (-1f))).IsLess(0.0001f);
        AssertThat(MathF.Abs(slotState.LayoutAxis.Z)).IsLess(0.0001f);

        // Move attackers to a different centroid shape; axis should remain stable.
        attackerA.Position = new SimVector3(19f, 0f, 5f);
        attackerB.Position = new SimVector3(19f, 0f, -5f);
        slotState = SimMeleeSlotManager.GetOrCreateTargetState(_state, summonerTarget, attackerA);
        AssertThat(MathF.Abs(slotState.LayoutAxis.X - (-1f))).IsLess(0.0001f);
        AssertThat(MathF.Abs(slotState.LayoutAxis.Z)).IsLess(0.0001f);
    }

    [TestCase]
    public void SummonerSlots_UseBubbleFrontage_NotAttackRangeClamp()
    {
        int summonerTarget = MatchState.GetSummonerTargetId(team: 1);
        var attacker = SimTestHelper.CreateMeleeUnit(
            _state,
            team: 0,
            x: 14f,
            z: 0f,
            attackRange: 3f
        );

        bool reserved = SimMeleeSlotManager.TryReserveSlot(attacker, _state, summonerTarget, out _);
        AssertThat(reserved).IsTrue();

        var slotPos = SimMeleeSlotManager.GetReservedSlotWorldPosition(attacker, _state);
        AssertThat(slotPos.HasValue).IsTrue();

        var summonerPos = _state.Summoners[1].Position;
        float dx = slotPos!.Value.X - summonerPos.X;
        float dz = slotPos.Value.Z - summonerPos.Z;
        float slotRadius = MathF.Sqrt((dx * dx) + (dz * dz));

        AssertThat(slotRadius).IsGreater(SummonerMeleeBubble.EffectiveRadius - 0.05f);
        AssertThat(slotRadius).IsGreater(attacker.AttackRange);
    }
}
