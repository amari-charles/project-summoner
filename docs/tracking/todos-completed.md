# Completed TODOs Archive

This document archives TODOs that have been completed. For active tasks, see [todos.md](todos.md).

---

## 2026-08 Completions

### Review the Generic Activity Preparation Screen
**Completed:** 2026-08-23
**Category:** Quests / Activities / Decks / UI/UX
**Effort:** Small

Accepted the existing Start Battle modal as the current functional scaffold.
It already owns the activity objective and rules, loadout/deck validity, reward
expectation, and Start/Back actions without duplicating general Collection
management.

**Resolution Summary:**
- ✅ Retained the current generic Activity Preparation surface.
- ✅ Kept activity-specific constraints and supplied/fixed loadouts in this flow.
- ✅ Routed editable saved and activity-specific loadouts through the shared
  Collection/Deck overlay instead of maintaining a second editor.
- ✅ Deferred final typography, art, spacing, and edge-state treatment to the
  designer rather than reopening the product flow.

**Representative Files:**
- `scenes/meta/screens/academy_activity_preparation.tscn`
- `scripts/meta/screens/academy_activity_preparation.gd`

---

### Move Tutorial Dialogue Triggers to Simulation Events
**Completed:** 2026-08-23
**Category:** Architecture / Battle Flow
**Effort:** Medium

Closed by the Narrative Director replacement. The legacy GDScript battle
dialogue controller and its per-frame scene-node proximity scans were removed;
battle narrative now receives typed gameplay facts from the battle/simulation
bridge.

**Resolution Summary:**
- ✅ Removed `battle_dialogue_controller.gd` and its polling path.
- ✅ Adapted battle start, phase change, and battle resolution facts into the
  typed Narrative Director contract.
- ✅ Kept presentation in the shared narrative presenter rather than deriving
  gameplay conditions from visual nodes.
- ✅ Future proximity-driven cues must add an explicit simulation fact instead
  of restoring scene-tree polling.

**Representative Files:**
- `scripts/csharp/Battle/View/BattleScene.cs`
- `scripts/csharp/Battle/Simulation/Simulation.cs`
- `scripts/shared/narrative_dialogue_presenter.gd`

---

### Support Upgrade-Specific Resource Costs
**Completed:** 2026-08-23
**Category:** Core Game Systems / Progression
**Effort:** Small

The generic trait-acquisition transaction now supports optional point and
material requirements without coupling those costs to automatic XP leveling.

**Resolution Summary:**
- ✅ Card and Summoner XP apply automatic levels without spending gold or materials.
- ✅ Trait definitions can configure point costs, material costs, or both.
- ✅ The shared transaction validates and spends configured requirements.
- ✅ Representative material-gated authoring and affordability presentation
  remain tracked by the discovery-driven development slice rather than by a
  duplicate future-cost task.

**Representative Files:**
- `scripts/csharp/Meta/Services/Traits/TraitTreeService.cs`
- `scripts/csharp/Infrastructure/Data/Traits/TraitDefinition.cs`

---

### Add Ranked Loadout Selection to Online Matchmaking
**Completed:** 2026-08-23
**Category:** Ranked Gameplay / UI/UX
**Effort:** Small
**PR:** `#375`

Added a visible, per-summoner ranked loadout to the Online screen without
changing the deck selected for offline activities.

**Resolution Summary:**
- ✅ Persisted one ranked deck selection per summoner.
- ✅ Reused the collection/deck-management flow for ranked confirmation.
- ✅ Prevented queueing with a missing or invalid ranked deck.
- ✅ Routed matchmaking deck exchange and battle launch through ranked selection.
- ✅ Presented the active summoner and actual selected cards before queueing.

---

### Introduce Battle Progression Authority and Migrate Battle Rewards
**Completed:** 2026-08-05
**Category:** Architecture / Progression / Rewards
**Effort:** Large
**PR:** `#352`

Introduced a provider-neutral progression authority for campaign battle attempts and terminal outcomes, then migrated battle XP and first-clear rewards onto that boundary.

**Resolution Summary:**
- ✅ Persisted authority-created attempt identity before battle launch and made outcome handling atomic and idempotent.
- ✅ Awarded XP only for distinct victorious attempts; defeat and abandonment grant nothing.
- ✅ Made first-clear rewards deterministic per summoner and stable across reloads.
- ✅ Derived battle cards from authority-owned selected decks and validated campaign membership and unlock state.
- ✅ Migrated reward presentation to normalized universal offers and removed the legacy battle reward path and old-save compatibility readers.
- ✅ Passed formal PR review, 1,178 C# tests, and 237 GUT tests with 1,746 assertions.

**Remaining Work Tracked Elsewhere:**
- Secure ranked results and rating authority.
- Atomic commerce authority.
- Incremental authority boundaries for other permanent progression commands.

---

## 2026-06 Completions

### Add More Spell Cards
**Completed:** 2026-06-04
**Category:** Content
**Effort:** Variable

Closed as an active spell-expansion item by product decision: the current spell roster is enough for now.

**Resolution Summary:**
- ✅ First-pass Fire/Water/Earth/Wind spell roster runtime coverage exists.
- ✅ Debug arena access exists for the active spell roster.
- ✅ Placeholder/readability VFX exist for active elemental spells.
- ✅ No additional spell-card concepts are needed in the active TODO queue right now.

**Remaining Work Tracked Elsewhere:**
- Spell balance tuning.
- Production-quality VFX and art direction.
- Final card art/presentation.
- Academy course and loot-pool integration.

**Related Active Tracking:**
- `Clean Up Non-Production VFX`
- `Scope Remaining Content, VFX, Items, and Academy Work`

---

### Fix Puff Lateral Movement Near Summoner/Enemy Unit
**Completed:** 2026-06-04
**Category:** Units & Combat / Movement
**Effort:** Small

Closed as stale/fixed by product review after the broader movement and targeting robustness pass.

**Resolution Summary:**
- ✅ Product review indicates the Puff lateral movement issue no longer appears active.
- ✅ Completed movement work already added summoner-wrap targeting, local crowd danger masking, objective-advance steering, close-range fallback fixes, and shared summoner melee bubble targeting.
- ✅ No active repro case remains in the tracker.

**Reopen Criteria:**
- Reopen as a fresh bug with a current replay/repro if Puff lateral jitter or stall returns.

---

### Per-Summoner Portrait Cropping Configuration
**Completed:** 2026-06-04
**Category:** Summoners / UI
**Effort:** Small

Closed after confirming per-summoner portrait crop tuning exists in runtime config and is consumed by the summoner icon widget.

**Resolution Summary:**
- ✅ `SummonerConfig` carries `portrait_uv_offset` and `portrait_uv_scale`.
- ✅ `summoner_icon_widget.gd` applies those values to the circular clip shader.
- ✅ The scene material still has defaults, but per-summoner config can override them.

**Representative Files:**
- `scripts/infrastructure/summoner_config.gd`
- `scripts/meta/components/summoner_icon_widget.gd`
- `scenes/meta/components/summoner_icon_widget.tscn`

---

### Move Campaign Data Definitions to C#
**Completed:** 2026-06-04
**Category:** Architecture / Consistency
**Effort:** Medium

Closed after confirming the old GDScript campaign data files are gone and current campaign/event definitions live in C# catalogs.

**Resolution Summary:**
- ✅ `CampaignCatalog.cs` defines the current campaign surfaces.
- ✅ `EventCatalog.cs` defines campaign battle/event data.
- ✅ Old `summoners_path_data.gd` and `test_arena_data.gd` files are no longer present.

**Representative Files:**
- `scripts/csharp/Infrastructure/Data/Events/CampaignCatalog.cs`
- `scripts/csharp/Infrastructure/Data/Events/EventCatalog.cs`

---

## 2026-03 Completions

### Shift Puff Attack Angle Downward
**Completed:** 2026-03-12
**Category:** Units & Combat / Ranged
**Effort:** Small

Adjusted Puff's projectile targeting cone downward while preserving its angular spread.

**Resolution Summary:**
- ✅ Added cone-center offset support (`TargetingConeCenterOffsetDegrees`) through `UnitDefinition -> SimUnitTemplate -> UnitData`.
- ✅ Set Puff targeting cone center offset to `-20°`.
- ✅ Validated with deterministic targeting coverage.

---

### Investigate Pathfinding & Targeting System Robustness
**Completed:** 2026-03-12
**Category:** Units & Combat / Performance
**Effort:** Medium

Completed the pathfinding and targeting robustness audit, then closed the highest-risk movement, aggro, forced-target, and dense-swarm follow-ups.

**Resolution Summary:**
- ✅ Added summoner-wrap movement targeting for occupied fronts.
- ✅ Added local crowd danger masking, blocked-nav tuning, and ORCA neighbor-search tuning for dense clumps.
- ✅ Added 60-unit summoner-focus regression coverage and profiled the dense-swarm scenario.
- ✅ Closed target-switch race and forced-target release/expiry validation.
- ✅ Added commit-lock aggro chase caps and explicit out-of-aggro retarget diagnostics.
- ✅ Updated ranged targeting profile defaults for air + ground targeting.
- ✅ Added summoner soft-lock aggro preempt, no-target objective-advance steering, close-range fallback fixes, and shared summoner melee bubble targeting.

**Representative Files Changed:**
- `scripts/csharp/Battle/Simulation/Movement/MovementTargetResolver.cs`
- `scripts/csharp/Battle/Simulation/Movement/ContextSteering.cs`
- `scripts/csharp/Battle/Simulation/Combat/SimBehavior.cs`
- `tests/csharp/Simulation/BlockedUnitReproTest.cs`

---

### Improve Hit Flash Feedback for Large Units
**Completed:** 2026-03-12
**Category:** Visual Polish
**Effort:** Small

Improved hit-flash behavior so large, durable units do not appear permanently lit while taking frequent low-impact hits.

**Resolution Summary:**
- ✅ Added configurable flash rate-limiting in both `SpriteVisualComponent` and `SkeletalVisualComponent`.
- ✅ Added separate minimum flash interval tuning for large units via width threshold.

**Representative Files Changed:**
- `scripts/csharp/Battle/View/Visual/SpriteVisualComponent.cs`
- `scripts/csharp/Battle/View/Visual/SkeletalVisualComponent.cs`

---

### Audit Summoner Secondary Stats
**Completed:** 2026-03-12
**Category:** Summoners / Stats
**Effort:** Small

Audited whether summoner secondary stats such as `damage_bonus` and `damage_reduction` are active, useful, and documented.

**Resolution Summary:**
- ✅ Verified `damage_bonus` and `damage_reduction` are consumed by simulation damage paths.
- ✅ Added clarifying in-code documentation for summoner-vs-unit and summoner-target lane behavior.
- ✅ Confirmed no dead-field removal is required in the current runtime.

**Representative Files Changed:**
- `scripts/infrastructure/data/summoner_instance.gd`
- `scripts/csharp/Battle/Simulation/Combat/SimDamage.cs`
- `scripts/csharp/Infrastructure/Data/Traits/TraitDefinitions.cs`

---

### Create Simulation Spatial Domain
**Completed:** 2026-03-12
**Category:** Architecture / Layering
**Effort:** Small

Created a dedicated simulation-owned spatial namespace for geometry, partition, and lane logic.

**Resolution Summary:**
- ✅ Moved `VirtualLanes` to `scripts/csharp/Battle/Simulation/Spatial/VirtualLanes.cs`.
- ✅ Updated simulation movement and combat consumers to `Fateforged.Simulation.Spatial`.
- ✅ Kept the refactor behavior-preserving.

**Representative Files Changed:**
- `scripts/csharp/Battle/Simulation/Spatial/VirtualLanes.cs`
- simulation movement and combat consumers

---

### Consolidate Battlefield Spawn Rules to C# Source-of-Truth
**Completed:** 2026-03-12
**Category:** Architecture / Consistency
**Effort:** Small

Removed mirrored GDScript spawn-rule helpers so spawn validation and clamping remain owned by C# battlefield bounds.

**Resolution Summary:**
- ✅ Removed mirrored spawn-rule helpers from `battlefield_constants.gd`.
- ✅ Kept spawn validation/clamping authority in C# `BattlefieldBounds`.
- ✅ Updated GDScript tests for remaining conversion/constants behavior.

**Representative Files Changed:**
- `scripts/battle/battlefield/battlefield_constants.gd`
- `scripts/csharp/Infrastructure/Constants/BattlefieldBounds.cs`
- `scripts/csharp/Battle/Input/InputCollector.cs`

---

### Add Timeouts to UI Async Waits
**Completed:** 2026-03-12
**Category:** Performance / Reliability
**Effort:** Small

Added timeout and fallback guards to UI async flows so missing signals do not leave screens permanently blocked.

**Resolution Summary:**
- ✅ Added timeout/fallback behavior to title and event screen async waits.
- ✅ Improved resilience for slower devices and interrupted UI flows.

**Representative Files Changed:**
- `scripts/meta/screens/title_screen.gd`
- `scripts/meta/screens/event_screen.gd`

---

## 2026-04 Completions

### Refactor Debug Arena Infrastructure for Extendability
**Completed:** 2026-04-20
**Category:** Architecture / Tooling
**Effort:** Medium

Completed Debug Arena extendability hardening with typed bridge contracts, unified deck-source resolution, and focused regression coverage for debug deck behavior.

**Resolution Summary:**
- ✅ Introduced typed spawner panel bridge contract and factory path for `DebugArenaScene` integration.
- ✅ Added `DebugArenaDeckProvider` abstraction with explicit source modes (file/context/override precedence).
- ✅ Unified deck resolution between debug scene and panel sync flow.
- ✅ Replaced broad “all summons” fallback with curated fallback deck behavior.
- ✅ Added regression coverage for provider mode precedence, scene/panel parity, and test arena consistency/localization integrity.

**Representative Files Changed:**
- `scripts/csharp/Battle/View/Debug/DebugArenaScene.cs`
- `scripts/csharp/Battle/View/Debug/SpawnerPanel/DebugArenaSpawnerPanelBridge.cs`
- `scripts/csharp/Battle/View/Debug/DeckSources/DebugArenaDeckProvider.cs`
- `scripts/battle/ui/debug/unit_spawner_panel.gd`
- `scripts/debug/debug_menu.gd`
- `tests/csharp/View/DebugArenaSceneTest.cs`
- `tests/csharp/View/DebugArenaDeckProviderTest.cs`
- `tests/csharp/Services/TestArenaCatalogConsistencyTest.cs`
- `tests/csharp/Services/EventLocalizationKeyIntegrityTest.cs`
- `tests/unit/test_debug_arena_unit_spawner_panel_stub.gd`
- `tests/unit/test_debug_menu_stub.gd`

