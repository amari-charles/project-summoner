using System;
using System.Collections.Generic;
using Fateforged.Data.Projectiles;
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
    private const float ClientSteerFallback = 180f;
    private const float ClientWeaveReferenceDistance = 10f;
    private const float ClientWeaveMinDistance = 3f;
    private const float ClientWeavePitchRatio = 0.45f;
    private const float ClientWeaveCounterYawRatio = 0.85f;
    private const float ClientWeaveCounterPitchRatio = 0.6f;
    private const float ClientHomingWeaveSettleDistance = 10.0f;
    private const float ClientHomingWeaveYawRatio = 0.55f;
    private const float ClientHomingWeavePitchRatio = 0.28f;
    private const float ClientHomingWeaveFrequency = 8.5f;
    private const float ClientHomingWeavePitchFrequency = 6.2f;
    private const float ClientHomingFarSteerScale = 0.38f;
    private const float ClientHomingFinalLockDistance = 6.0f;
    private const float ClientHomingFinalLockTime = 0.55f;
    private const float ClientVectorEpsilon = 0.0001f;
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
        var currentPosition = new SimVector3(spawned.CurrentPosition.X, spawned.CurrentPosition.Y, spawned.CurrentPosition.Z);
        var targetPosition = new SimVector3(spawned.TargetPosition.X, spawned.TargetPosition.Y, spawned.TargetPosition.Z);
        var direction = new SimVector3(spawned.Direction.X, spawned.Direction.Y, spawned.Direction.Z);
        if (direction.LengthSquared() <= ClientVectorEpsilon)
        {
            var toTarget = targetPosition - currentPosition;
            if (toTarget.LengthSquared() > ClientVectorEpsilon)
                direction = toTarget.Normalized();
        }

        var projectile = new SimProjectileData
        {
            ProjectileId = spawned.ProjectileId,
            ProjectileCatalogId = spawned.ProjectileCatalogId ?? "",
            SourceUnitId = spawned.SourceUnitId,
            TargetUnitId = spawned.TargetUnitId,
            Team = (Team)spawned.Team,
            MovementType = (ProjectileMovementType)spawned.MovementType,
            StartPosition = currentPosition,
            CurrentPosition = currentPosition,
            LastPosition = currentPosition,
            Direction = direction,
            TargetPosition = targetPosition,
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
            HitRadius = spawned.HitRadius,
            HitSpace = spawned.HitSpace,
            SteerStrength = ClientSteerFallback,
            IsDead = false
        };

        ApplyClientProjectileDefaults(projectile);
        _localState.Projectiles[spawned.ProjectileId] = projectile;
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
                Lifetime: seed.Lifetime,
                HitRadius: seed.HitRadius,
                HitSpace: seed.HitSpace
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

        switch (projectile.MovementType)
        {
            case ProjectileMovementType.WeavingHoming:
                TickClientWeavingProjectile(projectile, delta);
                return;
            case ProjectileMovementType.Homing:
                TickClientHomingProjectile(projectile, delta);
                return;
        }

        var direction = projectile.Direction;
        if (direction.LengthSquared() <= ClientVectorEpsilon)
        {
            direction = (projectile.TargetPosition - projectile.CurrentPosition).Normalized();
            projectile.Direction = direction;
        }

        projectile.CurrentPosition += direction * projectile.Speed * delta;
    }

    private void TickClientHomingProjectile(SimProjectileData projectile, float delta)
    {
        if (projectile.TargetUnitId >= 0 && _localState.Units.TryGetValue(projectile.TargetUnitId, out var targetUnit))
            projectile.TargetPosition = targetUnit.Position;

        var toTarget = projectile.TargetPosition - projectile.CurrentPosition;
        if (toTarget.LengthSquared() > ClientVectorEpsilon)
        {
            projectile.Direction = SteerToward(
                projectile.Direction,
                toTarget.Normalized(),
                projectile.SteerStrength,
                delta);
        }

        if (projectile.Direction.LengthSquared() <= ClientVectorEpsilon)
            projectile.Direction = toTarget.Normalized();

        projectile.CurrentPosition += projectile.Direction * projectile.Speed * delta;
    }

    private void TickClientWeavingProjectile(SimProjectileData projectile, float delta)
    {
        projectile.PhaseTimer += delta;

        if (projectile.TargetUnitId >= 0 && _localState.Units.TryGetValue(projectile.TargetUnitId, out var targetUnit))
            projectile.TargetPosition = targetUnit.Position;

        switch (projectile.WeavingPhase)
        {
            case WeavingPhase.Straight:
            {
                if (projectile.PhaseTimer >= projectile.ScaledVeerDelay)
                {
                    projectile.WeavingPhase = WeavingPhase.VeeringOut;
                    projectile.PhaseTimer = 0f;
                }
                else
                {
                    var toTarget = projectile.TargetPosition - projectile.CurrentPosition;
                    if (toTarget.LengthSquared() > ClientVectorEpsilon)
                        projectile.Direction = toTarget.Normalized();
                }

                break;
            }

            case WeavingPhase.VeeringOut:
            {
                if (projectile.PhaseTimer >= projectile.ScaledVeerDuration)
                {
                    projectile.WeavingPhase = WeavingPhase.VeeringBack;
                    projectile.PhaseTimer = 0f;
                }
                else
                {
                    var desired = BlendWithTarget(projectile, projectile.VeerDirection, 0.06f);
                    projectile.Direction = SteerToward(projectile.Direction, desired, projectile.SteerStrength, delta);
                }

                break;
            }

            case WeavingPhase.VeeringBack:
            {
                if (projectile.PhaseTimer >= projectile.ScaledCounterVeerDuration)
                {
                    projectile.WeavingPhase = WeavingPhase.Homing;
                    projectile.PhaseTimer = 0f;
                }
                else
                {
                    var desired = BlendWithTarget(projectile, projectile.CounterVeerDirection, 0.12f);
                    projectile.Direction = SteerToward(projectile.Direction, desired, projectile.SteerStrength, delta);
                }

                break;
            }

            case WeavingPhase.Homing:
            {
                var toTarget = projectile.TargetPosition - projectile.CurrentPosition;
                if (toTarget.LengthSquared() > ClientVectorEpsilon)
                {
                    float distanceToTarget = toTarget.Length();
                    bool finalLock = distanceToTarget <= ClientHomingFinalLockDistance
                                     || projectile.PhaseTimer >= ClientHomingFinalLockTime;
                    var desired = finalLock ? toTarget.Normalized() : ApplyClientHomingWeave(projectile, toTarget);
                    float settle = Math.Clamp(distanceToTarget / ClientHomingWeaveSettleDistance, 0f, 1f);
                    float steerScale = finalLock
                        ? 1f
                        : (ClientHomingFarSteerScale + ((1f - ClientHomingFarSteerScale) * (1f - settle)));
                    projectile.Direction = SteerToward(
                        projectile.Direction,
                        desired,
                        projectile.SteerStrength * steerScale,
                        delta);
                }

                break;
            }
        }

        if (projectile.Direction.LengthSquared() <= ClientVectorEpsilon)
            projectile.Direction = (projectile.TargetPosition - projectile.CurrentPosition).Normalized();

        projectile.CurrentPosition += projectile.Direction * projectile.Speed * delta;

        var frameTravel = projectile.CurrentPosition - projectile.LastPosition;
        if (frameTravel.LengthSquared() > ClientVectorEpsilon)
            projectile.Direction = frameTravel.Normalized();
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

    private void ApplyClientProjectileDefaults(SimProjectileData projectile)
    {
        var projectileData = ResolveProjectileData(projectile);
        if (projectileData == null)
            return;

        projectile.SteerStrength = projectileData.SteerStrength;
        projectile.ArcHeight = projectileData.ArcHeight;

        if (projectile.MovementType == ProjectileMovementType.WeavingHoming)
            InitializeClientWeavingState(projectile, projectileData);
    }

    private ProjectileData? ResolveProjectileData(SimProjectileData projectile)
    {
        if (!string.IsNullOrEmpty(projectile.ProjectileCatalogId))
        {
            var byProjectileId = ProjectileDefinitions.Get(projectile.ProjectileCatalogId);
            if (byProjectileId != null)
                return byProjectileId;
        }

        if (!_localState.Units.TryGetValue(projectile.SourceUnitId, out var sourceUnit))
            return null;
        if (string.IsNullOrEmpty(sourceUnit.CatalogId))
            return null;

        var unitDef = UnitDefinitions.Get(sourceUnit.CatalogId);
        if (unitDef?.Ranged == null)
            return null;

        return ProjectileDefinitions.Get(unitDef.Ranged.ProjectileId);
    }

    private static void InitializeClientWeavingState(SimProjectileData projectile, ProjectileData projectileData)
    {
        projectile.WeavingPhase = WeavingPhase.Straight;
        projectile.PhaseTimer = 0f;
        projectile.Velocity = projectile.Direction * projectile.Speed;

        float distance = projectile.CurrentPosition.DistanceTo(projectile.TargetPosition);
        float distanceScale = Math.Clamp(distance / ClientWeaveReferenceDistance, 0f, 1f);
        if (distance < ClientWeaveMinDistance)
        {
            projectile.ScaledVeerDelay = 0f;
            projectile.ScaledVeerDuration = 0f;
            projectile.ScaledCounterVeerDuration = 0f;
            projectile.WeavingPhase = WeavingPhase.Homing;
            return;
        }

        projectile.ScaledVeerDelay = projectileData.VeerDelay * distanceScale;
        float weaveDuration = projectileData.VeerDuration * distanceScale;
        projectile.ScaledVeerDuration = weaveDuration;
        projectile.ScaledCounterVeerDuration = weaveDuration * 0.85f;

        float scaledVeerAngle = projectileData.VeerAngle * distanceScale;
        float veerSign = (projectile.ProjectileId & 1) == 0 ? 1f : -1f;
        float pitchSign = ((projectile.ProjectileId >> 1) & 1) == 0 ? 1f : -1f;
        float veerYawRadians = Mathf.DegToRad(scaledVeerAngle) * veerSign;
        float veerPitchRadians = Mathf.DegToRad(scaledVeerAngle * ClientWeavePitchRatio) * pitchSign;

        var direction = ToGodot(projectile.Direction);
        if (direction.LengthSquared() <= ClientVectorEpsilon)
        {
            direction = ToGodot(projectile.TargetPosition - projectile.CurrentPosition).Normalized();
            projectile.Direction = ToSim(direction);
        }

        var rightAxis = GetStableRightAxis(direction);
        var outDir = RotateAround(direction, Vector3.Up, veerYawRadians);
        outDir = RotateAround(outDir, rightAxis, veerPitchRadians);
        var backDir = RotateAround(direction, Vector3.Up, -veerYawRadians * ClientWeaveCounterYawRatio);
        backDir = RotateAround(backDir, rightAxis, -veerPitchRadians * ClientWeaveCounterPitchRatio);

        projectile.VeerDirection = ToSim(outDir.Normalized());
        projectile.CounterVeerDirection = ToSim(backDir.Normalized());
    }

    private static SimVector3 BlendWithTarget(SimProjectileData projectile, SimVector3 weaveDirection, float targetWeight)
    {
        var toTarget = projectile.TargetPosition - projectile.CurrentPosition;
        if (toTarget.LengthSquared() <= ClientVectorEpsilon)
            return weaveDirection;

        var desired = (weaveDirection * (1f - targetWeight)) + (toTarget.Normalized() * targetWeight);
        return desired.LengthSquared() <= ClientVectorEpsilon ? weaveDirection : desired.Normalized();
    }

    private static SimVector3 ApplyClientHomingWeave(SimProjectileData projectile, SimVector3 toTarget)
    {
        var targetDirection = toTarget.Normalized();
        float targetDistance = toTarget.Length();
        if (targetDistance <= ClientVectorEpsilon)
            return targetDirection;

        float arc = MathF.Max(0f, projectile.ArcHeight);
        if (arc <= ClientVectorEpsilon)
            return targetDirection;

        float settle = Math.Clamp(targetDistance / ClientHomingWeaveSettleDistance, 0f, 1f);
        float yawAmplitudeDegrees = MathF.Max(22f, arc * 16f);
        float yawAmplitudeRadians = Mathf.DegToRad(yawAmplitudeDegrees);

        float phase = (projectile.TimeAlive * ClientHomingWeaveFrequency) + (projectile.ProjectileId * 0.37f);
        float yawOffset = MathF.Sin(phase) * yawAmplitudeRadians * ClientHomingWeaveYawRatio * settle;
        float pitchOffset = MathF.Cos(phase * (ClientHomingWeavePitchFrequency / ClientHomingWeaveFrequency))
                            * yawAmplitudeRadians * ClientHomingWeavePitchRatio * settle;

        var targetDirectionGodot = ToGodot(targetDirection);
        var rightAxis = GetStableRightAxis(targetDirectionGodot);
        var woven = RotateAround(targetDirectionGodot, Vector3.Up, yawOffset);
        woven = RotateAround(woven, rightAxis, pitchOffset);
        return ToSim(woven.Normalized());
    }

    private static SimVector3 SteerToward(SimVector3 currentDirection, SimVector3 desiredDirection, float steerStrength, float delta)
    {
        var current = ToGodot(currentDirection);
        var desired = ToGodot(desiredDirection);
        if (desired.LengthSquared() <= ClientVectorEpsilon)
            return currentDirection;

        desired = desired.Normalized();
        if (current.LengthSquared() <= ClientVectorEpsilon)
            return ToSim(desired);

        current = current.Normalized();
        float angle = current.AngleTo(desired);
        if (angle <= ClientVectorEpsilon)
            return ToSim(desired);

        float maxRotation = Mathf.DegToRad(steerStrength) * delta;
        if (maxRotation <= ClientVectorEpsilon || angle <= maxRotation)
            return ToSim(desired);

        float weight = Mathf.Clamp(maxRotation / angle, 0f, 1f);
        return ToSim(current.Slerp(desired, weight).Normalized());
    }

    private static Vector3 RotateAround(Vector3 vector, Vector3 axis, float radians)
    {
        if (axis.LengthSquared() <= ClientVectorEpsilon)
            return vector;

        var basis = new Basis(axis.Normalized(), radians);
        return basis * vector;
    }

    private static Vector3 GetStableRightAxis(Vector3 forward)
    {
        var right = forward.Cross(Vector3.Up);
        if (right.LengthSquared() <= ClientVectorEpsilon)
            right = forward.Cross(Vector3.Forward);
        if (right.LengthSquared() <= ClientVectorEpsilon)
            right = Vector3.Right;
        return right.Normalized();
    }

    private static Vector3 ToGodot(SimVector3 value) => new(value.X, value.Y, value.Z);
    private static SimVector3 ToSim(Vector3 value) => new(value.X, value.Y, value.Z);

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
