# Multiplayer Architecture

This document describes the multiplayer architecture for Fateforged.

---

## Design Decisions

### Network Model: Host-Authoritative P2P

One player is designated host and runs the authoritative simulation. The client receives periodic state snapshots and never runs game logic of its own.

```
┌─────────────────────────────────────────────┐
│              IMatchRunner (interface)        │
├─────────────────────────────────────────────┤
│  - Initialize(MatchSession)                  │
│  - ProcessFrame(double delta)                │
│  - HandleMessage(int senderId, object msg)   │
│  - RequestCardPlay(int cardIndex, Vector3)   │
│  - RequestForfeit()                          │
│  - Cleanup()                                 │
└─────────────────────────────────────────────┘
         ▲                        ▲
         │                        │
┌─────────────────┐    ┌──────────────────────┐
│   HostRunner    │    │    ClientRunner      │
│ (P2P - Phase 1) │    │  (client-only view)  │
└─────────────────┘    └──────────────────────┘
```

**Phase 1 (Launch):** One player hosts, runs the authoritative `Simulation.cs` tick, and broadcasts snapshots at 10 Hz. The other player connects P2P via Nakama relay.

**Phase 2 (Scale):** Swap `HostRunner` for a `ServerAuthority` implementation using the same `IMatchRunner` interface — no game logic rewrite required.

### Backend: Nakama

We chose **Nakama** for backend services because:
- Open source (MIT license) — no vendor lock-in
- Self-hostable — control costs at scale
- Official Godot GDScript SDK
- Built-in: accounts, matchmaking, leaderboards, friends
- Can start on a $5 VPS, scale as needed

See `backend-research.md` for the full analysis.

### Game Mode: 1v1 Ranked PvP

Initial multiplayer is 1v1 only. This simplifies:
- Matchmaking (exactly 2 players)
- Authority (one host, one client)
- Win conditions (one winner, one loser)

Future modes (co-op, spectator, tournaments) build on this foundation.

### Offline Support

Single-player campaign works without internet:
- No `MatchSession` is created for single-player battles
- Profile data stored in local JSON
- Nakama only required for multiplayer features
- If offline, multiplayer options are hidden in UI

---

## Core Architecture

### HostRunner

`scripts/csharp/Multiplayer/Authority/HostRunner.cs`

Runs the authoritative simulation on the host side. Key responsibilities:

- Subscribes to `SimulationNode.OnTickCompleted` to receive the list of `SimEvent`s produced each tick
- Converts those events into protocol messages via `HostEventBroadcaster` (visitor pattern) and broadcasts them
- Broadcasts a full `StateSnapshot` at **10 Hz** (every 100 ms) via a timer — `SnapshotInterval = 0.1` constant in source
- Validates card play requests received from the client via `RequestValidator`; submits accepted commands to `SimulationNode.SubmitCommand()`
- On desync detection, triggers an immediate out-of-cycle snapshot broadcast

HostRunner does **not** run simulation logic itself — it delegates entirely to `SimulationNode` / `Simulation.cs`.

### ClientRunner

`scripts/csharp/Multiplayer/Client/ClientRunner.cs`

Receives snapshots and events from the host, routes them to `SimulationNode`. Never calls `Tick()`.

- Routes `StateSnapshot` → `SimulationNode.ApplySnapshot()` which overwrites `MatchState` with host-authoritative values
- Routes `UnitSpawned` → calls `SimulationNode.PreRegisterRemoteUnit()` then emits `RemoteUnitSpawned` signal for GDScript visual spawning
- Routes `UnitDied` / `DamageDealt` / `SummonerDamaged` → emits corresponding Godot signals on `SimulationNode`
- Routes `MatchEnded` → emits `GameOver` on `SimulationNode`
- Performs position interpolation: `StateInterpolator` smooths unit positions between 10 Hz snapshot ticks and writes them back to `UnitData` so visuals are framerate-smooth
- Sends periodic `Ping` at 1 Hz for latency measurement
- Sends periodic `StateHashReport` (every 60 frames) for desync detection

The guard in `SimulationNode._PhysicsProcess()`:

```csharp
// Only the host runs Tick(). Clients receive events/snapshots from the host.
if (!IsHost)
    return;
```

ensures the client never advances simulation state locally. `ClientRunner.Initialize()` explicitly sets `SimulationNode.Current.IsHost = false`.

### MatchSession

`scripts/csharp/Multiplayer/Core/MatchSession.cs`

Central orchestrator for a multiplayer match. It owns:

