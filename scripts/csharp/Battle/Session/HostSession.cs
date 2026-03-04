using System;
using System.Collections.Generic;
using Fateforged.Simulation;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;

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
        var result = _commandRouter.Validate(command, _state);
        if (!result.IsValid)
        {
            Simulation.Simulation.Log?.Invoke($"[HostSession] Command rejected: {result.Reason}");
            return;
        }

        command.ExecuteFrame = _state.FrameNumber + 1;
        _state.PendingCommandBuffer.Add(command);
    }

    public void Tick(float delta)
    {
        var events = _simulation.Tick(delta);
        // TODO: broadcast snapshot to clients via transport (no transport wired yet)
        if (events.Count > 0)
        {
            SimEventsEmitted?.Invoke(events);
        }
    }

    /// <summary>
    /// Handle a command received from a remote client.
    /// Validates via CommandRouter before feeding to simulation.
    /// </summary>
    public void HandleRemoteCommand(int senderId, ICommand command)
    {
        var result = _commandRouter.Validate(command, _state);
        if (!result.IsValid)
        {
            Simulation.Simulation.Log?.Invoke(
                $"[HostSession] Remote command from {senderId} rejected: {result.Reason}");
            return;
        }

        command.ExecuteFrame = _state.FrameNumber + 1;
        _state.PendingCommandBuffer.Add(command);
    }
}
