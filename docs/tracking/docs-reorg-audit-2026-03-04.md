# Docs Reorganization Audit (2026-03-04)

## Scope

Audit and reorganize `docs/` for better discoverability, lower drift, and clearer active-vs-historical boundaries.

## Snapshot

- Markdown docs under `docs/`: `211`
- Largest folders by volume:
  - `elements/` (`77`)
  - `architecture/` (`30`)
  - `technical/` (`28`)
  - `features/` (`19`)

## Problems Found

1. Active navigation mixed current docs with historical migration execution logs.
2. Legacy technical references still looked canonical despite stale architecture terms.
3. Broken links in onboarding/reference docs reduced trust in index pages.
4. No single doc describing folder-level organization intent for active vs historical docs.

## Proposed Organization (Target State)

Keep top-level structure but enforce role clarity:

1. `docs/architecture/` — canonical architecture model and boundaries.
2. `docs/features/` — game/system specs (product/design behavior).
3. `docs/technical/` — implementation references that are current and runnable.
4. `docs/workflows/` + `docs/tracking/` — process and live project state.
5. `docs/archive/` — historical migration plans, audits, and superseded deep references.

Rule: if a doc primarily describes *how we migrated* (not *how the code works now*), archive it.

## Archived In This Pass

Moved to `docs/archive/doc-reorg-2026-03/`:

- `migration/architectural-issues.md`
- `migration/architecture-task-plan.md`
- `migration/cross-cutting-plan.md`
- `migration/deletion-sequence.md`
- `migration/implementation-checklist.md`
- `migration/layer-map.md`
- `migration/meta-game-plan.md`
- `migration/planning-checklist.md`
- `technical/simulation-reference.md`
- `technical/simulation-walkthrough.md`
- `technical/architecture.md`
- `technical/card-definition-refactor.md`
- `technical/trait-definition-refactor.md`
- `technical/unit-definition-refactor.md`
- `tracking/todos-completed.md`
- `architecture/refactor-audit-2026-01-25-campaign-graph.md`
- `architecture/refactor-audit-2026-01-30-typed-event-data.md`

## Active Docs Updated

- Updated active indexes and architecture pages to point at canonical current docs.
- Updated migration hub to point archived execution docs instead of keeping them in active root.
- Updated technical simulation architecture “See Also” to avoid stale active references.
- Updated onboarding and workflow docs for new archive locations.

## Broken-Link Follow-Up

Completed in this pass: non-archive markdown links now resolve with zero broken links.

## Next Cleanup Phase (Recommended)

1. Add a lightweight status banner to all docs in `technical/` and `features/` (`CURRENT`, `REFERENCE`, `HISTORICAL`).
2. Split very large tracker docs (`tracking/todos.md`) into domain-specific todo files.
3. Add "canonical owner" metadata to major docs to reduce drift.
4. Continue archiving superseded one-off research docs as architecture stabilizes.
