# Academy Class Flow Overhaul Plan

**Status:** PR REVIEW COMPLETE — READY FOR MANUAL TESTING
**Initiative:** `academy-class-flow-overhaul`
**Domain:** `meta`
**Last Updated:** 2026-08-13
**Owner:** Codex + user

## Summary

Replace the disjoint Class Hall, Course Path, activity modal, Collection detour, deck validation, and reward presentation with one coherent course flow and one full-screen activity-preparation experience. The product contract is [Academy Class Flow](../../design/academy-class-flow.md). Existing screens and data shapes are migration inputs, not compatibility constraints; once the new path is wired, superseded code is deleted.

## Goals

1. Give every course one canonical preview, active, and completed flow.
2. Make objectives, rules, rewards, and the actual battle loadout understandable before Start.
3. Represent fixed, owned, and class loadout modes explicitly and exclusively.
4. Attach narrative to meaningful activities instead of empty lesson nodes.
5. Remove duplicate and obsolete Academy paths rather than maintaining adapters.

## Non-Goals

1. Building the general Narrative Director inside this initiative; Academy consumes its approved contract.
2. Preserving current Academy save files, scene paths, modal APIs, or inferred loadout behavior.
3. Redesigning the general-purpose Collection/deck-management experience.
4. Producing final visual art during the architecture passes.

## Architecture Decisions

1. A single Course Flow presenter renders preview, active, and completed states from one service-owned view model.
2. A full-screen Activity Preparation presenter is the sole battle launch surface for Academy activities.
3. The Academy domain owns authored activity rules, deck mode, reward preview, and activity-local loadout state.
4. Deck modes are a required typed discriminated model: fixed deck, owned deck, or class loadout. Behavior is never inferred from non-empty card lists.
5. Class loadouts persist as activity progress while incomplete and remain separate from saved decks. Filling is an additive copy from a selected saved deck; saving explicitly creates a named deck or replaces a confirmed existing deck, does not change the active deck, and reports unowned supplied-card omissions.
6. Reward eligibility and claiming remain authoritative domain/application operations; UI renders their state.
7. Narrative is requested with typed context events through the general Narrative Director.
8. Battle runtime receives a resolved generic deck/config and remains Academy-agnostic.
9. Practice and assessment are presentation variants of the same activity flow, not separate screen or launch architectures.
10. Activity execution is typed and extensible, but dialogue is optional and never constitutes an activity by itself. Do not add a generic arbitrary-script activity escape hatch.
11. Activity role is typed independently from execution kind. Assessment role records a permanent official outcome; the initial policy treats defeat and abandonment as a poor completed outcome with no XP or victory reward.

## Public API / Interface / Type Changes

1. Replace overlapping `LoanerPlayerDeck`, `FixedClassDeck`, and `AdditionalLoanerCards` semantics with one typed activity loadout definition.
2. Add a Course Flow view model containing enrollment state, ordered activity state, inspectability, reward state, and navigation actions.
3. Add an Activity Preparation view model containing objective, rules, reward state, deck mode, resolved/editable slots, validity, and start readiness.
4. Add commands for enroll, inspect activity, update activity loadout, fill a loadout from a saved deck, create or replace a saved deck from a loadout, start activity, and claim/select reward.
5. Add typed Academy narrative events at preparation and activity lifecycle boundaries.
6. Replace `AcademyCourseActivityType` with independent typed execution-kind and activity-role contracts; add typed encounter style and deck mode, while lifecycle state is returned authoritatively in the view model.

## Legacy Removal Scope

1. Delete the Class Hall course-details modal and any duplicate course-detail composition once Course Flow owns it.
2. Delete text-only activity completion behavior and migrate useful copy to narrative cues.
3. Delete unconditional battle deck-edit actions, including Edit Deck for fixed-deck activities.
4. Delete Academy launch paths that leave course context for the general Collection screen.
5. Delete inferred loadout-mode logic based on card-list contents.
6. Delete or replace `LoanerPlayerDeck`, `FixedClassDeck`, and `AdditionalLoanerCards` after content migration.
7. Delete screen-side deck rule/reward derivation superseded by authoritative view models.
8. Delete UI behavior that filters claimed rewards out of course history.
9. Delete the text “Campus” exit action and replace it with the shared accessible back control.
10. Supersede conflicting assumptions in the completed `academy-activity-limitations` technical plan; do not preserve its compatibility-oriented loadout layering.
11. Remove obsolete tests and fixtures only after equivalent new validation cases are implemented.
12. Delete combined `Lesson`, `PracticeBattle`, `AssessmentBattle`, and `RewardChoice` activity-type behavior, the duplicate `IsOfficialAssessment` flag, and GDScript string comparisons against those values.

## Pass Acceptance Criteria

### PASS 1: USE CASES + VALIDATION

1. Product decisions are recorded in the design source of truth.
2. New ownership, interfaces, and full legacy-removal scope are explicit.
3. Validation cases cover all three deck modes, reward timing, navigation, lifecycle states, and narrative handoff.
4. Remaining product decisions are resolved collaboratively before Pass 2 approval.

### PASS 2: STUBS + WIRING

1. Final typed activity/loadout and view-model contracts compile.
2. New Course Flow and Activity Preparation scene shells are wired as the intended navigation path.
3. Conflicting old entry points are disconnected or removed.
4. Test skeletons map to every validation case.

### PASS 3: IMPLEMENTATION + TESTS

1. All course states, deck modes, reward states, and navigation behavior are implemented.
2. Academy narrative events integrate with the approved Narrative Director boundary.
3. Superseded code, resources, tests, and documentation are removed or rewritten.
4. Validation cases pass or are explicitly deferred with rationale.

### PR REVIEW: READY

1. Review confirms there is one canonical course flow and one Academy launch path.
2. Review confirms no compatibility adapter or duplicate legacy behavior remains.
3. Review confirms all pass artifacts and validation mappings are complete.

## Open Risks

1. Course Flow and Activity Preparation may share enough shell/navigation UI to warrant a common presenter component, but should not collapse into an ambiguous stateful modal.
2. Exact visual density remains playtest-tunable, but must preserve the approved information and interaction hierarchy.
3. Coordinating the Academy and Narrative Director initiatives requires their typed event boundary to be stubbed consistently in Pass 2.
4. Permanent assessment outcomes are intentionally provisional game design and require playtest evaluation rather than architectural entrenchment.

## Assumptions and Defaults

1. Existing development saves can be discarded.
2. Claimed rewards remain reviewable forever for that summoner/course.
3. Fixed activity rewards are first-success-only unless content explicitly declares otherwise in a future design.
4. All new visible copy is localized and included in content validation.

## Pass Gate Status

Current state:

1. `PASS 1: USE CASES + VALIDATION` (complete)
2. `PASS 2: STUBS + WIRING` (complete)
3. `PASS 3: IMPLEMENTATION + TESTS` (complete)
4. `PR REVIEW` (complete)

The implementation and PR review gates are complete. Manual gameplay testing is the remaining delivery check.

## PR Review Evidence

Reviewed on 2026-08-13 against the repository PR and structure checklists. The review consolidated fill/validation rule predicates, added explicit operation error results, enforced the global saved-deck maximum, and added rejection and supplied-card ownership coverage.

1. `dotnet build Fateforged.csproj --no-restore` - passed with 0 warnings and 0 errors.
2. Full C# suite - 1,198 passed.
3. Unit GUT suite - 246 passed with 1,797 assertions.
