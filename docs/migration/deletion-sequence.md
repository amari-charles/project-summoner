# Old System Deletion Sequence

Ordered deletion plan for retiring legacy systems during the four-layer migration. Each tier lists exact files, autoloads, scene updates, test changes, and verification steps.

**Scope:** ~80 code files (~18,700 LOC), 11 autoloads removed, ~25 scene files updated, 5 test files deleted or updated.

For replacement designs, see: [session design-specs.md](../architecture/gameplay/session/design-specs.md), [view design-specs.md](../architecture/gameplay/view/design-specs.md), [cross-cutting-plan.md](cross-cutting-plan.md), [meta-game-plan.md](meta-game-plan.md).

---

## Tier Overview

| Tier | Blocker | Files Deleted | LOC | Autoloads Removed |
|------|---------|--------------|-----|-------------------|
| 0 | *(none)* | 0 | 0 | 0 |
| 1 | UnitVisual replaces Unit3D | 12 | ~2,250 | 3 (DamageSystem, ModifierService, ProjectileService) |
| 2 | View layer migration complete | 34+ | ~10,440 | 2 (HPBarService, UnitDebugService) |
| 3 | Session layer migration complete | 12 | ~2,400 | 0 |
| 4 | Input layer migration complete | 7 | ~1,670 | 3 (SpellTargetingManager, RedirectManager, BattleRNG) |
| 5 | Capabilities + targeting retire | 29 | ~2,460 | 3 (TargetingConfigRegistryCS, HitResolver, SpatialGrid) |
| **Total** | | **~94** | **~19,220** | **11** |

---

## No Tier 0 — No Unblocked Deletions

There are no systems that can be deleted today without first implementing their replacement.

**BattleRNG was initially considered** for Tier 0, but has 4 active GDScript consumers:
- `heuristic_ai.gd` — AI decision randomness + spawn position randomness (11 calls)
- `summoner.gd` — deck shuffling (2 calls)
- `online_screen.gd` — seed initialization (1 call)
- `multiplayer_lobby.gd` — seed initialization (1 call)

BattleRNG is assigned to **Tier 4** (Input layer migration), when AI and summoner logic migrate to use the simulation's `DeterministicRng`.

---

## Tier 1 — Delete After UnitVisual Replaces Unit3D

**Blocker:** UnitVisual must be implemented and wired as the visual shell. Unit3D's direct references to DamageSystem, ModifierService, and ProjectileService are the only remaining consumers.

**Order:** Independent within tier — all three systems can be deleted in any order.

### Files to Delete

**DamageSystem (837 LOC):**

| File | Path | LOC |
|------|------|-----|
| `DamageSystem.cs` | `scripts/csharp/Battle/Simulation/Combat/DamageSystem.cs` | 837 |
| `DamageSystem.tscn` | `scripts/csharp/Battle/Simulation/Combat/DamageSystem.tscn` | — |
| `IDamageSystem.cs` | `scripts/csharp/Meta/Services/Interfaces/IDamageSystem.cs` | 109 |
| `.uid` files | `DamageSystem.cs.uid`, `IDamageSystem.cs.uid` | — |

**ModifierService (714 LOC deleted, 401 LOC relocated):**

| File | Path | LOC | Action |
|------|------|-----|--------|
| `ModifierService.cs` | `scripts/csharp/Systems/Modifiers/ModifierService.cs` | 454 | Delete |
| `ModifierService.tscn` | `scripts/csharp/Systems/Modifiers/ModifierService.tscn` | — | Delete |
| `IModifierService.cs` | `scripts/csharp/Meta/Services/Interfaces/IModifierService.cs` | 49 | Delete |
| `CardModifierProvider.cs` | `scripts/csharp/Systems/Modifiers/CardModifierProvider.cs` | 62 | Delete |
| `ItemModifierProvider.cs` | `scripts/csharp/Systems/Modifiers/ItemModifierProvider.cs` | 30 | Delete |
| `SummonerModifierProvider.cs` | `scripts/csharp/Systems/Modifiers/SummonerModifierProvider.cs` | 67 | Delete |
| `IModifierProvider.cs` | `scripts/csharp/Systems/Modifiers/IModifierProvider.cs` | 22 | Delete |
| `ModifierContext.cs` | `scripts/csharp/Systems/Modifiers/ModifierContext.cs` | 125 | Delete |
| `ConditionKeys.cs` | `scripts/csharp/Systems/Modifiers/ConditionKeys.cs` | 50 | Delete |
| **`StatModifier.cs`** | `scripts/csharp/Systems/Modifiers/StatModifier.cs` | 353 | **⚠️ RELOCATE to `scripts/csharp/Battle/Simulation/Stats/`** |
| **`TriggerCondition.cs`** | `scripts/csharp/Systems/Modifiers/TriggerCondition.cs` | 48 | **⚠️ RELOCATE to `scripts/csharp/Battle/Simulation/Stats/`** |
| `.uid` files | All corresponding `.cs.uid` files | — | Delete/Relocate |

