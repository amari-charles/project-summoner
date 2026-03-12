# Combat Commit-Slot v1 Validation Cases

**Status:** PASS 3 IMPLEMENTATION COMPLETE  
**Initiative:** `combat-commit-slot-v1`  
**Domain:** `runtime`  
**Last Updated:** `2026-03-12`  
**Companion Plan:** `combat-commit-slot-v1-plan.md`

## How To Use

1. Define all baseline scenarios in PASS 1 with stable case IDs.
2. Add/expand skeleton tests in PASS 2 for each case ID.
3. Update statuses in PASS 3 to `Implemented` or `Deferred`.

Allowed status values:
1. `Design-Covered`
2. `Implemented`
3. `Deferred`

## Case Matrix

| Case ID | Scenario | Expected Result | Test Type | Planned Test File | Status |
|---|---|---|---|---|---|
| CCS-001 | Commit lock with nearby spawn | No retarget unless explicit criteria met | simulation | `tests/csharp/Simulation/SimTargetingCommitTest.cs` | Implemented |
| CCS-002 | Summoner lock under spawn pressure | Unit preempts to valid in-aggro enemy within one tick; otherwise keeps summoner target | simulation | `tests/csharp/Simulation/SimTargetingCommitTest.cs` | Implemented |
| CCS-003 | All slots full on target | Wait, retry, then reacquire by timeout ordering | simulation | `tests/csharp/Simulation/SimMeleeSlotManagerTest.cs` | Implemented |
| CCS-004 | Slot reservation contention | Reserved slot cannot be stolen | unit | `tests/csharp/Simulation/SimMeleeSlotManagerTest.cs` | Implemented |
| CCS-005 | Attack phase anchoring | No translation during windup/active/recovery | simulation | `tests/csharp/Simulation/SimAttackLoopTest.cs` | Implemented |
| CCS-006 | Dead target slot cleanup | Slots released before reacquire pass in same tick | integration | `tests/csharp/Simulation/SimulationIntegrationTest.cs` | Implemented |
| CCS-007 | Dense melee clump stability | Lower churn, higher hit uptime versus baseline repro | simulation | `tests/csharp/Simulation/MeleeClumpingStabilityTest.cs` | Implemented |
| CCS-008 | Deterministic slot assignment ties | Stable outcomes via distance then unit id | unit | `tests/csharp/Simulation/SimMeleeSlotManagerTest.cs` | Implemented |

## Determinism Cases

| Case ID | Seed | Inputs | Checkpoints | Hash/State Assertions | Status |
|---|---|---|---|---|---|
| DCCS-001 | fixed (`424242`) | mirrored frontline clash with equal-distance slot contenders | pre-contact, first-attack, mid-fight, end-fight | target ids, slot ids, attack phases, hp deltas are identical across reruns | Implemented |
| DCCS-002 | fixed (`989898`) | dense 15v1 overflow then timeout-ordered reacquire | slot wait timeout, unreachable timeout, post-reacquire | retarget reasons and resulting slot occupancy timelines are identical across reruns | Implemented |

## Exit Criteria Mapping

### PASS 2

1. Every case has at least one skeleton test entry. (Complete)
2. Planned file targets exist and compile. (Complete)
3. Determinism case harness structure exists for fixed-seed replay checks. (Complete)

### PASS 3

1. Every required case is `Implemented` or `Deferred`.
2. Any deferred case includes rationale and explicit follow-up target.
3. Determinism cases include seed, checkpoints, and assertion evidence.

## Notes

1. These cases intentionally focus on attack commitment, slot authority, and deterministic order.
2. Existing projectile/hit-shape mechanics are validated by existing suites and are not duplicated here unless touched by integration behavior.
