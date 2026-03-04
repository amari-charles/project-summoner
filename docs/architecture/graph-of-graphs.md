# Graph-Of-Graphs Model

**Status:** CURRENT  
**Last Updated:** 2026-03-04

This is the shared vocabulary for describing Fateforged architecture.

## Why This Exists

The codebase is not best described as one flat layer diagram.
It is a **graph of graphs**:

- the root architecture is a graph;
- each major node can expand into its own subgraph;
- some subgraphs are layered internally.

## Projections (Use The Right One)

When discussing architecture, always state which projection you mean:

1. **Containment projection (tree view)**: what subgraphs/components are inside a node.
2. **Dependency projection (call/reference direction)**: who depends on whom.
3. **Data/event flow projection**: where authoritative state/events originate and where they are consumed.

These projections are complementary; they are not interchangeable.

## Root Map (Current)

```text
SystemRoot
├── ApplicationGraph
├── GameplayGraph
│   ├── Simulation
│   ├── Session
│   ├── View
│   └── Input
├── MetaGraph
└── InfrastructureGraph
```

## Gameplay Example

### Containment projection

```text
GameplayGraph
├── Simulation (subgraph)
├── Session (subgraph)
├── View (subgraph)
└── Input (subgraph)
```

### Dependency projection

```text
Input -> Session <- View
           |
           v
      Simulation
```

### Authoritative data/event flow projection

```text
Simulation -> Session -> View
```

## Terms

| Term | Meaning |
|---|---|
| **Node** | A component or subsystem in a graph. |
| **Subgraph** | A node expanded into internal structure. |
| **Layer** | A subgraph with explicit boundary contract + constrained dependency direction. |
| **Contract** | Inputs/outputs a node exposes across its boundary. |
| **Boundary crossing** | Any edge that crosses subgraph/layer boundary. |
| **Projection** | A chosen view (containment, dependency, data flow). |

## Edge Types (Label Explicitly)

| Edge Type | Meaning | Example |
|---|---|---|
| `depends_on` | Compile/runtime reference | `View depends_on Session` |
| `owns` | Lifecycle ownership | `BattleScene owns EntityManager` |
| `reads` | Pulls read-only state | `UnitVisual reads MatchState via IGameSession` |
| `emits` | Pushes discrete events | `Session emits SimEventsEmitted` |
| `routes` | Forwards/dispatches | `EntityManager routes SimEvents to shells` |

## Navigation Guide

Use this order when understanding or changing architecture docs:

1. Start with [target-architecture.md](target-architecture.md) for high-level gameplay boundaries.
2. Read [application-layer.md](application-layer.md) for scene/lifecycle orchestration.
3. Read [gameplay/README.md](gameplay/README.md) for gameplay subgraph map.
4. Drill into subgraph docs (`gameplay/simulation`, `gameplay/session`, `gameplay/view`, `gameplay/input`).

## Change Description Template

Use this format in PRs/docs when proposing architecture changes:

1. **Projection:** containment / dependency / data flow.
2. **Nodes touched:** list explicit nodes/subgraphs.
3. **Boundary crossings added/removed:** list edges.
4. **Contract changes:** list API/signal/state changes.
5. **Invariant check:** confirm unchanged invariants or document intentional changes.

## Global Invariants

1. Simulation remains deterministic and transport-agnostic.
2. Session is the gameplay orchestration boundary.
3. View is read-only with respect to authoritative game state.
4. Input and View do not depend on each other directly.
5. Infrastructure does not depend on Gameplay or Meta.
