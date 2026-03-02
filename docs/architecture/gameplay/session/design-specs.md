# Session Layer — Design Specs

Detailed design for Session-layer components beyond the stubs in [README.md](README.md). Each section covers responsibilities, invariants, integration points, and edge cases.

For the stub API surface, see [README.md](README.md). For the layer overview, see [target-architecture.md](../../target-architecture.md) §3.

---

## 1. AI Command Submission

**Decision:** AI is an Input-layer peer (Decision #12). It submits commands through `IGameSession.SubmitCommand()`, exactly like human input.

### Design

AI does NOT live inside Session. `LocalSession` has no AI mode — it's just a session that validates commands and ticks the simulation. AI is an external command source.

```
Human Input ─┐
              ├──► IGameSession.SubmitCommand()
AI Input ────┘
```

**AI reads state:** `IGameSession.GetState()` each tick to evaluate the board.

**AI submits commands:** `IGameSession.SubmitCommand(new PlayCardCommand(...))` when it decides to play a card.

**AI timing:** AI evaluates on a configurable delay (not every frame) to simulate "thinking time" and avoid overwhelming the simulation with commands on the same tick.

### Integration

```
// Battle scene setup (pseudocode)
var session = new LocalSession(simulation, commandRouter, state);

// Human player
var inputCollector = new InputCollector();
inputCollector.Initialize(session);

// AI opponent
var aiController = new AIController();
aiController.Initialize(session, playerIndex: 1);
```

Both `InputCollector` and `AIController` call `session.SubmitCommand()`. Session doesn't know or care which is human and which is AI.

### Multiplayer AI

For multiplayer with AI fill-in (e.g., opponent disconnects), the host creates an `AIController` that submits commands on behalf of the disconnected player. No session changes needed — AI is just another command source.

---

## 2. StateInterpolator

**Current:** `Multiplayer/Client/StateInterpolator.cs` — lerps entity positions between snapshots.

### Design

`StateInterpolator` is owned by `ClientSession`. It runs **after** snapshot application and **before** View reads state.

```
ClientSession.Tick(delta):
  1. Apply latest snapshot to _localState     (discrete jump)
  2. _interpolator.Update(delta)              (smooth positions)
  3. Write interpolated positions into _localState
  4. Fire SimEventsEmitted
  → View reads _localState (sees smooth positions)
```

**Key invariant:** The View layer is completely unaware of interpolation. It just reads `IGameSession.GetState()` and gets smooth values. Interpolation is a Session-internal concern.

### Migration from Current

Current `StateInterpolator` uses `networkId` keys and `Godot.Vector3`. Target version:
- Uses `unitId` keys (via `IdentityMap` translation at snapshot application)
- Uses `SimVector3` or writes directly into `UnitData.Position` fields
- No Godot imports (pure C#, consistent with session layer)

### Edge Cases

- **Teleports:** If distance > `SnapThreshold` (5.0), snap instead of interpolate. Prevents slow drift across the map on large state corrections.
- **New entities:** First position is set to target (no interpolation from origin). Already handled by `SetTarget` initializing `CurrentPosition = targetPosition`.
- **Removed entities:** Call `Remove(unitId)` when entity leaves MatchState. `ClientSession` handles this during snapshot diff.

---

## 3. Deterministic RNG Synchronization

**Current:** `MatchSession.Seed` is shared at match start. `DeterministicRng` uses xorshift32.

### Design

**Seed ownership:** The host generates a random seed at match start and sends it to the client via the `MatchStarted` protocol message (already exists: `MatchStarted.Seed`).

**RNG lifecycle:**
```
Host:   new DeterministicRng(seed) → passed to Simulation constructor
Client: receives seed in MatchStarted → new DeterministicRng(seed) (but client doesn't tick sim)
```

Since the client doesn't tick its own simulation (it applies host snapshots), the client's `DeterministicRng` is unused for gameplay. The host's RNG drives all gameplay randomness.

**Where RNG is called:** Only inside `Simulation.Tick()` — damage rolls, crit checks, targeting tiebreaks. No code outside the simulation calls the gameplay RNG.

**View RNG (Decision #16):** View layer creates its own `DeterministicRng` with a separate seed (derived from the battle seed, e.g., `seed + 1`). This provides deterministic visuals across machines without affecting sim determinism.

### Invariant

The simulation's `DeterministicRng` is NEVER accessed outside the `Simulation` class. Session creates it and passes it to `Simulation` at construction. After that, only `Simulation.Tick()` advances the sequence.

---

## 4. Client Prediction

**Decision:** Predict mana deduction + card removal from hand. Unit spawn waits for host confirmation.

### Design

When the client plays a card:

```
1. Client calls IGameSession.SubmitCommand(PlayCardCommand)
2. ClientSession:
   a. Send command to host over network
   b. Locally: deduct mana, remove card from hand in _localState
   c. Add prediction to PredictionBuffer with sequence number
3. Host receives, validates via CommandRouter, applies to sim
4. Host broadcasts next snapshot
5. ClientSession receives snapshot:
   a. Compare predicted state with host state
   b. If consistent: remove prediction from buffer (confirmed)
   c. If inconsistent: rollback — restore mana, return card to hand
```

### PredictionBuffer Integration

```csharp
public class CardPlayPrediction
{
    public int Sequence { get; init; }
    public int CardIndex { get; init; }
    public float ManaCost { get; init; }
    public string CatalogId { get; init; }  // card removed from hand
}
```

**Reconciliation:** When a snapshot arrives, `ClientSession` checks if predicted mana and hand match the host's values. If they match, the prediction was correct — discard it. If they don't match, the host rejected the play — restore mana, re-insert the card into the hand.

### Edge Cases

- **Multiple predictions in flight:** Each has a sequence number. Process in order during reconciliation.
- **Full resync:** On reconnection or desync, clear all predictions and accept host state as authoritative.
- **Latency spike:** Predictions pile up in the buffer. Cap at ~5 pending predictions; reject further card plays until some are confirmed.

---

## 5. BattleConfig — Session Initialization

**Decision:** Typed `BattleConfig` passed to session constructors (Decision #13). `BattleResultHandler` handles post-battle aftermath.

### BattleConfig Structure

```csharp
public class BattleConfig
{
    // Players
    public required PlayerConfig Player { get; init; }
    public required PlayerConfig Opponent { get; init; }

    // Battle settings
    public required GameMode Mode { get; init; }         // SP, Host, Client
    public required WinCondition WinCondition { get; init; }
    public BiomeId Biome { get; init; } = BiomeId.SummerPlains;
    public int LevelCap { get; init; } = 0;              // 0 = uncapped

    // Multiplayer only
    public long Seed { get; init; }
    public string MatchId { get; init; } = "";
}

public class PlayerConfig
{
    public required string SummonerId { get; init; }
    public required IReadOnlyList<SimCardData> Deck { get; init; }
    public required SummonerData SummonerData { get; init; }
    public string UserId { get; init; } = "";             // MP only
}
```

### Session Construction

```csharp
// Singleplayer
var session = new LocalSession(config, simulation, commandRouter);

// Multiplayer host
var session = new HostSession(config, simulation, commandRouter, transport);

// Multiplayer client
var session = new ClientSession(config, transport);
```

Each session type reads what it needs from `BattleConfig` at construction. `LocalSession` needs both player configs and the simulation. `ClientSession` doesn't need a simulation instance (it applies snapshots).

### BattleContext Role After Migration

`BattleContext` autoload stays but becomes a `BattleConfig` builder:
1. `configure_campaign_battle()` → builds `BattleConfig` from campaign event data
2. `configure_multiplayer_battle()` → builds `BattleConfig` from match start data
3. `configure_practice_battle()` → builds `BattleConfig` with test defaults
4. Stores the built `BattleConfig` for the battle scene to read at init

`BattleContext` no longer handles completion callbacks, XP grants, or scene transitions. Those move to `BattleResultHandler`.

### BattleResultHandler

New component that reacts to battle completion:

```
BattleResultHandler:
  - Watches IGameSession for GamePhase.Victory/Defeat
  - On victory: grant card XP, grant summoner XP, report ranked match
  - Transitions to appropriate scene (reward screen, campaign map, lobby)
  - Reads BattleContext.Mode to determine which aftermath to run
```

This replaces the completion callbacks currently scattered across `BattleContext._handle_campaign_completion()`, `_handle_multiplayer_completion()`, etc.

---

## 6. CommandRouter Validation Rules

**Current:** `RequestValidator` validates card plays and forfeits. Only runs for client requests on the host.

**Target:** `CommandRouter` validates ALL commands for ALL session types (Decision #9 from architectural issues).

### Validation Rules

```csharp
public class CommandRouter
{
    public ValidationResult Validate(ICommand command, MatchState state)
    {
        return command switch
        {
            PlayCardCommand play => ValidatePlayCard(play, state),
            ForfeitCommand forfeit => ValidateForfeit(forfeit, state),
            _ => new ValidationResult(false, $"Unknown command type: {command.GetType().Name}")
        };
    }
}
```

#### PlayCardCommand Validation

| Check | Rule | Error |
|-------|------|-------|
| Player index | `0 <= playerIndex < state.Summoners.Length` | "Invalid player index" |
| Card index | `0 <= cardIndex < summoner.Hand.Count` | "Card index out of range" |
| Mana | `summoner.Mana >= cardData.ManaCost` | "Not enough mana" |
| Phase | `state.Phase == GamePhase.Battle` | "Cannot play cards in current phase" |
| Casting state | `!summoner.IsCasting` | "Already casting" |
| Card exists | `state.CardDataMap.ContainsKey(catalogId)` | "Card data not found" |

#### ForfeitCommand Validation

| Check | Rule | Error |
|-------|------|-------|
| Player index | `0 <= playerIndex < state.Summoners.Length` | "Invalid player index" |
| Phase | `state.Phase != GamePhase.GameOver` | "Game already over" |

### Integration

```
LocalSession.SubmitCommand(cmd):
  result = _commandRouter.Validate(cmd, _state)
  if result.IsValid → queue for next tick
  else → log rejection (or fire rejection event)

HostSession.HandleRemoteCommand(senderId, cmd):
  result = _commandRouter.Validate(cmd, _state)
  if result.IsValid → queue for next tick
  else → send rejection to client
```

**Key change from current:** Host also validates its own commands. No more bypassing validation for local host plays (fixes issue #9).

---

## 7. MatchSession Retirement

**Current:** `Multiplayer/Core/MatchSession.cs` — 359-line orchestrator that owns transport, runner, serializer, and network IDs.

### What Transfers to NetworkSession

| MatchSession Responsibility | Target | Notes |
|----------------------------|--------|-------|
| `Seed`, `MatchId`, `PlayerIds`, `SummonerIds` | `BattleConfig` | Match config data → init parameter |
| `IsHost`, `LocalPlayerIndex` | `BattleConfig.Mode` | Determines session type (Host/Client) |
| `CurrentFrame`, `MatchTime` | Session internals | Each session type tracks its own |
| `NetworkIds` registry | `IdentityMap` | Already stubbed |
| `_runner` (HostRunner/ClientRunner) | Eliminated | Session IS the runner now |
| `_transport` | `NetworkSession` | Transport ownership stays |
| `_serializer` | `SnapshotCodec` | Serialization consolidates |
| `StartMatch()` | Constructor | Session constructed with config |
| `EndMatch()` | `BattleResultHandler` + session cleanup | Split: result handling vs cleanup |
| `RequestCardPlay()` / `RequestForfeit()` | `IGameSession.SubmitCommand()` | Unified command interface |
| `Send()` / `Broadcast()` / `SendTo()` | `NetworkSession` internals | Transport methods stay |
| `HandleRawMessage()` | `NetworkSession.HandleMessage()` | Message routing stays |
| `HandlePeerDisconnected()` | `NetworkSession` | Disconnect handling stays |
| `HandleTransportDisconnected()` | `NetworkSession` + `ReconnectionHandler` | Reconnect logic stays |
| `MatchStarted` / `MatchEnded` signals | `IGameSession.SimEventsEmitted` | Unified event system |
| `StartMatchFromGDScript()` | GDScript bridge on `BattleScene` | GDScript interop moves to View |

### Migration Order

1. Implement `NetworkSession` with transport ownership and message routing
2. Implement `HostSession` extending `NetworkSession` (absorbs `HostRunner`)
3. Implement `ClientSession` extending `NetworkSession` (absorbs `ClientRunner`)
4. Update multiplayer lobby to construct `HostSession`/`ClientSession` instead of `MatchSession` + runner
5. Delete `MatchSession`, `HostRunner`, `ClientRunner`, `IMatchRunner`

### What Gets Deleted (Not Transferred)

- `MatchSession.Current` singleton — constructor injection replaces static access
- `IMatchRunner` interface — no separate runner concept
- `HostEventBroadcaster` — absorbed into `HostSession`
- `IMessageBroadcaster` — direct transport calls replace interface

---

## 8. ReconnectionHandler Migration

**Current:** `Multiplayer/Core/ReconnectionHandler.cs` — singleton with 338 lines. Handles disconnect detection, exponential backoff reconnection, and state resync requests.

### Design

`ReconnectionHandler` is owned by `NetworkSession`. Not a singleton — each network session has its own reconnection handler.

```csharp
public abstract class NetworkSession : IGameSession
{
    protected readonly IdentityMap _identityMap = new();
    protected readonly SnapshotCodec _snapshotCodec = new();
    protected readonly ReconnectionHandler _reconnection;

    protected NetworkSession(BattleConfig config, IMatchTransport transport)
    {
        _reconnection = new ReconnectionHandler(transport);
    }
}
```

### Reconnection Flow

```
Client loses connection:
  1. ReconnectionHandler detects disconnect (transport callback)
  2. Starts exponential backoff reconnection attempts
  3. UI shows reconnection overlay (reads ReconnectionHandler.State)
  4. On success: send StateResyncRequest to host
  5. Host sends full MatchState snapshot
  6. ClientSession applies snapshot, clears PredictionBuffer
  7. Resume normal operation
```

### Changes from Current

| Current | Target |
|---------|--------|
| Static singleton (`Instance`) | Owned by `NetworkSession` |
| Uses `GD.Randf()` for jitter | Uses standard `Random` (no Godot dep) |
| Emits Godot signals | Exposes C# events or state that View reads |
| `StateResyncRequest`/`Response` classes | Part of the protocol messages, handled by `ClientSession` |
| Reconnection triggers transport calls | `NetworkSession` coordinates between `ReconnectionHandler` and transport |

### Host-Side Reconnection

When the host detects a client reconnect:
1. Accept the new connection
2. Receive `StateResyncRequest` with client's last known frame
3. Send full `MatchState` snapshot via `SnapshotCodec`
4. Resume normal snapshot broadcasting

The host doesn't need its own `ReconnectionHandler` for reconnecting (hosts can't reconnect to themselves). But `NetworkSession` still has one for detecting client disconnects and managing grace periods.
