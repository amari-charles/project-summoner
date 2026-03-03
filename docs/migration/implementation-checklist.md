# Implementation Checklist

Actionable, dependency-ordered checklist for building all four layers and deleting legacy systems. Each milestone follows the **Build → Wire → Verify → Delete** pattern.

**Sources:** [deletion-sequence.md](deletion-sequence.md), [planning-checklist.md](planning-checklist.md), [session design-specs.md](../architecture/gameplay/session/design-specs.md), [view design-specs.md](../architecture/gameplay/view/design-specs.md), [cross-cutting-plan.md](cross-cutting-plan.md)

**Stub files:** `scripts/csharp/Session/` (8 files), `scripts/csharp/View/` (5 files), `scripts/csharp/Input/` (1 file) — all throw `NotImplementedException`.

> **Migration Principle: Don't Port 1:1.** When extracting logic from legacy files, don't mechanically move code to a new class with the same shape. Ask: in the new architecture, who OWNS this data? Put factory methods on the types themselves (e.g., `UnitDefinitions.BuildSimTemplate()`, `SimCardData.FromCardDefinition()`). Mark bridge code (e.g., old targeting → sim targeting) as temporary with the milestone where it dies.

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

- [x] All unit behaviors work (move, attack, die, spawn) — verified by `SimBehaviorTest`
- [x] `MatchState` contains all needed data: units, projectiles, summoners, phase, timer
- [x] `Simulation.Tick(delta)` advances game state deterministically
- [x] `DeterministicRng` is used for all gameplay randomness inside simulation
- [x] `SimEvent` types cover all game events (attack, damage, death, spell, buff, projectile, summoner, etc.)

### IGameSession Interface

- [x] `IGameSession` interface is finalized (`scripts/csharp/Session/IGameSession.cs`):
  - `MatchState GetState()`
  - `event Action<IReadOnlyList<SimEvent>> SimEventsEmitted`
  - `void SubmitCommand(ICommand command)`
  - `void Tick(float delta)`
- [x] `ICommand` interface exists with `PlayCardCommand` and `ForfeitCommand` implementations
- [x] `ValidationResult` type exists for `CommandRouter`

### Commands

- [x] `PlayCardCommand` — exists
- [x] `ForfeitCommand` — exists
- [x] `CastSpellCommand` — N/A: PlayCardCommand handles spells via TargetUnitId + SpawnPosition
- [x] `RedirectCommand` — deferred (Rally/Guard/Charge archived)

### Gate: Prerequisites Met

- [x] `dotnet build` succeeds
- [x] `dotnet test --settings test.runsettings` passes
- [x] All stub files compile (they throw `NotImplementedException`, which is fine)

---

## Milestone 1: UnitVisual — Unblocks Tier 1 Deletions

**Stub file:** `scripts/csharp/View/UnitVisual.cs`

**Goal:** UnitVisual renders units by reading `UnitData` from `MatchState`. Once verified, delete DamageSystem, ModifierService, ProjectileService (3 autoloads).

### 1.1: Implement UnitVisual Self-Sync

- [x] `Initialize(IGameSession session, int unitId)` — store session reference and unit ID
- [x] `_PhysicsProcess(double delta)` — read `UnitData` from `MatchState` each frame:
  - Sync `GlobalPosition` from `UnitData.Position` (SimVector3 → Godot Vector3)
  - Sync facing direction from `UnitData.Facing`
  - Drive animation state (idle, walk, attack) from `UnitData.BehaviorState`

### 1.2: Implement Event Reaction Methods

- [x] `PlayAttackAnimation()` — trigger attack animation on the visual
- [x] `FlashDamage(float damage, bool isCrit)` — damage flash VFX + floating damage number
- [x] `BeginDeath()` — death animation, then queue_free
- [x] `ShowBuffIcon(EffectType effectType)` — show buff/debuff icon above unit
- [x] `ShowEvadeText()` — show "Evade!" floating text