> **DO NOT DELETE `StatModifier.cs` or `TriggerCondition.cs`.** These are pure data types used by 27+ files across the codebase (UnitStats, TraitDefinition, ItemService, LevelCapService, CardDefinition, UnitStatCalculator, etc.). They must be relocated to `scripts/csharp/Battle/Simulation/Stats/` when the `Systems/Modifiers/` directory is otherwise emptied.

**ProjectileService (509 LOC):**

| File | Path | LOC |
|------|------|-----|
| `ProjectileService.cs` | `scripts/csharp/Projectiles/ProjectileService.cs` | 509 |
| `ProjectileService.tscn` | `scripts/csharp/Projectiles/ProjectileService.tscn` | — |
| `.uid` file | `ProjectileService.cs.uid` | — |

### Autoloads to Remove from `project.godot`

1. `DamageSystem="*res://scripts/csharp/Battle/Simulation/Combat/DamageSystem.tscn"`
2. `ModifierService="*res://scripts/csharp/Systems/Modifiers/ModifierService.tscn"`
3. `ProjectileService="*res://scripts/csharp/Projectiles/ProjectileService.tscn"`

### Test Files

| File | Path | LOC | Action |
|------|------|-----|--------|
| `ModifierServiceTest.cs` | `tests/csharp/Systems/Modifiers/ModifierServiceTest.cs` | 152 | **Delete** |
| `StatModifierTest.cs` | `tests/csharp/Systems/Modifiers/StatModifierTest.cs` | 137 | **Relocate** (follows `StatModifier.cs`) |
| `TriggerConditionTest.cs` | `tests/csharp/Systems/Modifiers/TriggerConditionTest.cs` | 59 | **Relocate** (follows `TriggerCondition.cs`) |

### Architectural Issues Resolved

- **#1** — Duplicate damage systems (DamageSystem vs SimDamage)
- **#2** — Duplicate modifier/buff systems (ModifierService vs SimEffects)
- **#3** — Duplicate projectile systems (ProjectileService vs SimProjectile)

### Verification Checklist

- [ ] `dotnet build` succeeds
- [ ] `dotnet test --settings test.runsettings` passes
- [ ] Grep for `DamageSystem` — only `SimDamage` references remain
- [ ] Grep for `ModifierService` — zero references remain
- [ ] Grep for `ProjectileService` — zero references remain
- [ ] Grep for `IDamageSystem` — zero references remain
- [ ] Grep for `IModifierService` — zero references remain
- [ ] `StatModifier.cs` and `TriggerCondition.cs` exist in `scripts/csharp/Battle/Simulation/Stats/`
- [ ] 3 autoloads removed from `project.godot`

---

## Tier 2 — Delete After View Layer Migration

**Blocker:** View layer must be fully migrated — `EntityManager`, `UnitVisual`, `ProjectileVisual`, `BattleScene`, `BattleHUD` self-polling all operational.

**Order:** Sequential within tier (numbered). Later deletions depend on earlier ones.

### Step 1: HPBarService (563 LOC)

| File | Path | LOC |
|------|------|-----|
| `HPBarService.cs` | `scripts/csharp/Meta/Services/HPBarService.cs` | 563 |
| `HPBarService.tscn` | `scripts/csharp/Meta/Services/HPBarService.tscn` | — |
| `.uid` file | `HPBarService.cs.uid` | — |

**Autoload to remove:** `HPBarService="*res://scripts/csharp/Meta/Services/HPBarService.tscn"`

