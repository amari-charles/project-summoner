namespace Fateforged.Tests.View;

using System.Collections.Generic;
using Fateforged.Simulation;
using Fateforged.View;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class EntityManagerTest
{
    private readonly List<Node> _createdNodes = [];

    [AfterTest]
    public void Cleanup()
    {
        for (int i = _createdNodes.Count - 1; i >= 0; i--)
        {
            var node = _createdNodes[i];
            if (!GodotObject.IsInstanceValid(node))
                continue;

            node.GetParent()?.RemoveChild(node);
            node.Free();
        }

        _createdNodes.Clear();
    }

    [TestCase]
    public void SummonerDestroyedEvent_EmitsSummonerDestroyedOnce()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var root = tree.Root;

        var manager = new EntityManager { Name = $"EntityManagerTest_{_createdNodes.Count}" };
        root.AddChild(manager);
        _createdNodes.Add(manager);

        var summoner = new SummonerVisual
        {
            Name = $"SummonerVisualTest_{_createdNodes.Count}",
            Team = 0,
        };
        root.AddChild(summoner);
        _createdNodes.Add(summoner);

        int destroyedSignalCount = 0;
        summoner.Connect(
            SummonerVisual.SignalName.SummonerDestroyed,
            Callable.From<Node3D>(_ => destroyedSignalCount++)
        );

        manager.RegisterSummonerVisual(summoner, 0);
        manager.Visit(new SummonerDestroyedEvent(team: 0, killerUnitId: 123));

        AssertThat(destroyedSignalCount).IsEqual(1);
    }
}
