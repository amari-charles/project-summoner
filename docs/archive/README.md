# Archived Documentation

This folder contains outdated documentation that has been superseded by newer docs. These files are kept for historical reference only.

**Do not use these docs for current development.** Always refer to the main documentation at [docs/README.md](../README.md).

---

## Archived Files

| File | Superseded By | Archived Date |
|------|---------------|---------------|
| [summoner-and-nexus.md](summoner-and-nexus.md) | [features/summoners/README.md](../features/summoners/README.md) | 2026-01-19 |
| [integration-status.md](integration-status.md) | [migration/README.md](../migration/README.md) — layered architecture migration hub | 2026-03-02 |
| [transformation-roadmap.md](transformation-roadmap.md) | [migration/README.md](../migration/README.md) — all 7 phases complete/cancelled (Jan 2026) | 2026-03-02 |

## Archived Folders

### `rewrite-research-2026-02/` — February 2026

Planning and research documents from the host-authoritative multiplayer simulation rewrite. All 8 phases of the rewrite are complete and the branch (`feature/host-authoritative-sim`) has been merged.

**Contents** (4 files):
- `problem-analysis.md` — Historical context on why the previous `feature/match-state-simulation` branch failed (36 external mutation sites, dual-authority bug, unwired command queue)
- `architecture-decisions.md` — The 10 architecture decisions and 6 invariants that governed the rewrite; now reflected in the live codebase
- `ai-implementation-guide.md` — Session handoff protocol and no-go rules used during AI-assisted implementation; no longer needed
- `implementation-plan.md` — 8-phase implementation plan (Pre-Work through Phase 8); all phases complete as of 2026-02-27

**Permanent docs created from this research:**
- `docs/architecture/game-requirements.md` — Comprehensive gameplay requirements spec (from `requirements.md`)
- `docs/technical/simulation-reference.md` — Mermaid system diagrams, full data structure reference for MatchState, UnitData, all SimEvents, all Commands, all enums, and all network messages (from `architecture-diagram.md`)
- `docs/technical/simulation-walkthrough.md` — Human-readable gameplay flows for match init, card play, unit death, multiplayer, and all other key flows (from `architecture-walkthrough.md`)

---

## Why Archive Instead of Delete?

- Historical context for design decisions
- Understanding how systems evolved
- Reference for migration discussions
