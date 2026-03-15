# Debug Arena Spawner Revamp Validation Cases

**Status:** PASS 3 IMPLEMENTATION COMPLETE (Awaiting PR Review)  
**Initiative:** `debug-arena-spawner-revamp`  
**Domain:** `tooling`  
**Last Updated:** `2026-03-15`  
**Companion Plan:** `debug-arena-spawner-revamp-plan.md`

## How To Use

1. Define baseline scenarios with stable case IDs.
2. Add skeleton wiring/tests in Pass 2.
3. Mark each case `Implemented` or `Deferred` in Pass 3.

Allowed status values:
1. `Design-Covered`
2. `Implemented`
3. `Deferred`

## Case Matrix

| Case ID | Scenario | Expected Result | Test Type | Test File | Status |
|---|---|---|---|---|---|
| DAS-001 | Panel shows dual unit lists (player left, enemy right) | Team-spawn workflow no longer requires repeated side toggling | manual | `scripts/battle/ui/debug/unit_spawner_panel.gd` | Implemented |
| DAS-002 | Default ordering | Both lists default to A-Z sort | manual | `scripts/battle/ui/debug/unit_spawner_panel.gd` | Implemented |
| DAS-003 | Player AI toggle enabled/disabled | Team 0 summoner AI flips between `Heuristic/Balanced` and `None` | unit | `tests/csharp/View/DebugArenaSceneTest.cs` | Implemented |
| DAS-004 | Team-specific clear action | `ClearTeamUnits(1)` removes only enemy units/projectiles | unit | `tests/csharp/View/DebugArenaSceneTest.cs` | Implemented |
| DAS-005 | Undo after additional AI spawns | Undo removes tracked batch units, not merely latest team IDs | unit | `tests/csharp/View/DebugArenaSceneTest.cs` | Implemented |
| DAS-006 | Spawn settings payload wiring | Drag payload carries spawn mode/burst/formation settings to input layer | unit | `scripts/csharp/Battle/Input/InputCollector.cs` | Implemented |
| DAS-007 | Advanced drawer positioning | Drawer/handle stay aligned with panel during rect changes | manual | `scripts/battle/ui/debug/unit_spawner_panel.gd` | Implemented |
| DAS-008 | Full panel collapse/expand | Main panel content hides/restores and controls remain operable | manual | `scripts/battle/ui/debug/unit_spawner_panel.gd` | Implemented |
| DAS-009 | Legacy spawn toggle path removal | No runtime dependency on `spawn_as_enemy` or `get_spawn_team` | static | `scripts/battle/ui/debug/unit_spawner_panel.gd` | Implemented |
| DAS-010 | Localization integrity | Added debug spawner keys remain valid under localization key tests | unit | `tests/unit/test_localization_keys.gd` | Implemented |

## Deferred Cases

None.

## Exit Criteria Mapping

### PASS 2

1. Every required case has an implementation or explicit placeholder mapping.
2. New signals/methods are wired end-to-end without compile/runtime breakage.

### PASS 3

1. Every required case is marked `Implemented` or `Deferred`.
2. Full C# + GUT suites pass after final implementation.
