# Combat Spatial Model V2 Plan

**Status:** PASS 3 COMPLETE (Implementation + Tests), awaiting PR Review  
**Initiative:** `combat-spatial-model-v2`  
**Domain:** `runtime`  
**Last Updated:** `2026-03-11`  
**Owner:** `Codex + Gameplay`

## Summary

This initiative redesigns combat geometry around player readability and balance safety. The current runtime uses `SeparationRadius` for both movement spacing and projectile/AoE contact, which couples crowding behavior to hit fairness. We will split spatial concerns into explicit gameplay channels: movement footprint, damage hurtbox, engage gate, and damage shape.

From a game perspective, this should make combat feel fair and readable in 2.5D sprite battles: attacks should start only when targets are visually plausible, and damage should apply in predictable shapes (cone/line/sphere/chain) with deterministic ordering.

## Goals

1. Separate movement spacing and damage contact into independent runtime concepts.
2. Define a two-stage attack contract: `Engage Gate` (can start attack) and `Damage Shape` (who gets hit).
3. Prevent visually confusing 2.5D attacks (for example, ground targets far up-screen still counting as valid without directional gating).
4. Keep deterministic outcomes across host/client and repeated runs.
5. Preserve current movement quality in dense fights while enabling independent combat tuning.

## Non-Goals

1. Full roster-wide final balance pass for all units.
2. Complete VFX/telegraph art overhaul.
3. Rewrite of networking protocol or snapshot format beyond required field mapping for compatibility.
4. Broad redesign of trait/effect systems outside combat geometry touchpoints.

## Architecture Decisions

1. Introduce explicit geometry channels:
   - `Navigation Footprint` for movement/spacing/spawn layout.
   - `Hurtbox` for projectile/AoE/melee contact checks.
2. Attack startup and hit resolution are distinct phases:
   - `Engage Gate`: distance + directional arc + optional depth tolerance + layer compatibility.
   - `Damage Shape`: cone/line/sphere/chain resolution at hit frame using locked aim mode.
3. Keep all combat geometry in deterministic simulation math on world XZ plane, with explicit hit-space mode behavior for ground/air.
4. Use a centralized simulation geometry helper to avoid drift between projectile, melee, and AoE logic.
5. Maintain backward-compatible defaults so existing units keep expected behavior unless explicitly configured.
6. Debug visualization must split `Navigation Footprint`, `Hurtbox`, `Engage Gate`, and `Damage Shape` so tuning can be read without conflating systems.

## Public API / Interface / Type Changes

1. Runtime geometry model additions (or equivalent fields):
   - `NavigationRadius` (or explicit rename/migration of current spacing field)
   - `Hurtbox` config (shape/size/offset)
2. Attack contract additions:
   - `Engage` settings: max distance, arc half-angle, depth tolerance, layer mask
   - `AimMode`: facing / primary-at-windup (and optional future modes)
   - `DamageShape` params for line/cone/sphere/chain
3. Template/runtime propagation updates:
   - `UnitDefinition -> SimUnitTemplate -> UnitData`
4. Debug/inspection surface updates:
   - Distinct debug overlays for navigation footprint vs hurtbox vs engage gate vs damage shape.
   - Attack range debug marker semantics become explicit: "Attack Range" represents engage gate only.
   - Rename debug toggle/pathing terminology from "Separation Radius" to "Navigation Footprint" and remove legacy alias paths.

## Legacy Removal Scope

1. Remove projectile/AoE dependency on `SeparationRadius` for hit contact thresholds.
2. Remove debug assumption that hurtbox radius always equals spacing radius.
3. Remove implicit "single scalar range is enough" startup checks for directional attacks that require arc/depth gating.

## Pass Acceptance Criteria

### PASS 1: USE CASES + VALIDATION

1. Game-facing problems/goals are defined and decision-complete for geometry ownership.
2. Validation scenarios cover startup gating, hit-shape resolution, determinism, and compatibility mapping.
3. Every required scenario has test type + target file mapping.

### PASS 2: STUBS + WIRING

1. Compile-safe types/fields are wired end-to-end for new geometry channels.
2. Central geometry helper API is introduced and referenced by combat callsites.
3. Legacy `SeparationRadius` fallback paths are removed from runtime combat geometry.
4. Test skeletons are added for every validation case ID.

### PASS 3: IMPLEMENTATION + TESTS

