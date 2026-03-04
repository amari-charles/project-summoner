using System;
using Godot.Collections;

namespace Fateforged.Multiplayer.Transport;

/// <summary>
/// Interface for network transport implementations.
/// Abstracts the underlying network layer (P2P, Nakama relay, dedicated server).
/// </summary>
public interface IMatchTransport
{
    /// <summary>
    /// Whether currently connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Local peer ID in the network.
    /// </summary>
    int LocalPeerId { get; }

    /// <summary>
    /// Whether this transport is acting as host.
    /// </summary>
    bool IsHost { get; }

    /// <summary>
    /// Send a message to the host (client → host).
    /// </summary>
    void Send(Dictionary message);

    /// <summary>
    /// Broadcast a message to all connected peers (host only).
    /// </summary>
    void Broadcast(Dictionary message);

    /// <summary>
    /// Send a message to a specific peer (host only).
    /// </summary>
    void SendTo(int peerId, Dictionary message);

    /// <summary>
    /// Fired when a message is received.
    /// Parameters: senderId, message
    /// </summary>
    event Action<int, Dictionary>? OnMessageReceived;

    /// <summary>
    /// Fired when a peer connects.
    /// </summary>
    event Action<int>? OnPeerConnected;

    /// <summary>
    /// Fired when a peer disconnects.
    /// </summary>
    event Action<int>? OnPeerDisconnected;

    /// <summary>
    /// Fired when connection is established.
    /// </summary>
    event Action? OnConnected;

    /// <summary>
    /// Fired when disconnected from host/server.
    /// </summary>
    event Action<string>? OnDisconnected;

    /// <summary>
    /// Start hosting on a port.
    /// </summary>
    void Host(int port);

    /// <summary>
    /// Connect to a host.
    /// </summary>
    void Connect(string address, int port);

    /// <summary>
    /// Disconnect from the network.
    /// </summary>
    void Disconnect();
}