**Replacement:** Each visual shell (`UnitVisual`, `SummonerVisual`) creates and owns its own HP bar. HP bars read health from `MatchState`. See [meta-game-plan.md §1](meta-game-plan.md#1-service-assessment-table).

### Step 2: SimEventSignalEmitter (109 LOC)

| File | Path | LOC |
|------|------|-----|
| `SimEventSignalEmitter.cs` | `scripts/csharp/Battle/Simulation/SimEventSignalEmitter.cs` | 109 |
| `.uid` file | `SimEventSignalEmitter.cs.uid` | — |

**Replacement:** `EntityManager` subscribes to `IGameSession.SimEventsEmitted` directly. Signal-based bridge no longer needed.

### Step 3: SimulationNode Slim-Down (~842 LOC removed)

| File | Path | LOC | Action |
|------|------|-----|--------|
| `SimulationNode.cs` | `scripts/csharp/Battle/Simulation/SimulationNode.cs` | 942 | **Slim to ~100 lines** |

**NOT a deletion** — SimulationNode becomes a thin Godot bridge (factory + accessor). Approximately 842 lines of game logic move to Session layer implementations. What stays: `_Ready()`, `_ExitTree()`, Simulation construction, public accessor.

### Step 4: Unit3D + Subclasses + Components (~3,076 LOC)

| File | Path | LOC |
|------|------|-----|
| `Unit3D.cs` | `scripts/csharp/Units/Unit3D.cs` | 2,285 |
| `MeleeUnit3D.cs` | `scripts/csharp/Units/MeleeUnit3D.cs` | 158 |
| `RangedUnit3D.cs` | `scripts/csharp/Units/RangedUnit3D.cs` | 257 |
| `DucklingUnit3D.cs` | `scripts/csharp/Units/DucklingUnit3D.cs` | 38 |
| `UnitHealth.cs` | `scripts/csharp/Units/Components/UnitHealth.cs` | 139 |
| `UnitMovement.cs` | `scripts/csharp/Units/Components/UnitMovement.cs` | 199 |
| `.uid` files | All corresponding `.cs.uid` files | — |

**Replacement:** `UnitVisual` (pure visual shell, reads `UnitData` from `MatchState`).

**Scene files to update (20 files):** All unit scene files must be updated to reference `UnitVisual` instead of `MeleeUnit3D`/`RangedUnit3D`/`DucklingUnit3D`:

- `scenes/battle/units/puff_3d.tscn` (RangedUnit3D → UnitVisual)
- `scenes/battle/units/fire_spider_3d.tscn` (RangedUnit3D → UnitVisual)
- `scenes/battle/units/earth_rock_thrower_3d.tscn` (RangedUnit3D → UnitVisual)
- `scenes/battle/units/life_wisp_3d.tscn` (MeleeUnit3D → UnitVisual)
- `scenes/battle/units/fire_wisp_3d.tscn` (MeleeUnit3D → UnitVisual)
- `scenes/battle/units/lightning_wisp_3d.tscn` (MeleeUnit3D → UnitVisual)
- `scenes/battle/units/shadow_wisp_3d.tscn` (MeleeUnit3D → UnitVisual)
- `scenes/battle/units/fire_ant_3d.tscn` (MeleeUnit3D → UnitVisual)
- `scenes/battle/units/fire_titan_3d.tscn` (MeleeUnit3D → UnitVisual)
- `scenes/battle/units/stone_ape_3d.tscn` (MeleeUnit3D → UnitVisual)
- `scenes/battle/units/water_wisp_3d.tscn` (MeleeUnit3D → UnitVisual)
- `scenes/battle/units/rock_3d.tscn` (MeleeUnit3D → UnitVisual)
- `scenes/battle/units/mama_duck_3d.tscn` (MeleeUnit3D → UnitVisual)
- `scenes/battle/units/wind_wisp_3d.tscn` (MeleeUnit3D → UnitVisual)
- `scenes/battle/units/water_frog_3d.tscn` (MeleeUnit3D → UnitVisual)
- `scenes/battle/units/earth_wisp_3d.tscn` (MeleeUnit3D → UnitVisual)
- `scenes/battle/units/duckling_3d.tscn` (DucklingUnit3D → UnitVisual)
- `scenes/battle/units/earth_sprite_3d.tscn` (MeleeUnit3D → UnitVisual)
- `scenes/battle/units/death_wisp_3d.tscn` (MeleeUnit3D → UnitVisual)
- `scenes/battle/units/fire_boar_3d.tscn` (MeleeUnit3D → UnitVisual)

### Step 5: Additional Legacy Systems (~3,538 LOC)

Systems identified by deletion-sequence audit that were missing from the original plan.

**Cards/Effects/ Directory (14 files, ~1,300 LOC):**

| Directory | Path | LOC |
|-----------|------|-----|
| `Effects/` | `scripts/csharp/Cards/Effects/` (all 14 files) | ~1,300 |

**Replacement:** Simulation absorbs spell logic via `PlayCardCommand` processing. See [cross-cutting-plan.md §3](cross-cutting-plan.md).

**SpellCard.cs (71 LOC):**

| File | Path | LOC |
|------|------|-----|
| `SpellCard.cs` | `scripts/csharp/Cards/SpellCard.cs` | 71 |

**Replacement:** Old spell execution path — deleted with Effects. Simulation handles `PlayCardCommand`.

**summoner.gd (979 LOC):**

| File | Path | LOC |
|------|------|-----|
| `summoner.gd` | `scripts/core/summoner.gd` | 979 |

**Replacement:** `SummonerVisual` (View layer). Session init, mana/HP/hand state, and command production all move to Session + Input layers. See [view design-specs.md §5](../architecture/gameplay/view/design-specs.md). Scene files referencing `summoner.gd` need update.

**UnitSpawner.cs (419 LOC):**

| File | Path | LOC |
|------|------|-----|
| `UnitSpawner.cs` | `scripts/csharp/Summons/UnitSpawner.cs` | 419 |

**Replacement:** Simulation handles spawns; `EntityManager` creates `UnitVisual` instances from `MatchState`.

**UnitSteering.cs (462 LOC):**

| File | Path | LOC |
|------|------|-----|
| `UnitSteering.cs` | `scripts/csharp/Movement/UnitSteering.cs` | 462 |

**Replacement:** Simulation handles movement; `UnitVisual` just reads position from `MatchState`.

**SpawnRevealComponent.cs (240 LOC):**

| File | Path | LOC |
|------|------|-----|
| `SpawnRevealComponent.cs` | `scripts/csharp/Units/Components/SpawnRevealComponent.cs` | 240 |

**Replacement:** Unit3D component — dies with Unit3D (Step 4).

**UnitDebugService.cs (67 LOC):**

| File | Path | LOC |
|------|------|-----|
| `UnitDebugService.cs` | `scripts/csharp/Units/UnitDebugService.cs` | 67 |

**Replacement:** Debug overlay for Unit3D — dies with Unit3D. **⚠️ Also an autoload** — remove from `project.godot`.

**Files to UPDATE (not delete):**

| File | Path | Action |
|------|------|--------|
| `SummonPreview.cs` | `scripts/csharp/Battle/Input/SummonPreview.cs` | **UPDATE** — rewrite to read `InputCollector` drag state + create preview from `UnitVisual` instead of `Unit3D`. See [view design-specs.md §4](../architecture/gameplay/view/design-specs.md). |
| `UnitGhost.cs` | `scripts/csharp/Battle/Input/UnitGhost.cs` | **UPDATE** — rewrite to work with `UnitVisual` patterns instead of `Unit3D`. |
| `CardFactory.cs` | `scripts/csharp/Cards/CardFactory.cs` | **UPDATE** — remove `ModifierService` references in Tier 1, `SpatialGrid` references in Tier 5. See [cross-cutting-plan.md §6](cross-cutting-plan.md). |

### Step 6: Projectile3D + ProjectileData (~1,445 LOC)

| File | Path | LOC |
|------|------|-----|
| `Projectile3D.cs` | `scripts/csharp/Projectiles/Projectile3D.cs` | 1,128 |
| `ProjectileData.cs` | `scripts/csharp/Projectiles/ProjectileData.cs` | 317 |
| `.uid` files | All corresponding `.cs.uid` files | — |

**Replacement:** `ProjectileVisual` (visual shell, reads `SimProjectileData` from `MatchState`).

**Scene files to update:**
- `scenes/battle/projectiles/base_projectile_3d.tscn` (Projectile3D → ProjectileVisual)

### Step 7: GameController3D + Test Controller (~1,225 LOC)

| File | Path | LOC |
|------|------|-----|
| `game_controller_3d.gd` | `scripts/core/game_controller_3d.gd` | 1,048 |
| `test_game_controller.gd` | `scripts/core/test_game_controller.gd` | 177 |
| `.uid` files | All corresponding `.gd.uid` files | — |

**Replacement:** `BattleScene` (view layer facade).

**Scene files to update:**
- `scenes/battle/battlefield/battle_3d.tscn` (game_controller_3d → BattleScene)
- Test scenes using `test_game_controller.gd` (update or delete)

### Autoloads to Remove from `project.godot`

1. `HPBarService="*res://scripts/csharp/Meta/Services/HPBarService.tscn"`
2. `UnitDebugService` — check `project.godot` for exact autoload path

### Test Files

| File | Path | LOC | Action |
|------|------|-----|--------|
| `ClientInitializationTest.cs` | `tests/csharp/Multiplayer/ClientInitializationTest.cs` | 107 | **Update** (references to Unit3D/SimulationNode change) |

### Architectural Issues Resolved

- **#7** — SimulationNode is a god class
- **#23** — Unit3D mixes combat logic with visual rendering
- **#24** — Projectile3D mixes hit detection with VFX
- **#25** — GameController3D mixes game state with UI orchestration

### Verification Checklist

- [ ] `dotnet build` succeeds
- [ ] `dotnet test --settings test.runsettings` passes
- [ ] Grep for `Unit3D` — zero references in production code
- [ ] Grep for `Projectile3D` — zero references in production code
- [ ] Grep for `game_controller_3d` — zero references remain
- [ ] Grep for `SimEventSignalEmitter` — zero references remain
- [ ] Grep for `HPBarService` — zero references remain
- [ ] Grep for `Cards/Effects/` — directory deleted, zero references remain
- [ ] Grep for `SpellCard` — zero references in production code
- [ ] Grep for `summoner.gd` — zero references remain (only `SummonerVisual`)
- [ ] Grep for `UnitSpawner` — zero references in production code
- [ ] Grep for `UnitSteering` — zero references in production code
- [ ] Grep for `SpawnRevealComponent` — zero references remain
- [ ] Grep for `UnitDebugService` — zero references remain
- [ ] `SummonPreview.cs` updated to use `UnitVisual` patterns
- [ ] `UnitGhost.cs` updated to use `UnitVisual` patterns
- [ ] `CardFactory.cs` — `ModifierService` references removed
- [ ] All 20 unit scene files reference `UnitVisual`
- [ ] `SimulationNode.cs` is ≤100 lines
- [ ] 2 autoloads removed from `project.godot`

---

## Tier 3 — Delete After Session Layer Migration

**Blocker:** `LocalSession`, `HostSession`, `ClientSession`, `CommandRouter`, `IdentityMap`, `SnapshotCodec` must be fully implemented and tested.

**Order:** Ordered within tier: runners → session → utilities → interfaces.

**Can run in parallel with Tiers 4 and 5** (no dependencies between them after Tier 2 completes).

### Files to Delete

**Runners (727 LOC):**

| File | Path | LOC |
|------|------|-----|
| `HostRunner.cs` | `scripts/csharp/Multiplayer/Authority/HostRunner.cs` | 275 |
| `ClientRunner.cs` | `scripts/csharp/Multiplayer/Client/ClientRunner.cs` | 452 |
| `.uid` files | Corresponding `.cs.uid` files | — |

**Replacement:** `HostSession` replaces `HostRunner`, `ClientSession` replaces `ClientRunner`.

**Session + Utilities (1,130 LOC):**

| File | Path | LOC | Replacement |
|------|------|-----|-------------|
| `MatchSession.cs` | `scripts/csharp/Multiplayer/Core/MatchSession.cs` | 359 | `NetworkSession` |
| `RequestValidator.cs` | `scripts/csharp/Multiplayer/Authority/RequestValidator.cs` | 87 | `CommandRouter` |
| `NetworkIdRegistry.cs` | `scripts/csharp/Multiplayer/Core/NetworkIdRegistry.cs` | 138 | `IdentityMap` |
| `StateSnapshotBuilder.cs` | `scripts/csharp/Multiplayer/Sync/StateSnapshotBuilder.cs` | 215 | `SnapshotCodec` |
| `DesyncDetector.cs` | `scripts/csharp/Multiplayer/Sync/DesyncDetector.cs` | 331 | `DesyncChecker` (rename + rewrite — reads `MatchState` only, no scene tree) |
| `.uid` files | Corresponding `.cs.uid` files | — | — |

**ReconnectionHandler (373 LOC):**

| File | Path | LOC | Replacement |
|------|------|-----|-------------|
| `ReconnectionHandler.cs` | `scripts/csharp/Multiplayer/Core/ReconnectionHandler.cs` | 373 | Rewrite into `NetworkSession` (like DesyncDetector → DesyncChecker). See [session design-specs.md §8](../architecture/gameplay/session/design-specs.md). |

**Interfaces (174 LOC):**

| File | Path | LOC |
|------|------|-----|
| `IMatchRunner.cs` | `scripts/csharp/Multiplayer/Core/IMatchRunner.cs` | 42 |
| `IMessageBroadcaster.cs` | `scripts/csharp/Multiplayer/Core/IMessageBroadcaster.cs` | 12 |
| `HostEventBroadcaster.cs` | `scripts/csharp/Multiplayer/Authority/HostEventBroadcaster.cs` | 120 |
| `.uid` files | Corresponding `.cs.uid` files | — |

### Autoloads to Remove

None — no session-layer files are autoloads.

### Test Files

| File | Path | LOC | Action |
|------|------|-----|--------|
| `SimEventCoverageTest.cs` | `tests/csharp/Multiplayer/SimEventCoverageTest.cs` | 175 | **Update** (references to old broadcast/event types) |
| `BroadcastFieldTest.cs` | `tests/csharp/Multiplayer/BroadcastFieldTest.cs` | 262 | **Update** (references to old broadcast patterns) |

### Architectural Issues Resolved

- **#5** — NetworkId bleeds into simulation data model
- **#6** — DesyncDetector reads Godot scene tree
- **#8** — Frame counter never updated on client
- **#9** — Host bypasses RequestValidator
- **#10** — Different update mechanisms
- **#11** — ClientRunner.HandleMessage has no default case
- **#12** — Three distinct game modes with different init paths
- **#13** — Signal vs polling split
- **#14** — Card play routing divergence
- **#15** — Singleton naming inconsistency
- **#16** — Four dependency lookup patterns
- **#21** — Prediction stubs

### Verification Checklist

- [ ] `dotnet build` succeeds
- [ ] `dotnet test --settings test.runsettings` passes
- [ ] Grep for `HostRunner` — zero references remain
- [ ] Grep for `ClientRunner` — zero references remain
- [ ] Grep for `MatchSession` — zero references remain
- [ ] Grep for `RequestValidator` — zero references remain
- [ ] Grep for `NetworkIdRegistry` — zero references remain
- [ ] Grep for `StateSnapshotBuilder` — zero references remain
- [ ] Grep for `DesyncDetector` — zero references remain (only `DesyncChecker`)
- [ ] Grep for `ReconnectionHandler` — zero references remain (logic moved to `NetworkSession`)
- [ ] Grep for `IMatchRunner` — zero references remain
- [ ] `UnitData.NetworkId` and `UnitData.TargetNetworkId` fields removed

---

## Tier 4 — Delete After Input Layer Migration

**Blocker:** `InputCollector` must be fully implemented — handles spell targeting, redirects, drop zone validation, and command production.

**Can run in parallel with Tiers 3 and 5** (no dependencies between them after Tier 2 completes).

### Files to Delete

**SpellTargetingManager (375 LOC):**

| File | Path | LOC |
|------|------|-----|
| `spell_targeting_manager.gd` | `scripts/battle/ui/spell_targeting_manager.gd` | 375 |
| `.uid` file | `spell_targeting_manager.gd.uid` | — |

**Replacement:** State machine + gesture handling moves to `InputCollector`. Circle/arrow preview rendering moves to View layer. See [view design-specs.md §2](../architecture/gameplay/view/design-specs.md).

**RedirectManager (402 LOC):**

| File | Path | LOC |
|------|------|-----|
| `redirect_manager.gd` | `scripts/managers/redirect_manager.gd` | 402 |
| `.uid` file | `redirect_manager.gd.uid` | — |

**Replacement:** `RedirectCommand` type. Gesture handling → `InputCollector`. Cooldowns + forced targeting → Simulation. Visuals → View. See [view design-specs.md §3](../architecture/gameplay/view/design-specs.md).

**BattlefieldDropZone (515 LOC):**

| File | Path | LOC |
|------|------|-----|
| `battlefield_drop_zone.gd` | `scripts/battle/ui/battlefield_drop_zone.gd` | 515 |
| `.uid` file | `battlefield_drop_zone.gd.uid` | — |

**Replacement:** Drop validation + command production → `InputCollector`. Preview management → View. See [view design-specs.md §8](../architecture/gameplay/view/design-specs.md).

**Scene files to update (5 files):**
- `scenes/battle/ui/battle_hud.tscn`
- `scenes/battle/battlefield/dev/test_collision.tscn`
- `scenes/battle/battlefield/dev/test_battle_abilities.tscn`
- `scenes/battle/battlefield/dev/test_battle_vfx.tscn`
- `scenes/battle/debug/rally_guard_test.tscn`

**BattleRNG + rng_domain (237 LOC):**

| File | Path | LOC |
|------|------|-----|
| `battle_rng.gd` | `scripts/multiplayer/rng/battle_rng.gd` | 207 |
| `rng_domain.gd` | `scripts/multiplayer/rng/rng_domain.gd` | 30 |
| `.uid` files | Corresponding `.gd.uid` files | — |

**Replacement:** Simulation uses `DeterministicRng`. View uses a separate `DeterministicRng` for cosmetic randomness. See [decisions.md #16](../architecture/decisions.md).

**Consumer migration table:**

| Consumer | File | Usage | Migration Target |
|----------|------|-------|-----------------|
| `heuristic_ai.gd` | `scripts/ai/heuristic_ai.gd` | AI decision randomness, spawn position randomness (11 calls) | AI submits commands via `IGameSession`; sim handles randomness |
| `summoner.gd` | `scripts/core/summoner.gd` | Deck shuffling (2 calls) | Session handles deck shuffle at battle init |
| `online_screen.gd` | `scripts/meta/screens/online_screen.gd` | Seed initialization (1 call) | Session receives seed via `BattleConfig` |
| `multiplayer_lobby.gd` | `scripts/meta/screens/multiplayer_lobby.gd` | Seed initialization (1 call) | Session receives seed via `BattleConfig` |
| `DamageSystem.cs` | `scripts/csharp/Battle/Simulation/Combat/DamageSystem.cs` | Crit rolls (1 call) | Already deleted in Tier 1 |

**PlayerInput files (138 LOC):**

| File | Path | LOC |
|------|------|-----|
| `player_input.gd` | `scripts/core/player_input.gd` | 43 |
| `player_input_3d.gd` | `scripts/core/player_input_3d.gd` | 95 |
| `.uid` files | Corresponding `.gd.uid` files | — |

**Replacement:** `InputCollector` handles all player input.

### Autoloads to Remove from `project.godot`

1. `SpellTargetingManager="*res://scripts/battle/ui/spell_targeting_manager.gd"`
2. `RedirectManager="*res://scripts/managers/redirect_manager.gd"`
3. `BattleRNG="*res://scripts/multiplayer/rng/battle_rng.gd"`

### Test Files

None directly associated.

### Architectural Issues Resolved

None directly — Input layer migration enables cleaner command production but doesn't resolve numbered issues.

### Verification Checklist

- [ ] `dotnet build` succeeds
- [ ] `dotnet test --settings test.runsettings` passes
- [ ] Grep for `SpellTargetingManager` — zero references in production code
- [ ] Grep for `RedirectManager` — zero references in production code
- [ ] Grep for `BattlefieldDropZone` — zero references in production code
- [ ] Grep for `BattleRNG` — zero references in production code
- [ ] Grep for `player_input` — zero references in production code
- [ ] 3 autoloads removed from `project.godot`
- [ ] 5 scene files updated (no BattlefieldDropZone references)

---

## Tier 5 — Delete When Capabilities Retire

**Blocker:** All targeting and hit detection must be fully handled by simulation subsystems (`SimTargeting`, `SimProjectile`, `SimDamage`). Unit3D must be deleted (Tier 2 prerequisite).

**Can run in parallel with Tiers 3 and 4** (no dependencies between them after Tier 2 completes).

### Files to Delete

**Capabilities/ (5 interfaces, 135 LOC):**

| File | Path | LOC |
|------|------|-----|
| `IDamageable.cs` | `scripts/csharp/Capabilities/IDamageable.cs` | 30 |
| `IRangedAttacker.cs` | `scripts/csharp/Capabilities/IRangedAttacker.cs` | 32 |
| `IAreaAttacker.cs` | `scripts/csharp/Capabilities/IAreaAttacker.cs` | 22 |
| `IVfxAttacker.cs` | `scripts/csharp/Capabilities/IVfxAttacker.cs` | 20 |
| `IStatModifier.cs` | `scripts/csharp/Capabilities/IStatModifier.cs` | 31 |
| `.uid` files | All corresponding `.cs.uid` files | — |

These are Unit3D combat capability interfaces. They have no purpose once visual shells replace Unit3D. See [cross-cutting-plan.md §10](cross-cutting-plan.md#10-capabilities-interfaces).

**Targeting/ (17 files, 982 LOC):**

| File | Path | LOC |
|------|------|-----|
| `ITargetingBehavior.cs` | `scripts/csharp/Targeting/ITargetingBehavior.cs` | 236 |
| `TargetingConfig.cs` | `scripts/csharp/Targeting/TargetingConfig.cs` | 103 |
| `TargetingConfigRegistryCS.cs` | `scripts/csharp/Targeting/TargetingConfigRegistryCS.cs` | 32 |
| `TargetingConfigRegistryCS.tscn` | `scripts/csharp/Targeting/TargetingConfigRegistryCS.tscn` | — |
| `BaseAttackConstraint.cs` | `scripts/csharp/Targeting/Constraints/BaseAttackConstraint.cs` | 64 |
| `CompositeConstraint.cs` | `scripts/csharp/Targeting/Constraints/CompositeConstraint.cs` | 62 |
| `ConeConstraint3D.cs` | `scripts/csharp/Targeting/Constraints/ConeConstraint3D.cs` | 94 |
| `HorizontalConeConstraint.cs` | `scripts/csharp/Targeting/Constraints/HorizontalConeConstraint.cs` | 69 |
| `RangeConstraint.cs` | `scripts/csharp/Targeting/Constraints/RangeConstraint.cs` | 34 |
| `BaseTargetFilter.cs` | `scripts/csharp/Targeting/Filters/BaseTargetFilter.cs` | 28 |
| `CompositeTargetFilter.cs` | `scripts/csharp/Targeting/Filters/CompositeTargetFilter.cs` | 40 |
| `LayerTargetFilter.cs` | `scripts/csharp/Targeting/Filters/LayerTargetFilter.cs` | 36 |
| `ValidTargetFilter.cs` | `scripts/csharp/Targeting/Filters/ValidTargetFilter.cs` | 29 |
| `BaseTargetScorer.cs` | `scripts/csharp/Targeting/Scorers/BaseTargetScorer.cs` | 40 |
| `BelowTargetScorer.cs` | `scripts/csharp/Targeting/Scorers/BelowTargetScorer.cs` | 37 |
| `CompositeScorer.cs` | `scripts/csharp/Targeting/Scorers/CompositeScorer.cs` | 29 |
| `DistanceScorer.cs` | `scripts/csharp/Targeting/Scorers/DistanceScorer.cs` | 23 |
| `HealthScorer.cs` | `scripts/csharp/Targeting/Scorers/HealthScorer.cs` | 26 |
| `.uid` files | All corresponding `.cs.uid` files | — |

**Replacement:** `SimTargeting` handles all targeting logic in the simulation layer.

**Combat/Hitbox/ (6 files, 777 LOC):**

| File | Path | LOC |
|------|------|-----|
| `HitboxComponent.cs` | `scripts/csharp/Battle/Simulation/Combat/Hitbox/HitboxComponent.cs` | 257 |
| `HitboxLifetime.cs` | `scripts/csharp/Battle/Simulation/Combat/Hitbox/HitboxLifetime.cs` | 31 |
| `HitResolver.cs` | `scripts/csharp/Battle/Simulation/Combat/Hitbox/HitResolver.cs` | 237 |
| `HitResolver.tscn` | `scripts/csharp/Battle/Simulation/Combat/Hitbox/HitResolver.tscn` | — |
| `HitResult.cs` | `scripts/csharp/Battle/Simulation/Combat/Hitbox/HitResult.cs` | 28 |
| `HurtboxComponent.cs` | `scripts/csharp/Battle/Simulation/Combat/Hitbox/HurtboxComponent.cs` | 206 |
| `HurtboxCategory.cs` | `scripts/csharp/Battle/Simulation/Combat/Hitbox/HurtboxCategory.cs` | 18 |
| `.uid` files | All corresponding `.cs.uid` files | — |

**Replacement:** `SimProjectile` + `SimDamage` handle all hit detection and damage in the simulation layer.

**SpatialGrid (563 LOC):**

| File | Path | LOC |
|------|------|-----|
| `SpatialGrid.cs` | `scripts/csharp/Systems/SpatialGrid.cs` | 563 |
| `SpatialGrid.tscn` | `scripts/csharp/Systems/SpatialGrid.tscn` | — |
| `.uid` file | `SpatialGrid.cs.uid` | — |

**Replacement:** Simulation iterates `MatchState.Units` directly. <50 units makes spatial hashing unnecessary. See [cross-cutting-plan.md §11](cross-cutting-plan.md#11-spatialgrid-migration).

### GDScript Mirror Enum Update

Remove `HurtboxCategory` mirror enum from `scripts/infrastructure/data/unit_constants.gd` (no longer needed once C# `HurtboxCategory.cs` is deleted).

### Autoloads to Remove from `project.godot`

1. `TargetingConfigRegistryCS="*res://scripts/csharp/Targeting/TargetingConfigRegistryCS.tscn"`
2. `HitResolver="*res://scripts/csharp/Battle/Simulation/Combat/Hitbox/HitResolver.tscn"`
3. `SpatialGrid="*res://scripts/csharp/Systems/SpatialGrid.tscn"`

### Test Files

| File | Path | LOC | Action |
|------|------|-----|--------|
| `test_targeting_config_registry.gd` | `tests/unit/test_targeting_config_registry.gd` | 138 | **Delete** |

### Architectural Issues Resolved

None directly numbered — these are structural removals that follow from the simulation absorbing all game logic.

### Verification Checklist

- [ ] `dotnet build` succeeds
- [ ] `dotnet test --settings test.runsettings` passes
- [ ] Grep for `IDamageable` — zero references in production code (only sim-internal equivalents)
- [ ] Grep for `IRangedAttacker` — zero references remain
- [ ] Grep for `ITargetingBehavior` — zero references remain
- [ ] Grep for `TargetingConfig` — zero references (only sim-internal targeting)
- [ ] Grep for `HitResolver` — zero references remain
- [ ] Grep for `HitboxComponent` — zero references remain
- [ ] Grep for `SpatialGrid` — zero references remain
- [ ] `HurtboxCategory` mirror enum removed from `unit_constants.gd`
- [ ] 3 autoloads removed from `project.godot`

---

## Cross-Tier Dependency Diagram

```
Tier 1 ─────────► Tier 2 ──────┬──► Tier 3 (Session)
(DamageSystem,      (Unit3D,       │
 ModifierService,    Projectile3D, ├──► Tier 4 (Input)
 ProjectileService)  GameController,│
                     HPBarService, ├──► Tier 5 (Capabilities,
                     SimEventEmitter)    Targeting, Hitbox,
                                         SpatialGrid)
```

**Key constraints:**
- Tier 1 must complete before Tier 2 (Unit3D depends on the autoload services)
- Tier 2 must complete before Tiers 3, 4, 5 (all depend on Unit3D/GameController being retired)
- **Tiers 3, 4, 5 are independent** — they can run in parallel after Tier 2 completes

---

## Deletion Statistics

### By Tier

| Tier | Code Files | LOC | Scene Updates | Test Changes |
|------|-----------|-----|---------------|-------------|
| 1 | 12 | ~2,250 | 0 | 1 deleted, 2 relocated |
| 2 | 34+ | ~10,440 | 21 | 1 updated |
| 3 | 12 | ~2,400 | 0 | 2 updated |
| 4 | 7 | ~1,670 | 5 | 0 |
| 5 | 29 | ~2,460 | 0 | 1 deleted |
| **Total** | **~94** | **~19,220** | **26** | **2 deleted, 2 relocated, 3 updated** |

### Autoload Removal Summary

| Tier | Autoloads Removed | Names |
|------|------------------|-------|
| 1 | 3 | DamageSystem, ModifierService, ProjectileService |
| 2 | 2 | HPBarService, UnitDebugService |
| 3 | 0 | *(none are autoloads)* |
| 4 | 3 | SpellTargetingManager, RedirectManager, BattleRNG |
| 5 | 3 | TargetingConfigRegistryCS, HitResolver, SpatialGrid |
| **Total** | **11** | |

### Architectural Issues Resolved by Tier

| Tier | Issues | Numbers |
|------|--------|---------|
| 1 | 3 | #1, #2, #3 |
| 2 | 4 | #7, #23, #24, #25 |
| 3 | 12 | #5, #6, #8, #9, #10, #11, #12, #13, #14, #15, #16, #21 |
| 4 | 0 | *(input cleanup, no numbered issues)* |
| 5 | 0 | *(structural removal, no numbered issues)* |
| **Total** | **19 of 25** | (4 already resolved: #4, #19, #20, #22. 2 remain: #17 Team type, #18 Unit IDs — resolved incrementally) |