1. Engage gate and damage-shape behavior is implemented for shipped scope, with deferred cases explicitly documented in validation artifacts.
2. Projectile/AoE contact uses hurtbox channel; movement systems use navigation footprint only.
3. Validation cases are marked `Implemented` or `Deferred` with rationale.
4. Dense-fight and determinism regressions are validated with test outputs.
5. Debug overlays and toggles are rewired so engage gate and damage shape display independently.

### PR REVIEW: READY

1. Approval-gated pass order evidence exists and artifacts are complete.
2. Review confirms behavior aligns with game goals (readability, fairness, deterministic outcomes).

## PASS 3 Outcome Summary

1. Implemented geometry split wiring for navigation footprint vs hurtbox in movement/projectile/spawn paths.
2. Implemented debug overlay split and naming migration:
   - `Engage Range` overlay channel (startup gate semantics).
   - `Damage Shape` overlay channel (hit geometry semantics).
   - `Navigation Footprint` naming and canonical debug API usage.
3. Added debug-marker tests for engage-vs-shape separation and canonical debug toggle behavior.
4. Carried forward deferred items for engage-depth, locked-aim cone behavior, and expanded determinism coverage.

## Open Risks

1. Splitting geometry channels may surface hidden assumptions in existing tests/content tuning.
2. Engage-depth tuning can over-constrain attacks if defaults are too strict for current camera perspective.
3. Migration period may need dual-read compatibility to avoid save/snapshot regressions.
4. Debug overlays may initially disagree with authored content until all unit definitions are normalized.
5. Debug-menu key rename migration (`attack_ranges` -> `engage_ranges`) can reset persisted toggle preferences on first run.

## Assumptions and Defaults

1. Keep default behavior as close as possible to current live gameplay unless a case is explicitly improved.
2. Use deterministic sort/tie-break (`distance/projection`, then `UnitId`) for all multi-recipient resolution.
3. Ground combat remains XZ-first; full 3D checks are opt-in via hit-space mode.
4. Existing units without explicit hurtbox config get safe default mappings.

## Likely Files

1. `scripts/csharp/Infrastructure/Data/Units/UnitDefinition.cs`
2. `scripts/csharp/Infrastructure/Data/Units/UnitDefinitions.cs`
3. `scripts/csharp/Battle/Simulation/Data/SimCardData.cs`
4. `scripts/csharp/Battle/Simulation/Data/UnitData.cs`
5. `scripts/csharp/Battle/Simulation/Simulation.cs`
6. `scripts/csharp/Battle/Simulation/Combat/SimProjectile.cs`
7. `scripts/csharp/Battle/Simulation/Combat/SimBehavior.cs`
8. `scripts/csharp/Battle/Simulation/Combat/SimTargeting.cs`
9. `scripts/csharp/Battle/Simulation/Movement/OrcaAvoidance.cs`
10. `scripts/csharp/Battle/Simulation/Movement/OverlapCorrection.cs`
11. `scripts/csharp/Battle/Simulation/Movement/MovementTargetResolver.cs`
12. `scripts/csharp/Battle/View/UnitVisual.cs`
13. `scripts/csharp/Battle/View/Spawning/SpawnPositionCalculator.cs`
14. `scripts/csharp/Battle/Input/SummonPreview.cs`
15. `tests/csharp/Simulation/SimBehaviorTest.cs`
16. `tests/csharp/Simulation/SimTargetingTest.cs`
17. `tests/csharp/Simulation/SimProjectileTest.cs`
18. `tests/csharp/Simulation/OrcaAvoidanceTest.cs`
19. `tests/csharp/Simulation/SimulationIntegrationTest.cs`
20. `tests/csharp/View/UnitVisualDebugMarkersTest.cs`
21. `scripts/csharp/Debug/BattlefieldDebugService.cs`
22. `scripts/debug/debug_menu.gd`

## Pass Gate Status

Current state:
1. `PASS 1: USE CASES + VALIDATION` complete
2. `PASS 2: STUBS + WIRING` complete
3. `PASS 3: IMPLEMENTATION + TESTS` complete
4. `PR REVIEW: READY` not started

Gate note:
1. Use explicit approval text to advance.
2. If waiting, state: `blocked waiting approval`.

## Approval Evidence

1. `PASS 2: STUBS + WIRING` approval was explicitly recorded in the implementation thread on `2026-03-11` (`Approve Pass 2`).
2. `PASS 3: IMPLEMENTATION + TESTS` work was executed after that approval.
