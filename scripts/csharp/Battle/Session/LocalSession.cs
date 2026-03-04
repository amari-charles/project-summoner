using System;
using System.Collections.Generic;
using Fateforged.Simulation;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;

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
        var result = _commandRouter.Validate(command, _state);
        if (!result.IsValid)
        {
            Simulation.Simulation.Log?.Invoke($"[LocalSession] Command rejected: {result.Reason}");
            return;
        }

        command.ExecuteFrame = _state.FrameNumber + 1;
        _state.PendingCommandBuffer.Add(command);
    }

    public void Tick(float delta)
    {
        var events = _simulation.Tick(delta);
        if (events.Count > 0)
        {
            SimEventsEmitted?.Invoke(events);
        }
    }
}
