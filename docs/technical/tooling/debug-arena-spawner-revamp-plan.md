# Debug Arena Spawner Revamp Plan

**Status:** PASS 3 COMPLETE (Implementation + Tests), PR REVIEW READY  
**Initiative:** `debug-arena-spawner-revamp`  
**Domain:** `tooling`  
**Last Updated:** `2026-03-15`  
**Owner:** `Codex + Gameplay`

## Summary

This initiative revamps the debug arena spawning workflow so developers can stage battles faster and with less menu friction. The old flow relied on a single list and repeated team toggling. The new flow adds side-by-side player/enemy lists, advanced controls in a detachable drawer, team-specific clear actions, player/enemy AI toggles, richer spawn controls (single/burst/paint + formation), and spawn history undo.

## Goals

1. Remove repeated spawn-side toggling friction by exposing player/enemy lists simultaneously.
2. Keep primary spawn actions visible while moving advanced controls into a left drawer.
3. Support full AI-vs-AI arena simulation by enabling AI for both player and enemy teams.
4. Improve spawn iteration speed with search/filter/sort and multi-spawn controls.
5. Keep debug actions safe with team-scoped clears and undo support.

## Non-Goals

1. Changes to ranked or production battle UI.
2. New combat mechanics or simulation tuning outside debug tools.
3. Asset-polish work for non-debug screens.

## Architecture Decisions

1. Keep UI orchestration in GDScript (`UnitSpawnerPanel`) and runtime state mutation in C# (`DebugArenaScene`, `InputCollector`).
2. Use fixed-team buttons (player list always team 0, enemy list always team 1).
3. Track spawn history centrally in `DebugArenaScene` and emit log events to the panel.
4. Keep advanced controls detachable from the main panel to reduce default panel footprint.
5. Remove obsolete single-toggle spawn-side path now that dual lists are authoritative.

## Public API / Interface / Type Changes

1. Added new panel signals:
   - `player_ai_toggled(enabled: bool)`
   - `clear_team_requested(team: int)`
   - `undo_requested()`
2. Added panel methods:
   - `get_player_ai_enabled()`
   - `get_spawn_settings()`
   - `append_spawn_log(message: String)`
3. Added C# scene handlers:
   - `OnPlayerAiToggled(bool enabled)`
   - `ClearTeamUnits(int team)`
   - `UndoLastSpawnBatch()`
4. Extended spawn batch tracking to carry explicit spawned unit IDs for reliable undo.

## Legacy Removal Scope

1. Removed scene/script reliance on `spawn_as_enemy` toggle path for debug spawns.
2. Removed panel `get_spawn_team()` usage from spawn buttons (fixed-team lists are canonical).

## Pass Acceptance Criteria

### PASS 1: USE CASES + VALIDATION

1. Define arena-spawner UX goals and non-goals.
2. Define validation matrix for dual-list spawn flow, AI toggles, clear/undo reliability, and drawer behavior.
3. Map each validation case to test type and target file.

### PASS 2: STUBS + WIRING

1. Wire new panel signals and methods without final behavior regressions.
2. Introduce spawn settings payload plumbing between GDScript drag data and C# input handling.
3. Add/expand baseline tests for new debug-scene APIs.

### PASS 3: IMPLEMENTATION + TESTS

1. Implement side-by-side lists, advanced drawer/collapse behavior, and spawn controls.
2. Implement player AI toggle + team clear + undo behavior.
3. Ensure undo tracks real spawned IDs (not only latest team IDs).
4. Run full C# and GUT suites and mark validation matrix statuses.

### PR REVIEW: READY

1. Artifacts complete and pass-state order documented.
2. Validation matrix reflects implemented coverage and any deferred items.

## PASS 3 Outcome Summary

1. Implemented dual player/enemy spawn lists with default A-Z sorting and shared filtering controls.
2. Implemented detachable left advanced drawer and full panel collapse/expand behavior.
3. Implemented player + enemy AI toggles, team clears, spawn log, and undo.
4. Implemented reliable undo by tracking explicit spawned unit IDs per batch.
5. Added/expanded debug scene tests for player AI toggling, team clear behavior, and undo correctness.

## Open Risks

1. `UnitSpawnerPanel` is now a large script and may benefit from future split into focused subcomponents.
2. Some UI behaviors (drawer/collapse layout under unusual viewport transitions) rely on manual validation.

## Likely Files

1. `scripts/battle/ui/debug/unit_spawner_panel.gd`
2. `scripts/battle/ui/debug/spawnable_unit_button.gd`
3. `scripts/csharp/Battle/Input/InputCollector.cs`
4. `scripts/csharp/Battle/View/Debug/DebugArenaScene.cs`
5. `tests/csharp/View/DebugArenaSceneTest.cs`
6. `scenes/battle/battlefield/dev/debug_arena.tscn`
7. `localization/data/en.json`

## Pass Gate Status

Current state:
1. `PASS 1: USE CASES + VALIDATION` complete
2. `PASS 2: STUBS + WIRING` complete
3. `PASS 3: IMPLEMENTATION + TESTS` complete
4. `PR REVIEW: READY` in progress

## Approval Evidence

1. PASS 1 -> PASS 2 approval captured in delivery thread on `2026-03-13`: `Sure. and lets sort a-z by default.`
2. PASS 2 -> PASS 3 approval captured in delivery thread on `2026-03-13`: `Yes lets decide on the enhancements and then implement`.
3. Scope extension approval captured in delivery thread on `2026-03-13`: `All these good. Also would be good if we could turn on ai spawning for the player as well.`