### 1.3: Wire UnitVisual Into Existing Battle Flow

- [x] Create a test harness that runs UnitVisual alongside Unit3D (dual-running verification)
- [x] Verify UnitVisual position matches Unit3D position within tolerance
- [x] Verify attack animations trigger at the correct times
- [x] Verify damage numbers appear correctly
- [x] Verify death plays animation and cleans up

### Gate: UnitVisual Verified

- [x] UnitVisual renders all unit types correctly (melee, ranged, duckling)
- [x] Unit3D has zero remaining unique consumers that UnitVisual can't serve
- [x] Visual parity: UnitVisual looks equivalent to Unit3D in battle

### 1.4: Tier 1 Deletions

**Delete DamageSystem (837 LOC):**
- [x] Delete `scripts/csharp/Combat/DamageSystem.cs` + `.tscn` + `.uid`
- [x] Delete `scripts/csharp/Services/Interfaces/IDamageSystem.cs` + `.uid`
- [x] Remove `DamageSystem` autoload from `project.godot`

**Delete ModifierService (714 LOC deleted, 401 LOC relocated):**
- [x] Delete `scripts/csharp/Systems/Modifiers/ModifierService.cs` + `.tscn`
- [x] Delete `scripts/csharp/Services/Interfaces/IModifierService.cs`
- [x] Delete `CardModifierProvider.cs`, `ItemModifierProvider.cs`, `SummonerModifierProvider.cs`
- [x] Delete `IModifierProvider.cs`, `ModifierContext.cs`, `ConditionKeys.cs`
- [x] **RELOCATE** `StatModifier.cs` → `scripts/csharp/Stats/StatModifier.cs` (27+ consumers!)
- [x] **RELOCATE** `TriggerCondition.cs` → `scripts/csharp/Stats/TriggerCondition.cs`
- [x] Remove `ModifierService` autoload from `project.godot`
- [x] Delete `tests/csharp/Systems/Modifiers/ModifierServiceTest.cs`
- [x] **RELOCATE** `StatModifierTest.cs` → `tests/csharp/Stats/StatModifierTest.cs`
- [x] **RELOCATE** `TriggerConditionTest.cs` → `tests/csharp/Stats/TriggerConditionTest.cs`

**Delete ProjectileService (509 LOC):**
- [x] Delete `scripts/csharp/Projectiles/ProjectileService.cs` + `.tscn` + `.uid`
- [x] Remove `ProjectileService` autoload from `project.godot`

**Update CardFactory.cs:**
- [x] Remove `ModifierService` references from `CardFactory.cs`

### Gate: Tier 1 Complete

- [x] `dotnet build` succeeds
- [x] `dotnet test --settings test.runsettings` passes
- [x] Grep `DamageSystem` — only `SimDamage` references remain
- [x] Grep `ModifierService` — zero references
- [x] Grep `ProjectileService` — zero references
- [x] Grep `IDamageSystem` — zero references
- [x] Grep `IModifierService` — zero references
- [x] `StatModifier.cs` and `TriggerCondition.cs` exist in `scripts/csharp/Stats/`
- [x] 3 autoloads removed from `project.godot`

---

## Milestone 2: Full View Layer — Unblocks Tier 2 Deletions

**Stub files:** `EntityManager.cs`, `ProjectileVisual.cs`, `SummonerVisual.cs`, `BattleScene.cs`

**Goal:** All view layer components operational. EntityManager manages lifecycle and event routing. Visual shells self-sync from MatchState.

### 2a: EntityManager — Central Lifecycle Coordinator

**Stub file:** `scripts/csharp/View/EntityManager.cs`

- [x] `Initialize(IGameSession session)` — store session, subscribe to `SimEventsEmitted`
- [x] `_PhysicsProcess(double delta)` — entity diffing:
  - Poll `MatchState` for current unit list
  - Spawn `UnitVisual` shells for new units (call `SpawnUnitShell`)
  - Destroy shells for removed units (call `DestroyShell`)
  - Spawn `ProjectileVisual` shells for new projectiles
  - Destroy projectile shells for removed projectiles
