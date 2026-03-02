# Implementation Checklist

Actionable, dependency-ordered checklist for building all four layers and deleting legacy systems. Each milestone follows the **Build → Wire → Verify → Delete** pattern.

**Sources:** [deletion-sequence.md](deletion-sequence.md), [planning-checklist.md](planning-checklist.md), [session design-specs.md](../architecture/gameplay/session/design-specs.md), [view design-specs.md](../architecture/gameplay/view/design-specs.md), [cross-cutting-plan.md](cross-cutting-plan.md)

**Stub files:** `scripts/csharp/Session/` (8 files), `scripts/csharp/View/` (5 files), `scripts/csharp/Input/` (1 file) — all throw `NotImplementedException`.

---

## Dependency Diagram

```
Milestone 0 ─► Milestone 1 ─► Milestone 2 ──┬──► Milestone 3a (Session)
(Prerequisites)  (UnitVisual)   (View Layer)  ├──► Milestone 3b (Input)
                                              └──► Milestone 3c (Capabilities)
```

Milestones 3a, 3b, 3c are **independent** and can run in parallel after Milestone 2 completes.

---

## Milestone 0: Prerequisites

Verify the foundation is solid before building new layers.

### Simulation Completeness

- [ ] All unit behaviors work (move, attack, die, spawn) — verified by `SimBehaviorTest`
- [ ] `MatchState` contains all needed data: units, projectiles, summoners, phase, timer
- [ ] `Simulation.Tick(delta)` advances game state deterministically
- [ ] `DeterministicRng` is used for all gameplay randomness inside simulation
- [ ] `SimEvent` types cover all game events (attack, damage, death, spell, buff, projectile, summoner, etc.)

### IGameSession Interface

- [ ] `IGameSession` interface is finalized (`scripts/csharp/Session/IGameSession.cs`):
  - `MatchState GetState()`
  - `event Action<IReadOnlyList<SimEvent>> SimEventsEmitted`
  - `void SubmitCommand(ICommand command)`
  - `void Tick(float delta)`
- [ ] `ICommand` interface exists with `PlayCardCommand` and `ForfeitCommand` implementations
- [ ] `ValidationResult` type exists for `CommandRouter`

### Commands

- [ ] `PlayCardCommand` — exists
- [ ] `ForfeitCommand` — exists
- [ ] `CastSpellCommand` — needed later (Milestone 3b), but define the type now if convenient
- [ ] `RedirectCommand` — needed later (Milestone 3b), but define the type now if convenient

### Gate: Prerequisites Met

- [ ] `dotnet build` succeeds
- [ ] `dotnet test --settings test.runsettings` passes
- [ ] All stub files compile (they throw `NotImplementedException`, which is fine)

---

## Milestone 1: UnitVisual — Unblocks Tier 1 Deletions

**Stub file:** `scripts/csharp/View/UnitVisual.cs`

**Goal:** UnitVisual renders units by reading `UnitData` from `MatchState`. Once verified, delete DamageSystem, ModifierService, ProjectileService (3 autoloads).

### 1.1: Implement UnitVisual Self-Sync

- [ ] `Initialize(IGameSession session, int unitId)` — store session reference and unit ID
- [ ] `_PhysicsProcess(double delta)` — read `UnitData` from `MatchState` each frame:
  - Sync `GlobalPosition` from `UnitData.Position` (SimVector3 → Godot Vector3)
  - Sync facing direction from `UnitData.Facing`
  - Drive animation state (idle, walk, attack) from `UnitData.BehaviorState`

### 1.2: Implement Event Reaction Methods

- [ ] `PlayAttackAnimation()` — trigger attack animation on the visual
- [ ] `FlashDamage(float damage, bool isCrit)` — damage flash VFX + floating damage number
- [ ] `BeginDeath()` — death animation, then queue_free
- [ ] `ShowBuffIcon(EffectType effectType)` — show buff/debuff icon above unit
- [ ] `ShowEvadeText()` — show "Evade!" floating text

### 1.3: Wire UnitVisual Into Existing Battle Flow

- [ ] Create a test harness that runs UnitVisual alongside Unit3D (dual-running verification)
- [ ] Verify UnitVisual position matches Unit3D position within tolerance
- [ ] Verify attack animations trigger at the correct times
- [ ] Verify damage numbers appear correctly
- [ ] Verify death plays animation and cleans up

### Gate: UnitVisual Verified

- [ ] UnitVisual renders all unit types correctly (melee, ranged, duckling)
- [ ] Unit3D has zero remaining unique consumers that UnitVisual can't serve
- [ ] Visual parity: UnitVisual looks equivalent to Unit3D in battle

### 1.4: Tier 1 Deletions

