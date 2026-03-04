# Simulation Architecture

## Overview

The deterministic simulation layer (`Fateforged.Simulation` namespace) is a pure C# system with **no Godot dependencies**. It operates entirely on `MatchState` data and produces `SimEvent` lists that the presentation layer (SimulationNode, UnitVisual) consumes.

### Design Principles

- **Pure determinism**: All state mutations flow through `Simulation.Tick()`. Same inputs → same outputs.
- **No Godot types**: Positions use `SimVector3`, not `Vector3`. Logging uses a delegate, not `GD.Print()`.
- **Host-authoritative**: Only the host runs `Simulation.Tick()`. Clients receive state snapshots and render.
- **Event-driven presentation**: Simulation produces `SimEvent` lists; `SimulationNode` emits them as Godot signals.

## File Map

| File | Purpose |
|------|---------|
| `Simulation.cs` | Core tick loop (11-step contract), command execution, phase transitions, casting, spawning |
| `MatchState.cs` | Central state container: units, summoners, projectiles, commands, RNG |
| `SimulationNode.cs` | **Godot bridge** — wraps Simulation, emits signals, handles coordinate transforms |
| `SimConstants.cs` | Shared constants (ActivationState values, DeathCleanupSeconds) |
| `SimUtils.cs` | Shared utilities (ResolveTargetPosition, KillUnit) |
| `Combat/SimBehavior.cs` | Unit behavior FSM: NoTarget → Chasing → InRange → Attacking |
| `Combat/SimTargeting.cs` | Target acquisition: filter → score pipeline, summoner fallback |
| `Combat/SimDamage.cs` | Damage calculation: evasion, crits, elemental matchups, defense, shields |
| `Movement/SimMovement.cs` | Movement execution: forward, toward-target, strafe |
| `Movement/SimSteering.cs` | Steering forces: separation, flanking, overlap correction |
| `SimEffects.cs` | Buff/debuff/trigger system, periodic effects, delayed effects, stat queries |
| `SimProjectile.cs` | Projectile simulation (movement, homing, pierce, AoE) |

## Tick Order Contract (11 steps)

```
1.  Increment frame, advance match time (Battle only)
2.  Drain and execute due commands (PlayCard, Forfeit)
3.  Phase timers / transitions (Preparation → Battle: activate units, refresh hands)
4.  Tick casting (decrement timers, handle completions)
5.  Tick spawn timers (activate units whose timer expired)
6.  Tick units (cooldowns → targeting → behavior → movement → pending damage)
7.  Tick projectiles
8.  Tick effects (buffs: decrement, periodic, remove expired)
9.  Tick delayed effects (death explosions, timed AoE)
10. Death cleanup (timer countdown, remove expired units)
11. Evaluate win conditions
```

Each step produces events appended to that tick's event list. Order matters — targeting happens before behavior, movement before pending damage, effects before win condition evaluation.

## Key Design Decisions

### Summoner Damage Bypasses SimDamage.Calculate()

Summoner damage intentionally does **not** go through `SimDamage.Calculate()`. Summoners are not units — they don't have evasion, crit interaction, elemental matchups, defense, or shields. Only summoner-level modifiers (`DamageBonus`, `DamageReduction`) apply. See `SimBehavior.ApplyDamageToSummoner()`.

### Instance-Scoped IDs

All ID counters (`_nextUnitId`, `_nextBuffId`, `_nextProjectileId`, `_nextNetworkId`) live on `MatchState` as instance fields. This ensures determinism across matches — a static counter would leak state between matches.

### Activation States

Units spawn as `Inactive` and become `Active` either:
- When `SpawnTimer` expires during Battle phase (battle-spawned units)
- When `ActivateAllUnits()` fires on Preparation → Battle transition (prep-spawned units)

Constants are centralized in `SimConstants` (mirrors `ProjectSummoner.Units.ActivationState`).

### Death Handling

All death logic flows through `SimUtils.KillUnit()` — the single source of truth for HP zeroing, alive flag, cleanup timer, kill count, and death event. Callers are responsible for firing appropriate triggers (OnKill, OnDeath, LeaderDeath) since trigger context varies by call site.

## Multiplayer Coordination

### HostRunner vs ClientRunner

Both implement `IMatchRunner` (`scripts/csharp/Multiplayer/Core/IMatchRunner.cs`). The concrete type is selected by `MultiplayerGameBridge` at match start and set on `SimulationNode.IsHost`.

**HostRunner** (`scripts/csharp/Multiplayer/Authority/HostRunner.cs`):
- Subscribes to `SimulationNode.OnTickCompleted` to receive the `List<SimEvent>` produced each tick
- Converts `SimEvent` objects into protocol messages (via `HostEventBroadcaster`, visitor pattern) and broadcasts them to all clients
- Receives `CardPlayRequest`, `ForfeitRequest`, `StateHashReport`, and `Ping` messages from clients
- Validates incoming `CardPlayRequest` via `RequestValidator` before submitting to the simulation
- Submits accepted commands as `PlayCardCommand` to `SimulationNode.SubmitCommand()`
- Broadcasts periodic `StateSnapshot` messages to clients (see Snapshot Frequency below)
- On desync detection: immediately broadcasts a full snapshot to resync the client

