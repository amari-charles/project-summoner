# Documentation Usefulness Audit (2026-03-04)

> Follow-up (2026-03-04): `docs/multiplayer/` was consolidated to a single canonical architecture doc (`architecture.md`). Legacy phase/research docs were removed.

## Scope

Audit goal: decide which docs are still useful for the current architecture, which should be updated, and which should be archived before further doc rewrites.

## Snapshot

- Markdown files under `docs/`: `210`
- Markdown files excluding `docs/archive/`: `202`
- Largest content buckets:
  - `elements/`: `77` docs
  - `technical/`: `28` docs
  - `architecture/`: `27` docs
  - `features/`: `19` docs

## High-Risk Drift Signals

Files with the highest density of legacy architecture terms (`HostRunner`, `ClientRunner`, `StateSnapshotBuilder`, `RequestValidator`, etc.):

1. `docs/technical/simulation-reference.md` (34 hits)
2. `docs/multiplayer/implementation-phases.md` (25 hits)
3. `docs/multiplayer/architecture.md` (22 hits)
4. `docs/migration/architectural-issues.md` (20 hits)
5. `docs/migration/deletion-sequence.md` (11 hits)
6. `docs/technical/simulation-walkthrough.md` (7 hits)

Interpretation: these are most likely to conflict with the current `HostSession/ClientSession` model.

## Broken/Confusing Link Findings

Known missing links in onboarding/index surfaces:

- `docs/start-here.md -> art/asset-specifications.md` (missing target file)

Known missing links in non-entry docs (mostly relative-path mistakes in element/lore subtrees):

- `docs/elements/*` and `docs/lore/characters/fateforgers/*` have several cross-links to non-existent `elements/*.md` roots.
- `docs/project/brief.md -> ../current-state.md` (wrong relative path from `docs/project/`).

## Usefulness Classification (Proposed)

### Tier 1 — Canonical (keep in active nav, maintain aggressively)

- `docs/README.md`
- `docs/project/current-state.md`
- `docs/architecture/target-architecture.md`
- `docs/architecture/gameplay/**` (except clearly marked stale specs)
- `docs/features/**`
- `docs/workflows/**`

### Tier 2 — Active but Drift-Prone (keep, but mark status + owner)

- `docs/technical/simulation-architecture.md`
- `docs/technical/simulation-walkthrough.md`
- `docs/technical/simulation-reference.md`
- `docs/technical/projectile-system.md`
- `docs/multiplayer/**`
- `docs/migration/**`

Requirement for Tier 2: each file should carry a current status header and a maintainer/owner.

### Tier 3 — Historical/Execution Logs (archive candidates, not deletion candidates)

- `docs/multiplayer/implementation-phases.md` (phase-log style, class names now stale)
- Migration execution records once their tasks are fully complete (`deletion-sequence`, old issue logs)
- Any doc whose primary value is "how we migrated" rather than "how the code works now"

## Suggested Next Actions (Safe Sequence)

1. Freeze nav: define a minimal "active docs" set in `docs/README.md` (Tier 1 only).
2. Mark drift-prone docs with status banners (`CURRENT`, `REVIEW NEEDED`, `HISTORICAL`).
3. Fix broken links in entry docs first (`docs/README.md`, `docs/start-here.md`).
4. Move Tier 3 docs to `docs/archive/` (with short "superseded by" notes), not hard-delete.
5. Rewrite Tier 2 docs only after archive boundaries are set.

## Open Decisions Needed (Resolved)

Resolved by the 2026-03-04 multiplayer docs consolidation pass.

1. Should `docs/multiplayer/implementation-phases.md` remain in active nav, or move to archive immediately?
2. Should `docs/technical/simulation-reference.md` be rewritten in place, or replaced by a slimmer "current runtime reference" doc?
3. Should `docs/start-here.md` be trimmed to onboarding-only and moved design/tutorial content elsewhere?
