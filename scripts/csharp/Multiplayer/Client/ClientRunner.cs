using System;
using System.Collections.Generic;
using Godot;
using Fateforged.Multiplayer.Core;
using Fateforged.Multiplayer.Protocol;
using Fateforged.Multiplayer.Sync;

namespace Fateforged.Multiplayer.Client;

/// <summary>
/// Runs the predicted simulation on the client side.
/// Handles client-side prediction and server reconciliation.
/// </summary>
public class ClientRunner : IMatchRunner
{
    private MatchSession? _session;
    private readonly PredictionBuffer _predictions = new();
    private readonly StateInterpolator _interpolator = new();
    private StateSnapshotBuilder? _snapshotBuilder;
    private DesyncDetector? _desyncDetector;

    /// <summary>
    /// Sequence counter for outgoing requests.
    /// </summary>
    private int _nextSequence;

    /// <summary>
    /// Last received server frame for lag detection.
    /// </summary>
    private long _lastServerFrame;

    /// <summary>
    /// Latency to server in milliseconds.
    /// </summary>
    public int LatencyMs { get; private set; }

    /// <summary>
    /// Timer for periodic ping.
    /// </summary>
    private double _pingTimer;
    private const double PingInterval = 1.0; // Ping every second

    /// <summary>
    /// Frame counter for hash reporting.
    /// </summary>
    private long _frameCounter;

    /// <summary>
    /// Get the desync detector for debugging/monitoring.
    /// </summary>
    public DesyncDetector? DesyncDetector => _desyncDetector;

    public void Initialize(MatchSession session)
    {
        _session = session;
        _nextSequence = 0;
        _lastServerFrame = 0;
        LatencyMs = 0;
        _pingTimer = 0;
        _frameCounter = 0;
        _snapshotBuilder = new StateSnapshotBuilder(session);
        _desyncDetector = new DesyncDetector(session);

        GD.Print("[ClientRunner] Initialized");
    }

    public void ProcessFrame(double delta)
    {
        if (_session == null) return;

        _frameCounter++;

        // Interpolate remote entities toward their last known positions
        _interpolator.Update(delta);

        // Periodic ping for latency measurement
        _pingTimer += delta;
        if (_pingTimer >= PingInterval)
        {
            SendPing();
            _pingTimer = 0;
        }

        // Periodic hash report to host for desync detection
        if (_frameCounter % DesyncDetector.HashReportIntervalFrames == 0)
        {
            SendHashReport();
        }
    }

    public void HandleMessage(int senderId, object message)
    {
        if (_session == null) return;

        switch (message)
        {
            case CardPlayConfirmed confirmed:
                HandleCardPlayConfirmed(confirmed);
                break;

            case CardPlayRejected rejected:
                HandleCardPlayRejected(rejected);
                break;

            case StateSnapshot snapshot:
                HandleStateSnapshot(snapshot);
                break;

            case UnitSpawned spawned:
                HandleUnitSpawned(spawned);
                break;

            case UnitDied died:
                HandleUnitDied(died);
                break;

            case Pong pong:
                HandlePong(pong);
                break;

            case MatchEnded ended:
                // Handled by MatchSession
                break;
        }
    }

    public void RequestCardPlay(int cardIndex, Vector3 position)
    {
        if (_session == null) return;

        var sequence = _nextSequence++;

        // Create prediction for optimistic update
        var prediction = new CardPlayPrediction(
            sequence,
            cardIndex,
            position,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        );
        _predictions.Add(prediction);

        // Apply prediction locally (optimistic)
        ApplyPrediction(prediction);

        // Send request to host
        var request = new CardPlayRequest(
            sequence,
            _session.LocalPlayerIndex,
            cardIndex,
            position,
            prediction.Timestamp
        );
        _session.Send(request);

        GD.Print($"[ClientRunner] Sent CardPlayRequest: seq {sequence}, card {cardIndex}");
    }

    public void RequestForfeit()
    {
        if (_session == null) return;

        _session.Send(new ForfeitRequest(_session.LocalPlayerIndex));
        GD.Print("[ClientRunner] Sent ForfeitRequest");
    }

    public void Cleanup()
    {
        _predictions.Clear();
        _interpolator.Clear();
        _desyncDetector?.Reset();
        _session = null;
        _snapshotBuilder = null;
        _desyncDetector = null;
        GD.Print("[ClientRunner] Cleaned up");
    }

    #region Message Handlers

