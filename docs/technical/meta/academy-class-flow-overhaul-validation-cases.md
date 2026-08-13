# Academy Class Flow Overhaul Validation Cases

**Status:** PR REVIEW COMPLETE
**Initiative:** `academy-class-flow-overhaul`
**Domain:** `meta`
**Last Updated:** 2026-08-13
**Companion Plan:** [academy-class-flow-overhaul-plan.md](academy-class-flow-overhaul-plan.md)

## Case Matrix

| Case ID | Scenario | Expected Result | Test Type | Test File | Status |
|---|---|---|---|---|---|
| ACF-01 | Open an unenrolled course | Canonical Course Flow shows the real path, rules, deck modes, rewards, and Enroll; Start is unavailable | integration | `tests/unit/meta/test_academy_class_flow.gd` | Implemented |
| ACF-02 | Open an active or completed course | The same Course Flow renders progress or history without a parallel details modal | integration | `tests/unit/meta/test_academy_class_flow.gd` | Implemented |
| ACF-03 | Inspect a fixed-deck battle | Preparation shows the supplied read-only deck and never selects, validates, or edits a player deck | unit + integration | `tests/csharp/Services/AcademyProgressServiceTest.cs` | Implemented |
| ACF-04 | Inspect an owned-deck battle | Preparation allows saved-deck selection and shows authoritative rule validity in context | unit + integration | `tests/csharp/Services/AcademyProgressServiceTest.cs` | Implemented |
| ACF-05 | Build a class loadout | Required cards occupy visibly locked slots in the normal deck grid; player fills remaining slots in place; validity and Start update deterministically | unit + integration | `tests/csharp/Services/AcademyProgressServiceTest.cs` | Implemented |
| ACF-06 | Leave and return to an incomplete class loadout | Activity-local choices persist without changing any saved deck | integration | `tests/csharp/Services/AcademyProgressServiceTest.cs` | Implemented |
| ACF-07 | Explicitly save a class loadout | Save to My Decks explicitly creates a named deck or replaces a confirmed existing deck, preserves the active-deck selection, and reports class-supplied cards omitted because they are not owned | integration | `tests/csharp/Services/AcademyProgressServiceTest.cs` | Implemented |
| ACF-25 | Fill a class loadout from a saved deck | Compatible owned cards copy in saved-deck order into open Lesson Loadout slots; supplied cards remain locked and the source deck is unchanged | integration | `tests/csharp/Services/AcademyProgressServiceTest.cs` | Implemented |
| ACF-08 | Inspect activity rewards before play | Fixed, selectable, and absent rewards are represented accurately in preparation | unit + integration | `tests/csharp/Services/AcademyProgressServiceTest.cs` | Implemented |
| ACF-09 | Win an activity with a fixed reward | Reward is granted once and Results separates Earned now from Course progress | integration | `tests/csharp/Services/AcademyProgressServiceTest.cs` | Implemented |
| ACF-10 | Win an activity with a selectable reward | Results presents inspectable options in place and progress pauses until a valid selection is authoritatively confirmed | integration | `tests/unit/meta/test_academy_activity_results.gd` | Implemented |
| ACF-11 | Reopen or replay completed content | Claimed rewards remain visible as Earned and one-time rewards are not duplicated | integration | `tests/csharp/Services/AcademyProgressServiceTest.cs` | Implemented |
| ACF-12 | Complete final required activity | Course reward becomes claimable/grants at the course boundary, separate from activity reward | integration | `tests/csharp/Services/AcademyProgressServiceTest.cs` | Implemented |
| ACF-13 | Encounter authored teaching text | It plays from a meaningful preparation/gameplay trigger; no text-only node auto-completes | integration | `tests/unit/application/test_narrative_director.gd` | Implemented |
| ACF-14 | Navigate back from Course Flow or Preparation | Shared back icon returns to the correct parent context and exposes an accessible label | integration | `tests/unit/meta/test_academy_class_flow.gd` | Implemented |
| ACF-15 | Validate Academy content | Invalid deck modes, missing rewards/content, and missing localization keys fail pre-runtime validation | unit | `tests/csharp/Data/AcademyCourseCatalogTest.cs`, `tests/unit/test_localization_keys.gd` | Implemented |
| ACF-16 | Search the finished runtime for legacy paths | No old course modal, Collection launch detour, inferred loadout mode, or legacy loadout fields remain | structural | `tests/unit/meta/test_academy_class_flow.gd` | Implemented |
| ACF-17 | Inspect a locked future activity | Type, title, deck mode, relevant rules, and possible rewards are visible while marked story surprises remain hidden | integration | `tests/unit/meta/test_academy_class_flow.gd` | Implemented |
| ACF-18 | Play a battle with no authored dialogue | Activity prepares, launches, resolves, and advances normally without requiring a narrative cue | integration | `tests/csharp/Services/AcademyProgressServiceTest.cs` | Implemented |
| ACF-19 | Validate activity kinds | Typed meaningful kinds are accepted; dialogue-only and arbitrary-script activity definitions are rejected | unit | `tests/csharp/Data/AcademyActivityDefinitionTest.cs` | Implemented |
| ACF-20 | Lose an official assessment | A permanent poor outcome is recorded, the course advances, and no XP or victory reward is granted | integration | `tests/csharp/Services/AcademyProgressServiceTest.cs` | Implemented |
| ACF-21 | Leave an official assessment after it starts | Abandonment records the same poor official outcome as defeat and grants no XP or victory reward | integration | `tests/csharp/Services/AcademyProgressServiceTest.cs` | Implemented |
| ACF-22 | Lose or leave a practice activity | No XP or victory reward is granted; practice remains available and no official transcript outcome is recorded | integration | `tests/csharp/Services/AcademyProgressServiceTest.cs` | Implemented |
| ACF-23 | Author different activity combinations | Execution kind, role, encounter style, deck mode, and lifecycle state compose without combined enum variants or duplicate booleans | unit | `tests/csharp/Data/AcademyActivityDefinitionTest.cs` | Implemented |
| ACF-24 | Render activity type information | UI consumes authoritative typed/view-model fields without comparing legacy activity-type strings | structural + integration | `tests/unit/meta/test_academy_class_flow.gd` | Implemented |

## Determinism Cases

| Case ID | Seed | Inputs | Checkpoints | Hash/State Assertions | Status |
|---|---|---|---|---|---|
| ACF-D01 | summoner seed + activity identity | reload before completion | initial, edited, reloaded | Same activity-local loadout and reward offer; reload does not reroll | Implemented |

## Deferred Cases

None currently. Any deferral requires explicit rationale and a named follow-up target.

## Exit Criteria Mapping

### Pass 2

1. Every case has a final interface owner and test skeleton.
2. Structural removal assertions name the actual legacy files/types discovered during stubbing.

### Pass 3

1. Every case is `Implemented` or explicitly `Deferred`.
2. Focused tests and the project-wide suite pass.
