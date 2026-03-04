using System;
using System.Collections.Generic;
using Fateforged.Multiplayer.Protocol;
using Fateforged.Multiplayer.Transport;
using Fateforged.Simulation;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Events;
using Godot;

namespace Fateforged.Session;

/// <summary>
/// Multiplayer host session. The authority — ticks the simulation locally,
/// validates all commands via CommandRouter, broadcasts snapshots to clients
/// after each tick.
/// </summary>
public class HostSession : NetworkSession
{
    private const float ReconnectGraceSeconds = 30f;
    private const int HostTeam = 0;
    private const int ClientTeam = 1;
    private enum DisconnectTimeoutOutcome
    {
        None,
        LocalDisconnected,
        PeerDisconnected
    }

    private readonly Simulation.Simulation _simulation;
    private readonly CommandRouter _commandRouter;
    private readonly MatchState _state;
    private float _snapshotAccumulator;
    private bool _matchEndedBroadcasted;
    private bool _transportConnected = true;
    private bool _peerConnected = true;
    private bool _disconnectTimeoutHandled;
    private DisconnectTimeoutOutcome _disconnectTimeoutOutcome = DisconnectTimeoutOutcome.None;
    private int _remotePeerId = -1;
    private const float SnapshotSendInterval = 0.1f;

    public override event Action<IReadOnlyList<SimEvent>>? SimEventsEmitted;

    public HostSession(
        Simulation.Simulation simulation,
        CommandRouter commandRouter,
        MatchState state,
        IMatchTransport transport) : base(transport)
    {
        _simulation = simulation;
        _commandRouter = commandRouter;
        _state = state;
    }

    public override MatchState GetState() => _state;

    public override void SubmitCommand(ICommand command)
    {
        var result = _commandRouter.Validate(command, _state);
        if (!result.IsValid)
        {
            Simulation.Simulation.Log?.Invoke($"[HostSession] Command rejected: {result.Reason}");
            return;
        }

        command.ExecuteFrame = _state.FrameNumber + 1;
        _state.PendingCommandBuffer.Add(command);
    }

    public override void Tick(float delta)
    {
        if (IsAwaitingReconnect && !_disconnectTimeoutHandled)
        {
            ReconnectRemainingSeconds = Math.Max(0f, ReconnectRemainingSeconds - delta);
            if (ReconnectRemainingSeconds <= 0f)
            {
                _disconnectTimeoutHandled = true;
                EndMatchForDisconnectTimeout();
            }
            return;
        }

        var events = _simulation.Tick(delta);
        if (events.Count > 0)
        {
            SimEventsEmitted?.Invoke(events);

            foreach (var evt in events)
            {
                if (evt is GameOverEvent gameOver)
                {
                    BroadcastMatchEnd(gameOver.WinnerTeam, gameOver.Reason, _state.MatchTime);
                    break;
                }

                if (evt is SummonerDamagedEvent summonerDamaged && _transport.IsConnected)
                {
                    var flash = new SummonerDamageFlash(
                        summonerDamaged.Team,
                        summonerDamaged.Damage,
                        summonerDamaged.AttackerUnitId);
                    _transport.Broadcast(_messageSerializer.Serialize(flash));
                }
            }
        }

        if (_transport.IsConnected)
        {
            _snapshotAccumulator += delta;
            if (_snapshotAccumulator >= SnapshotSendInterval)
            {
                _snapshotAccumulator = 0f;
                BroadcastSnapshot();
            }
        }
    }

    protected override void HandleDisconnect(string reason)
    {
        _transportConnected = false;
        BeginReconnect($"Transport disconnected: {reason}", DisconnectTimeoutOutcome.LocalDisconnected);
    }

    protected override void HandleConnected()
    {
        _transportConnected = true;
        TryResolveReconnect();
    }

    protected override void HandlePeerDisconnected(int peerId)
    {
        _peerConnected = false;
        if (_remotePeerId == peerId)
            _remotePeerId = -1;
        BeginReconnect($"Peer {peerId} disconnected", DisconnectTimeoutOutcome.PeerDisconnected);
    }

