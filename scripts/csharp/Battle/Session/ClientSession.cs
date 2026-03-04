using System;
using System.Collections.Generic;
using Fateforged.Multiplayer.Protocol;
using Fateforged.Multiplayer.Transport;
using Fateforged.Projectiles;
using Fateforged.Simulation;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Events;
using Fateforged.Units;
using Godot;

namespace Fateforged.Session;

/// <summary>
/// Multiplayer client session. Does NOT tick the simulation — sends local
/// commands to the host over the network and applies snapshots received
/// from the host to a local copy of MatchState.
/// </summary>
public class ClientSession : NetworkSession
{
    private const float ReconnectGraceSeconds = 30f;
    private enum DisconnectTimeoutOutcome
    {
        None,
        LocalDisconnected,
        PeerDisconnected
    }

    private readonly record struct UnitVisualSnapshot(
        float Hp,
        bool IsAlive,
        BehaviorState BehaviorState,
        int? TargetNetworkId
    );

    private readonly MatchState _localState;
    private readonly List<SimEvent> _pendingEvents = new();
    private readonly HashSet<int> _snapshotUnitIds = new();
    private readonly List<int> _staleUnitIds = new();
    private readonly HashSet<int> _snapshotProjectileIds = new();
    private readonly List<int> _staleProjectileIds = new();
    private readonly int _localPlayerIndex;
    private int _nextCommandSequence = 1;
    private bool _firstSnapshotReceived;
    private bool _reconnectTimedOut;
    private DisconnectTimeoutOutcome _disconnectTimeoutOutcome = DisconnectTimeoutOutcome.None;

    public override event Action<IReadOnlyList<SimEvent>>? SimEventsEmitted;
    public event Action? FirstSnapshotApplied;

    public ClientSession(MatchState localState, IMatchTransport transport, int localPlayerIndex) : base(transport)
    {
        _localState = localState;
        _localPlayerIndex = localPlayerIndex;
    }

    public override MatchState GetState() => _localState;

    public override void SubmitCommand(ICommand command)
    {
        if (!_transport.IsConnected)
        {
            GD.PrintErr("[ClientSession] Cannot submit command while disconnected");
            return;
        }

        switch (command)
        {
            case PlayCardCommand play:
            {
                int sequence = play.Sequence > 0 ? play.Sequence : _nextCommandSequence++;
                var request = new CardPlayRequest(
                    sequence,
                    play.Team,
                    play.CardIndex,
                    new Vector3(play.SpawnPosition.X, play.SpawnPosition.Y, play.SpawnPosition.Z),
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                );
                _transport.Send(_messageSerializer.Serialize(request));
                break;
            }
            case ForfeitCommand forfeit:
                _transport.Send(_messageSerializer.Serialize(new ForfeitRequest(forfeit.Team)));
                break;
            default:
                GD.PushWarning($"[ClientSession] Unsupported command type for client submission: {command.GetType().Name}");
                break;
        }
    }

    public override void Tick(float delta)
    {
        if (IsAwaitingReconnect && !_reconnectTimedOut)
        {
            ReconnectRemainingSeconds = Math.Max(0f, ReconnectRemainingSeconds - delta);
            if (ReconnectRemainingSeconds <= 0f)
            {
                _reconnectTimedOut = true;
                ForceDisconnectLoss("Reconnect timeout");
            }
        }

        if (_pendingEvents.Count > 0)
        {
            SimEventsEmitted?.Invoke(_pendingEvents);
            _pendingEvents.Clear();
        }
    }

    protected override void HandleMessage(int senderId, object message)
    {
        switch (message)
        {
            case StateSnapshot snapshot:
                ApplySnapshot(snapshot);
                break;
            case MatchEnded ended:
                _localState.WinnerTeam = ended.WinnerIndex;
                _localState.Phase = GamePhase.GameOver;
                _pendingEvents.Add(new GameOverEvent(ended.WinnerIndex, ended.Reason));
                break;
            case SummonerDamageFlash flash:
                _pendingEvents.Add(new SummonerDamagedEvent(flash.Team, flash.Damage, flash.AttackerUnitId));
                break;
        }
    }

    protected override void HandleDisconnect(string reason)
    {
        if (_localState.Phase == GamePhase.GameOver)
            return;

        BeginReconnect(reason, DisconnectTimeoutOutcome.LocalDisconnected);
    }

    protected override void HandleConnected()
    {
        ResolveReconnect();
    }

    protected override void HandlePeerDisconnected(int peerId)
    {
        if (_localState.Phase == GamePhase.GameOver)
            return;

        BeginReconnect($"Peer {peerId} disconnected", DisconnectTimeoutOutcome.PeerDisconnected);
    }

