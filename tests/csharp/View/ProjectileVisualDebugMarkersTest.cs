namespace Fateforged.Tests.View;

using System;
using System.Collections.Generic;
using System.Reflection;
using Fateforged.Infrastructure.Debug;
using Fateforged.Projectiles;
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
public partial class ProjectileVisualDebugMarkersTest
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
    public void Process_ProjectileHitGeometryFlag_CreatesHitRadiusMarker()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var root = tree.Root;

        var debugService = new BattlefieldDebugService { Name = "BattlefieldDebug" };
        root.AddChild(debugService);
        _createdNodes.Add(debugService);

        var visual = new ProjectileVisual { Name = "ProjectileVisualDebugTest" };
        root.AddChild(visual);
        _createdNodes.Add(visual);

        const int projectileId = 404;
        var state = new MatchState();
        state.Projectiles[projectileId] = new SimProjectileData
        {
            ProjectileId = projectileId,
            CurrentPosition = new SimVector3(1f, 0f, 2f),
            LastPosition = new SimVector3(0.8f, 0f, 2f),
            Direction = new SimVector3(1f, 0f, 0f),
            HitRadius = 0.25f,
            HitSpace = ProjectileHitSpace.GroundCylinder,
            IsDead = false,
        };

        visual.Initialize(new StubSession(state), projectileId);

        var markerField = typeof(ProjectileVisual).GetField(
            "_debugHitRadiusMarker",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        AssertThat(markerField).IsNotNull();

        debugService.ProjectileHitGeometryEnabled = true;
        visual._Process(1.0 / 60.0);

        var marker = markerField!.GetValue(visual) as MeshInstance3D;
        AssertThat(marker).IsNotNull();
        AssertThat(marker!.Mesh is CylinderMesh).IsTrue();
        AssertThat(visual.Visible).IsTrue();
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

        public void SubmitCommand(ICommand command) { }

        public void Tick(float delta) { }
    }
}
