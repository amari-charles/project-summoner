# Academy Activity Limitations Validation Cases

**Status:** PASS 3 COMPLETE
**Initiative:** `academy-activity-limitations`
**Domain:** `meta`
**Last Updated:** `2026-06-18`
**Companion Plan:** `academy-activity-limitations-plan.md`

## How To Use

1. Use these cases to drive Pass 2 stubs and Pass 3 implementation.
2. Keep limitation validation owned by Academy C# services.
3. Update status during implementation; do not mark cases implemented until tests or explicit review coverage exists.
4. PASS 3 implements the first-pass limitation engine and updates required cases to `Implemented`.

Allowed status values:
1. `Design-Covered`
2. `Implemented`
3. `Deferred`

## Case Matrix

| Case ID | Scenario | Expected Result | Test Type | Test File | Status |
|---|---|---|---|---|---|
| L01 | Activity has no limitation data | Activity remains launchable with the normal profile/current deck path and emits no forced player deck override | unit | `tests/csharp/Services/AcademyProgressServiceTest.cs` | Implemented |
| L02 | Activity provides a fixed class deck | View model says class deck is provided; launch config resolves to exactly that fixed deck regardless of selected player deck | unit | `tests/csharp/Services/AcademyProgressServiceTest.cs` | Implemented |
| L03 | Activity adds temporary loaner cards to the player deck | Launch config includes selected/player cards plus temporary loaner entries without granting collection ownership or mutating saved deck | unit | `tests/csharp/Services/AcademyProgressServiceTest.cs` | Implemented |
| L04 | Activity requires at least N spells and at least M summons | Valid deck passes; invalid deck returns specific missing-spell or missing-summon reasons and blocks start | unit | `tests/csharp/Services/AcademyProgressServiceTest.cs` | Implemented |
| L05 | Activity restricts allowed card types | Summoning Basics can allow summons-only; decks containing spells are invalid with a specific reason | unit | `tests/csharp/Services/AcademyProgressServiceTest.cs` | Implemented |
| L06 | Activity restricts allowed elements | Element class accepts its element plus Neutral and rejects other element cards with specific reasons | unit | `tests/csharp/Services/AcademyProgressServiceTest.cs` | Implemented |
| L07 | Activity enforces maximum deck size | Oversized deck is invalid; a deck at or below the cap is valid | unit | `tests/csharp/Services/AcademyProgressServiceTest.cs` | Implemented |
| L08 | Activity requires a specific teaching card | Deck missing required card is invalid unless the activity supplies that card as fixed/loaner | unit | `tests/csharp/Services/AcademyProgressServiceTest.cs` | Implemented |
| L09 | Course Path requests an activity view model | Class Rules, validity state, and invalid reasons render from service-owned fields; UI does not recompute rules | UI/unit | `tests/unit` or `tests/csharp/View` | Implemented |
| L10 | Player launches a valid restricted Academy battle | Battle receives a generic `player_side.deck` only when the Academy resolver intentionally emits one; battle runtime does not reference Academy limitation types | integration | `tests/csharp/View/BattleSceneTest.cs` | Implemented |
| L11 | Player attempts to launch an invalid restricted activity | Start is blocked before scene transition and the player sees exact invalid reasons | UI/unit | `tests/unit/meta` | Implemented |
| L12 | Practical Spellcraft prep-phase spell lockout lesson is authored | Activity can force/compose a deck that includes visible spell cards and uses class rules to explain why spells are unavailable during prep | content/unit | `tests/csharp/Data/AcademyCourseCatalogTest.cs` | Implemented |
| L13 | Deck selection is surfaced near the classroom | Course Path or Class Hall can show/select the active deck for the selected activity without leaving the Academy flow blind | UI/integration | `tests/unit/meta` + manual playtest | Implemented |

## Determinism Cases

| Case ID | Seed | Inputs | Checkpoints | Hash/State Assertions | Status |
|---|---|---|---|---|---|
| D01 | `limit-101` | Practical Spellcraft fixed/loaner spell-lockout activity | launch config build | Resolved deck entries are stable and ordered for the same selected deck | Implemented |

## Deferred Cases

| Case ID | Reason Deferred | Planned Follow-up |
|---|---|---|
| L14 | Full deck editor UX duplication may be larger than the limitation service contract | First implement activity-local deck validity and a simple classroom deck access/select surface; expand full editor duplication after UX review |
| L15 | Equipment, trait, rarity, and owned-copy limitation rules are likely needed later but not required for Practical Spellcraft V1 | Add as later rule kinds after first-pass card type/element/count limitations prove useful |

## Exit Criteria Mapping

### Pass 2

1. Every required case has a planned test type and file target.
2. Every case has a status value.
3. Stub checklist maps limitation data, service view models, launch resolution, and UI rendering separately.

### Pass 3

1. Every required case is `Implemented` or `Deferred`.
2. Each deferred case has explicit rationale and follow-up target.
3. Manual playtest confirms invalid decks are understandable before launch.
