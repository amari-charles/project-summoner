# Academy Playtest Content Plan

**Status:** PASS 3 COMPLETE, PR REVIEW READY
**Initiative:** `academy-playtest-content`
**Domain:** `meta`
**Last Updated:** `2026-06-09`
**Owner:** `Codex + user`

## Summary

This initiative turns Academy course content into a playtestable progression experience, starting with a Magic 101 revamp. The work should prioritize clear player learning, meaningful deck growth, and robust systems that can support many future courses without course-specific battle hacks. Technical docs are used here to capture delivery criteria and validation expectations; product/design docs remain the source of truth for intent.

## Goals

1. Make each course activity teach or test one clear player behavior.
2. Keep battle runtime systems independent from Academy progression systems.
3. Let Academy activities author temporary/loaner player decks without requiring permanent card ownership.
4. Ensure rewards happen at purposeful moments rather than after every activity by default.
5. Add tests that protect content authoring, battle config translation, and duplicate-safe rewards.

## Non-Goals

1. Production art, audio, or final card names.
2. A full strategic AI rewrite.
3. Rewriting product/design intent docs to match current implementation.
4. Locking final balance numbers before playtest data.

## Quality Criteria

### Idea Quality

1. Each activity must have a single teachable purpose.
2. Enemy deck, HP, AI timing, and rewards must support that purpose.
3. Placeholder names should be plain and functional until the design is stable.
4. Not every activity needs a permanent reward; empty reward beats filler reward.

### UX Criteria

1. The player should understand what changed from the previous activity.
2. Early failure should communicate a specific mistake, such as not summoning or ignoring enemy units.
3. Loaner cards should let the player try a tool before earning it.
4. Completion rewards should feel earned and immediately explainable.
5. Courses should avoid fake “lesson” nodes that only exist to fill a path.

### Code Criteria

1. Battle code must not know about Academy course concepts.
2. Academy code may author battle config, but battle systems own loading and running that config.
3. Temporary player decks should use a general battle config contract, not Academy-only runtime branches.
4. New fields should be typed at the Academy authoring layer and serialized through one boundary.
5. Tests should verify the boundary contract instead of depending on UI screen behavior.

## Architecture Decisions

1. Academy activities can define a loaner player deck as typed course data.
2. The battle scene/session continues to consume generic battle config dictionaries.
3. Academy-authored loaner decks serialize into the generic `player_side.deck` battle contract.
4. Player mana should usually come from the player profile/summoner state, not fixed tutorial overrides.

## Public API / Interface / Type Changes

1. Add a typed Academy battle-config field for a loaner player deck.
2. Serialize that field into `player_side.deck` while enemy stats/deck/controller serialize into `enemy_side`.
3. Add tests that confirm Academy battle config can emit both enemy and loaner player side definitions.

## Legacy Removal Scope

1. The old loose battle config keys were replaced by explicit side definitions in the battle session layer.
2. The debug-named `dev_player_deck` bridge is no longer part of the Academy battle config path.
3. Literal lesson activities were removed from Magic 101 in favor of playable battle activities.

## Pass Acceptance Criteria

### PASS 1: USE CASES + VALIDATION

1. Quality criteria exist for idea, UX, and code review.
2. Validation cases cover Academy loaner deck authoring and battle-layer independence.

### PASS 2: STUBS + WIRING

1. Academy battle config has a typed loaner deck field.
2. Academy serialization emits the expected generic battle config shape.
3. Tests cover the new serialized contract.

### PASS 3: IMPLEMENTATION + TESTS

1. Magic 101 can use loaner cards before granting permanent rewards.
2. Existing Academy catalog tests still pass.
3. New tests prove the content path does not require pre-owned cards.

### PR REVIEW: READY

1. Review confirms no battle runtime dependency on Academy course types.
2. Review confirms UX criteria are reflected in Magic 101 activity structure.
3. Review confirms no product/design source-of-truth docs were rewritten to fit code drift.

## Open Risks

1. If deck editing or profile initialization assumptions differ, Magic 101 may need a clearer pre-battle explanation of loaner cards.
2. Academy authoring currently uses a default enemy mana profile; future tuning may need explicit per-activity enemy mana knobs.
3. Deterministic end-to-end battle smoke coverage is still deferred until the team chooses the standard battle harness shape.

## Assumptions and Defaults

1. Placeholder card/unit names are acceptable for internal playtesting.
2. Early Academy balance should be approximate and revised after manual play.
3. Player mana should be natural unless a specific tutorial moment truly needs an override.

## Pass Gate Status

Current state:
1. `PASS 3: IMPLEMENTATION + TESTS`
2. `PR REVIEW: READY`

Gate note:
1. User approved the first implementation step: Academy loaner deck support.
2. User approved continuing to Magic 101 content implementation.
3. Next content changes should still be proposed one at a time before implementation.
