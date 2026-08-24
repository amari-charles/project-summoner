# Application Layer

**Status:** CURRENT  
**Last Updated:** 2026-08-05

The Application layer orchestrates scene lifecycle and cross-domain handoffs.
It is above gameplay/meta domain internals and below product-level UX flows.

## Position In Root Graph

```text
SystemRoot
├── ApplicationGraph   <-- this document
├── GameplayGraph
├── MetaGraph
└── InfrastructureGraph
```

## Core Responsibility

Coordinate runtime flow between subsystems without owning gameplay or progression rules.

Examples:

- configure context before scene transition;
- transition scenes safely with cleanup/readiness checks;
- resume/pause scripted flows;
- carry navigation return intent.

## Non-Responsibilities

Application should not:

- implement combat mechanics (Simulation concern);
- implement session authority/network protocol rules (Session concern);
- implement rendering behavior (View concern);
- own persistent business rules (Meta service concern).

Application can **call** those systems, but should not duplicate their logic.

## Current Component Map (`scripts/application/`)

| Component | Role |
|---|---|
| `scene_manager.gd` | Canonical scene path registry + raw/coordinated transition API |
| `scene_coordinator.gd` | Transition orchestration: cleanup, service checks, readiness wait |
| `battle_context.gd` | Battle configuration + battle lifecycle state bag |
| `event_context.gd` | Active event configuration + completion state |
| `navigation_context.gd` | Return-stack for nested navigation flows |
| `NarrativeDirector.cs` | Typed narrative cue matching, deterministic queueing, occurrence policy, and playback orchestration |
| `capability_manager.gd` | Fine-grained gameplay capability gating |

## Boundary Contracts

### Inbound (from UI/screens)

- "start battle/event" intents;
- scene transition requests;
- dialogue/sequence progression requests.

### Outbound (to domains)

- scene changes to `SceneManager` / active scene nodes;
- context config consumed by gameplay (`BattleSessionConfig.FromBattleContext()`);
- service calls to meta systems (`Quests`, `Encounters`, etc.) when orchestrating completion.

## Common Flows

### Authored Battle Start

1. Screen configures `BattleContext`.
2. Screen calls `SceneManager.transition_to(SCENE_BATTLE_3D)`.
3. `SceneCoordinator` performs cleanup + service verification.
4. `BattleScene` reads typed config and initializes gameplay.

### Event Flow

1. Screen configures `EventContext`.
2. Scene transition to event screen.
3. `NarrativeDirector` matches any typed narrative cue; gameplay behavior remains with its authoritative owner.
4. `EventContext.complete_event()` updates event completion state.

### Multiplayer Battle Start

1. Lobby/match UI configures multiplayer fields in `BattleContext`.
2. Scene transition to battle scene.
3. Battle scene builds `BattleSessionConfig` and initializes `HostSession`/`ClientSession` path.

## Invariants

1. Application owns flow orchestration, not domain rules.
2. Context singletons are state bags, not business-logic engines.
3. Scene transitions use `SceneCoordinator` for complex flows.
4. Cleanup and readiness checks happen before considering a scene "ready".
5. Application changes must preserve subsystem boundary contracts.

## Related Docs

- [graph-of-graphs.md](graph-of-graphs.md)
- [gameplay/README.md](gameplay/README.md)
- [gameplay/view/README.md](gameplay/view/README.md)
- [target-architecture.md](target-architecture.md)
