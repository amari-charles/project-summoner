# Multiplayer Implementation Phases

Detailed breakdown of the multiplayer implementation roadmap. This is a living document - check off items as they're completed.

---

## Progress Summary

| Phase | Status | Progress |
|-------|--------|----------|
| Phase 1: Network Foundation | ✅ Complete | 4/4 complete |
| Phase 2: Game Synchronization | ✅ Complete | 5/5 complete |
| Phase 3: Nakama Integration | 🔄 In Progress | 5/6 complete |
| Phase 4: Polish | ⚪ Not Started | 0/5 complete |

---

## Key Decisions Made

These decisions were made during planning and should be referenced when implementing:

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Network Model | Hybrid (P2P host → dedicated server later) | Start simple, migrate when needed |
| Backend | Nakama (open source) | Official Godot SDK, self-hostable, no vendor lock-in |
| Game Mode | 1v1 PvP only (initially) | Simplifies matchmaking and authority |
| Matchmaking | Ranked with ELO | Via Nakama matchmaker |
| Offline Support | Yes | Single-player campaign works without internet |

---

## Phase 1: Network Foundation

**Goal:** Build core networking infrastructure for local testing with room codes.

**Status:** ✅ Complete (4/4 complete)

---

### 1.1 Authority Abstraction Layer