    protected override void HandlePeerConnected(int peerId)
    {
        ResolveReconnect();
    }

    private void BeginReconnect(string reason, DisconnectTimeoutOutcome timeoutOutcome)
    {
        if (IsAwaitingReconnect)
            return;

        IsAwaitingReconnect = true;
        ReconnectReason = reason;
        ReconnectRemainingSeconds = ReconnectGraceSeconds;
        _reconnectTimedOut = false;
        _disconnectTimeoutOutcome = timeoutOutcome;
        GD.Print($"[ClientSession] Reconnect window started ({ReconnectGraceSeconds:0}s): {reason}");
    }

    private void ResolveReconnect()
    {
        if (!IsAwaitingReconnect)
            return;

        IsAwaitingReconnect = false;
        ReconnectReason = "";
        ReconnectRemainingSeconds = 0f;
        _reconnectTimedOut = false;
        _disconnectTimeoutOutcome = DisconnectTimeoutOutcome.None;
        GD.Print("[ClientSession] Reconnected");
    }

    private void ForceDisconnectLoss(string reason)
    {
        if (_localState.Phase == GamePhase.GameOver)
            return;

        int winner = _disconnectTimeoutOutcome == DisconnectTimeoutOutcome.PeerDisconnected
            ? _localPlayerIndex
            : MatchState.GetEnemyTeam(_localPlayerIndex);
        _localState.WinnerTeam = winner;
        _localState.Phase = GamePhase.GameOver;
        _pendingEvents.Add(new GameOverEvent(winner, $"Disconnected: {reason}"));
        _disconnectTimeoutOutcome = DisconnectTimeoutOutcome.None;
    }

    /// <summary>
    /// Apply an authoritative snapshot received from the host.
    /// </summary>
    public void ApplySnapshot(StateSnapshot snapshot)
    {
        var previousUnits = CaptureUnitVisualState(_localState.Units);

        _localState.FrameNumber = snapshot.Frame;
        _localState.MatchTime = snapshot.MatchTime;
        _localState.Phase = (GamePhase)snapshot.Phase;
        _localState.PrepTimeRemaining = snapshot.PrepTimeRemaining;
        _localState.IsOvertime = snapshot.IsOvertime;

        // Copy summoner data
        for (int i = 0; i < snapshot.Summoners.Length && i < _localState.Summoners.Length; i++)
        {
            var src = snapshot.Summoners[i];
            var dst = _localState.Summoners[i];
            dst.Team = (Team)src.Team;
            dst.CurrentHp = src.Hp;
            dst.MaxHp = src.MaxHp;
            dst.IsAlive = src.Hp > 0f;
            dst.Mana = src.Mana;
            dst.MaxMana = src.MaxMana;
            dst.IsCasting = src.IsCasting;
            dst.CastingTimeRemaining = src.CastingTimeRemaining;
            dst.CastingTimeTotal = src.CastingTimeTotal;
            dst.CastingCardIndex = src.CastingCardIndex;
            dst.CastingSpawnPosition = new SimVector3(src.CastingSpawnPosition.X, src.CastingSpawnPosition.Y, src.CastingSpawnPosition.Z);
            dst.CastingNetworkId = src.CastingNetworkId;
            dst.Hand.Clear();
            dst.Hand.AddRange(src.Hand ?? Array.Empty<string>());
            dst.Deck.Clear();
            dst.Deck.AddRange(src.Deck ?? Array.Empty<string>());
            dst.DiscardPile.Clear();
            dst.DiscardPile.AddRange(src.DiscardPile ?? Array.Empty<string>());
        }

        // Copy units (in-place to avoid per-snapshot object churn/GC spikes)
        _snapshotUnitIds.Clear();
        foreach (var src in snapshot.Units)
        {
            int unitId = src.NetworkId;
            _snapshotUnitIds.Add(unitId);

            if (!_localState.Units.TryGetValue(unitId, out var unit))
            {
                unit = new UnitData
                {
                    UnitId = unitId,
                    NetworkId = src.NetworkId
                };
                _localState.Units[unitId] = unit;
            }

            ApplySnapshotUnitState(unit, src);
        }

        _staleUnitIds.Clear();
        foreach (var unitId in _localState.Units.Keys)
        {
            if (!_snapshotUnitIds.Contains(unitId))
                _staleUnitIds.Add(unitId);
        }

        foreach (var staleId in _staleUnitIds)
        {
            _localState.Units.Remove(staleId);
        }

        _snapshotProjectileIds.Clear();
        foreach (var src in snapshot.Projectiles)
        {
            int projectileId = src.ProjectileId;
            _snapshotProjectileIds.Add(projectileId);

            if (!_localState.Projectiles.TryGetValue(projectileId, out var projectile))
            {
                projectile = new SimProjectileData
                {
                    ProjectileId = projectileId
                };
                _localState.Projectiles[projectileId] = projectile;
            }

            ApplySnapshotProjectileState(projectile, src);
        }

        _staleProjectileIds.Clear();
        foreach (var projectileId in _localState.Projectiles.Keys)
        {
            if (!_snapshotProjectileIds.Contains(projectileId))
                _staleProjectileIds.Add(projectileId);
        }

        foreach (var staleProjectileId in _staleProjectileIds)
            _localState.Projectiles.Remove(staleProjectileId);

        if (_firstSnapshotReceived)
            QueueDerivedUnitEvents(previousUnits, snapshot.Units, _snapshotUnitIds);

        if (!_firstSnapshotReceived)
        {
            _firstSnapshotReceived = true;
            FirstSnapshotApplied?.Invoke();
        }
    }

