using Fateforged.Projectiles;
using Fateforged.Simulation.Data;
using Godot;

namespace Fateforged.Multiplayer.Protocol;

/// <summary>
/// All network message types for multiplayer communication.
/// These messages form the contract between host and client.
/// Transport-agnostic: works for P2P, Nakama relay, or dedicated server.
/// </summary>
public interface IProtocolMessage;

public interface IRealtimeProtocolMessage : IProtocolMessage;

public interface IReconnectSeedProtocolMessage : IProtocolMessage;

// ============================================================================
// CLIENT → HOST MESSAGES
// ============================================================================

/// <summary>
/// Client requests to play a card. Host will validate and confirm/reject.
/// </summary>
/// <param name="Sequence">Unique sequence number for reconciliation</param>
/// <param name="PlayerIndex">Which player (0 or 1)</param>
/// <param name="CardIndex">Index in hand</param>
/// <param name="Position">World position for spawn</param>
/// <param name="ClientTimestamp">Client's local timestamp for latency calculation</param>
public readonly record struct CardPlayRequest(
    int Sequence,
    int PlayerIndex,
    int CardIndex,
    Vector3 Position,
    long ClientTimestamp
) : IRealtimeProtocolMessage;

/// <summary>
/// Client wants to forfeit/surrender the match.
/// </summary>
public readonly record struct ForfeitRequest(int PlayerIndex) : IRealtimeProtocolMessage;

/// <summary>
/// Client reports their computed state hash for desync detection.
/// </summary>
public readonly record struct StateHashReport(int PlayerIndex, long Frame, int Hash)
    : IRealtimeProtocolMessage;

/// <summary>
/// Client signals they are ready to start the match.
/// </summary>
public readonly record struct PlayerReady(int PlayerIndex, bool IsReady) : IRealtimeProtocolMessage;

// ============================================================================
// HOST → CLIENT MESSAGES
// ============================================================================

/// <summary>
/// Host confirms a card play was valid and executed.
/// </summary>
public readonly record struct CardPlayConfirmed(
    int Sequence,
    int PlayerIndex,
    int CardIndex,
    Vector3 Position,
    long ServerFrame,
    int SpawnedUnitNetworkId
) : IRealtimeProtocolMessage;

/// <summary>
/// Host rejects a card play (not enough mana, invalid position, etc.)
/// </summary>
public readonly record struct CardPlayRejected(int Sequence, int PlayerIndex, string Reason)
    : IRealtimeProtocolMessage;

/// <summary>
/// Periodic state snapshot for synchronization and desync correction.
/// Sent at ~10 Hz (every 100ms).
/// </summary>
public readonly record struct StateSnapshot(
    long Frame,
    float MatchTime,
    int Phase,
    float PrepTimeRemaining,
    SummonerState[] Summoners,
    UnitState[] Units,
    ProjectileState[] Projectiles,
    int StateHash,
    bool IsOvertime
) : IRealtimeProtocolMessage;

/// <summary>
/// A unit was spawned (from card play).
/// This is the primary event for card plays - receiving this means the play succeeded.
/// </summary>
public readonly record struct UnitSpawned(
    int NetworkId,
    string UnitType,
    int Team,
    Vector3 Position,
    long MatchTick,
    int? SourceSequence,
    int? SourcePlayerIndex,
    float SpawnDuration
) : IRealtimeProtocolMessage;

/// <summary>
/// A unit died.
/// </summary>
public readonly record struct UnitDied(int NetworkId, int? KillerNetworkId)
    : IRealtimeProtocolMessage;

/// <summary>
/// Damage was dealt to a unit or summoner.
/// Used for damage numbers and visual feedback.
/// </summary>
public readonly record struct DamageDealt(
    int TargetNetworkId,
    float Amount,
    bool IsCrit,
    int? SourceNetworkId
) : IRealtimeProtocolMessage;

/// <summary>
/// Summoner took direct damage.
/// </summary>
public readonly record struct SummonerDamaged(int Team, float Amount, float NewHp)
    : IRealtimeProtocolMessage;

