# Ability System V1 Plan

**Status:** PR REVIEW READY
**Initiative:** `ability-system-v1`
**Domain:** `runtime`
**Last Updated:** 2026-03-12
**Owner:** Codex

## Summary
Ability System V1 introduces a simulation-owned runtime for non-basic unit abilities without rewriting the combat core. The implementation adds deterministic ability ticking, per-unit ability runtime state, projectile impact payload extensions (ally affinity, heal impacts, on-hit status), and new ability/status events. This wave ships six abilities: rock artillery, healer bullets, healing field spell, taunt pulse guardian, poison needles (stack potency DoT), and piercing laser. Follow-up abilities (flaming burn shot and cone cleaver polish) remain intentionally deferred.

## Goals
1. Add `SimAbilityOrchestrator` as a simulation sibling subsystem.
2. Add deterministic per-unit ability runtime state and spawn-path wiring.
3. Extend projectile impacts to support heal + status payloads with ally/enemy affinity.
4. Ship first six ability content entries and associated tests.

## Non-Goals
1. Full generalized ability editor/UI tooling.
2. Full burn-content rollout in this initiative.
3. Cone cleaver geometry polish beyond baseline existing melee paths.

## Architecture Decisions
1. Ability logic stays in simulation and writes authoritative `MatchState` only.
2. Projectile payload path is reused for poison/heal/laser behavior to avoid duplicate systems.
3. Taunt semantics use soft forced-target override (respect active foreign hard locks).
4. DoT reapplications stack potency up to a bounded max and refresh duration.

## Public API / Interface / Type Changes
1. Added unit ability authoring/runtime types: `UnitAbilityConfig`, `UnitAbilityState`, and supporting enums.
2. Extended ranged/ projectile runtime payload contract with affinity, impact kind, and status payload fields.
3. Added `SimEvent` types: `AbilityActivatedEvent`, `StatusAppliedEvent` and visitor support.
4. Added `SpellCategory.Heal` and spell-to-sim mapping support.

## Legacy Removal Scope
1. No legacy subsystem deletion in V1.
2. Existing `SimBehavior` auto-attack flow retained for non-ability units.
3. Existing spell damage path retained and expanded (no rewrite).

## Pass Acceptance Criteria

### PASS 1: USE CASES + VALIDATION
1. Six-ability scope and defaults agreed.
2. Validation matrix with mapped tests authored.

### PASS 2: STUBS + WIRING
1. Compile-safe ability/runtime type plumbing added through template->unit spawn path.
2. Tick-loop wiring and event contract scaffolding added.

### PASS 3: IMPLEMENTATION + TESTS
1. Ability runtime behavior and projectile payload behavior implemented.
2. Validation tests added and passing for implemented cases.

### PR REVIEW: READY
1. `$pr-review` pass still required.
2. Final ready-to-merge state depends on review findings.

## Open Risks
1. Healer targeting heuristics are intentionally simple (lowest HP% + distance tie-break) and may need design tuning.
2. Status stacking is global per status kind on target; future per-source policy may be needed.

## Assumptions and Defaults
1. Soft taunt never overrides an active foreign forced target.
2. Poison uses magic-typed periodic payload damage in current implementation.
3. Ability VFX routing is minimal in V1; simulation events are available for richer view work.

## Pass Gate Status
Current state:
1. `PASS 1: USE CASES + VALIDATION` - completed
2. `PASS 2: STUBS + WIRING` - completed
3. `PASS 3: IMPLEMENTATION + TESTS` - completed
4. `PR REVIEW: READY` - completed
