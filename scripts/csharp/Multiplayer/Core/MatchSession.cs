using System;
using Godot;
using Fateforged.Multiplayer.Protocol;
using Fateforged.Multiplayer.Transport;

namespace Fateforged.Multiplayer.Core;

/// <summary>
/// Central orchestrator for a multiplayer match.
/// Manages the match lifecycle, network communication, and game state synchronization.
/// </summary>
public partial class MatchSession : Node
{
    #region Singleton

    /// <summary>
    /// The currently active match session.
    /// Null when not in a multiplayer match.
    /// </summary>
    public static MatchSession? Current { get; private set; }

    #endregion

    #region Configuration

    /// <summary>
    /// Random seed for deterministic simulation.
    /// </summary>
    public long Seed { get; private set; }

    /// <summary>
    /// Unique match identifier.
    /// </summary>
    public string MatchId { get; private set; } = "";

    /// <summary>
    /// Player IDs in the match (index 0 = player 1, index 1 = player 2).
    /// </summary>
    public string[] PlayerIds { get; private set; } = Array.Empty<string>();

    /// <summary>
    /// Summoner IDs each player is using.
    /// </summary>
    public string[] SummonerIds { get; private set; } = Array.Empty<string>();

    /// <summary>
    /// Whether this instance is the host/authority.
    /// </summary>
    public bool IsHost { get; private set; }

    /// <summary>
    /// Local player's index (0 or 1).
    /// </summary>
    public int LocalPlayerIndex { get; private set; }

    /// <summary>
    /// Whether the match is currently active.
    /// </summary>
    public bool IsActive { get; private set; }

    #endregion

    #region State

    /// <summary>
    /// Current simulation frame number.
    /// </summary>
    public long CurrentFrame { get; set; }

    /// <summary>
    /// Elapsed match time in seconds.
    /// </summary>
    public float MatchTime { get; set; }

    /// <summary>
    /// Registry mapping network IDs to nodes.
    /// </summary>
    public NetworkIdRegistry NetworkIds { get; } = new();

    #endregion

    #region Components

    private IMatchRunner? _runner;
    private IMatchTransport? _transport;
    private readonly MessageSerializer _serializer = new();

    #endregion

    #region Events

    /// <summary>
    /// Fired when a card play is confirmed by the host.
    /// </summary>
    [Signal]
    public delegate void CardPlayConfirmedEventHandler(int sequence, int playerIndex, int cardIndex, Vector3 position, int unitNetworkId);

    /// <summary>
    /// Fired when a card play is rejected by the host.
    /// </summary>
    [Signal]
    public delegate void CardPlayRejectedEventHandler(int sequence, int playerIndex, string reason);

    /// <summary>
    /// Fired when a unit is spawned.
    /// </summary>
    [Signal]
    public delegate void UnitSpawnedEventHandler(int networkId, string unitType, int team, Vector3 position);

    /// <summary>
    /// Fired when a unit dies.
    /// </summary>
    [Signal]
    public delegate void UnitDiedEventHandler(int networkId, int killerNetworkId);

    /// <summary>
    /// Fired when damage is dealt.
    /// </summary>
    [Signal]
    public delegate void DamageDealtEventHandler(int targetNetworkId, float amount, bool isCrit, int sourceNetworkId);

    /// <summary>
    /// Fired when a summoner takes damage.
    /// </summary>
    [Signal]
    public delegate void SummonerDamagedEventHandler(int team, float amount, float newHp);

    /// <summary>
    /// Fired when a state snapshot is received (client only).
    /// </summary>
    [Signal]
    public delegate void StateSnapshotReceivedEventHandler(long frame, float matchTime);

    /// <summary>
    /// Fired when the match ends.
    /// </summary>
    [Signal]
    public delegate void MatchEndedEventHandler(int winnerIndex, string reason, float duration);

    /// <summary>
    /// Fired when the match starts.
    /// </summary>
    [Signal]
    public delegate void MatchStartedEventHandler();

