namespace Fateforged.Tests.Simulation;

using System.Collections.Generic;
using Fateforged.Simulation;
using Fateforged.Simulation.Combat;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Movement;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class SimAttackLoopTest
{
    private const float Delta = 1f / 60f;
    private MatchState _state = null!;

    [BeforeTest]
    public void Setup()
    {
        _state = SimTestHelper.CreateBattleState();
    }

    [TestCase]
    public void Begin_SetsWindupAndLockedTarget()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 0f, z: 0f);

        unit.Attack.Timing.WindupSeconds = 0.3f;
        SimAttackLoop.Begin(unit, _state, targetId: 99);

        AssertThat(unit.AttackPhase).IsEqual(AttackPhase.Windup);
        AssertThat(unit.AttackPhaseLockTargetId.HasValue).IsTrue();
        AssertThat(unit.AttackPhaseLockTargetId!.Value).IsEqual(99);
        AssertThat(unit.AttackPhaseTimer).IsEqual(0.3f);
    }

    [TestCase]
    public void Tick_TransitionsWindupToActiveToRecoveryToNone()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 0f, z: 0f);
        unit.Attack.Timing.WindupSeconds = 0.01f;
        unit.Attack.Timing.ActiveSeconds = 0f;
        unit.Attack.Timing.RecoverySeconds = 0f;
        SimAttackLoop.Begin(unit, _state, targetId: 5);

        SimAttackLoop.Tick(unit, _state, delta: 0.02f, new List<SimEvent>());
        AssertThat(unit.AttackPhase).IsEqual(AttackPhase.Active);

        unit.AttackPhaseTimer = 0f;
        SimAttackLoop.Tick(unit, _state, delta: 0f, new List<SimEvent>());
        AssertThat(unit.AttackPhase).IsEqual(AttackPhase.Recovery);

        unit.AttackPhaseTimer = 0f;
        SimAttackLoop.Tick(unit, _state, delta: 0f, new List<SimEvent>());
        AssertThat(unit.AttackPhase).IsEqual(AttackPhase.None);
        AssertThat(unit.AttackPhaseLockTargetId).IsNull();
    }

    [TestCase]
    public void AttackPhase_AnchorsPosition_NoTranslation()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 0f, z: 0f);
        unit.AttackPhase = AttackPhase.Active;

        var start = unit.Position;
        var behavior = new SimBehavior.BehaviorResult { Movement = MovementResult.None };
        SimMovement.Tick(unit, behavior, _state, Delta);

        AssertThat(unit.Position.X).IsEqual(start.X);
        AssertThat(unit.Position.Z).IsEqual(start.Z);
    }

    [TestCase]
    public void Tick_WindupCommit_ResolvesPendingAttackExactlyOnce()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 0f, z: 0f, damage: 25f);
        attacker.CritChance = 0f;
        attacker.ElementId = 0;
        attacker.Attack.Timing.WindupSeconds = 0.01f;
        var target = SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 1.5f, z: 0f, hp: 100f);
        target.Evasion = 0f;

        attacker.TargetUnitId = target.UnitId;
        attacker.AttackCooldown = 0f;

        var events = new List<SimEvent>();
        SimBehavior.TickBehavior(attacker, _state, Delta, events);
        SimAttackLoop.Begin(attacker, _state, target.UnitId);

        float hpBefore = target.CurrentHp;
        SimAttackLoop.Tick(attacker, _state, delta: 0.02f, events);
        float hpAfterCommit = target.CurrentHp;

        AssertThat(hpAfterCommit).IsLess(hpBefore);

        // Advance through active/recovery: no second commit should occur.
        attacker.AttackPhaseTimer = 0f;
        SimAttackLoop.Tick(attacker, _state, delta: 0f, events);
        attacker.AttackPhaseTimer = 0f;
        SimAttackLoop.Tick(attacker, _state, delta: 0f, events);

        AssertThat(target.CurrentHp).IsEqual(hpAfterCommit);
    }

    [TestCase]
    public void Cancel_DuringWindup_ClearsPendingAttack()
    {
        var attacker = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 0f, z: 0f, damage: 25f);
        attacker.CritChance = 0f;
        attacker.ElementId = 0;
        var target = SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 1.5f, z: 0f, hp: 100f);
        target.Evasion = 0f;

        attacker.TargetUnitId = target.UnitId;
        attacker.AttackCooldown = 0f;
        var events = new List<SimEvent>();

        SimBehavior.TickBehavior(attacker, _state, Delta, events);
        SimAttackLoop.Begin(attacker, _state, target.UnitId);
        SimAttackLoop.Cancel(attacker, _state);
        SimAttackLoop.Tick(attacker, _state, delta: 1f, events);

        AssertThat(attacker.PendingAttackTargetId).IsNull();
        AssertThat(target.CurrentHp).IsEqual(100f);
    }

    [TestCase]
    public void Begin_WindupPrecedence_UsesAuthoredBeforeLegacyDelay()
    {
        var unit = SimTestHelper.CreateRangedUnit(_state, team: 0, projectileDelay: 0.55f);
        unit.Attack.Timing.WindupSeconds = 0.2f;

        SimAttackLoop.Begin(unit, _state, targetId: 99);

        AssertThat(unit.AttackPhaseTimer).IsEqual(0.2f);
    }

    [TestCase]
    public void Begin_WindupPrecedence_UsesLegacyDelayWhenAuthoredUnset()
    {
        var unit = SimTestHelper.CreateRangedUnit(_state, team: 0, projectileDelay: 0.55f);
        unit.Attack.Timing.WindupSeconds = 0f;

        SimAttackLoop.Begin(unit, _state, targetId: 99);

        AssertThat(unit.AttackPhaseTimer).IsEqual(0.55f);
    }

    [TestCase]
    public void Begin_FallbackWindup_ClampsToCooldownBudget()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, team: 0, attackSpeed: 3f);
        unit.Attack.Timing.WindupSeconds = 0f;
        unit.Attack.Timing.ActiveSeconds = 0f;
        unit.Attack.Timing.RecoverySeconds = 0f;

        SimAttackLoop.Begin(unit, _state, targetId: 99);

        float expectedMax = (1f / 3f) - 0.05f - 0.15f - 0.01f;
        AssertThat(unit.AttackPhaseTimer).IsEqual(expectedMax);
    }
}
