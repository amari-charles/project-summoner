# Academy Activity Limitations Plan

**Status:** PASS 3 COMPLETE (Implementation + Tests)  
**Initiative:** `academy-activity-limitations`  
**Domain:** `meta`  
**Last Updated:** `2026-06-18`  
**Owner:** `Codex + user`

## Summary

Academy classes need local activity limitations so courses can teach by constraining the player's deck and play pattern. This makes limitations part of the progression philosophy: a summoner may own many tools, but a class can require the player to solve a lesson under explicit academic rules. The system should support fixed class decks, normal player decks, restricted player decks, and player decks with temporary loaner cards. C# Academy services own limitation validation and launch-ready deck resolution; GDScript screens display class rules, deck validity, and classroom-adjacent deck selection without recreating domain rules.

## Goals

1. Add a typed Academy activity limitation model that can express first-pass deck rules.
2. Validate the selected/current deck in C# and return specific user-facing validity reasons.
3. Keep battle runtime Academy-agnostic by resolving limitations into generic battle config before launch.
4. Surface class rules and deck validity before activity start.
5. Put deck selection/editing access close to Class Hall and Course Path flows.

## Non-Goals

1. Rebuilding the full deck editor UI in this pass.
2. Implementing every future rule shape such as equipment bans, trait bans, sideboards, or drafting.
3. Making battle simulation enforce Academy rules after launch.
4. Reworking all Year 1 course content in the same pass.
5. Changing the core deck editing principle outside activity-local class rules.

## Architecture Decisions

1. Academy course/activity definitions own authored limitations.
2. `AcademyProgressHandler` or a nearby Academy domain helper owns deck validation and returns view-model data for screens.
3. GDScript screens render limitation summaries, validity state, failure reasons, and launch/deck-edit actions from service-owned data.
4. Battle launch consumes only a resolved generic `player_side.deck` when limitations require a fixed, loaner, or composed deck.
5. Normal unrestricted activities preserve current profile deck behavior and do not emit a `player_side` override.

## Public API / Interface / Type Changes

1. Add an Academy activity loadout/limitation model, likely on `AcademyCourseActivity`.
2. Add first-pass rule fields for:
   - fixed class deck
   - loaner/additional cards
   - allowed card types
   - allowed elements
   - minimum summons
   - minimum spells
   - maximum deck size
   - required cards
   - banned cards or categories
3. Add service view-model fields for limitation summaries, deck validity, invalid reasons, and launch readiness.
4. Add a launch resolver that returns the generic battle deck shape after applying fixed decks or loaners.
5. Add `CampaignApi` wrappers for any new deck validity or selected-deck calls needed by Academy screens.

## Legacy Removal Scope

1. Do not remove existing loaner deck support; fold it into the broader loadout model.
2. Avoid adding new screen-side deck-rule checks that would become legacy immediately.
3. Do not add battle-runtime Academy checks; any accidental Academy-specific battle branches should be rejected in review.

## Pass Acceptance Criteria

### PASS 1: USE CASES + VALIDATION

1. Product/design docs identify class limitations as core Academy progression philosophy.
2. Technical plan defines ownership, first-pass rule shapes, and battle-boundary behavior.
3. Validation cases cover fixed decks, restricted decks, loaners, invalid decks, and classroom-adjacent deck selection.

### PASS 2: STUBS + WIRING

1. Typed limitation/loadout data structures compile and are attached to activity definitions.
2. Service view models expose limitation summaries and placeholder validity state.
3. Course Path/Class Hall can render class rules without owning validation logic.
4. Stub tests map to validation cases and confirm default unrestricted activities remain compatible.
5. Stub checklist is created with explicit remaining implementation items.

### PASS 3: IMPLEMENTATION + TESTS

1. C# validates current/selected decks against first-pass limitations.
2. Academy launch resolves fixed decks, normal decks, and player-plus-loaner decks into the generic battle config contract.
3. Invalid decks block activity start with specific reasons.
4. Practical Spellcraft uses limitations to teach spell prep-phase lockout and mixed summon/spell deck construction.
5. Focused tests pass for services, catalog, battle config, and relevant UI wrappers.

### PR REVIEW: READY

1. Review confirms limitation rules remain Academy-owned and battle runtime remains Academy-agnostic.
2. Review confirms UI displays service-owned validity rather than recreating validation.
3. Review confirms no broad deck editor rewrite or unrelated content refactor slipped into the pass.

## Open Risks

1. Current deck-selection APIs may not expose enough classroom-adjacent deck metadata; Pass 2 may need a small service/view-model bridge.
2. Opening-hand behavior may need a separate activity option to make spell lockout visible during prep.
3. Rule summaries may need localization keys rather than generated strings if the first-pass copy grows.
4. Player-plus-loaner composition must avoid mutating the real deck or creating duplicate permanent ownership.

## Assumptions and Defaults

1. First-pass validation targets cards by type, element, ID, and simple count/cap rules.
2. Fixed class decks and loaner cards are temporary battle loadouts, not collection grants.
3. Invalid deck reasons can use plain functional localization at first.
4. Course Path is the first classroom-adjacent deck validity surface; Class Hall can follow with the same service data.

## Pass Gate Status

Current state:
1. `PASS 1: USE CASES + VALIDATION` (complete)
2. `PASS 2: STUBS + WIRING` (complete)
3. `PASS 3: IMPLEMENTATION + TESTS` (complete)
4. `PR REVIEW: READY` (waiting approval)

Gate note:
1. Product/design docs were updated to establish limitation philosophy.
2. PASS 2 added compile-safe limitation/loadout types, service-owned placeholder validity state, launch resolver wiring, Course Path/Class Hall UI surfaces, and the stub checklist.
3. PASS 3 implemented service-owned deck validation, fixed/player-plus-loaner deck resolution, invalid-start blocking, Practical Spellcraft limitation content, and focused test coverage.
4. Use explicit approval text to advance to `PR REVIEW: READY`.
5. blocked waiting approval
