using System;
using System.Threading.Tasks;
using Godot;
using Fateforged.Multiplayer.Backend;
using Fateforged.Multiplayer.Transport;

namespace Fateforged.Multiplayer.Core;

/// <summary>
/// Handles connection loss detection and automatic reconnection attempts.
/// Works with both P2P and Nakama-based connections.
/// </summary>
public partial class ReconnectionHandler : Node
{
    #region Configuration

    /// <summary>
    /// Maximum number of reconnection attempts before giving up.
    /// </summary>
    private const int MaxReconnectAttempts = 5;

    /// <summary>
    /// Base delay between reconnection attempts (exponential backoff).
    /// </summary>
    private const float BaseReconnectDelay = 1.0f;

    /// <summary>
    /// Maximum delay between reconnection attempts.
    /// </summary>
    private const float MaxReconnectDelay = 15.0f;

    /// <summary>
    /// Time before considering a connection lost (no ping response).
    /// </summary>
    private const float ConnectionTimeoutSeconds = 10.0f;

    #endregion

    #region State

    public enum ConnectionState
    {
        Connected,
        Disconnected,
        Reconnecting,
        Failed
    }

    private ConnectionState _state = ConnectionState.Disconnected;
    private int _reconnectAttempts;
    private float _lastPingTime;
    private float _reconnectTimer;
    private string? _lastAddress;
    private int _lastPort;
    private bool _wasHost;

    /// <summary>
    /// Current connection state.
    /// </summary>
    public ConnectionState State => _state;

    /// <summary>
    /// Number of reconnection attempts made.
    /// </summary>
    public int ReconnectAttempts => _reconnectAttempts;

    /// <summary>
    /// Whether we're currently trying to reconnect.
    /// </summary>
    public bool IsReconnecting => _state == ConnectionState.Reconnecting;

    #endregion

    #region Signals

    /// <summary>
    /// Emitted when connection is lost.
    /// </summary>
    [Signal]
    public delegate void ConnectionLostEventHandler(string reason);

    /// <summary>
    /// Emitted when reconnection starts.
    /// </summary>
    [Signal]
    public delegate void ReconnectingEventHandler(int attempt, int maxAttempts);

    /// <summary>
    /// Emitted when reconnection succeeds.
    /// </summary>
    [Signal]
    public delegate void ReconnectedEventHandler();

    /// <summary>
    /// Emitted when all reconnection attempts fail.
    /// </summary>
    [Signal]
    public delegate void ReconnectionFailedEventHandler();

    /// <summary>
    /// Emitted when state needs to be resynced after reconnection.
    /// </summary>
    [Signal]
    public delegate void StateResyncRequestedEventHandler();

    #endregion

    #region Singleton

    /// <summary>
    /// Global instance of the ReconnectionHandler.
    /// </summary>
    public static ReconnectionHandler? Instance { get; private set; }

    #endregion

    #region Lifecycle

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public override void _Ready()
    {
        GD.Print("[ReconnectionHandler] Initialized");
    }

