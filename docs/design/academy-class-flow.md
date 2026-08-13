# Academy Class Flow

**Status:** DESIGN SPEC
**Last Updated:** 2026-08-05
**Related:** [Academy Forging Model](academy-forging-model.md), [Implementation Plan](../technical/meta/academy-class-flow-overhaul-plan.md)

## Purpose

Academy classes should feel like one coherent learning journey rather than a collection of unrelated nodes, modals, deck screens, and reward screens. This document is the product source of truth for the class-facing experience. It intentionally replaces the current UI flow; compatibility with existing Academy screens or save data is not a requirement.

## Course Flow

1. The Class Hall is a course browser, not a second course-details experience.
2. Selecting a course always opens one canonical full-screen Course Flow screen.
3. The same screen has three states:
   - **Preview:** shows the real path, activity rules, deck modes, and rewards before enrollment. Activities are inspectable but cannot be started. The primary action is Enroll.
   - **Active:** shows progress, the current activity, future activities, and earned or remaining rewards.
   - **Completed:** remains available for reviewing completed activities, rewards, and replayable practice.
4. There are no text-only activity nodes. Information is delivered through contextual dialogue while the player is preparing for or performing a meaningful activity.
5. Navigation uses a consistent back-arrow icon in the top-left. It has an accessible destination label and tooltip; it is not a text button named “Campus.”
6. Locked future activities reveal their activity type, title, deck mode, relevant rules, and possible rewards. Story-specific surprises may remain hidden. The exact visual density is a playtest-tunable presentation choice.

## Activity Preparation

Every battle activity opens a dedicated full-screen Activity Preparation state. It keeps the course and activity context visible while showing:

1. Objective and relevant rules.
2. Available and already-earned rewards.
3. The exact loadout that will enter the activity.
4. Loadout validity and actionable correction controls when player input is required.
5. The Start action.

This is part of the canonical flow, not a small modal and not a detour to the general Collection screen.

## Practice And Assessment Presentation

Practice and assessment are roles within the same activity and preparation system, not different UI systems.

1. Practice uses lighter milestone styling and clearly communicates that it is replayable learning.
2. Assessment uses stronger milestone styling and clearly communicates that its official outcome is permanent.
3. Both use the same Course Flow, Activity Preparation, battle launch, and results components.
4. Assessment performance follows the existing Academy grading rules: objectives drive grades, Honors, and reward upside; failure usually removes upside rather than blocking the summoner's overall progression.
5. Assessment is an explicit typed activity role so its stakes are never inferred from labels or UI. In the first implementation, an assessment defeat or abandonment records the official poor outcome and advances the course; it grants neither XP nor victory rewards.
6. The permanent-assessment model must be evaluated through playtesting and may be revised if its stakes do not produce good play.

## Deck Modes

Every battle activity declares exactly one explicit deck mode:

1. **Fixed deck:** the class supplies the entire read-only deck. Player decks are neither selected nor validated, and no deck-edit control is shown.
2. **Owned deck:** the player chooses a saved deck and that deck is validated against the activity rules within preparation.
3. **Class loadout:** the class places required teaching cards into locked slots in the normal deck grid, and the player fills the remaining slots from owned cards within the same preparation screen. A lock treatment and class marker communicate why those cards cannot be changed without requiring a separate card section.

A class loadout belongs to the activity. It persists while that activity is incomplete, but never mutates or overwrites a saved deck. A separate explicit **Save as Deck** action may copy it into the player’s deck collection.

## Rewards

1. Activity rewards are visible in Activity Preparation before the player starts.
2. Fixed rewards are granted immediately on the first successful completion.
3. Selectable rewards pause forward progression until the choice is made.
4. Course rewards are awarded after the final required activity.
5. Results distinguish **Earned now** from **Course progress**.
6. Claimed rewards remain visible and are marked **Earned**; they are not removed from the course display.
7. Replaying practice does not grant the same one-time reward again.
8. A course or activity may intentionally grant no immediate reward.
9. Results first show the activity outcome and immediately granted fixed rewards. If a selectable reward is pending, its options appear within that same Results screen and forward progression remains blocked until the player confirms a choice. There is no separate reward-choice screen.

## Dialogue In The Flow

Dialogue is attached to meaningful moments such as opening preparation, beginning battle, entering a phase, rejecting an action for the first time, satisfying an objective, resolving a battle, or completing an activity. Dialogue may teach, characterize, react, or deliver story. Academy dialogue uses the general Narrative Director defined in [Narrative Director and Dialogue System](narrative-dialogue-system.md); it does not own a separate tutorial dialogue engine.

Dialogue is optional. An activity does not need dialogue, special scripting, or a narrative cue when its gameplay is already self-explanatory.

## Activity Extensibility

The activity model may support additional typed, meaningful interaction kinds in the future, but new kinds are implemented only when their gameplay is designed. A battle may stand alone as an activity. Dialogue alone is never an activity, and the architecture should not create a generic arbitrary-script activity type.

Each course activity is composed from independent typed dimensions rather than one combined type:

1. **Execution kind:** Battle, Lab, or another explicitly implemented interaction.
2. **Role:** Standard, Practice, or Assessment.
3. **Encounter style:** Standard, Boss, Challenge, or another implemented presentation/gameplay style.
4. **Deck mode:** Fixed deck, Owned deck, or Class loadout.
5. **Lifecycle state:** Locked, Available, Active, or Completed.

For example, a boss exam is `Battle + Assessment + Boss` with its chosen deck mode. This avoids proliferating combined types and prevents UI labels or booleans from secretly defining behavior.

## Replacement Rule

The overhaul should remove superseded screens, modals, inferred deck modes, and duplicate launch paths once their replacements are wired. Existing saves may be discarded. No adapters, compatibility branches, or dual-path operation should be added solely to retain the current implementation.
