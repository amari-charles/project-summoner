# Layered Architecture Migration

## Goal

Replace the monolithic architecture — `SimulationNode` (942 lines), `Unit3D` (2304 lines), `GameController3D` (1048 lines) — with four clean layers:

```
Input → Session → Simulation
                ↑
          View reads Session
```

Each layer has a single responsibility. Dependencies only flow downward. The simulation is pure C# with zero Godot imports. Session hides SP/MP differences behind `IGameSession`. View is read-only rendering. Input just produces Commands.

## Why

25 architectural issues drove this redesign: parallel game systems (DamageSystem vs SimDamage), god classes mixing logic with rendering, SP/MP code path divergence, leaky abstractions (NetworkId in simulation data). See [archived problem-analysis.md](../archive/rewrite-research-2026-02/problem-analysis.md) for the full history.

## Architecture Specs

| Doc | What It Covers |
|-----|----------------|
| [target-architecture.md](../architecture/target-architecture.md) | Primary layer spec — all four layers, boundaries, deletion blockers |
| [decisions.md](../architecture/decisions.md) | Settled architecture decisions (10 decisions, 6 invariants) |
| [game-requirements.md](../architecture/game-requirements.md) | Gameplay requirements the architecture must satisfy |
| [Simulation layer](../architecture/gameplay/simulation/README.md) | `Simulation`, `MatchState`, subsystems, SimulationNode transition plan |
| [Session layer](../architecture/gameplay/session/README.md) | `IGameSession` hierarchy, stubs, current equivalents |
| [Input layer](../architecture/gameplay/input/README.md) | `InputCollector` design |
| [View layer](../architecture/gameplay/view/) | `BattleScene`, `EntityManager`, all visual shells (9 docs) |

## Migration Backlog

- [architectural-issues.md](architectural-issues.md) — The 25 issues driving this migration, annotated with resolution status
- [planning-checklist.md](planning-checklist.md) — 8-phase planning checklist with checkboxes (persistent across sessions)
- [layer-map.md](layer-map.md) — Comprehensive system-to-layer assignment for every file in the codebase
- [cross-cutting-plan.md](cross-cutting-plan.md) — Migration plan for shared types, catalogs, and utilities (Cards/, Stats/, Constants/, etc.)
- [meta-game-plan.md](meta-game-plan.md) — Migration plan for meta-game services, domain objects, and GDScript facades
- [deletion-sequence.md](deletion-sequence.md) — Ordered deletion plan for retiring old systems (5 tiers, ~73 files, 10 autoloads)
- [documentation-guide.md](documentation-guide.md) — Principles for architecture documentation

---

## Migration Status

### Completed

