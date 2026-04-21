# Debug Arena Extendability Hardening Validation Cases

**Status:** PASS 3 IMPLEMENTATION REVIEWED  
**Initiative:** `debug-arena-extendability-hardening`  
**Domain:** `tooling`  
**Last Updated:** `2026-03-19`  
**Companion Plan:** `debug-arena-extendability-hardening-plan.md`

## How To Use

1. Define baseline scenarios for debug arena extendability and source-of-truth behavior in Pass 1.
2. Add compile-safe skeleton coverage in Pass 2.
3. Mark as `Implemented`/`Deferred` in Pass 3 after behavior and tests are complete.

Allowed status values:
1. `Design-Covered`
2. `Implemented`
3. `Deferred`

## Case Matrix

| Case ID | Scenario | Expected Result | Test Type | Test File | Status |
|---|---|---|---|---|---|
| C01 | Event-authored debug mission deck with debug scene | Debug Arena loads mission-provided `dev_player_deck` instead of forcing `debug_deck.json` | integration | `tests/csharp/View/DebugArenaSceneTest.cs` | Implemented |
| C02 | Explicit file-backed deck mode | Debug Arena loads `res://data/debug/debug_deck.json` when file mode is selected | unit | `tests/csharp/View/DebugArenaDeckProviderTest.cs` | Implemented |
| C03 | Missing/invalid deck file | Debug Arena falls back to curated fallback deck, not "all summons" | unit | `tests/csharp/View/DebugArenaDeckProviderTest.cs` | Implemented |
| C04 | Deck source precedence | Source priority follows defined order (override > event config > file > curated fallback) | unit | `tests/csharp/View/DebugArenaDeckProviderTest.cs` | Implemented |
| C05 | Panel bridge wiring on ready | Required debug panel actions/signals are bound through typed bridge path | integration | `tests/csharp/View/DebugArenaSpawnerPanelBridgeTest.cs` | Implemented |
| C06 | Panel lookup fallback compatibility | If typed bridge unavailable, legacy fallback path remains safe and logs warning | unit | `tests/csharp/View/DebugArenaSpawnerPanelBridgeFactoryTest.cs` | Implemented |
| C07 | Existing controls remain functional | `ClearAllUnits`, `ClearTeamUnits`, `UndoLastSpawnBatch`, AI toggles still work after refactor | integration | `tests/csharp/View/DebugArenaSceneTest.cs` | Implemented |
| C08 | New Wind/Earth debug mission practical usability | `arena_wind_earth_new_cards` launches with intended deck contents in debug scene | integration | `tests/csharp/Services/TestArenaWindEarthMissionTest.cs` | Implemented |
| C09 | Shared deck source between scene and panel | Spawner panel and scene report the same resolved deck entries | integration | `tests/csharp/View/DebugArenaSceneTest.cs` | Implemented |
| C10 | Spawner spawn-mode parity | `single`, `burst`, and `paint` modes produce expected spawn counts/paths | integration | `tests/csharp/Input/InputCollectorDebugSpawnTest.cs` | Implemented |
| C11 | Spawner formation parity | `stack`, `line`, `arc`, and `random` formation behaviors remain functional | integration | `tests/csharp/Input/InputCollectorDebugSpawnTest.cs` | Implemented |
| C12 | Spawner panel actions parity | clear player/enemy/all and undo remain wired through bridge contract | integration | `tests/csharp/View/DebugArenaSceneTest.cs` | Implemented |
| C13 | Spawner settings persistence parity | spawn mode/count/formation/AI/skip-prep settings round-trip via config | unit | `tests/unit/test_debug_arena_unit_spawner_panel_stub.gd` | Implemented |
| C14 | DebugMenu battle utility parity | skip-prep, win/lose, campaign map open, arena quick-launch still callable | integration | `tests/unit/test_debug_menu_stub.gd` | Implemented |
| C15 | DebugMenu visualization toggle parity | all visualization toggles still map to debug services and persist | integration | `tests/unit/test_debug_menu_stub.gd` | Implemented |
| C16 | DebugMenu console parity | command execution + autocomplete + submit feedback still function | integration | `tests/unit/test_debug_menu_stub.gd` | Implemented |
| C17 | DebugMenu camera tooling parity | overlay/auto-log/zoom-log controls and diagnostics remain functional | integration | `tests/unit/test_debug_menu_stub.gd` | Implemented |
| C18 | Test arena quick-launch completeness | Debug menu test-arena launch list stays in sync with campaign test-arena battles (no missing new battle buttons) | unit | `tests/csharp/Services/TestArenaCatalogConsistencyTest.cs` | Implemented |
| C19 | Event localization integrity | Every `EventCatalog` `name_key` / `description_key` resolves in localization data | unit | `tests/csharp/Services/EventLocalizationKeyIntegrityTest.cs` | Implemented |
| C20 | Scene/panel deck-source parity (context mode) | When context deck mode is selected, spawner panel catalog aligns with scene-resolved deck entries | integration | `tests/csharp/View/DebugArenaSceneTest.cs` | Implemented |
| C21 | Debug menu list presets | Debug menu quick-launch list can be sourced from preset catalog entries (including \"new cards only\" list) | integration | `tests/unit/test_debug_menu_stub.gd` | Implemented |

## Determinism Cases (If Applicable)

| Case ID | Seed | Inputs | Checkpoints | Hash/State Assertions | Status |
|---|---|---|---|---|---|
| D01 | `fixed-seed-4201` | Spawn/undo/clear sequence across deck-source modes | init/mid/end | unit counts and spawn history are stable per mode | Implemented |

## Deferred Cases

| Case ID | Reason Deferred | Planned Follow-up |
|---|---|---|
| none | n/a | n/a |

## Exit Criteria Mapping

### Pass 2

1. Every listed case has a stub/skeleton test target.
2. Deck provider and panel bridge compile and are wired.

### Pass 3

1. Cases C01-C21 and D01 are marked `Implemented` or `Deferred`.
2. Any deferred case includes rationale and follow-up target.
