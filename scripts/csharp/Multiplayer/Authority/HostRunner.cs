using System;
using System.Collections.Generic;
using Godot;
using Fateforged.Multiplayer.Core;
using Fateforged.Multiplayer.Protocol;
using Fateforged.Multiplayer.Sync;

namespace Fateforged.Multiplayer.Authority;

/// <summary>
/// Runs the authoritative simulation on the host side.
/// Validates client requests, runs the game, and broadcasts state to clients.
/// </summary>
public class HostRunner : IMatchRunner
{
    private MatchSession? _session;
    private readonly RequestValidator _validator = new();
    private StateSnapshotBuilder? _snapshotBuilder;
    private DesyncDetector? _desyncDetector;

    /// <summary>
    /// Time since last state snapshot broadcast.
    /// </summary>
    private double _snapshotTimer;

    /// <summary>
    /// Interval between state snapshots in seconds.
    /// 10 Hz = 100ms between snapshots.
    /// </summary>
    private const double SnapshotInterval = 0.1;

    /// <summary>
    /// Sequence counter for host's own card plays.
    /// </summary>
    private int _localSequence;

    /// <summary>
    /// Get the desync detector for debugging/monitoring.
    /// </summary>
    public DesyncDetector? DesyncDetector => _desyncDetector;

    public void Initialize(MatchSession session)
    {
        _session = session;
        _snapshotTimer = 0;
        _localSequence = 0;
        _snapshotBuilder = new StateSnapshotBuilder(session);
        _desyncDetector = new DesyncDetector(session);

        _desyncDetector.OnDesyncDetected += OnDesyncDetected;

        GD.Print("[HostRunner] Initialized");
    }

    private void OnDesyncDetected(DesyncEvent desyncEvent)
    {
        // When desync is detected, immediately send a full state snapshot
        // to help the client resync
        GD.PrintErr($"[HostRunner] Desync detected at frame {desyncEvent.Frame}, sending full snapshot");
        BroadcastSnapshot();
    }

    public void ProcessFrame(double delta)
    {
        if (_session == null) return;

        _session.CurrentFrame++;
        _session.MatchTime += (float)delta;

        // Broadcast periodic state snapshots to clients
        _snapshotTimer += delta;
        if (_snapshotTimer >= SnapshotInterval)
        {
            BroadcastSnapshot();
            _snapshotTimer = 0;
        }
    }

    public void HandleMessage(int senderId, object message)
    {
        if (_session == null) return;

        switch (message)
        {
            case CardPlayRequest request:
                HandleCardPlayRequest(senderId, request);
                break;

            case ForfeitRequest request:
                HandleForfeitRequest(senderId, request);
                break;

            case StateHashReport report:
                HandleStateHashReport(senderId, report);
                break;

            case Ping ping:
                HandlePing(senderId, ping);
                break;
        }
    }

    public void RequestCardPlay(int cardIndex, Vector3 position)
    {
        if (_session == null) return;

        // Host plays cards directly (self-request)
        var request = new CardPlayRequest(
            _localSequence++,
            _session.LocalPlayerIndex,
            cardIndex,
            position,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        );

        // Validate our own request
        var validation = _validator.ValidateCardPlay(_session, request);
        if (!validation.IsValid)
        {
            GD.PrintErr($"[HostRunner] Self card play rejected: {validation.Reason}");
            return;
        }

        // Execute and broadcast
        ExecuteCardPlay(request);
    }

    public void RequestForfeit()
    {
        if (_session == null) return;

        var winnerIndex = _session.LocalPlayerIndex == 0 ? 1 : 0;
        var endMsg = new MatchEnded(winnerIndex, "Forfeit", _session.MatchTime);

        _session.Broadcast(endMsg);
        _session.EndMatch(winnerIndex, "Forfeit");
    }

    public void Cleanup()
    {
        if (_desyncDetector != null)
        {
            _desyncDetector.OnDesyncDetected -= OnDesyncDetected;
            _desyncDetector.Reset();
        }
        _session = null;
        _snapshotBuilder = null;
        _desyncDetector = null;
        GD.Print("[HostRunner] Cleaned up");
    }

    #region Message Handlers

    private void HandleCardPlayRequest(int senderId, CardPlayRequest request)
    {
        if (_session == null) return;

        GD.Print($"[HostRunner] Received CardPlayRequest from peer {senderId}: card {request.CardIndex} at {request.Position}");

        // Validate the request
        var validation = _validator.ValidateCardPlay(_session, request);
        if (!validation.IsValid)
        {
            GD.Print($"[HostRunner] Rejected: {validation.Reason}");
            _session.SendTo(senderId, new CardPlayRejected(
                request.Sequence,
                request.PlayerIndex,
                validation.Reason
            ));
            return;
        }

        // Execute the card play
        ExecuteCardPlay(request);
    }

    private void HandleForfeitRequest(int senderId, ForfeitRequest request)
    {
        if (_session == null) return;

        GD.Print($"[HostRunner] Player {request.PlayerIndex} forfeited");

        var winnerIndex = request.PlayerIndex == 0 ? 1 : 0;
        var endMsg = new MatchEnded(winnerIndex, "Forfeit", _session.MatchTime);

        _session.Broadcast(endMsg);
        _session.EndMatch(winnerIndex, "Forfeit");
    }

    private void HandleStateHashReport(int senderId, StateHashReport report)
    {
        if (_session == null || _desyncDetector == null) return;

        // Use DesyncDetector to check client hash
        _desyncDetector.CheckClientHash(report.Hash, report.Frame);
    }

    private void HandlePing(int senderId, Ping ping)
    {
        if (_session == null) return;

        _session.SendTo(senderId, new Pong(
            ping.Timestamp,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        ));
    }

    #endregion

    #region Execution

    private void ExecuteCardPlay(CardPlayRequest request)
    {
        if (_session == null) return;

        // TODO(Phase-4): Integrate with actual card execution system
        // Currently broadcasts confirmation and spawn messages without actually spawning units.
        // Phase 4 will:
        // 1. Look up the card from the player's hand
        // 2. Validate mana cost and deduct mana
        // 3. Call UnitSpawner to create the actual unit
        // 4. Register the spawned unit (not a placeholder) with NetworkIdRegistry
        // For now, we reserve a network ID and broadcast the intent.

        // Reserve network ID for the unit that will be spawned
        // Note: The actual unit registration happens in UnitSpawner when the unit is created
        var networkId = _session.NetworkIds.NextIdWithoutRegistering();

        // Broadcast confirmation
        var confirmation = new CardPlayConfirmed(
            request.Sequence,
            request.PlayerIndex,
            request.CardIndex,
            request.Position,
            _session.CurrentFrame,
            networkId
        );
        _session.Broadcast(confirmation);

        // Broadcast unit spawn intent
        // TODO(Phase-4): Get actual unit type from card catalog lookup
        var spawn = new UnitSpawned(
            networkId,
            $"card_{request.CardIndex}", // Placeholder unit type until card lookup is implemented
            request.PlayerIndex, // Team = player index
            request.Position,
            _session.CurrentFrame,
            request.Sequence,
            request.PlayerIndex
        );
        _session.Broadcast(spawn);

        GD.Print($"[HostRunner] Executed card play: player {request.PlayerIndex}, card {request.CardIndex}, networkId {networkId}");
    }

    #endregion

    #region Snapshot

    private void BroadcastSnapshot()
    {
        if (_session == null || _snapshotBuilder == null) return;

        var snapshot = _snapshotBuilder.Build();
        _session.Broadcast(snapshot);
    }

    #endregion
}