    private static void ApplySnapshotUnitState(UnitData dst, UnitState src)
    {
        dst.UnitId = src.NetworkId;
        dst.NetworkId = src.NetworkId;
        dst.Team = (Team)src.Team;
        dst.Position = new SimVector3(src.Position.X, src.Position.Y, src.Position.Z);
        dst.CurrentHp = src.Hp;
        dst.MaxHp = src.MaxHp;
        dst.IsAlive = src.IsAlive;
        dst.ActivationState = (ActivationState)src.ActivationState;
        dst.BehaviorState = (BehaviorState)src.BehaviorState;
        dst.IsFacingRight = src.IsFacingRight;
        dst.TargetNetworkId = src.TargetNetworkId;
        dst.CatalogId = src.CatalogId ?? "";
        dst.SpawnTimer = src.SpawnTimer;
        dst.AttackAnimationTimer = src.AttackAnimationTimer;
    }

    private static void ApplySnapshotProjectileState(SimProjectileData dst, ProjectileState src)
    {
        dst.ProjectileId = src.ProjectileId;
        dst.SourceUnitId = src.SourceUnitId;
        dst.TargetUnitId = src.TargetUnitId;
        dst.Team = (Team)src.Team;
        dst.MovementType = (ProjectileMovementType)src.MovementType;
        dst.CurrentPosition = new SimVector3(src.CurrentPosition.X, src.CurrentPosition.Y, src.CurrentPosition.Z);
        dst.Direction = new SimVector3(src.Direction.X, src.Direction.Y, src.Direction.Z);
        dst.TargetPosition = new SimVector3(src.TargetPosition.X, src.TargetPosition.Y, src.TargetPosition.Z);
        dst.Progress = src.Progress;
        dst.Speed = src.Speed;
        dst.IsDead = src.IsDead;
    }

    private static Dictionary<int, UnitVisualSnapshot> CaptureUnitVisualState(Dictionary<int, UnitData> units)
    {
        var snapshot = new Dictionary<int, UnitVisualSnapshot>(units.Count);
        foreach (var (unitId, unit) in units)
        {
            snapshot[unitId] = new UnitVisualSnapshot(
                unit.CurrentHp,
                unit.IsAlive,
                unit.BehaviorState,
                unit.TargetNetworkId);
        }
        return snapshot;
    }

    private void QueueDerivedUnitEvents(
        Dictionary<int, UnitVisualSnapshot> previousUnits,
        UnitState[] currentUnits,
        HashSet<int> currentIds)
    {
        foreach (var src in currentUnits)
        {
            int unitId = src.NetworkId;

            if (!previousUnits.TryGetValue(unitId, out var previous))
                continue;

            if (previous.IsAlive && !src.IsAlive)
            {
                int killerId = src.TargetNetworkId ?? previous.TargetNetworkId ?? -1;
                _pendingEvents.Add(new UnitDiedEvent(unitId, killerId));
                continue;
            }

            if (!src.IsAlive)
                continue;

            float damage = previous.Hp - src.Hp;
            if (damage > 0.01f)
            {
                int attackerId = src.TargetNetworkId ?? previous.TargetNetworkId ?? -1;
                _pendingEvents.Add(new UnitDamagedEvent(unitId, attackerId, damage, false));
            }

            var newBehavior = (BehaviorState)src.BehaviorState;
            if (previous.BehaviorState != BehaviorState.Attacking && newBehavior == BehaviorState.Attacking)
            {
                int targetId = src.TargetNetworkId ?? previous.TargetNetworkId ?? -1;
                if (targetId >= 0)
                    _pendingEvents.Add(new UnitAttackedEvent(unitId, targetId));
            }
        }

        foreach (var (unitId, previous) in previousUnits)
        {
            if (previous.IsAlive && !currentIds.Contains(unitId))
                _pendingEvents.Add(new UnitDiedEvent(unitId, -1));
        }
    }
}
