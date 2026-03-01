namespace Fateforged.Multiplayer.Core;

/// <summary>
/// Abstraction for broadcasting protocol messages to all clients.
/// Implemented by MatchSession for production use.
/// Tests can provide a capturing implementation to inspect broadcast output
/// without requiring a Godot scene tree or network transport.
/// </summary>
public interface IMessageBroadcaster
{
    void Broadcast(object message);
}
