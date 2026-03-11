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
}