---

### Finish Trait Rule Centralization
**Completed:** 2026-03-10
**Category:** Meta / Trait Tree
**Effort:** Small

Closed trait-rule duplication cleanup by routing spendability evaluation through the centralized C# trait-tree service in non-screen surfaces.

**Resolution Summary:**
- ✅ Replaced dev console trait spendability checks with centralized trait-tree service payload/reason output
- ✅ Audited remaining trait UI surfaces and removed local eligibility recomputation where centralized payload already exists

**Representative Files Changed:**
- `scripts/debug/dev_console.gd`
- `scripts/meta/screens/trait_tree_screen.gd`
- `scripts/meta/screens/card_trait_tree_screen.gd`

---

### Complete DamageProfile-Based Armor/MagicResist Integration
**Completed:** 2026-03-09
**Category:** Units & Combat / Stats
**Effort:** Medium

Completed combat correctness integration for mixed damage lanes and summoner combat-modifier wiring in simulation.

**Resolution Summary:**
- ✅ Integrated mixed `DamageProfile` routing into `SimDamage` using physical and elemental split lanes
- ✅ Added runtime data propagation: `UnitDefinitions -> SimUnitTemplate -> UnitData`
- ✅ Wired summoner combat modifiers (`damage_bonus`, `damage_reduction`, elemental buckets) from computed profile stats into simulation state
- ✅ Added/updated deterministic coverage for split-lane math, elemental bonus matching, template mapping, and spawn propagation
- ✅ Ran C# and GUT validation passes successfully
- ✅ Product decision: explicit damage-type indicators on hand cards are not required for this task

**Representative Files Changed:**
- `scripts/csharp/Battle/Simulation/Combat/SimDamage.cs`
- `scripts/csharp/Infrastructure/Data/Units/UnitDefinitions.cs`
- `scripts/csharp/Battle/Simulation/Data/SimCardData.cs`
- `scripts/csharp/Battle/Simulation/Data/UnitData.cs`
- `scripts/csharp/Battle/Session/BattleSessionFactory.cs`
- `scripts/csharp/Battle/Simulation/SimulationNode.cs`
- `scripts/csharp/Battle/View/BattleScene.cs`
- `tests/csharp/Simulation/SimDamageTest.cs`
- `tests/csharp/Simulation/UnitDefinitionsTargetingProfileTest.cs`
- `tests/csharp/Simulation/SimulationIntegrationTest.cs`

---

### Implement Ranked Gameplay Mode
**Completed:** 2026-03-09
**Category:** Core Game Systems / Multiplayer
**Effort:** Large

Completed ranked mode phase 4 closeout with production flow polish and deterministic local Nakama E2E coverage.

**Resolution Summary:**
- ✅ Queue UI/match-found polish completed (state-driven animation wiring on `online_screen`)
- ✅ Matchmaking opponent metadata extraction fixed (user, username, summoner, rating)
- ✅ Combined leaderboard refresh path completed (top players + player rank in one cycle)
- ✅ Dedicated E2E Nakama namespace (`ranked_e2e`) with fixed ports and client endpoint CLI overrides
- ✅ Automated E2E harness gates A/B/C/D/E passing in one run, including reconnect smoke

**Representative Files Changed:**
- `scripts/csharp/Infrastructure/Backend/NakamaGameClient.cs`
- `scripts/csharp/Meta/Matchmaking/MatchmakingService.cs`
- `scripts/csharp/Meta/Ranking/LeaderboardService.cs`
- `scripts/meta/screens/online_screen.gd`
- `tools/run_ranked_e2e.sh`
- `docs/technical/ranked-e2e.md`

---

### Audit Sim/Visual State Desync Points
**Completed:** 2026-03-08
**Category:** Architecture / Simulation
**Effort:** Medium

Completed a full desync audit pass across battle sim/view boundaries and closed remaining high-risk sync gaps.

**Resolution Summary:**
- ✅ Phase sync hardened: mapped sim `GamePhase` to UI phase values, deduped `PhaseChanged` emission, and added regression tests.
- ✅ Summoner destruction sync fixed: removed duplicate destroy-signal path and validated single emission with tests.
- ✅ Activation sync fixed: `UnitVisual` now respects sim `ActivationState` (inactive units no longer play walk/attack animation logic).
- ✅ Position/death/targeting audit validated: no additional stale-position, alive/dead, or stale-target ownership issues found in current battle flow wiring.

**Representative Files Changed:**
- `scripts/csharp/Battle/View/BattleScene.cs`
- `scripts/csharp/Battle/View/EntityManager.cs`
- `scripts/csharp/Battle/View/UnitVisual.cs`
- `tests/csharp/View/BattleSceneTest.cs`
- `tests/csharp/View/EntityManagerTest.cs`
- `tests/csharp/View/UnitVisualStateSyncTest.cs`

---

### Investigate Units Getting Stuck in Idle When Blocked
**Completed:** 2026-03-08
**Category:** Units & Combat / Pathfinding
**Effort:** Medium

Closed blocked-unit idle freeze follow-through after movement pipeline fixes, deterministic regression coverage, and manual validation signoff.

**Resolution Summary:**
- ✅ Movement/block reset fixes merged in movement pipeline refactor
- ✅ Deterministic repro coverage added (`tests/csharp/Simulation/BlockedUnitReproTest.cs`)
- ✅ Manual in-battle verification signoff completed (tracker close)

**PR/Commit Context:**
- PR `#287` (`refactor(simulation): movement intent + ORCA pipeline and stability fixes`)
- Commit `27462750` (blocked-nav reset edge-case fix)

**Representative Files Changed:**
- `scripts/csharp/Battle/Simulation/Movement/BlockedNavigationController.cs`
- `scripts/csharp/Battle/Simulation/Movement/SimMovement.cs`
- `scripts/csharp/Battle/Simulation/Movement/SimSteering.cs`
- `tests/csharp/Simulation/BlockedUnitReproTest.cs`

---

### Eliminate Remaining GDScript Unsafe Variant Access Warnings
**Completed:** 2026-03-08
**Category:** Architecture / Type Safety
**Effort:** Medium

Completed a broad GDScript typed-API migration pass to remove unsafe Variant access patterns and tighten boundary checks across UI/application/service wrapper flows.

**PR Merge Date:** 2026-03-08 (`#288`)

**Representative Files Changed:**
- `scripts/application/battle_context.gd`
- `scripts/application/event_context.gd`
- `scripts/application/scene_coordinator.gd`
- `scripts/infrastructure/services/*.gd` (typed wrapper updates)
- `scripts/meta/screens/reward_screen.gd`
- `tests/unit/test_service_api_wrappers.gd`

---

### Normalize StringName-Safe String Coercion Across GDScript
**Completed:** 2026-03-08
**Category:** Architecture / Type Safety
**Effort:** Medium

Standardized Variant-to-string coercion to safely accept both `String` and `StringName` across event/UI/data paths, preventing silent fallbacks when values cross C#/GDScript boundaries.

**PR Merge Date:** 2026-03-08 (`#290`)

**Representative Files Changed:**
- `scripts/infrastructure/safe_type_utils.gd`
- `scripts/meta/components/node_panels/typed_event_data.gd`
- `scripts/application/event_sequencer.gd`
- `scripts/meta/screens/event_screen.gd`
- `scripts/infrastructure/element_types.gd`
- `tests/unit/test_safe_type_utils.gd`
- `tests/unit/test_element_types.gd`

---

### Refactor Service Handlers to Typed-Only Internal Methods
**Completed:** 2026-03-08
**Category:** Architecture / Type Safety
**Effort:** Medium

Completed typed-internal handler migration so `string` IDs are now bounded to GDScript-facing entry points while internal C# flows operate on typed value objects.

**Commit Context:**
- `6aacae87` (`refactor: typed-only internal APIs for service handlers`)

**Representative Files Changed:**
- `scripts/csharp/Meta/Services/Campaign/Handlers/CampaignProgressHandler.cs`
- `scripts/csharp/Meta/Services/Campaign/Handlers/CampaignRewardHandler.cs`
- `scripts/csharp/Meta/Services/Cards/Handlers/CardOwnershipHandler.cs`
- `scripts/csharp/Meta/Services/Cards/Handlers/CardProgressionHandler.cs`
- `scripts/csharp/Meta/Services/Deck/Handlers/DeckCrudHandler.cs`
- `scripts/csharp/Meta/Services/Economy/EconomyService.cs`
- `scripts/csharp/Meta/Services/Summoner/SummonerProgressionService.cs`

---

### Add Loading Screen with Asset Preloading
**Completed:** 2026-03-08
**Category:** UI/UX / Performance
**Effort:** Medium

Completed loading transition flow with real threaded preloading and progress display to remove first-spawn hitching.

**Commit Context:**
- `edc058c7` (`feat(loading): replace fake loading bar with real ResourcePreloader`)
- `a089a971` (`polish(preloader): ... fix progress smoothness`)

**Representative Files Changed:**
- `scripts/csharp/Infrastructure/ResourcePreloader.cs`
- `scripts/meta/screens/title_screen.gd`
- `scripts/meta/screens/campaign_map.gd`

---

### Fix Orthographic Camera Mode Toggle
**Completed:** 2026-03-06
**Category:** Battle / Camera
**Effort:** Small

Replaced Godot's `project_ray_origin()`/`project_ray_normal()` with analytical ray math in ortho branches of `get_ground_footprint_xz()` and `_get_horizontal_sample_bounds_x()`. Godot's projection matrix was stale immediately after mode switch, causing the zoom solver to collapse `max_ortho_size` to `min_ortho_size`. Added regression test with realistic map bounds. PR #283.

**Superseded:** 2026-03-09

Battle camera orthographic mode was removed in PR #297 (`refactor(camera): remove orthographic battle camera support`). Runtime camera behavior is now perspective-only.

---

### Complete PendingReward Typed Domain Object Migration
**Completed:** 2026-03-06
**Category:** Architecture / Type Safety
**Effort:** Small

Replaced `CampaignProgress.PendingReward` from `Dictionary<string, object>?` to typed `PendingRewardData` class with `BattleId`, `RewardType`, `ChoiceIndex`, and `CaravanPurchases` fields. Updated CampaignRewardHandler, ProfileRepository caravan methods, and DtoConverters serialization. GDScript API unchanged.

---

### Eliminate Dynamic Call() in BattleSessionFactory
**Completed:** 2026-03-06
**Category:** Architecture / Type Safety
**Effort:** Small

Replaced 5 dynamic `Call()` sites in `BattleSessionFactory.cs` with typed `GetNodeOrNull<T>()` calls to `DeckService`, `CardService`, `ProfileRepository`, and `SummonerSelectionService`. Eliminates runtime string-based dispatch and enables compile-time method validation.

---

## 2026-02 Completions

### Host-Authoritative Simulation Rewrite (Phases 0-8)
**Completed:** 2026-02-27
**Category:** Architecture / Multiplayer / Core Game Systems
**Effort:** Very Large

**Description:**
Complete rewrite of the simulation architecture to a host-authoritative model. All combat, card play, movement, targeting, damage, and win conditions now run inside `Simulation.Tick()` on the host. Clients receive events and snapshots from the host.

**Phases Completed:**
- Phase 0: Host-only Tick + fixed timestep accumulator
- Phase 1: Data model foundation (damage types, groups, effects, defense stats)
- Phase 2: Command-based card play + prep→battle transition
- Phase 3: Read-only Unit3D (presentation-only, no MatchState writes)
- Phase 4: Summoner damage + flexible win conditions in simulation
- Phase 5: Abilities & triggers in simulation
- Phase 6: Spell cards via effect system
- Phase 7: Wire multiplayer (host broadcasts events/snapshots, client receives)
- Phase 8: Dead code removal & polish

**Key Accomplishments:**
- ✅ Single-player battles fully driven by deterministic simulation
- ✅ Multiplayer: host runs Tick(), client receives events + snapshots
- ✅ Command queue for all player input (card plays, forfeits)
- ✅ Physical/magic damage types with defense stats
- ✅ Configurable win conditions (destroy base, survive time, timed destroy, kill count)
- ✅ State snapshot builder for client sync
- ✅ RequestValidator validates card-in-hand, mana cost, casting state
- ✅ Dead code cleanup: removed 10 unused SimulationNode methods, 3 dead ability scripts

**Files Changed:**
- `scripts/csharp/Battle/Simulation/` - Core simulation (Simulation.cs, SimulationNode.cs, MatchState, etc.)
- `scripts/csharp/Multiplayer/` - HostRunner, ClientRunner, RequestValidator, StateSnapshotBuilder
- `scripts/csharp/Units/Unit3D.cs` - Read-only presentation node
- `scripts/core/summoner.gd` - Card play via command queue
- `docs/rewrite-research/implementation-plan.md` - Full plan with all phases

**Branch:** `feature/host-authoritative-sim`

---

### Multiplayer Request Validation (Partial)
**Completed:** 2026-02-27
**Category:** Multiplayer / Anti-cheat
**Effort:** Small

**Description:**
Request validation now checks card-in-hand bounds, mana cost, and casting state for multiplayer card play requests. This was originally implemented in `RequestValidator` and later consolidated into `CommandRouter` during the host-authoritative refactor.

**Follow-up Completion:**
- Fully completed on 2026-03-05 (see "Complete Multiplayer Request Validation" in the 2026-03 section)

**Files Changed:**
- `scripts/csharp/Multiplayer/Authority/RequestValidator.cs` (historical implementation)
- `scripts/csharp/Battle/Session/CommandRouter.cs` (current location after refactor)

---

## 2026-03 Completions

### Allow Camera Panning Up to Boundary When Zoomed In
**Completed:** 2026-03-05
**Category:** Camera / Controls
**Effort:** Small

**Description:**
Completed camera-boundary follow-through so zoom and pan use projected ground-footprint clamping, including zoom-limit solving and drag-pan pre-constrained motion at map edges.

