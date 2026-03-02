# Migration Planning Checklist

Persistent checklist for the four-layer migration. Organized into 8 dependency-ordered phases. Each phase has a context block so a new AI session can pick up where the previous one left off.

**How to use:** Check boxes as items are completed. Phases depend on earlier phases — don't start Phase N until its dependencies are satisfied (or explicitly unblocked).

---

## Phase 1: Resolve Open Architectural Decisions

**Goal:** Make the decisions that block layer assignment for ~20 systems.

**Depends on:** Nothing

**Context for new sessions:** Read `docs/architecture/decisions.md` (decisions #9–#16), `docs/migration/layer-map.md` (Unresolved section), and this checklist.

### Open Questions from `decisions.md`

- [x] **Decision A — Targeting Visuals** → Input owns state machine, View renders visuals (Decision #9)
- [x] **Decision B — AudioManager** → Standalone service outside layers (Decision #10)
- [x] **Decision C — Unit-Type-Specific Logic** → Composition / data-driven strategies (Decision #11)

### Additional Decisions Surfaced by Gap Analysis

- [x] **AI System** → Input-layer peer, submits commands via `IGameSession.SubmitCommand()` (Decision #12)
- [x] **BattleContext** → Typed `BattleConfig` passed to session constructors (Decision #13)
- [x] **CardFactory** → Cross-cutting utility, base stats only (Decision #14)
- [x] **GameStateEvents** → Keep for non-battle, `SimEventsEmitted` for battle; revisit in Phase 6 (Decision #15)
- [x] **BattleRNG** → Isolated sim `DeterministicRng` + separate View `DeterministicRng`; GDScript `BattleRNG` retired for gameplay (Decision #16)

---

## Phase 2: Complete the Layer Map

**Goal:** Every system in the codebase has a target layer assignment (or is explicitly marked "Unresolved pending Phase 1").

**Depends on:** Phase 1 (resolved decisions unblock assignments)

**Context for new sessions:** Read `docs/migration/layer-map.md` and this checklist. The layer map was created with initial assignments; this phase fills gaps and resolves contradictions.

- [x] Apply Decision A results — targeting systems assigned (Input state + View renders)
- [x] Apply Decision B results — AudioManager assigned (Standalone service section)
- [x] Apply Decision C results — composition approach documented in decisions.md #11
- [x] Assign AI system files — added to Input Layer section
- [x] Assign BattleContext — added to Infrastructure (Scene Navigation and Battle Setup)
- [x] Assign CardFactory — confirmed cross-cutting
- [x] Assign GameStateEvents — confirmed meta-game (kept for non-battle)
- [x] Assign BattleRNG — added to Delete Queue (retired for gameplay)
- [x] Verify every autoload in `project.godot` appears in the layer map (67/67 covered)
- [x] Verify every directory under `scripts/csharp/` is covered (27/27 directories)
- [x] Verify every file under `scripts/` (GDScript) is covered (all files mapped)
- [x] Cross-reference with `docs/architecture/target-architecture.md` for contradictions — none found
- [x] Cross-reference with `docs/migration/architectural-issues.md` for missed items — all 25 issues covered

---

## Phase 3: Design Missing Session-Layer Specs

**Goal:** Architecture specs exist for every Session-layer component — not just stubs, but design docs covering responsibilities, invariants, and edge cases.

**Depends on:** Phase 2 (need to know what goes in Session)

**Context for new sessions:** Read `docs/architecture/gameplay/session/README.md` (existing stubs and hierarchy), `docs/migration/README.md` (current migration status), and the Session section of `docs/migration/layer-map.md`.

All specs documented in `docs/architecture/gameplay/session/design-specs.md`.

- [x] **AI Command Submission** — AI is Input peer, calls `IGameSession.SubmitCommand()` (§1)
- [x] **StateInterpolator Design** — Owned by ClientSession, runs after snapshot application (§2)
- [x] **Deterministic RNG in Session** — Host generates seed, sends in MatchStarted. Sim RNG isolated. (§3)
- [x] **Client Prediction Design** — Predict mana + card removal. PredictionBuffer tracks pending. Rollback on rejection. (§4)
- [x] **BattleConfig → Session Initialization** — Typed `BattleConfig` passed to constructors. `BattleResultHandler` handles aftermath. (§5)
- [x] **CommandRouter Validation Rules** — Full rule set: player index, card index, mana, phase, casting state (§6)
- [x] **MatchSession Retirement Plan** — Transfer table, migration order, deletion list (§7)
- [x] **ReconnectionHandler Migration** — Owned by NetworkSession, no longer singleton (§8)

---

## Phase 4: Design Missing View/Input Specs

**Goal:** Architecture specs exist for every View and Input component that needs decomposition or retirement.

**Depends on:** Phase 3 (Session specs define what View/Input interact with)

**Context for new sessions:** Read `docs/architecture/gameplay/view/README.md`, `docs/architecture/gameplay/input/README.md`, the View/Input sections of `docs/migration/layer-map.md`, and `docs/architecture/target-architecture.md` §4-6.

All specs documented in `docs/architecture/gameplay/view/design-specs.md`.

### View/Input Components Needing Specs

- [x] **HandUI Split** — ~85% View (rendering, animation, glow), ~15% Input (drag gesture). Drag stays on CardDisplay; Command production moves to InputCollector. (§1)
- [x] **SpellTargetingManager Retirement** — State machine + gesture → InputCollector. Circle/arrow preview → View. Autoload removed. (§2)
- [x] **RedirectManager → Command** — New `RedirectCommand` type. Gesture → InputCollector. Cooldowns + forced targeting → Simulation. Visuals → View. Autoload removed. (§3)
- [x] **SpawnPreview Migration** — View-layer component. Reads InputCollector drag state. No structural changes. (§4)
- [x] **Summoner Decomposition** — Splits into: Session init (BattleConfig), MatchState (mana/HP/hand), SummonerVisual (View), InputCollector (command production). `summoner.gd` retired. (§5)
- [x] **GameController3D Decomposition** — Init → Session construction. Game flow → Session.Tick(). View wiring → BattleScene. Redirect input → InputCollector. (§6)
- [x] **SimEventSignalEmitter Retirement** — EntityManager subscribes to `SimEventsEmitted` directly. Signal-based bridge deleted. All 17+ event types handled. (§7)
- [x] **BattlefieldDropZone Migration** — Drop validation + Command production → InputCollector. Preview management → View. Drop zone Control removed. (§8)
- [x] **GameUI Migration** — ~95% View, becomes BattleHUD. Reads MatchState instead of signals. No decomposition needed. (§9)
- [x] **SpawnZoneOverlay Migration** — Pure View. Visibility driven by InputCollector drag state. No changes needed. (§10)

---

## Phase 5: Plan Cross-Cutting Type Migration

**Goal:** Migration plan for shared types, data catalogs, and utilities that don't belong to a single layer.

**Depends on:** Phase 2 (layer assignments known)

**Context for new sessions:** Read `docs/migration/layer-map.md` (Cross-cutting section), the `scripts/csharp/Cards/` directory listing, `scripts/csharp/Data/` directory listing, and `scripts/csharp/Constants/` directory listing.

All specs documented in `docs/migration/cross-cutting-plan.md`.

### Cards/ Decomposition

- [x] **Card Definition Types** — Pure data, cross-cutting, stay. `Card.cs` will split into `SimCardData` (sim) + `Card` Resource (UI) when Session is implemented. (§1)
- [x] **Card Configs** — Godot Resources, stay as cross-cutting editor data. No migration needed. (§2)
- [x] **Card Effects System** — **Must be rewritten** as simulation-internal spell processing. Current effects manipulate Godot nodes directly (TakeDamage, Set("rally_point"), ProjectileService, VFXManager). Blocked by sim absorbing spell logic. (§3)
- [x] **Card Spawning** — Pure data definitions (`SummonSpec`, `UnitSpawnEntry`, `SpawnPlacement`). Cross-cutting, stay. (§4)
- [x] **Card Formations** — Pure math, moves to simulation. `Vector3` → `SimVector3` when sim handles spawning. (§5)
- [x] **CardFactory Migration** — Shrinks to stats-only utility once sim handles spawning + effects. Currently 678 lines → target ~50 lines. (§6)
- [x] **CardCatalog / CardCatalogBridge** — Keep bridge pattern. GDScript UI depends on `card_catalog.gd`; C# uses `CardCatalog.cs`. Consolidates only if/when UI migrates to C#. (§7)

### Other Cross-Cutting Concerns

- [x] **UnitStatCalculator Dependencies** — Sim-layer logic with GDScript interop bolted on. Calculator moves to sim namespace; `UnitStats`/`StatKey` stay cross-cutting. Godot interop methods removed when GDScript callers retire. (§8)
- [x] **Constants Layer Assignment** — `BattlefieldBounds` + `ElementMatchups` → Simulation. `ElementColors` → View. `GroupIDs` + `UnitId` → Cross-cutting. Namespace moves only. (§9)
- [x] **Capabilities Interfaces** — Delete with Unit3D (Phase 7, Tier 2). No purpose once visual shells replace Unit3D. (§10)
- [x] **SpatialGrid Migration** — Delete when all targeting is in simulation. <50 units makes spatial hashing unnecessary; sim iterates MatchState directly. (§11)
- [x] **GDScript Data Catalogs** — Keep unchanged. GDScript UI depends on them. Bridge pattern works. (§12)
- [x] **GDScript Constants** — Keep unchanged. Continue mirror enum pattern for new enums. (§13)

---

## Phase 6: Plan Meta-Game Service Migration

**Goal:** Migration plan for services, domain objects, and GDScript facades that exist outside the battle loop.

**Depends on:** Phase 2 (layer assignments known)

**Context for new sessions:** Read `docs/migration/meta-game-plan.md` for the full plan. Read `docs/migration/layer-map.md` (Meta-game section) for layer assignments.

All specs documented in `docs/migration/meta-game-plan.md`.

### C# Services

- [x] **Campaign Service** — Stays. Meta-game, no sim deps. (§1)
- [x] **Card Service** — Stays. Meta-game, no sim deps. (§1)
- [x] **Deck Service** — Stays. Meta-game, no sim deps. (§1)
- [x] **Economy Service** — Stays. Meta-game, no sim deps. (§1)
- [x] **Item Service** — Stays. Meta-game, no sim deps. (§1)
- [x] **Reward Service** — Stays. Caller changes from BattleContext to BattleResultHandler. (§1, §6)
- [x] **Shop Service** — Stays. Meta-game, no sim deps. (§1)
- [x] **Summoner Services** — Stays. Meta-game, no sim deps. (§1)
- [x] **HPBarService** — Retire as autoload. Each visual shell creates its own HP bar. (§1)
- [x] **LevelCapService** — Stays. Cross-cutting data lookup. (§1)
- [x] **Service Interfaces** — `IDamageSystem` + `IModifierService` delete with implementations. `ICardFactory` review later. (§1)

### Domain Layer

- [x] **Profile Data** — Pure C# domain objects, no Godot deps. No migration needed. (§2)
- [x] **Infrastructure** — Persistence stays as infrastructure layer. (§2)

### GDScript Service Facades

- [x] **C#-wrapping facades** (7 files) — Stay until Phase 8 GDScript UI migration. (§3)
- [x] **DialogueManager** — Stays as GDScript. Orchestrates GDScript UI flows. (§3)
- [x] **EventSequencer** — Stays as GDScript. Drives GDScript event sequences. (§3)
- [x] **CapabilityManager** — Stays as GDScript. Meta-game feature flags. (§3)
- [x] **SceneCoordinator / SceneManager / NavigationContext** — Infrastructure, stays. (§3)

### Cross-Cutting Concerns

- [x] **GameStateEvents Replacement** — Rename to MetaGameEvents, typed C# events. Phase 8 execution. (§4)
- [x] **Battle Launch Path** — BattleContext builds typed BattleConfig internally. Services unchanged. (§5)
- [x] **Battle Completion Path** — BattleResultHandler replaces BattleContext callbacks. Services unchanged. (§6)

### Billing

- [x] **Billing System** — Infrastructure. 5 files, platform billing integration. No migration needed. (§7)

---

## Phase 7: Plan Old System Deletion Sequence ✅

**Goal:** Ordered deletion plan with exact blockers and verification steps for every system being retired.

**Depends on:** Phases 3-6 (need to know what replaces each system)

**Context for new sessions:** Read `docs/migration/deletion-sequence.md` for the full plan. Read `docs/migration/layer-map.md` (Delete section) and `docs/migration/architectural-issues.md` for background.

**Deliverable:** [`docs/migration/deletion-sequence.md`](deletion-sequence.md) — complete deletion plan with 5 tiers, ~94 files (~19,220 LOC), 11 autoloads, 26 scene updates, verification checklists per tier.

### No Tier 0

- [x] **Confirmed no unblocked deletions.** BattleRNG has 4 active GDScript consumers (`heuristic_ai.gd`, `summoner.gd`, `online_screen.gd`, `multiplayer_lobby.gd`) — assigned to Tier 4.

### Tier 1 — Delete After UnitVisual Replaces Unit3D

- [x] **DamageSystem.cs** + `DamageSystem.tscn` + `IDamageSystem.cs` — 3 files, autoload. Resolves issue #1.
- [x] **ModifierService.cs** + `ModifierService.tscn` + `IModifierService.cs` + 6 supporting files — autoload. Resolves issue #2.
  - `StatModifier.cs` and `TriggerCondition.cs` **DO NOT DELETE** — relocate to `scripts/csharp/Stats/` (27+ consumers).
- [x] **ProjectileService.cs** + `ProjectileService.tscn` — autoload. Resolves issue #3.
- [x] 3 autoloads removed. Test: `ModifierServiceTest.cs` deleted; `StatModifierTest.cs` + `TriggerConditionTest.cs` relocated.

### Tier 2 — Delete After View Layer Migration

- [x] **HPBarService.cs** + `HPBarService.tscn` — autoload. Shell-owned HP bars replace it.
- [x] **SimEventSignalEmitter.cs** — replaced by EntityManager reading `SimEventsEmitted` directly.
- [x] **SimulationNode.cs** slim-down — ~842 lines removed, ~100 stay as thin bridge.
- [x] **Unit3D** + `MeleeUnit3D` + `RangedUnit3D` + `DucklingUnit3D` + `UnitHealth` + `UnitMovement` — 20 scene files updated. Resolves issue #23.
- [x] **Projectile3D** + `ProjectileData` — 1 scene file updated. Resolves issue #24.
- [x] **game_controller_3d.gd** + `test_game_controller.gd` — battle scene + test scenes updated. Resolves issue #25.
- [x] **Cards/Effects/** (14 files, ~1,300 LOC) — entire directory deleted. Blocked by simulation absorbing spell logic (PlayCardCommand processing).
- [x] **SpellCard.cs** (71 LOC) — old spell execution path, deleted with Effects.
- [x] **summoner.gd** (979 LOC) — replaced by SummonerVisual. Scene files referencing summoner.gd need update.
- [x] **UnitSpawner.cs** (419 LOC) — replaced by simulation handling spawns + EntityManager creating UnitVisual.
- [x] **UnitSteering.cs** (462 LOC) — replaced by simulation handling movement.
- [x] **SpawnRevealComponent.cs** (240 LOC) — Unit3D component, dies with Unit3D.
- [x] **UnitDebugService.cs** (67 LOC) — Unit3D debug overlay. Also an autoload — remove from project.godot.
- [x] **SpawnPreview.cs + GhostUnit3D.cs** — UPDATE (stay, rewrite to use UnitVisual patterns).
- [x] **CardFactory.cs** — UPDATE (remove ModifierService references in Tier 1, SpatialGrid references in Tier 5).
- [x] 2 autoloads removed. `ClientInitializationTest.cs` updated. Resolves issues #7, #23, #24, #25.

### Tier 3 — Delete After Session Layer Migration

- [x] **HostRunner.cs**, **ClientRunner.cs** — replaced by `HostSession`, `ClientSession`.
- [x] **MatchSession.cs** — replaced by `NetworkSession`.
- [x] **RequestValidator.cs** — replaced by `CommandRouter`.
- [x] **NetworkIdRegistry.cs** — replaced by `IdentityMap`.
- [x] **StateSnapshotBuilder.cs** — replaced by `SnapshotCodec`.
- [x] **DesyncDetector.cs** — rename + rewrite to `DesyncChecker` (reads `MatchState` only).
- [x] **ReconnectionHandler.cs** (373 LOC) — rewrite into `NetworkSession` (like DesyncDetector → DesyncChecker). See session design-specs §8.
- [x] **IMatchRunner.cs**, **IMessageBroadcaster.cs**, **HostEventBroadcaster.cs** — interfaces for old patterns.
- [x] 0 autoloads (none are autoloads). `SimEventCoverageTest.cs` + `BroadcastFieldTest.cs` updated.
- [x] Resolves issues #5, #6, #8, #9, #10, #11, #12, #13, #14, #15, #16, #21.

### Tier 4 — Delete After Input Layer Migration

- [x] **SpellTargetingManager** (`scripts/ui/battle/spell_targeting_manager.gd`, autoload) — retired when InputCollector handles spell targeting.
- [x] **RedirectManager** (`scripts/managers/redirect_manager.gd`, autoload) — retired when InputCollector handles redirect commands.
- [x] **BattlefieldDropZone** (`scripts/ui/battle/battlefield_drop_zone.gd`) — absorbed into InputCollector. 5 scene files updated.
- [x] **BattleRNG** (`scripts/multiplayer/rng/battle_rng.gd`, autoload) + `rng_domain.gd` — 4 GDScript consumers migrated to sim's `DeterministicRng`.
- [x] **player_input.gd** + **player_input_3d.gd** (`scripts/core/`) — replaced by InputCollector.
- [x] 3 autoloads removed.

### Tier 5 — Delete When Capabilities Retire

- [x] **Capabilities/** — 5 interfaces (135 LOC). Delete with Unit3D.
- [x] **Targeting/** — 17 files (982 LOC) + `TargetingConfigRegistryCS` autoload. Replaced by `SimTargeting`.
- [x] **Combat/Hitbox/** — 6 files (777 LOC) + `HitResolver` autoload. Replaced by `SimProjectile` + `SimDamage`.
- [x] **SpatialGrid** (563 LOC, autoload) — delete when all targeting is in simulation. <50 units makes spatial hashing unnecessary.
- [x] 3 autoloads removed. `unit_constants.gd` HurtboxCategory mirror enum removed. `test_targeting_config_registry.gd` deleted.

### Cross-Tier Parallelism

- Tier 1 → Tier 2 (sequential)
- Tier 2 → Tiers 3, 4, 5 (all three run in **parallel** after Tier 2 completes)

---

## Phase 8: Plan GDScript UI → C# Migration (Future)

**Goal:** Migrate all GDScript UI screens to C#, eliminate facade wrappers, consolidate to C#-only services. Enables typed `MetaGameEvents`.

**Depends on:** Phase 7 (old battle systems deleted, clean slate for UI migration)

**Context for new sessions:** Read `docs/migration/meta-game-plan.md` §8 for the stub. This phase is not blocked by and does not block current migration work.

### Scope

- [ ] **Establish C# UI pattern** — Define how to build Godot UI screens in C#. Migrate one pilot screen to validate approach.
- [ ] **Screen migration order** — Determine dependency-based migration order for all GDScript UI screens.
- [ ] **Facade elimination** — Plan removal of 7 GDScript service facades once their UI consumers are migrated.
- [ ] **Catalog consolidation** — Plan migration of GDScript data catalogs (`card_catalog.gd`, `summoner_catalog.gd`, etc.) to C#-only.
- [ ] **MetaGameEvents implementation** — Implement typed C# event system replacing `GameStateEvents` (meta-game-plan.md §4).
- [ ] **GDScript-native service migration** — Plan rewrite of `DialogueManager`, `EventSequencer`, `CapabilityManager` in C#.
- [ ] **GDScript constants retirement** — Plan migration of `*_ids.gd` files to C# enum/const references.
