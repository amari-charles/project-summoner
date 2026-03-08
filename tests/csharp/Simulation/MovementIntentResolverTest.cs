namespace Fateforged.Tests.Simulation;

using System;
using Fateforged.Simulation;
using Fateforged.Simulation.Combat;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Movement;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class MovementIntentResolverTest
{
    private const float Delta = 1f / 60f;
    private MatchState _state = null!;

    [BeforeTest]
    public void Setup()
    {
        _state = SimTestHelper.CreateBattleState();
    }

    [TestCase]
    public void Resolve_StrategyChangesIntentGeneratorOutput()
    {
        var target = SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 0.10f, z: 0f, moveSpeed: 0f, attackSpeed: 0f);
        var directUnit = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 0f, z: 0f);
        var contextUnit = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 0f, z: 0f);

        directUnit.MovementIntentStrategy = MovementIntentStrategy.Direct;
        contextUnit.MovementIntentStrategy = MovementIntentStrategy.Context;

        var behavior = new SimBehavior.BehaviorResult
        {
            Movement = MovementResult.TowardTarget,
            MoveTargetId = target.UnitId
        };

        var directIntent = MovementIntentResolver.Resolve(directUnit, behavior, _state, delta: 1f / 60f);
        var contextIntent = MovementIntentResolver.Resolve(contextUnit, behavior, _state, delta: 1f / 60f);

        AssertThat(directIntent.DesiredVelocity.LengthSquared()).IsGreater(0f);
        AssertThat(contextIntent.DesiredVelocity.LengthSquared()).IsEqual(0f);
    }

    [TestCase]
    public void Resolve_BlockedContextIntent_TriggersYieldThenEscape()
    {
        var target = SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 0.10f, z: 0f, moveSpeed: 0f, attackSpeed: 0f);
        var unit = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 0f, z: 0f);
        unit.MovementIntentStrategy = MovementIntentStrategy.Context;

        var behavior = new SimBehavior.BehaviorResult
        {
            Movement = MovementResult.TowardTarget,
            MoveTargetId = target.UnitId
        };

        bool sawYield = false;
        bool sawEscape = false;

        for (int i = 0; i < 120; i++)
        {
            var intent = MovementIntentResolver.Resolve(unit, behavior, _state, Delta);

            if (intent.DesiredVelocity.LengthSquared() < 0.0001f && unit.NavigationYieldTimer > 0f)
                sawYield = true;

            if (unit.NavigationEscapeTimer > 0f && MathF.Abs(intent.DesiredVelocity.Z) > 0.001f)
                sawEscape = true;
        }

        AssertThat(sawYield).IsTrue();
        AssertThat(sawEscape).IsTrue();
    }

    [TestCase]
    public void Tick_NoMovement_ResetsBlockedNavigationState()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 0f, z: 0f);
        unit.NavigationTargetId = 999;
        unit.NavigationLastTargetDistance = 2.5f;
        unit.NavigationBlockedTime = 0.4f;
        unit.NavigationYieldTimer = 0.2f;
        unit.NavigationEscapeTimer = 0.3f;
        unit.NavigationEscapeQueued = true;
        unit.NavigationEscapeDirectionSign = -1;

        var behavior = new SimBehavior.BehaviorResult
        {
            Movement = MovementResult.None
        };

        SimMovement.Tick(unit, behavior, _state, Delta);

        AssertThat(unit.NavigationTargetId).IsNull();
        AssertThat(unit.NavigationLastTargetDistance).IsEqual(-1f);
        AssertThat(unit.NavigationBlockedTime).IsEqual(0f);
        AssertThat(unit.NavigationYieldTimer).IsEqual(0f);
        AssertThat(unit.NavigationEscapeTimer).IsEqual(0f);
        AssertThat(unit.NavigationEscapeQueued).IsFalse();
    }
}
