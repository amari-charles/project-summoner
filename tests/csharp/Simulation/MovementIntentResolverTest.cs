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
        SimMovement.DebugHoldPlayerAdvanceEnabled = false;
    }

    [TestCase]
    public void Resolve_StrategyChangesIntentGeneratorOutput()
    {
        var target = SimTestHelper.CreateMeleeUnit(
            _state,
            team: 1,
            x: 0.10f,
            z: 0f,
            moveSpeed: 0f,
            attackSpeed: 0f
        );
        var directUnit = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 0f, z: 0f);
        var contextUnit = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 0f, z: 0f);

        directUnit.MovementIntentStrategy = MovementIntentStrategy.Direct;
        contextUnit.MovementIntentStrategy = MovementIntentStrategy.Context;

        var behavior = new SimBehavior.BehaviorResult
        {
            Movement = MovementResult.TowardTarget,
            MoveTargetId = target.UnitId,
        };

        var directIntent = MovementIntentResolver.Resolve(
            directUnit,
            behavior,
            _state,
            delta: 1f / 60f
        );
        var contextIntent = MovementIntentResolver.Resolve(
            contextUnit,
            behavior,
            _state,
            delta: 1f / 60f
        );

        AssertThat(directIntent.DesiredVelocity.LengthSquared()).IsGreater(0f);
        AssertThat(contextIntent.DesiredVelocity.LengthSquared()).IsEqual(0f);
    }

    [TestCase]
    public void Resolve_BlockedContextIntent_TriggersYieldThenEscape()
    {
        var target = SimTestHelper.CreateMeleeUnit(
            _state,
            team: 1,
            x: 0.10f,
            z: 0f,
            moveSpeed: 0f,
            attackSpeed: 0f
        );
        var unit = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 0f, z: 0f);
        unit.MovementIntentStrategy = MovementIntentStrategy.Context;

        var behavior = new SimBehavior.BehaviorResult
        {
            Movement = MovementResult.TowardTarget,
            MoveTargetId = target.UnitId,
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

        var behavior = new SimBehavior.BehaviorResult { Movement = MovementResult.None };

        SimMovement.Tick(unit, behavior, _state, Delta);

        AssertThat(unit.NavigationTargetId).IsNull();
        AssertThat(unit.NavigationLastTargetDistance).IsEqual(-1f);
        AssertThat(unit.NavigationBlockedTime).IsEqual(0f);
        AssertThat(unit.NavigationYieldTimer).IsEqual(0f);
        AssertThat(unit.NavigationEscapeTimer).IsEqual(0f);
        AssertThat(unit.NavigationEscapeQueued).IsFalse();
    }

    [TestCase]
    public void Resolve_ForwardIntent_UsesObjectiveAdvanceCurve_ForAllUnitTypes()
    {
        var melee = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 12f, z: 10f);
        var ranged = SimTestHelper.CreateRangedUnit(_state, team: 0, x: 12f, z: 10f);
        var flying = SimTestHelper.CreateFlyingUnit(_state, team: 0, x: 12f, z: 10f);
        melee.MovementIntentStrategy = MovementIntentStrategy.Direct;
        ranged.MovementIntentStrategy = MovementIntentStrategy.Direct;
        flying.MovementIntentStrategy = MovementIntentStrategy.Direct;

        var behavior = new SimBehavior.BehaviorResult { Movement = MovementResult.Forward };
        var meleeIntent = MovementIntentResolver.Resolve(melee, behavior, _state, Delta);
        var rangedIntent = MovementIntentResolver.Resolve(ranged, behavior, _state, Delta);
        var flyingIntent = MovementIntentResolver.Resolve(flying, behavior, _state, Delta);

        AssertThat(meleeIntent.DesiredVelocity.X).IsGreater(0f);
        AssertThat(meleeIntent.DesiredVelocity.Z).IsLess(0f);
        AssertThat(rangedIntent.DesiredVelocity.X).IsGreater(0f);
        AssertThat(rangedIntent.DesiredVelocity.Z).IsLess(0f);
        AssertThat(flyingIntent.DesiredVelocity.X).IsGreater(0f);
        AssertThat(flyingIntent.DesiredVelocity.Z).IsLess(0f);
    }

    [TestCase]
    public void Tick_CommitMeleeForwardWithoutTarget_MovesInsteadOfIdling()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: -12f, z: 0f, moveSpeed: 3f);
        var behavior = new SimBehavior.BehaviorResult
        {
            Movement = MovementResult.Forward,
            MoveTargetId = null,
        };

        var startPos = unit.Position;
        SimMovement.Tick(unit, behavior, _state, Delta);

        AssertThat(unit.Velocity.LengthSquared()).IsGreater(0.0001f);
        AssertThat(unit.Position.X).IsGreater(startPos.X);
    }

    [TestCase]
    public void Tick_DebugHoldPlayerAdvanceStopsOnlyPlayerForwardMovement()
    {
        var player = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: -12f, z: 0f, moveSpeed: 3f);
        var enemy = SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 12f, z: 0f, moveSpeed: 3f);
        var behavior = new SimBehavior.BehaviorResult { Movement = MovementResult.Forward };
        var playerStart = player.Position;
        var enemyStart = enemy.Position;

        try
        {
            SimMovement.DebugHoldPlayerAdvanceEnabled = true;
            SimMovement.Tick(player, behavior, _state, Delta);
            SimMovement.Tick(enemy, behavior, _state, Delta);
        }
        finally
        {
            SimMovement.DebugHoldPlayerAdvanceEnabled = false;
        }

        AssertThat(player.Position.X).IsEqual(playerStart.X);
        AssertThat(player.Position.Z).IsEqual(playerStart.Z);
        AssertThat(player.Velocity.LengthSquared()).IsEqual(0f);
        AssertThat(enemy.Position.X).IsLess(enemyStart.X);
        AssertThat(enemy.Velocity.LengthSquared()).IsGreater(0.0001f);
    }
}