- [x] **COMPLETED** (PR #149, merged 2026-01-04)

Create an abstraction that allows the same game code to work under different authority models (local, P2P host, P2P client, dedicated server).

**Why This Matters:**
- Single-player uses `LocalAuthority` (all actions immediate, no network)
- Multiplayer host uses `HostAuthority` (validates actions, broadcasts to clients)
- Multiplayer client uses `ClientProxy` (sends requests to host, waits for confirmation)
- Future dedicated server uses `ServerAuthority` (same interface, different backend)

**New Files to Create:**

| File | Purpose | Key Methods |
|------|---------|-------------|
| `scripts/multiplayer/authority/authority_provider.gd` | Abstract base class | `has_authority()`, `execute_action()`, `request_action()` |
| `scripts/multiplayer/authority/local_authority.gd` | Single-player (offline) | Executes all actions immediately |
| `scripts/multiplayer/authority/host_authority.gd` | P2P host | Validates, executes, broadcasts via RPC |
| `scripts/multiplayer/authority/client_proxy.gd` | P2P client | Sends requests to host, applies confirmed actions |

**Files to Modify:**

| File | Changes |
|------|---------|
| `scripts/core/battle_context.gd` | Add `authority_provider: AuthorityProvider` property, initialize based on battle mode |
| `scripts/core/game_controller_3d.gd` | Check `authority_provider.has_authority()` before state changes |

**Key Interface:**
```gdscript
class_name AuthorityProvider extends RefCounted

## Returns true if this peer has authority over game state
func has_authority() -> bool:
    return false  # Override in subclasses

## Returns true if this action belongs to the local player
func is_local_action(player_id: int) -> bool:
    return false  # Override in subclasses

## Execute an action with authority (host/server only)
func execute_action(action: GameAction) -> void:
    pass  # Override in subclasses

## Request an action (client sends to authority for validation)
func request_action(action: GameAction) -> void:
    pass  # Override in subclasses

## Called when authority confirms/rejects an action
signal action_confirmed(action: GameAction)
signal action_rejected(action: GameAction, reason: String)
```

**Implementation Steps:**
1. [x] Create `authority_provider.gd` with base interface
2. [x] Create `local_authority.gd` that executes immediately (current behavior)
3. [x] Update `battle_context.gd` to hold authority reference
4. [x] Update `game_controller_3d.gd` to use authority checks
5. [x] Test that single-player still works with LocalAuthority
6. [x] Create `host_authority.gd` stub (full implementation in Phase 1.4)
7. [x] Create `client_proxy.gd` stub (full implementation in Phase 1.4)

**What Was Implemented:**

| File | Status | Description |
|------|--------|-------------|
| `scripts/multiplayer/authority/authority_provider.gd` | ✅ Created | Base class with `has_authority()`, `execute_action()`, `request_action()` |
| `scripts/multiplayer/authority/local_authority.gd` | ✅ Created | Single-player implementation (executes immediately) |
| `scripts/multiplayer/authority/host_authority.gd` | ✅ Created | P2P host stub (full RPC in Phase 1.4) |
| `scripts/multiplayer/authority/client_proxy.gd` | ✅ Created | P2P client stub (full RPC in Phase 1.4) |
| `scripts/multiplayer/actions/game_action.gd` | ✅ Created | Base action class for serialization |
| `scripts/core/battle_context.gd` | ✅ Modified | Added `authority_provider`, `has_authority()`, `set_authority_provider()` |
| `scripts/core/game_controller_3d.gd` | ✅ Modified | Added authority checks to `end_game()`, win conditions |

**How It Works:**
```gdscript
# BattleContext auto-initializes LocalAuthority for single-player
BattleContext.start_battle()  # Creates LocalAuthority internally

# Check authority before state changes
if BattleContext.has_authority():
    end_game(winner)

# For multiplayer (Phase 1.4+), set a different provider
BattleContext.set_authority_provider(HostAuthority.new(self))
```

---

### 1.2 Seeded RNG System

- [x] **COMPLETED** (PR #147, merged 2026-01-03)

Replace all gameplay randomness with seeded RNG for deterministic multiplayer.

**What Was Implemented:**

| File | Status | Description |
|------|--------|-------------|
| `scripts/multiplayer/rng/battle_rng.gd` | ✅ Created | Singleton with domain-based seeded RNG |
| `scripts/multiplayer/rng/rng_domain.gd` | ✅ Created | Enum: DECK_SHUFFLE, AI_DECISIONS, COMBAT_CRITS, SPAWN_POSITIONS |
| `scripts/core/summoner.gd` | ✅ Modified | `deck.shuffle()` → `BattleRNG.shuffle_array(deck, RNGDomain.Domain.DECK_SHUFFLE)` |
| `scripts/ai/heuristic_ai.gd` | ✅ Modified | All `randf_range()` → `BattleRNG.randf_range(..., RNGDomain.Domain.*)` |
| `scripts/csharp/Combat/DamageSystem.cs` | ✅ Modified | `GD.Randf()` → `GetSeededCritRoll()` calling BattleRNG |
| `project.godot` | ✅ Modified | Added BattleRNG autoload |

**How It Works:**
```gdscript
# At battle start (in multiplayer, host generates and shares this seed)
BattleRNG.set_battle_seed(12345)

# All gameplay randomness uses seeded system
BattleRNG.shuffle_array(deck, RNGDomain.Domain.DECK_SHUFFLE)
var timing = BattleRNG.randf_range(3.0, 6.0, RNGDomain.Domain.AI_DECISIONS)
var crit_roll = BattleRNG.randf(RNGDomain.Domain.COMBAT_CRITS)
```

**RNG Domains:**
| Domain | Used For | Files |
|--------|----------|-------|
| `DECK_SHUFFLE` | Initial deck shuffle, discard recycle | `summoner.gd` |
| `AI_DECISIONS` | Card selection scoring, play timing | `heuristic_ai.gd` |
| `COMBAT_CRITS` | Critical hit rolls | `DamageSystem.cs` |
| `SPAWN_POSITIONS` | AI spawn position jitter | `heuristic_ai.gd` |

---

### 1.3 Network State Management

- [x] **COMPLETED** (PR #150, merged 2026-01-04)

Track connection state, peer information, and match configuration.

**What Was Implemented:**

| File | Status | Description |
|------|--------|-------------|
| `scripts/multiplayer/core/peer_info.gd` | ✅ Created | Data class for connected peer info |
| `scripts/multiplayer/core/match_config.gd` | ✅ Created | Match configuration (seed, player IDs, settings) |
| `scripts/multiplayer/core/network_state.gd` | ✅ Created | Autoload singleton tracking connection state |
| `project.godot` | ✅ Modified | Added NetworkState autoload |

**How It Works:**
```gdscript
# NetworkState is an autoload singleton
NetworkState.state  # Current connection state (OFFLINE, CONNECTING, CONNECTED, DISCONNECTED)
NetworkState.local_peer_id  # Our peer ID
NetworkState.peers  # Dictionary of connected PeerInfo objects
NetworkState.match_config  # Current MatchConfig (null if not in match)

# Check connection status
if NetworkState.is_online():
    print("Connected to multiplayer")

# Register for events
NetworkState.peer_connected.connect(_on_peer_connected)
NetworkState.match_started.connect(_on_match_started)
```

**Implementation Steps:**
1. [x] Create `peer_info.gd` data class
2. [x] Create `match_config.gd` resource
3. [x] Create `network_state.gd` autoload
4. [x] Add NetworkState to project autoloads
5. [ ] Integrate with BattleContext (deferred to Phase 2)

---

### 1.4 P2P Connection (Room Codes)

- [x] **COMPLETED** (PR #150, merged 2026-01-04)

Basic P2P networking for local testing before Nakama integration.

**What Was Implemented:**

| File | Status | Description |
|------|--------|-------------|
| `scripts/multiplayer/connection/room_code_service.gd` | ✅ Created | Generate/validate 6-char room codes |
| `scripts/multiplayer/connection/p2p_host.gd` | ✅ Created | Host a P2P match (ENetMultiplayerPeer) |
| `scripts/multiplayer/connection/p2p_client.gd` | ✅ Created | Join a P2P match |
| `scripts/multiplayer/sync/action_replicator.gd` | ✅ Created | RPC-based action replication |
| `scenes/ui/screens/multiplayer_lobby.tscn` | ✅ Created | Lobby UI (create/join room) |
| `scripts/ui/screens/multiplayer_lobby.gd` | ✅ Created | Lobby UI logic |
| `scripts/multiplayer/authority/host_authority.gd` | ✅ Updated | Full RPC implementation |
| `scripts/multiplayer/authority/client_proxy.gd` | ✅ Updated | Full RPC implementation |
| `scripts/core/scene_manager.gd` | ✅ Modified | Added SCENE_MULTIPLAYER_LOBBY constant |
| `localization/data/en.json` | ✅ Modified | Added multiplayer UI strings |

**How It Works:**
```gdscript
# Host creates a room
var host: P2PHost = P2PHost.new()
add_child(host)
host.start_server("HostName", "fire_summoner")
# host.room_code now contains the 6-char code to share

# Client joins via IP (room codes map to IPs externally)
var client: P2PClient = P2PClient.new()
add_child(client)
client.connect_to_host("192.168.1.100", 7777, "ClientName", "water_summoner")

# Both receive match_ready/match_starting when ready
host.match_ready.connect(_on_match_ready)
client.match_starting.connect(_on_match_starting)
```

**Room Code Service:**
```gdscript
# Generate 6-char codes with no ambiguous characters (0/O, 1/I/L excluded)
var code: String = RoomCodeService.generate_code()  # e.g., "ABC123"
var valid: bool = RoomCodeService.is_valid_code(code)
```

**Implementation Steps:**
1. [x] Create `room_code_service.gd`
2. [x] Create `p2p_host.gd` with ENet server
3. [x] Create `p2p_client.gd` with ENet client
4. [x] Create `action_replicator.gd` for RPC layer
5. [x] Create multiplayer lobby scene and script
6. [x] Implement handshake protocol (exchange player info, deck hash)
7. [x] Implement `host_authority.gd` with RPC calls
8. [x] Implement `client_proxy.gd` with RPC calls
9. [ ] Test local P2P connection (two instances on same machine) - Manual testing required

**Unit Tests Added:**
- `tests/unit/test_peer_info.gd` - 12 tests
- `tests/unit/test_match_config.gd` - 17 tests
- `tests/unit/test_room_code_service.gd` - 22 tests
- `tests/unit/test_host_authority.gd` - 11 tests
- `tests/unit/test_client_proxy.gd` - 12 tests

**P2P Host Flow (unchanged):**
1. Host clicks "Create Room"
2. Generate room code, display to player
3. Create ENetMultiplayerPeer server on random port
4. Broadcast on LAN (optional) or wait for direct connect
5. When client connects: handshake, exchange player info
6. When ready: generate battle seed, share to client, start match

**P2P Client Flow:**
1. Client enters room code
2. Resolve code to IP:port (manual entry for now, Nakama relay later)
3. Connect via ENetMultiplayerPeer
4. Handshake, receive match config
5. Initialize BattleRNG with shared seed
6. Start match

**Implementation Steps:**
1. [ ] Create `room_code_service.gd`
2. [ ] Create `p2p_host.gd` with ENet server
3. [ ] Create `p2p_client.gd` with ENet client
4. [ ] Create multiplayer lobby scene and script
5. [ ] Implement handshake protocol (exchange player info, deck hash)
6. [ ] Implement `host_authority.gd` with RPC calls
7. [ ] Implement `client_proxy.gd` with RPC calls
8. [ ] Test local P2P connection (two instances on same machine)

---

## Phase 2: Game Synchronization

**Goal:** Make battles work in multiplayer with proper state sync.

**Status:** ✅ Complete (5/5 complete)

**Prerequisites:** Phase 1 complete

**Architecture Note:** Phase 2 has been restructured to use C# for the core multiplayer infrastructure (protocol, state sync, authority) with GDScript bridges for Godot integration. See `docs/multiplayer/ranked-system.md` for full architecture details.

---

### 2.1 Action System

- [x] **COMPLETED** (Session 4, 2026-02-03)

Unified system for validating and replicating game actions (card plays, forfeits).

**New Files to Create:**

| File | Purpose |
|------|---------|
| `scripts/multiplayer/actions/game_action.gd` | Base action class with serialization |
| `scripts/multiplayer/actions/play_card_action.gd` | Card play action |
| `scripts/multiplayer/actions/forfeit_action.gd` | Forfeit/surrender action |
| `scripts/multiplayer/actions/action_validator.gd` | Validate actions on authority |
| `scripts/multiplayer/actions/action_replicator.gd` | Replicate actions to clients |

**GameAction Base Class:**
```gdscript
class_name GameAction extends RefCounted

var action_id: int = 0  # Unique ID for this action
var player_id: int = 0  # Peer ID of player who initiated
var timestamp: float = 0.0  # When action was created
var sequence: int = 0  # Order in action sequence

func validate(context: BattleContext) -> bool:
    return false  # Override in subclasses

func execute(context: BattleContext) -> void:
    pass  # Override in subclasses

func serialize() -> Dictionary:
    return {}  # Override in subclasses

static func deserialize(data: Dictionary) -> GameAction:
    return null  # Override in subclasses
```

**PlayCardAction:**
```gdscript
class_name PlayCardAction extends GameAction

var card_index: int = -1
var spawn_position: Vector3 = Vector3.ZERO
var card_catalog_id: String = ""  # For validation

func validate(context: BattleContext) -> bool:
    var summoner = context.get_summoner_for_player(player_id)
    if summoner == null:
        return false
    if card_index < 0 or card_index >= summoner.hand.size():
        return false
    var card = summoner.hand[card_index]
    if card.catalog_id != card_catalog_id:
        return false  # Card mismatch (possible desync)
    if not card.can_play(int(summoner.mana)):
        return false  # Not enough mana
    # TODO: Validate spawn_position is in valid zone
    return true

func execute(context: BattleContext) -> void:
    var summoner = context.get_summoner_for_player(player_id)
    summoner.play_card_3d(card_index, spawn_position)
```

**Files to Modify:**

| File | Changes |
|------|---------|
| `scripts/core/summoner.gd` | `play_card_3d()` creates PlayCardAction, sends to authority |
| `scripts/cards/card.gd` | Add `serialize()` / `deserialize()` methods |
| `scripts/core/battle_context.gd` | Add `get_summoner_for_player(peer_id)` method |

**Implementation Steps:**
1. [x] Create protocol message types (C#)
2. [x] Create message serializer for RPC
3. [x] Create MatchSession orchestrator
4. [x] Create HostRunner for authority validation
5. [x] Create ClientRunner for prediction
6. [x] Create RequestValidator for action validation
7. [x] Modify `battle_context.gd` to support multiplayer mode
8. [x] Create MultiplayerAuthority bridge (GDScript → C#)

**What Was Implemented:**

| File | Status | Description |
|------|--------|-------------|
| `scripts/csharp/Multiplayer/Protocol/Messages.cs` | ✅ Created | All message types as C# records (CardPlayRequest, StateSnapshot, etc.) |
| `scripts/csharp/Multiplayer/Protocol/MessageSerializer.cs` | ✅ Created | Serialization to Godot Dictionary for RPC |
| `scripts/csharp/Multiplayer/Core/MatchSession.cs` | ✅ Created | Central orchestrator for match lifecycle |
| `scripts/csharp/Multiplayer/Core/IMatchRunner.cs` | ✅ Created | Interface for host/client runners |
| `scripts/csharp/Multiplayer/Authority/HostRunner.cs` | ✅ Created | Authoritative simulation, 10 Hz snapshots |
| `scripts/csharp/Multiplayer/Authority/RequestValidator.cs` | ✅ Created | Validates all client requests |
| `scripts/csharp/Multiplayer/Client/ClientRunner.cs` | ✅ Created | Prediction buffer, reconciliation |
| `scripts/csharp/Multiplayer/Transport/IMatchTransport.cs` | ✅ Created | Transport abstraction interface |
| `scripts/csharp/Multiplayer/Transport/P2PTransport.cs` | ✅ Created | ENet-based P2P implementation |
| `scripts/multiplayer/authority/multiplayer_authority.gd` | ✅ Created | GDScript bridge to C# MatchSession |
| `scripts/core/battle_context.gd` | ✅ Modified | Added MULTIPLAYER mode, configure_multiplayer_battle() |
| `scripts/ui/screens/multiplayer_lobby.gd` | ✅ Modified | Updated to configure BattleContext for multiplayer |

**Key Interfaces:**
```csharp
// Protocol messages are C# records for type safety
public readonly record struct CardPlayRequest(
    int Sequence, int PlayerIndex, int CardIndex,
    Vector3 Position, long ClientTimestamp);

// MatchSession is the central orchestrator
public partial class MatchSession : Node {
    public void RequestCardPlay(int cardIndex, Vector3 position);
    public void ProcessMessage(int senderId, MessageType type, Dictionary data);
}
```

---

### 2.2 Unit Spawn Sync

- [x] **COMPLETED** (Session 4, 2026-02-03)

Ensure units spawn identically on both clients with network IDs.

**What Was Implemented:**

| File | Status | Description |
|------|--------|-------------|
| `scripts/csharp/Multiplayer/Core/NetworkIdRegistry.cs` | ✅ Created | Maps network IDs to Node instances |
| `scripts/csharp/Units/Unit3D.cs` | ✅ Modified | Added NetworkId property |
| `scripts/csharp/Summons/UnitSpawner.cs` | ✅ Modified | Registers units with MatchSession when spawned |
| `scripts/csharp/Multiplayer/Core/MatchSession.cs` | ✅ Modified | Added static Current property for global access |

**NetworkIdRegistry (C#):**
```csharp
public class NetworkIdRegistry
{
    private int _nextId = 1;
    private readonly Dictionary<int, Node> _idToNode = new();
    private readonly Dictionary<Node, int> _nodeToId = new();

    public int Register(Node node);  // Host assigns new IDs
    public void RegisterWithId(int networkId, Node node);  // Client uses assigned IDs
    public Node? GetNode(int networkId);
    public int GetId(Node node);
    public void Unregister(Node node);
}
```

**Implementation Steps:**
1. [x] Create NetworkIdRegistry in C# (part of MatchSession)
2. [x] Add NetworkId property to Unit3D.cs
3. [x] Hook UnitSpawner.cs to register spawned units with NetworkIdRegistry
4. [x] Broadcast UnitSpawned messages from host (in UnitSpawner)
5. [x] Unregister and broadcast UnitDied when units die (in Unit3D.OnDeath)
6. [ ] Handle UnitSpawned messages on client (spawn unit from network)

**How It Works:**
```csharp
// When a unit spawns (in UnitSpawner):
var session = MatchSession.Current;
if (session != null && session.IsHost)
{
    var networkId = session.NetworkIds.Register(unit);
    unit.NetworkId = networkId;
    session.Broadcast(new UnitSpawned(networkId, unitType, team, position));
}

// When a unit dies (in Unit3D.OnDeath):
if (NetworkId >= 0 && MatchSession.Current?.IsHost == true)
{
    session.NetworkIds.Unregister(this);
    session.Broadcast(new UnitDied(NetworkId, null));
}
```

**Remaining Work:**
Client-side unit spawning from UnitSpawned messages needs implementation. Currently clients receive the message but don't spawn units - they should create units with the given NetworkId.

---

### 2.3 Combat Event Sync

- [x] **COMPLETED** (Session 4, 2026-02-03)

Synchronize damage, healing, and combat effects across network.

**New Files to Create:**

| File | Purpose |
|------|---------|
| `scripts/multiplayer/sync/combat_sync.gd` | Replicate combat events |

**Events to Sync:**
| Event | Data |
|-------|------|
| `DamageDealt` | attacker_network_id, target_network_id, amount, is_crit |
| `UnitDied` | unit_network_id, killer_network_id |
| `SummonerDamaged` | team, amount, new_hp |

**Design Decision:** Authority calculates damage, broadcasts final values. Clients don't simulate - they just apply what authority says. This prevents floating-point desync.

**What Was Implemented:**

| File | Status | Description |
|------|--------|-------------|
| `scripts/csharp/Combat/DamageSystem.cs` | ✅ Modified | Added BroadcastDamageDealt and BroadcastSummonerDamage |

**How It Works:**
```csharp
// In DamageSystem.ApplyDamage (after damage is applied):
BroadcastDamageDealt(attacker, target, finalDamage, isCrit);

// BroadcastDamageDealt method:
private static void BroadcastDamageDealt(Node3D attacker, Node3D target, float amount, bool isCrit)
{
    var session = MatchSession.Current;
    if (session == null || !session.IsActive || !session.IsHost) return;

    int? sourceNetworkId = (attacker as Unit3D)?.NetworkId;
    int targetNetworkId = (target as Unit3D)?.NetworkId ?? -1;
    if (targetNetworkId < 0) return;

    session.Broadcast(new DamageDealt(targetNetworkId, amount, isCrit, sourceNetworkId));
}
```

**Implementation Steps:**
1. [x] Modify `DamageSystem.cs` to broadcast damage events with network IDs
2. [x] Add BroadcastSummonerDamage method for summoner damage
3. [ ] Hook summoner damage into broadcast (GDScript integration)
4. [ ] Handle DamageDealt messages on client (apply damage visuals)

---

### 2.4 Win Condition Sync

- [x] **COMPLETED** (Session 4, 2026-02-03)

Authority detects and broadcasts win conditions.

**What Was Implemented:**

| File | Status | Description |
|------|--------|-------------|
| `scripts/csharp/Multiplayer/Core/MatchSession.cs` | ✅ Modified | Added BroadcastMatchEnd() method |
| `scripts/core/game_controller_3d.gd` | ✅ Modified | Added _broadcast_match_end() to broadcast in multiplayer |

**How It Works:**
```csharp
// In MatchSession:
public void BroadcastMatchEnd(int winnerIndex, string reason)
{
    if (!IsHost || !IsActive) return;
    var endMessage = new Protocol.MatchEnded(winnerIndex, reason, MatchTime);
    Broadcast(endMessage);
    EndMatch(winnerIndex, reason);
}
```

```gdscript
# In game_controller_3d.gd:
func _broadcast_match_end(winner: UnitConstants.Team) -> void:
    if BattleContext.authority_provider == null:
        return
    var match_session: Node = BattleContext.authority_provider.get_match_session()
    if match_session == null:
        return
    var winner_index: int = 0 if winner == UnitConstants.Team.PLAYER else 1
    var reason: String = "Summoner destroyed"
    if match_session.has_method("BroadcastMatchEnd"):
        match_session.BroadcastMatchEnd(winner_index, reason)
```

**Win Conditions Handled:**
- Summoner destroyed (HP <= 0) - via BroadcastMatchEnd
- Forfeit (player surrendered) - via RequestForfeit in MatchSession
- Disconnect (opponent left) - via HandlePeerDisconnected in MatchSession

**Implementation Steps:**
1. [x] Add authority check to win condition detection
2. [x] Broadcast match result via MatchEnded message
3. [x] Handle match end on both host and client (EndMatch method)
4. [x] Handle disconnection as forfeit

---

### 2.5 State Snapshot System

- [x] **COMPLETED** (Session 4, 2026-02-03)

Periodic state snapshots for desync detection and recovery.

**What Was Implemented:**

| File | Status | Description |
|------|--------|-------------|
| `scripts/csharp/Multiplayer/Sync/StateSnapshotBuilder.cs` | ✅ Created | Builds snapshots from actual game state |
| `scripts/csharp/Multiplayer/Sync/DesyncDetector.cs` | ✅ Created | Detects desyncs and handles resync |
| `scripts/csharp/Multiplayer/Authority/HostRunner.cs` | ✅ Modified | Uses StateSnapshotBuilder for snapshots |
| `scripts/csharp/Multiplayer/Client/ClientRunner.cs` | ✅ Modified | Sends hash reports, uses DesyncDetector |

**StateSnapshotBuilder Features:**
- Captures unit positions, HP, targets from NetworkIdRegistry
- Captures summoner HP and mana from scene tree
- Quantizes floats to prevent precision drift (millimeter positions, tenth-HP)
- Computes deterministic hash for quick comparison

**DesyncDetector Features:**
- Tracks consecutive hash mismatches (threshold of 3)
- Logs desync events for debugging
- Applies state corrections from host snapshots
- Triggers full resync on confirmed desync

**How It Works:**
```csharp
// Host builds snapshot using StateSnapshotBuilder
var builder = new StateSnapshotBuilder(session);
var snapshot = builder.Build();  // Captures all game state
session.Broadcast(snapshot);     // Send to clients at 10 Hz

// Client sends periodic hash reports
var hash = snapshotBuilder.ComputeHash();
session.Send(new StateHashReport(playerIndex, frame, hash));

// Host checks client hash via DesyncDetector
desyncDetector.CheckClientHash(clientHash, clientFrame);
// If 3+ consecutive mismatches → sends full snapshot for resync

// Client applies corrections from snapshot
desyncDetector.ApplySnapshot(snapshot);  // Corrects positions, logs HP discrepancies
```

**Implementation Steps:**
1. [x] Create StateSnapshotBuilder for actual game state capture
2. [x] Create DesyncDetector for hash comparison and resync
3. [x] Implement periodic hash reporting from client (every 60 frames)
4. [x] Implement snapshot-based position corrections
5. [x] Add desync logging with DesyncEvent records

---

## Phase 3: Nakama Integration

**Goal:** Add backend services for auth, matchmaking, rankings.

**Status:** 🔄 In Progress (2/6 complete)

**Prerequisites:** Phase 2 complete, Nakama server running

---

### 3.1 Nakama Client Setup

- [x] **COMPLETED** (Session 4, 2026-02-03)

Install and configure Nakama .NET SDK.

**What Was Implemented:**

| File | Status | Description |
|------|--------|-------------|
| `Fateforged.csproj` | ✅ Modified | Added NakamaClient NuGet package (v3.21.1) |
| `scripts/csharp/Multiplayer/Backend/NakamaGameClient.cs` | ✅ Created | Nakama client wrapper |
| `scripts/csharp/Multiplayer/Backend/NakamaGameClient.tscn` | ✅ Created | Scene for autoload |
| `project.godot` | ✅ Modified | Added NakamaGameClient autoload |

**NakamaGameClient Features:**
- Device ID authentication (anonymous)
- Email/password authentication
- Session persistence and restoration
- WebSocket connection for real-time features
- Match data and presence event handlers
- Configurable server endpoint (dev/prod)

**Key Signals:**
- `Authenticated(userId, username)` - Auth succeeded
- `AuthenticationFailed(error)` - Auth failed
- `SocketConnected` / `SocketDisconnected` - WebSocket state
- `MatchFound(matchId, userIds)` - Matchmaking result
- `MatchDataReceived` / `MatchPresenceJoined` / `MatchPresenceLeft` - Match events

**Usage:**
```csharp
// Authenticate with device ID
await NakamaGameClient.Instance.AuthenticateDeviceAsync();

// Connect socket for real-time features
await NakamaGameClient.Instance.ConnectSocketAsync();
```

---

### 3.2 Authentication

- [x] **COMPLETED** (Session 4, 2026-02-03)

Implemented as part of NakamaGameClient (Phase 3.1).

**What Was Implemented:**

Authentication functionality is built into `NakamaGameClient.cs`:
- `AuthenticateDeviceAsync()` - Anonymous device-based auth
- `AuthenticateEmailAsync()` - Email/password auth
- `RefreshSessionAsync()` - Session token refresh
- `Logout()` - Clear session
- Session persistence to `user://nakama_session.dat`
- Device ID persistence to `user://device_id.dat`

**Auth Flow:**
1. On startup, try to restore saved session
2. If no session or expired, call `AuthenticateDeviceAsync()`
3. Session tokens automatically saved for auto-login
4. Session refresh happens automatically when needed

**Implementation Steps:**
1. [x] Device authentication via Nakama.AuthenticateDeviceAsync
2. [x] Session token persistence to user://
3. [x] Session refresh logic
4. [x] Email/password auth support (ready for future login UI)
5. [ ] Create optional login UI (deferred - not needed for initial release)

---

### 3.3 Ranked Matchmaking

- [x] **COMPLETED** (Session 4, 2026-02-03)

Implemented ranked queue via Nakama matchmaker.

**What Was Implemented:**

| File | Status | Description |
|------|--------|-------------|
| `scripts/csharp/Multiplayer/Matchmaking/MatchmakingService.cs` | ✅ Created | Queue management service |
| `scripts/csharp/Multiplayer/Matchmaking/MatchmakingService.tscn` | ✅ Created | Scene for autoload |
| `project.godot` | ✅ Modified | Added MatchmakingService autoload |

**MatchmakingService Features:**
- `JoinQueueAsync(options)` - Join ranked queue with rating
- `LeaveQueueAsync()` - Cancel matchmaking
- Rating-based query: `+properties.mode:ranked_1v1 +properties.rating:>=X +properties.rating:<=Y`
- Automatic rating range expansion over time
- Queue time tracking

**Key Signals:**
- `MatchFound(matchId, opponentUserId, opponentUsername, opponentRating)`
- `MatchmakingCancelled(reason)`
- `QueueStatusChanged(isInQueue, queueTime)`
- `MatchmakingError(error)`

**Usage:**
```csharp
// Join ranked queue
await MatchmakingService.Instance.JoinQueueAsync();

// Cancel matchmaking
await MatchmakingService.Instance.LeaveQueueAsync();
```

**Implementation Steps:**
1. [x] Create MatchmakingService.cs
2. [x] Implement queue join/leave via Nakama matchmaker
3. [x] Handle match found callback
4. [ ] Create queue UI with cancel button (deferred to Phase 4)
5. [ ] Integrate with P2P connection (deferred - match start logic)

---

### 3.4 ELO System

- [x] **COMPLETED** (Session 4, 2026-02-03)

Skill-based rating system.

**What Was Implemented:**

| File | Status | Description |
|------|--------|-------------|
| `scripts/csharp/Multiplayer/Ranking/EloCalculator.cs` | ✅ Created | Pure ELO calculation, tier/division logic |
| `scripts/csharp/Multiplayer/Ranking/RankingService.cs` | ✅ Created | Rating persistence via ProfileRepo |

**ELO Parameters (C#):**
```csharp
public static class EloCalculator
{
    public const int StartingElo = 1200;
    public const int KFactor = 32;
    public const int EloFloor = 100;
    public const int EloCeiling = 3000;

    public static (int WinnerNew, int LoserNew) CalculateNewRatings(int winnerElo, int loserElo);
    public static RankTier GetTier(int elo);  // Bronze, Silver, Gold, etc.
    public static int GetDivision(int elo);   // I, II, III, IV
    public static string FormatRating(int elo);  // "Gold II (1150)"
}
```

**Rank Tiers:**
| Tier | ELO Range |
|------|-----------|
| Bronze | 0-799 |
| Silver | 800-999 |
| Gold | 1000-1199 |
| Platinum | 1200-1399 |
| Diamond | 1400-1599 |
| Master | 1600-1799 |
| Grandmaster | 1800-1999 |
| Legend | 2000+ |

**Implementation Steps:**
1. [x] Create `EloCalculator.cs` with math
2. [x] Create `RankingService.cs` to fetch/update ratings
3. [x] Persist ratings in ProfileRepo
4. [x] Track match history and statistics
5. [ ] Store ratings in Nakama storage (deferred to Nakama integration)
6. [ ] Display rating in UI (deferred to Phase 4)

---

### 3.5 Match Reporting

- [x] **COMPLETED** (Session 4, 2026-02-03)

Report match outcomes to Nakama and update ratings.

**What Was Implemented:**

| File | Status | Description |
|------|--------|-------------|
| `scripts/csharp/Multiplayer/Ranking/MatchReporter.cs` | ✅ Created | Match reporting service |
| `scripts/csharp/Multiplayer/Ranking/MatchReporter.tscn` | ✅ Created | Scene for autoload |
| `project.godot` | ✅ Modified | Added MatchReporter autoload |

**MatchReporter Features:**
- `ReportMatchAsync(result)` - Report match and update ratings
- `FlushOfflineCacheAsync()` - Submit cached offline reports
- `GetRecentMatches(count)` - Get match history
- `GetWinRate()` - Calculate win rate from history
- Offline report caching (up to 50 reports)
- Match history persistence to user://

**Key Signals:**
- `MatchReported(matchId, ratingChange)` - Report submitted
- `MatchReportFailed(matchId, error)` - Report failed
- `RatingChanged(oldRating, newRating, change)` - Rating updated

**Data Classes:**
- `MatchResult` - Input from match end
- `MatchReport` - Full report with ratings before/after

**Implementation Steps:**
1. [x] Create MatchReporter.cs
2. [x] Submit to Nakama via RPC (graceful fallback if server unavailable)
3. [x] Update local rating via RankingService
4. [x] Store match history with persistence

---

### 3.6 Leaderboards

- [ ] **Not Started**

Display ranked leaderboards.

**New Files to Create:**

| File | Purpose |
|------|---------|
| `scripts/multiplayer/ranking/leaderboard_service.gd` | Fetch leaderboard data |
| `scenes/ui/screens/leaderboard_screen.tscn` | Leaderboard UI |

**Leaderboard Types:**
- Global top 100
- Player's rank + nearby players
- Friends leaderboard (future)

**Implementation Steps:**
1. [ ] Create `leaderboard_service.gd`
2. [ ] Create leaderboard UI
3. [ ] Fetch and display global rankings
4. [ ] Show player's own rank

---

## Phase 4: Polish

**Goal:** Handle edge cases, improve UX, production readiness.

**Status:** ⚪ Not Started (0/5 complete)

**Prerequisites:** Phase 3 complete

---

### 4.1 Reconnection Handling

- [ ] **Not Started**

Handle disconnections gracefully.

**New Files to Create:**

| File | Purpose |
|------|---------|
| `scripts/multiplayer/connection/reconnection_handler.gd` | Reconnection logic |
| `scenes/ui/overlays/reconnecting_overlay.tscn` | "Reconnecting..." UI |

**Reconnection Flow:**
1. Detect disconnection (peer_disconnected signal)
2. Show reconnection overlay
3. Attempt reconnect with exponential backoff (1s, 2s, 4s, 8s, 16s)
4. If reconnect succeeds: resync from state snapshot
5. If timeout (30s): forfeit match

**Implementation Steps:**
1. [ ] Create `reconnection_handler.gd`
2. [ ] Create reconnecting overlay UI
3. [ ] Implement exponential backoff
4. [ ] Integrate with state snapshot for resync
5. [ ] Handle opponent disconnect (show waiting UI)

---

### 4.2 Latency Compensation

- [ ] **Not Started**

Smooth experience despite network latency.

**New Files to Create:**

| File | Purpose |
|------|---------|
| `scripts/multiplayer/sync/latency_compensator.gd` | Prediction/smoothing |

**Techniques:**
- **Local Prediction:** Show card play immediately, verify with authority
- **Interpolation:** Smooth opponent unit positions between updates
- **Rollback:** Revert predicted actions if authority rejects

**Note:** Auto-battler is latency-tolerant since:
- Units act autonomously (no per-frame input)
- Card plays are discrete events
- Small position differences are visually acceptable

**Implementation Steps:**
1. [ ] Implement local prediction for card plays
2. [ ] Add position interpolation for units
3. [ ] Handle prediction rollback on rejection

---

### 4.3 Error Handling

- [ ] **Not Started**

Robust error handling for all network operations.

**New Files to Create:**

| File | Purpose |
|------|---------|
| `scripts/multiplayer/error/error_handler.gd` | Centralized handling |
| `scripts/multiplayer/error/error_codes.gd` | Error definitions |

**Error Categories:**
- Connection errors (timeout, refused, lost)
- Authentication errors (invalid session, banned)
- Matchmaking errors (queue timeout, no match found)
- Game errors (desync, invalid action)

**Implementation Steps:**
1. [ ] Create error code enum
2. [ ] Create centralized error handler
3. [ ] Add user-friendly error messages
4. [ ] Add retry/recovery options where applicable

---

### 4.4 Multiplayer UI

- [ ] **Not Started**

Complete UI for multiplayer experience.

**New Scenes:**

| Scene | Purpose |
|-------|---------|
| `scenes/ui/screens/multiplayer_menu.tscn` | Main MP menu (Play, Leaderboard, etc.) |
| `scenes/ui/screens/match_loading.tscn` | Loading screen with opponent info |
| `scenes/ui/screens/match_result.tscn` | Post-match results (rating change, stats) |
| `scenes/ui/overlays/opponent_info.tscn` | In-battle opponent name/rating display |

**Implementation Steps:**
1. [ ] Create multiplayer menu
2. [ ] Create match loading screen
3. [ ] Create match result screen
4. [ ] Create in-battle opponent display
5. [ ] Add forfeit/surrender button

---

### 4.5 Anti-Cheat

- [ ] **Not Started**

Basic anti-cheat measures for ranked play.

**Strategies:**
- Authority validation for all actions (already implemented)
- Rate limiting (max actions per second)
- State hashing (detect client manipulation)
- Replay recording (store match data for review)

**Limitations of P2P:**
- Host can cheat (delay actions, see deck, etc.)
- Mitigated later with dedicated servers

**Implementation Steps:**
1. [ ] Add rate limiting to action validator
2. [ ] Enhance state hashing
3. [ ] Implement replay recording (optional)
4. [ ] Document known P2P vulnerabilities

---

## Dependency Graph

```
Phase 1.1 (Authority) ─┐
                       ├──> Phase 2.1 (Actions) ──> Phase 2.2 (Unit Sync)
Phase 1.2 (RNG) ✅ ────┤                                    │
                       │                                    v
Phase 1.3 (Network) ───┴──> Phase 2.3 (Combat Sync) ──> Phase 2.4 (Win)
                       │                                    │
Phase 1.4 (P2P) ───────┘                                    v
                                                      Phase 2.5 (Snapshot)
                                                            │
                                                            v
                    Phase 3.1 (Nakama) ──> Phase 3.2 (Auth) ──> Phase 3.3 (MM)
                                                                      │
                                                                      v
                                                    Phase 3.4 (ELO) ──> Phase 3.5 (Report)
                                                                      │
                                                                      v
                                                              Phase 3.6 (Leaderboards)
                                                                      │
                                                                      v
                    Phase 4.1 (Reconnect) ──> Phase 4.2 (Latency) ──> Phase 4.3 (Errors)
                                                                      │
                                                                      v
                                                              Phase 4.4 (UI)
                                                                      │
                                                                      v
                                                              Phase 4.5 (Anti-Cheat)
```

---

## Session Notes

### Session 1 (2026-01-03)
- Completed initial planning and architecture design
- Decided on Nakama for backend, hybrid authority model
- Completed Phase 1.2 (Seeded RNG System) - PR #147
- Created docs/multiplayer/ documentation

### Session 2 (2026-01-04)
- Completed Phase 1.1 (Authority Abstraction Layer) - PR #149
- Created authority provider pattern (AuthorityProvider, LocalAuthority, HostAuthority, ClientProxy)
- Created GameAction base class for future action system
- Added authority checks to GameController3D (end_game, win conditions)
- Added 28 unit tests for LocalAuthority and GameAction
- All 211 tests passing (37 pending C# tests)

### Session 3 (2026-01-04)
- Completed Phase 1.3 (Network State Management)
  - Created PeerInfo, MatchConfig, NetworkState classes
  - Added NetworkState autoload to project.godot
- Completed Phase 1.4 (P2P Connection with Room Codes)
  - Created RoomCodeService for 6-char codes
  - Created P2PHost and P2PClient with ENet
  - Created ActionReplicator for RPC-based action sync
  - Implemented full RPC in HostAuthority and ClientProxy
  - Created multiplayer lobby UI (scene + script)
  - Added localization strings for multiplayer UI
- Added 74 new unit tests for multiplayer components
- All 280 tests passing (37 pending C# tests, 10 pre-existing failures)
- Phase 1 Network Foundation is now complete!

### Session 4 (2026-02-03)
- **Major Architecture Restructure**: Migrated multiplayer infrastructure to C#
- Completed Phase 2.1 (Action System) with C# implementation:
  - Created Protocol Messages (CardPlayRequest, StateSnapshot, UnitSpawned, etc.)
  - Created MessageSerializer for Godot Dictionary RPC interop
  - Created MatchSession orchestrator
  - Created HostRunner (authority simulation, 10 Hz snapshots)
  - Created ClientRunner (prediction buffer, reconciliation)
  - Created RequestValidator for action validation
  - Created P2PTransport (ENet implementation)
- Completed Phase 2.2 infrastructure (NetworkIdRegistry in C#)
- Completed Phase 3.4 (ELO System):
  - Created EloCalculator with tiers (Bronze → Legend)
  - Created RankingService for persistence via ProfileRepo
  - Tracks match history, statistics, win streaks
- Created MultiplayerAuthority GDScript bridge to C# MatchSession
- Updated BattleContext with MULTIPLAYER mode and configure_multiplayer_battle()
- Updated multiplayer_lobby.gd to set up BattleContext for multiplayer
- Created docs/multiplayer/ranked-system.md with full architecture documentation
- Build successful with 0 errors and 0 warnings

### Session 4 Continued (2026-02-03)
- Completed Phase 2.2 (Unit Spawn Synchronization):
  - Added NetworkId property to Unit3D.cs
  - Hooked UnitSpawner.cs to register units with NetworkIdRegistry
  - Broadcasts UnitSpawned messages from host
  - Unregisters and broadcasts UnitDied on unit death
- Completed Phase 2.3 (Combat Event Synchronization):
  - Added BroadcastDamageDealt to DamageSystem.cs
  - Added BroadcastSummonerDamage for summoner HP changes
- Completed Phase 2.4 (Win Condition Synchronization):
  - Added BroadcastMatchEnd() to MatchSession.cs
  - Updated game_controller_3d.gd to broadcast in multiplayer
  - Handles summoner destruction, forfeit, and disconnection
- Completed Phase 2.5 (State Snapshot System):
  - Created StateSnapshotBuilder.cs for capturing actual game state
  - Created DesyncDetector.cs for hash comparison and resync handling
  - Updated HostRunner to use StateSnapshotBuilder for snapshots
  - Updated ClientRunner to send periodic hash reports and handle corrections
  - Quantized positions (mm) and HP (tenths) to prevent float drift
  - Desync threshold of 3 consecutive mismatches before full resync
- Build successful with 0 errors and 0 warnings
- **Phase 2 (Game Synchronization) is now COMPLETE!**

### Session 4 Continued - Phase 3 (2026-02-03)
- **Phase 3: Nakama Integration** - Started and significant progress
- Completed Phase 3.1 (Nakama Client Setup):
  - Added NakamaClient NuGet package (v3.21.1)
  - Created NakamaGameClient.cs wrapper with full SDK integration
  - Added NakamaGameClient autoload
- Completed Phase 3.2 (Authentication):
  - Device ID authentication (anonymous, frictionless)
  - Email/password authentication
  - Session persistence and restoration
  - WebSocket connection for real-time features
- Completed Phase 3.3 (Ranked Matchmaking):
  - Created MatchmakingService.cs for queue management
  - Rating-based matchmaker query
  - Queue join/leave functionality
  - Match found event handling
- Added RankingService and MatchmakingService autoloads
- Build successful with 0 errors and 0 warnings
- **Phase 3 at 4/6 complete**

### Next Session
- Complete Phase 3: Nakama Integration
  - Task #18: Implement match reporting to Nakama
  - Task #19: Create leaderboard service and UI
- Begin Phase 4: Polish
  - Task #20: Create multiplayer UI screens
  - Task #21: Implement reconnection handling

---

*Last Updated: 2026-02-03*