/// <summary>
/// Summoner took damage — triggers visual flash on client.
/// Distinct from SummonerDamaged which carries HP state.
/// </summary>
public readonly record struct SummonerDamageFlash(int Team, float Damage, int AttackerUnitId)
    : IRealtimeProtocolMessage;

/// <summary>
/// Summoner was destroyed — triggers death animation on client.
/// </summary>
public readonly record struct SummonerDestroyed(int Team, int KillerUnitId)
    : IRealtimeProtocolMessage;

/// <summary>
/// Match has ended.
/// </summary>
public readonly record struct MatchEnded(int WinnerIndex, string Reason, float Duration)
    : IRealtimeProtocolMessage;

/// <summary>
/// Authoritative spell cast visual cue. Keeps cast VFX deterministic across peers.
/// </summary>
public readonly record struct SpellCastVisual(
    int Team,
    SimCardCatalogId CatalogId,
    Vector3 Position
) : IRealtimeProtocolMessage;

/// <summary>
/// Authoritative transient beam visual cue for instant hitscan attacks.
/// Fire-and-forget visual event; reconnect does not replay prior beams.
/// </summary>
public readonly record struct HitscanBeamVisual(
    int ProjectileId,
    SimProjectileCatalogId ProjectileCatalogId,
    Vector3 StartPosition,
    Vector3 EndPosition,
    float DurationSeconds
) : IRealtimeProtocolMessage;

/// <summary>
/// Authoritative projectile spawn message for client-side visual simulation.
/// </summary>
public readonly record struct ProjectileSpawned(
    int ProjectileId,
    int SourceUnitId,
    int TargetUnitId,
    int Team,
    int MovementType,
    Vector3 CurrentPosition,
    Vector3 Direction,
    Vector3 TargetPosition,
    float Speed,
    SimProjectileCatalogId ProjectileCatalogId = default,
    float Acceleration = 0f,
    float MinSpeed = 1f,
    bool UseSpeedEasing = false,
    float SpeedStart = 0f,
    float SpeedEnd = 0f,
    float SpeedTransitionDuration = 1f,
    int SpeedEasing = 0,
    float SpeedEaseExponent = 2f,
    float TimeAlive = 0f,
    float Lifetime = 5f,
    float HitRadius = 2.5f,
    ProjectileHitSpace HitSpace = ProjectileHitSpace.GroundCylinder,
    Vector3 VeerDirection = default,
    Vector3 CounterVeerDirection = default
) : IRealtimeProtocolMessage;

/// <summary>
/// Authoritative projectile hit message for client-side impact visuals.
/// </summary>
public readonly record struct ProjectileImpact(int ProjectileId, int TargetUnitId)
    : IRealtimeProtocolMessage;

/// <summary>
/// Authoritative projectile removal message for cleanup on clients.
/// </summary>
public readonly record struct ProjectileDespawned(int ProjectileId) : IRealtimeProtocolMessage;

/// <summary>
/// Seed state for one active projectile used after reconnect/late join recovery.
/// </summary>
public readonly record struct ActiveProjectileSeed(
    int ProjectileId,
    int SourceUnitId,
    int TargetUnitId,
    int Team,
    int MovementType,
    Vector3 CurrentPosition,
    Vector3 Direction,
    Vector3 TargetPosition,
    float Speed,
    SimProjectileCatalogId ProjectileCatalogId = default,
    float Acceleration = 0f,
    float MinSpeed = 1f,
    bool UseSpeedEasing = false,
    float SpeedStart = 0f,
    float SpeedEnd = 0f,
    float SpeedTransitionDuration = 1f,
    int SpeedEasing = 0,
    float SpeedEaseExponent = 2f,
    float TimeAlive = 0f,
    float Lifetime = 5f,
    float HitRadius = 2.5f,
    ProjectileHitSpace HitSpace = ProjectileHitSpace.GroundCylinder,
    Vector3 VeerDirection = default,
    Vector3 CounterVeerDirection = default
);

/// <summary>
/// Full active projectile seed snapshot sent after reconnect to rebuild client visuals.
/// </summary>
public readonly record struct ProjectileSeedSnapshot(long Frame, ActiveProjectileSeed[] Projectiles)
    : IReconnectSeedProtocolMessage;

