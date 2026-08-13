# Academy Class Flow Overhaul Stub Checklist

**Status:** PASS 3 COMPLETE
**Initiative:** `academy-class-flow-overhaul`
**Domain:** `meta`
**Last Updated:** 2026-08-13

## Types Created

1. `AcademyActivityExecutionKind` - meaningful runtime interaction kind.
2. `AcademyActivityRole` - Standard, Practice, or Assessment stakes.
3. `AcademyEncounterStyle` - Standard, Boss, or Challenge encounter identity.
4. `AcademyDeckMode` - Fixed, Owned, or ClassLoadout ownership contract.
5. `AcademyActivityLifecycleState` - authoritative Locked, Available, Active, or Completed state.

## Interfaces Created

1. `CampaignService.GetAcademyCourseFlowState` - canonical Course Flow read model boundary.
2. `CampaignService.GetAcademyActivityPreparationState` - canonical preparation read model boundary.
3. `UpdateAcademyActivityLoadout` - persists validated activity-local owned-card slots.
4. `FillAcademyActivityLoadoutFromDeck` - copies compatible cards from a saved deck into open activity-local slots without mutating the source deck.
5. `SaveAcademyActivityLoadoutToDeck` - explicitly creates a named saved deck or replaces a confirmed existing deck while reporting class-supplied cards the player does not own.
6. Matching `CampaignApi` GDScript facade methods.

## Wiring Points Updated

1. Class Hall course selection routes directly to `academy_course_flow.tscn`; its details modal is disconnected.
2. Course Flow shell renders preview/active/completed state and activity inspection from the canonical service boundary.
3. Activity Preparation shell consumes the preparation boundary and publishes a typed PreparationOpened narrative event.
4. Battle return context points to Course Flow.
5. Academy JSON uses independent execution, role, encounter-style, and deck-mode fields.

## Legacy Paths Removed or Disabled

1. `AcademyCourseActivityType` combined enum - removed.
2. `IsOfficialAssessment` and authored `repeatable` flags - removed; role is authoritative.
3. Text-only lesson entries in `data/academy/courses.json` - removed.
4. Class Hall details modal - deleted.
5. `academy_course_path.tscn`, its script, and its tests - deleted.
6. `LoanerPlayerDeck`, `FixedClassDeck`, and `AdditionalLoanerCards` - replaced by `AcademyActivityLoadoutDefinition`.
7. Old string branches and Collection detour - deleted.

## Compile-Safe Stub Behavior Checks

1. Invalid loadout updates return `false`; valid changes persist through the profile repository.
2. Preparation Start follows authoritative loadout validity and launches the resolved config.
3. Course Flow can inspect locked activities without launching them.
4. `dotnet build --no-restore` passes.
5. Godot headless editor import parses the new autoload and scenes without script errors.

## Test Skeleton Coverage Map

| Case IDs | Skeleton Test File | Coverage |
|---|---|---|
| ACF-01–ACF-25, ACF-D01 | `tests/unit/meta/test_academy_class_flow.gd`, `tests/csharp/Services/AcademyProgressServiceTest.cs` | Runtime, persistence, outcome, and structural assertions |
| ACF-19, ACF-23 | `tests/csharp/Data/AcademyActivityDefinitionTest.cs` | Executing contract/catalog assertions |

## Gate Output Requirement

Pass 3 is complete. The implementation is ready for the PR review gate.
