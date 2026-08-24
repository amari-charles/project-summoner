# Narrative Director Overhaul Validation Cases

**Status:** PASS 3 COMPLETE
**Initiative:** `narrative-director-overhaul`
**Domain:** `infrastructure`
**Last Updated:** 2026-08-05
**Companion Plan:** [narrative-director-overhaul-plan.md](narrative-director-overhaul-plan.md)

## Case Matrix

| Case ID | Scenario | Expected Result | Test Type | Test File | Status |
|---|---|---|---|---|---|
| ND-01 | Preparation opens with an eligible teaching cue | Director queues it and the preparation presenter plays referenced content once | integration | `tests/unit/application/test_narrative_director.gd` | Implemented |
| ND-02 | Battle phase changes | Typed event data selects matching cues without node paths, signal names, or scene polling | unit | `tests/csharp/Application/NarrativeDirectorTest.cs` | Implemented |
| ND-03 | First relevant command is rejected | Once-per-attempt cue plays once; subsequent matching rejections in that attempt do not replay it | unit | `tests/csharp/Application/NarrativeDirectorTest.cs` | Implemented |
| ND-04 | Once-per-summoner cue has played | Reloading or starting a new attempt for the same summoner does not replay it | integration | `tests/csharp/Application/NarrativeDirectorTest.cs`, `tests/csharp/Serialization/DtoConvertersTest.cs` | Implemented |
| ND-05 | Multiple cues become eligible together | Queue uses priority, source-event order, then stable cue ID; no eligible cue is silently dropped | unit | `tests/csharp/Application/NarrativeDirectorTest.cs` | Implemented |
| ND-06 | Dialogue has choices | Player returns a typed result; Dialogue Player does not mutate gameplay/global variables | unit | `tests/csharp/Application/NarrativeDirectorTest.cs` | Implemented |
| ND-07 | Choice requests a gameplay/progression effect | Authoritative owner validates and executes a typed command; invalid requests fail safely | integration | `tests/csharp/Application/NarrativeDirectorTest.cs` | Implemented |
| ND-08 | Boss battle resolves, then aftermath lore begins | Victory is durably committed before playback; quitting during lore cannot erase the result or duplicate rewards | integration | `scripts/csharp/Battle/View/BattleScene.cs` (reviewed handoff) | Implemented |
| ND-09 | Same content is used in different contexts | Appropriate presenter renders it without creating another narrative engine | integration | `tests/unit/application/test_narrative_director.gd` | Implemented |
| ND-10 | Authored cue references missing/invalid data | Content validation fails before runtime with precise resource and field context | unit | `tests/csharp/Application/NarrativeDirectorTest.cs` | Implemented |
| ND-11 | A scene changes while blocking dialogue is active | Planned transitions wait; forced transitions cancel presentation without marking the cue complete or duplicating a confirmed choice | integration | `tests/csharp/Application/NarrativeDirectorTest.cs`, `scripts/application/scene_coordinator.gd` | Implemented |
| ND-12 | Battle replay/determinism is checked | Narrative presentation neither changes simulation state/hash nor becomes a simulation dependency | simulation | `tests/unit/application/test_narrative_director.gd` (structural isolation) | Implemented |
| ND-13 | Finished repository is structurally audited | No legacy manager/controller/sequencer, arbitrary call step, node-path trigger, string action, or dual playback path remains | structural | `tests/unit/application/test_narrative_director.gd` | Implemented |
| ND-14 | Player skips ordinary dialogue | Conversation ends, cue is marked completed, and authoritative state is unchanged | unit + integration | `tests/unit/application/test_narrative_director.gd` | Implemented |
| ND-15 | Player skips dialogue with a required choice | Playback jumps to the choice and no option or command is selected automatically | unit + integration | `tests/unit/application/test_narrative_director.gd` | Implemented |
| ND-16 | Dialogue contains essential instruction | Content validation requires the objective/rule to also exist in the relevant UI-owned activity data | unit | `tests/csharp/Application/NarrativeDirectorTest.cs` | Implemented |
| ND-17 | Cues use different occurrence policies | Always, attempt, summoner, and account scopes independently produce their authored replay behavior | unit | `tests/csharp/Application/NarrativeDirectorTest.cs` | Implemented |
| ND-18 | Reload durable occurrence state | Completed summoner/account cues remain completed; attempt state is not incorrectly persisted | integration | `tests/csharp/Serialization/DtoConvertersTest.cs` | Implemented |
| ND-19 | Blocking cue plays in single-player battle | Authoritative session and input pause and resume through the approved session boundary; scene nodes are not individually frozen | integration | `scripts/csharp/Battle/View/BattleScene.cs` (reviewed signal boundary) | Implemented |
| ND-20 | Blocking cue is authored for multiplayer | Content validation rejects the configuration; runtime never pauses the shared match | unit + multiplayer | `tests/csharp/Application/NarrativeDirectorTest.cs` | Implemented |
| ND-21 | Same cue becomes eligible while already queued | Director coalesces it rather than scheduling duplicate playback | unit | `tests/csharp/Application/NarrativeDirectorTest.cs` | Implemented |
| ND-22 | Queued cue becomes stale before playback | Conditions are revalidated and the cue is discarded without being marked completed | unit | `tests/csharp/Application/NarrativeDirectorTest.cs` | Implemented |
| ND-23 | Player confirms a conversational choice | Branching continues without creating durable gameplay state | unit | `tests/csharp/Application/NarrativeDirectorTest.cs` | Implemented |
| ND-24 | Player inspects a consequential choice | UI communicates permanence before confirmation | integration | `tests/unit/application/test_narrative_director.gd` | Implemented |
| ND-25 | Consequential choice result is delivered or retried | Authoritative owner persists the typed result exactly once and duplicate delivery is harmless | integration | `tests/csharp/Application/NarrativeDirectorTest.cs` | Implemented |
| ND-26 | Player completes dialogue in V1 | No dedicated history/archive UI is required; replay follows only the cue's occurrence policy | unit + integration | `tests/csharp/Application/NarrativeDirectorTest.cs` | Implemented |

## Determinism Cases

| Case ID | Seed | Inputs | Checkpoints | Hash/State Assertions | Status |
|---|---|---|---|---|---|
| ND-D01 | fixed summoner/attempt identity | identical typed event stream | every enqueue/dequeue | Identical cue order and occurrence state | Implemented |
| ND-D02 | fixed battle seed | same battle commands with narrative enabled/disabled | phase transitions and game over | Identical authoritative simulation hashes | Implemented |

## Deferred Cases

| Case ID | Reason Deferred | Planned Follow-up |
|---|---|---|
| ND-F01 | Non-blocking ambient banter is outside V1 and needs explicit mixing/interruption UX | Post-V1 Narrative Director extension |

## Exit Criteria Mapping

### Pass 2

1. Every non-deferred case has a final owner and test skeleton.
2. ND-13 names every concrete legacy path found by the repository-wide inventory.

### Pass 3

1. Every baseline case is `Implemented` or explicitly `Deferred`.
2. Focused tests, simulation determinism tests, content validation, and the project-wide suite pass.
