# View Layer

> **Implementation-detail review pending:** The boundary rules and component
> ownership in this document remain current. Some linked component pages still
> contain pre-implementation “Today” and stub sections; their reconciliation is
> tracked in `docs/tracking/todos.md`.

Reads game state and renders it. No game logic, no mutation — purely visual.

For the boundary-level contract (graph position, edge types, invariants), read [layer-contract.md](layer-contract.md) first.

## Naming Convention

**Role-based suffixes, not Godot node types.** This is a 2.5D game — the `3D` suffix reflects Godot plumbing, not the game's visual paradigm. Names describe what components DO.

| Component | Role | Old Name |
|-----------|------|----------|
| `BattleScene` | Top-level facade: owns all battle visual components | `GameController3D` |
| `EntityManager` | Entity lifecycle + event dispatch + registry | `GameView` / `BattleSceneManager` |
| `UnitVisual` | Visual shell for one unit | `Unit3D` |
| `ProjectileVisual` | Visual shell for one projectile | `ProjectileView` |
| `SummonerVisual` | Visual shell for one summoner | Visual code in `summoner.gd` |
| `BattleHUD` | 2D battle overlay | *(unchanged)* |
| `BattleCamera` | Camera controller | `CameraController3D` |
| `BattlefieldEnvironment` | Biome visuals, sky, ground | `BattlefieldVisuals3D` |
| `VFXManager` | VFX pooling + spawning service | *(unchanged)* |

**Rule:** No `3D` suffix on View components. Use the component's role as the name.

## BattleScene Facade

`BattleScene` is the top-level C# class (`Node3D`) that owns all battle visual components. It replaces `GameController3D` as a thin typed facade — no game logic, just wiring and accessors.

```csharp
// scripts/csharp/Battle/View/BattleScene.cs
public partial class BattleScene : Node3D
{
    private IGameSession? _session;

    // Child components — resolved from scene tree
    public EntityManager EntityManager { get; private set; } = null!;

    /// Wires IGameSession to EntityManager and BattleHUD.
    /// Camera and Environment are state-independent.
    public void Initialize(IGameSession session)
    {
        throw new NotImplementedException();
    }
}
```

**Today:** `GameController3D` (1048 lines) mixes this wiring role with game logic, UI orchestration, and state management. `BattleScene` is what remains after game logic moves to Session.

## Data Model: Hybrid (Pull Continuous + Push Discrete)

- **Shells pull** their own continuous state (position, HP, animation) each frame via `_PhysicsProcess` reading `IGameSession.GetState()`
- **EntityManager pushes** discrete events (attack, damage, death) by routing SimEvents to the correct shell via registry lookup
- **EntityManager handles lifecycle** — spawns/destroys shells when entities appear/disappear in MatchState
- **BattleHUD** reads `IGameSession` independently, not mediated by EntityManager

## Component Map

```
BattleScene (top-level facade, wires everything to IGameSession)
 |
 |-- .EntityManager -----> EntityManager (lifecycle + event dispatch + registry)
 |                              |-- spawns/destroys --> UnitVisual (self-syncing shell)
 |                              |-- spawns/destroys --> ProjectileVisual (self-syncing shell)
 |                              |-- registers -------> SummonerVisual (self-syncing shell, at battle init)
 |                              |-- routes events to --> UnitVisual / ProjectileVisual / SummonerVisual
 |                              |-- calls for env VFX --> VFXManager
 |
 |-- .Hud --------------> BattleHUD (independent 2D overlay, reads IGameSession)
 |-- .Camera ------------> BattleCamera (pan/zoom/shake, standalone)
 |-- .Environment -------> BattlefieldEnvironment (sky/ground/biome, standalone)
```

`BattleScene` owns all four top-level components. `EntityManager` and `Hud` both read `IGameSession` but have no dependency on each other. `Camera` and `Environment` are state-independent.

## Component Docs

| Component | Doc |
|-----------|-----|
| View Layer Contract | [layer-contract.md](layer-contract.md) |
| EntityManager | [battlefield/](battlefield/) |
| UnitVisual | [battlefield/unit-visual.md](battlefield/unit-visual.md) |
| ProjectileVisual | [battlefield/projectile-visual.md](battlefield/projectile-visual.md) |
| SummonerVisual | [battlefield/summoner-visual.md](battlefield/summoner-visual.md) |
| BattleHUD | [battle-hud.md](battle-hud.md) |
| BattleCamera | [battle-camera.md](battle-camera.md) |
| BattlefieldEnvironment | [battlefield-environment.md](battlefield-environment.md) |
| VFXManager | [battlefield/vfx-manager.md](battlefield/vfx-manager.md) |

## Invariants

1. **Read-only.** View never writes to MatchState.
2. **Session-agnostic.** No `if is_host` branches. Reads `IGameSession`, doesn't know the session type.
3. **No game logic.** No damage calculation, targeting, cooldowns, or behavior decisions.
4. **Shells self-sync.** UnitVisual, ProjectileVisual, and SummonerVisual read their own state each frame. EntityManager doesn't push per-frame data.
5. **EntityManager owns 3D lifecycle.** Only it spawns/destroys battlefield shells (and registers summoner shells at init).
6. **BattleHUD is independent.** Reads `IGameSession` directly, not mediated by EntityManager.
7. **Event dispatch is centralized.** SimEvent-to-visual routing goes through EntityManager. Shells don't subscribe to `SimEventsEmitted` directly. (BattleHUD is the exception for HUD events.)
8. **Naming follows role.** No `3D` suffix. Names describe what the component does.

## Historical Decomposition Plan

The completed View/Input migration plan is archived at
`docs/archive/superseded-runtime-architecture-2026-08/view-input-decomposition-specs.md`.

## Supporting Services

| Service | Status |
|---------|--------|
| `VFXManager` | Documented — [battlefield/vfx-manager.md](battlefield/vfx-manager.md) |
| `AudioManager` | Deferred — placement in layer model is an [open question](../../decisions.md#b-audiomanager-placement) |

## Client Interpolation

`EntityManager` owns a shared `StateInterpolator` that smooths remote unit render positions.
It does not mutate `MatchState`; interpolation is render-only and consumed by `UnitVisual`.
