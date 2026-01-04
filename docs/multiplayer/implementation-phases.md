# Multiplayer Implementation Phases

Detailed breakdown of the multiplayer implementation roadmap. This is a living document - check off items as they're completed.

---

## Progress Summary

| Phase | Status | Progress |
|-------|--------|----------|
| Phase 1: Network Foundation | ✅ Complete | 4/4 complete |
| Phase 2: Game Synchronization | ⚪ Not Started | 0/5 complete |
| Phase 3: Nakama Integration | ⚪ Not Started | 0/6 complete |
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

**Status:** ⚪ Not Started (0/5 complete)

**Prerequisites:** Phase 1 complete

---

### 2.1 Action System

- [ ] **Not Started**

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
1. [ ] Create `game_action.gd` base class
2. [ ] Create `play_card_action.gd` with validation
3. [ ] Create `forfeit_action.gd`
4. [ ] Create `action_validator.gd`
5. [ ] Create `action_replicator.gd` with RPC broadcasting
6. [ ] Modify `summoner.gd` to create actions instead of direct execution
7. [ ] Modify `battle_context.gd` to map peer IDs to summoners
8. [ ] Test action flow: client request → host validate → execute → broadcast

---

### 2.2 Unit Spawn Sync

- [ ] **Not Started**

Ensure units spawn identically on both clients with network IDs.

**New Files to Create:**

| File | Purpose |
|------|---------|
| `scripts/multiplayer/sync/unit_sync.gd` | Sync unit spawn/death events |
| `scripts/multiplayer/sync/network_id_registry.gd` | Map network IDs to node instances |

**NetworkIdRegistry:**
```gdscript
extends Node
class_name NetworkIdRegistryClass

var _next_id: int = 1
var _id_to_node: Dictionary = {}  # network_id -> Node
var _node_to_id: Dictionary = {}  # Node -> network_id

func register(node: Node) -> int:
    var id = _next_id
    _next_id += 1
    _id_to_node[id] = node
    _node_to_id[node] = id
    return id

func get_node(network_id: int) -> Node:
    return _id_to_node.get(network_id)

func get_id(node: Node) -> int:
    return _node_to_id.get(node, -1)

func unregister(node: Node) -> void:
    var id = _node_to_id.get(node, -1)
    if id != -1:
        _id_to_node.erase(id)
        _node_to_id.erase(node)
```

**Files to Modify:**

| File | Changes |
|------|---------|
| `scripts/cards/card.gd` | Register spawned units with NetworkIdRegistry |
| `scripts/csharp/Units/Unit3D.cs` | Add `NetworkId` property |

**Implementation Steps:**
1. [ ] Create `network_id_registry.gd` autoload
2. [ ] Create `unit_sync.gd` for spawn/death sync
3. [ ] Modify `card.gd` to assign network IDs on spawn
4. [ ] Modify `Unit3D.cs` to store NetworkId
5. [ ] Test that both clients see same units with same IDs

---

### 2.3 Combat Event Sync

- [ ] **Not Started**

Synchronize damage, healing, and combat effects across network.

**New Files to Create:**

| File | Purpose |
|------|---------|
| `scripts/multiplayer/sync/combat_sync.gd` | Replicate combat events |

**Events to Sync:**
| Event | Data |
|-------|------|
| `DamageDealt` | attacker_network_id, target_network_id, amount, is_crit, damage_type |
| `UnitDied` | unit_network_id, killer_network_id |
| `SummonerDamaged` | team, amount |
| `UnitHealed` | healer_network_id, target_network_id, amount |

**Design Decision:** Authority calculates damage, broadcasts final values. Clients don't simulate - they just apply what authority says. This prevents floating-point desync.

**Files to Modify:**

| File | Changes |
|------|---------|
| `scripts/csharp/Combat/DamageSystem.cs` | Emit network-friendly events with network IDs |
| `scripts/services/game_state_events.gd` | Add network event signals |

**Implementation Steps:**
1. [ ] Create `combat_sync.gd`
2. [ ] Modify `DamageSystem.cs` to emit events with network IDs
3. [ ] Implement RPC broadcasting for combat events
4. [ ] Test that damage/deaths sync correctly