// ============================================================================
// BIDIRECTIONAL / CONNECTION MESSAGES
// ============================================================================

/// <summary>
/// Match is starting. Sent by host to all clients.
/// </summary>
public readonly record struct MatchStarted(
    long Seed,
    string MatchId,
    string[] PlayerIds,
    string[] SummonerIds
) : IRealtimeProtocolMessage;

/// <summary>
/// Player info exchange during lobby.
/// </summary>
public readonly record struct PlayerInfo(
    int PlayerIndex,
    string PlayerId,
    string DisplayName,
    string SummonerId
) : IRealtimeProtocolMessage;

/// <summary>
/// Ping/pong for latency measurement.
/// </summary>
public readonly record struct Ping(long Timestamp) : IRealtimeProtocolMessage;

public readonly record struct Pong(long OriginalTimestamp, long ServerTimestamp)
    : IRealtimeProtocolMessage;

// ============================================================================
// STATE SUB-TYPES
// ============================================================================

/// <summary>
/// Summoner state for snapshots.
/// </summary>
public readonly record struct SummonerState(
    int Team,
    float Hp,
    float MaxHp,
    float Mana,
    float MaxMana,
    bool IsCasting,
    float CastingTimeRemaining,
    float CastingTimeTotal,
    int CastingCardIndex,
    Vector3 CastingSpawnPosition,
    int CastingNetworkId,
    int CardStateHash,
    SimCardCatalogId[] Hand,
    SimCardCatalogId[] Deck,
    SimCardCatalogId[] DiscardPile,
    // Required for MP client UI reconstruction in PollMatchState and reconnect flow.
    SimCardCatalogId CastingCatalogId = default
);

/// <summary>
/// Unit state for snapshots.
/// Positions are quantized to reduce bandwidth and avoid float drift.
/// </summary>
public readonly record struct UnitState(
    int NetworkId,
    int Team,
    Vector3 Position,
    float Hp,
    float MaxHp,
    int? TargetNetworkId,
    bool IsAlive,
    int ActivationState,
    int BehaviorState,
    bool IsFacingRight,
    SimUnitCatalogId CatalogId = default,
    float SpawnTimer = 0f,
    float AttackAnimationTimer = 0f
);

/// <summary>
/// Projectile state for snapshots.
/// Mirrors active projectile simulation state needed by client visuals.
/// </summary>
public readonly record struct ProjectileState(
    int ProjectileId,
    int SourceUnitId,
    int TargetUnitId,
    int Team,
    int MovementType,
    Vector3 CurrentPosition,
    Vector3 Direction,
    Vector3 TargetPosition,
    float Progress,
    float Speed,
    bool IsDead,
    SimProjectileCatalogId ProjectileCatalogId = default,
    float Acceleration = 0f,
    float MinSpeed = 1f,
    bool UseSpeedEasing = false,
    float SpeedStart = 0f,
    float SpeedEnd = 0f,
    float SpeedTransitionDuration = 1f,
    int SpeedEasing = 0,
    float SpeedEaseExponent = 2f,
    float TimeAlive = 0f,
    float Lifetime = 5f
);

// ============================================================================
// MESSAGE TYPE ENUM (for serialization)
// ============================================================================

public enum MessageType : byte
{
    // Client → Host
    CardPlayRequest = 1,
    ForfeitRequest = 2,
    StateHashReport = 3,
    PlayerReady = 4,

    // Host → Client
    CardPlayConfirmed = 10,
    CardPlayRejected = 11,
    StateSnapshot = 12,
    UnitSpawned = 13,
    UnitDied = 14,
    DamageDealt = 15,
    SummonerDamaged = 16,
    MatchEnded = 17,
    SummonerDamageFlash = 18,
    SummonerDestroyed = 19,
    ProjectileSpawned = 24,
    ProjectileImpact = 25,
    ProjectileDespawned = 26,
    ProjectileSeedSnapshot = 27,
    SpellCastVisual = 28,
    HitscanBeamVisual = 29,

    // Bidirectional
    MatchStarted = 20,
    PlayerInfo = 21,
    Ping = 22,
    Pong = 23,
}
