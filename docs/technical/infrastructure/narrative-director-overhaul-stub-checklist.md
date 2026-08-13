# Narrative Director Overhaul Stub Checklist

**Status:** PASS 3 COMPLETE
**Initiative:** `narrative-director-overhaul`
**Domain:** `infrastructure`
**Last Updated:** 2026-08-05

## Types Created

1. Typed narrative event, context, occurrence-policy, playback-mode, and choice-kind enums.
2. `NarrativeEvent` and `NarrativeCueDefinition`.
3. `DialogueContentDefinition`, `DialogueChoiceDefinition`, and `DialogueResult`.
4. `NarrativeCommandRequest` with an idempotency key.

## Interfaces Created

1. `INarrativeOccurrenceStore` - durable/ephemeral occurrence boundary.
2. `NarrativeDirector.PublishEvent` - typed application event ingress.
3. Presenter registration/unregistration and cue completion/cancellation boundaries.
4. `NarrativeDirectorApi` - enum-backed GDScript bridge.

## Wiring Points Updated

1. `NarrativeDirector` is registered as a C# autoload.
2. Activity Preparation publishes PreparationOpened through the new boundary.
3. Battlefield no longer instances `BattleDialogueController` or legacy `DialogueBox`.
4. C# autoload paths expose Narrative Director.
5. `BattleScene` adapts BattleStarted, PhaseChanged, and BattleResolved facts into typed Narrative Director events without changing simulation.

## Legacy Paths Removed or Disabled

1. Battle-scene `BattleDialogueController` node - removed from active wiring.
2. Battle-scene legacy `DialogueBox` - removed from active wiring.
3. `DialogueManager` and `EventSequencer` autoloads - removed.
4. Legacy managers, controllers, data resources, dialogue UI, and sequence assets - deleted after content migration.
5. Event, shop, onboarding, and scene-transition callers - migrated to Narrative Director.
6. Legacy authoring/architecture documents and cleanup TODO - rewritten or closed.

## Compile-Safe Stub Behavior Checks

1. Valid typed events are matched and queued with monotonically increasing source order.
2. Invalid enum values and empty source IDs fail deterministically.
3. Authored cues play through a registered context presenter.
4. Completion, cancellation, occurrence state, and typed commands enforce their final boundaries.
5. `dotnet build --no-restore` and Godot headless parsing pass.

## Test Skeleton Coverage Map

| Case IDs | Skeleton Test File | Coverage |
|---|---|---|
| ND-01–ND-26, ND-D01–ND-D02 | `tests/unit/application/test_narrative_director.gd`, `tests/csharp/Application/NarrativeDirectorTest.cs` | Queue, policy, lifecycle, choice, validation, and structural assertions |

## Gate Output Requirement

Pass 3 is complete. The implementation is ready for the PR review gate.
