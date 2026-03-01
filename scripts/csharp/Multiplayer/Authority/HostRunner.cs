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
    private HostEventBroadcaster? _broadcaster;

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
        _broadcaster = null; // Created lazily in HandleTickEvents when SimulationNode.Current is available

        _desyncDetector.OnDesyncDetected += OnDesyncDetected;

        // Wire up to SimulationNode (must exist — created before MatchSession in phase order)
        if (SimulationNode.Current != null)
        {
            SimulationNode.Current.OnTickCompleted += HandleTickEvents;
            SimulationNode.Current.LocalPlayerIndex = 0; // Host is always player 0
            GD.Print("[HostRunner] Connected to SimulationNode");
        }
        else
        {
            GD.PrintErr("[HostRunner] SimulationNode.Current is null during Initialize — this is a bug");
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

        // Create and submit command to simulation
        // NetworkId=-1: simulation assigns real NetworkIds via MatchState.NextNetworkId()
        var cmd = new PlayCardCommand(0, cardIndex, simPos, -1); // Host is team 0
        SimulationNode.Current.SubmitCommand(cmd);

        GD.Print($"[HostRunner] Host card play: card {cardIndex} at {canonicalPos}");
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
        _broadcaster = null;
        GD.Print("[HostRunner] Cleaned up");
    }

    #region Tick Event Handler

    /// <summary>
    /// Convert SimEvents to protocol messages and broadcast to clients.
    /// Called after each simulation tick via OnTickCompleted delegate.
    ///
    /// Uses the visitor pattern for compile-time exhaustiveness:
    /// adding a new SimEvent without a Visit() in HostEventBroadcaster causes a compile error.
    /// </summary>
    private void HandleTickEvents(List<SimEvent> events)
    {
        if (_session == null) return;

        var state = SimulationNode.Current?.State;
        if (state == null) return;

        _broadcaster ??= new HostEventBroadcaster((IMessageBroadcaster)_session, state);

        foreach (var evt in events)
        {
            evt.Accept(_broadcaster);
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

        // Submit command to simulation
        // NetworkId=-1: simulation assigns real NetworkIds via MatchState.NextNetworkId()
        var cmd = new PlayCardCommand(request.PlayerIndex, request.CardIndex, simPos, -1);
        SimulationNode.Current.SubmitCommand(cmd);

        // Broadcast confirmation to client (NetworkId will be assigned by sim at spawn time)
        var confirmation = new CardPlayConfirmed(
            request.Sequence,
            request.PlayerIndex,
            request.CardIndex,
            request.Position,
            _session.CurrentFrame,
            -1
        );
        _session.Broadcast(confirmation);

        GD.Print($"[HostRunner] Accepted card play: player {request.PlayerIndex}, card {request.CardIndex}");
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