- [x] `SpawnUnitShell(UnitData unitData)` — instantiate scene, call `Initialize`
- [x] `SpawnProjectileShell(SimProjectileData projData)` — instantiate scene, call `Initialize`
- [x] `DestroyShell(int entityId)` — remove from tracking, queue_free
- [x] `RegisterSummonerVisual(SummonerVisual shell, int teamIndex)` — register pre-placed summoner shells

**ISimEventVisitor implementation (route events to visual shells):**
- [x] `Visit(UnitAttackedEvent)` → `GetShell(attackerId)?.PlayAttackAnimation()`
- [x] `Visit(UnitDamagedEvent)` → `GetShell(targetId)?.FlashDamage(damage, isCrit)`
- [x] `Visit(UnitDiedSimEvent)` → `GetShell(unitId)?.BeginDeath()`
- [x] `Visit(ProjectileHitSimEvent)` → `GetProjectileShell(projId)?.PlayImpactAndDestroy()`
- [x] `Visit(SummonerDamagedEvent)` → `GetSummonerShell(teamIndex)?.FlashDamage()`
- [x] `Visit(SummonerDestroyedEvent)` → `GetSummonerShell(teamIndex)?.BeginDeath()`
- [x] `Visit(AttackEvadedEvent)` → `GetShell(targetId)?.ShowEvadeText()`
- [x] `Visit(BuffAppliedSimEvent)` → `GetShell(unitId)?.ShowBuffIcon(effectType)`
- [x] `Visit(SpellCastEvent)` → trigger spell VFX
- [x] `Visit(DelayedEffectFiredSimEvent)` → trigger delayed effect VFX
- [x] Handle remaining no-op event types (log or ignore)
- [x] `Pause()` / `Resume()` — pause/resume visual processing

### 2b: ProjectileVisual — Self-Syncing Projectile Shell

**Stub file:** `scripts/csharp/View/ProjectileVisual.cs`

- [x] `Initialize(IGameSession session, int projectileId)` — store session ref and ID
- [x] `_PhysicsProcess(double delta)` — read `SimProjectileData` from `MatchState`:
  - Sync `GlobalPosition` from projectile position
  - Sync rotation to face movement direction
  - Manage trail effect
- [x] `PlayImpactAndDestroy()` — play impact VFX, fade trail, queue_free

### 2c: SummonerVisual — Self-Syncing Summoner Shell

**Stub file:** `scripts/csharp/View/SummonerVisual.cs`

- [x] `Initialize(IGameSession session, int teamIndex)` — store session ref and team index
- [x] `_PhysicsProcess(double delta)` — read `SummonerData` from `MatchState`:
  - Sync HP display
  - Sync mana display (for UI elements attached to summoner)
  - Update casting state visual
- [x] `FlashDamage()` — summoner hit flash VFX
- [x] `BeginDeath()` — summoner destruction animation
- [x] Own HP bar (create and manage inline, replacing HPBarService pattern)

### 2d: BattleScene — Top-Level Facade

**Stub file:** `scripts/csharp/View/BattleScene.cs`

- [x] `Initialize(IGameSession session)`:
  - Wire session to `EntityManager` (call `EntityManager.Initialize(session)`)
  - Wire session to `BattleHUD` (if applicable)
  - Set up camera, environment (state-independent)
- [x] Replace `GameController3D` as the scene root script for `battle_3d.tscn`

### 2e: Scene File Updates

