using System;
using System.Collections.Generic;
using Fateforged.Simulation;

namespace Fateforged.Session;

/// <summary>
/// Abstract base for multiplayer sessions. Holds shared network infrastructure:
/// IdentityMap (UnitId ↔ NetworkId), SnapshotCodec, DesyncChecker.
/// </summary>
public abstract class NetworkSession : IGameSession
{
    protected readonly IdentityMap _identityMap = new();
    protected readonly SnapshotCodec _snapshotCodec = new();

    public abstract MatchState GetState();
    public abstract event Action<IReadOnlyList<SimEvent>>? SimEventsEmitted;
    public abstract void SubmitCommand(ICommand command);
    public abstract void Tick(float delta);

    /// <summary>
    /// Process incoming network data (snapshots, commands, desync reports).
    /// </summary>
    public void HandleMessage(object message)
    {
        throw new NotImplementedException();
    }
}
