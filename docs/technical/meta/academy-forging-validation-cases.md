# Academy Forging Validation Cases

**Status:** PASS 3 COMPLETE (C# + GUT Coverage Live)
**Initiative:** `academy-forging-model`
**Domain:** `meta`
**Last Updated:** `2026-05-18`
**Companion Plan:** `docs/technical/meta/academy-forging-plan.md`

## Case Matrix

| Case ID | Scenario | Expected Result | Test Type | Test File | Status |
|---|---|---|---|---|---|
| A01 | Fresh academy progress is opened for a summoner. | Year 1 Semester 1 initializes, remaining enrollments are set, and the mandatory intro course is enrolled. | unit | `tests/csharp/Services/AcademyProgressServiceTest.cs` | Implemented |
| A02 | Player attempts to enroll in a future semester course from the API. | Enrollment is rejected and progress is unchanged. | unit | `tests/csharp/Services/AcademyProgressServiceTest.cs` | Implemented |
| A03 | Player leaves an untaken intro element elective and reaches Semester 2. | Approved intro element carry-over remains enrollable. | unit | `tests/csharp/Services/AcademyProgressServiceTest.cs` | Implemented |
| A04 | Course dictionaries are requested for Class Hall rendering. | Course state includes group id, title key, sort order, track title, reward grant state, and activity state. | unit | `tests/csharp/Services/AcademyProgressServiceTest.cs` | Implemented |
| A05 | Player completes academy lesson, practice, and assessment activities. | Activity gates advance deterministically, course completes, transcript updates, and immediately grantable card rewards are granted. | unit | `tests/csharp/Services/AcademyProgressServiceTest.cs` | Implemented |
| A06 | Academy course catalog is loaded. | Year/semester content, rewards, and tuning remain catalog-consistent. | unit | `tests/csharp/Data/AcademyCourseCatalogTest.cs` | Implemented |
| A07 | Academy progress is persisted through profile conversion. | Academy fields round-trip without losing current semester, enrollments, transcript, or course activity state. | unit | `tests/csharp/Serialization/DtoConvertersTest.cs` | Implemented |
| A08 | Academy course battle configs are checked for first-pass tuning. | Onboarding and Semester 2 course activities use gentle enemy decks, HP, AI type, and difficulty settings. | unit | `tests/csharp/Data/AcademyCourseCatalogTest.cs` | Implemented |
| A09 | Academy hub scene loads. | Bounded walkable scene opens headlessly without missing script, asset, or route errors. | smoke | `scenes/meta/screens/walkable_academy_hub.tscn` | Implemented |
| A10 | Class Hall scene loads. | Scene opens headlessly and can request/render course data. | smoke | `scenes/meta/screens/academy_class_hall.tscn` | Implemented |
| A11 | Campus Shop scene loads. | Scene opens headlessly after shop UI changes. | smoke | `scenes/meta/screens/shop_screen.tscn` | Implemented |
| A12 | User clicks a course card in Class Hall. | Details, rewards, activities, and enrollment/continue action appear in a centered opaque popup. | integration | `tests/unit/meta` + manual PR validation | Design-Covered |
| A13 | Player completes a course whose reward is preview-only for the current pass. | Course completion still succeeds end-to-end, transcript updates, and no card grant side effect fires. | unit | `tests/csharp/Services/AcademyProgressServiceTest.cs` | Implemented |

## Deferred Cases

| Case ID | Reason Deferred | Planned Follow-up |
|---|---|---|
| A12 | Popup visual polish is still being iterated by design, and exact screenshot assertions would churn with temporary art. | Add a focused GUT UI contract after the Class Hall visual direction settles. |
| D01 | Non-card reward resolution is intentionally not implemented in this pass. The service marks these rewards as `preview_only` so class completion can work end-to-end without silently granting the wrong thing. | Add the selected reward/trait grant model once the academy reward-choice UX is designed. |

## Exit Criteria Mapping

### PASS 1: USE CASES + VALIDATION

1. User-facing academy flows have validation cases.
2. Service authority and UI smoke coverage are represented.

### PASS 2: STUBS + WIRING

1. New screens and API routes have loadable scene coverage.
2. C# service entrypoints have deterministic tests or planned coverage.

### PASS 3: IMPLEMENTATION + TESTS

1. Required course initialization, invalid enrollment rejection, carry-over enrollment, card reward grants, and preview-only reward completion are implemented.
2. Full C# and GUT suites pass before PR review.

### PR REVIEW: READY

1. Remaining deferred work is polish-specific and does not block function.
