namespace Fateforged.Tests.View;

using System;
using System.Collections.Generic;
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
public partial class EntityManagerProjectileDebugOverlayTest
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
    public void Process_ProjectileDebugEnabled_CreatesEntityManagerOverlayMarker()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var root = tree.Root;

        var debugService = new BattlefieldDebugService { Name = "BattlefieldDebug" };
        root.AddChild(debugService);
        _createdNodes.Add(debugService);

        var state = new MatchState();
        const int projectileId = 33;
        state.Projectiles[projectileId] = new SimProjectileData
        {
            ProjectileId = projectileId,
            CurrentPosition = new SimVector3(1f, 0f, -2f),
            LastPosition = new SimVector3(0.9f, 0f, -2f),
            HitRadius = 0.75f,
            IsDead = false
        };

        var entityManager = new EntityManager { Name = "EntityManagerOverlayTest" };
        root.AddChild(entityManager);
        _createdNodes.Add(entityManager);
        entityManager.Initialize(new StubSession(state));

        debugService.ProjectileHitGeometryEnabled = true;
        entityManager._Process(1.0 / 60.0);

        var status = entityManager.GetProjectileDebugOverlayStatus();
        AssertThat(status.GetValueOrDefault("debug_enabled", false).AsBool()).IsTrue();
        AssertThat(status.GetValueOrDefault("projectiles_in_state", 0).AsInt32()).IsEqual(1);
        AssertThat(status.GetValueOrDefault("radius_markers", 0).AsInt32()).IsEqual(1);
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
