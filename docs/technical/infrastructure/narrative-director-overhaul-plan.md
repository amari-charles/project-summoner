# Narrative Director Overhaul Plan

**Status:** PASS 3 COMPLETE — READY FOR PR REVIEW
**Initiative:** `narrative-director-overhaul`
**Domain:** `infrastructure`
**Last Updated:** 2026-08-05
**Owner:** Codex + user

## Summary

Replace the overlapping DialogueManager, EventSequencer, and BattleDialogueController systems with one application-layer Narrative Director and a deliberately small Dialogue Player. The product and architecture contract is [Narrative Director and Dialogue System](../../design/narrative-dialogue-system.md). Valuable dialogue content will be migrated, but existing APIs and resource shapes receive no compatibility guarantees.

## Goals

1. Support teaching, reactive battle dialogue, boss lore, story choices, and future contextual conversations through one architecture.
2. Separate trigger/cue selection, dialogue playback, presentation, and authoritative state changes.
3. Use typed events, conditions, occurrence policies, choice results, and command requests.
4. Make ordering and persistence deterministic and testable.
5. Completely remove the old narrative execution paths after content migration.

## Non-Goals

1. A general cutscene engine for camera choreography, spawning, shops, animation, and arbitrary functions.
2. Giving narrative content direct write access to simulation, progression, inventory, rewards, or scene nodes.
3. Preserving old `.tres` sequence shapes, string actions, node paths, autoload APIs, or saves.
4. Shipping non-blocking ambient banter in the first implementation.
5. Building a dialogue history, transcript browser, or lore archive in V1.

## Architecture Decisions

1. Narrative Director lives in application orchestration above simulation/session and meta domain owners.
2. Typed source events are matched against authored Narrative Cues.
3. One ordered queue resolves simultaneous eligible cues deterministically.
4. Dialogue Player renders referenced content and returns completion or a typed choice result only.
5. Context presenters adapt playback to preparation, battle, results, campus, and future screens.
6. Gameplay effects cross back to authoritative owners as validated typed command requests.
7. Durable gameplay/progression results commit before aftermath narrative.
8. V1 supports blocking playback and explicit Always, Once-per-attempt, Once-per-summoner, and Once-per-account occurrence policies.
9. Missing/invalid authored references fail content validation before runtime.
10. Blocking is context-owned: single-player battle uses an authoritative session pause, non-simulation screens block their actions, and multiplayer rejects blocking cues.
11. Choice kind is typed as conversational or consequential. Consequential results are visibly signposted and applied idempotently by their authoritative owner.

## Public API / Interface / Type Changes

1. Add typed `NarrativeEvent`, trigger payload variants, and source adapter boundaries.
2. Add `NarrativeCue`, typed conditions, priority/order, occurrence policy, dialogue reference, playback mode, and optional result mapping.
3. Add `DialogueContent`, `DialogueChoice`, and typed `DialogueResult` contracts independent of global variables/effects.
4. Add `NarrativeDirector` queue/eligibility API and durable occurrence-state repository boundary.
5. Add `NarrativePresenter` contract and context registrations.
6. Add typed narrative command request/result contracts handled by authoritative application/domain services.

## Legacy Removal Scope

