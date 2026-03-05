using System;
using System.Collections.Generic;
using Fateforged.Simulation;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;
using Fateforged.Multiplayer.Protocol;
using Fateforged.Multiplayer.Transport;
using Godot;
using Godot.Collections;

namespace Fateforged.Session;

/// <summary>
/// Abstract base for multiplayer sessions. Holds shared network infrastructure:
/// transport wiring, message serialization, and reconnect state.
/// </summary>
public abstract class NetworkSession : IGameSession, IDisposable
{
    protected readonly IMatchTransport _transport;
    protected readonly MessageSerializer _messageSerializer = new();
    private bool _disposed;

    public bool IsAwaitingReconnect { get; protected set; }
    public float ReconnectRemainingSeconds { get; protected set; }
    public string ReconnectReason { get; protected set; } = "";

    protected NetworkSession(IMatchTransport transport)
    {
        _transport = transport;
        _transport.OnMessageReceived += OnTransportMessageReceived;
        _transport.OnPeerConnected += OnTransportPeerConnected;
        _transport.OnPeerDisconnected += OnTransportPeerDisconnected;
        _transport.OnConnected += OnTransportConnected;
        _transport.OnDisconnected += OnTransportDisconnected;
    }

    public abstract MatchState GetState();
    public abstract event Action<IReadOnlyList<SimEvent>>? SimEventsEmitted;
    public abstract void SubmitCommand(ICommand command);
    public abstract void Tick(float delta);

    protected abstract void HandleMessage(int senderId, IProtocolMessage message);

    protected virtual void HandleDisconnect(string reason)
    {
        // Override in concrete session if disconnect should end match.
    }

    protected virtual void HandleConnected()
    {
    }

    protected virtual void HandlePeerConnected(int peerId)
    {
    }

    protected virtual void HandlePeerDisconnected(int peerId)
    {
    }

    private void OnTransportMessageReceived(int senderId, Dictionary message)
    {
        IProtocolMessage deserialized;
        try
        {
            deserialized = _messageSerializer.Deserialize(message);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[{GetType().Name}] Failed to deserialize transport message from {senderId}: {ex.Message}");
            return;
        }

        HandleMessage(senderId, deserialized);
    }

    private void OnTransportDisconnected(string reason)
    {
        HandleDisconnect(reason);
    }

    private void OnTransportConnected()
    {
        HandleConnected();
    }

    private void OnTransportPeerConnected(int peerId)
    {
        HandlePeerConnected(peerId);
    }

    private void OnTransportPeerDisconnected(int peerId)
    {
        HandlePeerDisconnected(peerId);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _transport.OnMessageReceived -= OnTransportMessageReceived;
        _transport.OnPeerConnected -= OnTransportPeerConnected;
        _transport.OnPeerDisconnected -= OnTransportPeerDisconnected;
        _transport.OnConnected -= OnTransportConnected;
        _transport.OnDisconnected -= OnTransportDisconnected;
        _disposed = true;
    }
}