**PR Merge Date:** 2026-03-05 (`#267`)

**Files Changed:**
- `scripts/battle/battlefield/camera_controller_3d.gd`
- `tests/unit/test_camera_controller_3d.gd`
- `docs/tracking/bugs-resolved.md`

---

### Improve Projectile Collision Detection for 2.5D Sprites
**Completed:** 2026-03-05
**Category:** Units & Combat / Projectiles
**Effort:** Medium

**Description:**
Implemented hit-geometry v1 for projectiles to make contacts more forgiving and deterministic for 2.5D gameplay:
- First-contact segment resolution with deterministic nearest-contact ordering
- Effective contact math using `projectile.HitRadius + target.SeparationRadius`
- Hit-space modes (`GroundCylinder` and `Sphere3D`)
- Per-projectile anti-repeat-hit guard
- Debug overlay markers for projectile hit geometry and AoE radius

**PR Merge Date:** Pending (`#269`)

**Files Changed:**
- `scripts/csharp/Battle/Simulation/Combat/SimProjectile.cs`
- `scripts/csharp/Battle/Simulation/Data/SimProjectileData.cs`
- `scripts/csharp/Infrastructure/Data/Projectiles/ProjectileData.cs`
- `scripts/csharp/Infrastructure/Data/Projectiles/ProjectileHitSpace.cs`
- `scripts/csharp/Battle/View/ProjectileVisual.cs`
- `scripts/csharp/Battle/Session/Protocol/Messages.cs`
- `scripts/csharp/Battle/Session/Protocol/MessageSerializer.cs`
- `scripts/csharp/Battle/Session/HostSession.cs`
- `scripts/csharp/Battle/Session/ClientSession.cs`
- `scripts/debug/debug_menu.gd`
- `tests/csharp/Simulation/SimProjectileTest.cs`
- `tests/csharp/Multiplayer/MessageSerializerTest.cs`
- `tests/csharp/Session/NetworkSessionWiringTest.cs`
- `docs/technical/runtime/hit-geometry-v1.md`

---

### Implement Puff Target Stickiness + Cone-Aware Target Preference
**Completed:** 2026-03-05
**Category:** Units & Combat / Targeting
**Effort:** Medium

**Description:**
Implemented policy-based targeting to stop unnecessary target churn for Puff and other cone-sensitive units.

**Key Accomplishments:**
- ✅ Added policy-based targeting (`TargetPolicyId`, registry, and policy implementations)
- ✅ Added `PreferAttackableAndStick` behavior to keep valid current targets
- ✅ Prioritized attackable-now targets before score-only fallback selection
- ✅ Added typed targeting profiles/tunables in unit definitions
- ✅ Added simulation tests for cone-aware selection and lock-expiry keep-current behavior

**PR Merge Date:** 2026-03-05 (`#270`)

**Files Changed:**
- `scripts/csharp/Battle/Simulation/Combat/SimBehavior.cs`
- `scripts/csharp/Battle/Simulation/Combat/SimTargeting.cs`
- `scripts/csharp/Battle/Simulation/Combat/Targeting/TargetPolicyRegistry.cs`
- `scripts/csharp/Battle/Simulation/Combat/Targeting/PreferAttackableAndStickTargetPolicy.cs`
- `scripts/csharp/Infrastructure/Data/Units/UnitDefinitions.cs`
- `tests/csharp/Simulation/SimBehaviorTest.cs`
- `tests/csharp/Simulation/SimTargetingTest.cs`
- `tests/csharp/Simulation/TargetPolicyRegistryTest.cs`
- `tests/csharp/Simulation/UnitDefinitionsTargetingProfileTest.cs`

---

### Verify Wisp Single-Target Behavior After Major Refactor
**Completed:** 2026-03-05
**Category:** Units & Combat / Targeting
**Effort:** Small

**Description:**
Validated the previously reported Wisp multi-target issue after the major simulation/targeting refactor and confirmed it is no longer reproducible in the current architecture.

**Outcome:**
- ✅ Verified current wisp behavior is single-target
- ✅ Confirmed no additional code fix was required
- ✅ Updated trackers to move the bug from active to resolved

**Refactor Context:**
- Host-authoritative simulation rewrite (`#260`)
- Policy-based targeting refactor (`#270`)

**Related Files:**
- `scripts/csharp/Battle/Simulation/Combat/SimBehavior.cs`
- `scripts/csharp/Battle/Simulation/Combat/SimTargeting.cs`
- `scripts/csharp/Infrastructure/Data/Units/UnitDefinitions.cs`
- `docs/tracking/bugs.md`
- `docs/tracking/bugs-resolved.md`

---
### Complete Multiplayer Request Validation
**Completed:** 2026-03-05
**Category:** Multiplayer / Anti-cheat
**Effort:** Small

**Description:**
Completed the remaining command validation work in `CommandRouter` by adding:
- Spawn position bounds validation
- Team spawn-zone validation for summon cards
- Play-command rate limiting

This closes the previously tracked "partial" validation gap from the host-authoritative migration.

**PR Merge Date:** Pending (local completion captured in audit; no merged PR tag yet)

**Files Changed:**
- `scripts/csharp/Battle/Session/CommandRouter.cs`
- `scripts/csharp/Infrastructure/Constants/BattlefieldBounds.cs`
- `tests/csharp/Session/CommandRouterTest.cs`
- `tests/csharp/Session/LocalSessionTest.cs`

---

### Investigate MP Client Casting Signal
**Completed:** 2026-03-05
**Category:** Multiplayer / Battle UI
**Effort:** Small

**Description:**
Resolved MP client casting signal reconstruction so `SummonerVisual` no longer emits null casting card payloads during polling/reconnect scenarios.

**Key Accomplishments:**
- ✅ Added `CastingCatalogId` to realtime `SummonerState` protocol snapshot payload
- ✅ Threaded `CastingCatalogId` through host snapshot build, serializer, and client snapshot apply
- ✅ Updated MP polling casting flow in `SummonerVisual` to reconstruct `Card` payloads from authoritative state
- ✅ Added runtime tests for `SummonerVisual` casting signal behavior and invalid-catalog failure path

**PR Merge Date:** Pending (`#266`)

**Files Changed:**
- `scripts/csharp/Battle/Session/Protocol/Messages.cs`
- `scripts/csharp/Battle/Session/Protocol/MessageSerializer.cs`
- `scripts/csharp/Battle/Session/HostSession.cs`
- `scripts/csharp/Battle/Session/ClientSession.cs`
- `scripts/csharp/Battle/View/SummonerVisual.cs`
- `tests/csharp/Multiplayer/MessageSerializerTest.cs`
- `tests/csharp/Session/NetworkSessionWiringTest.cs`
- `tests/csharp/View/SummonerVisualTest.cs`

---

### Multiplayer Opponent Summoner Stats Exchange
**Completed:** 2026-03-01
**Category:** Multiplayer / Ranked Gameplay
**Effort:** Small

**Description:**
Previously the multiplayer battle setup hardcoded the opponent summoner as "ignis" with an empty deck. Now both players exchange their real summoner instance data and deck during match setup, and the host applies the opponent's summoner bonuses correctly.

**Key Accomplishments:**
- ✅ Both players send summoner instance data alongside deck during match setup
- ✅ Host reconstructs opponent `SummonerInstance` from exchanged data before initializing the battle
- ✅ Added `set_summoner_instance()` to `Summoner` so the host can inject enemy stats
- ✅ Summoner bonuses (HP, attack, etc.) applied for any loaded instance, not just the local player
- ✅ Only the local player summoner stats are cached in `BattleContext` (opponent excluded)
- ✅ `configure_multiplayer_battle()` receives and forwards `opponent_summoner_data`

**Completed (also in this batch):**
- ✅ Sync `MaxHp` from host to client in `UnitState` protocol message — clients were setting `MaxHp = CurrentHp`, so damaged units appeared at full health; added `MaxHp` to `UnitState`, `StateSnapshotBuilder`, `MessageSerializer`, and `ApplySnapshot`

**Files Changed:**
- `scripts/application/battle_context.gd` - Scope opponent summoner data out of BattleContext cache
- `scripts/core/game_controller_3d.gd` - Pass opponent summoner data through battle init
- `scripts/core/summoner.gd` - Add `set_summoner_instance()`, apply bonuses for any instance
- `scripts/meta/screens/online_screen.gd` - Exchange summoner instance data during match setup
- `scripts/csharp/Multiplayer/Protocol/Messages.cs` - Add `MaxHp` to `UnitState`
- `scripts/csharp/Multiplayer/Protocol/MessageSerializer.cs` - Serialize/deserialize `MaxHp`
- `scripts/csharp/Multiplayer/Sync/StateSnapshotBuilder.cs` - Include `MaxHp` in snapshots
- `scripts/csharp/Battle/Simulation/SimulationNode.cs` - Apply `MaxHp` in `ApplySnapshot`

**Commits:** `2d8bfca4`, `846f068e`
**Branch:** `feature/host-authoritative-sim`

---

### Implement OnDeath Trigger for Modifier System
**Completed:** 2026-03-04
**Category:** Units & Combat / Modifiers
**Effort:** Small

**Description:**
OnDeath triggers are now wired in the simulation death pipeline via `SimEffects.FireDeathTriggers()`, called from both melee/ranged and projectile kill paths.

**PR Merge Date:** 2026-03-04 (`#260`)

**Files Changed:**
- `scripts/csharp/Battle/Simulation/Subsystems/SimEffects.cs`
- `scripts/csharp/Battle/Simulation/Combat/SimBehavior.cs`
- `scripts/csharp/Battle/Simulation/Combat/SimProjectile.cs`

---

### Implement Periodic Trigger for Modifier System
**Completed:** 2026-03-04
**Category:** Units & Combat / Modifiers
**Effort:** Small

**Description:**
Periodic trigger ticking is implemented through `TickPeriodicTriggers()` in `SimEffects`, with interval/timer handling on active triggers.

**PR Merge Date:** 2026-03-04 (`#260`)

**Files Changed:**
- `scripts/csharp/Battle/Simulation/Subsystems/SimEffects.cs`
- `scripts/csharp/Battle/Simulation/Enums/EffectTypes.cs`

---

### Replace /root/VFXManager Lookup in ProjectileVisual
**Completed:** 2026-03-04
**Category:** Architecture / Maintainability
**Effort:** Trivial

**Description:**
`ProjectileVisual` no longer does `/root/VFXManager` lookup. The projectile view path was refactored in the host-authoritative migration and the old lookup pattern was removed.

**PR Merge Date:** 2026-03-04 (`#260`)

**Files Changed:**
- `scripts/csharp/Battle/View/ProjectileVisual.cs`

---

## 2026-01 Completions

### Migrate ProfileRepository.UpdateCard to Typed CardUpdate DTO
**Completed:** 2026-01-27
**Category:** Architecture / Type Safety
**Effort:** Small

**Description:**
`ProfileRepository.UpdateCard()` and related call paths were migrated from loose dictionary-based card update payloads to a typed DTO (`CardUpdate`), reducing primitive/dictionary misuse in fixed-schema update paths.

**PR Merge Date:** 2026-01-27 (`#219`)

**Files Changed:**
- `scripts/csharp/Meta/Domain/Profile/Collection/CardUpdate.cs`
- `scripts/csharp/Infrastructure/Persistence/IProfileRepository.cs`
- `scripts/csharp/Infrastructure/Persistence/ProfileRepository.cs`
- `scripts/csharp/Meta/Services/Cards/Handlers/CardProgressionHandler.cs`

---

### Replace Synchronous Unit Preloading with Async Loading
**Completed:** 2026-01-17
**Category:** Performance / Loading
**Effort:** Medium

**Description:**
Battle startup unit preloading was migrated to async threaded loading using `ResourceLoader.LoadThreadedRequest()` to avoid synchronous startup stalls.

**PR Merge Date:** 2026-01-17 (`perf/async-unit-preloading`)

**Files Changed:**
- `scripts/csharp/Battle/View/BattleScene.cs`

---

### Migrate deck_service.gd / summoner_progression_service.gd / campaign_service.gd / shop_service.gd to C#
**Completed:** 2026-01-24
**Category:** Architecture / C# Migration
**Effort:** Large

**Description:**
Core meta services were migrated from GDScript service scripts to typed C# service classes.

**PR Merge Date:** 2026-01-24 (`#202`)

**Files Changed:**
- `scripts/csharp/Meta/Services/Deck/DeckService.cs`
- `scripts/csharp/Meta/Services/Summoner/SummonerProgressionService.cs`
- `scripts/csharp/Meta/Services/Campaign/CampaignService.cs`
- `scripts/csharp/Meta/Services/Shop/ShopService.cs`

---

### Create EventCatalog with Typed Event Definitions
**Completed:** 2026-01-30
**Category:** Architecture / Type Safety
**Effort:** Large

**Description:**
Typed event definitions and catalog lookup/query surface were implemented in C#.

**PR Merge Date:** 2026-01-30 (`#233`)

**Files Changed:**
- `scripts/csharp/Infrastructure/Data/Events/EventDefinition.cs`
- `scripts/csharp/Infrastructure/Data/Events/EventCatalog.cs`
- `scripts/csharp/Infrastructure/Data/Events/BattleRewardConfig.cs`

---

### Create CampaignCatalog for Campaign Graph Definitions
**Completed:** 2026-01-30
**Category:** Architecture / Type Safety
**Effort:** Medium

**Description:**
Typed campaign graph definitions and campaign catalog APIs were implemented in C#.

**PR Merge Date:** 2026-01-30 (`#233`)

**Files Changed:**
- `scripts/csharp/Infrastructure/Data/Events/CampaignDefinition.cs`
- `scripts/csharp/Infrastructure/Data/Events/CampaignCatalog.cs`

---

### Update Node Panels to Receive Typed EventDefinitions
**Completed:** 2026-01-31
**Category:** Architecture / Type Safety
**Effort:** Medium

**Description:**
Node panels were migrated away from raw dictionary access patterns through typed panel wiring (`NodePanelFactory` + `TypedEventData`), removing most flag-style field access from panel implementations.

**PR Merge Date:** 2026-01-31 (`#236`)