    protected override void HandlePeerConnected(int peerId)
    {
        _peerConnected = true;
        _remotePeerId = peerId;
        TryResolveReconnect();
    }

    protected override void HandleMessage(int senderId, object message)
    {
        switch (message)
        {
            case CardPlayRequest request:
            {
                if (!TryResolveRemoteTeam(senderId, out int senderTeam))
                    break;
                if (request.PlayerIndex != senderTeam)
                {
                    Simulation.Simulation.Log?.Invoke(
                        $"[HostSession] Remote peer {senderId} claimed team {request.PlayerIndex}, using authoritative team {senderTeam}");
                }

                var cmd = new PlayCardCommand(
                    senderTeam,
                    request.CardIndex,
                    new SimVector3(request.Position.X, request.Position.Y, request.Position.Z))
                {
                    Sequence = request.Sequence
                };
                SubmitRemoteCommand(senderId, cmd);
                break;
            }
            case ForfeitRequest request:
            {
                if (!TryResolveRemoteTeam(senderId, out int senderTeam))
                    break;
                if (request.PlayerIndex != senderTeam)
                {
                    Simulation.Simulation.Log?.Invoke(
                        $"[HostSession] Remote peer {senderId} claimed forfeit team {request.PlayerIndex}, using authoritative team {senderTeam}");
                }
                SubmitRemoteCommand(senderId, new ForfeitCommand(senderTeam));
                break;
            }
        }
    }

    public void BroadcastMatchEnd(int winnerTeam, string reason, float duration)
    {
        if (_matchEndedBroadcasted || !_transport.IsConnected)
            return;

        _matchEndedBroadcasted = true;
        _transport.Broadcast(_messageSerializer.Serialize(new MatchEnded(winnerTeam, reason, duration)));
    }

    private void SubmitRemoteCommand(int senderId, ICommand command)
    {
        var result = _commandRouter.Validate(command, _state);
        if (!result.IsValid)
        {
            Simulation.Simulation.Log?.Invoke(
                $"[HostSession] Remote command from {senderId} rejected: {result.Reason}");
            return;
        }

        command.ExecuteFrame = _state.FrameNumber + 1;
        _state.PendingCommandBuffer.Add(command);
    }

    /// <summary>
    /// Resolve authoritative team ownership for a remote sender.
    /// Host ignores any team index claimed by payload; sender identity is authoritative.
    /// </summary>
    private bool TryResolveRemoteTeam(int senderId, out int team)
    {
        team = ClientTeam;

        if (senderId == _transport.LocalPeerId)
        {
            Simulation.Simulation.Log?.Invoke(
                $"[HostSession] Ignoring remote command from local peer id {senderId}");
            return false;
        }

        // Current architecture is 1v1 host/client; all valid remote commands map to client team.
        if (_remotePeerId > 0 && senderId != _remotePeerId)
        {
            Simulation.Simulation.Log?.Invoke(
                $"[HostSession] Ignoring remote command from unexpected peer id {senderId} (expected {_remotePeerId})");
            return false;
        }

        if (_remotePeerId < 0)
            _remotePeerId = senderId;

        return true;
    }

    private void BroadcastSnapshot()
    {
        var snapshot = BuildSnapshot();
        _transport.Broadcast(_messageSerializer.Serialize(snapshot));
    }

    private void BeginReconnect(string reason, DisconnectTimeoutOutcome timeoutOutcome)
    {
        if (IsAwaitingReconnect)
            return;

        IsAwaitingReconnect = true;
        ReconnectReason = reason;
        ReconnectRemainingSeconds = ReconnectGraceSeconds;
        _disconnectTimeoutHandled = false;
        _disconnectTimeoutOutcome = timeoutOutcome;
        GD.Print($"[HostSession] Reconnect window started ({ReconnectGraceSeconds:0}s): {reason}");
    }

    private void TryResolveReconnect()
    {
        if (!_transportConnected || !_peerConnected)
            return;
        if (!IsAwaitingReconnect)
            return;

        IsAwaitingReconnect = false;
        ReconnectReason = "";
        ReconnectRemainingSeconds = 0f;
        _disconnectTimeoutHandled = false;
        _disconnectTimeoutOutcome = DisconnectTimeoutOutcome.None;
        GD.Print("[HostSession] Reconnected, resuming simulation");
    }

