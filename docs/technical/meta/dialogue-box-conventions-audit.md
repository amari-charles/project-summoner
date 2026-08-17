# Dialogue Box Conventions Audit

**Status:** Research and implementation audit
**Date:** 2026-08-16
**Scope:** Campus NPC dialogue presentation; this does not replace the
[Narrative Director and Dialogue System](../../design/narrative-dialogue-system.md).

## Relevant Established Patterns

- Yarn Spinner separates line presentation from option presentation and treats
  advancing input as its own concern. Its standard line presenter exposes an
  explicit continue action, while its options presenter waits for a selected
  option before narrative execution resumes.
- Yarn Spinner authors options as potential player lines, not merely generic UI
  commands. It can also run the selected option as a spoken line.
- Ink similarly distinguishes displayed choice text, the chosen line's output,
  and the branch that follows. Choices may be conditional, one-time, reusable,
  or implicit fallbacks.
- Ren'Py provides text-speed preferences, skip behavior, rollback, and dialogue
  history. Fateforged's current design explicitly defers history/rollback, so
  those are reference points rather than V1 requirements.

## Baseline Fateforged Contract

The campus box should:

1. Show one localized speaker line at a time.
2. Advance from a left click on the visible box or the standard confirm input.
3. Ignore right clicks and held-key repeat.
4. Present authored player-spoken responses as a vertical list.
5. Move keyboard/controller focus to the first response without selecting it.
6. Never auto-select a response when dialogue is skipped.
7. Block campus movement while visible and restore it after completion.
8. Keep quest and progression mutation in their authoritative systems.

## Audit Result

Fixed in the current pass:

- The text label could consume pointer input before the panel received it.
- Any mouse button could advance dialogue.
- Held confirm input could repeat across lines.
- Response choices did not receive initial keyboard/controller focus.
- There was no safe skip-to-required-response behavior.

Still intentionally deferred:

- Typewriter text and the first-press-reveals/second-press-advances behavior.
- A visible continue indicator, text-speed setting, auto mode, portraits, and
  voice playback.
- Dialogue history or rollback, which the current design excludes from V1.
- Migrating the campus-specific presenter fully behind the shared Narrative
  Director contract. This is the important remaining architecture cleanup; it
  should happen before dialogue content expands substantially.

## Sources

- [Yarn Spinner: Dialogue Presenters](https://yarnspinner.dev/docs/unity/10-components/02-dialogue-view/)
- [Yarn Spinner: Options Presenter](https://yarnspinner.dev/docs/unity/10-components/02-dialogue-view/02-options-presenter/)
- [Yarn Spinner: Nodes, Lines, and Options](https://docs.yarnspinner.dev/2.1/getting-started/writing-in-yarn/lines-nodes-and-options)
- [Ink: Writing with Ink](https://github.com/inkle/ink/blob/master/Documentation/WritingWithInk.md)
- [Ren'Py: Dialogue History](https://www.renpy.org/doc/html/history.html)
- [Ren'Py: Preference Variables](https://www.renpy.org/doc/html/preferences.html)