1. Delete `scripts/application/dialogue_manager.gd` and its autoload registration after callers migrate.
2. Delete `scripts/application/event_sequencer.gd` and remove its autoload/scene wiring.
3. Delete `scripts/battle/battle_dialogue_controller.gd` and its battlefield scene node.
4. Delete `scripts/infrastructure/data/event_sequence.gd` and `event_step.gd` after authored content migration.
5. Replace or remove legacy `dialogue_data.gd` and `dialogue_choice.gd` if their final contracts differ.
6. Replace `scripts/shared/dialogue_box.gd` with presenter-driven playback UI; remove deprecated `notify_ui_connected` behavior.
7. Migrate `resources/sequences/charge_tutorial.tres`, `first_trial_tutorial.tres`, `caravan_tutorial.tres`, and applicable dialogue resources; then delete old sequence resources.
8. Remove string `variable=value` actions, global dialogue variables, arbitrary custom-function steps, signal-name/node-path triggers, scene-tree polling, and dialogue-completion polling.
9. Remove hard-coded tutorial spawn/card/action methods from battle presentation code.
10. Rewrite `docs/technical/infrastructure/dialogue-system.md` to the implemented architecture or remove it in favor of the new canonical documents.
11. Update/remove `docs/workflows/creating-dialogue.md`, dialogue test scenes, and legacy tests that teach old authoring APIs.
12. Delete `scripts/infrastructure/services/dialogue_manager_api.gd`; callers use the new typed application boundary.
13. Replace legacy calls in `scripts/application/scene_coordinator.gd`, `scripts/meta/screens/event_screen.gd`, `shop_screen.gd`, `summoner_selection.gd`, and `first_card_selection.gd`.
14. Replace stale current-architecture guidance in `docs/architecture/application-layer.md`, `docs/features/events/architecture.md`, and `docs/architecture/gameplay/view/design-specs.md`. Historical archive and development-history records remain historical and are not runtime guidance.
15. Consolidate or close the narrower EventSequencer/dialogue cleanup entries in `docs/tracking/todos.md` when this initiative subsumes them.
16. Search project files and authored resources again during Pass 2 to expand this inventory before deletion begins.

## Pass Acceptance Criteria

### PASS 1: USE CASES + VALIDATION

1. General narrative use cases and ownership rules are documented independently of Academy.
2. Typed boundary and first-pass policies are decision-complete.
3. Validation cases cover ordering, persistence, choices, interruption, authoritative outcomes, and legacy removal.
4. Remaining product decisions are resolved collaboratively before Pass 2 approval.

### PASS 2: STUBS + WIRING

1. Final cue, event, dialogue, result, command, presenter, and occurrence-state contracts compile.
2. Director queue and source adapters have deterministic safe stub behavior.
3. Old runtime paths are disconnected as each equivalent stub boundary is wired; there is no double playback.
4. Test skeletons map to every validation case and the legacy inventory is expanded from a repository-wide search.

### PASS 3: IMPLEMENTATION + TESTS

1. Cue matching, ordering, occurrence policies, playback, choices, and persistence work end to end.
2. Academy and representative battle/boss narrative use the new system.
3. All superseded code, resources, autoloads, scene nodes, tests, and stale instructions are deleted or rewritten.
4. Validation cases pass or are explicitly deferred with rationale.

### PR REVIEW: READY

1. Review confirms narrative cannot directly mutate authoritative gameplay state.
2. Review confirms there is one narrative orchestration path and no compatibility layer remains.
3. Review confirms no arbitrary function, signal-path, node-path, or string-effect execution survives.

## Open Risks

1. Existing EventSequencer responsibilities unrelated to narrative require deletion or placement in purpose-built owners, not absorption into Narrative Director.
2. Choice consequence needs may reveal additional typed command variants during content inventory; new variants must still cross an authoritative typed boundary.
3. Persisted narrative state needs migration-free replacement wiring without accidentally becoming authoritative for gameplay outcomes.

## Assumptions and Defaults

1. Development saves and old authored resource formats can be discarded.
2. Queue ordering uses explicit priority plus stable authored order/identity as a tie-breaker.
3. V1 dialogue is blocking; non-blocking banter is a future explicit mode.
4. Battle simulation emits facts and never waits on or depends on narrative presentation.
5. Planned scene transitions await blocking dialogue; forced transitions cancel presentation without completing the cue, while already-confirmed choice effects remain idempotent.
6. Attempt occurrence state is ephemeral; summoner/account occurrence state is persisted by its matching authority scope.
7. Narrative never pauses battle by freezing scene-tree nodes or visual processes.

## Pass Gate Status

Current state:

1. `PASS 1: USE CASES + VALIDATION` (complete)
2. `PASS 2: STUBS + WIRING` (complete)
3. `PASS 3: IMPLEMENTATION + TESTS` (complete)

The implementation gate is complete. PR review is the next required gate.
