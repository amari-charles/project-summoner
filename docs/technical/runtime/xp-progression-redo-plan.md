# XP Progression System Redo Plan (Cards + Summoners)

**Status:** Historical shared-core implementation record; manual application flow superseded
**Initiative:** `xp-progression-redo`
**Domain:** `runtime`
**Last Updated:** `2026-03-09`
**Owner:** Gameplay Systems

## Summary

Refactor card and summoner progression into one deterministic shared progression core while keeping gameplay behavior stable.

> **Current application contract (2026-08-22):** The shared deterministic core
> remains authoritative, but manual level-up calls and `can_level_up` UI state
> have been retired. Granting XP now repeatedly applies every affordable level,
> carries remaining XP, banks owner-bound development points, and emits each
> level gained. See
> [Discovery-Driven Development](../../design/discovery-driven-development.md).

Current code duplicates progression math in two places:
1. Card math in `CardProgressionHandler` (`scripts/csharp/Meta/Services/Cards/Handlers/CardProgressionHandler.cs`)
2. Summoner math in `SummonerProgressionService` (`scripts/csharp/Meta/Services/Summoner/SummonerProgressionService.cs`)

Both currently model stored XP as XP carried toward next level (non-cumulative), and both consume XP on level-up with carryover. This initiative preserves that behavior, removes duplicated math, and makes XP semantics explicit in naming/contracts.

## Goals

1. Introduce one shared progression core (pure deterministic logic) used by cards and summoners.
2. Keep service layers thin: orchestration only (repo access, signals, boundary conversion).
3. Make XP semantics explicit and consistent across card/summoner code and UI contracts.
4. Preserve existing gameplay behavior (XP spend, carryover, trait-point grant per successful level-up).
5. Add/align tests and docs for deterministic parity and API contract stability.

## Non-Goals

1. Trait system redesign.
2. Save migration compatibility adapters.
3. UI redesign beyond required field contract consistency.

## Locked XP Semantics (Pass 1)

1. `xp` (stored) means: current XP banked toward the *next* level cost, not lifetime cumulative XP.
2. `xp_for_next_level` means: current level-up cost after curve/policy evaluation.
3. `xp_to_next_level` means: `max(0, xp_for_next_level - xp)`.
4. `xp_progress` means: normalized `[0..1]` value `xp / xp_for_next_level` (clamped).
5. Level-up behavior:
1. Success requires `xp >= xp_for_next_level` and `level < max_level`.
2. XP spend subtracts exactly `xp_for_next_level`.
3. Leftover XP is retained (carryover), allowing multi-level progression with repeated level-up calls.
6. Trait points:
1. Exactly +1 unspent trait point per successful level-up.
2. No trait point grant on failed/no-op level-up attempts.

## Architecture Decisions

1. Add shared core namespace (`scripts/csharp/Meta/Progression/Core/`):
1. `ProgressionState` (level, xp_toward_next, max_level)
2. `ProgressionCurve` policy contract (cost resolver)
3. `ProgressionResult`/metadata for level-up application
4. `ProgressionEngine` pure static/service methods:
1. `GetXpCostForNextLevel`
2. `CanLevelUp`
3. `GetProgress01`
4. `ApplyLevelUp`
2. Card and summoner services become adapters:
1. Build `ProgressionState` from profile entity
2. Call shared engine with domain-specific curve/policy
3. Persist returned state
4. Emit existing signals and return existing public payload shape
3. Domain curve policies remain independent inputs:
1. Card: existing threshold + rarity scaling behavior
2. Summoner: existing summoner threshold behavior
4. Shared core remains deterministic and side-effect free.

## Public API / Interface / Type Changes (Target)

1. Internal C# progression internals will use explicit names (`xp_toward_next` semantics via type fields) to remove ambiguity around cumulative vs banked XP.
2. Historical UI payload keys for the manual flow were:
1. `xp`
2. `xp_for_next_level`
3. `xp_progress`
4. `can_level_up`
3. The current automatic flow exposes `xp`, `xp_for_next_level`, and
   `xp_progress`; it no longer exposes `can_level_up` or a manual mutation API.
4. Optional internal cleanup:
1. deprecate/rename misleading `GetXpForLevel` comments that imply cumulative-only semantics when used as per-level delta source.

## Legacy Removal Scope

1. Remove duplicated card/summoner level-up math implementations from service-specific paths.
2. Keep only domain-specific curve definitions in adapters; move shared arithmetic/guards to core engine.
3. Eliminate any drift-prone local formulas for `xp_to_next_level`, `can_level_up`, and progress normalization.

## Pass Acceptance Criteria

### PASS 1: USE CASES + VALIDATION

1. Plan and validation docs exist in `docs/technical/runtime/`.
2. XP semantics are locked and explicit.
3. Required baseline cases are defined and mapped to planned tests.
4. Pass gate enforced: no stubs/implementation before approval.

### PASS 2: STUBS + WIRING

1. Shared progression core types/interfaces exist and compile.
2. Card/summoner services are wired to shared core entrypoints (stub-safe deterministic behavior).
3. Duplicate arithmetic paths are removed/disabled.
4. Stub checklist artifact exists and maps all baseline cases to test skeletons.

### PASS 3: IMPLEMENTATION + TESTS

1. Shared core fully implements level-up, carryover, progress, and deterministic guards.
2. Card/summoner adapters persist and signal correctly via shared core outcomes.
3. Required validation scenarios pass or are explicitly deferred with rationale.
4. Trait point grant behavior remains once-per-successful-level-up.

### PR REVIEW: READY

1. Pass-gate compliance is explicit and complete.
2. Shared core usage by both domains is verified.
3. No duplicated level-up math remains in adapters.
4. Test evidence covers required scenarios and determinism parity.

## Open Risks

1. Naming migration risk: internal renames may accidentally alter payload expectations if adapter mapping is incomplete.
2. Curve-policy wiring risk: card rarity scaling could be lost if adapter does not pass rarity context to policy.
3. Max-level edge behavior must stay consistent (`xp_progress=1`, `xp_for_next_level=0`, no level-up).
4. Existing tests may encode current behavior that is underspecified (comments mention cumulative thresholds while runtime stores banked XP).

## Assumptions and Defaults

1. Stored XP remains banked/non-cumulative for this initiative.
2. Public behavior remains stable unless explicitly documented and approved.
3. No compatibility shims for old internal types are required.
4. Determinism is required for all progression computations.

## Pass Gate Status

Current state:
1. `PASS 1: USE CASES + VALIDATION` complete
2. `PASS 2: STUBS + WIRING` complete
3. `PASS 3: IMPLEMENTATION + TESTS` complete
4. `PR REVIEW: READY` complete

Gate note:
1. Use explicit approval text to advance.
2. If waiting, state: `blocked waiting approval`.
