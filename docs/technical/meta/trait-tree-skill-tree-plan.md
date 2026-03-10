# Trait Tree Skill Tree Plan

**Status:** PASS 2 COMPLETE (Stubs + Wiring)  
**Initiative:** `trait-tree-skill-tree`  
**Domain:** `meta`  
**Last Updated:** `2026-03-10`  
**Owner:** `Meta UX / Progression`

## Summary

This initiative defines the approved architecture and validation scope for the new trait-tree-based progression flow. The target UX is: `Level Up` grants trait points only, and all spending happens in a dedicated trait tree surface. Card upgrades are card-instance scoped and must not be treated as collection-global progression. Summoner, card, and spell trees should use one shared tree UI/service pattern with C# progression validation as the authority. This pass locks decisions and validation coverage before additional wiring or behavior work.

## Goals

1. Make trait spending tree-based, icon-first, and confirmation-driven.
2. Enforce card-instance-specific upgrades (no collection-global card spending).
3. Keep spend validation and point mutation authoritative in C# services.
4. Reuse one trait tree UI/service pattern across card, summoner, and spell trees.
5. Keep one-off traits in a separate tab/surface from progression-tree unlocks.

## Non-Goals

1. Final icon art, final VFX polish, and animation polish.
2. Trait economy rebalance (point rates, costs, or reward cadence).
3. Backward-compatibility migrations for deprecated trait flow data shapes.

## Architecture Decisions

1. `Level Up` does not offer immediate random trait-choice cards; it only grants trait points.
2. `Traits` navigation opens the tree surface; spend availability is indicated on the trigger button badge.
3. Progression nodes use circular icon nodes with connector lines; name/description appear in hover/selection popup, not persistent inline text blocks.
4. Unlock interaction uses a confirmation modal; unlock controls are no longer right-side persistent controls.
5. Tree layout is bottom-up and branch spacing must avoid overlapping connector lines.
6. `TraitAcquisitionMode` controls routing (`level_up_offer` -> progression tree, `granted_only` -> one-off tab).
7. C# progression services enforce unlock eligibility, point availability, and trait ownership updates; GDScript UI only requests and renders.

## Public API / Interface / Type Changes

1. Card detail and collection navigation flows expose explicit route into card trait tree using `card_instance_id` and `card_catalog_id`.
2. Trait catalog bridge surfaces acquisition-mode-aware filtering for tree tabs.
3. Shared trait tree canvas/component interface is used by card and summoner tree screens (and spell tree when added).
4. Progression APIs expose: current points, owned traits, unlock eligibility state, and spend operation with rejection reason.
5. Unlock confirmation modal contract includes selected trait id, display metadata, affordance state, and confirm/cancel callbacks.

## Legacy Removal Scope

1. Remove/disable inline trait-offer list flow in card detail modal for progression spending.
2. Remove/disable legacy level-up “pick one of three” trait-upgrade flow where it conflicts with point-spend tree flow.
3. Remove/disable collection-global card-upgrade path for trait spending.
4. Remove duplicated tree rendering logic where shared tree component exists.

## Pass Acceptance Criteria

### PASS 1: USE CASES + VALIDATION

1. Plan + validation artifacts exist and cover card/summoner tree behavior, unlock gating, and confirmation flow.
2. All baseline scenarios map to test type and target file with `Design-Covered` status.

### PASS 2: STUBS + WIRING

1. Trait tree screens, shared tree component contracts, and C# service entrypoints are wired with compile-safe deterministic stubs.
2. Legacy conflicting pathways are disconnected for scoped surfaces, and test skeletons exist for all baseline cases.

### PASS 3: IMPLEMENTATION + TESTS

1. End-to-end card and summoner tree spend flows work with confirmation modal, unlock gating, and badge state updates.
2. Validation matrix scenarios are `Implemented` or explicitly `Deferred` with rationale and follow-up target.

### PR REVIEW: READY

1. Review confirms pass-gate compliance and no implementation-before-approval violations.
2. Review confirms card-instance scoping, shared tree reuse, and legacy-path removal coverage.

## Open Risks

1. Recent `main` updates touched overlapping modal/navigation files, increasing merge-conflict risk while trait tree changes continue.
2. Existing saved progression state may not match updated acquisition-mode and tree-shape assumptions.
3. Shared tree component adoption can regress one surface while fixing another if test coverage is incomplete.

## Assumptions and Defaults

1. No backward-compatibility requirement for deprecated trait-selection flow data/contracts.
2. Placeholder icons are acceptable during implementation as long as node shape/state is correct.
3. Clicking any trait node always opens trait details; unlock button is conditionally enabled/disabled in that popup.
4. If no unlock is currently possible, UI shows disabled CTA and explicit reason (for example insufficient points or unmet prerequisite).

## Pass Gate Status

Current state:
1. `PASS 1: USE CASES + VALIDATION` (complete)
2. `PASS 2: STUBS + WIRING` (complete)
3. `PASS 3: IMPLEMENTATION + TESTS` (not started)
4. `PR REVIEW: READY` (not started)

Gate note:
1. Use explicit approval text to advance to Pass 2.
2. If waiting, state: `blocked waiting approval`.
