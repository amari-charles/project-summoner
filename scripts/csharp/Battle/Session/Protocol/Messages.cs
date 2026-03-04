using Godot;

namespace Fateforged.Multiplayer.Protocol;

/// <summary>
/// All network message types for multiplayer communication.
/// These messages form the contract between host and client.
/// Transport-agnostic: works for P2P, Nakama relay, or dedicated server.
/// </summary>

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
);

/// <summary>
/// Client wants to forfeit/surrender the match.
/// </summary>
public readonly record struct ForfeitRequest(int PlayerIndex);

/// <summary>
/// Client reports their computed state hash for desync detection.
/// </summary>
public readonly record struct StateHashReport(
    int PlayerIndex,
    long Frame,
    int Hash
);

/// <summary>
/// Client signals they are ready to start the match.
/// </summary>
public readonly record struct PlayerReady(int PlayerIndex, bool IsReady);


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
);

/// <summary>
/// Host rejects a card play (not enough mana, invalid position, etc.)
/// </summary>
public readonly record struct CardPlayRejected(
    int Sequence,
    int PlayerIndex,
    string Reason
);

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
    int StateHash,
    bool IsOvertime
);

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
);

/// <summary>
/// A unit died.
/// </summary>
public readonly record struct UnitDied(
    int NetworkId,
    int? KillerNetworkId
);

/// <summary>
/// Damage was dealt to a unit or summoner.
/// Used for damage numbers and visual feedback.
/// </summary>
public readonly record struct DamageDealt(
    int TargetNetworkId,
    float Amount,
    bool IsCrit,
    int? SourceNetworkId
);

/// <summary>
/// Summoner took direct damage.
/// </summary>
public readonly record struct SummonerDamaged(
    int Team,
    float Amount,
    float NewHp
);

/// <summary>
/// Summoner took damage — triggers visual flash on client.
/// Distinct from SummonerDamaged which carries HP state.
/// </summary>
public readonly record struct SummonerDamageFlash(
    int Team,
    float Damage,
    int AttackerUnitId
);

/// <summary>
/// Summoner was destroyed — triggers death animation on client.
/// </summary>
public readonly record struct SummonerDestroyed(
    int Team,
    int KillerUnitId
);

/// <summary>
/// Match has ended.
/// </summary>
public readonly record struct MatchEnded(
    int WinnerIndex,
    string Reason,
    float Duration
);


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
);

/// <summary>
/// Player info exchange during lobby.
/// </summary>
public readonly record struct PlayerInfo(
    int PlayerIndex,
    string PlayerId,
    string DisplayName,
    string SummonerId
);

/// <summary>
/// Ping/pong for latency measurement.
/// </summary>
public readonly record struct Ping(long Timestamp);
public readonly record struct Pong(long OriginalTimestamp, long ServerTimestamp);


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
    string[] Hand,
    string[] Deck,
    string[] DiscardPile
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
    bool IsFacingRight
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

    // Bidirectional
    MatchStarted = 20,
    PlayerInfo = 21,
    Ping = 22,
    Pong = 23,
}
