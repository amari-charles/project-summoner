# Combat Spatial Model V2 Validation Cases

**Status:** PASS 3 IMPLEMENTATION REVIEWED  
**Initiative:** `combat-spatial-model-v2`  
**Domain:** `runtime`  
**Last Updated:** `2026-03-11`  
**Companion Plan:** `combat-spatial-model-v2-plan.md`

## How To Use

1. Define all baseline scenarios in Pass 1 with stable case IDs.
2. Add test skeletons in Pass 2 for each case.
3. Mark each case `Implemented` or `Deferred` in Pass 3.

Allowed status values:
1. `Design-Covered`
2. `Implemented`
3. `Deferred`

## Case Matrix

| Case ID | Scenario | Expected Result | Test Type | Test File | Status |
|---|---|---|---|---|---|
| CSM-001 | Legacy unit with no explicit hurtbox/engage fields | Runtime fallback preserves prior behavior within tolerance | unit | `tests/csharp/Simulation/CombatSpatialModelV2StubCoverageTest.cs` | Implemented |
| CSM-002 | Directional attack startup with target far up/down battlefield depth | Attack does not start when target violates engage arc/depth gate | unit | `tests/csharp/Simulation/SimTargetingTest.cs` | Deferred |
| CSM-003 | Directional attack startup with valid forward target | Attack starts when engage gate passes (distance + arc + depth + layer) | unit | `tests/csharp/Simulation/CombatSpatialModelV2StubCoverageTest.cs` | Implemented |
| CSM-004 | Piercing line attack with `LineLength` greater than engage distance | Startup uses engage rules; hit resolution uses line corridor geometry | simulation | `tests/csharp/Simulation/SimBehaviorTest.cs` | Deferred |
| CSM-005 | Cone attack with locked aim at windup | Recipients are selected from cone shape at hit frame using locked aim direction | simulation | `tests/csharp/Simulation/SimBehaviorTest.cs` | Deferred |
| CSM-006 | Cone attack with center offset (for example Puff downward center) | Cone center offset changes recipient membership as authored | simulation | `tests/csharp/Simulation/SimBehaviorTest.cs` | Deferred |
| CSM-007 | Projectile contact where target hurtbox differs from navigation radius | Contact uses hurtbox channel only; movement footprint does not inflate/deflate hit fairness | simulation | `tests/csharp/Simulation/SimProjectileTest.cs` | Implemented |
| CSM-008 | AoE radius check where target hurtbox differs from navigation radius | AoE inclusion uses hurtbox channel only | simulation | `tests/csharp/Simulation/SimProjectileTest.cs` | Implemented |
| CSM-009 | Dense-unit movement avoidance and overlap correction | ORCA/overlap behavior uses navigation footprint only and remains stable | simulation | `tests/csharp/Simulation/OrcaAvoidanceTest.cs` | Implemented |
| CSM-010 | Summoner orbit slot selection with varied hurtbox sizes | Orbit slot density/scoring remains tied to navigation footprint only | simulation | `tests/csharp/Simulation/SimBehaviorTest.cs` | Deferred |
| CSM-011 | Spawn safety check with unit-specific spacing | Spawn placement uses navigation footprint and does not regress overlap safety | integration | `tests/csharp/View/Spawning/SpawnPositionCalculatorTest.cs` | Deferred |
| CSM-012 | Debug overlays enabled for unit geometry | Distinct overlays render for navigation footprint and hurtbox; no conflation | unit | `tests/csharp/View/UnitVisualDebugMarkersTest.cs` | Implemented |
| CSM-013 | GroundCylinder vs Sphere3D projectile hit-space modes | Ground targets use XZ logic in GroundCylinder; Sphere3D applies full 3D | simulation | `tests/csharp/Simulation/SimProjectileTest.cs` | Implemented |
| CSM-014 | Multi-recipient tie cases for line/cone around boundaries | Recipient ordering remains deterministic with documented tie-breaks | simulation | `tests/csharp/Simulation/SimBehaviorTest.cs` | Deferred |
| CSM-015 | Debug attack-range marker for directional/vector attacks | "Attack range" overlay renders engage gate only; damage shape is shown by separate overlay channel | unit | `tests/csharp/View/UnitVisualDebugMarkersTest.cs` | Implemented |
| CSM-016 | Debug toggle migration for navigation footprint naming | Renamed navigation-footprint toggle controls footprint marker and preserves compatibility alias behavior | unit | `tests/csharp/View/UnitVisualDebugMarkersTest.cs` | Implemented |

## Determinism Cases (If Applicable)

| Case ID | Seed | Inputs | Checkpoints | Hash/State Assertions | Status |
|---|---|---|---|---|---|
| DCSM-001 | fixed (`424242`) | mirrored line and cone engagements with equal-distance candidates | pre-hit, hit-frame, post-hit | recipient set + order + HP deltas match repeat runs | Deferred |
| DCSM-002 | fixed (`989898`) | dense swarm with mixed movement layers and projectile AoE | mid-fight and end-of-fight snapshots | no host/client divergence in hit membership/order | Deferred |

## Deferred Cases

| Case ID | Reason Deferred | Planned Follow-up |
|---|---|---|
| CSM-002 | Engage-depth tolerance channel is not yet modeled separately from cone arc/range checks. | Add explicit engage-depth field + targeting tests in follow-up pass. |
| CSM-004 | Line attacks still rely on current attack-range engage semantics; no dedicated engage-vs-line-length contract yet. | Introduce engage contract fields and line-specific startup validation. |
| CSM-005 | Locked-aim-at-windup for cone resolution is not yet exposed as explicit runtime mode. | Add `AimMode` state and cone hit-frame lock tests. |
| CSM-006 | Cone center offset authoring is not yet surfaced in runtime attack geometry mapping. | Add cone center-offset fields + recipient-membership tests. |
| CSM-010 | Runtime behavior is navigation-footprint-only, but dedicated hurtbox-variance orbit regression test is still missing. | Add simulation regression in `SimBehaviorTest` with varied hurtbox vs navigation values. |
| CSM-011 | Simulation spawn offset uses navigation radius, but view spawn safety regression coverage is incomplete for mixed radii. | Add integration/view tests asserting mixed-footprint spawn safety invariants. |
| CSM-014 | Determinism tie coverage for boundary cases is not yet comprehensive across cone + line mixed edges. | Add explicit boundary tie-case deterministic ordering tests. |
| DCSM-001 | End-to-end deterministic replay assertions for mirrored line/cone scenarios are not implemented. | Add fixed-seed repeated-run hash assertions. |
| DCSM-002 | Dense swarm determinism checks across movement + projectile membership/order are not implemented. | Add fixed-seed snapshot hash checks in simulation integration tests. |

## Exit Criteria Mapping

### Pass 2

1. Every required case has a planned skeleton test entry.
2. All geometry ownership boundaries are represented in stubbed type/wiring surfaces.

### Pass 3

1. Every required case is `Implemented` or `Deferred`.
2. Any deferred case includes explicit rationale and follow-up target.
3. Determinism cases pass with stable ordering assertions.