**Files Changed:**
- `scripts/meta/components/node_panels/node_detail_panel_base.gd`
- `scripts/meta/components/node_panels/battle_node_panel.gd`
- `scripts/meta/components/node_panels/caravan_node_panel.gd`
- `scripts/meta/components/node_panels/choice_node_panel.gd`

---

### Clean Up Premium Store Placeholder Content
**Completed:** 2026-01-31
**Category:** Content / Clarity
**Effort:** Small

**Description:**
The Premium Store contained placeholder items with plausible-sounding names. Following the CLAUDE.md guideline to use obviously temporary names for placeholder content, we removed non-essential placeholders and renamed remaining ones to be clearly temporary.

**Changes Made:**
- **Removed:** Card back cosmetics (card_back_gold, card_back_obsidian) - not essential, removed entirely
- **Removed:** UI theme cosmetics (ui_theme_crimson, ui_theme_void) - not essential, removed entirely
- **Renamed:** All emotes to PLACEHOLDER EMOTE 1-8 (emote system not implemented yet)
- **Renamed:** All purchasable summoners to PLACEHOLDER SUMMONER 1-4 (not real summoners yet)
- **Kept:** Default cosmetics (card_back_default, ui_theme_default) as free fallbacks
- Updated tests to use default cosmetics instead of removed ones

**Files Changed:**
- `localization/data/en.json` - Removed card back/theme entries, renamed emotes to PLACEHOLDER format
- `scripts/infrastructure/data/cosmetics_catalog.gd` - Removed 4 placeholder cosmetics
- `scripts/services/shop_service.gd` - Removed 4 cosmetic offerings from premium store
- `tests/unit/test_cosmetics_catalog.gd` - Updated to test default cosmetics

---

### Add Mana Stones Currency for Premium Store
**Completed:** 2026-01-31
**Category:** Economy / Design
**Effort:** Medium

**Description:**
The Premium Store was incorrectly using Gold as its currency. Gold should only be used for in-campaign Caravan purchases. The Premium Store now uses "Mana Stones" (backed by the existing unused "gems" resource).

**Currency Design:**
- **Gold**: Campaign-scoped currency for Caravan purchases during gameplay
- **Mana Stones**: Meta-progression currency for Premium Store (summoners, cosmetics, emotes)

**Resolution:**
- Added localization key `ui.shop.mana_stones_label` for currency display
- Updated all premium store offerings from `currency_type: "gold"` to `currency_type: "gems"`
- Updated `PremiumStoreScreen` to display Mana Stones balance and check gems for affordability
- Updated `PremiumStoreOfferingItem` to show prices with gem icon format (`💎 150`)

**Files Changed:**
- `localization/data/en.json` - Added mana stones label
- `scripts/services/shop_service.gd` - Changed all premium offerings to use gems
- `scripts/meta/screens/premium_store_screen.gd` - Display and check mana stones
- `scripts/meta/components/premium_store_offering_item.gd` - Show mana stones price format

---

### Fix Card Level-Up UI Incorrectly Mentioning Gold Cost
**Completed:** 2026-01-31
**Category:** UI / Economy
**Effort:** Small

**Description:**
The card level-up UI displayed hardcoded placeholder text showing "Cost: 25 Gold" and "Your Gold: 100" even though card level-ups require only XP - there is no gold cost.

**Resolution:**
Removed the `CostContainer` node and its children (`CostLabel`, `GoldLabel`, `HSeparator3`) from `scenes/meta/modals/card_level_up_panel.tscn`. These were hardcoded placeholder labels that were never wired to any code.

**Files Changed:**
- `scenes/meta/modals/card_level_up_panel.tscn` - Removed misleading gold cost UI elements

---

### Configure Campaign Battles to Be Replayable (XP Only)
**Completed:** 2026-01-31
**Category:** Campaign / Economy
**Effort:** Small

**Description:**
Allow players to replay completed campaign battles for XP grinding, but without regaining gold or card rewards.

**Implementation:**
- Updated `node_detail_panel_base.gd` to allow combat events (battle/elite/boss) to be replayed
- `is_start_disabled()` now checks `event.is_combat()` - combat events are always replayable
- `get_start_button_text()` shows "Replay" button for completed combat events
- XP was already granted correctly at battle end (before reward screen)
- Gold/cards were already blocked for replays via `claim_battle_rewards()` guard

**Files Changed:**
- `scripts/meta/components/node_panels/node_detail_panel_base.gd` - Allow combat event replays

---

### Configure Card Pools to Exclude Already-Owned Cards
**Completed:** 2026-01-31
**Category:** Campaign / Economy
**Effort:** Small

**Description:**
Card reward pools now exclude cards the player already owns, preventing duplicate rewards.

**Implementation:**
- Updated `BattleRewardSpec.FromBattleId()` to accept `ownedCatalogIds` parameter
- Flexible reward options are filtered to exclude owned cards when `ExcludeOwned = true`
- Edge case handled: if all cards are owned, shows all options anyway (no empty rewards)
- Updated `RewardService.GetBattleRewardSpec()` to pass owned IDs to spec builder
- Set `ExcludeOwned = true` on all flexible reward configs in EventCatalog

**Files Changed:**
- `scripts/csharp/Meta/Services/Rewards/BattleRewardSpec.cs` - Added filtering logic
- `scripts/csharp/Meta/Services/Rewards/RewardService.cs` - Pass owned IDs to spec builder
- `scripts/csharp/Infrastructure/Data/Events/EventCatalog.cs` - Set ExcludeOwned on all flexible rewards

---

### Battle Reward System Refactor (Full Implementation)
**Completed:** 2026-01-30
**Category:** Core Game Systems / Rewards
**Effort:** Medium

**Description:**
Complete refactor of the battle reward system with type-safe C# pool system, combinable filters, bug fixes, and cleaner architecture.

**Bug Fixes:**
- Added completion guard in `claim_battle_rewards()` to prevent duplicate gold on replay
- Fixed summoner screen gold display to use `campaign_gold_changed` signal and `get_campaign_gold()`

**Type-Safe C# Pool System:**
- `RewardPoolId.cs` - Enum for predefined pools (type-safe, no strings)
- `RewardPoolCatalog.cs` - Pool definitions with:
  - Curated pools: TutorialRewards, StarterRewards, BossLoot (explicit card lists)
  - Filter pools: FireCommonUnits, WaterCommonUnits, etc. (element + rarity + type)
  - Composite pools: ElementalStarters (union of other pools)
- `RewardConstants.gd` - GDScript mirror enums for type safety across boundary
- Inline filter support via `reward_filters` dictionary

**Battle Config Options:**
```gdscript
# Option 1: Predefined pool (enum-based)
{
    "reward_pool": RewardConstants.PoolId.FIRE_COMMON_UNITS,
    "draw_count": 3,
    "exclude_owned": true,
}

# Option 2: Inline filters (combinable)
{
    "reward_filters": {
        "element": RewardConstants.Element.FIRE,
        "rarity": RewardConstants.Rarity.COMMON,
        "card_type": RewardConstants.CardType.SUMMON,
    },
    "draw_count": 3,
}
```

**Architecture Changes:**
- Added `get_reward_spec()` to RewardService for unified reward data
- Refactored RewardScreen to be a thin display layer using the spec pattern
- Added `reward_options` validation in CampaignService
- C# handles all pool resolution via `DrawFromPoolEnum()` and `DrawWithFilterDict()`

**Files Created:**
- `scripts/csharp/Meta/Services/Rewards/RewardPoolId.cs` - Pool enum
- `scripts/infrastructure/data/reward_constants.gd` - GDScript mirror enums

**Files Changed:**
- `scripts/csharp/Meta/Services/Rewards/RewardPoolCatalog.cs` - Pool definitions and resolution
- `scripts/csharp/Meta/Services/Rewards/RewardService.cs` - New draw methods
- `scripts/services/campaign_service.gd` - completion guard, validation
- `scripts/services/reward_service.gd` - uses C# pool methods
- `scripts/meta/screens/reward_screen.gd` - simplified to display-only
- `scripts/meta/screens/summoner_screen.gd` - fixed gold display signal

---

### Clean Up Redundant/Unused Profile Data Fields
**Completed:** 2026-01-26
**Category:** Database / Cleanup
**Effort:** Small

**Resolution:** Removed duplicate `profile_id` from `resources` object and removed unused `roll_json` field from card instance creation. Existing saves will still work as the code doesn't require these fields.

---

### Add Quit Game Functionality
**Completed:** 2026-01-26
**Category:** Core Game Systems / UI
**Effort:** Small

**Resolution:** Added Quit button to title screen (bottom-right corner). Uses localized text from `menu.quit` and calls `get_tree().quit()`.

---

### Standardize .tscn Placeholder Text Pattern
**Completed:** 2026-01-26
**Category:** UI / Code Style
**Effort:** Trivial

**Resolution:** Updated `title_screen.tscn` to use empty strings. Left `nav_drawer.tscn` unchanged as the `[ui.nav.menu]` pattern is informative for developers editing the scene.

---

### Fix async void Pattern in CompositeEffect
**Completed:** 2026-01-26
**Category:** Performance / Reliability
**Effort:** Small

**Resolution:** Already fixed. Both `CompositeEffect.cs` and `DeathExplosionAbility.cs` use typed `SceneTreeTimer.SignalName.Timeout` and have `IsInstanceValid` guards after await.

---

### Make Projectiles Disappear on Hit
**Completed:** 2026-01-26
**Category:** Units & Combat / Visual Polish
**Effort:** Small

**Resolution:** Already implemented. `HandlePierce()` in `Projectile3D.cs` calls `ExpireWithFade()` or `ExpireImmediate()` after hits, and `TriggerImpactEffects()` spawns VFX at collision point.

---

### Display Battle Rewards in Campaign Node UI
**Completed:** 2026-01-27
**Category:** UI/UX / Campaign
**Effort:** Small

**Description:**
When clicking on a battle node in the campaign map, the detail panel now shows comprehensive reward preview including gold, summoner XP, and card rewards.

**Changes Made:**
- `campaign_map.gd`: Updated `_update_detail_panel()` to show gold, XP, and card rewards
- `en.json`: Added localization keys for reward display (gold, summoner_xp, card_choice, card_choice_options)

**Reward Display:**
- Gold reward amount (if > 0)
- Summoner XP reward (if > 0)
- Card rewards:
  - FIXED: Shows specific card names
  - FLEXIBLE: Shows "Choose 1 of X cards" with options list if available
  - NONE: No card reward line

---

### Remove Gold Costs from Card/Summoner Leveling
**Completed:** 2026-01-27
**Category:** Core Game Systems / Progression
**Effort:** Small

**Description:**
Card and summoner leveling now requires only XP, not gold. Gold is campaign-scoped and should only be used for Caravan shop purchases, not permanent progression.

**Changes Made:**
- `CardProgressionHandler.cs`: Removed `LevelUpGoldCost` array and gold checks
- `CardService.cs`: Removed `SetEconomyCallbacks`, `GetLevelUpGoldCost`, `CanAffordLevelUp` methods
- `SummonerProgressionService.cs`: Removed gold cost checks from level-up methods
- `summoner_progression_service.gd`: Removed gold callbacks and related methods
- `card_level_up_panel.gd`: Removed gold cost display from UI
- `summoner_screen.gd`: Removed gold cost from level-up button
- `summoner_roster_item.gd`: Removed gold cost from level-up button
- `card_detail_modal.gd`: Removed gold cost from level-up button
- `en.json`: Updated localization strings for XP-only leveling

**Design Rationale:**
- Cards are permanent (persist across campaigns)
- Gold is campaign-scoped (lost when campaign ends)
- Players must be able to max out cards over time regardless of campaign outcomes
- Gold should create tension for in-campaign purchases, not gate permanent progression

**Related Docs:**
- `docs/design/card-progression-economy.md` - Updated to reflect XP-only leveling

---

### Migrate collection_service.gd & summoner_selection_service.gd to C#
**Completed:** 2026-01-23
**Category:** Architecture / C# Migration
**Effort:** Small

**Description:**
Removed GDScript wrapper services that were duplicating functionality already implemented in C#. Updated all callers to use the C# services directly.

**Key Changes:**
- Removed `scripts/services/collection_service.gd` (GDScript wrapper)
- Removed `scripts/services/summoner_selection_service.gd` (GDScript wrapper)
- Updated `project.godot` autoloads to point directly to C# services (`Collection`, `SummonerSelection`)
- Added `RemoveCardWithCascade()` and `DismantleCard()` methods to `CollectionService.cs`
- Added GameStateEvents connection to `SummonerSelectionService.cs` for battle state tracking
- Updated 14+ GDScript files to use PascalCase method names (e.g., `GetActiveSummonerId()` instead of `get_active_summoner_id()`)
- Updated `MockCollectionService` to use `GrantCard` (PascalCase)

**Architecture Notes:**
- GDScript now calls C# services directly with PascalCase methods
- Signals use PascalCase in C# (e.g., `SummonerChanged`, `CollectionChanged`)
- Eliminated dual autoload pattern (CS + GDScript wrapper)

**Files Deleted:**
- `scripts/services/collection_service.gd`
- `scripts/services/summoner_selection_service.gd`

**Files Modified:**
- `project.godot` - Updated autoload config
- `scripts/csharp/Meta/Services/Collection/CollectionService.cs` - Added cascade delete methods
- `scripts/csharp/Meta/Services/Summoner/SummonerSelectionService.cs` - Added GameStateEvents connection
- 14+ GDScript callers updated to use PascalCase method names

---

### Phase 4: Boons → Items Refactor
**Completed:** 2026-01-23
**Category:** Core Game Systems / Items
**Effort:** Large

**Description:**
Replace abstract boons with 4-slot equippable items (Weapon, Ring1, Ring2, Vestments).

**Solution Implemented:**
- Created `ItemCatalog.cs`, `ItemSlot.cs`, `ItemService.cs` in C#
- Created `item_service.gd` GDScript wrapper (autoloaded as `Items`)
- Created `ContentBinding` enum for AccountWide vs SummonerBound ownership
- Added equipment UI to summoner screen with `EquipmentSlotModal`
- Migration v5→v6: Converts legacy boons to items
- Added console commands for testing (`/items_grant`, `/items_list`, etc.)
- Documentation: `docs/features/equipment-system.md`

---