**ClientRunner** (`scripts/csharp/Multiplayer/Client/ClientRunner.cs`):
- Never calls `Simulation.Tick()` — `SimulationNode.IsHost` is set to `false`
- Receives `StateSnapshot`, `UnitSpawned`, `UnitDied`, `DamageDealt`, `SummonerDamaged`, and `MatchEnded` messages from the host
- Routes `StateSnapshot` to `SimulationNode.ApplySnapshot()` for authoritative state application
- Pre-registers incoming `UnitSpawned` units in `MatchState` before emitting the `RemoteUnitSpawned` signal, so that `UnitVisual._Ready()` can immediately claim the correct `UnitData`
- Maintains a `StateInterpolator` that smoothly interpolates unit positions between 10Hz snapshots at the display frame rate
- Sends `CardPlayRequest` (with canonical coordinates) for local card plays; applies an optimistic prediction locally while awaiting host confirmation
- Rolls back optimistic predictions when the host responds with `CardPlayRejected`
- Sends periodic `Ping` messages (every 1 second) to measure round-trip latency
- Sends periodic `StateHashReport` messages to the host for desync detection

### Snapshot Frequency

The host broadcasts a full `StateSnapshot` every **100ms (10Hz)**.

This is defined as `SnapshotInterval = 0.1` in `HostRunner.cs` (line 34). The snapshot contains positions, HP, mana, casting state, hand/deck/discard for both summoners, and activation/behavior state for all alive units.

The simulation itself ticks at **60Hz** (`FIXED_DELTA = 1.0f / 60.0f` in `SimulationNode.cs`, line 67), driven by `_PhysicsProcess` with a fixed-timestep accumulator. This means between each snapshot the host has already advanced ~6 simulation ticks.

The `ClientRunner` uses `StateInterpolator` to smooth the 10Hz positional data to the display frame rate so unit movement remains fluid on the client.

### DesyncDetector

`DesyncDetector` (`scripts/csharp/Multiplayer/Sync/DesyncDetector.cs`) runs on **both** host and client but serves different roles:

**Client side** — called by `ClientRunner.ProcessFrame`:
- Every `HashReportIntervalFrames` (60 frames, approximately 1 second at 60fps), the client computes a hash of its local `MatchState` via `StateSnapshotBuilder.ComputeHash()` and sends a `StateHashReport` to the host
- The client also calls `DesyncDetector.ApplySnapshot()` on each received snapshot, comparing local hash against `snapshot.StateHash`; on mismatch it applies positional corrections and increments the mismatch counter

**Host side** — called when a `StateHashReport` arrives:
- `DesyncDetector.CheckClientHash()` computes the authoritative hash at the current server frame and compares it with the client-reported hash
- If the frame lag between client and server exceeds `MaxFrameLagTolerance` (60 frames), the comparison is skipped to avoid false positives during catch-up
- After `DesyncThreshold` (3) consecutive mismatches the detector fires `OnDesyncDetected`, which causes `HostRunner` to immediately broadcast a full `StateSnapshot` for resync

### Coordinate Transforms

The simulation stores all positions in **canonical (network) space**:
- `X < 0` is the host's spawn zone
- `X > 0` is the client's spawn zone

`CoordinateTransform` (`scripts/csharp/Multiplayer/Core/CoordinateTransform.cs`) converts between canonical and **local space** (each player always sees their own spawn zone on the negative-X side):
- For the host, canonical and local are identical (no transform)
- For the client, `LocalToCanonical` and `CanonicalToLocal` both mirror the X axis (`-v.X`)

`SimulationNode` exposes `SimToLocal(SimVector3)` so presentation-layer code (UnitVisual, signals) always works in local space. All outgoing network messages use canonical coordinates; `ClientRunner` calls `CoordinateTransform.LocalToCanonical()` before sending `CardPlayRequest`.

Team IDs in `MatchState` are also network-perspective: team 0 = host, team 1 = client. `SimulationNode.RemapTeam()` converts between local team (PLAYER=0, ENEMY=1 from GDScript) and network team. The host has `LocalPlayerIndex = 0` so no remapping occurs; the client has `LocalPlayerIndex = 1` and swaps 0↔1.

---

## See Also

- **[Simulation Reference](simulation-reference.md)** — Mermaid diagrams, `MatchState` data structure reference, protocol message catalog
- **[Simulation Walkthrough](simulation-walkthrough.md)** — Human-readable gameplay flow examples (card play, combat, win condition)
