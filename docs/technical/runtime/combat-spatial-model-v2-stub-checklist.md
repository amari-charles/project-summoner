# Combat Spatial Model V2 Stub Checklist

**Status:** PASS 3 COMPLETE (Checklist Closed with Deferred Items)  
**Initiative:** `combat-spatial-model-v2`  
**Domain:** `runtime`  
**Last Updated:** `2026-03-11`

## Types Created

1. `CombatGeometry` - centralized simulation geometry helper for navigation/hurtbox and hit-space math.

## Interfaces Created

1. none (PASS 2 scope uses data-shape and helper wiring only).

## Wiring Points Updated

1. `SimUnitTemplate` now carries `NavigationRadius`, `HurtboxRadius`, `HurtboxHeight`, `HurtboxHorizontal`, `HurtboxOffset`.
2. `UnitData` now carries the same runtime geometry channels.
3. `UnitDefinitions.BuildSimTemplate(...)` now maps visual separation + optional hurtbox config into the new channels.
4. `Simulation.SpawnUnitsFromCard(...)` now propagates new geometry fields from template to spawned units.
5. `Simulation.SpawnUnitsFromCard(...)` spawn offset now reads navigation footprint first (fallback to legacy separation radius).
6. `SimProjectile` contact/AoE checks now resolve target size via `CombatGeometry.GetHurtboxRadius(...)`.
7. `OrcaAvoidance`, `OverlapCorrection`, `MovementTargetResolver`, and `ContextSteering` now resolve footprint sizing via `CombatGeometry.GetNavigationRadius(...)`.

## Legacy Paths Removed or Disabled

1. `SimProjectile` direct target-size checks against `UnitData.SeparationRadius` - disabled (uses hurtbox channel helper).
2. Movement overlap/avoidance/orbit/crowd-danger direct navigation sizing from `UnitData.SeparationRadius` - disabled (uses navigation channel helper).

## Compile-Safe Stub Behavior Checks

1. `CombatGeometry.GetNavigationRadius(...)` preserves compatibility fallback to legacy `SeparationRadius`.
2. `CombatGeometry.GetHurtboxRadius(...)` preserves compatibility fallback to navigation radius.
3. Projectile hit-space routing (`GroundCylinder` vs `Sphere3D`) is centralized and deterministic.
4. Existing content remains behavior-compatible because default mappings initialize `NavigationRadius` from existing visual separation radius.

## Test Skeleton Coverage Map

| Case ID | Skeleton Test File | Test Name | Notes |
|---|---|---|---|
| CSM-001 | `tests/csharp/Simulation/CombatSpatialModelV2StubCoverageTest.cs` | `CSM_001_LegacyFallback_UsesSeparationRadiusWhenNewFieldsUnset` | Compatibility fallback hook |
| CSM-002 | `tests/csharp/Simulation/CombatSpatialModelV2StubCoverageTest.cs` | `CSM_002_EngageArcDepthGate_Stub_OutOfArcRejected` | Engage arc/depth hook |
| CSM-003 | `tests/csharp/Simulation/CombatSpatialModelV2StubCoverageTest.cs` | `CSM_003_EngageArcDepthGate_Stub_ForwardTargetAccepted` | Engage positive-path hook |
| CSM-004 | `tests/csharp/Simulation/CombatSpatialModelV2StubCoverageTest.cs` | `CSM_004_PiercingLineRangeContract_Stub` | Pass 3 implementation placeholder |
| CSM-005 | `tests/csharp/Simulation/CombatSpatialModelV2StubCoverageTest.cs` | `CSM_005_ConeLockedAim_Stub` | Pass 3 implementation placeholder |
| CSM-006 | `tests/csharp/Simulation/CombatSpatialModelV2StubCoverageTest.cs` | `CSM_006_ConeCenterOffset_Stub` | Pass 3 implementation placeholder |
| CSM-007 | `tests/csharp/Simulation/CombatSpatialModelV2StubCoverageTest.cs` | `CSM_007_ProjectileContact_UsesHurtboxChannel_Stub` | Hurtbox channel hook |
| CSM-008 | `tests/csharp/Simulation/CombatSpatialModelV2StubCoverageTest.cs` | `CSM_008_AoeInclusion_UsesHurtboxChannel_Stub` | Hurtbox AoE hook |
| CSM-009 | `tests/csharp/Simulation/CombatSpatialModelV2StubCoverageTest.cs` | `CSM_009_MovementSystems_UseNavigationFootprint_Stub` | Navigation channel hook |
| CSM-010 | `tests/csharp/Simulation/CombatSpatialModelV2StubCoverageTest.cs` | `CSM_010_SummonerOrbit_UsesNavigationFootprint_Stub` | Pass 3 implementation placeholder |
| CSM-011 | `tests/csharp/Simulation/CombatSpatialModelV2StubCoverageTest.cs` | `CSM_011_SpawnSafety_UsesNavigationFootprint_Stub` | Spawn-path hook |
| CSM-012 | `tests/csharp/View/UnitVisualDebugMarkersTest.cs` | `Process_HurtboxDebugFlag_CreatesAndQueuesMarkerForRemoval` | Real debug marker coverage (hurtbox split path) |
| CSM-013 | `tests/csharp/Simulation/CombatSpatialModelV2StubCoverageTest.cs` | `CSM_013_HitSpaceMode_GroundCylinderVsSphere3D` | Hit-space routing hook |
| CSM-014 | `tests/csharp/Simulation/CombatSpatialModelV2StubCoverageTest.cs` | `CSM_014_MultiRecipientTieOrdering_Stub` | Deterministic ordering placeholder |
| CSM-015 | `tests/csharp/View/UnitVisualDebugMarkersTest.cs` | `Process_EngageRangeAndDamageShape_UseIndependentDebugMarkers` | Engage gate and damage shape overlays are independent |
| CSM-016 | `tests/csharp/View/UnitVisualDebugMarkersTest.cs` | `DebugService_NavigationFootprintToggle_UsesCanonicalApi` | Canonical navigation-footprint debug API behavior |
| DCSM-001 | `tests/csharp/Simulation/CombatSpatialModelV2StubCoverageTest.cs` | `DCSM_001_DeterminismScenario_Stub` | Determinism placeholder |
| DCSM-002 | `tests/csharp/Simulation/CombatSpatialModelV2StubCoverageTest.cs` | `DCSM_002_DeterminismDenseSwarm_Stub` | Determinism placeholder |

## Next Gate

1. End PASS 2 with explicit request for PASS 3 approval.
2. If approval not provided, state: `blocked waiting approval`.
