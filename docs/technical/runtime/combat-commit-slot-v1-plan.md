# Combat Commit-Slot v1 Plan

**Status:** PASS 3 COMPLETE (Implementation + Tests), PR REVIEW READY  
**Initiative:** `combat-commit-slot-v1`  
**Domain:** `runtime`  
**Last Updated:** `2026-03-11`  
**Owner:** `Gameplay Simulation`

## Summary

Create a decision-complete refactor package before code mutation for the combat commit-slot rewrite. This plan aligns naming, folder placement, interfaces, invariants, and deterministic order to the current simulation architecture. The objective is to remove movement churn and low-commit attack behavior by replacing current combat-time behavior arbitration with target lock, slot-based melee contact, and phase-anchored attack execution.

## Goals

1. Define exact repo-native system naming and file locations for the rewrite.
2. Lock deterministic runtime invariants before implementation.
3. Prevent scope loss with a single authoritative checklist.
4. Provide PASS 1 validation mapping for all baseline combat outcomes.
5. Prepare PASS 2/3 handoff without ambiguity.

## Non-Goals

1. Implement runtime behavior changes in PASS 1.
2. Perform unit balance retuning across the roster.
3. Rewrite projectile/hit-shape systems already considered correct.
4. Merge unrelated simulation architecture changes.

## Repository-Native System Naming

| External Design Name | Repo Canonical Name | Planned File | Namespace |
|---|---|---|---|
| CombatStateMachine | `SimCombatStateMachine` | `scripts/csharp/Battle/Simulation/Combat/Lifecycle/SimCombatStateMachine.cs` | `Fateforged.Simulation.Combat` |
| TargetingSystem | `SimTargeting` (rewrite in place) | `scripts/csharp/Battle/Simulation/Combat/Targeting/SimTargeting.cs` | `Fateforged.Simulation.Combat` |
| SlotManager | `SimMeleeSlotManager` | `scripts/csharp/Battle/Simulation/Combat/Slots/SimMeleeSlotManager.cs` | `Fateforged.Simulation.Combat.Slots` |
| MovementSystem | `SimMovement` (simplified rewrite in place) | `scripts/csharp/Battle/Simulation/Movement/SimMovement.cs` | `Fateforged.Simulation.Movement` |
| AttackSystem | `SimAttackLoop` | `scripts/csharp/Battle/Simulation/Combat/Attack/SimAttackLoop.cs` | `Fateforged.Simulation.Combat` |
| OverlapResolver | `SimOverlapResolver` | `scripts/csharp/Battle/Simulation/Movement/Collision/SimOverlapResolver.cs` | `Fateforged.Simulation.Movement` |

## Folder Placement Rules

1. New combat orchestration and slot logic goes under `scripts/csharp/Battle/Simulation/Combat/` with subsystem folders (`Lifecycle`, `Attack`, `Targeting`, `Slots`).
2. Slot-specific files go under `scripts/csharp/Battle/Simulation/Combat/Slots/`.
3. Movement and overlap resolution remain under `scripts/csharp/Battle/Simulation/Movement/` with overlap in `Movement/Collision/`.
4. Runtime per-unit/per-target state lives under `scripts/csharp/Battle/Simulation/Data/`.
5. New enums go under `scripts/csharp/Battle/Simulation/Enums/`.
6. Runtime docs for this initiative remain under `docs/technical/runtime/`.
7. Simulation tests remain under `tests/csharp/Simulation/`.

## Architecture Decisions

1. Commit-first targeting lifecycle is authoritative.
2. Melee target contact capacity is controlled by target-owned slots.
3. Retarget is allowed only for invalid target, forced override, or unreachable timeout.
4. Summoner is always a valid target candidate in acquire/reacquire.
5. Attack phases (`Windup`, `Active`, `Recovery`) are translation-locked.
6. Combat-time movement is simplified to destination-following (`MoveToSlot`, `AdvanceObjective`, `Idle`).
7. Deep-overlap correction is retained; light overlap is allowed.
8. Deterministic simulation order is explicit and centralized in `Simulation.cs`.

## Public API / Interface / Type Changes

1. Add `SimCombatStateMachine`, `SimAttackLoop`, `SimMeleeSlotManager`, and `SimOverlapResolver`.
2. Add runtime state in `UnitData` for target commitment, slot reservation/occupancy, attack phase, and progress tracking.
3. Add target-owned slot state container in `Data/`, keyed by target id (including summoner ids).
4. Add enums: `CombatLifecycleState`, `AttackPhase`, `RetargetReason`, `SlotOccupancyState`.
5. Keep compile compatibility with existing enums/types during PASS 2 transition.

## Legacy Removal Scope

1. Disable periodic lock-based opportunistic retarget flow in `SimBehavior`.
2. Remove blocked-navigation yield/escape behavior for slot-committed melee paths.
3. Remove combat-time context steering influence for slot-committed melee paths.
4. Remove same-target close-combat ORCA special-casing once slot flow is authoritative.
5. Remove temporary compatibility shims after PASS 2 so commit-slot flow is authoritative.

