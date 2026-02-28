using System;
using System.Collections.Generic;
using Godot;
using Fateforged.Multiplayer.Core;
using Fateforged.Multiplayer.Protocol;
using Fateforged.Multiplayer.Sync;
using Fateforged.Simulation;

namespace Fateforged.Multiplayer.Authority;

/// <summary>
/// Runs the authoritative simulation on the host side.
/// Subscribes to SimulationNode.OnTickCompleted to convert SimEvents into
/// protocol messages for broadcast to clients. Validates client requests
/// and submits commands to the simulation.
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
    /// Get the desync detector for debugging/monitoring.
    /// </summary>
    public DesyncDetector? DesyncDetector => _desyncDetector;

    public void Initialize(MatchSession session)
    {
        _session = session;
        _snapshotTimer = 0;
        _snapshotBuilder = new StateSnapshotBuilder(session);
        _desyncDetector = new DesyncDetector(session);

        _desyncDetector.OnDesyncDetected += OnDesyncDetected;

        // Subscribe to simulation tick events for broadcasting to clients
        if (SimulationNode.Current != null)
        {
            SimulationNode.Current.OnTickCompleted += HandleTickEvents;
            SimulationNode.Current.LocalPlayerIndex = 0; // Host is always player 0
        }

        GD.Print("[HostRunner] Initialized");
    }

    private void OnDesyncDetected(DesyncEvent desyncEvent)
    {
        GD.PrintErr($"[HostRunner] Desync detected at frame {desyncEvent.Frame}, sending full snapshot");
        BroadcastSnapshot();
    }

    public void ProcessFrame(double delta)
    {
        if (_session == null) return;

        // Sync MatchSession frame/time FROM MatchState (SimulationNode owns these)
        if (SimulationNode.Current != null)
        {
            _session.CurrentFrame = SimulationNode.Current.State.FrameNumber;
            _session.MatchTime = SimulationNode.Current.State.MatchTime;
        }

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
        if (_session == null || SimulationNode.Current == null) return;

        // Convert local position to canonical for the simulation
        var canonicalPos = CoordinateTransform.LocalToCanonical(position);
        var simPos = new SimVector3(canonicalPos.X, canonicalPos.Y, canonicalPos.Z);

        // Reserve network ID for spawned unit
        var networkId = _session.NetworkIds.NextIdWithoutRegistering();

        // Create and submit command to simulation
        var cmd = new PlayCardCommand(0, cardIndex, simPos, networkId); // Host is team 0
        SimulationNode.Current.SubmitCommand(cmd);

        GD.Print($"[HostRunner] Host card play: card {cardIndex} at {canonicalPos}, networkId {networkId}");
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
        // Unsubscribe from simulation events
        if (SimulationNode.Current != null)
        {
            SimulationNode.Current.OnTickCompleted -= HandleTickEvents;
        }

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

    #region Tick Event Handler

    /// <summary>
    /// Convert key SimEvents to protocol messages and broadcast to clients.
    /// Called after each simulation tick via OnTickCompleted delegate.
    /// </summary>
    private void HandleTickEvents(List<SimEvent> events)
    {
        if (_session == null) return;

        var state = SimulationNode.Current?.State;
        if (state == null) return;

        foreach (var evt in events)
        {
            switch (evt)
            {
                case CastingCompletedEvent e:
                {
                    // Find the spawned unit's catalog ID from MatchState
                    string unitType = "unknown";
                    foreach (var kvp in state.Units)
                    {
                        if (kvp.Value.NetworkId == e.NetworkId)
                        {
                            unitType = kvp.Value.CatalogId;
                            break;
                        }
                    }

                    var pos = new Vector3(e.SpawnPosition.X, e.SpawnPosition.Y, e.SpawnPosition.Z);
                    var spawn = new UnitSpawned(
                        e.NetworkId,
                        unitType,
                        e.Team,
                        pos,
                        state.FrameNumber,
                        null,
                        e.Team
                    );
                    _session.Broadcast(spawn);
                    break;
                }

                case UnitDiedSimEvent e:
                {
                    // Map UnitId → NetworkId
                    int networkId = -1;
                    int? killerNetworkId = null;

                    if (state.Units.TryGetValue(e.UnitId, out var deadUnit))
                        networkId = deadUnit.NetworkId;
                    if (e.KillerUnitId >= 0 && state.Units.TryGetValue(e.KillerUnitId, out var killerUnit))
                        killerNetworkId = killerUnit.NetworkId;

                    if (networkId >= 0)
                    {
                        _session.Broadcast(new UnitDied(networkId, killerNetworkId));
                    }
                    break;
                }

                case UnitDamagedEvent e:
                {
                    // Map UnitIds → NetworkIds
                    int targetNetworkId = -1;
                    int? sourceNetworkId = null;

                    if (state.Units.TryGetValue(e.TargetUnitId, out var targetUnit))
                        targetNetworkId = targetUnit.NetworkId;
                    if (e.AttackerUnitId >= 0 && state.Units.TryGetValue(e.AttackerUnitId, out var attackerUnit))
                        sourceNetworkId = attackerUnit.NetworkId;

                    if (targetNetworkId >= 0)
                    {
                        _session.Broadcast(new DamageDealt(targetNetworkId, e.Damage, e.IsCrit, sourceNetworkId));
                    }
                    break;
                }

                case SummonerHpChangedEvent e:
                {
                    // Compute damage amount from the difference
                    var summoner = state.Summoners[e.Team];
                    float amount = summoner.MaxHp - e.Hp; // Total damage taken (cumulative)
                    _session.Broadcast(new SummonerDamaged(e.Team, amount, e.Hp));
                    break;
                }

                case GameOverEvent e:
                {
                    _session.Broadcast(new MatchEnded(e.WinnerTeam, e.Reason, state.MatchTime));
                    break;
                }
            }
        }
    }

    #endregion

    #region Message Handlers

    private void HandleCardPlayRequest(int senderId, CardPlayRequest request)
    {
        if (_session == null || SimulationNode.Current == null) return;

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

        // Client sends position in canonical coordinates (client applies LocalToCanonical before sending)
        var simPos = new SimVector3(request.Position.X, request.Position.Y, request.Position.Z);

        // Reserve network ID for the spawned unit
        var networkId = _session.NetworkIds.NextIdWithoutRegistering();

        // Submit command to simulation
        var cmd = new PlayCardCommand(request.PlayerIndex, request.CardIndex, simPos, networkId);
        SimulationNode.Current.SubmitCommand(cmd);

        // Broadcast confirmation to client
        var confirmation = new CardPlayConfirmed(
            request.Sequence,
            request.PlayerIndex,
            request.CardIndex,
            request.Position,
            _session.CurrentFrame,
            networkId
        );
        _session.Broadcast(confirmation);

        GD.Print($"[HostRunner] Accepted card play: player {request.PlayerIndex}, card {request.CardIndex}, networkId {networkId}");
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

    #region Snapshot

    private void BroadcastSnapshot()
    {
        if (_session == null || _snapshotBuilder == null) return;

        var snapshot = _snapshotBuilder.Build();
        _session.Broadcast(snapshot);
    }

    #endregion
}
