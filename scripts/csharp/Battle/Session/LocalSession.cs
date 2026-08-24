using System;
using System.Collections.Generic;
using Fateforged.Simulation;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;

namespace Fateforged.Session;

/// <summary>
/// Singleplayer session. Validates commands via CommandRouter, feeds them to
/// the Simulation, and ticks locally. The only session type needed for
/// authored, tutorial, and AI battles.
/// </summary>
public class LocalSession : IGameSession
{
    private readonly Simulation.Simulation _simulation;
    private readonly CommandRouter _commandRouter;
    private readonly MatchState _state;

    public event Action<IReadOnlyList<SimEvent>>? SimEventsEmitted;

    public LocalSession(
        Simulation.Simulation simulation,
        CommandRouter commandRouter,
        MatchState state
    )
    {
        _simulation = simulation;
        _commandRouter = commandRouter;
        _state = state;
    }

    public MatchState GetState() => _state;

    public void SubmitCommand(ICommand command)
    {
        if (command is PlayCardCommand play && HasMatchingPendingPlay(play))
            return;

        var result = _commandRouter.Validate(command, _state);
        if (!result.IsValid)
        {
            Simulation.Simulation.Log?.Invoke($"[LocalSession] Command rejected: {result.Reason}");
            return;
        }

        command.ExecuteFrame = _state.FrameNumber + 1;
        _state.PendingCommandBuffer.Add(command);
    }

    private bool HasMatchingPendingPlay(PlayCardCommand incoming)
    {
        foreach (var pending in _state.PendingCommandBuffer)
        {
            if (pending is not PlayCardCommand existing)
                continue;

            if (existing.Team != incoming.Team || existing.CardIndex != incoming.CardIndex)
                continue;

            if (!SamePosition(existing.SpawnPosition, incoming.SpawnPosition))
                continue;

            return true;
        }

        return false;
    }

    private static bool SamePosition(SimVector3 a, SimVector3 b)
    {
        const float epsilon = 0.001f;
        return Math.Abs(a.X - b.X) <= epsilon
            && Math.Abs(a.Y - b.Y) <= epsilon
            && Math.Abs(a.Z - b.Z) <= epsilon;
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
