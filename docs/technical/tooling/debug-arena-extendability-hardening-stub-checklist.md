# Debug Arena Extendability Hardening Stub Checklist

**Status:** PASS 3 IMPLEMENTATION NOTES (COMPLETE)  
**Initiative:** `debug-arena-extendability-hardening`  
**Domain:** `tooling`  
**Last Updated:** `2026-03-19`

## Types Created

1. `DebugArenaDeckSourceMode` - typed deck source-selection enum.
2. `DebugArenaDeckResolveRequest` - normalized request contract for deck resolution.
3. `DebugArenaDeckResolution` - typed output contract for resolved player/enemy decks.
4. `DebugArenaDeckSourceModeResolver` - config-to-enum parser (`debug_arena_deck_source`).
5. `DebugArenaDeckProvider` - compile-safe provider stub with explicit precedence branches.
6. `DebugArenaSpawnerPanelBridge` - typed adapter over GDScript panel signal/method contract.
7. `DebugArenaSpawnerPanelBridgeFactory` - panel discovery + bridge creation entrypoint.

## Interfaces Created

1. `IDebugArenaDeckProvider` - deck resolution abstraction.
2. `IDebugArenaSpawnerPanelBridge` - typed panel contract abstraction.

## Wiring Points Updated

1. `DebugArenaScene.BuildPracticeConfig()` now resolves decks through `IDebugArenaDeckProvider`.
2. `DebugArenaScene` now resolves panel integration through `IDebugArenaSpawnerPanelBridge`.
3. `DebugArenaScene` initialization now applies initial skip-prep/enemy-AI/player-AI state through bridge getters.
4. `DebugArenaScene.AppendSpawnLog(...)` now routes through typed bridge append API.
5. `DebugArenaSceneTest` now validates provider-injection wiring (`CreateDeckProvider()` override seam).

## Legacy Paths Removed or Disabled

1. Inline deck-file parsing logic in `DebugArenaScene` - removed and replaced by `DebugArenaDeckProvider`.
2. Raw signal/method wiring scattered through `DebugArenaScene.ConnectSpawnerPanel()` - replaced by typed bridge calls.
3. Legacy panel discovery heuristics - retained as compatibility fallback inside factory (not primary call site anymore).

## Compile-Safe Stub Behavior Checks

1. Default mode now prioritizes context decks (`ContextThenFileThenFallback`) with explicit file and fallback paths.
2. Context/override precedence branches are explicit and deterministic in provider switch paths.
3. Typed bridge safely no-ops on optional signals/methods and warns on missing required ones.
4. Existing debug arena behavior (`clear`, `undo`, AI toggles, skip prep) remains wired and callable.

## Test Skeleton Coverage Map