- The `IMatchRunner` instance (either `HostRunner` or `ClientRunner`, selected in `StartMatch()`)
- The `IMatchTransport` reference (`NakamaMatchTransport` in current P2P mode)
- `NetworkIdRegistry` — maps network IDs to scene nodes
- Session metadata: `Seed`, `MatchId`, `PlayerIds`, `SummonerIds`, `IsHost`, `LocalPlayerIndex`
- Lifecycle signals: `MatchStarted`, `MatchEnded`, `ConnectionLost`

`MatchSession` routes raw incoming `Dictionary` messages through `MessageSerializer.Deserialize()` and dispatches them to the runner. Session-level messages (e.g., `MatchEnded`) are also handled directly in `DispatchMessageEvent()`.

`MatchSession` is a Godot `Node` (added as a child of `GameController3D`) with `ProcessMode = Always` so it continues ticking while the scene tree is paused.

A `MatchSession.Current` singleton accessor is provided, following the same pattern as `SimulationNode.Current`.

### StateSnapshotBuilder

`scripts/csharp/Multiplayer/Sync/StateSnapshotBuilder.cs`

Converts `MatchState` into the `StateSnapshot` protocol message. Reads directly from `SimulationNode.Current.State` — never touches the scene tree.

Snapshot contents:
- Frame number and match time
- Phase and prep time remaining
- Per-summoner: HP, MaxHp, mana, casting state, hand/deck/discard (as catalog ID arrays)
- Per-unit (alive only): NetworkId, team, position, HP, MaxHp, target NetworkId, activation state, behavior state, facing direction
- A deterministic `StateHash` for desync detection

Positions are quantized to millimeter precision (`×1000`) and HP/mana to tenth precision (`×10`) before hashing to avoid float drift producing false mismatches.

`StateSnapshotBuilder.ComputeHash()` is also called by `ClientRunner` to generate the client-side `StateHashReport`.

### DesyncDetector

`scripts/csharp/Multiplayer/Sync/DesyncDetector.cs`

Detects state divergence between host and client.

**Host side:** Receives `StateHashReport` from the client (sent every 60 frames). Computes its own authoritative hash and compares. If 3 consecutive mismatches occur (`DesyncThreshold = 3`), fires `OnDesyncDetected` which causes `HostRunner` to broadcast an immediate full snapshot.

**Client side:** On each `StateSnapshot` received, `DesyncDetector.ApplySnapshot()` compares the client's local hash against the snapshot's embedded `StateHash`. If they differ, it attempts position corrections on registered `Unit3D` nodes.

Frame lag tolerance (`MaxFrameLagTolerance = 60` frames) prevents false mismatches when the client is still catching up after joining.

---

## Summoner Exchange

Before transitioning to the battle scene, both players exchange their deck and summoner instance data over Nakama using **opCode 100**.

### Exchange Protocol

`scripts/meta/screens/online_screen.gd` — `_exchange_deck_data()`

1. Each player serializes their active deck and `SummonerInstance` via `SummonerInstance.to_dict()` which includes: `summoner_id`, `level`, `xp`, `acquired_trait_ids`
2. Both are JSON-encoded and sent via `NakamaGameClient.SendMatchData(100, deck_json)`
3. `_on_match_data_received()` receives the opponent's data (filtered to opCode 100)
4. The opponent `SummonerInstance` dict is stored as `_pending_opponent_summoner_data` and passed to `BattleContext.configure_multiplayer_battle()`

### Applying Opponent Stats

`scripts/core/game_controller_3d.gd` — `_init_summoners()`

On the **host** (who runs simulation for both teams), the opponent's summoner instance data is reconstructed:

```gdscript
var opponent_data: Dictionary = BattleContext.battle_config.get("opponent_summoner_data", {})
var opponent_instance: SummonerInstance = SummonerInstance.from_dict(opponent_data)
enemy_summoner.set_summoner_instance(opponent_instance)
```

`SummonerInstance.from_dict()` re-loads the config from `SummonerCatalog` (using the exchanged `summoner_id`) and restores level, xp, and acquired traits. The reconstructed instance is passed to `enemy_summoner.set_summoner_instance()` before `enemy_summoner.init()` is called, so `_apply_summoner_bonuses()` applies the correct level-scaled HP and mana values to the simulation.

The client does **not** reconstruct the opponent instance — it receives all state from host snapshots.

---

## GDScript View Layer

`scripts/core/game_controller_3d.gd` and `scripts/core/summoner.gd`

GDScript is a **pure read-only view** of `SimulationNode.State`. Neither `GameController3D` nor `Summoner` owns authoritative match state.

### Principle

