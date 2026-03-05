namespace Fateforged.Tests.View;

using System;
using System.Collections.Generic;
using System.Reflection;
using Fateforged.Session;
using Fateforged.Simulation;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;
using Fateforged.Tests.Simulation;
using Fateforged.View;
using Fateforged.Cards;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class SummonerVisualTest
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
    public void PollMatchState_CastingTransition_EmitsNonNullCardSignals()
    {
        var state = SimTestHelper.CreateBattleState();
        var session = new TestSession(state);
        var visual = CreateVisual(session);

        Card? played = null;
        Card? started = null;
        Card? completed = null;
        visual.Connect(SummonerVisual.SignalName.CardPlayed, Callable.From<Card>(card => played = card));
        visual.Connect(SummonerVisual.SignalName.CastingStarted, Callable.From<Card, float>((card, _) => started = card));
        visual.Connect(SummonerVisual.SignalName.CastingCompleted, Callable.From<Card>(card => completed = card));

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

    private SummonerVisual CreateVisual(IGameSession session)
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var root = tree.Root;

        var visual = new SummonerVisual
        {
            Name = $"SummonerVisualTest_{_createdNodes.Count}",
            Team = 0
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

        public void SubmitCommand(ICommand command)
        {
        }

        public void Tick(float delta)
        {
        }
    }
}