**Delete DamageSystem (837 LOC):**
- [ ] Delete `scripts/csharp/Combat/DamageSystem.cs` + `.tscn` + `.uid`
- [ ] Delete `scripts/csharp/Services/Interfaces/IDamageSystem.cs` + `.uid`
- [ ] Remove `DamageSystem` autoload from `project.godot`

**Delete ModifierService (714 LOC deleted, 401 LOC relocated):**
- [ ] Delete `scripts/csharp/Systems/Modifiers/ModifierService.cs` + `.tscn`
- [ ] Delete `scripts/csharp/Services/Interfaces/IModifierService.cs`
- [ ] Delete `CardModifierProvider.cs`, `ItemModifierProvider.cs`, `SummonerModifierProvider.cs`
- [ ] Delete `IModifierProvider.cs`, `ModifierContext.cs`, `ConditionKeys.cs`
- [ ] **RELOCATE** `StatModifier.cs` → `scripts/csharp/Stats/StatModifier.cs` (27+ consumers!)
- [ ] **RELOCATE** `TriggerCondition.cs` → `scripts/csharp/Stats/TriggerCondition.cs`
- [ ] Remove `ModifierService` autoload from `project.godot`
- [ ] Delete `tests/csharp/Systems/Modifiers/ModifierServiceTest.cs`
- [ ] **RELOCATE** `StatModifierTest.cs` → `tests/csharp/Stats/StatModifierTest.cs`
- [ ] **RELOCATE** `TriggerConditionTest.cs` → `tests/csharp/Stats/TriggerConditionTest.cs`

**Delete ProjectileService (509 LOC):**
- [ ] Delete `scripts/csharp/Projectiles/ProjectileService.cs` + `.tscn` + `.uid`
- [ ] Remove `ProjectileService` autoload from `project.godot`

**Update CardFactory.cs:**
- [ ] Remove `ModifierService` references from `CardFactory.cs`

### Gate: Tier 1 Complete

- [ ] `dotnet build` succeeds
- [ ] `dotnet test --settings test.runsettings` passes
- [ ] Grep `DamageSystem` — only `SimDamage` references remain
- [ ] Grep `ModifierService` — zero references
- [ ] Grep `ProjectileService` — zero references
- [ ] Grep `IDamageSystem` — zero references
- [ ] Grep `IModifierService` — zero references
- [ ] `StatModifier.cs` and `TriggerCondition.cs` exist in `scripts/csharp/Stats/`
- [ ] 3 autoloads removed from `project.godot`

---

## Milestone 2: Full View Layer — Unblocks Tier 2 Deletions

**Stub files:** `EntityManager.cs`, `ProjectileVisual.cs`, `SummonerVisual.cs`, `BattleScene.cs`

**Goal:** All view layer components operational. EntityManager manages lifecycle and event routing. Visual shells self-sync from MatchState.

### 2a: EntityManager — Central Lifecycle Coordinator

**Stub file:** `scripts/csharp/View/EntityManager.cs`

- [ ] `Initialize(IGameSession session)` — store session, subscribe to `SimEventsEmitted`
- [ ] `_PhysicsProcess(double delta)` — entity diffing:
  - Poll `MatchState` for current unit list
  - Spawn `UnitVisual` shells for new units (call `SpawnUnitShell`)
  - Destroy shells for removed units (call `DestroyShell`)
  - Spawn `ProjectileVisual` shells for new projectiles
  - Destroy projectile shells for removed projectiles
- [ ] `SpawnUnitShell(UnitData unitData)` — instantiate scene, call `Initialize`
- [ ] `SpawnProjectileShell(SimProjectileData projData)` — instantiate scene, call `Initialize`
- [ ] `DestroyShell(int entityId)` — remove from tracking, queue_free
- [ ] `RegisterSummonerVisual(SummonerVisual shell, int teamIndex)` — register pre-placed summoner shells

**ISimEventVisitor implementation (route events to visual shells):**
- [ ] `Visit(UnitAttackedEvent)` → `GetShell(attackerId)?.PlayAttackAnimation()`
- [ ] `Visit(UnitDamagedEvent)` → `GetShell(targetId)?.FlashDamage(damage, isCrit)`
- [ ] `Visit(UnitDiedSimEvent)` → `GetShell(unitId)?.BeginDeath()`
- [ ] `Visit(ProjectileHitSimEvent)` → `GetProjectileShell(projId)?.PlayImpactAndDestroy()`
- [ ] `Visit(SummonerDamagedEvent)` → `GetSummonerShell(teamIndex)?.FlashDamage()`
- [ ] `Visit(SummonerDestroyedEvent)` → `GetSummonerShell(teamIndex)?.BeginDeath()`
- [ ] `Visit(AttackEvadedEvent)` → `GetShell(targetId)?.ShowEvadeText()`
- [ ] `Visit(BuffAppliedSimEvent)` → `GetShell(unitId)?.ShowBuffIcon(effectType)`
- [ ] `Visit(SpellCastEvent)` → trigger spell VFX
- [ ] `Visit(DelayedEffectFiredSimEvent)` → trigger delayed effect VFX
- [ ] Handle remaining no-op event types (log or ignore)
- [ ] `Pause()` / `Resume()` — pause/resume visual processing

