using System;
using System.Collections.Generic;
using Godot;
using Fateforged.Multiplayer.Core;
using Fateforged.Multiplayer.Protocol;
using Fateforged.Multiplayer.Sync;
using Fateforged.Simulation;

namespace Fateforged.Multiplayer.Client;

/// <summary>
/// Runs the client side of a multiplayer match.
/// Receives snapshots and events from the host, routes them to SimulationNode
/// for state application and signal emission. Never calls Tick().
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
    private const double PingInterval = 1.0;

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

        // Wire up to SimulationNode (must exist — created before MatchSession in phase order)
        if (SimulationNode.Current != null)
        {
            SimulationNode.Current.IsHost = false;
            SimulationNode.Current.LocalPlayerIndex = session.LocalPlayerIndex;
            GD.Print($"[ClientRunner] Connected to SimulationNode (LocalPlayerIndex={session.LocalPlayerIndex})");
        }
        else
        {
            GD.PrintErr("[ClientRunner] SimulationNode.Current is null during Initialize — this is a bug");
        }

        GD.Print("[ClientRunner] Initialized");
    }

    public void ProcessFrame(double delta)
    {
        if (_session == null) return;

        _frameCounter++;

        // Interpolate remote entities toward their last known positions
        _interpolator.Update(delta);

        // Write interpolated positions into UnitData so SyncFromSimulation
        // reads smooth frame-rate positions instead of stale 10Hz snapshot data
        SyncInterpolatedPositions();

        // Periodic ping for latency measurement
        _pingTimer += delta;
        if (_pingTimer >= PingInterval)
        {
            SendPing();
            _pingTimer = 0;
        }

        // Periodic hash report to host for desync detection
        // Skip until we've received at least one snapshot (otherwise hash is meaningless)
        if (_frameCounter % DesyncDetector.HashReportIntervalFrames == 0 && _lastServerFrame > 0)
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

            case DamageDealt damage:
                HandleDamageDealt(damage);
                break;

            case SummonerDamaged summonerDamage:
                HandleSummonerDamaged(summonerDamage);
                break;

            case SummonerDamageFlash flash:
                // TODO: Route to SummonerVisual.FlashDamage() via EntityManager
                break;

            case SummonerDestroyed destroyed:
                // TODO: Route to SummonerVisual.BeginDeath() via EntityManager
                break;

            case Pong pong:
                HandlePong(pong);
                break;

            case MatchEnded ended:
                HandleMatchEnded(ended);
                break;
        }
    }

    public void RequestCardPlay(int cardIndex, Vector3 position)
    {
        if (_session == null) return;

        var sequence = _nextSequence++;

        // Convert local position to canonical before sending to host
        var canonicalPos = CoordinateTransform.LocalToCanonical(position);

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

        // Send request to host with canonical position
        var request = new CardPlayRequest(
            sequence,
            _session.LocalPlayerIndex,
            cardIndex,
            canonicalPos,
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
                _predictions.Remove(confirmed.Sequence);
                GD.Print($"[ClientRunner] Prediction {confirmed.Sequence} confirmed");
            }
        }
    }

    private void HandleCardPlayRejected(CardPlayRejected rejected)
    {
        if (_session == null) return;

        if (rejected.PlayerIndex == _session.LocalPlayerIndex)
        {
            var prediction = _predictions.Get(rejected.Sequence);
            if (prediction != null)
            {
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

        // Route to SimulationNode for authoritative state application + signal emission
        SimulationNode.Current?.ApplySnapshot(snapshot);

        // Update interpolation targets for all units
        foreach (var unitState in snapshot.Units)
        {
            _interpolator.SetTarget(unitState.NetworkId, unitState.Position);
        }
    }

    private void HandleUnitSpawned(UnitSpawned spawned)
    {
        if (_session == null) return;

        GD.Print($"[ClientRunner] Unit spawned: id {spawned.NetworkId}, type {spawned.UnitType}, team {spawned.Team}");

        // Pre-register unit in MatchState BEFORE visual spawn so Unit3D._Ready()
        // can ClaimNextSimUnitId() and find the UnitData with the correct Team.
        if (SimulationNode.Current != null)
        {
            var canonicalPos = new SimVector3(spawned.Position.X, spawned.Position.Y, spawned.Position.Z);
            SimulationNode.Current.PreRegisterRemoteUnit(spawned.NetworkId, spawned.Team, canonicalPos);

            // Convert to local space and emit signal for visual spawning
            var localTeam = SimulationNode.Current.ToLocalTeam(new NetworkTeam(spawned.Team));
            var localPos = CoordinateTransform.CanonicalToLocal(spawned.Position);
            SimulationNode.Current.EmitSignal(
                SimulationNode.SignalName.RemoteUnitSpawned,
                spawned.UnitType, localTeam.Value, localPos, spawned.NetworkId, spawned.SpawnDuration
            );
        }
    }

    private void HandleUnitDied(UnitDied died)
    {
        if (_session == null) return;

        GD.Print($"[ClientRunner] Unit died: id {died.NetworkId}, killer {died.KillerNetworkId}");

        // Unregister from network IDs and interpolation
        _session.NetworkIds.UnregisterById(died.NetworkId);
        _interpolator.Remove(died.NetworkId);

        // Emit UnitDiedSim signal on SimulationNode for death animations
        if (SimulationNode.Current != null)
        {
            int? unitId = FindUnitIdByNetworkId(died.NetworkId);
            if (unitId.HasValue)
            {
                int killerUnitId = died.KillerNetworkId.HasValue
                    ? FindUnitIdByNetworkId(died.KillerNetworkId.Value) ?? -1
                    : -1;
                SimulationNode.Current.EmitSignal(SimulationNode.SignalName.UnitDiedSim, unitId.Value, killerUnitId);
            }
        }
    }

    private void HandleDamageDealt(DamageDealt damage)
    {
        if (_session == null || SimulationNode.Current == null) return;

        int? targetUnitId = FindUnitIdByNetworkId(damage.TargetNetworkId);
        int attackerUnitId = damage.SourceNetworkId.HasValue
            ? FindUnitIdByNetworkId(damage.SourceNetworkId.Value) ?? -1
            : -1;

        if (targetUnitId.HasValue)
        {
            SimulationNode.Current.EmitSignal(SimulationNode.SignalName.UnitDamaged, targetUnitId.Value, attackerUnitId, damage.Amount, damage.IsCrit);
        }
    }

    private void HandleSummonerDamaged(SummonerDamaged summonerDamage)
    {
        if (_session == null || SimulationNode.Current == null) return;

        // SummonerDamaged.Team is in network perspective — convert to local for signal emission
        var networkTeam = new NetworkTeam(summonerDamage.Team);
        var localTeam = SimulationNode.Current.ToLocalTeam(networkTeam);
        var summoner = SimulationNode.Current.State.Summoners[networkTeam.Value];

        SimulationNode.Current.EmitSignal(SimulationNode.SignalName.SummonerHpChanged, localTeam.Value, summonerDamage.NewHp, summoner.MaxHp);
    }

    private void HandleMatchEnded(MatchEnded ended)
    {
        if (_session == null || SimulationNode.Current == null) return;

        // Convert winner index to local team:
        // WinnerIndex 0 = host won, 1 = client won
        // If WinnerIndex == LocalPlayerIndex → local player won → Team.PLAYER (0)
        // Else → opponent won → Team.ENEMY (1)
        int localWinnerTeam = (ended.WinnerIndex == _session.LocalPlayerIndex) ? 0 : 1;

        GD.Print($"[ClientRunner] Match ended: winner index {ended.WinnerIndex}, local team {localWinnerTeam}");

        // Emit GameOver on SimulationNode so game_controller_3d.gd receives it
        SimulationNode.Current.EmitSignal(SimulationNode.SignalName.GameOver, localWinnerTeam, ended.Reason);
    }

    private void HandlePong(Pong pong)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        LatencyMs = (int)(now - pong.OriginalTimestamp) / 2;
        GD.Print($"[ClientRunner] Latency: {LatencyMs}ms");
    }

    #endregion

    #region Prediction

    private void ApplyPrediction(CardPlayPrediction prediction)
    {
        // Optimistic update: deduct mana, show card leaving hand
        // Full implementation deferred until client prediction is refined
        GD.Print($"[ClientRunner] Applied prediction {prediction.Sequence}");
    }

    private void RollbackPrediction(CardPlayPrediction prediction)
    {
        // Rollback: restore mana, return card to hand
        // Full implementation deferred until client prediction is refined
        GD.Print($"[ClientRunner] Rolled back prediction {prediction.Sequence}");
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Map a NetworkId to a sim UnitId via MatchState lookup.
    /// </summary>
    private int? FindUnitIdByNetworkId(int networkId)
    {
        var state = SimulationNode.Current?.State;
        if (state == null) return null;

        foreach (var kvp in state.Units)
            if (kvp.Value.NetworkId == networkId)
                return kvp.Value.UnitId;
        return null;
    }

    private void SendPing()
    {
        _session?.Send(new Ping(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
    }

    /// <summary>
    /// Write interpolated positions from StateInterpolator into UnitData.
    /// This bridges the gap: interpolator computes smooth frame-rate positions,
    /// UnitData stores them, SyncFromSimulation reads them for display.
    /// </summary>
    private void SyncInterpolatedPositions()
    {
        var sim = SimulationNode.Current;
        if (sim == null) return;

        foreach (var kvp in sim.State.Units)
        {
            var unitData = kvp.Value;
            if (!unitData.IsAlive || unitData.NetworkId < 0) continue;

            var pos = _interpolator.GetPosition(unitData.NetworkId);
            if (pos.HasValue)
            {
                unitData.Position = new SimVector3(pos.Value.X, pos.Value.Y, pos.Value.Z);
            }
        }
    }

    private void SendHashReport()
    {
        if (_session == null || _snapshotBuilder == null) return;

        // Compute hash from MatchState (via rewritten StateSnapshotBuilder)
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
