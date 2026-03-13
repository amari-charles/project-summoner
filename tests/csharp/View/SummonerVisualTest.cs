namespace Fateforged.Tests.View;

using System;
using System.Collections.Generic;
using System.Reflection;
using Fateforged.Cards;
using Fateforged.Infrastructure.Debug;
using Fateforged.Session;
using Fateforged.Simulation;
using Fateforged.Simulation.Combat;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;
using Fateforged.Tests.Simulation;
using Fateforged.UI;
using Fateforged.Units;
using Fateforged.View;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class SummonerVisualTest
{
    private readonly List<Node> _createdNodes = [];

    [BeforeTest]
    public void Setup()
    {
        SummonerMeleeBubble.ClearOverrideRadius();
    }

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
    public void PollMatchState_CastingTransition_EmitsNonNullCardSignals()
    {
        var state = SimTestHelper.CreateBattleState();
        var session = new TestSession(state);
        var visual = CreateVisual(session);

        Card? played = null;
        Card? started = null;
        Card? completed = null;
        visual.Connect(
            SummonerVisual.SignalName.CardPlayed,
            Callable.From<Card>(card => played = card)
        );
        visual.Connect(
            SummonerVisual.SignalName.CastingStarted,
            Callable.From<Card, float>((card, _) => started = card)
        );
        visual.Connect(
            SummonerVisual.SignalName.CastingCompleted,
            Callable.From<Card>(card => completed = card)
        );

        var summoner = state.Summoners[0];
        summoner.IsCasting = true;
        summoner.CastingCatalogId = "fire_wisp";
        summoner.CastingTimeTotal = 1.5f;
        summoner.CastingTimeRemaining = 1.5f;

        InvokePollMatchState(visual);

        AssertThat(played).IsNotNull();
        AssertThat(started).IsNotNull();
        AssertThat(started!.CatalogId).IsEqual("fire_wisp");
        AssertThat(completed).IsNull();

        summoner.IsCasting = false;
        summoner.CastingTimeRemaining = 0f;
        InvokePollMatchState(visual);

        AssertThat(completed).IsNotNull();
        AssertThat(completed!.CatalogId).IsEqual("fire_wisp");
    }

    [TestCase]
    public void PollMatchState_InvalidCastingCatalog_ThrowsInvalidOperation()
    {
        var state = SimTestHelper.CreateBattleState();
        var session = new TestSession(state);
        var visual = CreateVisual(session);

        var summoner = state.Summoners[0];
        summoner.IsCasting = true;
        summoner.CastingCatalogId = "definitely_not_a_real_card";
        summoner.CastingTimeTotal = 1f;
        summoner.CastingTimeRemaining = 1f;

        bool threwInvalidOperation = false;
        try
        {
            InvokePollMatchState(visual);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException)
        {
            threwInvalidOperation = true;
        }

        AssertThat(threwInvalidOperation).IsTrue();
    }

    [TestCase]
    public void OnSummonerDamaged_WithAttacker_SetsDirectionalShieldRippleParameters()
    {
        var state = SimTestHelper.CreateBattleState();
        const int attackerId = 999;
        state.Units[attackerId] = new UnitData
        {
            UnitId = attackerId,
            Team = Team.Player,
            IsAlive = true,
            Position = new SimVector3(10f, 0f, 0f),
        };
        var session = new TestSession(state);
        var visual = CreateVisual(session);
        visual.GlobalPosition = Vector3.Zero;

        visual.OnSummonerDamaged(10f, attackerId);

        var pulse = visual.GetNodeOrNull<MeshInstance3D>("SummonerImpactPulse");
        AssertThat(pulse).IsNotNull();
        AssertThat(pulse!.Mesh is ArrayMesh).IsTrue();
        AssertThat(pulse.MaterialOverride is ShaderMaterial).IsTrue();
        AssertThat(pulse.GlobalPosition.X).IsEqualApprox(visual.GlobalPosition.X, 0.01f);
        AssertThat(pulse.GlobalPosition.Z).IsEqualApprox(visual.GlobalPosition.Z, 0.01f);

        var rippleMaterial = (ShaderMaterial)pulse.MaterialOverride!;
        var impactDirection = (Vector3)rippleMaterial.GetShaderParameter("impact_dir");
        AssertThat(impactDirection.X).IsGreater(0.95f);
        AssertThat(MathF.Abs(impactDirection.Z)).IsLess(0.05f);
        AssertThat(impactDirection.Y).IsGreater(0.1f);
    }

    [TestCase]
    public void OnSummonerDamaged_MissingAttacker_UsesDeterministicFallbackDirection()
    {
        var state = SimTestHelper.CreateBattleState();
        var session = new TestSession(state);
        var visual = CreateVisual(session);
        visual.GlobalPosition = new Vector3(3.5f, 0f, -2.25f);

        visual.OnSummonerDamaged(7f, attackerUnitId: 123456);

        var pulse = visual.GetNodeOrNull<MeshInstance3D>("SummonerImpactPulse");
        AssertThat(pulse).IsNotNull();
        AssertThat(pulse!.Mesh is ArrayMesh).IsTrue();
        AssertThat(pulse.MaterialOverride is ShaderMaterial).IsTrue();
        AssertThat(pulse!.GlobalPosition.X).IsEqualApprox(visual.GlobalPosition.X, 0.01f);
        AssertThat(pulse.GlobalPosition.Z).IsEqualApprox(visual.GlobalPosition.Z, 0.01f);

        var rippleMaterial = (ShaderMaterial)pulse.MaterialOverride!;
        var impactDirection = (Vector3)rippleMaterial.GetShaderParameter("impact_dir");
        var expectedFallback = new Vector3(0.71f, 0.02f, 0.68f).Normalized();
        AssertThat(impactDirection.X).IsEqualApprox(expectedFallback.X, 0.001f);
        AssertThat(impactDirection.Y).IsEqualApprox(expectedFallback.Y, 0.001f);
        AssertThat(impactDirection.Z).IsEqualApprox(expectedFallback.Z, 0.001f);
    }

    [TestCase]
    public void PhysicsProcess_SummonerBubbleDebugToggle_ControlsBubbleMarker()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var root = tree.Root;
        var debugService = new BattlefieldDebugService { Name = "BattlefieldDebug" };
        root.AddChild(debugService);
        _createdNodes.Add(debugService);

        var state = SimTestHelper.CreateBattleState();
        var session = new TestSession(state);
        var visual = CreateVisual(session);
        var markerField = typeof(SummonerVisual).GetField(
            "_debugSummonerBubbleMarker",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        AssertThat(markerField).IsNotNull();

        debugService.SummonerBubbleEnabled = false;
        visual._PhysicsProcess(1.0 / 60.0);
        AssertThat(markerField!.GetValue(visual)).IsNull();

        debugService.SummonerBubbleEnabled = true;
        visual._PhysicsProcess(1.0 / 60.0);
        var marker = markerField.GetValue(visual) as Node3D;
        AssertThat(marker).IsNotNull();
        var dome = marker!.GetNodeOrNull<MeshInstance3D>("Dome");
        var ring = marker.GetNodeOrNull<MeshInstance3D>("Ring");
        AssertThat(dome).IsNotNull();
        AssertThat(ring).IsNotNull();
        AssertThat(dome!.Scale.Y).IsEqualApprox(1f, 0.0001f);
        AssertThat(dome.Mesh is ArrayMesh).IsTrue();
        if (dome.MaterialOverride is StandardMaterial3D domeMaterial)
            AssertThat(domeMaterial.AlbedoColor.A).IsGreater(0.2f);
        else
            AssertThat(false).IsTrue();

        debugService.SummonerBubbleEnabled = false;
        visual._PhysicsProcess(1.0 / 60.0);
        AssertThat(markerField.GetValue(visual)).IsNull();
    }

    [TestCase]
    public void BeginDeath_SnapsHpBarToZeroImmediately()
    {
        var state = SimTestHelper.CreateBattleState();
        state.Summoners[0].CurrentHp = 40f;
        state.Summoners[0].MaxHp = 100f;
        var session = new TestSession(state);
        var visual = CreateVisual(session);

        visual._PhysicsProcess(1.0 / 60.0);
        var hpBar = GetPrivateField<FloatingHPBar>(visual, "_hpBar");
        AssertThat(hpBar).IsNotNull();

        visual.BeginDeath();

        float target = GetPrivateField<float>(hpBar!, "_targetHpPercent");
        float display = GetPrivateField<float>(hpBar!, "_displayHpPercent");
        AssertThat(target).IsEqual(0f);
        AssertThat(display).IsEqual(0f);
        AssertThat(visual.IsAlive).IsFalse();
    }

    private SummonerVisual CreateVisual(IGameSession session)
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var root = tree.Root;

        var visual = new SummonerVisual
        {
            Name = $"SummonerVisualTest_{_createdNodes.Count}",
            Team = 0,
        };
        root.AddChild(visual);
        _createdNodes.Add(visual);
        visual.Initialize(session, teamIndex: 0);
        return visual;
    }

    private static void InvokePollMatchState(SummonerVisual visual)
    {
        var poll = typeof(SummonerVisual).GetMethod(
            "PollMatchState",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        poll?.Invoke(visual, null);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        var field = target
            .GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        return (T)field!.GetValue(target)!;
    }

    private sealed class TestSession : IGameSession
    {
        private readonly MatchState _state;

        public TestSession(MatchState state)
        {
            _state = state;
        }

        public MatchState GetState() => _state;

        public event Action<IReadOnlyList<SimEvent>>? SimEventsEmitted
        {
            add { }
            remove { }
        }

        public void SubmitCommand(ICommand command) { }

        public void Tick(float delta) { }
    }
}
