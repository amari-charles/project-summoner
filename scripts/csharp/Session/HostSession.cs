using System;
using System.Collections.Generic;
using Fateforged.Simulation;

namespace Fateforged.Session;

/// <summary>
/// Multiplayer host session. The authority — ticks the simulation locally,
/// validates all commands via CommandRouter, broadcasts snapshots to clients
/// after each tick.
/// </summary>
public class HostSession : NetworkSession
{
    private readonly Simulation.Simulation _simulation;
    private readonly CommandRouter _commandRouter;
    private readonly MatchState _state;
    private readonly List<SimEvent> _pendingEvents = new();

    public override event Action<IReadOnlyList<SimEvent>>? SimEventsEmitted;

    public HostSession(Simulation.Simulation simulation, CommandRouter commandRouter, MatchState state)
    {
        _simulation = simulation;
        _commandRouter = commandRouter;
        _state = state;
    }

    public override MatchState GetState() => _state;

    public override void SubmitCommand(ICommand command)
    {
        // Validate via CommandRouter, then queue into simulation
        throw new NotImplementedException();
    }

    public override void Tick(float delta)
    {
        // Tick simulation, collect events, broadcast snapshots, fire SimEventsEmitted
        throw new NotImplementedException();
    }

    /// <summary>
    /// Handle a command received from a remote client.
    /// Validates via CommandRouter before feeding to simulation.
    /// </summary>
    public void HandleRemoteCommand(int senderId, ICommand command)
    {
        throw new NotImplementedException();
    }
}
