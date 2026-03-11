namespace Fateforged.Tests.Simulation;

using System.Collections.Generic;
using Fateforged.Simulation;
using Fateforged.Simulation.Combat;
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
}
