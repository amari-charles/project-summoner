# Creating Narrative Dialogue

Use the canonical design in [Narrative Director and Dialogue System](../design/narrative-dialogue-system.md).

1. Add localized speaker, line, and choice keys to `localization/data/en.json`.
2. Add a dialogue-content entry to `data/narrative/narrative.json`.
3. Add a cue that references the content and declares its typed trigger, context, priority, occurrence policy, and conditions.
4. Publish the corresponding typed event through `NarrativeDirectorApi` or the C# `NarrativeDirector` boundary.
5. If a choice is consequential, define a typed command with a stable idempotency key and provide an authoritative `INarrativeCommandHandler` owner.
6. Run `./tools/run_tests.sh`; authored localization and content references are validated before runtime.

Do not add arbitrary function calls, node paths, signal names, global dialogue variables, or string effects to content. Essential instructions must also identify the UI fact that carries the same rule or objective.
