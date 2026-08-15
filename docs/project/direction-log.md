# Fateforged Product Direction Log

## Purpose

This document records meaningful changes in Fateforged's product and game direction: what was decided, why it changed, and what earlier direction it replaced.

It is not a release changelog, implementation diary, or list of every local design choice. Public release notes belong in [changelog.md](changelog.md), technical progress belongs in [development-history.md](development-history.md), and the current intended behavior belongs in the relevant product or design document.

## What Belongs Here

Record an explicitly approved decision when it does one or more of the following:

- Changes the structure of the player experience or core game loop.
- Introduces, replaces, or retires a major feature or player-facing flow.
- Changes which feature or system owns an important behavior or rule.
- Establishes a constraint that affects multiple features, screens, or future work.
- Changes progression, rewards, economy, matchmaking, content structure, or another broad product model.
- Supersedes prior direction in a way that future contributors may otherwise misunderstand.

A useful test is: **will someone later need to know why the game is structured this way, rather than merely how it was implemented?** If yes, the decision likely belongs here.

## What Does Not Belong Here

Do not record:

- Routine implementation details or file-level architecture choices.
- Refactors that preserve product behavior.
- Isolated bug fixes.
- Small spacing, positioning, color, tuning, or placeholder-art changes.
- Temporary experiments that have not been accepted as direction.
- Every pull request or feature implementation milestone.
- Ideas the user has not explicitly approved.

## Authority and Maintenance

- The user must approve the underlying product decision before it is recorded as accepted direction.
- Update this log when an approved direction is introduced, revised, superseded, or retired.
- Preserve old entries as historical records. A later entry should name the decision it supersedes instead of rewriting history.
- Link to the current design document whenever one exists. The design document remains authoritative for current behavior.
- Link to relevant pull requests when they help identify when the direction entered the product, but do not treat implementation alone as proof of product intent.
- Keep entries concise and focused on the decision and its consequences. Detailed implementation plans belong in technical documentation.

## Entry Format

```markdown
## YYYY-MM-DD — Decision title

**Status:** Accepted | Superseded | Retired
**Areas:** Player Journey, Academy, Battles, Decks, Progression, UI, etc.

### Decision

State the approved direction in direct language.

### Context

Explain the problem, previous direction, or product pressure that led to the decision.

### Consequences

- List the important constraints or follow-up implications.
- Focus on effects across features and future work, not file-level implementation.

### Supersedes

Link to an earlier direction-log entry when applicable, or write `None`.

### References

- Current design document
- Relevant tracking task
- Relevant pull request or implementation milestone, when useful
```

## Decision History

Entries are newest first. Historical backfill should include only decisions that can be supported by explicit user direction or authoritative product/design documentation.

## 2026-08-14 — Design elemental summons creature-first

**Status:** Accepted
**Areas:** Cards, Summons, Elements, Progression, Content Production

### Decision

Begin elemental summon design with the creature's identity and fantasy, then derive its battlefield behavior, abilities, stats, upgrades, and visual direction. Do not use detached ability ideas as the default starting point for filling the elemental roster.

### Context

Ability-first ideation produced mechanics without always producing memorable, coherent creatures. The intended content process should make the creature itself the source of its gameplay identity.

### Consequences

- Elemental roster work requires manually planned creature concepts and lightweight visual exploration.
- Card-stat and upgrade-tree work should preserve the creature's identity rather than flattening it into generic balance packages.
- Mechanics can still inspire concepts, but they are not the default organizing principle.

### Supersedes

The ability-first working approach used during early elemental ideation; no prior direction-log entry.

### References

- `docs/tracking/completion-roadmap.md`
- `docs/design/fire-content-working-notes.md`
- `docs/design/water-content-working-notes.md`
- `docs/design/earth-content-working-notes.md`
- `docs/design/wind-content-working-notes.md`

## 2026-08-14 — Add cracked cards as risky normal-card variants

**Status:** Accepted
**Areas:** Cards, Decks, Progression, Quests, Online

### Decision

Add cracked cards as variations of normal cards with a meaningful twist or altered rule. A cracked variation can enable unusual synergies, but its change is risky and is not required to be beneficial.

### Context

Cracked cards create build possibilities through altered behavior rather than a straightforward power tier. For example, a spell might gain broader impact while also affecting allies, creating both a new opportunity and a new liability.

### Consequences

- Normal-card identity and balance must be coherent before cracked variants are broadly authored.
- Cracked-card behavior and risk must be understandable to the player.
- Acquisition, permanence, deckbuilding limits, balance rules, and the exact cracking process remain dedicated design work.
- A black market or underground source is a possible presentation, not an accepted location or acquisition model.

### Supersedes

None.

### References

- `docs/tracking/completion-roadmap.md`

## 2026-08-14 — Use quests to connect the expanded Academy experience

**Status:** Accepted
**Areas:** Player Journey, Academy, Quests, Maps, Characters, Progression, UI

### Decision

Use quests as connective structure across lessons, characters, locations, battles, rewards, shops, and discoveries. The bounded walkable campus should support experiences beyond static menu navigation while the overall journey still culminates in graduation and online PvP.

### Context

Recovering the bounded campus opened opportunities for a more lived-in Academy experience. Keeping lessons primarily as a static interface risks making the curriculum feel disconnected from the world and its characters.

### Consequences

- The current course-flow interface must be reevaluated rather than assumed to be the final primary experience.
- The physicality of the school, professor interactions, quest delivery, and the relationship between courses and quest chains require dedicated design work.
- Additional bounded locations are selected only after defining player engagement and evaluating production value and reuse.
- Features do not automatically require bespoke locations; a character, existing campus space, reusable room, or interface may be sufficient.
- The exact map roster and controllable-combat model remain unapproved until their roadmap initiatives conclude.

### Supersedes

The assumption that the Academy curriculum is experienced primarily through static course-selection and activity-flow interfaces; no prior direction-log entry.

### References

- `docs/design/walkable-academy-hub.md`
- `docs/design/academy-class-flow.md`
- `docs/tracking/completion-roadmap.md`
