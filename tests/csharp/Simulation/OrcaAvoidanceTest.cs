namespace Fateforged.Tests.Simulation;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        var mover = SimTestHelper.CreateMeleeUnit(_state, team: 0, x: 0f, z: 0f, moveSpeed: 3f);
        mover.SeparationRadius = 0.5f;
        mover.Velocity = SimVector3.Zero;
        mover.BehaviorState = BehaviorState.Chasing;

        // Fill the cap first with distant low-impact neighbors.
        for (int i = 0; i < 10; i++)
        {
            float angle = i * (SimMath.Tau / 10f);
            float x = MathF.Cos(angle) * 2.4f;
            float z = MathF.Sin(angle) * 2.4f;
            var far = SimTestHelper.CreateMeleeUnit(_state, team: 1, x: x, z: z, moveSpeed: 0f, attackSpeed: 0f);
            far.SeparationRadius = 0.01f;
            far.Velocity = SimVector3.Zero;
            far.BehaviorState = BehaviorState.Chasing;
        }

        // Insert the closest blocker last; nearest-neighbor selection must still include it.
        var close = SimTestHelper.CreateMeleeUnit(_state, team: 1, x: 0.35f, z: 0f, moveSpeed: 0f, attackSpeed: 0f);
        close.SeparationRadius = 0.5f;
        close.Velocity = SimVector3.Zero;
        close.BehaviorState = BehaviorState.InRange;

        var preferredVelocity = new SimVector3(3f, 0f, 0f);
        OrcaAvoidance.ComputeSafeVelocity(mover, preferredVelocity, _state);

        var neighborsField = typeof(OrcaAvoidance).GetField(
            "_neighbors", BindingFlags.NonPublic | BindingFlags.Static);
        var selectedNeighbors = neighborsField?.GetValue(null) as List<UnitData>;

        AssertThat(selectedNeighbors).IsNotNull();
        AssertThat(selectedNeighbors!.Count).IsEqual(10);
        AssertThat(selectedNeighbors.Any(n => n.UnitId == close.UnitId)).IsTrue();
    }
}
