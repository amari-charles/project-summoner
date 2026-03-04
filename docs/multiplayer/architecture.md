# Multiplayer Architecture

**Status:** CURRENT  
**Last Updated:** 2026-03-04

This document defines multiplayer architecture using a recursive graph model.

Shared terminology source: [../architecture/graph-of-graphs.md](../architecture/graph-of-graphs.md).

## Terminology (Updated)

- **Architecture graph**: directed dependency graph between components.
- **Subgraph**: a node expanded into its own internal graph.
- **Layer**: a subgraph with a clear boundary contract and constrained dependency direction.
- **Architecture tree view**: a containment view of subgraphs (what contains what).

A layer is not only a flat box. A layer node can expand into another graph.

## Graph-Of-Graphs Model

At the top, the project has a root architecture graph. Gameplay is one subgraph inside it.

```text
SystemRoot
└── GameplayGraph
    ├── Simulation
    ├── Session
    └── View
```

Non-gameplay subgraphs (meta/progression/tooling) exist at the root too, but are outside this doc's scope.

## Gameplay Subgraph

Gameplay is modeled as layered subgraphs, each with its own internal graph.

```text
GameplayGraph
├── Simulation (subgraph)
├── Session (subgraph)
└── View (subgraph)
```

Two directions must be distinguished:

1. **Dependency direction (downward)**: `View -> Session -> Simulation`
2. **Authoritative data/event flow (upward)**: `Simulation -> Session -> View`

This avoids confusion when people say "Simulation -> Session -> View" (data ownership flow) vs "View depends on Session" (code dependency flow).

## Session Subgraph (Multiplayer Focus)

```text
IGameSession
├── LocalSession
└── NetworkSession (abstract)
    ├── HostSession
    └── ClientSession
```

Session-owned internals:

```text
Battle/Session/
├── Transport/           (network transport adapters)
├── Protocol/            (wire messages + serialization)
├── HostSession.cs       (authoritative tick + broadcast)
├── ClientSession.cs     (submit commands + apply snapshots)
├── NetworkSession.cs    (shared MP infrastructure)
└── LocalSession.cs      (offline/local orchestration)
```

## View Subgraph (Example)

`View` is also a layer-subgraph, not a leaf node:

```text
View
├── BattleScene
├── EntityManager
│   ├── UnitVisual
│   ├── ProjectileVisual
│   ├── SummonerVisual
│   └── StateInterpolator
└── HUD/UI components
```

`StateInterpolator` is therefore view-layer behavior inside the `View` subgraph, not simulation/session logic.

## Layer Contracts

- **Simulation contract**: consume commands + produce `MatchState` and `SimEvent`s.
- **Session contract**: `IGameSession` boundary (`SubmitCommand`, `Tick`, `GetState`, `SimEventsEmitted`) plus transport/authority policy.
- **View contract**: read state/events, render/polish only, never mutate authoritative state.

### Reconnect Ownership (Session Policy)

- Reconnect timers/grace windows are owned by `HostSession`/`ClientSession`, not by simulation or view.
- Timeout winner is decided by disconnect origin:
  - peer timeout => local side wins;
  - local transport timeout => local side loses.
- View only displays reconnect state via `BattleScene` signals.

## Invariants (Must Stay True)

1. Networking stays in Session, not Simulation.
2. Visual behavior stays in View, not Session.
3. Simulation remains deterministic and transport-agnostic.
4. Input and View both depend on Session, never directly on each other.
5. Local and multiplayer paths share the same session-facing API (`IGameSession`).
6. Any layer may expand into a subgraph, but its external contract must remain stable.