    private void EndMatchForDisconnectTimeout()
    {
        if (_state.Phase == GamePhase.GameOver)
            return;

        int winnerTeam = _disconnectTimeoutOutcome == DisconnectTimeoutOutcome.PeerDisconnected
            ? HostTeam
            : ClientTeam;
        int loserTeam = winnerTeam == HostTeam ? ClientTeam : HostTeam;
        string reason = _disconnectTimeoutOutcome == DisconnectTimeoutOutcome.PeerDisconnected
            ? "Peer reconnect timeout"
            : "Local reconnect timeout";

        _state.WinnerTeam = winnerTeam;
        _state.Phase = GamePhase.GameOver;

        var events = new List<SimEvent>
        {
            new GameOverEvent(winnerTeam, reason)
        };
        SimEventsEmitted?.Invoke(events);

        if (_transport.IsConnected)
            BroadcastMatchEnd(winnerTeam, "DisconnectTimeout", _state.MatchTime);

        // Ensure losing side cannot reconnect to continue this match.
        _state.Summoners[loserTeam].IsAlive = false;
        _disconnectTimeoutOutcome = DisconnectTimeoutOutcome.None;
    }

    private StateSnapshot BuildSnapshot()
    {
        var summoners = new SummonerState[_state.Summoners.Length];
        for (int i = 0; i < _state.Summoners.Length; i++)
        {
            var s = _state.Summoners[i];
            summoners[i] = new SummonerState(
                (int)s.Team,
                s.CurrentHp,
                s.MaxHp,
                s.Mana,
                s.MaxMana,
                s.IsCasting,
                s.CastingTimeRemaining,
                s.CastingTimeTotal,
                s.CastingCardIndex,
                new Vector3(s.CastingSpawnPosition.X, s.CastingSpawnPosition.Y, s.CastingSpawnPosition.Z),
                s.CastingNetworkId,
                s.ComputeCardHash(),
                s.Hand.ToArray(),
                s.Deck.ToArray(),
                s.DiscardPile.ToArray()
            );
        }

        var units = new List<UnitState>(_state.Units.Count);
        foreach (var unit in _state.Units.Values)
        {
            int stableNetworkId = unit.NetworkId >= 0 ? unit.NetworkId : unit.UnitId;
            units.Add(new UnitState(
                stableNetworkId,
                (int)unit.Team,
                new Vector3(unit.Position.X, unit.Position.Y, unit.Position.Z),
                unit.CurrentHp,
                unit.MaxHp,
                unit.TargetNetworkId,
                unit.IsAlive,
                (int)unit.ActivationState,
                (int)unit.BehaviorState,
                unit.IsFacingRight,
                unit.CatalogId ?? "",
                unit.SpawnTimer,
                unit.AttackAnimationTimer
            ));
        }

        var projectiles = new List<ProjectileState>(_state.Projectiles.Count);
        foreach (var projectile in _state.Projectiles.Values)
        {
            projectiles.Add(new ProjectileState(
                projectile.ProjectileId,
                projectile.SourceUnitId,
                projectile.TargetUnitId,
                (int)projectile.Team,
                (int)projectile.MovementType,
                new Vector3(projectile.CurrentPosition.X, projectile.CurrentPosition.Y, projectile.CurrentPosition.Z),
                new Vector3(projectile.Direction.X, projectile.Direction.Y, projectile.Direction.Z),
                new Vector3(projectile.TargetPosition.X, projectile.TargetPosition.Y, projectile.TargetPosition.Z),
                projectile.Progress,
                projectile.Speed,
                projectile.IsDead
            ));
        }

        return new StateSnapshot(
            _state.FrameNumber,
            _state.MatchTime,
            (int)_state.Phase,
            _state.PrepTimeRemaining,
            summoners,
            units.ToArray(),
            projectiles.ToArray(),
            0,
            _state.IsOvertime
        );
    }
}