---

### 2.4 Win Condition Sync

- [ ] **Not Started**

Authority detects and broadcasts win conditions.

**Files to Modify:**

| File | Changes |
|------|---------|
| `scripts/core/game_controller_3d.gd` | Only authority checks win conditions, broadcasts result |

**Win Conditions:**
- Summoner destroyed (HP <= 0)
- Forfeit (player surrendered)
- Disconnect (opponent left, timeout)
- Timeout (if time limit implemented)

**MatchResult:**
```gdscript
class_name MatchResult extends RefCounted

enum Reason { SUMMONER_DESTROYED, FORFEIT, DISCONNECT, TIMEOUT }

var winner_peer_id: int = 0
var loser_peer_id: int = 0
var reason: Reason = Reason.SUMMONER_DESTROYED
var duration_seconds: float = 0.0
var final_hp: Dictionary = {}  # peer_id -> hp
```

**Implementation Steps:**
1. [ ] Add authority check to win condition detection
2. [ ] Create MatchResult class
3. [ ] Broadcast match result to all peers
4. [ ] Handle match end on both host and client

---

### 2.5 State Snapshot System

- [ ] **Not Started**

Periodic state snapshots for desync detection and recovery.

**New Files to Create:**

| File | Purpose |
|------|---------|
| `scripts/multiplayer/sync/state_snapshot.gd` | Capture/compare game state |
| `scripts/multiplayer/sync/desync_detector.gd` | Detect and handle desync |

**Snapshot Contents:**
```gdscript
class_name StateSnapshot extends RefCounted

var frame_number: int = 0
var timestamp: float = 0.0
var rng_state_hash: int = 0

# Unit state (quantized positions to prevent float drift)
var unit_positions: Dictionary = {}  # network_id -> Vector3i (millimeter precision)
var unit_hp: Dictionary = {}  # network_id -> int (tenths of HP)

# Summoner state
var summoner_hp: Dictionary = {}  # team -> int
var summoner_mana: Dictionary = {}  # team -> int
var hand_hashes: Dictionary = {}  # team -> int (hash of card IDs)

func compute_hash() -> int:
    # Combine all state into single hash for quick comparison
    pass
```

**Desync Handling:**
1. Client periodically sends state hash to host
2. Host compares with authoritative hash
3. If mismatch: host sends full snapshot
4. Client resyncs from snapshot
5. Log desync event for debugging

**Implementation Steps:**
1. [ ] Create `state_snapshot.gd`
2. [ ] Create `desync_detector.gd`
3. [ ] Implement periodic snapshot comparison (every 60 frames?)
4. [ ] Implement snapshot-based resync
5. [ ] Add desync logging for debugging

---

## Phase 3: Nakama Integration

**Goal:** Add backend services for auth, matchmaking, rankings.

**Status:** ⚪ Not Started (0/6 complete)

**Prerequisites:** Phase 2 complete, Nakama server running

---

### 3.1 Nakama Client Setup

- [ ] **Not Started**

Install and configure Nakama Godot SDK.

**Tasks:**
1. [ ] Download Nakama Godot SDK from AssetLib or GitHub
2. [ ] Add to project addons folder
3. [ ] Create `scripts/multiplayer/nakama/nakama_client.gd` wrapper
4. [ ] Add NakamaClient autoload
5. [ ] Configure server endpoint (dev vs prod)
6. [ ] Test connection to local Nakama server

**NakamaClient Wrapper:**
```gdscript
extends Node
class_name NakamaClientClass

var client: NakamaClient
var session: NakamaSession
var socket: NakamaSocket

const SERVER_KEY = "defaultkey"  # Change for production
const HOST = "127.0.0.1"  # Change for production
const PORT = 7350
const SCHEME = "http"

signal authenticated(session: NakamaSession)
signal authentication_failed(error: String)
signal socket_connected()
signal socket_disconnected()
signal match_found(match_id: String, opponent: Dictionary)
```

---

### 3.2 Authentication

- [ ] **Not Started**

Implement user authentication.

**New Files to Create:**