**20 unit scenes — replace root script with UnitVisual:** ✅ Done (Batch C)
- [x] `scenes/units/puff_3d.tscn` (RangedUnit3D → UnitVisual)
- [x] `scenes/units/fire_spider_3d.tscn` (RangedUnit3D → UnitVisual)
- [x] `scenes/units/earth_rock_thrower_3d.tscn` (RangedUnit3D → UnitVisual)
- [x] `scenes/units/life_wisp_3d.tscn` (MeleeUnit3D → UnitVisual)
- [x] `scenes/units/fire_wisp_3d.tscn` (MeleeUnit3D → UnitVisual)
- [x] `scenes/units/lightning_wisp_3d.tscn` (MeleeUnit3D → UnitVisual)
- [x] `scenes/units/shadow_wisp_3d.tscn` (MeleeUnit3D → UnitVisual)
- [x] `scenes/units/fire_ant_3d.tscn` (MeleeUnit3D → UnitVisual)
- [x] `scenes/units/fire_titan_3d.tscn` (MeleeUnit3D → UnitVisual)
- [x] `scenes/units/stone_ape_3d.tscn` (MeleeUnit3D → UnitVisual)
- [x] `scenes/units/water_wisp_3d.tscn` (MeleeUnit3D → UnitVisual)
- [x] `scenes/units/rock_3d.tscn` (MeleeUnit3D → UnitVisual)
- [x] `scenes/units/mama_duck_3d.tscn` (MeleeUnit3D → UnitVisual)
- [x] `scenes/units/wind_wisp_3d.tscn` (MeleeUnit3D → UnitVisual)
- [x] `scenes/units/water_frog_3d.tscn` (MeleeUnit3D → UnitVisual)
- [x] `scenes/units/earth_wisp_3d.tscn` (MeleeUnit3D → UnitVisual)
- [x] `scenes/units/duckling_3d.tscn` (DucklingUnit3D → UnitVisual)
- [x] `scenes/units/earth_sprite_3d.tscn` (MeleeUnit3D → UnitVisual)
- [x] `scenes/units/death_wisp_3d.tscn` (MeleeUnit3D → UnitVisual)
- [x] `scenes/units/fire_boar_3d.tscn` (MeleeUnit3D → UnitVisual)

**1 projectile scene:** ✅ Done (Batch C)
- [x] `scenes/projectiles/base_projectile_3d.tscn` (Projectile3D → ProjectileVisual)

**Battle scene:**
- [x] `scenes/battlefield/battle_3d.tscn` (game_controller_3d → BattleScene)
- [x] Update or delete test scenes using `test_game_controller.gd` — created `TestBattleScene.cs`, updated 3 dev scenes

### 2f: Tier 2 Deletions

**Step 1 — HPBarService (563 LOC):** ✅ Done (Batch A)
- [x] Delete `scripts/csharp/Services/HPBarService.cs` + `.tscn` + `.uid`
- [x] Remove `HPBarService` autoload from `project.godot`

**Step 2 — SimEventSignalEmitter (109 LOC):** ✅ Done (Batch A)
- [x] Delete `scripts/csharp/Simulation/SimEventSignalEmitter.cs` + `.uid`
- [x] Remove signal declarations from `SimulationNode` that were only used by emitter

**Step 3 — SimulationNode slim-down (~842 LOC removed):**
- [x] Extract card/unit template building to `SimCardData.FromCardDefinition()` + `UnitDefinitions.BuildSimTemplate()` (~182 LOC removed, 944→762)
- [x] Slim `SimulationNode.cs` (766 → 293 lines) — removed signals, EmitEvents, ApplySnapshot, PreRegisterRemoteUnit, unit accessors. Remaining LOC due to GDScript bridges (RegisterSummoner, PopulateCardData, SetSummonerHand) kept until summoner.gd deletion
- [x] Game logic migrated to Session layer implementations — CommandRouter + LocalSession implemented

**Step 4 — Unit3D + subclasses + components (~3,076 LOC):** ✅ Done (Batch B)
- [x] Delete `scripts/csharp/Units/Unit3D.cs` (2,285 LOC)
- [x] Delete `scripts/csharp/Units/MeleeUnit3D.cs` (158 LOC)
- [x] Delete `scripts/csharp/Units/RangedUnit3D.cs` (257 LOC)
- [x] Delete `scripts/csharp/Units/DucklingUnit3D.cs` (38 LOC)
- [x] Delete `scripts/csharp/Units/Components/UnitHealth.cs` (139 LOC)
- [x] Delete `scripts/csharp/Units/Components/UnitMovement.cs` (199 LOC)
- [x] Delete all corresponding `.uid` files

