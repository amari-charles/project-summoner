namespace Fateforged.Tests.Simulation;

using System;
using Fateforged.Simulation;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Geometry;
using Fateforged.Simulation.Movement;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class OrcaAvoidanceTest
{
    private MatchState _state = null!;

    [BeforeTest]
    public void Setup()
    {
        _state = SimTestHelper.CreateBattleState();
    }

    [TestCase]
    public void ComputeSafeVelocity_UsesNearestNeighbors_WhenNeighborCountExceedsCap()
    {
        const int farNeighborCount = 96;

        var mover = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 0f, z: 0f, moveSpeed: 3f);
        mover.NavigationRadius = 0.5f;
        mover.Velocity = SimVector3.Zero;
        mover.BehaviorState = BehaviorState.Chasing;

        // Populate many distant low-impact neighbors. This count intentionally exceeds
        // current ORCA cap and should remain comfortably above future tuning values.
        for (int i = 0; i < farNeighborCount; i++)
        {
            float angle = i * (SimMath.Tau / farNeighborCount);
            float x = MathF.Cos(angle) * 2.4f;
            float z = MathF.Sin(angle) * 2.4f;
            var far = SimTestHelper.CreateMeleeUnit(_state, team: 1, x: x, z: z, moveSpeed: 0f, attackSpeed: 0f);
            far.NavigationRadius = 0.01f;
            far.Velocity = SimVector3.Zero;
            far.BehaviorState = BehaviorState.Chasing;
        }

        var preferredVelocity = new SimVector3(3f, 0f, 0f);
        var baselineSafeVelocity = OrcaAvoidance.ComputeSafeVelocity(mover, preferredVelocity, _state);

        // Insert the closest blocker last; nearest-neighbor selection must still include it.
        var close = SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 0.35f, z: 0f, moveSpeed: 0f, attackSpeed: 0f);
        close.NavigationRadius = 0.5f;
        close.Velocity = SimVector3.Zero;
        close.BehaviorState = BehaviorState.InRange;

        var safeVelocity = OrcaAvoidance.ComputeSafeVelocity(mover, preferredVelocity, _state);

        // Black-box guard: adding a close blocker after many far neighbors should
        // still materially reduce forward speed.
        AssertThat(safeVelocity.X).IsLess(baselineSafeVelocity.X - 0.5f);
    }

    [TestCase]
    public void ComputeSafeVelocity_SameTargetMeleeNonOverlap_DoesNotAddPreOverlapRepulsion()
    {
        var target = SimTestHelper.CreateMeleeUnit(
            _state,
            team: 1,
            x: 2f,
            z: 0f,
            moveSpeed: 0f,
            attackSpeed: 0f,
            attackRange: 1f
        );
        var mover = SimTestHelper.CreateMeleeUnit(
            _state,
            team: 0,
            x: 0f,
            z: 0f,
            moveSpeed: 3f,
            attackRange: 2f
        );
        mover.NavigationRadius = 0.4f;
        mover.NavigationRadius = 0.4f;
        mover.BehaviorState = BehaviorState.Chasing;
        mover.TargetUnitId = target.UnitId;
        mover.EngageShape = EngageShape.ForwardRect;
        mover.EngageRectLength = 1.8f;
        mover.EngageRectHalfWidth = 0.7f;

        var preferredVelocity = new SimVector3(2.8f, 0f, 0f);
        var baselineSafeVelocity = OrcaAvoidance.ComputeSafeVelocity(mover, preferredVelocity, _state);

        var ally = SimTestHelper.CreateMeleeUnit(
            _state,
            team: 0,
            x: 0f,
            z: 0.85f,
            moveSpeed: 0f,
            attackSpeed: 0f,
            attackRange: 2f
        );
        ally.NavigationRadius = 0.4f;
        ally.NavigationRadius = 0.4f;
        ally.BehaviorState = BehaviorState.Chasing;
        ally.TargetUnitId = target.UnitId;
        ally.EngageShape = EngageShape.ForwardRect;
        ally.EngageRectLength = 1.8f;
        ally.EngageRectHalfWidth = 0.7f;

        float combinedRadius = CombatGeometry.GetNavigationRadius(mover) + CombatGeometry.GetNavigationRadius(ally);
        float pairDistance = new SimVector3(
            ally.Position.X - mover.Position.X,
            0f,
            ally.Position.Z - mover.Position.Z
        ).Length();
        AssertThat(pairDistance).IsGreater(combinedRadius);

        var safeVelocity = OrcaAvoidance.ComputeSafeVelocity(mover, preferredVelocity, _state);
        AssertThat(MathF.Abs(safeVelocity.X - baselineSafeVelocity.X)).IsLess(0.06f);
        AssertThat(MathF.Abs(safeVelocity.Z - baselineSafeVelocity.Z)).IsLess(0.06f);
    }
}
