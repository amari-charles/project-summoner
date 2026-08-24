# Academy Playtest Content Validation Cases

**Status:** PASS 3 COMPLETE, PR REVIEW READY
**Initiative:** `academy-playtest-content`
**Domain:** `meta`
**Last Updated:** `2026-06-09`
**Companion Plan:** `academy-playtest-content-plan.md`

## How To Use

1. Use these cases to evaluate Academy playtest-content changes before implementation and after tests.
2. Keep design-quality checks separate from product/design source-of-truth docs.
3. Update status during implementation passes.

Allowed status values:
1. `Design-Covered`
2. `Implemented`
3. `Deferred`

## Case Matrix

| Case ID | Scenario | Expected Result | Test Type | Test File | Status |
|---|---|---|---|---|---|
| C01 | Academy activity authors a loaner player deck | Serialized battle config includes `player_side.deck.cards` with catalog IDs and counts | unit | `tests/csharp/Services/AcademyProgressServiceTest.cs` | Implemented |
| C02 | Academy activity does not author a loaner player deck | Serialized battle config omits `player_side` and falls back to normal profile deck loading | unit | `tests/csharp/Services/AcademyProgressServiceTest.cs` | Implemented |
| C03 | Battle runtime receives a generic player deck override | Battle session loads the supplied cards without referencing Academy course types | integration | `tests/csharp/View/BattleSceneTest.cs` | Implemented |
| C04 | Magic 101 activity uses cards the player has not earned yet | Activity can be started and played with loaner cards before permanent rewards are granted | integration | `tests/csharp/Data/AcademyCourseCatalogTest.cs` | Implemented |
| C05 | Magic 101 reward cadence is reviewed | Only purposeful activities grant permanent cards; practice-only activities may grant progress only | content review | `tests/csharp/Services/AcademyProgressServiceTest.cs` | Implemented |
| C06 | Code review checks system independence | Battle code remains Academy-agnostic; Academy only serializes generic battle config | review | PR review checklist | Design-Covered |
| C07 | Magic 101 placeholder cards exist as normal catalog content | Starter unit, Magic Bolt, Training Target, and Weak Enemy Unit are ordinary card definitions with plain names and neutral affinity | unit | `tests/csharp/Cards/CardCatalogTest.cs` | Implemented |
| C08 | Magic 101 placeholder units have clear teaching behavior | Starter unit is plain melee, Training Target is passive/harmless, Weak Enemy Unit is low-pressure melee | unit | `tests/csharp/Units/AcademyPlaceholderUnitDefinitionsTest.cs` | Implemented |
| C09 | Academy activity rewards are service-owned and persisted | Current activity completion grants authored rewards once; failed activities do not grant; persisted claim keys prevent duplicate grants even if activity progress is rewound | unit | `tests/csharp/Services/AcademyProgressServiceTest.cs` | Implemented |
| C10 | Academy activity reward claim state persists | Claimed activity reward keys round-trip through campaign DTO conversion | unit | `tests/csharp/Serialization/DtoConvertersTest.cs` | Implemented |

## Determinism Cases

| Case ID | Seed | Inputs | Checkpoints | Hash/State Assertions | Status |
|---|---|---|---|---|---|
| D01 | `10101` | Magic 101 assessment default activity config | battle start, first enemy play, battle end | No missing-card failures; enemy deck and loaner deck load deterministically | Deferred |

## Deferred Cases

| Case ID | Reason Deferred | Planned Follow-up |
|---|---|---|
| D01 | Requires a dedicated deterministic battle smoke harness selection beyond the first Magic 101 content batch | Future battle smoke-test pass |

## Exit Criteria Mapping

### Pass 2

1. Every required case has a planned test type and file target.
2. Every case has a status value.
3. Loaner deck serialization is covered before Magic 101 content depends on it.

### Pass 3

1. Every required case is `Implemented` or `Deferred`.
2. Each deferred case has explicit rationale and follow-up target.
3. Manual playtest review checks the idea, UX, and code criteria from the companion plan.