    private void HandleCardPlayConfirmed(CardPlayConfirmed confirmed)
    {
        if (_session == null) return;

        // Check if this confirms our prediction
        if (confirmed.PlayerIndex == _session.LocalPlayerIndex)
        {
            var prediction = _predictions.Get(confirmed.Sequence);
            if (prediction != null)
            {
                // Prediction was correct - remove from buffer
                _predictions.Remove(confirmed.Sequence);
                GD.Print($"[ClientRunner] Prediction {confirmed.Sequence} confirmed");
            }
        }

        // Note: UnitSpawned message will follow with the actual spawn
    }

    private void HandleCardPlayRejected(CardPlayRejected rejected)
    {
        if (_session == null) return;

        if (rejected.PlayerIndex == _session.LocalPlayerIndex)
        {
            var prediction = _predictions.Get(rejected.Sequence);
            if (prediction != null)
            {
                // Rollback the prediction
                RollbackPrediction(prediction);
                _predictions.Remove(rejected.Sequence);
                GD.PrintErr($"[ClientRunner] Prediction {rejected.Sequence} rejected: {rejected.Reason}");
            }
        }
    }

    private void HandleStateSnapshot(StateSnapshot snapshot)
    {
        if (_session == null) return;

        _lastServerFrame = snapshot.Frame;
        _session.MatchTime = snapshot.MatchTime;

        // Update interpolation targets for all units
        foreach (var unitState in snapshot.Units)
        {
            _interpolator.SetTarget(unitState.NetworkId, unitState.Position);
        }

        // Use DesyncDetector to check and apply corrections
        _desyncDetector?.ApplySnapshot(snapshot);
    }

    private void HandleUnitSpawned(UnitSpawned spawned)
    {
        if (_session == null) return;

        // Log the spawn event - actual unit instantiation is handled via MatchSession.UnitSpawned signal
        // which is dispatched in MatchSession.DispatchMessageEvent()
        GD.Print($"[ClientRunner] Unit spawned: id {spawned.NetworkId}, type {spawned.UnitType}, team {spawned.Team}");

        // TODO(Phase-4): Client-side unit spawning from network messages
        // The UnitSpawned signal is already emitted by MatchSession. Game systems need to:
        // 1. Listen to MatchSession.UnitSpawned signal
        // 2. Instantiate the unit using UnitSpawner with the provided NetworkId
        // 3. Register the unit with NetworkIdRegistry using RegisterWithId()
    }

    private void HandleUnitDied(UnitDied died)
    {
        if (_session == null) return;

        GD.Print($"[ClientRunner] Unit died: id {died.NetworkId}, killer {died.KillerNetworkId}");

        // Unregister from network IDs and interpolation
        _session.NetworkIds.UnregisterById(died.NetworkId);
        _interpolator.Remove(died.NetworkId);

        // Death handling is done via MatchSession.UnitDied signal emitted in DispatchMessageEvent()
        // Game systems listen to this signal to trigger death animations and cleanup
    }

    private void HandlePong(Pong pong)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        LatencyMs = (int)(now - pong.OriginalTimestamp) / 2; // Round-trip / 2
        GD.Print($"[ClientRunner] Latency: {LatencyMs}ms");
    }

    #endregion

    #region Prediction

    private void ApplyPrediction(CardPlayPrediction prediction)
    {
        // TODO(Phase-4): Apply optimistic update to local game state for instant feedback
        // When integrated with gameplay:
        // - Deduct mana from local Summoner
        // - Show card leaving hand (visual feedback)
        // - Optionally show ghost unit at spawn position
        // This makes the game feel responsive while waiting for host confirmation.
        GD.Print($"[ClientRunner] Applied prediction {prediction.Sequence}");
    }

    private void RollbackPrediction(CardPlayPrediction prediction)
    {
        // TODO(Phase-4): Rollback the optimistic update when host rejects a card play
        // When integrated with gameplay:
        // - Restore mana to local Summoner
        // - Return card to hand (visual feedback)
        // - Remove any ghost unit that was shown
        // This handles cases where the host rejects a card play (e.g., not enough mana).
        GD.Print($"[ClientRunner] Rolled back prediction {prediction.Sequence}");
    }

    #endregion

    #region Helpers

    private void SendPing()
    {
        _session?.Send(new Ping(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
    }

    private void SendHashReport()
    {
        if (_session == null || _snapshotBuilder == null) return;

        var hash = _snapshotBuilder.ComputeHash();
        var report = new StateHashReport(
            _session.LocalPlayerIndex,
            _session.CurrentFrame,
            hash
        );
        _session.Send(report);
    }

    #endregion
}

/// <summary>
/// Represents a predicted card play waiting for server confirmation.
/// </summary>
public record CardPlayPrediction(
    int Sequence,
    int CardIndex,
    Vector3 Position,
    long Timestamp
);