Both host and client GDScript use the **same code paths**. The only variable is `LocalPlayerIndex` (set on `SimulationNode`) which remaps network teams to local display teams. GDScript does not branch on "am I host or client" for game state — it only reads from `SimulationNode`.

### Signal Sources

`SimulationNode` emits signals from **two paths**:

- **Host:** `SimEventSignalEmitter` runs after each `Tick()` inside `EmitEvents()`, converting `SimEvent` objects into Godot signals
- **Client:** `SimulationNode.ApplySnapshot()` emits the same signals (`PhaseChanged`, `PrepTimerUpdated`, `MatchTimeUpdated`, `SummonerHpChanged`, etc.) when values change in the applied snapshot

Same signals → same handlers → same behavior regardless of role.

### SimEventSignalEmitter

`scripts/csharp/Battle/Simulation/SimEventSignalEmitter.cs`

A visitor that converts `SimEvent` objects into Godot signal emissions on `SimulationNode`. Uses the visitor pattern for compile-time exhaustiveness: adding a new `SimEvent` subclass without a corresponding `Visit()` in `SimEventSignalEmitter` causes a compile error.

Handles team remapping via `SimulationNode.RemapTeam()` before emitting, so all emitted signals are already in local team perspective for GDScript.

### HostEventBroadcaster

`scripts/csharp/Multiplayer/Authority/HostEventBroadcaster.cs`

A parallel visitor to `SimEventSignalEmitter`, but targeting the network protocol instead of Godot signals. Converts `SimEvent` objects into protocol messages (`UnitSpawned`, `UnitDied`, `DamageDealt`, `SummonerDamaged`, `MatchEnded`) and broadcasts them to the client.

Events fully covered by the periodic `StateSnapshot` (phase changes, timer updates, mana, hand/deck) are no-ops in `HostEventBroadcaster` — they are implicitly synced via the next snapshot.

### Multiplayer Client Polling

For the multiplayer client, `GameController3D._process()` polls `SimulationNode` directly every frame for phase, prep timer, and match time:

```gdscript
if BattleContext.is_multiplayer_battle() and not BattleContext.has_authority():
    _poll_match_state()
    return
```

This produces smoother UI updates than relying solely on signals from 10 Hz snapshots.

---

## Transport Layer

### NakamaMatchTransport

`scripts/csharp/Battle/Session/Transport/NakamaMatchTransport.cs`

Implements `IMatchTransport`. Routes all game messages through the Nakama relay using **opCode 200** (opCode 100 is reserved for the pre-battle deck exchange in GDScript).

The transport filters out the local player's own messages by comparing `senderId` against `NakamaGameClient.UserId`. Since Nakama relay broadcasts to all match participants including the sender, self-filtering is required.

`SendTo(int peerId, Dictionary)` is a no-op for targeted sends — Nakama relay does not support peer-targeted messages within a match, so it broadcasts to all and receivers ignore unexpected messages.

The transport fires `OnPeerDisconnected` when `MatchPresenceLeft` arrives from `NakamaGameClient`, which causes `MatchSession` to end the match.

---

## Match Lifecycle

```
1. QUEUE
   └── Player enters ranked queue via MatchmakingService → Nakama
   └── Nakama finds opponent with similar rating

2. MATCH FOUND
   └── MatchmakingService emits MatchFound with: match_id, opponent info, opponent summoner_id
   └── Both players exchange deck + SummonerInstance over opCode 100
   └── Host role determined: lexicographically smaller UserId is host (player 0)
   └── Deterministic seed = match_id.hash()

3. BATTLE SCENE LOAD
   └── BattleContext.configure_multiplayer_battle() stores all exchanged data
   └── SceneManager transitions to battle scene
   └── GameController3D._ready() runs initialization phases:
       ├── SimulationNode created and initialized with seed from BattleContext
       ├── IsHost=false set immediately for client (before MatchSession)
       ├── Summoners initialized: host applies opponent SummonerInstance, client skips
       └── NakamaMatchTransport and MatchSession created (_setup_multiplayer)

4. MATCH START
   └── MatchSession.StartMatch() creates HostRunner or ClientRunner
   └── Runner.Initialize() wires up to SimulationNode
   └── Client awaits FirstSnapshotApplied signal before start_game()
   └── Host calls start_game() immediately

5. BATTLE
   └── Host: Simulation.Tick() runs at 60 Hz via SimulationNode._PhysicsProcess()
   └── Host: HostRunner broadcasts StateSnapshot at 10 Hz
   └── Host: HostEventBroadcaster broadcasts ephemeral events (spawns, deaths, damage)
   └── Client: receives snapshots → SimulationNode.ApplySnapshot() → signals → UI
   └── Players play cards → CardPlayRequest (client) or direct SubmitCommand (host)

6. END
   └── Host detects win via IWinCondition → GameOverEvent → MatchEnded broadcast
   └── Client receives MatchEnded → SimulationNode emits GameOver signal
   └── GameController3D.end_game() runs on both sides
   └── MatchSession.EndMatch() cleans up runners, transport, NetworkIdRegistry
   └── MatchReporter reports result to Nakama (ELO update)
```