## Invariants (Must Hold)

1. Retarget only on invalid target, forced override, or unreachable timeout.
2. Summoner always remains a valid candidate in acquire/reacquire scoring.
3. Nearby enemy spawns never trigger opportunistic retarget by themselves.
4. Reserved slot cannot be stolen unless owner releases or becomes invalid.
5. Slot change on same target allowed only if slot invalid or same-target progress timeout exceeded.
6. No translation during `Windup`, `Active`, or `Recovery`.
7. Damage shapes remain attacker-local authored at active window.
8. Deep-overlap correction executes only above penetration threshold.

## Slot Geometry Rules

1. Use target-local slot offsets with world reconstruction: `targetPosition + rotatedOffset`.
2. Use a world-stable layout axis with hysteresis refresh.
3. Axis refresh triggers only on `angle > 30 deg` or `target displacement > 0.5 * targetRadius`.
4. Default slot distribution: `60% front / 30% side / 10% rear`.
5. Slot count uses circumference scaling with `minSlots = 3`.
6. Default timeout constants: `slotWaitTimeout = 0.7s`, `unreachableTimeout = 1.2s`.

## Deterministic Update Order

1. Resolve deaths/despawns.
2. Release slots for dead/invalid entities.
3. Target reacquire pass.
4. Slot reservation/occupancy updates.
5. Movement toward slot/objective.
6. Attack phase updates.
7. Damage resolution.
8. Deep-overlap correction.

## Master Refactor Checklist (Single Source Of Truth)

### A) Artifact and Scope Lock
- [x] `DOC-001` Create plan artifact at `docs/technical/runtime/combat-commit-slot-v1-plan.md`.
- [x] `DOC-002` Create validation matrix at `docs/technical/runtime/combat-commit-slot-v1-validation-cases.md`.
- [x] `DOC-003` Create stub checklist at `docs/technical/runtime/combat-commit-slot-v1-stub-checklist.md`.
- [x] `DOC-004` Freeze initiative constants and invariants in plan doc before code work.
- [x] `DOC-005` Record pass-gate status block in all 3 artifacts.

### B) Runtime Data Model
- [x] `DATA-001` Add per-unit commit fields in `UnitData`: locked target, retarget reason, unreachable timer.
- [x] `DATA-002` Add per-unit slot fields in `UnitData`: reserved slot id, occupied slot id, slot target id, slot wait timer.
- [x] `DATA-003` Add per-unit phase fields in `UnitData`: attack phase enum, phase timer, phase lock target.
- [x] `DATA-004` Add per-unit progress fields: last distance to slot/target, no-progress timer.
- [x] `DATA-005` Add target-owned slot state container under `Data/` (keyed by target id, including summoner target ids).
- [x] `DATA-006` Add deterministic tie-break metadata for slot reservation conflicts (distance then unit id).
- [x] `DATA-007` Add cleanup-safe slot release state for death/despawn ordering.

### C) Enum/Contract Additions
- [x] `ENUM-001` Add `CombatLifecycleState` enum.
- [x] `ENUM-002` Add `AttackPhase` enum (`Windup`, `Active`, `Recovery`).
- [x] `ENUM-003` Add `RetargetReason` enum (`Invalid`, `ForcedOverride`, `UnreachableTimeout`).
- [x] `ENUM-004` Add `SlotOccupancyState` enum (`Free`, `Reserved`, `Occupied`).
- [x] `ENUM-005` Keep legacy enums compile-compatible during PASS 2 transition (then remove legacy mode usage in PASS 3).

### D) System Wiring (File-Level Ownership)
- [x] `SYS-001` Wire `Simulation.TickUnits` to call `SimCombatStateMachine` as the unit combat orchestrator.
- [x] `SYS-002` Move acquire/reacquire policy ownership to rewritten `SimTargeting`.
- [x] `SYS-003` Implement `SimMeleeSlotManager` for reserve/occupy/release and slot world-position queries.
- [x] `SYS-004` Implement `SimAttackLoop` for phase progression and attack-trigger timing.
- [x] `SYS-005` Simplify `SimMovement` to only `MoveToSlot`, `AdvanceObjective`, `Idle` for commit-slot melee paths.
- [x] `SYS-006` Implement `SimOverlapResolver` deep-overlap-only correction.
- [x] `SYS-007` Keep deterministic update order centralized in `Simulation.cs`.

### E) Invariants (Must Hold)
- [x] `INV-001` Retarget only on invalid target, forced override, or unreachable timeout.
- [x] `INV-002` Summoner always remains a valid candidate in acquire/reacquire scoring.
- [x] `INV-003` Nearby enemy spawns never trigger opportunistic retarget by themselves.
- [x] `INV-004` Reserved slot cannot be stolen unless owner releases or becomes invalid.
- [x] `INV-005` Slot change on same target allowed only if slot invalid or same-target progress timeout exceeded.
- [x] `INV-006` No translation during `Windup`, `Active`, or `Recovery`.
- [x] `INV-007` Damage shapes remain attacker-local authored at active window.
- [x] `INV-008` Deep-overlap correction executes only above penetration threshold.