**Step 5 — Additional legacy systems:**
- [x] Delete `scripts/csharp/Cards/Effects/` (14 files, ~1,300 LOC) — entire directory ✅ Done (Batch B)
- [x] Delete `scripts/csharp/Cards/SpellCard.cs` (71 LOC) ✅ Done (Batch B)
- [x] Delete `scripts/core/summoner.gd` (979 LOC) — replaced by SummonerVisual.cs + BattleScene.cs
- [x] Delete `scripts/csharp/Summons/UnitSpawner.cs` (419 LOC) ✅ Done (Batch B)
- [x] Delete `scripts/csharp/Movement/UnitSteering.cs` (462 LOC) ✅ Done (Batch B)
- [x] Delete `scripts/csharp/Units/Components/SpawnRevealComponent.cs` (240 LOC) ✅ Done (Batch B)
- [x] Delete `scripts/csharp/Units/UnitDebugService.cs` (67 LOC) ✅ Done (Batch B)
- [x] Remove `UnitDebugService` autoload from `project.godot` ✅ Done (cascade cleanup)

**Files to UPDATE (not delete):**
- [x] `scripts/csharp/Input/SummonPreview.cs` — fully implemented (292 LOC, includes embedded `UnitGhost` class), staged
- [x] Update `SummonPreview.cs` to read `InputCollector` drag state — InputCollector implemented
- [x] `CardFactory.cs` — verified clean (zero ModifierService refs)

**Step 6 — Projectile3D + ProjectileData (~1,445 LOC):**
- [x] Delete `scripts/csharp/Projectiles/Projectile3D.cs` (1,128 LOC) ✅ Done (Batch B)
- [x] Keep `scripts/csharp/Projectiles/ProjectileData.cs` (317 LOC) — valid visual config data, not legacy

**Step 7 — GameController3D + test controller (~1,225 LOC):** ✅ Done
- [x] Delete `scripts/core/game_controller_3d.gd` (1,048 LOC) — replaced by `BattleScene.cs`
- [x] Delete `scripts/core/test_game_controller.gd` (177 LOC) — replaced by `TestBattleScene.cs`
- [x] Updated all 10 GDScript consumers to PascalCase signal/method names + Node type hints

**Test updates:**
- [x] `tests/csharp/Multiplayer/ClientInitializationTest.cs` — verified clean (zero legacy deps)

### Gate: Tier 2 Complete

- [x] `dotnet build` succeeds
- [x] `dotnet test --settings test.runsettings` passes (441/441)
- [ ] Full view layer renders — units, projectiles, summoners all visible and animated (**manual test in Godot editor**)
- [x] Grep `Unit3D` — zero class usage (only comments/docstrings)
- [x] Grep `Projectile3D` — zero references
- [x] Grep `game_controller_3d` — zero references in `.tscn` files
- [x] Grep `SimEventSignalEmitter` — zero references
- [x] Grep `HPBarService` — zero references
- [x] Grep `Cards/Effects/` — directory deleted, zero references
- [x] Grep `SpellCard` — zero class usage (only SpellCardConfig, different class)
- [x] Grep `summoner.gd` — zero references in production code
- [x] Grep `UnitSpawner` — zero class usage (only UnitSpawnerPanel, different class)
- [x] Grep `UnitSteering` — zero references
- [x] Grep `SpawnRevealComponent` — zero class usage (View.SpawnRevealComponent is the replacement, not legacy)
- [x] Grep `UnitDebugService` — zero functional references (stale constant removed)
- [x] `SummonPreview.cs` uses UnitVisual patterns
- [x] `UnitGhost.cs` uses UnitVisual patterns
- [x] `CardFactory.cs` — no `ModifierService` references
- [x] All 20 unit scene files reference `UnitVisual`
- [x] `SimulationNode.cs` slimmed (766 → 293 lines; GDScript bridges remain until summoner.gd deletion)
- [x] 2 autoloads removed from `project.godot` (HPBarService, UnitDebugService)

