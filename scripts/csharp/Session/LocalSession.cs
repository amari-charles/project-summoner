using System;
using System.Collections.Generic;
using Fateforged.Simulation;

namespace Fateforged.Session;

/// <summary>
/// Singleplayer session. Validates commands via CommandRouter, feeds them to
/// the Simulation, and ticks locally. The only session type needed for
/// campaign, tutorial, and AI battles.
/// </summary>
public class LocalSession : IGameSession
{
    private readonly Simulation.Simulation _simulation;
    private readonly CommandRouter _commandRouter;
    private readonly MatchState _state;
    private readonly List<SimEvent> _pendingEvents = new();

    public event Action<IReadOnlyList<SimEvent>>? SimEventsEmitted;

    public LocalSession(Simulation.Simulation simulation, CommandRouter commandRouter, MatchState state)
    {
        _simulation = simulation;
        _commandRouter = commandRouter;
        _state = state;
    }

    public MatchState GetState() => _state;

    public void SubmitCommand(ICommand command)
    {
        // Validate via CommandRouter, then queue into simulation
        throw new NotImplementedException();
    }

    public void Tick(float delta)
    {
        // Tick simulation, collect events, fire SimEventsEmitted
        throw new NotImplementedException();
    }
}
