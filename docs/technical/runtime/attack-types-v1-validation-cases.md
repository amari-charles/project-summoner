# Attack Types V1 Validation Cases

**Status:** PASS 3 IMPLEMENTED  
**Initiative:** `attack-types-v1`  
**Domain:** `runtime`  
**Last Updated:** `2026-03-10`  
**Companion Plan:** `attack-types-v1-plan.md`

## How To Use

1. Define baseline scenarios in Pass 1 with concrete test mapping.
2. Add skeleton tests in Pass 2 for each listed case.
3. Mark each case `Implemented` or `Deferred` in Pass 3.

Allowed status values:
1. `Design-Covered`
2. `Implemented`
3. `Deferred`

## Case Matrix

| Case ID | Scenario | Expected Result | Test Type | Test File | Status |
|---|---|---|---|---|---|
| ATK-001 | Legacy unit with no new vector fields explicitly set | Behavior remains single-target and matches current damage/event flow | unit | `tests/csharp/Simulation/SimBehaviorTest.cs` | Implemented |
| ATK-002 | Vector config fields map `UnitDefinition` -> `SimUnitTemplate` | Template vector fields match definition values and defaults | unit | `tests/csharp/Simulation/UnitDefinitionsTargetingProfileTest.cs` | Implemented |
| ATK-003 | Spawn path maps template vector fields into `UnitData` | Runtime `UnitData` vector fields match template values | integration | `tests/csharp/Simulation/SimulationIntegrationTest.cs` | Implemented |
| ATK-004 | Preset mapping (`SingleTarget`) to vectors | Preset produces compatibility-equivalent vector configuration | unit | `tests/csharp/Simulation/UnitDefinitionsTargetingProfileTest.cs` | Implemented |
| ATK-005 | Selection=`Single` with nearby extra enemies | Only primary target takes melee damage | simulation | `tests/csharp/Simulation/SimBehaviorTest.cs` | Implemented |
| ATK-006 | Selection=`AreaCollect` + Shape=`Sphere` | Enemies in radius are damaged up to target limit | simulation | `tests/csharp/Simulation/SimBehaviorTest.cs` | Implemented |
| ATK-007 | Selection=`AreaCollect` + Shape=`Box` + facing | Only forward-box recipients are damaged | simulation | `tests/csharp/Simulation/SimBehaviorTest.cs` | Implemented |
| ATK-008 | Selection=`AreaCollect` + Shape=`Capsule` | Capsule boundary handling is deterministic | simulation | `tests/csharp/Simulation/SimBehaviorTest.cs` | Implemented |
| ATK-009 | Selection=`LineCollect` + Propagation=`Pierce` | Corridor recipients are damaged in deterministic order up to limit | simulation | `tests/csharp/Simulation/SimBehaviorTest.cs` | Implemented |
| ATK-010 | `LineCollect/Pierce` excludes off-corridor units | Units outside line width are ignored | simulation | `tests/csharp/Simulation/SimBehaviorTest.cs` | Implemented |
| ATK-011 | Selection=`ChainHops` + Propagation=`Chain` | Damage hops from primary to nearest valid targets for jump count | simulation | `tests/csharp/Simulation/SimBehaviorTest.cs` | Implemented |
| ATK-012 | `Chain` respects hop radius/liveness/team filters | Dead, ally, and out-of-radius units are skipped | simulation | `tests/csharp/Simulation/SimBehaviorTest.cs` | Implemented |
| ATK-013 | Target limit cap across vector modes | Recipient count is capped (`0` means unlimited) with stable ordering | simulation | `tests/csharp/Simulation/SimBehaviorTest.cs` | Implemented |
| ATK-014 | Cone constraint fails for primary target | Existing fallback movement semantics remain unchanged | unit | `tests/csharp/Simulation/SimBehaviorTest.cs` | Implemented |
| ATK-015 | Ranged unit with vector fields present | Projectile spawn and delayed damage behavior remain unchanged | unit | `tests/csharp/Simulation/SimBehaviorTest.cs` | Implemented |
| ATK-016 | Secondary recipients die in vector multi-hit scenarios | Secondary damage/death events fire; default trigger policy remains primary-only in V1 (with `EveryRecipient` opt-in covered) | simulation | `tests/csharp/Simulation/SimBehaviorTest.cs` | Implemented |
| ATK-017 | Summoner-target attack with non-single vectors configured | Summoner damage remains single-target only in V1 | simulation | `tests/csharp/Simulation/SimBehaviorTest.cs` | Implemented |

## Determinism Cases

| Case ID | Seed | Inputs | Checkpoints | Hash/State Assertions | Status |
|---|---|---|---|---|---|
| DATK-001 | fixed (`12345`) | same spawn layout and multi-target attack resolve | attack tick and post-attack snapshot | damaged target set + order + HP totals match between repeated runs | Implemented |
| DATK-002 | fixed (`67890`) | mirrored left/right facing box-hitbox scenario | attack tick and post-attack state | recipient membership is mirror-consistent and deterministic | Implemented |
| DATK-003 | fixed (`24680`) | chain pattern with equidistant candidate ties | chain resolution step and post-attack state | chain hop order uses deterministic tie-break (`UnitId`) | Implemented |
| DATK-004 | fixed (`11223`) | mixed vector configs with same target counts but different shape/selection | attack tick snapshots | no host/client divergence in recipient ordering and HP outcomes | Implemented |

## Deferred Cases

| Case ID | Reason Deferred | Planned Follow-up |
|---|---|---|
| (none) | - | - |

## Exit Criteria Mapping

### Pass 2

1. Every listed case has a stub/skeleton test entry.
2. New attack contract fields compile across definition/template/runtime layers.
3. Determinism cases have skeleton harness hooks.

### Pass 3

1. All required cases are `Implemented` or `Deferred`.
2. Any deferred case includes rationale and follow-up target.
3. Determinism cases pass with stable result ordering.
