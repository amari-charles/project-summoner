# Academy Activity Limitations Stub Checklist

**Status:** PASS 2 CHECKLIST + PASS 3 COMPLETION NOTES
**Initiative:** `academy-activity-limitations`
**Domain:** `meta`
**Last Updated:** `2026-06-18`

## Types Created Or Extended

1. `AcademyActivityLimitations` - typed activity-local deck/loadout rule shape for fixed decks, temporary loaners, allowed card types/elements, min card counts, deck caps, required cards, and banned cards.
2. `AcademyCourseActivity` - now carries `Limitations` alongside battle config and rewards.
3. `AcademyProgressHandler` - exposes service-owned limitation summaries, placeholder deck-validity state, and launch-state dictionaries.
4. `AcademyCourseCatalog` - Practical Spellcraft practice has authored PASS 2 limitation data to prove catalog wiring.

## Interfaces Created Or Extended

1. `CampaignService.GetAcademyActivityLaunchState(courseId, activityId)` returns the activity dictionary plus selected-deck summary and placeholder validation.
2. `CampaignService.ResolveAcademyActivityBattleConfig(courseId, activityId)` returns the generic battle config boundary for Academy launches.
3. `CampaignApi` mirrors both calls for GDScript screens.
4. Activity dictionaries now include `limitations`, `limitation_summary`, `deck_validation`, and `invalid_reasons`.

## Wiring Points Updated

1. Course Path activity modal renders service-owned class rules and deck status text.
2. Course Path starts battles through the Academy launch resolver instead of reading raw activity battle config directly.
3. Course Path exposes an Edit Deck action that pushes a return route and opens the existing Collection/deck editor.
4. Class Hall course modal previews the next activity's service-owned class rules.

## Legacy Paths Removed Or Disabled

1. No battle-runtime Academy limitation checks were added.
2. No duplicate Academy deck editor was added.
3. Existing unrestricted/loaner battle config behavior remains compatible during PASS 2.

## Compile-Safe Stub Behavior Checks

1. Unrestricted activities emit an unrestricted deck-validation state and remain launchable through the old profile/current-deck path.
2. Limited activities emit rule summaries, concrete deck-validation state, and invalid reasons when the active deck violates class rules.
3. Launch config resolution returns the existing generic battle config shape.
4. Edit Deck navigation returns to Course Path through `NavigationContext`.

## Test Coverage Map

| Case ID | Test File | Test Name / Check | PASS 2 Status |
|---|---|---|---|
| L01 | `tests/csharp/Services/AcademyProgressServiceTest.cs` | existing no-loaner config compatibility + unrestricted validation shape | Stub-wired |
| L02 | `tests/csharp/Services/AcademyProgressServiceTest.cs` | fixed deck model field exists; full resolver deferred | Stub-wired |
| L03 | `tests/csharp/Services/AcademyProgressServiceTest.cs` | loaner config preservation and additional-loaner field shape | Stub-wired |
| L04-L08 | `tests/csharp/Services/AcademyProgressServiceTest.cs` | typed rule fields and Practical Spellcraft authored stubs | Stub-wired |
| L09 | Course Path/Class Hall UI wiring | service-owned summaries render without UI recomputation | Stub-wired |
| L10 | `tests/csharp/View/BattleSceneTest.cs` | generic `player_side.deck` path remains battle-owned | Existing coverage |
| L11 | Course Path UI wiring | invalid-start branch is wired; real invalid reasons deferred | Stub-wired |
| L12 | `tests/csharp/Data/AcademyCourseCatalogTest.cs` follow-up | Practical Spellcraft limitation content exists; full content assertion deferred | Stub-wired |
| L13 | Course Path UI wiring | Edit Deck routes to Collection and returns to Course Path | Stub-wired |
| D01 | `tests/csharp/Services/AcademyProgressServiceTest.cs` follow-up | deterministic composed-deck hashing deferred until PASS 3 resolver | Deferred to PASS 3 |

## PASS 3 Completion Notes

1. Selected active deck card instances resolve to catalog IDs and validate against first-pass rule fields.
2. Fixed and player-plus-loaner deck outputs compose into generic battle config without mutating saved decks.
3. Invalid activity starts are blocked before battle transition with specific invalid reasons.
4. Practical Spellcraft content and deterministic composed-deck order have focused C# coverage.

## Gate Output Requirement

1. PASS 3 ends with explicit request for PR review approval.
2. If approval is not provided, state: `blocked waiting approval`.
