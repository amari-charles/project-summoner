namespace Fateforged.Tests.Input;

using System.Reflection;
using Fateforged.Input;
using Godot;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class InputCollectorDebugSpawnTest
{
    [TestCase]
    public void SpawnModeRuntime_SingleBurstPaint_ProduceExpectedPositionSets()
    {
        var collector = new InputCollector();
        var center = new Vector3(-10f, 0f, 0f);
        const int teamPlayer = 0;

        var single = InvokeBuildDebugSpawnPositions(
            collector,
            center,
            teamPlayer,
            "single",
            4,
            "line",
            2f
        );
        AssertThat(single.Count).IsEqual(1);
        AssertThat(single[0]).IsEqual(center);

        var burst = InvokeBuildDebugSpawnPositions(
            collector,
            center,
            teamPlayer,
            "burst",
            3,
            "line",
            2f
        );
        AssertThat(burst.Count).IsEqual(3);
        AssertThat(burst[0].Z).IsEqual(-2f);
        AssertThat(burst[1].Z).IsEqual(0f);
        AssertThat(burst[2].Z).IsEqual(2f);

        InvokeRegisterPaintPoint(collector, new Vector3(-12f, 0f, 1f), 2f);
        InvokeRegisterPaintPoint(collector, new Vector3(-12f, 0f, 1.2f), 2f); // ignored (too close)
        InvokeRegisterPaintPoint(collector, new Vector3(-13f, 0f, 3f), 2f);

        var paint = InvokeBuildDebugSpawnPositions(
            collector,
            center,
            teamPlayer,
            "paint",
            5,
            "stack",
            2f
        );
        AssertThat(paint.Count).IsEqual(2);
    }

    [TestCase]
    public void FormationRuntime_StackLineArcRandom_ProduceExpectedSpatialLayouts()
    {
        var collector = new InputCollector();
        var center = new Vector3(-10f, 0f, 0f);
        const int teamPlayer = 0;

        var stack = InvokeBuildDebugSpawnPositions(
            collector,
            center,
            teamPlayer,
            "burst",
            3,
            "stack",
            2f
        );
        AssertThat(stack.Count).IsEqual(3);
        AssertThat(stack[0]).IsEqual(center);
        AssertThat(stack[1]).IsEqual(center);
        AssertThat(stack[2]).IsEqual(center);

        var line = InvokeBuildDebugSpawnPositions(
            collector,
            center,
            teamPlayer,
            "burst",
            3,
            "line",
            2f
        );
        AssertThat(line.Count).IsEqual(3);
        AssertThat(line[0].Z).IsEqual(-2f);
        AssertThat(line[1].Z).IsEqual(0f);
        AssertThat(line[2].Z).IsEqual(2f);

        var arc = InvokeBuildDebugSpawnPositions(
            collector,
            center,
            teamPlayer,
            "burst",
            3,
            "arc",
            2f
        );
        AssertThat(arc.Count).IsEqual(3);
        AssertThat(arc[1].X).IsGreater(arc[0].X);
        AssertThat(Mathf.Abs(arc[0].Z + arc[2].Z)).IsLess(0.001f);

        var random = InvokeBuildDebugSpawnPositions(
            collector,
            center,
            teamPlayer,
            "burst",
            6,
            "random",
            2f
        );
        AssertThat(random.Count).IsEqual(6);
        foreach (var position in random)
        {
            float distance = center.DistanceTo(position);
            AssertThat(distance).IsGreaterEqual(0.39f);
            AssertThat(distance).IsLessEqual(2.05f);
        }
    }

    private static Godot.Collections.Array<Vector3> InvokeBuildDebugSpawnPositions(
        InputCollector collector,
        Vector3 center,
        int team,
        string spawnMode,
        int burstCount,
        string formationMode,
        float spacing
    )
    {
        var method = typeof(InputCollector).GetMethod(
            "BuildDebugSpawnPositions",
            BindingFlags.NonPublic | BindingFlags.Instance
        );
        var result = method!.Invoke(
            collector,
            new object[] { center, team, spawnMode, burstCount, formationMode, spacing }
        );
        return (Godot.Collections.Array<Vector3>)result!;
    }

    private static void InvokeRegisterPaintPoint(InputCollector collector, Vector3 position, float spacing)
    {
        var method = typeof(InputCollector).GetMethod(
            "RegisterPaintPoint",
            BindingFlags.NonPublic | BindingFlags.Instance
        );
        method!.Invoke(collector, new object[] { position, spacing });
    }
}
