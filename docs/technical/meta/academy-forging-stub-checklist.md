# Academy Forging Stub Checklist

**Status:** PASS 2 CHECKLIST
**Initiative:** `academy-forging-model`
**Domain:** `meta`
**Last Updated:** `2026-05-18`

## Types Created Or Extended

1. `AcademyProgressHandler` - academy progress initialization, enrollment validation, activity completion, reward grants, semester advancement, and course view-model dictionaries.
2. `AcademyCourseCatalog` - early academy course definitions, activities, rewards, prerequisites, and choice groups.
3. `AcademyClassHall` (`scripts/meta/screens/academy_class_hall.gd`) - Class Hall board, tabs, period picker, course cards, and course popup.
4. `AcademyHub` (`scripts/meta/screens/academy_hub.gd`) - spatial campus hub routing over temporary academy art.
5. `ShopScreen` (`scripts/meta/screens/shop_screen.gd`) - campus shop layout revisions.

## Interfaces Created Or Extended

1. Campaign service academy methods for progress, course lookup, semester course lists, enrollment, activity completion, course completion, and semester advancement.
2. `CampaignApi` GDScript facade methods for academy screens.
3. Course dictionary contract for `group_id`, `group_title_key`, `group_sort_order`, and `track_title_key`.
4. Battle/course completion bridge for academy assessment outcomes.

## Wiring Points Updated

1. Scene manager routes for academy hub, Class Hall, academy course path, and shop.
2. Campus hub overlay buttons route to Class Hall, Campus Shop, Mission Hall, Dorms, and Online Arena placeholders or screens.
3. Class Hall course cards open a popup instead of relying on a persistent right-hand detail panel.
4. Class Hall tabs split `My Classes` from `Open Classes`.
5. Academy progress changes refresh screens through campaign progress signals.

## Legacy Paths Removed Or Disabled

1. Old text-first academy hub copy and enrollment framing.
2. Class Hall full-width card/list behavior.
3. Persistent right-side course description/action panel.
4. UI-side hardcoded course taxonomy for choice groups and track ordering.
5. Distracting close/navigation click sound path.

## Compile-Safe Stub Behavior Checks

1. Academy hub, Class Hall, and shop scenes load headlessly.
2. Course popup remains bounded to a percentage of viewport height and uses opaque readable panel styling.
3. Mandatory first-semester course assignment happens during progress initialization.
4. Enrollment validation rejects future semester courses through service logic.
5. Course grouping metadata comes from C# view-model fields consumed by GDScript.

## Test Coverage Map

| Case ID | Test File | Test Name / Check | Notes |
|---|---|---|---|
| A01 | `tests/csharp/Services/AcademyProgressServiceTest.cs` | `FreshAcademyProgress_AutoEnrollsRequiredIntroCourse` | Implemented |
| A02 | `tests/csharp/Services/AcademyProgressServiceTest.cs` | `EnrollAcademyCourse_RejectsFutureSemesterCourse` | Implemented |
| A03 | `tests/csharp/Services/AcademyProgressServiceTest.cs` | `EnrollAcademyCourse_AllowsUntakenIntroElementsInSecondSemester` | Implemented |
| A04 | `tests/csharp/Services/AcademyProgressServiceTest.cs` | `GetAcademyCourse_ExposesDisplayGroupMetadata` | Implemented |
| A05 | `tests/csharp/Services/AcademyProgressServiceTest.cs` | activity completion and reward tests | Implemented |
| A09-A11 | scene load commands | hub, Class Hall, shop headless loads | Implemented |

## Gate Output Requirement

1. End Pass 2 with explicit request for Pass 3 approval when running this workflow prospectively.
2. Current PR state has completed implementation and is ready for PR review.
