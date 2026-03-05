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

        TickClientProjectiles(delta);

        if (_pendingEvents.Count > 0)
        {
            SimEventsEmitted?.Invoke(_pendingEvents);
            _pendingEvents.Clear();
        }
    }

    protected override void HandleMessage(int senderId, IProtocolMessage message)
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
            case SpellCastVisual spellCast:
                _pendingEvents.Add(new SpellCastEvent(
                    spellCast.Team,
                    spellCast.CatalogId,
                    new SimVector3(spellCast.Position.X, spellCast.Position.Y, spellCast.Position.Z)));
                break;
            case ProjectileSpawned spawned:
                HandleProjectileSpawned(spawned);
                break;
            case ProjectileImpact impact:
                HandleProjectileImpact(impact);
                break;
            case ProjectileDespawned despawned:
                HandleProjectileDespawned(despawned);
                break;
            case ProjectileSeedSnapshot seedSnapshot:
                ApplyProjectileSeedSnapshot(seedSnapshot);
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
            dst.CastingCatalogId = src.CastingCatalogId;
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

    private void HandleProjectileSpawned(ProjectileSpawned spawned)
    {
        _localState.Projectiles[spawned.ProjectileId] = new SimProjectileData
        {
            ProjectileId = spawned.ProjectileId,
            ProjectileCatalogId = spawned.ProjectileCatalogId ?? "",
            SourceUnitId = spawned.SourceUnitId,
            TargetUnitId = spawned.TargetUnitId,
            Team = (Team)spawned.Team,
            MovementType = (ProjectileMovementType)spawned.MovementType,
            CurrentPosition = new SimVector3(spawned.CurrentPosition.X, spawned.CurrentPosition.Y, spawned.CurrentPosition.Z),
            LastPosition = new SimVector3(spawned.CurrentPosition.X, spawned.CurrentPosition.Y, spawned.CurrentPosition.Z),
            Direction = new SimVector3(spawned.Direction.X, spawned.Direction.Y, spawned.Direction.Z),
            TargetPosition = new SimVector3(spawned.TargetPosition.X, spawned.TargetPosition.Y, spawned.TargetPosition.Z),
            Speed = spawned.Speed,
            Acceleration = spawned.Acceleration,
            MinSpeed = spawned.MinSpeed,
            UseSpeedEasing = spawned.UseSpeedEasing,
            SpeedStart = spawned.SpeedStart,
            SpeedEnd = spawned.SpeedEnd,
            SpeedTransitionDuration = spawned.SpeedTransitionDuration,
            SpeedEasing = (SpeedEasingType)spawned.SpeedEasing,
            SpeedEaseExponent = spawned.SpeedEaseExponent,
            TimeAlive = spawned.TimeAlive,
            Lifetime = spawned.Lifetime,
            IsDead = false
        };
    }

    private void HandleProjectileImpact(ProjectileImpact impact)
    {
        if (_localState.Projectiles.TryGetValue(impact.ProjectileId, out var projectile))
            projectile.IsDead = true;

        _pendingEvents.Add(new ProjectileHitEvent(impact.ProjectileId, impact.TargetUnitId));
    }

    private void HandleProjectileDespawned(ProjectileDespawned despawned)
    {
        _localState.Projectiles.Remove(despawned.ProjectileId);
    }

    private void ApplyProjectileSeedSnapshot(ProjectileSeedSnapshot seedSnapshot)
    {
        _staleProjectileIds.Clear();
        foreach (var projectileId in _localState.Projectiles.Keys)
            _staleProjectileIds.Add(projectileId);

        foreach (var seed in seedSnapshot.Projectiles)
        {
            HandleProjectileSpawned(new ProjectileSpawned(
                ProjectileId: seed.ProjectileId,
                SourceUnitId: seed.SourceUnitId,
                TargetUnitId: seed.TargetUnitId,
                Team: seed.Team,
                MovementType: seed.MovementType,
                CurrentPosition: seed.CurrentPosition,
                Direction: seed.Direction,
                TargetPosition: seed.TargetPosition,
                Speed: seed.Speed,
                ProjectileCatalogId: seed.ProjectileCatalogId,
                Acceleration: seed.Acceleration,
                MinSpeed: seed.MinSpeed,
                UseSpeedEasing: seed.UseSpeedEasing,
                SpeedStart: seed.SpeedStart,
                SpeedEnd: seed.SpeedEnd,
                SpeedTransitionDuration: seed.SpeedTransitionDuration,
                SpeedEasing: seed.SpeedEasing,
                SpeedEaseExponent: seed.SpeedEaseExponent,
                TimeAlive: seed.TimeAlive,
                Lifetime: seed.Lifetime
            ));
            _staleProjectileIds.Remove(seed.ProjectileId);
        }

        foreach (var staleProjectileId in _staleProjectileIds)
            _localState.Projectiles.Remove(staleProjectileId);
    }

    private void TickClientProjectiles(float delta)
    {
        if (_localState.Projectiles.Count == 0)
            return;

        _staleProjectileIds.Clear();

        foreach (var (projectileId, projectile) in _localState.Projectiles)
        {
            if (projectile.IsDead)
            {
                _staleProjectileIds.Add(projectileId);
                continue;
            }

            TickClientProjectileSpeed(projectile, delta);
            TickClientProjectileMovement(projectile, delta);

            projectile.TimeAlive += delta;
            if (projectile.TimeAlive >= MathF.Max(projectile.Lifetime, 0.01f))
                _staleProjectileIds.Add(projectileId);
        }

        foreach (var staleProjectileId in _staleProjectileIds)
            _localState.Projectiles.Remove(staleProjectileId);
    }

    private void TickClientProjectileMovement(SimProjectileData projectile, float delta)
    {
        projectile.LastPosition = projectile.CurrentPosition;

        var direction = projectile.Direction;

        bool canSteer = projectile.MovementType == ProjectileMovementType.Homing
                        || projectile.MovementType == ProjectileMovementType.WeavingHoming;

        if (canSteer)
        {
            if (projectile.TargetUnitId >= 0 && _localState.Units.TryGetValue(projectile.TargetUnitId, out var targetUnit))
                projectile.TargetPosition = targetUnit.Position;

            var desired = (projectile.TargetPosition - projectile.CurrentPosition).Normalized();
            if (desired.LengthSquared() > 0.0001f)
            {
                float steerWeight = MathF.Min(1f, delta * 8f);
                direction = direction.Lerp(desired, steerWeight).Normalized();
                projectile.Direction = direction;
            }
        }

        if (direction.LengthSquared() <= 0.0001f)
            direction = (projectile.TargetPosition - projectile.CurrentPosition).Normalized();

        projectile.CurrentPosition += direction * projectile.Speed * delta;
    }

    private static void TickClientProjectileSpeed(SimProjectileData projectile, float delta)
    {
        if (projectile.UseSpeedEasing)
        {
            float duration = MathF.Max(projectile.SpeedTransitionDuration, 0.0001f);
            float t = Math.Clamp(projectile.TimeAlive / duration, 0f, 1f);
            float eased = EvaluateSpeedEasing(t, projectile.SpeedEasing, projectile.SpeedEaseExponent);
            projectile.Speed = projectile.SpeedStart + ((projectile.SpeedEnd - projectile.SpeedStart) * eased);
            return;
        }

        if (MathF.Abs(projectile.Acceleration) < 0.0001f)
            return;

        projectile.Speed += projectile.Acceleration * delta;
        if (projectile.Acceleration < 0f && projectile.Speed < projectile.MinSpeed)
            projectile.Speed = projectile.MinSpeed;
    }

    private static float EvaluateSpeedEasing(float t, SpeedEasingType easingType, float exponent)
    {
        float clampedT = Math.Clamp(t, 0f, 1f);
        float safeExponent = MathF.Max(exponent, 1f);
        return easingType switch
        {
            SpeedEasingType.EaseIn => MathF.Pow(clampedT, safeExponent),
            SpeedEasingType.EaseOut => 1f - MathF.Pow(1f - clampedT, safeExponent),
            SpeedEasingType.EaseInOut => (1f - MathF.Cos(clampedT * MathF.PI)) * 0.5f,
            _ => clampedT
        };
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
