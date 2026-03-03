namespace ProjectSummoner.Tests.Session;

using System.Collections.Generic;
using Fateforged.Session;
using Fateforged.Simulation;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using GdUnit4;
using ProjectSummoner.Tests.Simulation;
using static GdUnit4.Assertions;

[TestSuite]
public class LocalSessionTest
{
    private MatchState _state = null!;
    private Fateforged.Simulation.Simulation _simulation = null!;
    private CommandRouter _router = null!;
    private LocalSession _session = null!;

    [BeforeTest]
    public void Setup()
    {
        _state = SimTestHelper.CreateBattleState();
        _simulation = new Fateforged.Simulation.Simulation(_state);
        _router = new CommandRouter();
        _session = new LocalSession(_simulation, _router, _state);

        // Set up a hand with known cards
        var card = SimTestHelper.CreateSummonCard("test_unit", manaCost: 3);
        _state.CardDataMap["test_unit"] = card;
        _state.Summoners[0].Hand.Add("test_unit");
        _state.Summoners[0].Mana = 10f;
    }

    [TestCase]
    public void Tick_FiresSimEventsEmitted()
    {
        var emittedEvents = new List<IReadOnlyList<SimEvent>>();
        _session.SimEventsEmitted += events => emittedEvents.Add(events);

        _session.Tick(0.016f);

        // Battle phase tick should produce at least MatchTimeUpdatedEvent
        AssertThat(emittedEvents.Count).IsGreater(0);
    }

    [TestCase]
    public void SubmitCommand_ValidCommand_QueuedAndApplied()
    {
        var cmd = new PlayCardCommand(0, 0, new SimVector3(0f, 0f, 0f));
        _session.SubmitCommand(cmd);

        // Command should be in pending buffer
        AssertThat(_state.PendingCommandBuffer.Count).IsEqual(1);
        AssertThat(cmd.ExecuteFrame).IsEqual(_state.FrameNumber + 1);

        // Tick to execute it
        var emittedEvents = new List<IReadOnlyList<SimEvent>>();
        _session.SimEventsEmitted += events => emittedEvents.Add(events);
        _session.Tick(0.016f);

        // Command should have been drained
        AssertThat(_state.PendingCommandBuffer.Count).IsEqual(0);

        // Should have casting started event
        AssertThat(emittedEvents.Count).IsGreater(0);
        var allEvents = new List<SimEvent>();
        foreach (var batch in emittedEvents)
            allEvents.AddRange(batch);
        AssertThat(SimTestHelper.CountEvents<CastingStartedEvent>(allEvents)).IsGreater(0);
    }

    [TestCase]
    public void SubmitCommand_InvalidCommand_Rejected()
    {
        _state.Summoners[0].Mana = 0f; // Not enough mana
        var cmd = new PlayCardCommand(0, 0, SimVector3.Zero);
        _session.SubmitCommand(cmd);

        // Command should NOT be in pending buffer
        AssertThat(_state.PendingCommandBuffer.Count).IsEqual(0);
    }

    [TestCase]
    public void GetState_ReturnsSameState()
    {
        AssertThat(_session.GetState()).IsSame(_state);
    }

    [TestCase]
    public void Integration_PlayCard_ThenTick_CastingStarted()
    {
        var cmd = new PlayCardCommand(0, 0, new SimVector3(5f, 0f, 0f));
        _session.SubmitCommand(cmd);

        var allEvents = new List<SimEvent>();
        _session.SimEventsEmitted += events => allEvents.AddRange(events);
        _session.Tick(0.016f);

        var castingEvent = SimTestHelper.FindEvent<CastingStartedEvent>(allEvents);
        AssertThat(castingEvent).IsNotNull();
        AssertThat(castingEvent!.Team).IsEqual(0);
    }
}