### 2b: ProjectileVisual — Self-Syncing Projectile Shell

**Stub file:** `scripts/csharp/View/ProjectileVisual.cs`

- [ ] `Initialize(IGameSession session, int projectileId)` — store session ref and ID
- [ ] `_PhysicsProcess(double delta)` — read `SimProjectileData` from `MatchState`:
  - Sync `GlobalPosition` from projectile position
  - Sync rotation to face movement direction
  - Manage trail effect
- [ ] `PlayImpactAndDestroy()` — play impact VFX, fade trail, queue_free

### 2c: SummonerVisual — Self-Syncing Summoner Shell

**Stub file:** `scripts/csharp/View/SummonerVisual.cs`

- [ ] `Initialize(IGameSession session, int teamIndex)` — store session ref and team index
- [ ] `_PhysicsProcess(double delta)` — read `SummonerData` from `MatchState`:
  - Sync HP display
  - Sync mana display (for UI elements attached to summoner)
  - Update casting state visual
- [ ] `FlashDamage()` — summoner hit flash VFX
- [ ] `BeginDeath()` — summoner destruction animation
- [ ] Own HP bar (create and manage inline, replacing HPBarService pattern)

### 2d: BattleScene — Top-Level Facade

**Stub file:** `scripts/csharp/View/BattleScene.cs`

- [ ] `Initialize(IGameSession session)`:
  - Wire session to `EntityManager` (call `EntityManager.Initialize(session)`)
  - Wire session to `BattleHUD` (if applicable)
  - Set up camera, environment (state-independent)
- [ ] Replace `GameController3D` as the scene root script for `battle_3d.tscn`

### 2e: Scene File Updates

**20 unit scenes — replace root script with UnitVisual:**
- [ ] `scenes/units/puff_3d.tscn` (RangedUnit3D → UnitVisual)
- [ ] `scenes/units/fire_spider_3d.tscn` (RangedUnit3D → UnitVisual)
- [ ] `scenes/units/earth_rock_thrower_3d.tscn` (RangedUnit3D → UnitVisual)
- [ ] `scenes/units/life_wisp_3d.tscn` (MeleeUnit3D → UnitVisual)
- [ ] `scenes/units/fire_wisp_3d.tscn` (MeleeUnit3D → UnitVisual)
- [ ] `scenes/units/lightning_wisp_3d.tscn` (MeleeUnit3D → UnitVisual)
- [ ] `scenes/units/shadow_wisp_3d.tscn` (MeleeUnit3D → UnitVisual)
- [ ] `scenes/units/fire_ant_3d.tscn` (MeleeUnit3D → UnitVisual)
- [ ] `scenes/units/fire_titan_3d.tscn` (MeleeUnit3D → UnitVisual)
- [ ] `scenes/units/stone_ape_3d.tscn` (MeleeUnit3D → UnitVisual)
- [ ] `scenes/units/water_wisp_3d.tscn` (MeleeUnit3D → UnitVisual)
- [ ] `scenes/units/rock_3d.tscn` (MeleeUnit3D → UnitVisual)
- [ ] `scenes/units/mama_duck_3d.tscn` (MeleeUnit3D → UnitVisual)
- [ ] `scenes/units/wind_wisp_3d.tscn` (MeleeUnit3D → UnitVisual)
- [ ] `scenes/units/water_frog_3d.tscn` (MeleeUnit3D → UnitVisual)
- [ ] `scenes/units/earth_wisp_3d.tscn` (MeleeUnit3D → UnitVisual)
- [ ] `scenes/units/duckling_3d.tscn` (DucklingUnit3D → UnitVisual)
- [ ] `scenes/units/earth_sprite_3d.tscn` (MeleeUnit3D → UnitVisual)
- [ ] `scenes/units/death_wisp_3d.tscn` (MeleeUnit3D → UnitVisual)
- [ ] `scenes/units/fire_boar_3d.tscn` (MeleeUnit3D → UnitVisual)

**1 projectile scene:**
- [ ] `scenes/projectiles/base_projectile_3d.tscn` (Projectile3D → ProjectileVisual)

**Battle scene:**
- [ ] `scenes/battlefield/battle_3d.tscn` (game_controller_3d → BattleScene)
- [ ] Update or delete test scenes using `test_game_controller.gd`

### 2f: Tier 2 Deletions

**Step 1 — HPBarService (563 LOC):**
- [ ] Delete `scripts/csharp/Services/HPBarService.cs` + `.tscn` + `.uid`
- [ ] Remove `HPBarService` autoload from `project.godot`

