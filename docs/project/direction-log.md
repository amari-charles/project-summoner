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