### Refactor Service Unit Tests for C# Hybrid Architecture
**Completed:** 2026-01-23
**Category:** Testing / Architecture
**Effort:** Medium

**Description:**
Unit tests for CampaignService failed because they were written before services migrated to hybrid GDScript/C# architecture. Tests created standalone instances that couldn't find C# autoloads.

**Solution Implemented:**
- Created `tests/mocks/mock_campaign_service_cs.gd` - Full GDScript mock of C# CampaignServiceCS
- Updated `campaign_service.gd` `init_for_testing()` to accept optional `cs_service_mock: Node` parameter
- Updated `test_campaign_service.gd` to create and inject mock
- Fixed `IsBattleTutorial()` to check `is_tutorial` field instead of `type == "tutorial"`
- Fixed double signal emission in mock's `SaveProgress()` method

**Test Results:**
- CampaignService: 28 failures → 0 failures (43/43 passing)
- ShopService: Was reported as failing but already passing (11/11)

**Files Created:**
- `tests/mocks/mock_campaign_service_cs.gd`

**Files Modified:**
- `scripts/services/campaign_service.gd`
- `tests/unit/test_campaign_service.gd`

---

### Add Boundary System to Prevent Units Walking Off Screen
**Completed:** 2026-01-13
**Category:** Units & Combat / Architecture
**Effort:** Medium

**Problem:**
Units could walk off the screen edge when there were no valid targets. No boundary enforcement existed for unit movement.

**Solution Implemented:**
- Created `BattlefieldBounds.cs` - Central C# boundary constants (X: -50 to +50, Z: -40 to +40)
- Added `EnforceBattlefieldBounds()` in `Unit3D.ApplyMovementResult()` - Clamps position after all physics
- Added boundary clamping in `UnitSteering.CorrectOverlaps()` - Prevents pushing units out of bounds
- Added mass-based push resistance (mass = radius³) - Large units resist being pushed by small units

**Files Changed:**
- Created: `scripts/csharp/Infrastructure/Constants/BattlefieldBounds.cs`
- Modified: `scripts/csharp/Units/Unit3D.cs`, `scripts/csharp/Movement/UnitSteering.cs`
- Tests: `tests/csharp/Constants/BattlefieldBoundsTest.cs`

**Related Bugs Fixed:** See bugs-resolved.md - "Units Can Move/Fly Out of Bounds", "Small Units Can Push Large Units Off Screen"

---

### Prevent Spawning Outside Battlefield Bounds
**Completed:** 2026-01-13
**Category:** Cards / Spawning
**Effort:** Medium

**Problem:**
When spawning units near crowded areas, the ring search algorithm could place units outside battlefield bounds or on the wrong team's side.

**Solution Implemented:**
- Added `team` parameter to `SpawnPositionCalculator` methods
- `IsSpawnPositionSafe()` now checks team spawn boundary (player: X <= 0, enemy: X > 0) and overall bounds
- Added `ClampToValidSpawnZone()` - Guarantees valid spawn position (outer bounds + team boundary)
- Fallback now clamps to team's valid zone instead of returning invalid position

**Files Changed:**
- Modified: `scripts/csharp/Summons/SpawnPositionCalculator.cs`
- Modified: `scripts/csharp/Cards/CardFactory.cs`, `scripts/csharp/Meta/Services/Interfaces/ICardFactory.cs`
- Modified: `scripts/battle/ui/battlefield_drop_zone.gd` (pass team parameter)
- Tests: Extended `tests/csharp/Summons/SpawnPositionCalculatorTest.cs`

**Related Bug Fixed:** See bugs-resolved.md - "Unit Spawn Boundary Can Be Bypassed When Blocked"

---

## Architecture

### Summon Abstraction + Stat Pipeline Unification
**Completed:** 2026-01-09
**Category:** Architecture / Cards / Stats
**Effort:** Large

**Problem:**
1. Cards lost references to spawned units after summoning
2. CardFactory was a 630+ line god object handling too many concerns
3. Stats were passed as string-keyed dictionaries with silent failures
4. Only 5 of 6 stats were actually applied to units (AggroRadius was ignored)

**Solution Implemented:**
- Created `StatKey` enum with type-safe stat identifiers and string conversion
- Created `UnitStats` immutable record for stat storage
- Created `UnitStatCalculator` with documented order of operations (base → upgrades → adds → mults → overrides)
- Created `UnitSummon` class to track spawned units with death events
- Created `SummonResult` wrapper for summon operation results
- Extracted `SpawnPositionCalculator` from CardFactory (safe position logic)
- Extracted `UnitSpawner` from CardFactory (unit instantiation)
- `CardFactory.execute_summon()` now returns `SummonResult` with unit references
- `card.gd` stores `_active_summon` and exposes `get_spawned_units()`
- CardFactory reduced from 631 to 431 lines

**Files Created:**
- `scripts/csharp/Battle/Simulation/Stats/StatKey.cs`
- `scripts/csharp/Battle/Simulation/Stats/UnitStats.cs`
- `scripts/csharp/Battle/Simulation/Stats/UnitStatCalculator.cs`
- `scripts/csharp/Summons/UnitSummon.cs`
- `scripts/csharp/Summons/SummonResult.cs`
- `scripts/csharp/Summons/SpawnPositionCalculator.cs`
- `scripts/csharp/Summons/UnitSpawner.cs`
- `tests/csharp/Stats/StatKeyTest.cs`
- `tests/csharp/Stats/UnitStatsTest.cs`
- `tests/csharp/Stats/UnitStatCalculatorTest.cs`

**Files Modified:**
- `scripts/csharp/Cards/CardFactory.cs` - Uses extracted components
- `scripts/csharp/Meta/Services/Interfaces/ICardFactory.cs` - Returns `SummonResult`
- `scripts/csharp/Units/Unit3D.cs` - Added `AggroRadius` property
- `scripts/cards/card.gd` - Stores `UnitSummon`, exposes `get_spawned_units()`

**Architecture Documents:**
- `docs/architecture/issues/summon-abstraction.md` - Marked RESOLVED
- `docs/architecture/issues/stat-pipeline.md` - Marked RESOLVED

---

### HP Bar Lifecycle Fix - GDScript to C# Migration
**Completed:** 2026-01-08
**Category:** Architecture / UI
**Effort:** Medium

**Problem:**
HP bars were not properly cleaned up when units died, particularly for multi-unit cards (Fire Ant Swarm). The cleanup relied on `UnregisterFromExternalSystems()` being called before the unit was freed, which failed in rapid-death or scene-unload scenarios.

**Solution Implemented:**
- Migrated HP bar system from GDScript to C#
- Created `HPBarService.cs` (pooling, lifecycle management)
- Created `FloatingHPBar.cs` (bar logic, rendering)
- Connected to unit's `TreeExiting` signal for guaranteed auto-cleanup
- Direct C# integration (no cross-language `Call()` needed)

**Key Fix:**
```csharp
unit.TreeExiting += OnUnitExiting;  // Fires BEFORE unit is freed
```

**Files Changed:**
- Created: `scripts/csharp/Meta/Services/HPBarService.cs`, `HPBarService.tscn`
- Created: `scripts/csharp/Battle/View/UI/FloatingHPBar.cs`
- Modified: `Unit3D.cs`, `summoner.gd`, `game_controller_3d.gd`, `project.godot`
- Deleted: `hp_bar_manager.gd`, `floating_hp_bar.gd`

**Architecture Document:** See `docs/architecture/issues/resolved/hp-bar-lifecycle.md`

---

### DRY Principle Audit - Formation Logic Unified
**Completed:** 2026-01-06
**Category:** Architecture / Code Quality
**Effort:** Medium

**Description:**
Performed comprehensive audit of formation logic duplication and unified into a single source of truth.

**Problem Identified:**
Formation logic was duplicated across 4+ files:
- C# CardFactory.cs
- GDScript Card.gd
- C# FormationHelper.cs (redundant)
- battlefield_drop_zone.gd

**Solution Implemented:**
- Created `SpawnOrchestrator.cs` as the single source of truth for formation positioning
- Deleted redundant `FormationHelper.cs`
- Updated `Card.gd` to delegate to SpawnOrchestrator
- Unified spawn preview and actual spawning to use same formation logic

**Related Bug Fixed:** "Spawn Preview and Actual Spawning Use Separate Formation Systems" (see bugs-resolved.md)

**Architecture Document:** See `docs/architecture/system-architecture.md` for current architecture

---

### CardProgressionService Removal
**Completed:** 2026-01-06
**Category:** Architecture / Services
**Effort:** Small

**Description:**
Removed the deprecated GDScript CardProgressionService, completing the migration to the C# PlayerCardService.

**Solution Implemented:**
- Updated all callers to use only PlayerCardService (no fallback)
- Removed CardProgression autoload from project.godot
- Deleted `scripts/services/card_progression_service.gd`

**Files Updated:**
- `scripts/cards/card.gd` - Removed fallback
- `scripts/application/battle_context.gd` - Removed fallback
- `scripts/meta/screens/collection_screen.gd` - Removed fallback
- `scripts/meta/modals/card_level_up_panel.gd` - Removed fallback (3 places)
- `scripts/meta/modals/card_detail_modal.gd` - Removed fallback (3 places)
- `project.godot` - Removed CardProgression autoload

---

### C# Modifier System Migration
**Completed:** 2026-01-06
**Category:** Architecture / Systems
**Effort:** Medium

**Description:**
Migrated the modifier system from GDScript to C# following the "C# = Systems & Mechanics" principle.

**Solution Implemented:**
- Created `ModifierService.cs` as central C# service (autoload)
- Created `StatModifier.cs` with typed modifier class
- Created `IModifierProvider.cs` interface
- Created `CardModifierProvider.cs` and `SummonerModifierProvider.cs` in C#
- Added factory methods for GDScript interop (`register_summoner_provider`, `register_card_provider`)
- Deleted deprecated GDScript files: `modifier_system.gd`, `card_modifier_provider.gd`, `summoner_modifier_provider.gd`

**Related Files:**
- `scripts/csharp/Systems/Modifiers/ModifierService.cs`
- `scripts/csharp/Systems/Modifiers/StatModifier.cs`
- `scripts/csharp/Systems/Modifiers/IModifierProvider.cs`

---

### Service Interfaces for Dependency Injection
**Completed:** 2026-01-06
**Category:** Architecture / Testing
**Effort:** Medium

**Description:**
Created service interfaces to enable future dependency injection and unit testing.

**Solution Implemented:**
- Created `ICardFactory.cs` interface
- Created `IModifierService.cs` interface
- Created `IPlayerCardService.cs` interface
- Created `IDamageSystem.cs` interface
- Updated all services to implement their respective interfaces
- All interfaces use snake_case for GDScript-compatible method names

**Related Files:**
- `scripts/csharp/Meta/Services/Interfaces/`

---

## Card & Spell System

### C# SummonCard Infrastructure
**Completed:** 2026-01-04
**Category:** Cards / Architecture
**Effort:** Medium

**Description:**
Ported summon card logic from GDScript to C# with pluggable formation strategies. All summons now execute via C# `CardFactory`. GDScript `Card.gd` reduced from ~463 to 265 lines.

**Architecture:**
```
SummonCard
├── SpawnConfig (scene path, count, summon time)
└── IFormationStrategy (pluggable: Grid, Ring, Line)
```

**Solution Implemented:**
- `SpawnConfig.cs` - Unit scene path, spawn count, summon time
- `IFormationStrategy.cs` - Interface for formation positioning
- `GridFormation.cs` - Default 2-row staggered formation (ported from GDScript)
- `RingFormation.cs` - Circular formation around spawn point
- `LineFormation.cs` - Horizontal line formation
- `SummonBuilder.cs` - Maps catalog IDs to formation strategies
- `SummonCard.cs` - Card type composing SpawnConfig + IFormationStrategy
- Renamed `SpellCardFactory.cs` → `CardFactory.cs` with unified spell/summon API
- `CardCatalog` sets `_csharp_summon_id` on summon cards
- `Card._summon_unit_3d()` delegates to C# `CardFactory.execute_summon()`
- Removed all GDScript summon logic (unit spawning, modifier integration, safe positioning)

**Related Files:**
- `scripts/csharp/Cards/CardFactory.cs` - Bridge autoload (spells + summons)
- `scripts/csharp/Cards/Formations/` - Formation strategies
- `scripts/csharp/Cards/SummonCard.cs` - Summon card type
- `scripts/cards/card.gd` - Delegation only (265 lines)

---

### C# Spell Effect System - Integration
**Completed:** 2026-01-04
**Category:** Cards / Architecture
**Effort:** Medium

**Description:**
Implemented a C# spell effect system with composition pattern. All spells now execute via C# `CardFactory`. GDScript `Card.gd` reduced from ~966 to 422 lines.