---

## Milestone 3a: Session Layer — Unblocks Tier 3 Deletions

**Stub files:** `CommandRouter.cs`, `LocalSession.cs`, `HostSession.cs`, `ClientSession.cs`, `NetworkSession.cs`, `IdentityMap.cs`, `SnapshotCodec.cs`

**Parallel with Milestones 3b and 3c** — no cross-dependencies.

### 3a.1: CommandRouter — Validation Logic ✅

**Stub file:** `scripts/csharp/Session/CommandRouter.cs`

- [x] Implement `Validate(ICommand command, MatchState state)` with pattern matching
- [x] PlayCardCommand validation rules (player index, card index, mana, phase, casting state, card exists)
- [x] ForfeitCommand validation rules (player index, phase)
- [x] SpawnUnitCommand — always valid (debug/event paths)
- [x] Unit tests for validation rules + unknown command rejection

### 3a.2: LocalSession — Singleplayer (Simplest Session) ✅

**Stub file:** `scripts/csharp/Session/LocalSession.cs`

- [x] Implement constructor, Tick, SubmitCommand, GetState
- [x] Command validation via CommandRouter

### 3a.3: NetworkSession + HostSession — Multiplayer Host ✅

**IdentityMap** (56 LOC) — fully implemented: O(1) bidirectional map, register/unregister, tests passing.

**SnapshotCodec** (211 LOC) — fully implemented: BinaryWriter/BinaryReader encode/decode, round-trip tests passing.

**NetworkSession** (30 LOC) — abstract base with IdentityMap + SnapshotCodec fields. `HandleMessage` stub awaits transport wiring.

**HostSession** (71 LOC) — fully implemented: constructor, Tick, SubmitCommand with CommandRouter validation, HandleRemoteCommand. Snapshot broadcast awaits transport wiring.

**Tests:** IdentityMap, SnapshotCodec, HostSession tests all passing.

### 3a.4: ClientSession — Multiplayer Client (partial)

**ClientSession** (91 LOC) — ApplySnapshot fully implemented (copies all MatchState fields). Tick works. SubmitCommand awaits transport wiring.

**Deferred to multiplayer transport milestone:**
- PredictionBuffer + reconciliation + rollback
- Client prediction tests

### 3a.5: Tier 3 Deletions ✅

All 11 files deleted in commit 698fd327. BroadcastFieldTest deleted. SimEventCoverageTest updated.

**Data cleanup:**
- [x] Remove `UnitData.TargetNetworkId` field (already removed)
- Note: `UnitData.NetworkId` is still actively used by SnapshotCodec, ClientSession, Messages

**Stale reference cleanup:**
- [x] `csharp_autoloads.gd` — removed `UNIT_DEBUG_SERVICE` constant
- [x] `debug_menu.gd` — nulled out deleted UnitDebugService reference

### Gate: Tier 3 Complete ✅

- [x] `dotnet build` succeeds
- [x] `dotnet test --settings test.runsettings` passes
- [x] All deleted classes have zero production references

---

## Milestone 3b: Input Layer — Unblocks Tier 4 Deletions

**Stub file:** `scripts/csharp/Input/InputCollector.cs`

**Parallel with Milestones 3a and 3c** — no cross-dependencies.

### 3b.1: InputCollector — Gesture→Command

**Stub file:** `scripts/csharp/Input/InputCollector.cs`

- [x] `Initialize(playerSummoner)` — stores summoner ref, finds Camera3D, adds to group
- [x] `_CanDropData()` / `_DropData()` — DnD protocol for card drops, submits via `SimulationNode.QueuePlayCard()`
- [x] Spell targeting — N/A: PlayCardCommand handles spells, Charge/Rally/Guard archived
- [x] Forfeit — ForfeitCommand exists, wiring deferred to UI integration