    /// <summary>
    /// Fired when connection is lost.
    /// </summary>
    [Signal]
    public delegate void ConnectionLostEventHandler(string reason);

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        // MatchSession is typically added as a child of a scene, not an autoload
    }

    public override void _Process(double delta)
    {
        if (IsActive && _runner != null)
        {
            _runner.ProcessFrame(delta);
        }
    }

    /// <summary>
    /// Start a match as host or client.
    /// </summary>
    /// <param name="config">Match configuration from host</param>
    /// <param name="transport">Network transport to use</param>
    /// <param name="asHost">Whether to run as host/authority</param>
    /// <param name="localPlayerIndex">This player's index (0 or 1)</param>
    public void StartMatch(MatchStarted config, IMatchTransport transport, bool asHost, int localPlayerIndex)
    {
        Seed = config.Seed;
        MatchId = config.MatchId;
        PlayerIds = config.PlayerIds;
        SummonerIds = config.SummonerIds;
        IsHost = asHost;
        LocalPlayerIndex = localPlayerIndex;

        _transport = transport;
        _transport.OnMessageReceived += HandleRawMessage;
        _transport.OnPeerDisconnected += HandlePeerDisconnected;
        _transport.OnDisconnected += HandleTransportDisconnected;

        // Record connection info for potential reconnection
        if (!asHost && ReconnectionHandler.Instance != null)
        {
            // Note: Address/port would need to be passed in or stored by transport
            // For now, just mark as connected
            ReconnectionHandler.Instance.RecordConnection("", 0, asHost);
        }

        // Create appropriate runner
        if (asHost)
        {
            _runner = new Authority.HostRunner();
        }
        else
        {
            _runner = new Client.ClientRunner();
        }
        _runner.Initialize(this);

        CurrentFrame = 0;
        MatchTime = 0;
        IsActive = true;
        Current = this;

        EmitSignal(SignalName.MatchStarted);
        GD.Print($"[MatchSession] Match started. Host: {IsHost}, Player: {LocalPlayerIndex}, Seed: {Seed}");
    }

    /// <summary>
    /// End the current match.
    /// </summary>
    public void EndMatch(int winnerIndex, string reason)
    {
        if (!IsActive) return;

        IsActive = false;
        _runner?.Cleanup();
        _runner = null;

        if (_transport != null)
        {
            _transport.OnMessageReceived -= HandleRawMessage;
            _transport.OnPeerDisconnected -= HandlePeerDisconnected;
            _transport.OnDisconnected -= HandleTransportDisconnected;
        }

        // Reset reconnection handler
        ReconnectionHandler.Instance?.Reset();

        NetworkIds.Clear();
        Current = null;

        EmitSignal(SignalName.MatchEnded, winnerIndex, reason, MatchTime);
        GD.Print($"[MatchSession] Match ended. Winner: {winnerIndex}, Reason: {reason}");
    }

    /// <summary>
    /// Broadcast match end to all clients (host only).
    /// Called by the game controller when a win condition is detected.
    /// </summary>
    /// <param name="winnerIndex">0 = player 1 (host), 1 = player 2 (client)</param>
    /// <param name="reason">Reason for match end (e.g., "Summoner destroyed", "Forfeit")</param>
    public void BroadcastMatchEnd(int winnerIndex, string reason)
    {
        if (!IsHost || !IsActive) return;

        var endMessage = new Protocol.MatchEnded(winnerIndex, reason, MatchTime);
        Broadcast(endMessage);

        GD.Print($"[MatchSession] Broadcast match end: winner {winnerIndex}, reason: {reason}");

        // End match locally after broadcast
        EndMatch(winnerIndex, reason);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Request to play a card. Goes through the runner (prediction for client, direct for host).
    /// </summary>
    public void RequestCardPlay(int cardIndex, Vector3 position)
    {
        if (!IsActive || _runner == null) return;
        _runner.RequestCardPlay(cardIndex, position);
    }

    /// <summary>
    /// Request to forfeit the match.
    /// </summary>
    public void RequestForfeit()
    {
        if (!IsActive || _runner == null) return;
        _runner.RequestForfeit();
    }

    /// <summary>
    /// Send a message through the transport.
    /// </summary>
    public void Send(object message)
    {
        if (_transport == null) return;
        var dict = _serializer.Serialize(message);
        _transport.Send(dict);
    }

    /// <summary>
    /// Broadcast a message to all peers (host only).
    /// </summary>
    public void Broadcast(object message)
    {
        if (_transport == null || !IsHost) return;
        var dict = _serializer.Serialize(message);
        _transport.Broadcast(dict);
    }

    /// <summary>
    /// Send a message to a specific peer (host only).
    /// </summary>
    public void SendTo(int peerId, object message)
    {
        if (_transport == null || !IsHost) return;
        var dict = _serializer.Serialize(message);
        _transport.SendTo(peerId, dict);
    }

    #endregion

    #region Message Handling

    private void HandleRawMessage(int senderId, Godot.Collections.Dictionary dict)
    {
        try
        {
            var message = _serializer.Deserialize(dict);
            _runner?.HandleMessage(senderId, message);
            DispatchMessageEvent(message);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[MatchSession] Failed to deserialize message: {ex.Message}");
        }
    }

    private void DispatchMessageEvent(object message)
    {
        switch (message)
        {
            case CardPlayConfirmed m:
                EmitSignal(SignalName.CardPlayConfirmed, m.Sequence, m.PlayerIndex, m.CardIndex, m.Position, m.SpawnedUnitNetworkId);
                break;

            case CardPlayRejected m:
                EmitSignal(SignalName.CardPlayRejected, m.Sequence, m.PlayerIndex, m.Reason);
                break;

            case UnitSpawned m:
                EmitSignal(SignalName.UnitSpawned, m.NetworkId, m.UnitType, m.Team, m.Position);
                break;

            case UnitDied m:
                EmitSignal(SignalName.UnitDied, m.NetworkId, m.KillerNetworkId ?? -1);
                break;

            case DamageDealt m:
                EmitSignal(SignalName.DamageDealt, m.TargetNetworkId, m.Amount, m.IsCrit, m.SourceNetworkId ?? -1);
                break;

            case SummonerDamaged m:
                EmitSignal(SignalName.SummonerDamaged, m.Team, m.Amount, m.NewHp);
                break;

            case StateSnapshot m:
                EmitSignal(SignalName.StateSnapshotReceived, m.Frame, m.MatchTime);
                break;

            case Protocol.MatchEnded m:
                EndMatch(m.WinnerIndex, m.Reason);
                break;
        }
    }

    private void HandlePeerDisconnected(int peerId)
    {
        if (!IsActive) return;

        GD.Print($"[MatchSession] Peer {peerId} disconnected");

        // If the host disconnects, end the match
        if (!IsHost && peerId == 1)
        {
            EmitSignal(SignalName.ConnectionLost, "Host disconnected");
            EndMatch(-1, "Host disconnected");
        }
        // If a client disconnects (and we're host), they forfeit
        else if (IsHost)
        {
            // Determine which player disconnected
            // For now, assume peer 2 is always player 1 (index 1)
            var disconnectedPlayerIndex = peerId == 1 ? 0 : 1;
            var winnerIndex = disconnectedPlayerIndex == 0 ? 1 : 0;

            Broadcast(new Protocol.MatchEnded(winnerIndex, "Opponent disconnected", MatchTime));
            EndMatch(winnerIndex, "Opponent disconnected");
        }
    }

    private void HandleTransportDisconnected(string reason)
    {
        if (!IsActive) return;

        GD.Print($"[MatchSession] Transport disconnected: {reason}");

        // Notify reconnection handler
        var reconnectHandler = ReconnectionHandler.Instance;
        if (reconnectHandler != null && !IsHost)
        {
            // Client lost connection - attempt reconnect
            reconnectHandler.HandleDisconnection(reason, autoReconnect: true);
        }
        else
        {
            // Host can't reconnect, or no handler
            EmitSignal(SignalName.ConnectionLost, reason);
            EndMatch(-1, "Connection lost: " + reason);
        }
    }

    #endregion
}
