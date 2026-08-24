# Narrative Director and Dialogue System

**Status:** CURRENT DESIGN SPEC
**Last Updated:** 2026-08-24

## Purpose

Fateforged needs one general narrative system for tutorials, class instruction, character moments, boss introductions and aftermaths, lore, and future campus conversations. Dialogue is presentation; narrative direction decides when and why content plays; gameplay and progression systems remain authoritative over state changes.

This architecture fully replaces the current DialogueManager, EventSequencer, and BattleDialogueController model. Backward compatibility with their resource shapes, string actions, node paths, or saved state is not required.

## Responsibility Model

```text
typed gameplay/meta events ─┐
authored narrative cues ────┼─> Narrative Director ─> Dialogue Player ─> Context Presenter
durable narrative state ────┘            │
                                         └─> typed command request ─> authoritative owner
```

### Narrative Director

The Narrative Director is an application-layer orchestrator. It:

1. Receives typed events from battle, Academy, progression, and other application flows.
2. Matches events against authored narrative cues and their conditions.
3. Enforces occurrence policy such as once per attempt or once per summoner.
4. Orders eligible cues through one deterministic queue.
5. Selects the presenter for the current context.
6. Requests content playback and receives completion or choice results.
7. Sends typed command requests to the system that owns any resulting gameplay or progression change.

It does not mutate simulation state, search scene trees, call arbitrary functions, or interpret node paths.

### Narrative Cue

A cue describes **when and why** a narrative beat is eligible. It references dialogue content but does not contain gameplay implementation. First-pass trigger families are:

1. Preparation opened.
2. Battle started.
3. Battle phase changed.
4. Player command rejected.
5. Battle event occurred.
6. Battle resolved.
7. Activity completed.

Cue conditions and occurrence policies are typed authored data. Initial policies include once per attempt and once per summoner.

### Dialogue Content and Player

Dialogue content describes **what is said**: speaker, localized lines, portraits or presentation metadata, and choices. The Dialogue Player only advances that content and returns completion or a typed choice result. It does not own global variables, apply gameplay effects, pause arbitrary nodes, or decide which story beat should play.

### Context Presenters

Presenters render the same dialogue contract appropriately in preparation, battle, results, campus, or another future context. Presentation differences do not create separate narrative engines.

### Typed Commands

A dialogue choice may request an explicit typed command. The authoritative owner validates and executes it. Content cannot invoke arbitrary functions or encode effects in strings such as `variable=value`.

Choices are explicitly one of two kinds:

1. **Conversational:** affects only the current conversation and requires no durable game-state mutation.
2. **Consequential:** returns a typed result to an authoritative owner for durable, idempotent application. The UI clearly communicates that the choice will be remembered before confirmation.

## Ordering, Blocking, and Persistence

1. Eligible cues enter an ordered queue; concurrent callers do not silently discard one another. Ordering uses explicit priority, then source-event order, then stable cue ID. The same cue cannot be queued twice simultaneously.
2. The first implementation supports blocking dialogue. Non-blocking banter is a later extension and must use an explicit playback mode rather than implicit timing behavior.
3. Durable gameplay outcomes are committed before aftermath dialogue begins. For example, a boss victory is recorded before post-battle lore is shown, so quitting during dialogue cannot erase the win.
4. Every cue explicitly declares one occurrence policy: Always, Once per attempt, Once per summoner, or Once per account. Attempt state is ephemeral; summoner and account completion are durable. Reloading does not replay a completed durable cue or reroll narrative eligibility.
5. Narrative state is not authoritative for battle results, rewards, inventory, or quest completion.
6. Planned scene transitions wait for blocking dialogue to finish. A forced scene transition cancels visible playback without marking the cue completed, allowing it to become eligible again later. Confirmed choices are never applied twice, and authoritative gameplay outcomes are already durable before either case.
7. In single-player battle, blocking dialogue pauses the authoritative battle session and player input through an explicit session boundary. Preparation and Results block their own navigation/actions. Blocking narrative is prohibited in multiplayer; future multiplayer narrative must use an explicit non-blocking mode.
8. A queued cue's conditions are revalidated immediately before playback. A stale cue is discarded without being marked completed so an irrelevant moment is never forced on the player.

## Authoring Rules

1. Use typed event and condition identifiers, not signal names, scene-tree paths, or polling.
2. Separate cue, content, presentation, and effect data.
3. Do not create text-only Academy nodes. Attach teaching dialogue to the relevant preparation or gameplay event.
4. Narrative must support non-teaching use cases, including boss lore and character reactions.
5. Missing content references, invalid triggers, invalid command payloads, and impossible occurrence policies must fail content validation before runtime.
6. Essential objectives and rules must remain visible in the relevant gameplay UI; dialogue is never their only source.

## Player Control

1. The player may instantly reveal the current line and advance normally.
2. The player may skip an entire conversation; ordinary skipped dialogue is marked completed.
3. If a required choice remains, skipping jumps to the choice and never selects an answer automatically.
4. Skipping presentation does not skip, repeat, or reorder authoritative gameplay outcomes.
5. Reloading or replaying presentation never applies a confirmed consequential choice twice.

## Replay Scope

V1 does not include a dedicated dialogue history, transcript browser, or lore archive. Dialogue replays only when its authored occurrence policy permits it. Essential rules, objectives, and durable choice outcomes remain available through their owning gameplay UI or state rather than depending on dialogue history. A dedicated archive may be evaluated later from playtest demand.

## Replacement Rule

The new system is a clean replacement. During its gated implementation, existing narrative content that remains valuable will be migrated to the new authored model, after which the old managers, controllers, sequence resources, deprecated APIs, test scenes, and stale documentation are removed. No compatibility translator or indefinite dual execution path should remain.
