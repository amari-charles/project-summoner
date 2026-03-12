# Combat Commit-Slot v1 Stub Checklist

**Status:** PASS 3 COMPLETE (Implementation + Tests), PR REVIEW READY  
**Initiative:** `combat-commit-slot-v1`  
**Domain:** `runtime`  
**Last Updated:** `2026-03-12`

## Purpose

Define PASS 2 stubs and wiring checkpoints ahead of implementation so no contracts, files, or tests are missed during the major refactor.

## Types Created (PASS 2)

- [x] `SimCombatStateMachine` - per-unit combat lifecycle orchestrator.
- [x] `SimAttackLoop` - attack phase progression and trigger processing.
- [x] `SimMeleeSlotManager` - target-owned slot reserve/occupy/release authority.
- [x] `SimOverlapResolver` - deep-overlap-only correction authority.
- [x] `TargetSlotState` (Data) - per-target slot array and occupancy metadata.
- [x] `MeleeSlotEntry` (Data) - slot id, offset, occupancy state, owner bindings.

## Interfaces / Contracts Created (PASS 2)

- [x] `CombatLifecycleState` enum.
- [x] `AttackPhase` enum (`Windup`, `Active`, `Recovery`).
- [x] `RetargetReason` enum (`Invalid`, `ForcedOverride`, `UnreachableTimeout`, `OutOfAggroRange`, `AggroPreempt`).
- [x] `SlotOccupancyState` enum (`Free`, `Reserved`, `Occupied`).
- [x] UnitData commit-slot state fields and timeout constants wiring.

## Wiring Points Updated (PASS 2)

- [x] `scripts/csharp/Battle/Simulation/Simulation.cs` - route unit tick flow through `SimCombatStateMachine` seam.
- [x] `scripts/csharp/Battle/Simulation/Combat/Targeting/SimTargeting.cs` - move to acquire/reacquire-only selection contract.
- [x] `scripts/csharp/Battle/Simulation/Movement/SimMovement.cs` - simplify to slot/objective movement modes for commit-slot melee paths.
- [x] `scripts/csharp/Battle/Simulation/Movement/Collision/SimOverlapResolver.cs` - called in deterministic step order.
- [x] `scripts/csharp/Battle/Simulation/Data/UnitData.cs` - add commit-slot runtime fields.
- [x] `scripts/csharp/Battle/Simulation/Data/MatchState.cs` - add target slot-state container lifecycle.

## Legacy Paths Removed or Disabled (PASS 2 Target)

- [x] `SimBehavior` periodic lock-based opportunistic retarget path - disabled for commit-slot flow.
- [x] `BlockedNavigationController` yield/escape for slot-committed melee - disabled.
- [x] `ContextSteering` influence in slot-committed melee attack context - disabled.
- [x] same-target ORCA special-case logic that conflicts with slot authority - disabled.
- [x] temporary compatibility shims removed after PASS 2 transition.

## Compile-Safe Stub Behavior Checks

- [x] Stubs compile with deterministic defaults and no null-state crashes.
- [x] Reserved slot anti-steal invariant is enforced in stubs.
- [x] Reacquire only uses allowed retarget reasons in runtime commit behavior.
- [x] Attack phase translation lock path compiles and can be exercised by tests.
- [x] Slot release-before-reacquire order is representable in simulation tick seam.

## Slot Geometry Stub Constants (Lock In PASS 2)

- [x] `slotWaitTimeout = 0.7f`.
- [x] `unreachableTimeout = 1.2f`.
- [x] axis hysteresis angle trigger `30 deg`.
- [x] axis displacement trigger `0.5 * targetRadius`.
- [x] slot distribution `60/30/10` and `minSlots = 3`.

## Test Skeleton Coverage Map

| Case ID | Skeleton Test File | Test Name | Notes |
|---|---|---|---|
| CCS-001 | `tests/csharp/Simulation/SimTargetingCommitTest.cs` | `CommitLock_DoesNotRetarget_OnNearbySpawn` | new file in PASS 2 |
| CCS-002 | `tests/csharp/Simulation/SimTargetingCommitTest.cs` | `SummonerCommit_PreemptsToInAggroUnit_WithinOneTick` | updated summoner soft-lock preempt contract |
| CCS-003 | `tests/csharp/Simulation/SimMeleeSlotManagerTest.cs` | `SlotOverflow_WaitsThenReacquires_ByTimeoutOrder` | new file in PASS 2 |
| CCS-004 | `tests/csharp/Simulation/SimMeleeSlotManagerTest.cs` | `ReservedSlot_CannotBeStolen_ByAnotherUnit` | anti-steal invariant |
| CCS-005 | `tests/csharp/Simulation/SimAttackLoopTest.cs` | `AttackPhase_AnchorsPosition_NoTranslation` | new file in PASS 2 |
| CCS-006 | `tests/csharp/Simulation/SimulationIntegrationTest.cs` | `DeathCleanup_ReleasesSlots_BeforeReacquire` | extend existing file |
| CCS-007 | `tests/csharp/Simulation/MeleeClumpingStabilityTest.cs` | `CommitSlotFlow_ReducesChurn_InDenseClump` | extend existing file |
| CCS-008 | `tests/csharp/Simulation/SimMeleeSlotManagerTest.cs` | `SlotTieBreak_UsesDistanceThenUnitId` | determinism tie-break |
| DCCS-001 | `tests/csharp/Simulation/SimulationIntegrationTest.cs` | `FixedSeed_ReplayParity_ForCommitSlotFlow` | add fixed-seed assertions |

## PASS 2 Scope Checklist

### Data + Enums
- [x] Add new enums under `scripts/csharp/Battle/Simulation/Enums/`.
- [x] Add UnitData fields for commitment, slots, phases, and progress.
- [x] Add target-owned slot state structure under `scripts/csharp/Battle/Simulation/Data/`.

### Systems + Wiring
- [x] Add `SimCombatStateMachine` under `Combat/Lifecycle/`.
- [x] Add `SimMeleeSlotManager` under `Combat/Slots/`.
- [x] Add `SimAttackLoop` under `Combat/Attack/`.
- [x] Add `SimOverlapResolver` under `Movement/Collision/`.
- [x] Wire `Simulation.cs` in deterministic step sequence.

### Compatibility
- [x] Removed temporary compile-safe adapters used during PASS 2 transition.
- [x] Ensure no behavior path bypasses new authority boundaries in stub mode.

### Tests
- [x] Add skeleton tests for all PASS 1 case IDs.
- [x] Ensure test project builds with new files and enums.

## Gate Output Requirement

1. End PASS 2 report with an explicit request for PASS 3 approval.
2. If approval is not provided, state: `blocked waiting approval`.

## Pass Gate Status

Current state:
1. `PASS 1: USE CASES + VALIDATION` complete.
2. `PASS 2: STUBS + WIRING` complete.
3. `PASS 3: IMPLEMENTATION + TESTS` complete.
4. `PR REVIEW: READY` not started.
