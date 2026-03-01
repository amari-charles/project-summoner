using Fateforged.Multiplayer.Protocol;

namespace Fateforged.Multiplayer.Core;

/// <summary>
/// Interface for match runners (host or client).
/// Handles the core simulation loop and message processing.
/// </summary>
public interface IMatchRunner
{
    /// <summary>
    /// Initialize the runner with the match session.
    /// SimulationNode.Current must exist before this is called.
    /// </summary>
    void Initialize(MatchSession session);

    /// <summary>
    /// Process a single frame of the simulation.
    /// </summary>
    /// <param name="delta">Time since last frame in seconds</param>
    void ProcessFrame(double delta);

    /// <summary>
    /// Handle an incoming network message.
    /// </summary>
    void HandleMessage(int senderId, object message);

    /// <summary>
    /// Request to play a card (local player action).
    /// </summary>
    void RequestCardPlay(int cardIndex, Godot.Vector3 position);

    /// <summary>
    /// Request to forfeit the match.
    /// </summary>
    void RequestForfeit();

    /// <summary>
    /// Clean up when match ends.
    /// </summary>
    void Cleanup();
}