**Solution Implemented (Phase A - C# Foundation):**
- Core interfaces: `ISpellEffect`, `ITargetingStrategy`, `ISpellCondition`, `ITargetFilter`
- Base classes: `SpellEffect`, `SpellContext`, `Affinity` enum
- Concrete effects: `DamageEffect`, `CommandEffect` (Rally/Guard/Charge), `CompositeEffect`, `ConditionalEffect`
- Targeting: `CircleTargeting`
- Conditions: `HPThresholdCondition`
- Card classes: `Card` (abstract), `SpellCard`, `CardConfig`, `SpellCardConfig`
- Factory: `SpellBuilder` with Fireball, Rally, Guard, Charge

**Solution Implemented (Phase B - GDScript→C# Bridge):**
- Created `CardFactory.cs` autoload with `has_effect()` and `execute_spell()`
- `CardCatalog` sets `_csharp_spell_id` on spell cards
- `Card._cast_spell_3d()` delegates to C# `CardFactory`
- Removed all GDScript spell logic (VFX helpers, command spells, AOE damage, projectiles)
- Verified working in editor with all 4 spells (Fireball, Rally, Guard, Charge)

**Related Files:**
- `scripts/csharp/Cards/CardFactory.cs` - Bridge autoload
- `scripts/csharp/Cards/SpellBuilder.cs` - Effect factory
- `scripts/cards/card.gd` - Delegation only, all execution in C#
- `scripts/infrastructure/data/card_catalog.gd` - Sets `_csharp_spell_id` for spell cards

---

## Summoner System

### Implement Summoner Unlock System
**Completed:** 2025-12-23
**Category:** Summoners / Progression
**Effort:** Medium

**Description:**
Implemented the system for unlocking additional summoners beyond the starting summoner.

**Solution Implemented:**
- Premium Store UI with Summoners tab
- ShopOffering SUMMONER type with pricing (750 gold each)
- Purchase limits (1 per account per summoner)
- RewardService summoner unlock granting
- ProfileRepo unlock/instance tracking (`unlock_summoner()`, `is_summoner_unlocked()`, `get_unlocked_summoners()`)
- Shop "already owned" validation
- Purchasable summoners in catalog: Lightning Adept, Verdant Sage, Void Walker
- Dev console commands: `/unlock_summoner`, `/unlock_all_summoners`
- SummonerSwitchScreen shows unlocked summoners

**Related Files:**
- `scripts/services/shop_service.gd` - Summoner offerings with pricing
- `scripts/infrastructure/data/summoner_catalog.gd` - Purchasable summoner configs
- `scripts/infrastructure/data/json_profile_repository.gd` - Unlock tracking
- `scripts/services/reward_service.gd` - Unlock granting
- `scripts/meta/screens/premium_store_screen.gd` - Shop UI
- `scripts/debug/dev_console.gd` - Dev unlock commands

---

### Standardize "Hero" vs "Summoner" Language
**Completed:** 2025-11-28
**Category:** Summoners / Architecture
**Effort:** Medium

**Description:**
The codebase inconsistently used "Summoner" and "Hero" to refer to the same concept (the player character). Standardized to "Summoner" throughout codebase, docs, and UI.

**Solution Implemented:**
- Renamed all `Hero*` classes to `Summoner*` (HeroConfig→SummonerConfig, HeroInstance→SummonerInstance, etc.)
- Updated all services: HeroCatalog→SummonerCatalog, HeroSelection→SummonerSelection, HeroProgression→SummonerProgression
- Updated UI components: HeroManagementPanel→SummonerManagementPanel, HeroIconWidget→SummonerIconWidget, etc.
- Updated all scenes (.tscn) with new script paths and node names
- Updated localization keys in en.json (hero→summoner)
- Updated documentation in docs/features/summoners/
- Created SummonerIDs class for type-safe summoner references

**Files Changed:**
- Renamed: `scripts/core/hero_*.gd` → `scripts/core/summoner_*.gd`
- Renamed: `scripts/services/hero_*.gd` → `scripts/services/summoner_*.gd`
- Renamed: `scripts/ui/hero_*.gd` → `scripts/ui/summoner_*.gd`
- Renamed: `scenes/ui/hero_*.tscn` → `scenes/ui/summoner_*.tscn`
- Updated: `project.godot` autoloads
- Updated: `localization/data/en.json`
- Updated: `docs/features/summoners/*`

---

## Units & Combat

### Add Flying Unit Type
**Completed:** 2025-12-23
**Category:** Units & Combat
**Effort:** Medium

**Description:**
Created flying unit type that can move over obstacles and other units.

**Solution Implemented:**
- Added `MovementLayer` enum (GROUND, AIR) to Unit3D
- Added `TargetLayer` enum (GROUND_ONLY, AIR_ONLY, BOTH) for targeting rules
- Implemented `flight_altitude` export variable for visual height
- Shadow scaling based on altitude (smaller/fainter shadows at higher altitudes)
- Demon Imp card uses AIR movement layer as first flying unit
- Targeting system respects ground vs air layers

**Related Files:**
- `scripts/units/unit_3d.gd` - MovementLayer, TargetLayer enums, flight constants
- `scenes/battle/units/demon_imp_3d.tscn` - Flying unit with movement_layer=1 (AIR)
- `scripts/infrastructure/data/card_catalog.gd` - Demon Imp card definition

---

### Implement Flying Movement Logic
**Completed:** 2025-12-23
**Category:** Units & Combat
**Effort:** Medium
**Dependencies:** Add Flying Unit Type

**Description:**
Implemented movement system for flying units including pathfinding and collision rules.

**Solution Implemented:**
- Flying units set position.y to flight_altitude on spawn
- Shadow scaling: size and opacity reduce with altitude
- Height tolerance for attacks: flying units ignore height differences when attacking
- Collision layers: FLYING_UNITS (layer 2) separate from ground units
- Targeting respects can_target (GROUND_ONLY, AIR_ONLY, BOTH)
- Flying units skip ground-based separation forces

**Related Files:**
- `scripts/units/unit_3d.gd` - Flying movement logic in _ready() and targeting
- `scripts/infrastructure/physics_layers.gd` - FLYING_UNITS layer constant

---

### Spatial Partitioning for Unit Targeting
**Completed:** 2025-12-12
**Category:** Units & Combat / Performance
**Effort:** Medium-Large

**Description:**
Replaced O(n²) unit targeting/separation queries with O(k) spatial grid queries for better performance with high unit counts.

**Solution Implemented:**
- Created `SpatialGrid` autoload with 10×10 unit cells (80 cells for 100×80 battlefield)
- Units register on spawn, unregister on death
- Position updates use 2.0 unit threshold to avoid per-frame cell updates
- Replaced 4 O(n²) methods in Unit3D:
  - `_acquire_target()` - enemy targeting
  - `_calculate_separation_force()` - collision avoidance
  - `_calculate_flank_direction_scores()` - flanking direction choice
  - `_correct_overlaps()` - post-movement overlap correction
- Debug visualization toggleable with F11 (grid lines, cell populations, stats)

**Files Changed:**
- New: `scripts/spatial/spatial_grid.gd`
- Modified: `scripts/units/unit_3d.gd`
- Modified: `project.godot` (autoload registration)

**Performance Impact:**
- 30 units: ~900 → ~60 checks/frame (~15x improvement)
- 50 units: ~2500 → ~100 checks/frame (~25x improvement)
- 100 units: ~10000 → ~200 checks/frame (~50x improvement)

---

### Lane-Based Unit Movement
**Completed:** 2025-11-29
**Category:** Units & Combat
**Effort:** Medium

**Description:**
Implemented lane-based movement where units march forward along the X-axis instead of pathfinding directly to the enemy base. Units only engage enemies that enter their attack range.

**Solution Implemented:**
- Units march forward in their lane (along X-axis) rather than pathing to base
- Lane-based targeting: units only consider enemies within their current lane (Z-axis tolerance)
- Turn zone system: units resume normal targeting when near enemy base
- Constants: `PLAYER_TURN_ZONE_X`, `ENEMY_TURN_ZONE_X`, `LANE_WIDTH_MULTIPLIER`
- New method `_move_forward_in_lane()` for lane marching behavior
- New method `_is_in_turn_zone()` to detect when near enemy base

**Behavior:**
1. Spawn → march forward in lane (X-axis movement only)
2. Enemy enters attack range → engage and attack
3. Enter turn zone near enemy base → resume normal target-based pathing
4. After killing target → resume lane marching (unless in turn zone)

**Related Files:**
- `scripts/units/unit_3d.gd`

---

### Prevent Units from Stacking on Same Coordinates
**Completed:** 2025-11-25
**Category:** Units & Combat
**Effort:** Small

**Description:**
Added collision/placement validation to prevent multiple units from occupying the same grid position.

**Solution Implemented:**
- Check for existing unit before placement
- Block movement to occupied tiles
- Handle edge cases (unit death, teleportation)
- Works for both player and AI units

---

## Database & Data Layer

### Consolidate Dual Catalog System (CardCatalog vs ContentCatalog)
**Completed:** 2025-11-25
**Category:** Database / Architecture
**Effort:** Medium

**Description:**
The codebase had TWO card catalog systems with incompatible data formats - `CardCatalog` (hardcoded GDScript, 21+ cards) and `ContentCatalog` (JSON-based, 4 cards). This created confusion and potential bugs due to type mismatches (`card_type: int` vs `card_type: String`).

**Solution Implemented:**
- Kept `CardCatalog` as the single source of truth for card data (it has all the cards)
- Removed card and unit loading from `ContentCatalog` (unused functionality)
- Deleted unused data classes: `CardData`, `UnitData`
- Deleted unused JSON content: `data/cards/`, `data/units/`
- Kept `ContentCatalog` for projectile data only (actively used by projectile system)
- `ContentCatalog` is now a focused "ProjectileCatalog" in function

**Files Changed:**
- `scripts/infrastructure/data/content_catalog.gd` - Removed card/unit loading, simplified to projectiles only
- Deleted: `scripts/infrastructure/data/card_data.gd`, `scripts/infrastructure/data/unit_data.gd`
- Deleted: `data/cards/*.json`, `data/units/*.json`

---

### Fix Services Using Dynamic call() Instead of Typed Access
**Completed:** 2025-11-25
**Category:** Database / Code Quality
**Effort:** Small

**Description:**
Domain services used `has_method()` + `call()` pattern instead of direct typed method calls, defeating the purpose of having a typed interface.

**Solution Implemented:**
Updated EconomyService, CollectionService, DeckService, and CampaignService to use direct `ProfileRepo.method()` calls instead of dynamic `call()` pattern. ShopService was already correct and served as reference.

**Related Files:**
- `scripts/services/economy_service.gd`
- `scripts/services/collection_service.gd`
- `scripts/services/deck_service.gd`
- `scripts/services/campaign_service.gd`

---

### Add CampaignProgress Methods to ProfileRepo
**Completed:** 2025-11-25
**Category:** Database / Architecture
**Effort:** Small

**Description:**
CampaignService was bypassing the service layer and directly mutating `profile["campaign_progress"]`, violating the repository pattern.

**Solution Implemented:**
Added `get_campaign_progress()` and `update_campaign_progress()` methods to both IProfileRepo interface and JsonProfileRepository implementation. Updated CampaignService to use these new methods.

**Related Files:**
- `scripts/infrastructure/data/profile_repository.gd`
- `scripts/infrastructure/data/json_profile_repository.gd`
- `scripts/services/campaign_service.gd`

---

### Fix JsonProfileRepository Not Extending IProfileRepo Interface
**Completed:** 2025-11-25
**Category:** Database / Architecture
**Effort:** Small

**Description:**
`JsonProfileRepository` extended `Node` instead of `IProfileRepo`, making the interface unused and unenforceable.

**Solution Implemented:**
Changed `JsonProfileRepository` to `extends IProfileRepo`. The interface methods are now properly inherited and enforced.

**Related Files:**
- `scripts/infrastructure/data/json_profile_repository.gd`

---

### Add Cascade Delete When Removing Cards from Collection
**Completed:** 2025-11-25
**Category:** Database / Data Integrity
**Effort:** Small

**Description:**
When a card was removed from collection, it wasn't automatically removed from decks, leaving orphaned references.

**Solution Implemented:**
Added cascade delete logic to `Collection.remove_card()` in collection_service.gd. After successfully removing a card from the collection, iterates through all decks and calls `Decks.clean_deck()` to remove any orphaned card references.

**Related Files:**
- `scripts/services/collection_service.gd`

---

### Localize HeroCatalog Names
**Completed:** 2025-11-25
**Category:** Database / Localization
**Effort:** Small

**Description:**
HeroCatalog stored hardcoded English strings for hero names and descriptions instead of using the localization system.

**Solution Implemented:**
Replaced all hardcoded `hero_name` and `description` strings with `Loc.t()` calls:
- `hero_fire.hero_name = Loc.t("hero.hero_fire.name")`
- `hero_fire.description = Loc.t("hero.hero_fire.description")`
- Same pattern for all 5 heroes (fire, water, wind, earth, shadow_initiate)

**Related Files:**
- `scripts/infrastructure/data/hero_catalog.gd`

---

### RarityIDs Constants Class
**Completed:** 2025-11-25
**Category:** Database / Code Quality
**Effort:** Small

**Description:**
Rarity strings ("common", "rare", "epic", "legendary") were used as magic strings throughout the codebase.

**Solution Implemented:**
Created `scripts/infrastructure/data/rarity_ids.gd` with:
- StringName constants: `COMMON`, `RARE`, `EPIC`, `LEGENDARY`
- `ALL_RARITIES` array for iteration
- `get_tier()` method to get rarity index
- `is_valid()` method for validation

Updated all usages in:
- `scripts/services/collection_service.gd` - match statements and default values
- `scripts/services/campaign_service.gd` - reward card definitions
- `scripts/shared/color_palette.gd` - rarity color lookup
- `scripts/debug/dev_console.gd` - test data

**Related Files:**
- `scripts/infrastructure/data/rarity_ids.gd` (new)
- `scripts/services/collection_service.gd`
- `scripts/services/campaign_service.gd`
- `scripts/shared/color_palette.gd`
- `scripts/debug/dev_console.gd`

---

## Core Game Systems

### Extract Magic Numbers in Hero System to Constants
**Completed:** 2025-11-25
**Category:** Core Game Systems / Code Quality
**Effort:** Small

**Description:**
Default stat values in the hero system were hardcoded without named constants, making them harder to maintain and tune.

**Solution Implemented:**
Added class-level constants to HeroConfig:
- `DEFAULT_BASE_HEALTH: float = 1000.0`
- `DEFAULT_MAX_MANA: float = 10.0`
- `DEFAULT_MANA_REGEN: float = 1.0`

Updated `@export` defaults and `from_dict()` fallbacks to use these constants.

**Related Files:**
- `scripts/core/hero_config.gd`

---

### CardTypeIDs / Card.CardType Enum Usage
**Completed:** 2025-11-25
**Category:** Core Game Systems / Code Quality
**Effort:** Small

**Description:**
CardCatalog used magic numbers `0` and `1` for card types instead of the `Card.CardType` enum, risking silent breakage if enum order changed.

**Solution Implemented:**
Replaced all `"card_type": 0` with `Card.CardType.SUMMON` and `"card_type": 1` with `Card.CardType.SPELL` throughout card_catalog.gd. Also updated comparison logic in `create_card_resource()` and `print_catalog_summary()`.

**Related Files:**
- `scripts/infrastructure/data/card_catalog.gd`

---

### Audit Codebase for Magic Strings - Replace with Constants/Enums
**Completed:** 2025-11-25
**Category:** Core Game Systems / Code Quality
**Effort:** Medium

**Description:**
Comprehensive audit to replace hardcoded string literals with type-safe constants throughout the codebase. This improves maintainability, catches typos at compile time, and provides better IDE autocomplete support.

**Solution Implemented:**
Created 11 constants classes with StringName constants:

1. **CardIDs** - 18 card catalog ID constants
2. **ProjectileIDs** - FIREBALL, ARROW, EMBER constants
3. **VFXIDs** - 7 VFX effect name constants
4. **RarityIDs** - COMMON, RARE, EPIC, LEGENDARY
5. **BiomeIDs** - SUMMER_PLAINS
6. **BattleIDs** - 5 battle/event ID constants
7. **GroupIDs** - 15+ Godot group name constants
8. **EventTypeIDs** - BATTLE, AFFINITY, FIRST_SUMMON, CARAVAN, ONBOARDING
9. **RewardTypeIDs** - FIXED, RANDOM, CHOICE, NONE
10. **UnitTypeIDs** - MELEE, RANGED, STRUCTURE
11. **ElementNameIDs** - 15 element name string constants

Updated 30+ files to use these constants instead of magic strings.

**Related Files:**
- `scripts/infrastructure/data/card_ids.gd`
- `scripts/infrastructure/data/projectile_ids.gd`
- `scripts/infrastructure/data/vfx_ids.gd`
- `scripts/infrastructure/data/rarity_ids.gd`
- `scripts/infrastructure/data/biome_ids.gd`
- `scripts/infrastructure/data/battle_ids.gd`
- `scripts/infrastructure/data/group_ids.gd`
- `scripts/infrastructure/data/event_type_ids.gd`
- `scripts/infrastructure/data/reward_type_ids.gd`
- `scripts/infrastructure/data/unit_type_ids.gd`
- `scripts/infrastructure/data/element_name_ids.gd`

---

## Visual Polish

### Add Building Hit/Damage Animation
**Completed:** 2025-11-12
**Category:** Visual Polish
**Effort:** Small

**Description:**
Added visual feedback when buildings (summoner bases) take damage with dynamic flash speed based on attack intensity.

---

### Fix Projectile Aiming on Moving Targets
**Completed:** 2025-11-12
**Category:** Units & Combat
**Effort:** Medium

**Description:**
Implemented predictive targeting for projectiles so they lead moving targets instead of aiming at current position.

---

## UI Revamp

### Revamp Main Menu UI / Navigation
**Completed:** 2025-12-04
**Category:** UI/UX
**Effort:** Medium
**PR:** #94

**Description:**
Replaced the main menu + game mode menu with a streamlined navigation system centered on the Campaign Map.

**Solution Implemented:**
- Replaced MainMenu scene with TitleScreen (tap-to-start entry point)
- Removed GameModeMenu entirely
- Campaign Map now serves as the central hub
- Added hamburger menu button (top-right) opening slide-in Nav Drawer
- Nav Drawer provides access to: Collection, Events, Shop, Settings
- Added campaign selector banner (top-left) to switch between campaigns
- Added Settings and Special Events placeholder screens
- Added NavigationContext service for proper back button behavior
- Moved campaign battle definitions from hardcoded GDScript to JSON files

**Files Changed:**
- New: `title_screen.tscn/gd`, `nav_drawer.tscn/gd`, `hamburger_button.tscn/gd`
- New: `settings_screen.tscn/gd`, `special_events_screen.tscn/gd`
- New: `campaign_selector_modal.tscn/gd`, `campaign_ids.gd`
- New: `data/campaigns/academy_trials.json`
- Deleted: `main_menu.tscn/gd`, `game_mode_menu.tscn/gd`
- Updated: `campaign_map.gd`, `campaign_service.gd`, `scene_manager.gd`
- Updated: `collection_screen.gd`, `shop_screen.gd` (NavigationContext back navigation)

---

### Revamp Pause Menu
**Completed:** 2025-11-12
**Category:** UI/UX
**Effort:** Small

**Description:**
Improved pause menu design with ESC key support and pause button in battle HUD.

---

## Campaign System

### Add Leave Buttons to Caravan Event
**Completed:** 2025-11-25
**Category:** Campaign / Events
**Effort:** Small

**Description:**
Added proper exit options for players who don't want to make a purchase in caravan events.

**Solution Implemented:**
- Added `LeaveIncompleteButton` ("Leave") - exits without completing, player can return
- Added `LeaveCompleteButton` ("Leave without purchasing") - completes event, allows progression
- Each button has its own confirmation popup with clear messaging
- Localization keys added for all button text and confirmation dialogs

---

## Core Game Systems

### Fix Hardcoded UI Strings - Add Localization
**Completed:** 2025-11-25
**Category:** Core Game Systems / Localization
**Effort:** Medium

**Description:**
Many UI files had hardcoded user-facing strings instead of using the `Loc.t()` localization pattern. All user-facing text must be localized for internationalization support.

**Solution Implemented:**
Updated all UI files to use `Loc.t()` with localization keys from `localization/data/en.json`:
- `game_ui.gd` - Win/lose messages
- `collection_screen.gd` - Card stats labels, deck info labels, empty state messages
- `mana_bar.gd` - Mana display format
- `speed_button.gd` - Tooltips
- `deck_builder.gd` - Validation messages, button labels, card popup labels
- `shop_screen.gd` - Gold label, offering details, price display
- `offering_card.gd` - Type labels, price format
- `hero_card.gd` - HP/Mana/Regen stat labels
- `hero_reveal.gd` - Random hero title text

---

## Core Game Systems

### Implement Deck Recycling After Exhaustion
**Completed:** 2025-11-25
**Category:** Core Game Systems
**Effort:** Small

**Description:**
When a player's deck is exhausted (all cards drawn), shuffle the discard pile back into the deck to continue play.

**Solution Implemented:**
- Added `discard_pile: Array[Card]` variable to track played cards
- Added `deck_recycled(card_count: int)` signal for UI/audio feedback
- Modified `play_card()` / `play_card_3d()` to add played cards to discard pile
- Recycle triggers only when BOTH hand AND deck are empty (not just deck)
- When recycling: shuffle discard into deck, then draw fresh full hand
- Added `_recycle_discard_pile()` helper that shuffles discard pile into deck
- Implemented in both `Summoner` (2D) and `Summoner3D` classes
- Logs deck recycle events for debugging

**Behavior:**
1. Play card → goes to discard pile → try to draw from deck
2. If deck has cards: draw 1 card
3. If deck empty but hand has cards: continue playing without drawing
4. When hand AND deck both empty: recycle discard → draw full new hand

**Edge Cases Handled:**
- Empty deck but cards in hand: keep playing until hand exhausted
- Empty deck AND empty discard pile: draw_card() safely returns

**Related Files:**
- `scripts/core/summoner.gd`
- `scripts/core/summoner_3d.gd`

---

## Campaign System

### Implement Win Condition System for Campaign Events
**Completed:** 2025-11-25
**Category:** Campaign / Battle System
**Effort:** Medium

**Description:**
Campaign battles now support configurable win/loss conditions beyond simple base destruction. Different battle types can have different objectives with time limits.

**Solution Implemented:**
- Created `WinConditionIDs` constants class with type-safe win condition references
- Four win condition types:
  - `DESTROY_BASE` - Default, destroy enemy base to win (no time limit)
  - `SURVIVE_TIME` - Survive for specified duration (win on timeout)
  - `TIMED_DESTROY` - Destroy base within time limit (lose on timeout)
  - `KILL_COUNT` - Kill specified number of enemy units
- Updated `GameController3D` to read win conditions from battle config
- Added kill tracking system for KILL_COUNT objective
- Added `objective_progress` signal for UI updates
- Documented usage in `campaign_service.gd`

**Usage in Battle Definitions:**
```gdscript
"win_condition": WinConditionIDs.TIMED_DESTROY,
"time_limit": 60.0,  # seconds
"kill_target": 10,   # for KILL_COUNT
```

**Related Files:**
- `scripts/infrastructure/data/win_condition_ids.gd` (new)
- `scripts/core/game_controller_3d.gd`
- `scripts/services/campaign_service.gd`

---

### Research and Implement Framerate Independence
**Completed:** 2025-11-25
**Category:** Core Game Systems / Performance
**Effort:** Medium

**Description:**
Audited codebase and implemented proper framerate-independent game mechanics to ensure consistent gameplay across different hardware and frame rates.

**Findings:**
- Codebase was already ~98% framerate-independent (excellent delta usage throughout)
- All movement code properly uses delta or Godot 4's move_and_slide() pattern
- All timers/cooldowns use time-based accumulation, not frame counts
- Mana regeneration correctly uses `mana_regen_rate * delta`

**Solution Implemented:**
- Enabled physics interpolation in project.godot for smooth motion at varying FPS
- Created FPS Test Tool (`scripts/debug/fps_test_tool.gd`) with F5-F8 hotkeys
- Created best practices documentation (`docs/technical/rendering/framerate-independence.md`)

**Testing:**
- F5: 30 FPS (mobile simulation)
- F6: 60 FPS (standard)
- F7: 120 FPS (high refresh)
- F8: Uncapped

**Related Files:**
- `project.godot` - Added physics interpolation setting
- `scripts/debug/fps_test_tool.gd` (new)
- `docs/technical/rendering/framerate-independence.md` (new)

---

## Visual Polish

### Improve Mana Bar UI Design (Tiered Mana Bar)
**Completed:** 2025-11-26
**Category:** UI/UX
**Effort:** Medium

**Description:**
Implemented a tiered mana bar system that wraps at 10 mana per tier with different colors, rather than growing the bar larger for higher mana values.

**Solution Implemented:**
- Created layered ColorRect system where previous tiers show underneath current tier
- Blue intensity color progression: Light Blue → Royal Blue → Indigo → Purple → Magenta
- Smooth fill animations using Tweens (0.2s duration)
- Tier multiplier label (x2, x3, etc.) for completed tiers
- Localized all UI text (mana label, tier multiplier)
- Extracted magic numbers to named constants (HIGHLIGHT_HEIGHT, FILL_ANIM_DURATION)

**Technical Details:**
- Each tier represents 10 mana (MANA_PER_TIER constant)
- Up to 5 tiers supported (50 max mana)
- Dynamically creates ColorRect fills for each tier
- Lower tiers render first (at bottom), higher tiers on top
- Example: 15/25 mana = full Light Blue (tier 1) + half Royal Blue (tier 2)

**Related Files:**
- `scripts/ui/mana_bar.gd` - Complete rewrite with tiered system
- `scenes/ui/mana_bar.tscn` - Updated scene structure
- `localization/data/en.json` - Added tier_multiplier localization

---

## Core Game Systems

### Implement Card and Hero Level System
**Completed:** 2025-11-27
**Category:** Core Game Systems / Progression
**Effort:** Large

**Description:**
Implemented leveling system for cards and heroes that allows them to grow stronger through gameplay.

**Solution Implemented:**

**Card Progression (PR #85):**
- CardProgressionService autoload with XP and level management
- XP thresholds with rarity scaling
- CardUpgradeCatalog with upgrade choices per level
- UI display for card levels and progress
- Level-up with upgrade selection modal
- *Note: Gold costs removed per design update - leveling requires only XP*

**Hero Progression (Phase 2 Foundation):**
- HeroProgressionService autoload (`scripts/services/hero_progression_service.gd`)
- XP thresholds: 0, 100, 250, 500, 850, 1300, 1900, 2700, 3800, 5200
- Max level: 10
- *Note: Gold costs removed per design update - leveling requires only XP*
- Signals: `hero_xp_changed`, `hero_leveled_up`, `hero_ready_to_level_up`
- Battle completion grants hero XP via `hero_xp_reward` in battle config
- Helper methods: `grant_hero_xp()`, `can_level_up()`, `level_up_hero()`, `get_hero_progression_info()`

**Related Files:**
- `scripts/services/card_progression_service.gd` - Card XP/levels
- `scripts/services/hero_progression_service.gd` - Hero XP/levels (new)
- `scripts/application/battle_context.gd` - Battle completion XP grants
- `scripts/services/campaign_service.gd` - Battle XP reward definitions

**Future Phases (tracked in design spec):**
- Phase 3: Level Traits with Trait Lines
- Phase 4: Ultimate Traits at level 10
- Phase 5: Story Traits from campaign events
- Phase 6: Boon System

---

## Campaign System

### Design Campaign Map Interface
**Completed:** 2025-11-19
**Category:** Campaign / UI
**Effort:** Large
**PR:** #54

**Description:**
Designed and implemented the visual and UX approach for the new map-based campaign interface to replace the old list view.

**Solution Implemented:**
- Visual map-based campaign screen with event nodes
- Linear path layout with sine wave positioning for visual interest
- Node/point design showing completed (✓), unlocked (number), and locked (🔒) states
- Progression visualization with path lines connecting nodes
- Lock/unlock indicators with distinct colors per state

---

### Implement Map Node System for Battles
**Completed:** 2025-11-19
**Category:** Campaign
**Effort:** Medium
**PR:** #54

**Description:**
Implemented the technical system for map nodes representing battles and their connections.

**Solution Implemented:**
- `event_nodes` dictionary for fast lookup by event_id
- `event_render_order` array for explicit draw order
- Lock/unlock state read from Campaign service
- Full save/load integration through profile system
- Supports multiple event types (battle, affinity, first_summon, caravan, onboarding)

---

### Add Map Navigation/Selection
**Completed:** 2025-11-19
**Category:** Campaign / UI
**Effort:** Medium
**PR:** #54

**Description:**
Implemented player interaction with the campaign map - selecting and starting battles.

**Solution Implemented:**
- Node click handler with visual feedback
- Detail panel popup showing event name, difficulty, description, rewards
- 2D panning with drag threshold (5px before panning starts)
- Auto-centering on latest unlocked mission
- Deck selector integration in detail panel

---

### Integrate Battle Progression on Map
**Completed:** 2025-11-19
**Category:** Campaign
**Effort:** Small
**PR:** #54

**Description:**
Connected battle completion to map progression - unlocking next nodes, visual updates.

**Solution Implemented:**
- Completed nodes show checkmark (✓) with green styling
- Automatic refresh of map on event completion
- Progress label showing "X/Y Battles Completed"
- Save progression state through Campaign service
- Signal connections for `battle_completed` and `campaign_progress_changed`

---

## Hero System

### Hero System Phase 2: Foundation Implementation
**Completed:** 2025-11-28
**Category:** Heroes / Architecture
**Effort:** Large

**Description:**
Implemented the foundational hero system with traits, progression services, per-hero campaign progress, and hero management UI.

**Solution Implemented:**

**Services:**
- `HeroSelectionService` - Manages active hero selection, hero switching
- `HeroProgressionService` - XP and level management (1-10)
- `TraitCatalog` - Central trait/boon registry with hero and unit modifiers

**Data Structures:**
- `HeroConfig` - Static configuration with base stats and innate trait IDs
- `HeroInstance` - Runtime state (level, xp, acquired boons, computed stats)
- Trait data with hero stat modifiers (flat/percent) and unit modifiers

**Battle Integration:**
- `BattleContext.set_player_hero_stats()` caches computed stats for DamageSystem
- `HeroModifierProvider` passes unit modifiers to ModifierSystem
- Element-specific damage bonuses (fire_damage_bonus, damage_reduction, etc.)

**Per-Hero Campaign Progress:**
- ProfileRepo stores campaign_progress per hero ID
- Migration preserves legacy progress in `_legacy_progress` backup
- New profiles start with empty per-hero structure

**UI Components:**
- `HeroManagementPanel` - Full roster view, stats, traits, level-up
- `HeroIconWidget` - Persistent hero button on screens (click to open panel)
- `HeroRosterItem` - Individual hero row with select/level-up buttons
- Element colors and symbols centralized in `ElementTypes`

**Localization:**
- All UI strings use `Loc.t()` pattern
- Trait names/descriptions use localization keys

**Deleted Old System:**
- Removed: ActiveModifier, ModifierConfig, ModifierDatabase, ModifierEffect, ModifierRegistry
- These were replaced by TraitCatalog + HeroInstance trait system

**Related Files:**
- `scripts/services/hero_selection_service.gd` (new)
- `scripts/services/hero_progression_service.gd` (new)
- `scripts/infrastructure/data/trait_catalog.gd` (new)
- `scripts/core/hero_instance.gd` (updated for traits)
- `scripts/core/hero_config.gd` (updated: innate_trait_ids)
- `scripts/infrastructure/data/json_profile_repository.gd` (per-hero campaign progress)
- `scripts/ui/hero_management_panel.gd` (new)
- `scripts/ui/hero_icon_widget.gd` (new)
- `scripts/ui/hero_roster_item.gd` (new)
- `scripts/systems/hero_modifier_provider.gd` (updated)
- `scripts/application/battle_context.gd` (hero stats caching)
- `scripts/combat/damage_system.gd` (hero damage bonuses)
- `docs/features/heroes/architecture.md` (updated)

---

### Add Hero Select UI
**Completed:** 2025-11-28
**Category:** UI/UX
**Effort:** Medium

**Description:**
Created hero selection/management UI for viewing and switching between heroes.

**Solution Implemented:**
- `HeroManagementPanel` - Modal panel showing full hero roster
- `HeroIconWidget` - Clickable hero portrait in corner of screens
- `HeroRosterItem` - Individual hero card with stats, XP, level-up button
- Added to CampaignMap, CollectionScreen, GameModeMenu

---

### Design Hero Data Structure
**Completed:** 2025-11-28
**Category:** Heroes / Architecture
**Effort:** Medium

**Description:**
Defined the data structures for hero configuration and runtime state.

**Solution Implemented:**
- `HeroConfig` resource with base stats, innate_trait_ids, element
- `HeroInstance` runtime class with level, xp, acquired_boon_ids
- `TraitCatalog` for trait definitions with modifiers
- Computed stats via `HeroInstance.get_computed_stats()`

---

### Implement Hero Stats System
**Completed:** 2025-11-28
**Category:** Heroes
**Effort:** Medium

**Description:**
Implemented hero stat computation with trait modifiers applied in battle.

**Solution Implemented:**
- Base stats from HeroConfig (base_health, max_mana, mana_regen)
- Trait modifiers apply flat/percent bonuses to stats
- Element-specific bonuses (fire_damage_bonus, damage_reduction)
- BattleContext caches hero stats for DamageSystem
- DamageSystem applies damage_bonus and damage_reduction

---

### Create Hero Selection Screen UI
**Completed:** 2025-11-28
**Category:** Heroes / UI
**Effort:** Medium

**Description:**
Implemented UI for selecting and switching between heroes.

**Solution Implemented:**
- HeroManagementPanel shows all unlocked heroes
- Hero switching via HeroSelection.switch_hero()
- Active hero highlighted in roster
- Stats, traits, XP progress displayed per hero

---

### Design Hero In-Battle UI Elements (Foundation)
**Completed:** 2025-11-28
**Category:** Heroes / UI
**Effort:** Medium

**Description:**
Added hero UI elements to game screens for battle context.

**Solution Implemented:**
- HeroIconWidget shows active hero element color and level
- Widget added to CampaignMap, CollectionScreen, GameModeMenu
- Click opens HeroManagementPanel for hero management

---

### Integrate Heroes into Battle System (Foundation)
**Completed:** 2025-11-28
**Category:** Heroes
**Effort:** Large

**Description:**
Connected hero system to battle mechanics for stat application.

**Solution Implemented:**
- Summoner loads HeroInstance via DeckLoader
- Hero stats applied via BattleContext.set_player_hero_stats()
- DamageSystem reads hero stats for damage calculations
- HeroModifierProvider passes unit modifiers to ModifierSystem
- Per-hero campaign progress saved and loaded correctly

---

## Developer Tools

### Implement Automated Testing Framework
**Completed:** 2025-11-28
**Category:** Developer Tools / Testing
**Effort:** Medium

**Description:**
Added GUT (Godot Unit Test) framework for automated testing of game services and logic.

**Solution Implemented:**
- Installed GUT v9.3.0 addon
- Created test directory structure (`tests/unit/`, `tests/integration/`, `tests/mocks/`)
- Created MockProfileRepo implementing IProfileRepo interface
- Created MockEconomyService and MockCollectionService for service mocking
- Refactored EconomyService and CampaignService for dependency injection
- Services now have `init_for_testing()` method for mock injection
- Created unit tests for EconomyService (15 tests)
- Created unit tests for CampaignService (20+ tests)
- Created unit tests for BattleContext (20+ tests)
- Added tests/README.md with documentation

**Related Files:**
- `addons/gut/` - GUT framework
- `tests/unit/test_economy_service.gd`
- `tests/unit/test_campaign_service.gd`
- `tests/unit/test_battle_context.gd`
- `tests/mocks/mock_profile_repo.gd`
- `tests/mocks/mock_economy_service.gd`
- `tests/mocks/mock_collection_service.gd`
- `scripts/services/economy_service.gd` - Added DI support
- `scripts/services/campaign_service.gd` - Added DI support

---

## Summoner System

### Add Summoner Select UI
**Completed:** 2025-12-04
**Category:** UI/UX
**Effort:** Medium

**Description:**
Created a summoner selection screen allowing players to choose their summoner before battle.

**Implementation:**
- SummonerManagementPanel provides full summoner roster view
- SummonerIconWidget provides persistent summoner button on screens
- SummonerRosterItem shows individual summoner details with stats

---

### Design Summoner Data Structure
**Completed:** 2025-12-04
**Category:** Summoners / Architecture
**Effort:** Medium

**Description:**
Defined the data structure and resource format for summoner characters.

**Implementation:**
- SummonerConfig: Static summoner configuration (base stats, innate traits)
- SummonerInstance: Runtime state (level, xp, acquired boons, computed stats)
- TraitCatalog: Central trait/boon registry with modifiers
- See `docs/features/summoners/architecture.md` for details

---

### Implement Summoner Stats System
**Completed:** 2025-12-04
**Category:** Summoners
**Effort:** Medium

**Description:**
Implemented the technical system for summoner-specific stats and attributes.

**Implementation:**
- SummonerInstance.get_computed_stats() applies trait modifiers to base stats
- BattleContext.set_player_summoner_stats() caches stats for DamageSystem
- Trait modifiers support flat and percent bonuses
- Element-specific damage bonuses (fire_damage_bonus, etc.)

---

### Create Summoner Selection Screen UI
**Completed:** 2025-12-04
**Category:** Summoners / UI
**Effort:** Medium

**Description:**
Designed and implemented the UI screen where players choose their summoner before battle.

**Implementation:**
- SummonerManagementPanel: Full roster view with stats, traits, level-up
- SummonerIconWidget: Persistent summoner button (click to open panel)
- SummonerRosterItem: Individual summoner row with select/level-up buttons
- Summoner switching via SummonerSelection service

---

### Design Summoner In-Battle UI Elements (Foundation)
**Completed:** 2025-12-04
**Category:** Summoners / UI
**Effort:** Medium

**Description:**
Designed UI elements for displaying summoner information and abilities during battle.

**Implementation:**
- SummonerIconWidget added to CampaignMap, CollectionScreen, GameModeMenu
- Shows active summoner element color and level
- Click opens SummonerManagementPanel

**Notes:**
- Ability buttons/cooldowns deferred to Phase 3/4 when abilities are added

---

### Integrate Summoners into Battle System (Foundation)
**Completed:** 2025-12-04
**Category:** Summoners
**Effort:** Large

**Description:**
Final integration of summoner system into the core battle gameplay loop.

**Implementation:**
- Summoner loads SummonerInstance via DeckLoader
- Summoner stats applied via BattleContext.set_player_summoner_stats()
- DamageSystem reads summoner stats for damage bonuses
- SummonerModifierProvider passes unit modifiers to ModifierSystem
- Per-summoner campaign progress in ProfileRepo

**Notes:**
- Summoner abilities deferred to Phase 3/4
- AI summoners for enemies planned for future

---

## UI/UX

### Card Replacement Should Happen In-Place
**Completed:** 2025-12-17
**Category:** UI/UX / Card System
**Effort:** Small

**Description:**
When a card was played and a new card was drawn to replace it, the hand reordered with the new card appearing at the end. This was disorienting as players couldn't remember card positions.

**Solution Implemented:**
- Modified `draw_card()` in summoner.gd to accept optional `target_index` parameter
- When target_index is provided, inserts new card at that position instead of appending
- Modified `_complete_card_play()` to pass the played card's index to draw_card()
- New card now appears in the same slot as the played card
- Other cards maintain their positions

**Related Files:**
- `scripts/core/summoner.gd` - Modified draw_card() and _complete_card_play()

---

## Audio

### Add Background Music System
**Completed:** 2025-12-18
**Category:** Audio
**Effort:** Medium

**Description:**
Implemented core music system with playback, volume control, and transitions.

**Solution Implemented:**
- Created AudioManager autoload (`scripts/infrastructure/audio_manager.gd`)
- Audio bus setup (Master, Music, SFX) with dynamic creation
- Crossfade transitions between music tracks (DEFAULT_CROSSFADE: 1.0s)
- Volume control with linear-to-dB conversion
- Settings persistence via ProfileRepo (music_volume, sfx_volume)
- Process mode set to PROCESS_MODE_ALWAYS for pause menu support

**Related Files:**
- `scripts/infrastructure/audio_manager.gd` (new)
- `project.godot` - AudioManager autoload registration

---

### Add Battle Music Tracks
**Completed:** 2025-12-18
**Category:** Audio
**Effort:** Small
**Dependencies:** Add Background Music System

**Description:**
Added battle music that plays during combat gameplay.

**Solution Implemented:**
- Added `battle.mp3` from freesound.org (humanoide9000, CC BY 4.0)
- Music starts on `start_game()` in GameController3D
- Music stops on battle end or quit with fade out
- Proper attribution in `resources/audio/ATTRIBUTION.md`

**Related Files:**
- `resources/audio/bgm/battle.mp3` (new)
- `resources/audio/ATTRIBUTION.md` (new)
- `scripts/core/game_controller_3d.gd` - play_music/stop_music calls
- `scripts/ui/pause_menu.gd` - stop music on quit

---

### Add UI Click/Interaction Sounds
**Completed:** 2025-12-18
**Category:** Audio
**Effort:** Small

**Description:**
Added sound feedback for UI interactions (button clicks, menu navigation).

**Solution Implemented:**
- Added `ui_click.wav` from freesound.org (Jaszunio15, CC0)
- `AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)` pattern
- Applied to all major UI buttons across screens:
  - Campaign map, Nav drawer, Deck builder
  - Settings screen, Shop screen, Pause menu
  - Card detail modal, Reward screen
  - Title screen, Summoner selection, Special events

**Related Files:**
- `resources/audio/sfx/ui_click.wav` (new)
- Multiple UI scripts updated with play_ui_sound() calls

---

### Add Card Play Sounds
**Completed:** 2025-12-18
**Category:** Audio
**Effort:** Small

**Description:**
Added sound effects when cards are played and drawn.

**Solution Implemented:**
- Added `card_draw.mp3` from freesound.org (Geoff-Bremner-Audio, CC0)
- Added `card_play.wav` from freesound.org (theplax, CC BY 4.0)
- Sounds triggered via `_on_card_played()` and `_on_card_drawn()` in hand_ui.gd
- Proper attribution in `resources/audio/ATTRIBUTION.md`

**Related Files:**
- `resources/audio/sfx/card_draw.mp3` (new)
- `resources/audio/sfx/card_play.wav` (new)
- `scripts/ui/hand_ui.gd` - sound triggers on card events

---

## UI Revamp

### Revamp Card Hand Display
**Completed:** 2025-12-23
**Category:** UI/UX
**Effort:** Medium

**Description:**
Improved the visual presentation of cards in the player's hand.

**Solution Implemented:**
- Card spacing and layout (CARD_WIDTH = 120, CARD_SPACING = 10)
- Smooth hover animations (rises 40px, scales to 1.2x, 0.25s transition)
- 3D rotation shader with velocity tracking
- Playability indicators (glow for affordable cards, visual feedback for insufficient mana)
- Pulsing glow effect for playable cards
- Draw animation when cards enter hand (0.4s duration)
- Handles varying hand sizes dynamically

**Related Files:**
- `scripts/battle/ui/hand_ui.gd` - Complete hand display implementation

---

*Last Updated: 2026-01-09 - Added Summon Abstraction + Stat Pipeline Unification*