**Step 2 — SimEventSignalEmitter (109 LOC):**
- [ ] Delete `scripts/csharp/Simulation/SimEventSignalEmitter.cs` + `.uid`
- [ ] Remove signal declarations from `SimulationNode` that were only used by emitter

**Step 3 — SimulationNode slim-down (~842 LOC removed):**
- [ ] Slim `SimulationNode.cs` to ~100 lines (thin Godot bridge: factory + accessor)
- [ ] Game logic migrated to Session layer implementations

**Step 4 — Unit3D + subclasses + components (~3,076 LOC):**
- [ ] Delete `scripts/csharp/Units/Unit3D.cs` (2,285 LOC)
- [ ] Delete `scripts/csharp/Units/MeleeUnit3D.cs` (158 LOC)
- [ ] Delete `scripts/csharp/Units/RangedUnit3D.cs` (257 LOC)
- [ ] Delete `scripts/csharp/Units/DucklingUnit3D.cs` (38 LOC)
- [ ] Delete `scripts/csharp/Units/Components/UnitHealth.cs` (139 LOC)
- [ ] Delete `scripts/csharp/Units/Components/UnitMovement.cs` (199 LOC)
- [ ] Delete all corresponding `.uid` files

**Step 5 — Additional legacy systems:**
- [ ] Delete `scripts/csharp/Cards/Effects/` (14 files, ~1,300 LOC) — entire directory
- [ ] Delete `scripts/csharp/Cards/SpellCard.cs` (71 LOC)
- [ ] Delete `scripts/core/summoner.gd` (979 LOC)
- [ ] Delete `scripts/csharp/Summons/UnitSpawner.cs` (419 LOC)
- [ ] Delete `scripts/csharp/Movement/UnitSteering.cs` (462 LOC)
- [ ] Delete `scripts/csharp/Units/Components/SpawnRevealComponent.cs` (240 LOC)
- [ ] Delete `scripts/csharp/Units/UnitDebugService.cs` (67 LOC)
- [ ] Remove `UnitDebugService` autoload from `project.godot`

**Files to UPDATE (not delete):**
- [ ] Update `SpawnPreview.cs` — rewrite to read `InputCollector` drag state + create preview from UnitVisual
- [ ] Update `GhostUnit3D.cs` — rewrite to use UnitVisual patterns
- [ ] Update `CardFactory.cs` — verify ModifierService references already removed in Milestone 1

**Step 6 — Projectile3D + ProjectileData (~1,445 LOC):**
- [ ] Delete `scripts/csharp/Projectiles/Projectile3D.cs` (1,128 LOC)
- [ ] Delete `scripts/csharp/Projectiles/ProjectileData.cs` (317 LOC)
- [ ] Delete corresponding `.uid` files

**Step 7 — GameController3D + test controller (~1,225 LOC):**
- [ ] Delete `scripts/core/game_controller_3d.gd` (1,048 LOC)
- [ ] Delete `scripts/core/test_game_controller.gd` (177 LOC)
- [ ] Delete corresponding `.uid` files

**Test updates:**
- [ ] Update `tests/csharp/Multiplayer/ClientInitializationTest.cs` (Unit3D/SimulationNode refs change)

### Gate: Tier 2 Complete

- [ ] `dotnet build` succeeds
- [ ] `dotnet test --settings test.runsettings` passes
- [ ] Full view layer renders — units, projectiles, summoners all visible and animated
- [ ] Grep `Unit3D` — zero references in production code
- [ ] Grep `Projectile3D` — zero references in production code
- [ ] Grep `game_controller_3d` — zero references
- [ ] Grep `SimEventSignalEmitter` — zero references
- [ ] Grep `HPBarService` — zero references
- [ ] Grep `Cards/Effects/` — directory deleted, zero references
- [ ] Grep `SpellCard` — zero references in production code
- [ ] Grep `summoner.gd` — zero references (only `SummonerVisual`)
- [ ] Grep `UnitSpawner` — zero references in production code
- [ ] Grep `UnitSteering` — zero references in production code
- [ ] Grep `SpawnRevealComponent` — zero references
- [ ] Grep `UnitDebugService` — zero references
- [ ] `SpawnPreview.cs` uses UnitVisual patterns
- [ ] `GhostUnit3D.cs` uses UnitVisual patterns
- [ ] `CardFactory.cs` — no `ModifierService` references
- [ ] All 20 unit scene files reference `UnitVisual`
- [ ] `SimulationNode.cs` is ≤100 lines
- [ ] 2 autoloads removed from `project.godot` (HPBarService, UnitDebugService)

---

## Milestone 3a: Session Layer — Unblocks Tier 3 Deletions

**Stub files:** `CommandRouter.cs`, `LocalSession.cs`, `HostSession.cs`, `ClientSession.cs`, `NetworkSession.cs`, `IdentityMap.cs`, `SnapshotCodec.cs`

