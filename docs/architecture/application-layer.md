# Application Layer

**Status:** CURRENT  
**Last Updated:** 2026-08-24

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
| `typed_battle_data.gd` | Typed GDScript projection of authored battle definitions |
| `battle_surface_router.gd` | Chooses the standard or Debug Arena runtime surface from authored data |
| `post_battle_report.gd` | Normalizes encounter and authored-battle outcomes for the shared Results screen |
| `navigation_context.gd` | Return-stack for nested navigation flows |
| `capability_manager.gd` | Fine-grained gameplay capability gating |

`scripts/csharp/Application/Narrative/NarrativeDirector.cs` owns typed narrative
cue matching, deterministic queueing, occurrence policy, and playback
orchestration at the same application boundary.

## Boundary Contracts

### Inbound (from UI/screens)

- quest, encounter, and direct authored-battle launch intents;
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

### Quest Encounter Flow

1. A professor or world interaction selects an encounter referenced by a quest step.
2. Encounter Preparation asks `EncounterService` for the loadout and authored battle configuration.
3. The screen configures `BattleContext` in `ENCOUNTER` mode and transitions to the battle scene.
4. Battle completion returns one normalized report to the shared Results screen; `EncounterService` advances the quest-owned encounter state.

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
