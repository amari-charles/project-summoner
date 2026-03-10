namespace Fateforged.Tests.Simulation;

using System;
using Fateforged.Simulation;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
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
        mover.SeparationRadius = 0.5f;
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
            far.SeparationRadius = 0.01f;
            far.Velocity = SimVector3.Zero;
            far.BehaviorState = BehaviorState.Chasing;
        }

        var preferredVelocity = new SimVector3(3f, 0f, 0f);
        var baselineSafeVelocity = OrcaAvoidance.ComputeSafeVelocity(mover, preferredVelocity, _state);

        // Insert the closest blocker last; nearest-neighbor selection must still include it.
        var close = SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 0.35f, z: 0f, moveSpeed: 0f, attackSpeed: 0f);
        close.SeparationRadius = 0.5f;
        close.Velocity = SimVector3.Zero;
        close.BehaviorState = BehaviorState.InRange;

        var safeVelocity = OrcaAvoidance.ComputeSafeVelocity(mover, preferredVelocity, _state);

        // Black-box guard: adding a close blocker after many far neighbors should
        // still materially reduce forward speed.
        AssertThat(safeVelocity.X).IsLess(baselineSafeVelocity.X - 0.5f);
    }
}