**Parallel with Milestones 3b and 3c** — no cross-dependencies.

### 3a.1: CommandRouter — Validation Logic

**Stub file:** `scripts/csharp/Session/CommandRouter.cs`

- [ ] Implement `Validate(ICommand command, MatchState state)` with pattern matching:
  - `PlayCardCommand` → `ValidatePlayCard()`
  - `ForfeitCommand` → `ValidateForfeit()`
  - Unknown → rejection

**PlayCardCommand validation rules** (from session design-specs §6):
- [ ] Player index: `0 <= playerIndex < state.Summoners.Length`
- [ ] Card index: `0 <= cardIndex < summoner.Hand.Count`
- [ ] Mana: `summoner.Mana >= cardData.ManaCost`
- [ ] Phase: `state.Phase == GamePhase.Battle`
- [ ] Casting state: `!summoner.IsCasting`
- [ ] Card exists: `state.CardDataMap.ContainsKey(catalogId)`

**ForfeitCommand validation rules:**
- [ ] Player index: `0 <= playerIndex < state.Summoners.Length`
- [ ] Phase: `state.Phase != GamePhase.GameOver`

**Tests:**
- [ ] Unit tests for each validation rule (valid + invalid cases)
- [ ] Test unknown command type rejection

### 3a.2: LocalSession — Singleplayer (Simplest Session)

**Stub file:** `scripts/csharp/Session/LocalSession.cs`

- [ ] Implement constructor: `BattleConfig` → create `Simulation` + `MatchState` + `CommandRouter`
- [ ] Implement `Tick(float delta)`:
  - Flush command queue into simulation
  - Call `Simulation.Tick(delta)`
  - Collect `SimEvent`s from simulation
  - Fire `SimEventsEmitted` event
- [ ] Implement `SubmitCommand(ICommand command)`:
  - Validate via `CommandRouter.Validate(command, state)`
  - If valid: queue for next tick
  - If invalid: log rejection
- [ ] Implement `GetState()` — return current `MatchState`

**Tests:**
- [ ] Integration test: `LocalSession` ticks and produces correct events
- [ ] Test: valid command is queued and applied next tick
- [ ] Test: invalid command is rejected (not applied)
- [ ] Test: `GetState()` returns updated state after tick

### 3a.3: NetworkSession + HostSession — Multiplayer Host

**Stub files:** `scripts/csharp/Session/NetworkSession.cs`, `scripts/csharp/Session/HostSession.cs`

**IdentityMap** (`scripts/csharp/Session/IdentityMap.cs`):
- [ ] Implement O(1) bidirectional map: `unitId ↔ networkId`
- [ ] `Register(int unitId, int networkId)`
- [ ] `GetNetworkId(int unitId)` / `GetUnitId(int networkId)`
- [ ] `Unregister(int unitId)`

**SnapshotCodec** (`scripts/csharp/Session/SnapshotCodec.cs`):
- [ ] Implement `byte[] Encode(MatchState state)` — serialize MatchState for network
- [ ] Implement `MatchState Decode(byte[] data)` — deserialize MatchState from network
- [ ] Handle all MatchState fields: units, projectiles, summoners, phase, timer

**NetworkSession** (abstract base):
- [ ] Implement `HandleMessage(object message)` — message routing
- [ ] Own `IdentityMap`, `SnapshotCodec`, `ReconnectionHandler` as protected fields
- [ ] Transport ownership and lifecycle

**HostSession:**
- [ ] Implement constructor: `BattleConfig` + `Simulation` + `CommandRouter` + transport
- [ ] Implement `Tick(float delta)`:
  - Flush local + remote command queues into simulation
  - Call `Simulation.Tick(delta)`
  - Serialize `MatchState` via `SnapshotCodec`
  - Broadcast snapshot to clients
  - Fire `SimEventsEmitted`
