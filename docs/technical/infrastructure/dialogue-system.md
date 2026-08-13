# Narrative Director Runtime

**Status:** IMPLEMENTED
**Design Source:** [Narrative Director and Dialogue System](../../design/narrative-dialogue-system.md)

The runtime has one narrative path. `NarrativeDirector` accepts typed application events, matches JSON-authored cues, orders them deterministically, enforces occurrence policy, and sends dialogue content to the presenter registered for the cue context.

## Runtime Components

- `data/narrative/narrative.json` — cues and localized dialogue-content references.
- `scripts/csharp/Application/Narrative/NarrativeDirector.cs` — matching, queueing, lifecycle, choices, and typed command dispatch.
- `scripts/csharp/Application/Narrative/ProfileNarrativeOccurrenceStore.cs` — attempt, summoner, and account occurrence state.
- `scripts/shared/narrative_dialogue_presenter.gd` — blocking line/choice playback.
- `scripts/infrastructure/services/narrative_director_api.gd` — typed GDScript-facing event boundary.

Gameplay and progression commits remain outside narrative. Consequential choices must pass through an injected `INarrativeCommandHandler`; unowned commands are rejected. Battle blocking crosses the explicit `BattleScene` pause/resume boundary and is prohibited for multiplayer-authored cues.

The removed DialogueManager, EventSequencer, BattleDialogueController, `.tres` dialogue resources, arbitrary string actions, and node-path execution are not supported compatibility formats.
