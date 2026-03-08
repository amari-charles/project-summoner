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
| `Movement/SimMovement.cs` | Movement execution pipeline: intent → ORCA → integrate → overlap correction |
| `Movement/MovementIntent*.cs` | Intent contract and intent strategy selection (`DirectIntentGenerator`, optional `ContextIntentGenerator`) |
| `Movement/OrcaAvoidance.cs` | Velocity obstacle solver for local collision avoidance |
| `Movement/OverlapCorrection.cs` | Position-only safety pass for residual overlaps |
| `Movement/FacingController.cs` | Stable facing updates with dead-zones/hold timer (avoids rapid flip jitter) |
| `SimEffects.cs` | Buff/debuff/trigger system, periodic effects, delayed effects, stat queries |
| `SimProjectile.cs` | Projectile simulation (movement, homing, pierce, AoE) |

## Tick Order Contract (11 steps)

```
1.  Increment frame, advance match time (Battle only)
2.  Drain and execute due commands (PlayCard, Forfeit)
3.  Phase timers / transitions (Preparation → Battle: activate units, refresh hands)
4.  Tick casting (decrement timers, handle completions)
5.  Tick spawn timers (activate units whose timer expired)
6.  Tick units (cooldowns → targeting → behavior → movement → delayed ranged resolution)
7.  Tick projectiles
8.  Tick effects (buffs: decrement, periodic, remove expired)
9.  Tick delayed effects (death explosions, timed AoE)
10. Death cleanup (timer countdown, remove expired units)
11. Evaluate win conditions
```

Each step produces events appended to that tick's event list. Order matters — targeting happens before behavior, movement before delayed ranged resolution, effects before win condition evaluation.

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

### Session Runtime Model

Multiplayer runtime is owned by the Session layer:

- `SimulationNode.Initialize(...)` starts with `LocalSession` by default.
- `SimulationNode.ConfigureMultiplayerSession(transport, isHost)` swaps to:
  - `HostSession` (authoritative simulation tick + snapshot broadcast)
  - `ClientSession` (command send + snapshot apply, no deterministic tick)

`HostSession` responsibilities:
- Receives `CardPlayRequest` / `ForfeitRequest` from transport.
- Resolves authoritative team from sender identity (ignores payload team index).
- Validates via `CommandRouter`, queues commands for simulation.
- Ticks simulation and broadcasts periodic `StateSnapshot`.
- Broadcasts `MatchEnded` and selected gameplay messages (`SummonerDamageFlash`).

`ClientSession` responsibilities:
- Sends local commands to host as protocol messages.
- Applies host snapshots into local `MatchState` (`Summoners`, `Units`, `Projectiles`).
- Emits derived visual events from snapshot deltas.
- Handles reconnect grace window state.

### Snapshot Frequency

The host broadcasts a full `StateSnapshot` every **100ms (10Hz)** (`HostSession.SnapshotSendInterval = 0.1f`).

The simulation still advances at **60Hz** (`SimulationNode.FIXED_DELTA = 1.0f / 60.0f`) on host.  
Client-side motion smoothing is presentation-layer interpolation (`EntityManager` + `StateInterpolator`) over authoritative snapshot targets.

### State Hash Reporting

`StateHashReport` remains in protocol as reserved/desync telemetry, but full hash-based correction flow is not wired in the current `HostSession`/`ClientSession` runtime path.

### Coordinate Transforms

The simulation stores all positions in **canonical (network) space**:
- `X < 0` is the host's spawn zone
- `X > 0` is the client's spawn zone

`CoordinateTransform` (`scripts/csharp/Multiplayer/Core/CoordinateTransform.cs`) converts between canonical and **local space** (each player always sees their own spawn zone on the negative-X side):
- For the host, canonical and local are identical (no transform)
- For the client, `LocalToCanonical` and `CanonicalToLocal` both mirror the X axis (`-v.X`)

`SimulationNode` exposes `SimToLocal(SimVector3)` so presentation-layer code (UnitVisual, signals) always works in local space. Outgoing command positions are converted to canonical space before session submission.

Team IDs in `MatchState` are also network-perspective: team 0 = host, team 1 = client. `SimulationNode.RemapTeam()` converts between local team (PLAYER=0, ENEMY=1 from GDScript) and network team. The host has `LocalPlayerIndex = 0` so no remapping occurs; the client has `LocalPlayerIndex = 1` and swaps 0↔1.

---

## See Also

- **[Graph-Of-Graphs Model](../architecture/graph-of-graphs.md)** — Shared architecture vocabulary and projection rules
- **[Target Architecture](../architecture/target-architecture.md)** — Gameplay layer contracts and boundaries
- **Archived deep references** — Historical simulation reference and walkthrough docs moved under `docs/archive/doc-reorg-2026-03/technical/`
