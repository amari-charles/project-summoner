# Simulation Layer

Pure C#, zero Godot imports. No networking. Testable without the engine.

## Overview

The simulation calculates what happens each frame — movement, targeting, combat, abilities, projectiles. It writes results into `MatchState` and emits `SimEvents` as a log of what changed.

For the full design, see [target-architecture.md &sect;2](../../target-architecture.md#2-simulation-layer).

## Key Types

| Type | Role |
|------|------|
| `Simulation` | Tick loop — advances game state each frame |
| `MatchState` | All game data: units, projectiles, summoners, phase |
| `Command` | Player intent (e.g., "play card X at position Y") |
| `SimEvent` | Notification of what changed (e.g., "unit A dealt 50 damage to unit B") |

## Subsystems

| Subsystem | Responsibility |
|-----------|---------------|
| `SimDamage` | Damage math — crit, evasion, elemental matchups |
| `SimEffects` | Buff timers, modifier application |
| `SimProjectile` | Projectile movement, collision, pierce logic |
| `SimAbility` | Ability triggers, activation conditions |
| `SimBehavior` | Unit behavior state machine, targeting |
| `SimTargeting` | Target selection and acquisition |

## Boundaries

`Command` and `SimEvent` are the only things that cross the layer boundary. Everything else is internal.

## SimulationNode Transition Plan

`SimulationNode.cs` (942 lines) is the current god class that will be decomposed into the target architecture. Here's what moves where:

### What SimulationNode is today

A single Godot `Node` that owns `MatchState`, runs the tick loop, manages unit IDs, handles snapshot application, populates card data, remaps coordinates for teams, and emits events. Too many responsibilities in one place (issue #7).

### What moves where

| Current Responsibility | Target Component | Layer |
|----------------------|-----------------|-------|
| Simulation ticking | `LocalSession.Tick()` / `HostSession.Tick()` | Session |
| Snapshot application (client mode) | `ClientSession.ApplySnapshot()` | Session |
| Unit ID management (`_claimedSimUnitIds`, `_unit3DBySimId`, `_nextClientUnitId`) | `IdentityMap` | Session |
| Event emission (`OnTickCompleted`) | Session fires `SimEventsEmitted` | Session |
| Card data population (lines 298-314) | Stays — initialization concern | Simulation |
| Team/coordinate remapping | Session boundary (during Command/Snapshot translation) | Session |

### What SimulationNode becomes

A thin Godot bridge (~100 lines):
1. Creates the appropriate `IGameSession` implementation based on game mode
2. Exposes the session to the scene tree (so GDScript can access it)
3. Handles Godot lifecycle (`_Ready`, `_ExitTree`)

Essentially a factory + accessor — no game logic, no state management.