- [ ] Implement `SubmitCommand(ICommand command)`:
  - Validate via `CommandRouter` (host also validates own commands — fixes issue #9)
  - If valid: queue for next tick
- [ ] Implement `HandleRemoteCommand(int senderId, ICommand command)`:
  - Validate via `CommandRouter`
  - If valid: queue for next tick
  - If invalid: send rejection to client

**Tests:**
- [ ] IdentityMap: bidirectional lookup, register/unregister
- [ ] SnapshotCodec: round-trip encode/decode preserves MatchState
- [ ] HostSession: ticks, broadcasts snapshots, validates commands

### 3a.4: ClientSession — Multiplayer Client

**Stub file:** `scripts/csharp/Session/ClientSession.cs`

- [ ] Implement constructor: `BattleConfig` + transport (no local simulation)
- [ ] Implement `ApplySnapshot(MatchState snapshot)`:
  - Patch local `MatchState` from host snapshot
  - Reconcile predictions via `PredictionBuffer`
  - Run `StateInterpolator` for smooth positions
- [ ] Implement `Tick(float delta)`:
  - Apply latest snapshot
  - Advance `StateInterpolator`
  - Fire `SimEventsEmitted`
- [ ] Implement `SubmitCommand(ICommand command)`:
  - Send command to host over network
  - Apply local prediction (mana deduction, card removal)
  - Add to `PredictionBuffer` with sequence number
- [ ] Implement `GetState()` — return interpolated local `MatchState`

**Client prediction (from session design-specs §4):**
- [ ] `PredictionBuffer` with sequence numbers
- [ ] Reconciliation: compare predicted state with host state
- [ ] Rollback on mismatch: restore mana, return card to hand
- [ ] Cap at ~5 pending predictions

**Tests:**
- [ ] ClientSession: applies snapshot, returns updated state
- [ ] ClientSession: prediction + reconciliation cycle
- [ ] ClientSession: rollback on rejected command

### 3a.5: Tier 3 Deletions

**Runners (727 LOC):**
- [ ] Delete `scripts/csharp/Multiplayer/Authority/HostRunner.cs` (275 LOC)
- [ ] Delete `scripts/csharp/Multiplayer/Client/ClientRunner.cs` (452 LOC)

**Session + Utilities (1,130 LOC):**
- [ ] Delete `scripts/csharp/Multiplayer/Core/MatchSession.cs` (359 LOC)
- [ ] Delete `scripts/csharp/Multiplayer/Authority/RequestValidator.cs` (87 LOC)
- [ ] Delete `scripts/csharp/Multiplayer/Core/NetworkIdRegistry.cs` (138 LOC)
- [ ] Delete `scripts/csharp/Multiplayer/Sync/StateSnapshotBuilder.cs` (215 LOC)
- [ ] Delete `scripts/csharp/Multiplayer/Sync/DesyncDetector.cs` (331 LOC) — replace with `DesyncChecker` (reads `MatchState` only, no scene tree)

**ReconnectionHandler (373 LOC):**
- [ ] Delete `scripts/csharp/Multiplayer/Core/ReconnectionHandler.cs` — rewrite logic into `NetworkSession` (no singleton, no Godot deps)

**Interfaces (174 LOC):**
- [ ] Delete `scripts/csharp/Multiplayer/Core/IMatchRunner.cs` (42 LOC)
- [ ] Delete `scripts/csharp/Multiplayer/Core/IMessageBroadcaster.cs` (12 LOC)
- [ ] Delete `scripts/csharp/Multiplayer/Authority/HostEventBroadcaster.cs` (120 LOC)

**Data cleanup:**
- [ ] Remove `UnitData.NetworkId` and `UnitData.TargetNetworkId` fields

**Test updates:**
- [ ] Update `tests/csharp/Multiplayer/SimEventCoverageTest.cs`
- [ ] Update `tests/csharp/Multiplayer/BroadcastFieldTest.cs`

### Gate: Tier 3 Complete

- [ ] `dotnet build` succeeds
- [ ] `dotnet test --settings test.runsettings` passes
- [ ] All three session modes work: LocalSession (SP), HostSession (MP host), ClientSession (MP client)
- [ ] Grep `HostRunner` — zero references
- [ ] Grep `ClientRunner` — zero references
- [ ] Grep `MatchSession` — zero references
- [ ] Grep `RequestValidator` — zero references
- [ ] Grep `NetworkIdRegistry` — zero references
- [ ] Grep `StateSnapshotBuilder` — zero references
- [ ] Grep `DesyncDetector` — zero references (only `DesyncChecker`)
- [ ] Grep `ReconnectionHandler` — zero references (logic in `NetworkSession`)
- [ ] Grep `IMatchRunner` — zero references
- [ ] `UnitData.NetworkId` and `UnitData.TargetNetworkId` fields removed

---

## Milestone 3b: Input Layer — Unblocks Tier 4 Deletions

**Stub file:** `scripts/csharp/Input/InputCollector.cs`

**Parallel with Milestones 3a and 3c** — no cross-dependencies.

### 3b.1: InputCollector — Gesture→Command

**Stub file:** `scripts/csharp/Input/InputCollector.cs`

- [ ] `Initialize(IGameSession session)` — already implemented (simple assignment)
- [ ] `OnCardDropped(int cardIndex, Vector3 position)`:
  - Create `PlayCardCommand(playerIndex, cardIndex, position)`
  - Submit via `session.SubmitCommand()`
- [ ] `OnSpellTargetConfirmed(int cardIndex, Vector3 position, int? targetUnitId)`:
  - Create `CastSpellCommand(playerIndex, cardIndex, position, targetUnitId)`
  - Submit via `session.SubmitCommand()`
- [ ] `OnForfeitRequested()`:
  - Create `ForfeitCommand(playerIndex)`
  - Submit via `session.SubmitCommand()`

**Public drag state for View to read:**
- [ ] `int? DraggedCardIndex` — which card is being dragged (null if none)
- [ ] `Vector3? DragPosition` — current drag position on battlefield
- [ ] `bool IsDraggingSummonCard` — convenience property for SpawnZoneOverlay
- [ ] Spell targeting state: `SpellTargetingState` (Inactive, AwaitingFirstClick, DraggingArrow)
- [ ] `Vector3? SpellTargetPosition` — current spell target position
- [ ] `float? SpellTargetRadius` — spell selection radius

**Redirect state:**
- [ ] Redirect mode state for View to read
- [ ] `OnRedirectConfirmed(Vector3 selectionCenter, float selectionRadius, Vector3 targetPosition, bool isAttack)`:
  - Create `RedirectCommand(...)` and submit

### 3b.2: New Command Types

- [ ] `CastSpellCommand` — `int PlayerIndex`, `int CardIndex`, `Vector3 Position`, `int? TargetUnitId`
- [ ] `RedirectCommand` — `int PlayerIndex`, `Vector3 SelectionCenter`, `float SelectionRadius`, `Vector3 TargetPosition`, `bool IsAttack`
- [ ] Add validation rules in `CommandRouter.Validate()` for both new types

### 3b.3: BattleRNG Consumer Migration

Migrate 4 GDScript consumers to simulation's `DeterministicRng`:

- [ ] `heuristic_ai.gd` (11 calls) — AI submits commands via `IGameSession`; sim handles randomness internally
- [ ] `summoner.gd` (2 calls) — session handles deck shuffle at battle init (summoner.gd already deleted in Milestone 2)
- [ ] `online_screen.gd` (1 call) — session receives seed via `BattleConfig`
- [ ] `multiplayer_lobby.gd` (1 call) — session receives seed via `BattleConfig`

### 3b.4: Tier 4 Deletions

- [ ] Delete `scripts/ui/battle/spell_targeting_manager.gd` (375 LOC) + `.uid`
- [ ] Remove `SpellTargetingManager` autoload from `project.godot`
- [ ] Delete `scripts/managers/redirect_manager.gd` (402 LOC) + `.uid`
- [ ] Remove `RedirectManager` autoload from `project.godot`
- [ ] Delete `scripts/ui/battle/battlefield_drop_zone.gd` (515 LOC) + `.uid`
- [ ] Delete `scripts/multiplayer/rng/battle_rng.gd` (207 LOC) + `.uid`
- [ ] Delete `scripts/multiplayer/rng/rng_domain.gd` (30 LOC) + `.uid`
- [ ] Remove `BattleRNG` autoload from `project.godot`
- [ ] Delete `scripts/core/player_input.gd` (43 LOC) + `.uid`
- [ ] Delete `scripts/core/player_input_3d.gd` (95 LOC) + `.uid`

**Scene file updates (5 files) — remove BattlefieldDropZone references:**
- [ ] `scenes/ui/battle/battle_hud.tscn`
- [ ] `scenes/battlefield/dev/test_collision.tscn`
- [ ] `scenes/battlefield/dev/test_battle_abilities.tscn`
- [ ] `scenes/battlefield/dev/test_battle_vfx.tscn`
- [ ] `scenes/test/rally_guard_test.tscn`

### Gate: Tier 4 Complete

- [ ] `dotnet build` succeeds
- [ ] `dotnet test --settings test.runsettings` passes
- [ ] InputCollector produces commands correctly for all gesture types
- [ ] Grep `SpellTargetingManager` — zero references in production code
- [ ] Grep `RedirectManager` — zero references in production code
- [ ] Grep `BattlefieldDropZone` — zero references in production code
- [ ] Grep `BattleRNG` — zero references in production code
- [ ] Grep `player_input` — zero references in production code
- [ ] 3 autoloads removed from `project.godot`
- [ ] 5 scene files updated

---

## Milestone 3c: Capability Retirement — Tier 5 Deletions

**No stub files** — this milestone is pure deletion after verifying simulation handles everything.

**Parallel with Milestones 3a and 3b** — no cross-dependencies.

### 3c.1: Verify Simulation Coverage

- [ ] All targeting handled by `SimTargeting` (no remaining consumers of `ITargetingBehavior`)
- [ ] All hit detection handled by `SimProjectile` + `SimDamage` (no remaining consumers of `HitResolver`)
- [ ] All damage handled by simulation (no remaining consumers of `IDamageable`)
- [ ] `SpatialGrid` has zero remaining consumers

### 3c.2: Delete Capabilities/ (5 interfaces, 135 LOC)

- [ ] Delete `scripts/csharp/Capabilities/IDamageable.cs`
- [ ] Delete `scripts/csharp/Capabilities/IRangedAttacker.cs`
- [ ] Delete `scripts/csharp/Capabilities/IAreaAttacker.cs`
- [ ] Delete `scripts/csharp/Capabilities/IVfxAttacker.cs`
- [ ] Delete `scripts/csharp/Capabilities/IStatModifier.cs`
- [ ] Delete all corresponding `.uid` files

### 3c.3: Delete Targeting/ (17 files, 982 LOC)

- [ ] Delete `scripts/csharp/Targeting/ITargetingBehavior.cs`
- [ ] Delete `scripts/csharp/Targeting/TargetingConfig.cs`
- [ ] Delete `scripts/csharp/Targeting/TargetingConfigRegistryCS.cs` + `.tscn`
- [ ] Remove `TargetingConfigRegistryCS` autoload from `project.godot`
- [ ] Delete `scripts/csharp/Targeting/Constraints/` (4 files: Base, Composite, Cone, HorizontalCone, Range)
- [ ] Delete `scripts/csharp/Targeting/Filters/` (4 files: Base, Composite, Layer, Valid)
- [ ] Delete `scripts/csharp/Targeting/Scorers/` (5 files: Base, Below, Composite, Distance, Health)
- [ ] Delete all corresponding `.uid` files

### 3c.4: Delete Combat/Hitbox/ (6 files, 777 LOC)

- [ ] Delete `scripts/csharp/Combat/Hitbox/HitboxComponent.cs`
- [ ] Delete `scripts/csharp/Combat/Hitbox/HitboxLifetime.cs`
- [ ] Delete `scripts/csharp/Combat/Hitbox/HitResolver.cs` + `.tscn`
- [ ] Remove `HitResolver` autoload from `project.godot`
- [ ] Delete `scripts/csharp/Combat/Hitbox/HitResult.cs`
- [ ] Delete `scripts/csharp/Combat/Hitbox/HurtboxComponent.cs`
- [ ] Delete `scripts/csharp/Combat/Hitbox/HurtboxCategory.cs`
- [ ] Delete all corresponding `.uid` files

### 3c.5: Delete SpatialGrid (563 LOC)

- [ ] Delete `scripts/csharp/Systems/SpatialGrid.cs` + `.tscn` + `.uid`
- [ ] Remove `SpatialGrid` autoload from `project.godot`

### 3c.6: Cleanup

- [ ] Remove `HurtboxCategory` mirror enum from `scripts/data/unit_constants.gd`
- [ ] Update `CardFactory.cs` — remove `SpatialGrid` references
- [ ] Delete `tests/unit/test_targeting_config_registry.gd`

### Gate: Tier 5 Complete

- [ ] `dotnet build` succeeds
- [ ] `dotnet test --settings test.runsettings` passes
- [ ] Grep `IDamageable` — zero references in production code
- [ ] Grep `IRangedAttacker` — zero references
- [ ] Grep `ITargetingBehavior` — zero references
- [ ] Grep `TargetingConfig` — zero references (only sim-internal targeting)
- [ ] Grep `HitResolver` — zero references
- [ ] Grep `HitboxComponent` — zero references
- [ ] Grep `SpatialGrid` — zero references
- [ ] `HurtboxCategory` mirror enum removed from `unit_constants.gd`
- [ ] 3 autoloads removed from `project.godot`

---

## Summary

| Milestone | Builds | Deletes | Autoloads Removed | LOC Deleted |
|-----------|--------|---------|-------------------|-------------|
| 0 | Prerequisites verified | 0 files | 0 | 0 |
| 1 | UnitVisual | 12 files | 3 | ~2,250 |
| 2 | EntityManager, ProjectileVisual, SummonerVisual, BattleScene | 34+ files | 2 | ~10,440 |
| 3a | CommandRouter, LocalSession, HostSession, ClientSession, IdentityMap, SnapshotCodec | 12 files | 0 | ~2,400 |
| 3b | InputCollector, CastSpellCommand, RedirectCommand | 7 files | 3 | ~1,670 |
| 3c | *(verification only)* | 29 files | 3 | ~2,460 |
| **Total** | **14 stub files implemented** | **~94 files** | **11** | **~19,220** |

### Stub File Coverage

Every stub file maps to a milestone:

| Stub File | Milestone |
|-----------|-----------|
| `IGameSession.cs` | 0 (verify) |
| `UnitVisual.cs` | 1 |
| `EntityManager.cs` | 2a |
| `ProjectileVisual.cs` | 2b |
| `SummonerVisual.cs` | 2c |
| `BattleScene.cs` | 2d |
| `CommandRouter.cs` | 3a.1 |
| `LocalSession.cs` | 3a.2 |
| `NetworkSession.cs` | 3a.3 |
| `HostSession.cs` | 3a.3 |
| `ClientSession.cs` | 3a.4 |
| `IdentityMap.cs` | 3a.3 |
| `SnapshotCodec.cs` | 3a.3 |
| `InputCollector.cs` | 3b.1 |
