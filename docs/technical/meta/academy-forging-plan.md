# Academy Forging Plan

**Status:** PASS 3 COMPLETE (Implementation + Tests)
**Initiative:** `academy-forging-model`
**Domain:** `meta`
**Last Updated:** `2026-05-18`
**Owner:** `Meta UX / Progression`

## Summary

This initiative reshapes the academy into a PC-first menu hub and course progression model. The target UX is: the campus hub provides fast menu-based access to Academy destinations, the Class Hall owns course enrollment and course continuation, and C# academy services remain the source of truth for semester state, required classes, available electives, prerequisites, activity progress, and rewards.

## Goals

1. Replace the text-heavy academy entry with a readable menu-based campus hub.
2. Auto-enroll mandatory first-semester coursework for fresh academy progress.
3. Present Class Hall courses by meaningful sections instead of one long list.
4. Move course choice details into a readable popup so the board stays scannable.
5. Keep enrollment validation, semester advancement, and rewards authoritative in C#.
6. Keep Campus Shop and Class Hall functional while final art/card assets remain temporary.

## Non-Goals

1. Final academy background art, final button styling, animation polish, or VFX.
2. Mobile layout support.
3. Full four-year academy content completion.
4. Economy rebalance outside the new academy course/reward scaffolding.

## Architecture Decisions

1. `AcademyProgressHandler` initializes fresh academy progress and assigns required courses.
2. `CampaignApi` remains the GDScript-facing facade; screens request state and actions through it.
3. Course availability is derived from candidate courses for the active semester, including approved carry-over cases.
4. Course display grouping metadata is emitted by the C# course view model, not guessed by the Class Hall UI.
5. The Class Hall board renders compact course cards; clicking a course opens a syllabus popup with description, activities, rewards, and action state.
6. Campus hub buttons are spatial overlays on the temporary academy image and route to the relevant screens.

## Public API / Interface / Type Changes

1. Academy course dictionaries expose enrollment state, activity state, reward previews, and display grouping metadata.
2. Campaign service/facade entrypoints expose academy progress, semester course lists, enrollment, activity completion, course completion, and semester advancement.
3. Academy battle completion can route back into course activity completion and reward flow.
4. Scene routes include the academy hub, class hall, course path, and campus shop surfaces.

## Legacy Removal Scope

1. Remove the old text-first academy menu as the primary campus entry.
2. Remove list-only Class Hall enrollment controls from the main board.
3. Remove broad course descriptions and enrollment actions from persistent side panels.
4. Disable noisy close/navigation click feedback where it harms menu feel.

## Pass Acceptance Criteria

### PASS 1: USE CASES + VALIDATION

1. Plan + validation artifacts exist and cover hub navigation, fresh academy initialization, course enrollment, semester carry-over, and course popup behavior.
2. Baseline scenarios map to C# or GUT coverage with `Design-Covered`, `Implemented`, or `Deferred` status.

### PASS 2: STUBS + WIRING

1. Academy hub, Class Hall, Campus Shop, and course path screens are wired through compile-safe scene routes and API calls.
2. Required course initialization and academy progress facades are present behind deterministic service entrypoints.

### PASS 3: IMPLEMENTATION + TESTS

1. Fresh progress auto-enrolls mandatory coursework, rejects invalid future enrollment, and supports approved second-semester carry-over.
2. Hub, shop, Class Hall, and course path screens load headlessly.
3. C# and GUT test suites pass with academy coverage live.

### PR REVIEW: READY

1. Review confirms pass-gate artifacts, service authority, and course grouping contract.
2. Review confirms remaining polish is art/UI iteration rather than broken academy function.

## Pass Gate Status

Current state:
1. `PASS 1: USE CASES + VALIDATION` (complete)
2. `PASS 2: STUBS + WIRING` (complete)
3. `PASS 3: IMPLEMENTATION + TESTS` (complete)
4. `PR REVIEW: READY` (ready)
