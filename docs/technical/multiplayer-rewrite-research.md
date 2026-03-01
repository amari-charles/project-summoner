# Multiplayer Architecture: Pure View Rewrite

## Date: 2026-02-28

## Problem Statement

After 100+ incremental fixes, multiplayer battles still desync. The root cause: the GDScript layer (GameController3D, Summoner) maintains parallel state and runs independent logic instead of reading from `SimulationNode.State`.

## Architectural Audit Findings

### C# Layer: Sound

The C# multiplayer layer (SimulationNode, HostRunner, ClientRunner, MatchSession) is architecturally correct:

- **Host:** `Tick()` mutates MatchState → `StateSnapshotBuilder.Build()` → `HostRunner.BroadcastSnapshot()` at 10Hz
- **Client:** `ApplySnapshot()` overwrites local MatchState with host's authoritative values
- **Guard:** `SimulationNode._PhysicsProcess()` line 226: `if (!IsHost) return;` — client never runs Tick()
- **Signals from ApplySnapshot:** `PhaseChanged`, `PrepTimerUpdated`, `MatchTimeUpdated`, `SummonerHpChanged`, `SummonerManaChanged`, `SummonerHandUpdated` — all correctly emitted
- **Events (ephemeral):** Host broadcasts UnitDied, DamageDealt, SummonerDamaged as separate messages; ClientRunner receives and emits same signals on SimulationNode

### GDScript Layer: Broken

- **GameController3D** maintained `current_phase`, `prep_time_remaining`, `match_time` independently
  - `_process()` ran its own prep timer, never read `SimulationNode.State.Phase`
  - When host skipped prep, client stayed stuck in PREPARATION forever
  - `_connect_summoner_combat_signals()` connected to `GameOver` but NOT `PhaseChanged`
- **Summoner** maintained `hand`, `mana`, `is_casting`, `current_hp` independently
  - `init_as_client()` connected to `CastingStarted`/`CastingCompleted`/`HandChanged` — dead signals that only fire during `Tick()` (host-only)
  - Client `is_casting` was always false → no casting lock → could spam card plays

### Specific Bugs Found

1. **ApplySnapshot hand sync guard (line 743):** `ss.Hand.Length > 0` blocked hand/deck/discard sync when hand was empty
2. **`get_active_deck()` missing:** `online_screen.gd` line 592 called method that didn't exist → always used placeholder deck
3. **No CastingStateChanged signal:** ApplySnapshot updated casting state in MatchState but emitted no signal for it

## Solution: Pure View Approach

**Principle:** Both host and client GDScript use the **same code** that reads from `SimulationNode.State`. The only variable is `LocalPlayerIndex` for team remapping. GDScript layers are pure views — zero state computation.

### Changes Made

1. **SimulationNode.cs:** Fixed hand sync guard; added `CastingStateChanged` signal with `_prevIsCasting` tracking
2. **GameController3D:** Connected to SimulationNode phase/timer signals; skips local timers in multiplayer
3. **Summoner:** Removed dead client signal connections; added `CastingStateChanged` handler for casting lock
4. **Profile repo:** Added `get_active_deck()` so multiplayer lobby sends real deck instead of placeholder

### Why This Works for Both Host and Client

SimulationNode emits `PhaseChanged`, `PrepTimerUpdated`, `MatchTimeUpdated` from BOTH paths:
- Host: `EmitEvents()` after `Tick()`
- Client: `ApplySnapshot()` when snapshot arrives

Same signals → same handlers → same behavior. GameController3D doesn't know or care if it's host or client.

## Digging Deeper (If Pure View Doesn't Fully Resolve)

### Transport Layer Reliability
- `NakamaGameClient.SendMatchData()` uses fire-and-forget `_ = _socket.SendMatchStateAsync()` — send failures are invisible
- No reconnection logic for lost socket connections
- Add logging in `NakamaMatchTransport.OnNakamaMatchData()` to confirm messages actually arrive

### Snapshot Content
- `StateSnapshotBuilder.Build()` — verify it actually includes units, summoner state, hand data
- Check if snapshots have empty Units/Summoners arrays
- Add logging to print snapshot contents on both host (send) and client (receive)

### Message Deserialization Failures
- `MatchSession.HandleRawMessage()` silently catches and logs deserialization exceptions
- If JSON round-trip corrupts data types (e.g. int→float), deserialization throws and the message is dropped
- Check Godot output for `[MatchSession] Failed to deserialize message:` errors

### Timing / Initialization Order
- MatchSession is started before `RemoteUnitSpawned` is connected — could miss early messages
- `FirstSnapshotApplied` signal — verify this fires and GDScript awaits it before starting game

### Deck Exchange
- `online_screen.gd` opCode 100 messages may be received by the sender (no self-filter for opCode 100)
- Opponent deck stored as `_pending_opponent_deck` — verify it reaches `BattleContext.configure_multiplayer_battle()`

### MatchState Drift Scenarios
- If ApplySnapshot misses a snapshot (dropped message), state stays stale until next snapshot (100ms later)
- If ApplySnapshot throws an exception mid-update, state is partially applied
- CardDataMap population via `PopulateSingleCard()` depends on CardCatalog having the card — missing catalog entries would break hand display
