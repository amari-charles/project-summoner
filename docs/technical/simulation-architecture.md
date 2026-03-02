# Simulation Architecture

## Overview

The deterministic simulation layer (`Fateforged.Simulation` namespace) is a pure C# system with **no Godot dependencies**. It operates entirely on `MatchState` data and produces `SimEvent` lists that the presentation layer (SimulationNode, Unit3D) consumes.

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

## Multiplayer Architecture

### Host

- Runs `Simulation.Tick()` at 60Hz fixed timestep
- Receives commands from clients via `HostRunner`
- Sends state snapshots at 10Hz to clients
- Authoritative for all game state

### Client

- Does **not** run `Simulation.Tick()`
- Applies state snapshots from host to update unit positions/HP/state
- Sends commands (PlayCard, Forfeit) to host
- Renders presentation layer from snapshot data

### Coordinate Transforms

The simulation uses **canonical coordinates** (team 0 on left, team 1 on right). `SimulationNode` handles perspective transforms so each player sees themselves on the left side of the battlefield.
