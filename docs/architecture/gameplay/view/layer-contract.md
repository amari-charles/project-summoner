# View Layer Contract

**Status:** CURRENT  
**Last Updated:** 2026-03-04

This document explains the View layer as a subgraph in the gameplay graph.

## Position In Gameplay Graph

```text
Input -> Session <- View
           |
           v
      Simulation
```

`View` depends on Session and consumes authoritative state/events.
It does not own gameplay authority.

## View Subgraph

```text
View
├── BattleScene (facade / composition root)
├── EntityManager (3D lifecycle + event routing)
│   ├── UnitVisual
│   ├── ProjectileVisual
│   ├── SummonerVisual
│   └── StateInterpolator
├── BattleHUD (independent 2D overlay)
├── BattleCamera (standalone)
└── BattlefieldEnvironment (standalone)
```

## Boundary Contract

### Inputs from Session

- `GetState()` read model for continuous values (positions, HP, mana, timers).
- `SimEventsEmitted` stream for discrete events (damage, death, hits, game-over).

### Outputs from View

- visual side effects only: animation, VFX, SFX triggers, HUD updates.
- no authoritative state mutation.

## Internal View Data Model

- **Pull continuous data:** shells self-sync each frame from `GetState()`.
- **Push discrete events:** `EntityManager` routes `SimEvents` to affected shells/components.
- **Interpolation:** `StateInterpolator` smooths render positions only for remote views.

## What Belongs In View

- mapping state/events to visual presentation;
- visual lifecycle of shells (`spawn/destroy/register`);
- render-only smoothing and cosmetic timing.

## What Does Not Belong In View

- damage/targeting/ability/cooldown rules;
- command validation/network authority;
- persistence/progression decisions.

## Invariants

1. View is read-only with respect to authoritative gameplay state.
2. View is session-type agnostic (`LocalSession`, `HostSession`, `ClientSession`).
3. Shells do not subscribe globally; event routing is centralized in `EntityManager`.
4. HUD remains independent from `EntityManager`.
5. Interpolation never changes simulation truth.

## Related Docs

- [README.md](README.md)
- [battlefield/README.md](battlefield/README.md)
- [../session/README.md](../session/README.md)
- [../../graph-of-graphs.md](../../graph-of-graphs.md)
