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
public class SimTargetingCommitTest
{
    private const float Delta = 1f / 60f;
    private MatchState _state = null!;

    [BeforeTest]
    public void Setup()
    {
        _state = SimTestHelper.CreateBattleState();
    }

    [TestCase]
    public void CommitLock_DoesNotRetarget_OnNearbySpawn()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 0f, z: 0f, attackRange: 3f, aggroRadius: 20f);
        var lockedEnemy = SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 2.4f, z: 0f);

        unit.CombatLifecycleState = CombatLifecycleState.AcquireTarget;
        unit.LockedTargetUnitId = lockedEnemy.UnitId;
        unit.TargetUnitId = lockedEnemy.UnitId;

        SimCombatStateMachine.Tick(unit, _state, Delta, new List<SimEvent>());

        // Nearby spawn should not break the lock while current target remains valid.
        SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 1.3f, z: 0f);
        SimCombatStateMachine.Tick(unit, _state, Delta, new List<SimEvent>());

        AssertThat(unit.TargetUnitId.HasValue).IsTrue();
        AssertThat(unit.TargetUnitId!.Value).IsEqual(lockedEnemy.UnitId);
        AssertThat(unit.LockedTargetUnitId.HasValue).IsTrue();
        AssertThat(unit.LockedTargetUnitId!.Value).IsEqual(lockedEnemy.UnitId);
    }

    [TestCase]
    public void CommitLock_DropsTarget_WhenTargetLeavesAggroRadius()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 0f, z: 0f, attackRange: 2.5f, aggroRadius: 6f);
        var lockedEnemy = SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 5f, z: 0f);

        unit.CombatLifecycleState = CombatLifecycleState.AcquireTarget;
        unit.LockedTargetUnitId = lockedEnemy.UnitId;
        unit.TargetUnitId = lockedEnemy.UnitId;

        lockedEnemy.Position = new SimVector3(20f, lockedEnemy.Position.Y, lockedEnemy.Position.Z);
        SimCombatStateMachine.Tick(unit, _state, Delta, new List<SimEvent>());

        AssertThat(unit.TargetUnitId.HasValue && unit.TargetUnitId!.Value == lockedEnemy.UnitId).IsFalse();
        AssertThat(unit.LockedTargetUnitId.HasValue && unit.LockedTargetUnitId!.Value == lockedEnemy.UnitId).IsFalse();
        AssertThat(unit.DroppedTargetUnitId.HasValue).IsTrue();
        AssertThat(unit.DroppedTargetUnitId!.Value).IsEqual(lockedEnemy.UnitId);
    }

    [TestCase]
    public void SummonerCommit_Persists_UntilInvalidForcedOrUnreachable()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 0f, z: 0f, attackRange: 3f, aggroRadius: 30f);
        unit.CombatLifecycleState = CombatLifecycleState.AcquireTarget;

        int summonerTarget = MatchState.GetSummonerTargetId(team: 1);
        unit.LockedTargetUnitId = summonerTarget;
        unit.TargetUnitId = summonerTarget;

        SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 0.8f, z: 0f);
        SimCombatStateMachine.Tick(unit, _state, Delta, new List<SimEvent>());

        AssertThat(unit.TargetUnitId.HasValue).IsTrue();
        AssertThat(unit.TargetUnitId!.Value).IsEqual(summonerTarget);
        AssertThat(unit.LockedTargetUnitId.HasValue).IsTrue();
        AssertThat(unit.LockedTargetUnitId!.Value).IsEqual(summonerTarget);
    }

    [TestCase]
    public void AcquireTargetCommit_DoesNotPrelockSummoner_WhenEnemyUnitsAliveOutsideAggro()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: -18f, z: 0f, attackRange: 2.5f, aggroRadius: 8f);
        SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 0f, z: 0f); // alive enemy exists but outside aggro

        int? target = SimTargeting.AcquireTargetCommit(
            unit,
            _state,
            currentTargetId: null,
            droppedTargetId: null,
            droppedTargetCooldownTimer: 0f);

        AssertThat(target.HasValue).IsFalse();
    }

    [TestCase]
    public void AcquireTargetCommit_PrefersEnemyUnit_WhenEnemyInAggro()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 0f, z: 0f, attackRange: 2.5f, aggroRadius: 12f);
        var enemy = SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 3f, z: 0f);

        int? target = SimTargeting.AcquireTargetCommit(
            unit,
            _state,
            currentTargetId: null,
            droppedTargetId: null,
            droppedTargetCooldownTimer: 0f);

        AssertThat(target.HasValue).IsTrue();
        AssertThat(target!.Value).IsEqual(enemy.UnitId);
    }

    [TestCase]
    public void CommitTick_UsesSlots_ForSummonerTargets()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 14f, z: 0f, attackRange: 2.5f, aggroRadius: 20f);
        unit.CombatLifecycleState = CombatLifecycleState.AcquireTarget;

        SimCombatStateMachine.Tick(unit, _state, Delta, new List<SimEvent>());

        int summonerTarget = MatchState.GetSummonerTargetId(team: 1);
        AssertThat(unit.TargetUnitId.HasValue).IsTrue();
        AssertThat(unit.TargetUnitId!.Value).IsEqual(summonerTarget);
        AssertThat(unit.SlotTargetId.HasValue).IsTrue();
        AssertThat(unit.SlotTargetId!.Value).IsEqual(summonerTarget);
        AssertThat(unit.ReservedSlotId.HasValue).IsTrue();

        var slotPos = Fateforged.Simulation.Combat.Slots.SimMeleeSlotManager.GetReservedSlotWorldPosition(unit, _state);
        AssertThat(slotPos.HasValue).IsTrue();
        float dx = slotPos!.Value.X - _state.Summoners[1].Position.X;
        float dz = slotPos.Value.Z - _state.Summoners[1].Position.Z;
        float dist = MathF.Sqrt((dx * dx) + (dz * dz));
        AssertThat(dist).IsLessEqual(unit.AttackRange + 0.05f);
    }

    [TestCase]
    public void AcquireTargetCommit_FallsBackToSummoner_WhenOnlyInAggroUnitIsSlotSaturated()
    {
        var saturatedTarget = SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 17f, z: 0f, hp: 600f);
        saturatedTarget.NavigationRadius = 0.2f;

        var blockerOne = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 15.5f, z: -0.5f);
        var blockerTwo = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 15.5f, z: 0f);
        var blockerThree = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 15.5f, z: 0.5f);
        AssertThat(SimMeleeSlotManager.TryReserveSlot(blockerOne, _state, saturatedTarget.UnitId, out _)).IsTrue();
        AssertThat(SimMeleeSlotManager.TryReserveSlot(blockerTwo, _state, saturatedTarget.UnitId, out _)).IsTrue();
        AssertThat(SimMeleeSlotManager.TryReserveSlot(blockerThree, _state, saturatedTarget.UnitId, out _)).IsTrue();

        var overflow = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 15f, z: 1.2f, attackRange: 2f, aggroRadius: 8f);

        int? target = SimTargeting.AcquireTargetCommit(
            overflow,
            _state,
            currentTargetId: null,
            droppedTargetId: null,
            droppedTargetCooldownTimer: 0f);

        AssertThat(target.HasValue).IsTrue();
        AssertThat(MatchState.IsSummonerTarget(target)).IsTrue();
        AssertThat(target!.Value).IsEqual(MatchState.GetSummonerTargetId(team: 1));
    }
}
