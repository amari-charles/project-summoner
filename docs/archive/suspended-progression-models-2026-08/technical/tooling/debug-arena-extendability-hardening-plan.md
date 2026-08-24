# Debug Arena Extendability Hardening Plan

**Status:** PASS 3 IMPLEMENTED  
**Initiative:** `debug-arena-extendability-hardening`  
**Domain:** `tooling`  
**Last Updated:** `2026-03-19`  
**Owner:** `Codex + Gameplay Engineering`

## Summary

Debug Arena behavior currently works for the happy path but is not reliably extensible. The key functional break is that scene-level practice configuration always sources decks from `debug_deck.json`, which bypasses event-authored deck variants and causes newly created targeted debug missions to appear non-functional. In parallel, the panel/controller contract is stringly-typed across C# and GDScript, and deck loading is duplicated across scene and panel layers. This initiative hardens Debug Arena by introducing explicit deck-source selection and a typed bridge contract so new debug missions, deck variants, and panel actions can be added safely.

## Goals

1. Make Debug Arena consume the intended deck source deterministically (event-provided deck, file deck, or fallback deck).
2. Remove brittle string-probing between `DebugArenaScene` and `unit_spawner_panel.gd` by defining a typed bridge contract.
3. Centralize debug deck loading logic to one authoritative provider path.
4. Preserve existing debug controls (`clear`, `undo`, AI toggles, skip prep) while improving test coverage for extensibility paths.
5. Preserve full debug feature parity across both debug surfaces:
   - `UnitSpawnerPanel` (battle-local spawn tooling)
   - `DebugMenu` autoload (global debug utilities + quick battle launch)

## Non-Goals

1. Redesigning the full debug UI layout or workflow.
2. Replacing `unit_spawner_panel.gd` with a full C# implementation.
3. Broad campaign/battle context redesign outside debug arena deck-selection scope.

## Architecture Decisions

1. Add a dedicated Debug Arena deck provider abstraction with explicit mode selection.
2. Prioritize battle-context/event-authored deck data when present, with explicit fallback order.
3. Keep panel rendering in GDScript, but move control contract surface to a typed C# bridge adapter.
4. Keep existing debug arena scene path and event wiring stable; only source selection and bridge contracts change.

## Public API / Interface / Type Changes

1. Add a typed deck-source enum and provider interface for Debug Arena deck resolution.
2. Add a typed panel bridge interface for required controls/signals currently accessed via raw strings.
3. Add optional debug configuration key(s) for explicit deck-source override in practice config.
4. Add/expand tests validating deck source precedence and bridge wiring behavior.
5. Add an explicit capability contract (typed config/action surfaces) so debug menu + spawner function parity is validated during refactors.

## Legacy Removal Scope

1. Remove duplicated deck loading logic split between `DebugArenaScene` and `unit_spawner_panel.gd`.
2. Remove heuristic panel discovery by method-name probing as the primary path.
3. Remove implicit “all summons” fallback as default failure behavior; replace with explicit curated fallback.

## Pass Acceptance Criteria

### PASS 1: USE CASES + VALIDATION

1. Validation matrix captures all known extendability issues and the mission-deck bypass failure case.
2. Planned tests map each case to a concrete file target.

### PASS 2: STUBS + WIRING

1. Deck provider and panel bridge types compile and are wired without changing intended behavior yet.
2. Deck source precedence is explicit in code path selection (stubs acceptable where behavior deferred).
3. Skeleton tests exist for all PASS 1 cases.

### PASS 3: IMPLEMENTATION + TESTS

1. Debug Arena uses correct deck source for event-authored debug missions.
2. Panel/controller contract no longer depends on fragile raw-name probing as primary integration.
3. Centralized deck loading path is implemented and covered by tests.
4. Existing debug controls remain working and covered.

### PR REVIEW: READY

1. Required artifacts exist and pass order is preserved.
2. Validation cases are marked `Implemented` or `Deferred` with rationale.

## Open Risks

1. BattleContext timing/order may still override intended deck source if integration points are not sequenced correctly.
2. Mixed GDScript/C# bridge changes may introduce editor/runtime signal regressions.
3. Changing fallback behavior may surprise workflows relying on legacy “all summons” behavior.

## Assumptions and Defaults

1. Debug arena deck precedence should be: explicit override > event-authored battle config > debug file > curated fallback.
2. Test Arena events using debug scene should be able to supply their own deck without modifying `debug_deck.json`.
3. The debug panel remains GDScript-driven for now; typed bridge is adapter-level.
4. Existing user-visible debug actions are baseline requirements, not optional niceties.

## Debug Function Parity Baseline

This initiative must preserve the current function surface. Refactors may relocate code, but behavior parity is required.

### `UnitSpawnerPanel` baseline

1. Spawn from curated/debug deck list for both teams (drag-and-drop spawn).
2. Spawn controls: `single`, `burst`, `paint`.
3. Formation controls for multi-spawn: `stack`, `line`, `arc`, `random`.
4. Formation spacing and burst count controls.
5. Team operations: clear player, clear enemy, clear all.
6. Undo last spawn batch.
7. Battle flow toggles: skip prep, enemy AI toggle, player AI toggle.
8. Search/filter/sort controls:
   - search by name/catalog id
   - filter by type/element/role
   - sort by name or mana
9. Spawn activity log.
10. Panel UX controls:
   - advanced drawer open/close
   - panel collapse/expand
   - persisted settings across sessions.

### `DebugMenu` baseline

1. Panel toggle hotkeys: backtick and `F12`.
2. FPS hotkeys + buttons: `F5/F6/F7/F8` and corresponding targets.
3. Battle flow shortcut: skip prep phase.
4. Visual debug toggles:
   - hurtboxes
   - target points
   - engage range
   - damage shapes
   - navigation footprint
   - projectile hit radius
   - summoner bubble
   - spawn boundary bypass.
5. Camera debug tools:
   - camera overlay toggle
   - auto-log toggle
   - zoom solver log toggle
   - diagnostic logging.
6. Console command entry + autocomplete + execution status.
7. Battle control buttons: instant win / lose.
8. Debug arena quick launch:
   - open test arena campaign map
   - launch specific arena battles from menu.
9. Snapshot manager launch.
10. Persisted debug menu settings across sessions.

## Pass Gate Status

Current state:
1. `PASS 1: USE CASES + VALIDATION` - completed
2. `PASS 2: STUBS + WIRING` - completed
3. `PASS 3: IMPLEMENTATION + TESTS` - completed
4. `PR REVIEW: READY` - pending

Gate note:
1. Use explicit approval text to advance.
2. If waiting, state: `blocked waiting approval`.