### F) Slot Geometry Rules
- [x] `SLOT-001` Use target-local slot offsets with world position reconstruction (`targetPosition + rotatedOffset`).
- [x] `SLOT-002` Use world-stable layout axis with hysteresis refresh.
- [x] `SLOT-003` Axis refresh triggers only on `angle > 30 deg` or `target displacement > 0.5 * targetRadius`.
- [x] `SLOT-004` Default slot distribution `60% front / 30% side / 10% rear`.
- [x] `SLOT-005` Slot count uses circumference scaling with `minSlots = 3` default.
- [x] `SLOT-006` Defaults: `slotWaitTimeout = 0.7s`, `unreachableTimeout = 1.2s`.

### G) Deterministic Update Order
- [x] `ORDER-001` Step 1 resolve deaths/despawns.
- [x] `ORDER-002` Step 2 release slots for dead/invalid entities.
- [x] `ORDER-003` Step 3 target reacquire pass.
- [x] `ORDER-004` Step 4 slot reservation/occupancy updates.
- [x] `ORDER-005` Step 5 movement toward slot/objective.
- [x] `ORDER-006` Step 6 attack phase updates.
- [x] `ORDER-007` Step 7 damage resolution.
- [x] `ORDER-008` Step 8 deep-overlap correction.

### H) Legacy Path Retirement (Controlled)
- [x] `LEG-001` Disable periodic lock-based opportunistic retarget flow in `SimBehavior`.
- [x] `LEG-002` Remove combat-time blocked-navigation yield/escape logic for slot-committed melee paths.
- [x] `LEG-003` Remove combat-time context steering influence for slot-committed melee paths.
- [x] `LEG-004` Remove same-target close-combat ORCA special-casing once slot flow is authoritative.
- [x] `LEG-005` Remove temporary compatibility shims carried during PASS 2.

### I) Test Coverage and Telemetry
- [x] `TEST-001` Add No-Churn Commit scenario test.
- [x] `TEST-002` Add Summoner Persistence scenario test.
- [x] `TEST-003` Add 15v1 Overflow slot contention test.
- [x] `TEST-004` Add Reserved Slot Anti-Steal test.
- [x] `TEST-005` Add No-Translation During Attack-Phase test.
- [x] `TEST-006` Add Slot Release Before Reacquire death edge test.
- [x] `TEST-007` Add determinism replay seed parity tests.
- [x] `TEST-008` Add telemetry counters: switches, blocked timeout triggers, `windupsStarted`, `windupsCancelled`.

## Pass Acceptance Criteria

### PASS 1: USE CASES + VALIDATION

1. Required artifacts exist in `docs/technical/runtime/` and are decision-complete.
2. Naming, folder placement, invariants, and deterministic order are locked.
3. Validation matrix includes explicit test mapping and status.
4. Master checklist is present and treated as single source of truth.

### PASS 2: STUBS + WIRING

1. Compile-safe scaffolding exists for systems, enums, and data fields. (Complete)
2. `Simulation.cs` is wired to new orchestration seams without final behavior changes. (Complete)
3. Stub checklist sections are completed with implementation deltas. (Complete)

### PASS 3: IMPLEMENTATION + TESTS

1. Commit-slot behavior is implemented as the default lifecycle mode and validated by simulation tests. (Complete)
2. All baseline validation cases are `Implemented`. (Complete)
3. Determinism and stability scenarios pass. (Complete)

### PR REVIEW: READY

1. Required artifact set exists and pass order is preserved.
2. Review confirms no phase-gate violations and acceptance criteria evidence is present.

## Open Risks

1. Slot axis refresh thresholds may require tuning after live stress tests.
2. Timeout tuning (`slotWaitTimeout`, `unreachableTimeout`) may differ by unit class later.
3. Summoner-orbit congestion scoring may need per-map tuning once wider playtests run.

## Assumptions and Defaults

1. Initiative slug is `combat-commit-slot-v1`.
2. Domain remains `runtime`.
3. Commit-slot behavior is implemented and validated as the default runtime combat flow.
4. Existing projectile/hit-shape deterministic machinery is reused.
5. Pass-gated workflow is mandatory.

## Pass Gate Status

Current state:
1. `PASS 1: USE CASES + VALIDATION` complete.
2. `PASS 2: STUBS + WIRING` complete.
3. `PASS 3: IMPLEMENTATION + TESTS` complete.
4. `PR REVIEW: READY` not started.

Gate note:
1. Use explicit approval text to advance.
2. Next step: run `PR REVIEW: READY`.