---

## Nakama Integration

```
┌─────────────────────────────────────────────┐
│                   Nakama                     │
├─────────────────────────────────────────────┤
│  Authentication     │  Matchmaking          │
│  - Device auth      │  - Ranked queue       │
│  - Email/social     │  - ELO-based matching │
├─────────────────────┼───────────────────────┤
│  Leaderboards       │  Player Data          │
│  - Global top 100   │  - Rating storage     │
│  - Friends          │  - Match history      │
└─────────────────────────────────────────────┘
```

Nakama serves as:
1. **Relay transport** for in-game messages (`NakamaMatchTransport` wraps this)
2. **Matchmaking** via `MatchmakingService`
3. **Match result reporting** via `MatchReporter` for ELO updates

---

## Anti-Cheat Strategy

Since host has authority:
1. **Authority Validation** — All card play requests validated by `RequestValidator` before accepting
2. **Rate Limiting** — RequestValidator can enforce per-second action limits
3. **State Hashing** — `DesyncDetector` compares client and host hashes; mismatches trigger resync
4. **Replay Storage** — Match data for review (Phase 4, not yet implemented)

**Known limitations of P2P:**
- Host can cheat (delay opponent's actions, inspect MatchState, etc.)
- Mitigated later by dedicated servers

---

## File Structure

```
scripts/csharp/Multiplayer/
├── Authority/
│   ├── HostRunner.cs           # Authoritative simulation runner
│   ├── HostEventBroadcaster.cs # SimEvent → protocol message visitor
│   └── RequestValidator.cs     # Validates client card play requests
├── Client/
│   ├── ClientRunner.cs         # Client-side snapshot consumer
│   ├── PredictionBuffer.cs     # Optimistic prediction tracking
│   └── StateInterpolator.cs    # Position interpolation between snapshots
├── Core/
│   ├── MatchSession.cs         # Central match orchestrator
│   ├── IMatchRunner.cs         # Interface for host/client runners
│   ├── IMessageBroadcaster.cs  # Interface for message sending
│   ├── NetworkIdRegistry.cs    # NetworkId → Node3D mapping
│   ├── LocalPlayer.cs          # Local player index singleton
│   ├── TeamIndex.cs            # Typed network/local team wrappers
│   ├── CoordinateTransform.cs  # Local ↔ canonical position conversion
│   └── ReconnectionHandler.cs  # Client reconnection logic
├── Sync/
│   ├── StateSnapshotBuilder.cs # MatchState → StateSnapshot conversion
│   └── DesyncDetector.cs       # Frame hash desync detection
├── Transport/
│   ├── IMatchTransport.cs      # Transport interface
│   ├── NakamaMatchTransport.cs # Nakama relay transport (current)
│   └── P2PTransport.cs         # Direct P2P transport (future/fallback)
├── Protocol/
│   ├── Messages.cs             # All protocol message record types
│   └── MessageSerializer.cs    # Dictionary ↔ message serialization
├── Backend/
│   └── NakamaGameClient.cs     # Nakama SDK wrapper autoload
├── Matchmaking/
│   └── MatchmakingService.cs   # Queue management
└── Ranking/
    ├── EloCalculator.cs
    ├── RankingService.cs
    └── LeaderboardService.cs

scripts/csharp/Battle/Simulation/
├── SimulationNode.cs           # Scene bridge: owns MatchState, runs Tick()
└── SimEventSignalEmitter.cs    # SimEvent → Godot signal visitor

scripts/core/
├── game_controller_3d.gd       # Pure view: reads SimulationNode, drives UI
└── summoner.gd                 # Pure view: HP/mana/hand display only

scripts/meta/screens/
└── online_screen.gd            # Matchmaking UI + deck/summoner exchange
```

---

## Future Considerations

### Dedicated Servers (Phase 2+)

- Implement `IMatchRunner` for a server authority
- Nakama orchestrates server allocation
- Both clients connect to server (no P2P)
- True anti-cheat (no trusted clients)

### Spectator Mode

- Additional client type (read-only)
- Receives all broadcast events
- No action permissions

### Tournaments

- Bracket system via Nakama
- Scheduled matches
- Prize distribution

---

*Last Updated: 2026-03-01*