**Public drag state for View to read:**
- [x] `int DraggedCardIndex` — which card is being dragged (-1 if none)
- [x] `Vector3 DragPosition` — current drag position on battlefield
- [x] `bool IsDragging` — convenience property

### 3b.2: New Command Types

- [x] `CastSpellCommand` — N/A: PlayCardCommand handles spells via TargetUnitId + SpawnPosition
- [x] `RedirectCommand` — deferred (Rally/Guard/Charge archived)
- [x] CommandRouter validation — PlayCardCommand + ForfeitCommand + SpawnUnitCommand covered

### 3b.3: BattleRNG Consumer Migration ✅

BattleRNG autoload deleted. Zero remaining consumers.
- [x] `heuristic_ai.gd` — uses BattleScene deck shuffle, no direct BattleRNG calls remain
- [x] `summoner.gd` — deleted
- [x] `online_screen.gd` — BattleRNG reference removed
- [x] `multiplayer_lobby.gd` — BattleRNG reference removed

### 3b.4: Tier 4 Deletions

- [x] Delete `scripts/ui/battle/spell_targeting_manager.gd` (375 LOC) + `.uid`
- [x] Remove `SpellTargetingManager` autoload from `project.godot`
- [x] Delete `scripts/managers/redirect_manager.gd` (402 LOC) + `.uid`
- [x] Remove `RedirectManager` autoload from `project.godot`
- [x] Delete `scripts/ui/battle/battlefield_drop_zone.gd` (515 LOC) + `.uid`
- [x] Delete `scripts/multiplayer/rng/battle_rng.gd` (207 LOC) + `.uid`
- [x] Delete `scripts/multiplayer/rng/rng_domain.gd` (30 LOC) + `.uid`
- [x] Remove `BattleRNG` autoload from `project.godot`
- [x] Delete `scripts/core/player_input.gd` (43 LOC) + `.uid`
- [x] Delete `scripts/core/player_input_3d.gd` (95 LOC) + `.uid`

**Scene file updates (5 files) — replace BattlefieldDropZone with InputCollector:**
- [x] `scenes/ui/battle/battle_hud.tscn`
- [x] `scenes/battlefield/dev/test_collision.tscn`
- [x] `scenes/battlefield/dev/test_battle_abilities.tscn`
- [x] `scenes/battlefield/dev/test_battle_vfx.tscn`
- [x] `scenes/test/rally_guard_test.tscn`

### Gate: Tier 4 Complete ✅

- [x] `dotnet build` succeeds (0 errors, 0 warnings)
- [x] `dotnet test --settings test.runsettings` passes (424/424)
- [x] InputCollector handles DnD card drops correctly
- [x] Grep `SpellTargetingManager` — zero references
- [x] Grep `RedirectManager` — zero references
- [x] Grep `BattlefieldDropZone` — zero references
- [x] Grep `BattleRNG` — zero references
- [x] Grep `player_input` — zero references
- [x] 4 autoloads removed from `project.godot` (SpellTargetingManager, RedirectManager, BattleRNG + summoner-related)
- [x] 5 scene files updated to InputCollector

---

## Milestone 3c: Capability Retirement — Tier 5 Deletions

**No stub files** — this milestone is pure deletion after verifying simulation handles everything.

**Parallel with Milestones 3a and 3b** — no cross-dependencies.

### 3c.1–3c.6: All Complete ✅

All Capabilities/, Targeting/, Combat/Hitbox/, and SpatialGrid files deleted in prior milestones.
Autoloads removed. HurtboxCategory mirror enum removed. test_targeting_config_registry.gd deleted.

### Gate: Tier 5 Complete ✅

- [x] `dotnet build` succeeds
- [x] `dotnet test --settings test.runsettings` passes
- [x] All grep checks pass — zero references to deleted systems

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