    public override void _Process(double delta)
    {
        if (_state == ConnectionState.Reconnecting)
        {
            _reconnectTimer -= (float)delta;
            if (_reconnectTimer <= 0)
            {
                AttemptReconnect();
            }
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Record successful connection details for potential reconnection.
    /// Call this when a connection is established.
    /// </summary>
    public void RecordConnection(string address, int port, bool isHost)
    {
        _lastAddress = address;
        _lastPort = port;
        _wasHost = isHost;
        _state = ConnectionState.Connected;
        _reconnectAttempts = 0;
        _lastPingTime = 0;

        GD.Print($"[ReconnectionHandler] Recorded connection: {address}:{port}, host={isHost}");
    }

    /// <summary>
    /// Record connection loss and optionally start reconnection.
    /// </summary>
    /// <param name="reason">Reason for disconnection</param>
    /// <param name="autoReconnect">Whether to attempt automatic reconnection</param>
    public void HandleDisconnection(string reason, bool autoReconnect = true)
    {
        if (_state == ConnectionState.Disconnected || _state == ConnectionState.Failed)
        {
            return; // Already disconnected
        }

        GD.Print($"[ReconnectionHandler] Connection lost: {reason}");
        _state = ConnectionState.Disconnected;

        EmitSignal(SignalName.ConnectionLost, reason);

        if (autoReconnect && !_wasHost && !string.IsNullOrEmpty(_lastAddress))
        {
            StartReconnecting();
        }
        else
        {
            // Hosts can't reconnect to themselves, and we can't reconnect without address
            _state = ConnectionState.Failed;
            EmitSignal(SignalName.ReconnectionFailed);
        }
    }

    /// <summary>
    /// Cancel any ongoing reconnection attempts.
    /// </summary>
    public void CancelReconnection()
    {
        if (_state == ConnectionState.Reconnecting)
        {
            GD.Print("[ReconnectionHandler] Reconnection cancelled");
            _state = ConnectionState.Disconnected;
            _reconnectAttempts = 0;
        }
    }

    /// <summary>
    /// Update ping time - call this when a valid message is received.
    /// </summary>
    public void RecordPing()
    {
        _lastPingTime = (float)Time.GetTicksMsec() / 1000.0f;
    }

    /// <summary>
    /// Check if connection has timed out based on ping tracking.
    /// </summary>
    public bool HasTimedOut()
    {
        if (_state != ConnectionState.Connected) return false;
        if (_lastPingTime == 0) return false;

        float now = (float)Time.GetTicksMsec() / 1000.0f;
        return (now - _lastPingTime) > ConnectionTimeoutSeconds;
    }

    /// <summary>
    /// Reset state when leaving a match.
    /// </summary>
    public void Reset()
    {
        _state = ConnectionState.Disconnected;
        _reconnectAttempts = 0;
        _lastAddress = null;
        _lastPort = 0;
        _wasHost = false;
        _lastPingTime = 0;
    }

    #endregion

    #region Private Methods

    private void StartReconnecting()
    {
        _state = ConnectionState.Reconnecting;
        _reconnectAttempts = 0;
        ScheduleReconnectAttempt();
    }

    private void ScheduleReconnectAttempt()
    {
        // Exponential backoff with jitter
        float delay = MathF.Min(
            BaseReconnectDelay * MathF.Pow(2, _reconnectAttempts),
            MaxReconnectDelay
        );
        // Add jitter (±20%)
        float jitter = delay * 0.2f * ((float)GD.Randf() * 2 - 1);
        _reconnectTimer = delay + jitter;

        GD.Print($"[ReconnectionHandler] Next reconnect attempt in {_reconnectTimer:F1}s");
    }

    private void AttemptReconnect()
    {
        _reconnectAttempts++;

        if (_reconnectAttempts > MaxReconnectAttempts)
        {
            GD.Print("[ReconnectionHandler] Max reconnection attempts reached");
            _state = ConnectionState.Failed;
            EmitSignal(SignalName.ReconnectionFailed);
            return;
        }

        GD.Print($"[ReconnectionHandler] Reconnection attempt {_reconnectAttempts}/{MaxReconnectAttempts}");
        EmitSignal(SignalName.Reconnecting, _reconnectAttempts, MaxReconnectAttempts);

        // The Reconnecting signal notifies the UI/lobby layer to attempt reconnection
        // The UI is responsible for actually calling Connect on the transport
        // If no listener handles it, we'll timeout and try again

        // Schedule the next attempt in case this one fails
        ScheduleReconnectAttempt();
    }

    /// <summary>
    /// Call this when a reconnection attempt fails (e.g., from UI/transport callback).
    /// </summary>
    public void HandleReconnectionAttemptFailed()
    {
        if (_state != ConnectionState.Reconnecting) return;

        GD.Print("[ReconnectionHandler] Reconnection attempt failed");
        // Next attempt will be triggered by the timer
    }

    /// <summary>
    /// Called when reconnection succeeds.
    /// </summary>
    public void HandleReconnectionSuccess()
    {
        if (_state != ConnectionState.Reconnecting) return;

        GD.Print("[ReconnectionHandler] Reconnection successful!");
        _state = ConnectionState.Connected;
        _reconnectAttempts = 0;

        EmitSignal(SignalName.Reconnected);

        // Request state resync from host
        EmitSignal(SignalName.StateResyncRequested);
    }

    #endregion
}

/// <summary>
/// Message for requesting state resync after reconnection.
/// </summary>
public class StateResyncRequest
{
    /// <summary>
    /// Client's last known frame number.
    /// </summary>
    public long LastKnownFrame { get; set; }

    /// <summary>
    /// Client's peer ID.
    /// </summary>
    public int PeerId { get; set; }
}

/// <summary>
/// Message containing full state snapshot for reconnected client.
/// </summary>
public class StateResyncResponse
{
    /// <summary>
    /// Current server frame.
    /// </summary>
    public long CurrentFrame { get; set; }

    /// <summary>
    /// Full state snapshot data.
    /// </summary>
    public byte[]? StateData { get; set; }

    /// <summary>
    /// Player 0's current score/health.
    /// </summary>
    public int Player0Health { get; set; }

    /// <summary>
    /// Player 1's current score/health.
    /// </summary>
    public int Player1Health { get; set; }

    /// <summary>
    /// Player 0's mana.
    /// </summary>
    public int Player0Mana { get; set; }

    /// <summary>
    /// Player 1's mana.
    /// </summary>
    public int Player1Mana { get; set; }
}
