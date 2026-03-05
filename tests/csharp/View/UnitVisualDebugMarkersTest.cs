namespace Fateforged.Tests.View;

using System;
using System.Collections.Generic;
using System.Reflection;
using Fateforged.Infrastructure.Debug;
using Fateforged.Session;
using Fateforged.Simulation;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;
using Fateforged.View;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public partial class UnitVisualDebugMarkersTest
{
    private readonly List<Node> _createdNodes = [];

    [AfterTest]
    public void Cleanup()
    {
        for (var i = _createdNodes.Count - 1; i >= 0; i--)
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
    public void Process_HurtboxDebugFlag_CreatesAndQueuesMarkerForRemoval()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var root = tree.Root;

        var debugService = new BattlefieldDebugService { Name = "BattlefieldDebug" };
        root.AddChild(debugService);
        _createdNodes.Add(debugService);

        var visual = new UnitVisual { Name = "UnitVisualDebugTest" };
        root.AddChild(visual);
        _createdNodes.Add(visual);

        const int unitId = 42;
        var state = new MatchState();
        state.Units[unitId] = new UnitData
        {
            UnitId = unitId,
            IsAlive = true,
            Position = new SimVector3(0f, 0f, 0f),
            SeparationRadius = 0.9f,
            AttackRange = 3f,
        };

        SetPrivateField(visual, "_session", new StubSession(state));
        SetPrivateField(visual, "_unitId", unitId);

        var markerField = typeof(UnitVisual).GetField("_debugHurtboxMarker", BindingFlags.Instance | BindingFlags.NonPublic);
        AssertThat(markerField).IsNotNull();

        debugService.HurtboxEnabled = true;
        visual._Process(1.0 / 60.0);

        var marker = markerField!.GetValue(visual) as MeshInstance3D;
        AssertThat(marker).IsNotNull();

        debugService.HurtboxEnabled = false;
        visual._Process(1.0 / 60.0);

        var markerAfterDisable = markerField.GetValue(visual) as MeshInstance3D;
        AssertThat(markerAfterDisable).IsNull();
        AssertThat(marker!.IsQueuedForDeletion()).IsTrue();
    }

    private static void SetPrivateField(object instance, string fieldName, object? value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field?.SetValue(instance, value);
    }

    private sealed class StubSession : IGameSession
    {
        private readonly MatchState _state;

        public StubSession(MatchState state)
        {
            _state = state;
        }

        public event Action<IReadOnlyList<SimEvent>> SimEventsEmitted
        {
            add { }
            remove { }
        }

        public MatchState GetState() => _state;

        public void SubmitCommand(ICommand command)
        {
        }

        public void Tick(float delta)
        {
        }
    }
}
