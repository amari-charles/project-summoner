# Approval-Gated Delivery Workflow

**Status:** CURRENT  
**Last Updated:** 2026-03-19  
**Owner:** Engineering Workflow

## Purpose

Use this workflow for medium and large initiatives so design intent, scaffolding, implementation, and review are separated into explicit approval gates.

This document is the canonical process contract for both the Codex skill (`$approval-gated-delivery`) and the Claude command (`/approval-gated-delivery`). Keep those entrypoints aligned with this workflow.

## When To Use

Apply this workflow when a task is any of the following:
1. Multi-file feature work.
2. Medium/large refactor across architecture layers.
3. Behavior changes that affect runtime contracts, simulation, networking, save/profile shape, or shared APIs.
4. Changes where correctness depends on clear scenario coverage.

Skip is allowed for small local fixes (single-file, low-risk, no cross-layer contract impact).

## Required Phases (Hard Stop Gates)

Every initiative must move in this order:
1. `PASS 1: USE CASES + VALIDATION`
2. `PASS 2: STUBS + WIRING`
3. `PASS 3: IMPLEMENTATION + TESTS`
4. `PR REVIEW: READY`

Approval protocol:
1. Pass 1 output must end with an explicit request for Pass 2 approval.
2. Pass 2 output must end with an explicit request for Pass 3 approval.
3. Only explicit approval text advances to the next pass.
4. Implied approval does not advance the pass.
5. If not approved, state: `blocked waiting approval`.

## Required Artifacts Per Initiative

Create or update these files under `docs/technical/<domain>/`:
1. `<initiative>-plan.md`
2. `<initiative>-validation-cases.md`
3. `<initiative>-stub-checklist.md` (created or expanded in Pass 2)

Use templates from `docs/workflows/templates/`.
1. `initiative-plan-template.md`
2. `initiative-validation-cases-template.md`
3. `initiative-stub-checklist-template.md`

## Naming and Content Conventions

Plan doc must define:
1. Goals and non-goals.
2. Decision-complete architecture and interface changes.
3. Acceptance criteria for each pass.
4. Open risks and explicit assumptions/defaults.

Validation cases doc must define:
1. Case IDs with stable naming.
2. Expected behavior.
3. Test type and target test file.
4. Status: `Design-Covered`, `Implemented`, or `Deferred`.

Stub checklist doc must define:
1. Types/interfaces introduced.
2. Wiring points changed.
3. Legacy path removals/disables.
4. Compile-safe deterministic stub behavior.
5. Test skeleton coverage map against case IDs.

## Architecture Placement Principle

When implementing PASS 3 behavior, optimize for architecture correctness first, not proximity to existing files.

Required placement rule:
1. If a concern becomes cross-cutting or duplicated (for example lifetime adaptation, area-resolution math, shared stat-materialization rules), introduce a dedicated simulation node/module for that concern.
2. Do not keep duplicated helper logic in multiple subsystems just because those files already exist.
3. Document each new node in the initiative plan under architecture/interface changes so future passes reuse it.

## What "Stubs" Means In This Codebase

Pass 2 stubs are not placeholder comments only. They must:
1. Compile and run safely.
2. Be deterministic in runtime-sensitive paths.
3. Expose final interface shapes (or approved near-final equivalents).
4. Remove or disconnect conflicting legacy paths.
5. Include test skeletons and explicit TODOs for remaining Pass 3 logic.

## Test Scenario Requirements

Each initiative must include scenario coverage planning and mapping:
1. Unit tests for local deterministic logic.
2. Integration/simulation tests for runtime behavior.
3. Determinism/replay checks where applicable (simulation/networked systems).

Pass 3 acceptance requires:
1. All baseline cases marked `Implemented` or `Deferred`.
2. Any `Deferred` case includes rationale and follow-up target.
3. Test outputs summarized in final implementation report.

## PR Review Requirements

PR review must explicitly validate pass-gate compliance for medium/large changes:
1. Required artifacts exist.
2. Pass states are present and in order.
3. Validation scenarios include test mapping and status.
4. No implementation phase happened before Pass 2 approval evidence.

If any item fails, PR review outcome is `not ready`.

## Workflow Validation Scenarios

Use these checks when introducing or updating this workflow:
1. Skill discovery: new skill appears in available skill list.
2. Trigger matching: phrases like `approval gate` or `use cases then stubs then implementation` select this workflow.
3. Gate enforcement: execution stops after Pass 1 and Pass 2 without explicit approval.
4. Artifact compliance: missing plan/validation/stub artifacts are flagged.
5. PR-review integration: review output includes pass-gate compliance and marks non-compliant changes not ready.
6. Documentation discoverability: `docs/README.md` and `docs/start-here.md` link this workflow doc.

## Example End-To-End Lifecycle

1. Draft `initiative-plan.md` and `initiative-validation-cases.md`.
2. Request approval and stop (`blocked waiting approval`).
3. After approval, create/update `initiative-stub-checklist.md`, add stubs and test skeletons.
4. Request approval and stop (`blocked waiting approval`).
5. After approval, implement full behavior and complete tests.
6. Run PR review and report pass-gate compliance + findings.