| Case ID | Skeleton Test File | Test Name | Notes |
|---|---|---|---|
| C01 | `tests/csharp/View/DebugArenaSceneTest.cs` | `BuildPracticeConfig_DefaultMode_UsesContextSourceModeInDeckRequest` | Pass 3 context-first default coverage |
| C02 | `tests/csharp/View/DebugArenaDeckProviderTest.cs` | `Resolve_FileBackedMode_ReturnsNonEmptyDecks` | File mode stub coverage |
| C03 | `tests/csharp/View/DebugArenaDeckProviderTest.cs` | `Resolve_FileBackedMode_InvalidDeckFile_FallsBackToCatalogSummons` | Missing/invalid file fallback coverage |
| C04 | `tests/csharp/View/DebugArenaDeckProviderTest.cs` | `Resolve_OverrideMode_PrefersOverrideDeckOverContextDeck` | Explicit precedence branch coverage |
| C05 | `tests/csharp/View/DebugArenaSpawnerPanelBridgeTest.cs` | `ConnectsSignalsThroughTypedBridge` | Typed bridge signal wiring coverage |
| C06 | `tests/csharp/View/DebugArenaSpawnerPanelBridgeFactoryTest.cs` | `TryCreate_UsesUiLayerProbeFallback_WhenTypedPanelNotPresent` | Legacy probe fallback coverage |
| C07 | `tests/csharp/View/DebugArenaSceneTest.cs` | `ClearAllUnits_ClearsSimulationStateAndQueuesVisualsForDeletion` | Existing behavior retained |
| C08 | `tests/csharp/Services/TestArenaWindEarthMissionTest.cs` | `ArenaWindEarthNewCards_UsesOnlyNewCardsPlusFireWisp` | Existing mission coverage retained |
| C09 | `tests/csharp/View/DebugArenaSceneTest.cs` | `ConnectSpawnerPanel_SyncsResolvedDeckEntriesToPanelBridge` | Scene/panel shared deck source coverage |
| C10 | `tests/csharp/Input/InputCollectorDebugSpawnTest.cs` | `SpawnModeRuntime_SingleBurstPaint_ProduceExpectedPositionSets` | Runtime spawn-mode coverage |
| C11 | `tests/csharp/Input/InputCollectorDebugSpawnTest.cs` | `FormationRuntime_StackLineArcRandom_ProduceExpectedSpatialLayouts` | Runtime formation coverage |
| C12 | `tests/csharp/View/DebugArenaSceneTest.cs` | `ClearTeamUnits_RemovesOnlyRequestedTeamUnitsAndProjectiles` | Existing panel actions retained |
| C13 | `tests/unit/test_debug_arena_unit_spawner_panel_stub.gd` | `test_c13_spawner_settings_round_trip_persists_across_instances` | PASS 3 persistence behavior coverage |
| C14 | `tests/unit/test_debug_menu_stub.gd` | `test_c14_open_map_and_battle_launch_use_expected_routing_hooks` | Debug battle utility behavior coverage |
| C15 | `tests/unit/test_debug_menu_stub.gd` | `test_c15_visualization_toggles_and_persistence_round_trip` | Visualization toggle + persistence behavior |
| C16 | `tests/unit/test_debug_menu_stub.gd` | `test_c16_console_submit_and_autocomplete_flow_behaves_as_expected` | Console submit/autocomplete behavior |
| C17 | `tests/unit/test_debug_menu_stub.gd` | `test_c17_camera_overlay_auto_log_and_zoom_solver_toggles_work` | Camera tooling behavior |
| C18 | `tests/csharp/Services/TestArenaCatalogConsistencyTest.cs` | `TestArenaCampaign_AllTestArenaPreset_MatchesCampaignEventIds` | PASS 3 preset/campaign sync coverage |
| C19 | `tests/csharp/Services/EventLocalizationKeyIntegrityTest.cs` | `AllEventNameAndDescriptionKeys_ResolveLocalizationEntries` | PASS 2 skeleton |
| C20 | `tests/csharp/View/DebugArenaSceneTest.cs` | `ConnectSpawnerPanel_ContextMode_SyncsContextDeckEntriesToPanelBridge` | Context-mode scene/panel parity |
| C21 | `tests/unit/test_debug_menu_stub.gd` | `test_c21_debug_menu_preset_catalog_contract_exists` | PASS 2 skeleton |
| D01 | `tests/csharp/View/DebugArenaSceneTest.cs` | `UndoLastSpawnBatch_RemovesTrackedUnitsInsteadOfLatestTeamUnits` | Deterministic undo seam retained |

## PASS 2 Scope Checklist

1. [x] Deck provider abstraction compiles and is wired into debug arena config path.
2. [x] Panel bridge abstraction compiles and is wired into scene signal/method integration path.
3. [x] Legacy inline scene deck-loader path removed.
4. [x] Legacy direct string-based panel wiring removed from scene body.
5. [x] PASS 1 case matrix has skeleton test targets mapped.

## Remaining Deferred After PASS 3

1. none

## Gate Output Requirement

1. End Pass 2 report with explicit Pass 3 approval request.
2. If approval is not provided, state: `blocked waiting approval`.
