# Debug Arena Spawner Revamp Stub Checklist

**Status:** PASS 3 COMPLETE (Checklist Closed)  
**Initiative:** `debug-arena-spawner-revamp`  
**Domain:** `tooling`  
**Last Updated:** `2026-03-15`

## Types Created

1. `SpawnBatch` in `DebugArenaScene` extended to track explicit spawned unit IDs.

## Interfaces / Signals Added

1. `UnitSpawnerPanel.player_ai_toggled(enabled: bool)`
2. `UnitSpawnerPanel.clear_team_requested(team: int)`
3. `UnitSpawnerPanel.undo_requested()`
4. `DebugArenaScene.SpawnLogged(message: string)`

## Wiring Points Updated

1. `DebugArenaScene.ConnectSpawnerPanel()` now binds player AI toggle, team clear, and undo signals.
2. `InputCollector.DropDebugSpawn(...)` now captures pre/post spawn unit IDs for reliable undo tracking.
3. `UnitSpawnerPanel` now provides `get_spawn_settings()` and `append_spawn_log(...)` for C# interop.
4. Scene layout updated for larger dual-list panel footprint.

## Legacy Paths Removed or Disabled

1. Removed debug arena scene property assignment `spawn_as_enemy = true`.
2. Removed `UnitSpawnerPanel.get_spawn_team()` method.
3. Removed `SpawnableUnitButton` dependency on `panel.get_spawn_team()` fallback path.

## Compile-Safe Behavior Checks

1. Spawner panel script loads with no missing-method signal hookups.
2. C# signatures remain Godot-interoperable (`RegisterDebugSpawnBatch` optional spawned-unit ID parameter).
3. Undo fallback path still works when explicit ID tracking is unavailable.

## Test Skeleton Coverage Map

| Case ID | Test File | Test Name | Status |
|---|---|---|---|
| DAS-003 | `tests/csharp/View/DebugArenaSceneTest.cs` | `OnPlayerAiToggled_ConfiguresAndDisablesPlayerAi` | Implemented |
| DAS-004 | `tests/csharp/View/DebugArenaSceneTest.cs` | `ClearTeamUnits_RemovesOnlyRequestedTeamUnitsAndProjectiles` | Implemented |
| DAS-005 | `tests/csharp/View/DebugArenaSceneTest.cs` | `UndoLastSpawnBatch_RemovesTrackedUnitsInsteadOfLatestTeamUnits` | Implemented |

## PASS 2 Scope Checklist

1. [x] New panel signals defined and emitted.
2. [x] C# debug scene signal handlers connected.
3. [x] Spawn settings drag payload contract wired.
4. [x] Test coverage extended for new C# debug behaviors.

## PASS 3 Scope Checklist

1. [x] Side-by-side player/enemy list UX implemented.
2. [x] Advanced drawer + collapse UX implemented.
3. [x] Team clear + player AI + undo features implemented.
4. [x] Undo reliability upgraded to tracked spawned IDs.
5. [x] Full C# and GUT suites passing after integration.

## Next Gate

1. Proceed to `PR REVIEW: READY` and resolve review findings.
