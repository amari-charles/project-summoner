namespace Fateforged.Tests.Simulation;

using System;
using Fateforged.Simulation.Combat;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Geometry;
using Fateforged.Simulation.Movement;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class MovementTargetResolverTest
{
    private MatchState _state = null!;

    [BeforeTest]
    public void Setup()
    {
        _state = SimTestHelper.CreateBattleState();
    }

    [TestCase]
    public void Resolve_MeleeSameTarget_UsesDeterministicDistinctApproachOffsets()
    {
        var target = SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 5f, z: 0f, moveSpeed: 0f, attackSpeed: 0f);
        var first = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 0f, z: 0f, attackRange: 2f);
        var second = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 0f, z: 0f, attackRange: 2f);
        var third = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 0f, z: 0f, attackRange: 2f);

        first.EngageShape = EngageShape.ForwardRect;
        second.EngageShape = EngageShape.ForwardRect;
        third.EngageShape = EngageShape.ForwardRect;
        first.EngageRectHalfWidth = 0.9f;
        second.EngageRectHalfWidth = 0.9f;
        third.EngageRectHalfWidth = 0.9f;

        first.TargetUnitId = target.UnitId;
        second.TargetUnitId = target.UnitId;
        third.TargetUnitId = target.UnitId;

        var thirdPoint = MovementTargetResolver.Resolve(third, target.UnitId, _state);
        var firstPoint = MovementTargetResolver.Resolve(first, target.UnitId, _state);
        var secondPoint = MovementTargetResolver.Resolve(second, target.UnitId, _state);

        AssertThat(firstPoint.HasValue).IsTrue();
        AssertThat(secondPoint.HasValue).IsTrue();
        AssertThat(thirdPoint.HasValue).IsTrue();

        float nav = CombatGeometry.GetNavigationRadius(first);
        float standoff = MathF.Min(0.9f * (nav + CombatGeometry.GetNavigationRadius(target)), 0.35f * first.AttackRange);
        float expectedBaseX = target.Position.X - standoff;
        float expectedStep = MathF.Max(0.18f, 0.55f * nav);
        float expectedBudget = MathF.Max(0.20f, MathF.Min(0.75f * first.EngageRectHalfWidth, 1.10f));

        AssertThat(firstPoint!.Value.X).IsEqualApprox(expectedBaseX, 0.0001f);
        AssertThat(secondPoint!.Value.X).IsEqualApprox(expectedBaseX, 0.0001f);
        AssertThat(thirdPoint!.Value.X).IsEqualApprox(expectedBaseX, 0.0001f);

        float expectedSecondOffset = Math.Clamp(expectedStep, -expectedBudget, expectedBudget);
        float expectedThirdOffset = Math.Clamp(-expectedStep, -expectedBudget, expectedBudget);

        AssertThat(firstPoint.Value.Z).IsEqualApprox(0f, 0.0001f);
        AssertThat(secondPoint.Value.Z).IsEqualApprox(expectedSecondOffset, 0.0001f);
        AssertThat(thirdPoint.Value.Z).IsEqualApprox(expectedThirdOffset, 0.0001f);
        AssertThat(secondPoint.Value.Z).IsGreater(firstPoint.Value.Z);
        AssertThat(thirdPoint.Value.Z).IsLess(firstPoint.Value.Z);
    }

    [TestCase]
    public void ResolveObjectiveAdvanceDirection_PreBand_IsStraightForward()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: -8f, z: 6f);

        var dir = MovementTargetResolver.ResolveObjectiveAdvanceDirection(unit, _state);

        AssertThat(dir.X).IsGreater(0.95f);
        AssertThat(MathF.Abs(dir.Z)).IsLess(0.05f);
    }

    [TestCase]
    public void ResolveObjectiveAdvanceDirection_PostBand_CurvesTowardEnemySummoner()
    {
        var unit = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 12f, z: 10f);

        var dir = MovementTargetResolver.ResolveObjectiveAdvanceDirection(unit, _state);

        // Enemy summoner is at +X, Z=0 so curved direction should pull negative Z.
        AssertThat(dir.X).IsGreater(0f);
        AssertThat(dir.Z).IsLess(0f);
    }

    [TestCase]
    public void ResolveObjectiveAdvanceDirection_CurveStrength_IncreasesWithProgress()
    {
        var earlyPostBand = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 10f, z: 10f);
        var latePostBand = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 18f, z: 10f);

        var earlyDir = MovementTargetResolver.ResolveObjectiveAdvanceDirection(earlyPostBand, _state);
        var lateDir = MovementTargetResolver.ResolveObjectiveAdvanceDirection(latePostBand, _state);

        AssertThat(MathF.Abs(lateDir.Z)).IsGreater(MathF.Abs(earlyDir.Z));
    }
}