| File | Purpose |
|------|---------|
| `scripts/multiplayer/auth/auth_service.gd` | Auth abstraction |
| `scripts/multiplayer/auth/device_auth.gd` | Device-based auth |
| `scenes/ui/screens/login_screen.tscn` | Optional login UI |

**Auth Flow:**
1. First launch → authenticate with device ID (anonymous)
2. Store session token locally for auto-login
3. Optional: link to email/social for account recovery

**Implementation Steps:**
1. [ ] Create `auth_service.gd` interface
2. [ ] Implement device authentication
3. [ ] Store session tokens securely
4. [ ] Add session refresh logic
5. [ ] Create optional login UI (email/password)

---

### 3.3 Ranked Matchmaking

- [ ] **Not Started**

Implement ranked queue via Nakama matchmaker.

**New Files to Create:**

| File | Purpose |
|------|---------|
| `scripts/multiplayer/matchmaking/matchmaking_service.gd` | Queue management |
| `scripts/multiplayer/matchmaking/match_ticket.gd` | Ticket data |
| `scenes/ui/screens/ranked_queue.tscn` | Queue UI |

**Match Properties:**
```gdscript
var match_properties: Dictionary = {
    "mode": "ranked_1v1",
    "rating": player_elo,
    "region": player_region  # Optional: for regional matching
}

var query = "+properties.mode:ranked_1v1 +properties.rating:>=%d +properties.rating:<=%d" % [
    player_elo - 200,  # Min rating
    player_elo + 200   # Max rating
]
```

**Implementation Steps:**
1. [ ] Create `matchmaking_service.gd`
2. [ ] Implement queue join/leave
3. [ ] Handle match found callback
4. [ ] Create queue UI with cancel button
5. [ ] Integrate with P2P connection (or Nakama relay)

---

### 3.4 ELO System

- [ ] **Not Started**

Skill-based rating system.

**New Files to Create:**

| File | Purpose |
|------|---------|
| `scripts/multiplayer/ranking/elo_calculator.gd` | ELO math |
| `scripts/multiplayer/ranking/ranking_service.gd` | Rating management |

**ELO Parameters:**
```gdscript
const STARTING_ELO = 1200
const K_FACTOR = 32
const ELO_FLOOR = 800

static func calculate_new_ratings(winner_elo: int, loser_elo: int) -> Dictionary:
    var expected_winner = 1.0 / (1.0 + pow(10, (loser_elo - winner_elo) / 400.0))
    var expected_loser = 1.0 - expected_winner

    var new_winner_elo = winner_elo + int(K_FACTOR * (1.0 - expected_winner))
    var new_loser_elo = max(ELO_FLOOR, loser_elo + int(K_FACTOR * (0.0 - expected_loser)))

    return {
        "winner": new_winner_elo,
        "loser": new_loser_elo
    }
```

**Implementation Steps:**
1. [ ] Create `elo_calculator.gd` with math
2. [ ] Create `ranking_service.gd` to fetch/update ratings
3. [ ] Store ratings in Nakama storage
4. [ ] Display rating in UI

---

### 3.5 Match Reporting

- [ ] **Not Started**

Report match outcomes to Nakama.

**New Files to Create:**

| File | Purpose |
|------|---------|
| `scripts/multiplayer/ranking/match_reporter.gd` | Submit results |

**Match Report Data:**
```gdscript
var match_report: Dictionary = {
    "match_id": nakama_match_id,
    "winner_id": winner_user_id,
    "loser_id": loser_user_id,
    "winner_elo_before": winner_elo,
    "loser_elo_before": loser_elo,
    "winner_elo_after": new_winner_elo,
    "loser_elo_after": new_loser_elo,
    "duration_seconds": match_duration,
    "end_reason": "summoner_destroyed",  # or "forfeit", "disconnect"
    "timestamp": Time.get_unix_time_from_system()
}
```

**Implementation Steps:**
1. [ ] Create `match_reporter.gd`
2. [ ] Submit match results to Nakama (server-side RPC)
3. [ ] Update local rating cache
4. [ ] Store match history

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

### Next Session
- Start Phase 2.1 (Action System) - PlayCardAction, ForfeitAction
- Integrate authority system with actual gameplay
- Manual testing of P2P connection

---

*Last Updated: 2026-01-04*
