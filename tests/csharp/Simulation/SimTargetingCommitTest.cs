namespace Fateforged.Tests.Simulation;

using System;
using System.Collections.Generic;
using Fateforged.Simulation;
using Fateforged.Simulation.Combat;
using Fateforged.Simulation.Combat.Slots;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Units;
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
        SummonerMeleeBubble.ClearOverrideRadius();
    }

    [TestCase]
    public void CommitLock_DoesNotRetarget_OnNearbySpawn()
    {
        var unit = SimTestHelper.CreateMeleeUnit(
            _state,
            team: 0,
            x: 0f,
            z: 0f,
            attackRange: 3f,
            aggroRadius: 20f
        );
        var lockedEnemy = SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 2.4f, z: 0f);

        unit.Engagement.LifecycleState = CombatLifecycleState.AcquireTarget;
        unit.Engagement.LockedTargetUnitId = lockedEnemy.UnitId;
        unit.Engagement.TargetUnitId = lockedEnemy.UnitId;

        SimCombatStateMachine.Tick(unit, _state, Delta, new List<SimEvent>());

        // Nearby spawn should not break the lock while current target remains valid.
        SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 1.3f, z: 0f);
        SimCombatStateMachine.Tick(unit, _state, Delta, new List<SimEvent>());

        AssertThat(unit.Engagement.TargetUnitId.HasValue).IsTrue();
        AssertThat(unit.Engagement.TargetUnitId!.Value).IsEqual(lockedEnemy.UnitId);
        AssertThat(unit.Engagement.LockedTargetUnitId.HasValue).IsTrue();
        AssertThat(unit.Engagement.LockedTargetUnitId!.Value).IsEqual(lockedEnemy.UnitId);
    }

    [TestCase]
    public void CommitLock_DropsTarget_WhenTargetLeavesAggroRadius()
    {
        var unit = SimTestHelper.CreateMeleeUnit(
            _state,
            team: 0,
            x: 0f,
            z: 0f,
            attackRange: 2.5f,
            aggroRadius: 6f
        );
        var lockedEnemy = SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 5f, z: 0f);

        unit.Engagement.LifecycleState = CombatLifecycleState.AcquireTarget;
        unit.Engagement.LockedTargetUnitId = lockedEnemy.UnitId;
        unit.Engagement.TargetUnitId = lockedEnemy.UnitId;

        lockedEnemy.Position = new SimVector3(20f, lockedEnemy.Position.Y, lockedEnemy.Position.Z);
        SimCombatStateMachine.Tick(unit, _state, Delta, new List<SimEvent>());

        AssertThat(unit.Engagement.TargetUnitId.HasValue && unit.Engagement.TargetUnitId!.Value == lockedEnemy.UnitId)
            .IsFalse();
        AssertThat(
                unit.Engagement.LockedTargetUnitId.HasValue
                    && unit.Engagement.LockedTargetUnitId!.Value == lockedEnemy.UnitId
            )
            .IsFalse();
        AssertThat(unit.Engagement.DroppedTargetUnitId.HasValue).IsTrue();
        AssertThat(unit.Engagement.DroppedTargetUnitId!.Value).IsEqual(lockedEnemy.UnitId);
    }

    [TestCase]
    public void SummonerCommit_PreemptsToInAggroUnit_WithinOneTick()
    {
        var unit = SimTestHelper.CreateMeleeUnit(
            _state,
            team: 0,
            x: 0f,
            z: 0f,
            attackRange: 3f,
            aggroRadius: 30f
        );
        unit.Engagement.LifecycleState = CombatLifecycleState.AcquireTarget;

        int summonerTarget = MatchState.GetSummonerTargetId(team: 1);
        unit.Engagement.LockedTargetUnitId = summonerTarget;
        unit.Engagement.TargetUnitId = summonerTarget;

        var enemy = SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 0.8f, z: 0f);
        SimCombatStateMachine.Tick(unit, _state, Delta, new List<SimEvent>());

        AssertThat(unit.Engagement.TargetUnitId.HasValue).IsTrue();
        AssertThat(unit.Engagement.TargetUnitId!.Value).IsEqual(enemy.UnitId);
        AssertThat(unit.Engagement.LockedTargetUnitId.HasValue).IsTrue();
        AssertThat(unit.Engagement.LockedTargetUnitId!.Value).IsEqual(enemy.UnitId);
        AssertThat(unit.Engagement.LastRetargetReason).IsEqual(RetargetReason.AggroPreempt);
    }

    [TestCase]
    public void CommitTick_DirectMeleeEngagement_DoesNotReserveSlots()
    {
        var unit = SimTestHelper.CreateMeleeUnit(
            _state,
            team: 0,
            x: 0f,
            z: 0f,
            attackRange: 3f,
            aggroRadius: 20f
        );
        unit.Attack.Rules.MeleeEngagementModel = MeleeEngagementModel.Direct;

        var enemy = SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 2f, z: 0f);

        unit.Engagement.LifecycleState = CombatLifecycleState.AcquireTarget;
        unit.Engagement.LockedTargetUnitId = enemy.UnitId;
        unit.Engagement.TargetUnitId = enemy.UnitId;
        unit.AttackCooldown = 0f;

        SimCombatStateMachine.Tick(unit, _state, Delta, new List<SimEvent>());

        AssertThat(unit.Engagement.SlotTargetId.HasValue).IsFalse();
        AssertThat(unit.Engagement.ReservedSlotId.HasValue).IsFalse();
        AssertThat(unit.Engagement.OccupiedSlotId.HasValue).IsFalse();
        AssertThat(_state.TargetSlotStates.Count).IsEqual(0);
    }

    [TestCase]
    public void CommitTick_SlotRingMeleeEngagement_ReservesSlotAgainstUnitTarget()
    {
        var unit = SimTestHelper.CreateMeleeUnit(
            _state,
            team: 0,
            x: 0f,
            z: 0f,
            attackRange: 3f,
            aggroRadius: 20f,
            meleeEngagementModel: MeleeEngagementModel.SlotRing
        );

        var enemy = SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 2f, z: 0f);

        unit.Engagement.LifecycleState = CombatLifecycleState.AcquireTarget;
        unit.Engagement.LockedTargetUnitId = enemy.UnitId;
        unit.Engagement.TargetUnitId = enemy.UnitId;
        unit.AttackCooldown = 0f;

        SimCombatStateMachine.Tick(unit, _state, Delta, new List<SimEvent>());

        AssertThat(unit.Engagement.SlotTargetId.HasValue).IsTrue();
        AssertThat(unit.Engagement.SlotTargetId!.Value).IsEqual(enemy.UnitId);
        AssertThat(unit.Engagement.ReservedSlotId.HasValue).IsTrue();
    }

    [TestCase]
    public void SummonerCommit_DoesNotPreempt_WhenEnemyOutsideAggro()
    {
        var unit = SimTestHelper.CreateMeleeUnit(
            _state,
            team: 0,
            x: 0f,
            z: 0f,
            attackRange: 3f,
            aggroRadius: 4f
        );
        unit.Engagement.LifecycleState = CombatLifecycleState.AcquireTarget;

        int summonerTarget = MatchState.GetSummonerTargetId(team: 1);
        unit.Engagement.LockedTargetUnitId = summonerTarget;
        unit.Engagement.TargetUnitId = summonerTarget;

        SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 8f, z: 0f);
        SimCombatStateMachine.Tick(unit, _state, Delta, new List<SimEvent>());

        AssertThat(unit.Engagement.TargetUnitId.HasValue).IsTrue();
        AssertThat(unit.Engagement.TargetUnitId!.Value).IsEqual(summonerTarget);
        AssertThat(unit.Engagement.LockedTargetUnitId.HasValue).IsTrue();
        AssertThat(unit.Engagement.LockedTargetUnitId!.Value).IsEqual(summonerTarget);
    }

    [TestCase]
    public void SummonerCommit_DoesNotPreempt_WhenLayerFilterRejectsCandidate()
    {
        var unit = SimTestHelper.CreateMeleeUnit(
            _state,
            team: 0,
            x: 0f,
            z: 0f,
            attackRange: 3f,
            aggroRadius: 20f
        );
        unit.Engagement.LifecycleState = CombatLifecycleState.AcquireTarget;
        unit.TargetLayerFilter = TargetLayer.AirOnly;

        int summonerTarget = MatchState.GetSummonerTargetId(team: 1);
        unit.Engagement.LockedTargetUnitId = summonerTarget;
        unit.Engagement.TargetUnitId = summonerTarget;

        SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 1f, z: 0f); // Ground-only candidate.
        SimCombatStateMachine.Tick(unit, _state, Delta, new List<SimEvent>());

        AssertThat(unit.Engagement.TargetUnitId.HasValue).IsTrue();
        AssertThat(unit.Engagement.TargetUnitId!.Value).IsEqual(summonerTarget);
        AssertThat(unit.Engagement.LockedTargetUnitId.HasValue).IsTrue();
        AssertThat(unit.Engagement.LockedTargetUnitId!.Value).IsEqual(summonerTarget);
    }

    [TestCase]
    public void SummonerCommit_DoesNotPreempt_WhenForcedTargetActive()
    {
        var unit = SimTestHelper.CreateMeleeUnit(
            _state,
            team: 0,
            x: 0f,
            z: 0f,
            attackRange: 3f,
            aggroRadius: 20f
        );
        unit.Engagement.LifecycleState = CombatLifecycleState.AcquireTarget;

        int summonerTarget = MatchState.GetSummonerTargetId(team: 1);
        unit.Engagement.LockedTargetUnitId = summonerTarget;
        unit.Engagement.TargetUnitId = summonerTarget;
        unit.Engagement.ForcedTargetUnitId = summonerTarget;
        unit.Engagement.ForcedTargetTimer = 2f;

        SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 1f, z: 0f);
        SimCombatStateMachine.Tick(unit, _state, Delta, new List<SimEvent>());

        AssertThat(unit.Engagement.TargetUnitId.HasValue).IsTrue();
        AssertThat(unit.Engagement.TargetUnitId!.Value).IsEqual(summonerTarget);
        AssertThat(unit.Engagement.LockedTargetUnitId.HasValue).IsTrue();
        AssertThat(unit.Engagement.LockedTargetUnitId!.Value).IsEqual(summonerTarget);
        AssertThat(unit.Engagement.LastRetargetReason).IsEqual(RetargetReason.ForcedOverride);
    }

    [TestCase]
    public void SummonerCommit_DoesNotPreempt_DuringActiveAttackPhase()
    {
        var unit = SimTestHelper.CreateMeleeUnit(
            _state,
            team: 0,
            x: 16f,
            z: 0f,
            attackRange: 3f,
            aggroRadius: 20f
        );
        unit.Engagement.LifecycleState = CombatLifecycleState.AcquireTarget;
        unit.AttackPhase = AttackPhase.Windup;
        unit.AttackPhaseTimer = 0.4f;

        int summonerTarget = MatchState.GetSummonerTargetId(team: 1);
        unit.Engagement.LockedTargetUnitId = summonerTarget;
        unit.Engagement.TargetUnitId = summonerTarget;

        var enemy = SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 16.5f, z: 0f);
        SimCombatStateMachine.Tick(unit, _state, Delta, new List<SimEvent>());

        AssertThat(unit.Engagement.TargetUnitId.HasValue).IsTrue();
        AssertThat(unit.Engagement.TargetUnitId!.Value).IsEqual(summonerTarget);
        AssertThat(unit.Engagement.LockedTargetUnitId.HasValue).IsTrue();
        AssertThat(unit.Engagement.LockedTargetUnitId!.Value).IsEqual(summonerTarget);
        AssertThat(unit.Engagement.TargetUnitId!.Value == enemy.UnitId).IsFalse();
    }

    [TestCase]
    public void Tick_NewlyStartedWindup_DoesNotConsumeDeltaImmediately()
    {
        var attacker = SimTestHelper.CreateRangedUnit(
            _state,
            team: 0,
            x: 0f,
            z: 0f,
            attackRange: 8f,
            attackSpeed: 1f,
            projectileDelay: 0f
        );
        var target = SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 2f, z: 0f);

        attacker.Engagement.LifecycleState = CombatLifecycleState.AcquireTarget;
        attacker.Engagement.LockedTargetUnitId = target.UnitId;
        attacker.Engagement.TargetUnitId = target.UnitId;
        attacker.AttackCooldown = 0f;
        attacker.Attack.Timing.WindupSeconds = 0.3f;

        SimCombatStateMachine.Tick(attacker, _state, Delta, new List<SimEvent>());

        AssertThat(attacker.AttackPhase).IsEqual(AttackPhase.Windup);
        AssertThat(attacker.AttackPhaseTimer).IsEqual(0.3f);
    }

    [TestCase]
    public void AcquireTargetCommit_DoesNotPrelockSummoner_WhenEnemyUnitsAliveOutsideAggro()
    {
        var unit = SimTestHelper.CreateMeleeUnit(
            _state,
            team: 0,
            x: -18f,
            z: 0f,
            attackRange: 2.5f,
            aggroRadius: 8f
        );
        SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 0f, z: 0f); // alive enemy exists but outside aggro

        int? target = SimTargeting.AcquireTargetCommit(
            unit,
            _state,
            currentTargetId: null,
            droppedTargetId: null,
            droppedTargetCooldownTimer: 0f
        );

        AssertThat(target.HasValue).IsFalse();
    }

    [TestCase]
    public void AcquireTargetCommit_PrefersEnemyUnit_WhenEnemyInAggro()
    {
        var unit = SimTestHelper.CreateMeleeUnit(
            _state,
            team: 0,
            x: 0f,
            z: 0f,
            attackRange: 2.5f,
            aggroRadius: 12f
        );
        var enemy = SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 3f, z: 0f);

        int? target = SimTargeting.AcquireTargetCommit(
            unit,
            _state,
            currentTargetId: null,
            droppedTargetId: null,
            droppedTargetCooldownTimer: 0f
        );

        AssertThat(target.HasValue).IsTrue();
        AssertThat(target!.Value).IsEqual(enemy.UnitId);
    }

    [TestCase]
    public void CommitTick_UsesSlots_ForSummonerTargets_WhenNotYetAttackable()
    {
        var unit = SimTestHelper.CreateMeleeUnit(
            _state,
            team: 0,
            x: 10f,
            z: 0f,
            attackRange: 2.5f,
            aggroRadius: 20f,
            meleeEngagementModel: MeleeEngagementModel.SlotRing
        );
        unit.Engagement.LifecycleState = CombatLifecycleState.AcquireTarget;

        SimCombatStateMachine.Tick(unit, _state, Delta, new List<SimEvent>());

        int summonerTarget = MatchState.GetSummonerTargetId(team: 1);
        AssertThat(unit.Engagement.TargetUnitId.HasValue).IsTrue();
        AssertThat(unit.Engagement.TargetUnitId!.Value).IsEqual(summonerTarget);
        AssertThat(unit.Engagement.SlotTargetId.HasValue).IsTrue();
        AssertThat(unit.Engagement.SlotTargetId!.Value).IsEqual(summonerTarget);
        AssertThat(unit.Engagement.ReservedSlotId.HasValue).IsTrue();

        var slotPos =
            Fateforged.Simulation.Combat.Slots.SimMeleeSlotManager.GetReservedSlotWorldPosition(
                unit,
                _state
            );
        AssertThat(slotPos.HasValue).IsTrue();
        float dx = slotPos!.Value.X - _state.Summoners[1].Position.X;
        float dz = slotPos.Value.Z - _state.Summoners[1].Position.Z;
        float dist = MathF.Sqrt((dx * dx) + (dz * dz));
        AssertThat(dist).IsGreater(SummonerMeleeBubble.EffectiveRadius - 0.05f);
    }

    [TestCase]
    public void CommitTick_SummonerAlreadyAttackable_StartsAttackWithoutSlotGate()
    {
        var unit = SimTestHelper.CreateMeleeUnit(
            _state,
            team: 0,
            x: 18f,
            z: 0f,
            attackRange: 2.5f,
            aggroRadius: 20f,
            meleeEngagementModel: MeleeEngagementModel.SlotRing
        );
        unit.Engagement.LifecycleState = CombatLifecycleState.AcquireTarget;

        var events = new List<SimEvent>();
        SimCombatStateMachine.Tick(unit, _state, Delta, events);

        int summonerTarget = MatchState.GetSummonerTargetId(team: 1);
        AssertThat(unit.Engagement.TargetUnitId.HasValue).IsTrue();
        AssertThat(unit.Engagement.TargetUnitId!.Value).IsEqual(summonerTarget);
        AssertThat(unit.Engagement.SlotTargetId.HasValue).IsTrue();
        AssertThat(unit.Engagement.SlotTargetId!.Value).IsEqual(summonerTarget);
        // Slots may still be reserved opportunistically for ring shaping, but
        // attack startup is no longer gated on slot ownership.
        AssertThat(unit.AttackCooldown > 0f).IsTrue();
        AssertThat(SimTestHelper.FindEvent<SummonerDamagedEvent>(events)).IsNull();
    }

    [TestCase]
    public void CommitLock_ForcedTargetExpiry_DropsForcedCommit_AndReacquires()
    {
        var unit = SimTestHelper.CreateMeleeUnit(
            _state,
            team: 0,
            x: 0f,
            z: 0f,
            attackRange: 2.5f,
            aggroRadius: 12f
        );
        var naturalEnemy = SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 2f, z: 0f);
        var forcedEnemy = SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 8f, z: 0f);
        unit.Engagement.LifecycleState = CombatLifecycleState.AcquireTarget;
        unit.Engagement.ForcedTargetUnitId = forcedEnemy.UnitId;
        unit.Engagement.ForcedTargetTimer = 0.05f;

        SimCombatStateMachine.Tick(unit, _state, Delta, new List<SimEvent>());
        AssertThat(unit.Engagement.TargetUnitId.HasValue).IsTrue();
        AssertThat(unit.Engagement.TargetUnitId!.Value).IsEqual(forcedEnemy.UnitId);

        // Simulation tick order decrements forced timers before lifecycle targeting.
        SimBehavior.TickCooldowns(unit, 0.1f);
        SimCombatStateMachine.Tick(unit, _state, Delta, new List<SimEvent>());

        AssertThat(unit.Engagement.ForcedTargetUnitId).IsNull();
        AssertThat(unit.Engagement.LockedTargetUnitId.HasValue).IsTrue();
        AssertThat(unit.Engagement.LockedTargetUnitId!.Value).IsEqual(naturalEnemy.UnitId);
        AssertThat(unit.Engagement.TargetUnitId.HasValue).IsTrue();
        AssertThat(unit.Engagement.TargetUnitId!.Value).IsEqual(naturalEnemy.UnitId);
    }

    [TestCase]
    public void CommitLock_InvalidForcedTarget_ClearsAndReacquiresValidTarget()
    {
        var unit = SimTestHelper.CreateMeleeUnit(
            _state,
            team: 0,
            x: 0f,
            z: 0f,
            attackRange: 2.5f,
            aggroRadius: 12f
        );
        var forcedEnemy = SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 2f, z: 0f);
        var fallbackEnemy = SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 3f, z: 0f);
        unit.Engagement.LifecycleState = CombatLifecycleState.AcquireTarget;
        unit.Engagement.ForcedTargetUnitId = forcedEnemy.UnitId;
        unit.Engagement.ForcedTargetTimer = 5f;
        forcedEnemy.IsAlive = false;

        SimCombatStateMachine.Tick(unit, _state, Delta, new List<SimEvent>());

        AssertThat(unit.Engagement.ForcedTargetUnitId).IsNull();
        AssertThat(unit.Engagement.LockedTargetUnitId.HasValue).IsTrue();
        AssertThat(unit.Engagement.LockedTargetUnitId!.Value).IsEqual(fallbackEnemy.UnitId);
        AssertThat(unit.Engagement.TargetUnitId.HasValue).IsTrue();
        AssertThat(unit.Engagement.TargetUnitId!.Value).IsEqual(fallbackEnemy.UnitId);
    }

    [TestCase]
    public void AcquireTargetCommit_PrefersInAggroUnit_EvenWhenSlotsAlreadySaturated()
    {
        var saturatedTarget = SimTestHelper.CreateMeleeUnit(
            _state,
            team: 1,
            x: 17f,
            z: 0f,
            hp: 600f
        );
        saturatedTarget.NavigationRadius = 0.2f;

        var blockerOne = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 15.5f, z: -0.5f);
        var blockerTwo = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 15.5f, z: 0f);
        var blockerThree = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 15.5f, z: 0.5f);
        AssertThat(
                SimMeleeSlotManager.TryReserveSlot(
                    blockerOne,
                    _state,
                    saturatedTarget.UnitId,
                    out _
                )
            )
            .IsTrue();
        AssertThat(
                SimMeleeSlotManager.TryReserveSlot(
                    blockerTwo,
                    _state,
                    saturatedTarget.UnitId,
                    out _
                )
            )
            .IsTrue();
        AssertThat(
                SimMeleeSlotManager.TryReserveSlot(
                    blockerThree,
                    _state,
                    saturatedTarget.UnitId,
                    out _
                )
            )
            .IsTrue();

        var overflow = SimTestHelper.CreateMeleeUnit(
            _state,
            team: 0,
            x: 15f,
            z: 1.2f,
            attackRange: 2f,
            aggroRadius: 8f
        );

        int? target = SimTargeting.AcquireTargetCommit(
            overflow,
            _state,
            currentTargetId: null,
            droppedTargetId: null,
            droppedTargetCooldownTimer: 0f
        );

        AssertThat(target.HasValue).IsTrue();
        AssertThat(MatchState.IsSummonerTarget(target)).IsFalse();
        AssertThat(target!.Value).IsEqual(saturatedTarget.UnitId);
    }

    [TestCase]
    public void AcquireTargetCommit_TieBreaksByLowerUnitId_ForStableSelection()
    {
        var unit = SimTestHelper.CreateMeleeUnit(
            _state,
            team: 0,
            x: 0f,
            z: 0f,
            attackRange: 2.5f,
            aggroRadius: 12f
        );
        var first = SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 4f, z: 1f);
        var second = SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 4f, z: -1f);

        int? target = SimTargeting.AcquireTargetCommit(
            unit,
            _state,
            currentTargetId: null,
            droppedTargetId: null,
            droppedTargetCooldownTimer: 0f
        );

        AssertThat(target.HasValue).IsTrue();
        AssertThat(target!.Value).IsEqual(first.UnitId);
        AssertThat(second.UnitId > first.UnitId).IsTrue();
    }

    [TestCase]
    public void SummonerAggroPreempt_TargetSwitchTrace_IsDeterministic()
    {
        string runA = RunSummonerPreemptTrace(seed: 24680);
        string runB = RunSummonerPreemptTrace(seed: 24680);

        AssertThat(runA).IsEqual(runB);
    }

    private static string RunSummonerPreemptTrace(uint seed)
    {
        var state = SimTestHelper.CreateBattleState(seed);
        var unit = SimTestHelper.CreateMeleeUnit(
            state,
            team: 0,
            x: 0f,
            z: 0f,
            attackRange: 3f,
            aggroRadius: 30f
        );
        unit.Engagement.LifecycleState = CombatLifecycleState.AcquireTarget;
        int summonerTarget = MatchState.GetSummonerTargetId(team: 1);
        unit.Engagement.LockedTargetUnitId = summonerTarget;
        unit.Engagement.TargetUnitId = summonerTarget;

        var trace = new List<string>();
        for (int frame = 0; frame < 8; frame++)
        {
            if (frame == 3)
                SimTestHelper.CreateMeleeUnit(state, team: 1, x: 1f, z: 0f);

            SimCombatStateMachine.Tick(unit, state, Delta, new List<SimEvent>());
            trace.Add($"{frame}:{unit.Engagement.TargetUnitId}:{unit.Engagement.LastRetargetReason}");
        }

        return string.Join("|", trace);
    }
}
