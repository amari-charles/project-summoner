using System;
using System.Collections.Generic;
using Fateforged.Simulation;

namespace Fateforged.Session;

/// <summary>
/// Multiplayer client session. Does NOT tick the simulation — sends local
/// commands to the host over the network and applies snapshots received
/// from the host to a local copy of MatchState.
/// </summary>
public class ClientSession : NetworkSession
{
    private readonly MatchState _localState = new();
    private readonly List<SimEvent> _pendingEvents = new();

    public override event Action<IReadOnlyList<SimEvent>>? SimEventsEmitted;

    public override MatchState GetState() => _localState;

    public override void SubmitCommand(ICommand command)
    {
        // Send command to host over network (no local validation)
        throw new NotImplementedException();
    }

    public override void Tick(float delta)
    {
        // Apply latest snapshot from host, fire SimEventsEmitted
        throw new NotImplementedException();
    }

    /// <summary>
    /// Apply an authoritative state snapshot received from the host.
    /// </summary>
    public void ApplySnapshot(MatchState snapshot)
    {
        throw new NotImplementedException();
    }
}