- [x] Simulation layer — all subsystems (`SimDamage`, `SimEffects`, `SimProjectile`, `SimAbility`, `SimBehavior`, `SimTargeting`)
- [x] `BehaviorState` enum (replaces const ints — issue #19)
- [x] `Team` value type stub
- [x] Dead ability system deleted (`BaseAbility.cs`, `SlowOnHitAbility.cs`, `IAbilityConfig.cs`)
- [x] Dead `AuthorityProvider` signals removed
- [x] Session stubs created (`LocalSession`, `NetworkSession`, `HostSession`, `ClientSession`, `CommandRouter`, `IdentityMap`, `SnapshotCodec`)
- [x] View + Input stubs created (`BattleScene`, `IGameSession` interface)
- [x] Architectural issues annotated with resolution status

### Blocked (waiting on View layer migration)

- [ ] Delete `DamageSystem.cs` — Unit3D still uses DamageSystem for Godot-side damage
- [ ] Delete `ModifierService` — Unit3D applies modifiers through it
- [ ] Delete `ProjectileService` — RangedUnit3D, DamageEffect reference it
- [ ] Remove `NetworkId`/`TargetNetworkId` from `UnitData` — needs IdentityMap at session boundary first

### Not Started — Session Layer

- [ ] Implement `LocalSession` (replaces direct SimulationNode path)
- [ ] Implement `HostSession` (replaces `HostRunner`)
- [ ] Implement `ClientSession` (replaces `ClientRunner`)
- [ ] Implement `CommandRouter` (replaces `RequestValidator`)
- [ ] Implement `IdentityMap` (replaces `NetworkIdRegistry`)
- [ ] Implement `SnapshotCodec` (replaces `StateSnapshotBuilder`)

### Not Started — View Layer

- [ ] Implement `EntityManager`
- [ ] Implement `UnitVisual` (replaces `Unit3D`)
- [ ] Implement `ProjectileVisual` (replaces `Projectile3D`)
- [ ] Implement `BattleScene` (replaces `GameController3D`)
- [ ] Implement `BattleHUD` self-polling
- [ ] Decompose `SimulationNode` into thin bridge
- [ ] Decompose `GameController3D` into `BattleScene`

### Not Started — Input Layer

- [ ] Implement `InputCollector`

---

## Key Transition Tables

### SimulationNode → Target Components

`SimulationNode.cs` (942 lines) is the current god class. Here's what moves where:

| Current Responsibility | Target Component | Layer |
|----------------------|-----------------|-------|
| Simulation ticking | `LocalSession.Tick()` / `HostSession.Tick()` | Session |
| Snapshot application (client mode) | `ClientSession.ApplySnapshot()` | Session |
| Unit ID management (`_claimedSimUnitIds`, `_unit3DBySimId`, `_nextClientUnitId`) | `IdentityMap` | Session |
| Event emission (`OnTickCompleted`) | Session fires `SimEventsEmitted` | Session |
| Card data population (lines 298-314) | Stays — initialization concern | Simulation |
| Team/coordinate remapping | Session boundary (during Command/Snapshot translation) | Session |

**What SimulationNode becomes:** A thin Godot bridge (~100 lines) — factory + accessor, no game logic, no state management.

### Current → Target Equivalents (Session)

| Target | Current File | Notes |
|--------|-------------|-------|
| `LocalSession` | *(none)* | SP goes directly through SimulationNode |
| `NetworkSession` | `MatchSession` (partial) | `Multiplayer/Core/MatchSession.cs` |
| `HostSession` | `HostRunner` | `Multiplayer/Authority/HostRunner.cs` |
| `ClientSession` | `ClientRunner` | `Multiplayer/Client/ClientRunner.cs` |
| `CommandRouter` | `RequestValidator` | `Multiplayer/Authority/RequestValidator.cs` — client-only today |
| `IdentityMap` | `NetworkIdRegistry` | `Multiplayer/Core/NetworkIdRegistry.cs` — maps Nodes, not ints |
| `SnapshotCodec` | `StateSnapshotBuilder` | `Multiplayer/Sync/StateSnapshotBuilder.cs` |
| `DesyncChecker` | `DesyncDetector` | `Multiplayer/Sync/DesyncDetector.cs` — rename only |

### View Naming (Old → New)

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

### Unit3D → UnitVisual (Keeps/Loses)

`Unit3D` is 2304 lines mixing game logic with rendering (issue #23).

**Keeps (~1100 lines):**
- Visual component setup (IVisualComponent, shadow, HP bar)
- Animation updates
- Position sync from state

**Loses (~1200 lines):**
- `UpdateTargeting()` → SimTargeting
- `UpdateBehavior()` → SimBehavior
- `UpdateCooldowns()` → sim subsystems
- Trigger system → SimAbility
- `TakeDamage()` game logic path → SimDamage
- Signal subscriptions to SimulationNode
- `IsSimDriven` flag — all units are sim-driven, no branching

**Event reactions (called by EntityManager):**

| Method | Triggered By |
|--------|-------------|
| `PlayAttackAnimation()` | UnitAttackedEvent |
| `FlashDamage()` | UnitDamagedEvent |
| `BeginDeath()` | UnitDiedSimEvent |
| `ShowBuffIcon(buff)` | BuffAppliedSimEvent |
| `ShowEvadeText()` | AttackEvadedEvent |

### Deletion Blockers

| File to Delete | Status | Blocked By |
|---------------|--------|-----------|
| `scripts/csharp/Battle/Simulation/Combat/DamageSystem.cs` + `.tscn` | Blocked | Unit3D still uses DamageSystem for Godot-side damage |
| `scripts/csharp/Systems/Modifiers/ModifierService.cs` | Blocked | Unit3D applies modifiers through it |
| `scripts/csharp/Projectiles/ProjectileService.cs` | Blocked | RangedUnit3D, DamageEffect reference it |
| `scripts/csharp/Abilities/BaseAbility.cs` | **Deleted** | Was dead code — removed in architecture gap audit |
| `scripts/csharp/Abilities/SlowOnHitAbility.cs` | **Deleted** | Was dead code — removed in architecture gap audit |
| `scripts/csharp/Abilities/IAbilityConfig.cs` | **Deleted** | Was dead code — removed in architecture gap audit |

The 3 remaining blocked deletions unblock when `UnitVisual` replaces `Unit3D`.
