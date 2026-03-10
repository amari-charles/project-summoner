# Attack Types V1 Plan

**Status:** PASS 3 COMPLETE (Implementation + Tests), PR REVIEW PENDING  
**Initiative:** `attack-types-v1`  
**Domain:** `runtime`  
**Last Updated:** `2026-03-10`  
**Owner:** `Codex + Gameplay`

## Summary

This initiative shifts from a rigid "type-first" combat model to a "vector-first" attack model. Instead of hardcoding each new attack as a separate mode, we define an attack as a composition of vectors such as timing, shape, delivery, selection, propagation, and limits. Presets like single-target, cleave, line-pierce, and chain are then built from those vectors.

The design keeps simulation deterministic and behavior-preserving for existing units by default. Existing units remain effectively single-target unless explicitly configured otherwise. Ranged projectile behavior and spell AoE remain out of scope for V1 implementation, but the vector schema is designed to support them later.

## Goals

1. Define an explicit vector-based attack contract in unit/runtime data.
2. Support four baseline presets via vectors: single-target, area multi-hit, line-pierce, and chain.
3. Enforce deterministic recipient selection and ordering across all vector combinations.
4. Keep existing unit behavior unchanged by default while enabling a few opt-in units for validation.

## Non-Goals

1. No redesign of spell AoE, projectile AoE, or projectile collision spaces in this initiative.
2. No global balance pass across the full roster.
3. No UI/VFX telegraph system in V1.
4. No status-effect system redesign.

## Architecture Decisions

1. Attack behavior is defined at the unit-definition layer and propagated through `SimUnitTemplate` into `UnitData`, keeping simulation runtime as source of truth.
2. Primary target acquisition stays in `SimTargeting`; vector-based recipient expansion occurs in attack execution (`SimBehavior`).
3. Geometry and recipient selection remain pure simulation math (no Godot physics nodes) for determinism/testability.
4. Use a shared deterministic recipient-selection helper so all vector combinations use the same filter/sort/tie-break logic.
5. Preserve trigger stability in V1: primary-hit trigger pipeline stays unchanged; secondary recipients do not fan out per-target on-hit triggers.

## Public API / Interface / Type Changes

1. Add vector-based attack config to `UnitDefinition`, `SimUnitTemplate`, and `UnitData`:
   - `AttackTiming`: windup, active window, recovery, tick interval
   - `AttackDeliveryMode`: instant (V1), projectile/persistent reserved
   - `AttackAreaShape`: sphere, box, capsule, line corridor
   - `AttackAreaSize`: shape dimensions
   - `AttackSelectionMode`: single / area collect / line collect / chain hops
   - `AttackTargetLimit`: max targets (`0` = unlimited)
   - `AttackPropagation`: none / pierce / chain with params
   - `AttackRules`: include summoner, repeat-hit policy, trigger policy
2. Keep compatibility preset field(s) for easier authoring in V1:
   - `AttackPreset` (maps to vector defaults)
3. New shared enums/types expected:
   - `AttackPreset`
   - `AttackSelectionMode`
   - `AttackAreaShape`
   - `AttackPropagationMode`
   - small records/structs for timing/area/chain params

## Legacy Removal Scope

1. Remove the assumption that melee attacks always damage exactly one unit.
2. Replace direct per-type branching with vector-driven resolution in `SimBehavior`.
3. Keep legacy behavior alive through defaults/preset mapping for backward compatibility.

## Pass Acceptance Criteria

### PASS 1: USE CASES + VALIDATION

1. Plan doc defines vector model, defaults, and compatibility strategy.
2. Validation matrix maps vector scenarios to deterministic tests.

### PASS 2: STUBS + WIRING

1. Vector fields compile end-to-end (`UnitDefinition` -> `SimUnitTemplate` -> `UnitData`).
2. `SimBehavior` has compile-safe vector-resolution skeleton paths.
3. Preset-to-vector mapping is wired with behavior-preserving defaults.
4. Test skeletons exist for all validation IDs.

### PASS 3: IMPLEMENTATION + TESTS

1. Area, line-pierce, and chain recipient selection implemented on vector model.
2. Existing unit defaults remain behavior-equivalent.
3. Deterministic ordering and cap behavior pass mapped tests.
4. Validation cases marked `Implemented` or `Deferred` with rationale.

### PR REVIEW: READY

1. Required artifacts exist (`plan`, `validation-cases`, `stub-checklist`) and pass order evidence is present.
2. Review confirms implementation matches vector contract and no pre-gate implementation occurred.

## Open Risks

1. Vector flexibility can increase configuration complexity without good defaults.
2. Chain and line edge cases can desync if tie-break/ordering rules are incomplete.
3. Trigger semantics for secondary recipients may need further iteration after V1 gameplay testing.

## Assumptions and Defaults

1. Default preset maps to legacy single-target behavior.
2. V1 applies vector execution to melee hit resolution only; ranged/spells keep current pipelines.
3. Deterministic ordering uses distance then `UnitId` tie-break.
4. Summoner targets stay single-target in V1 regardless of area/line/chain vectors.

## Likely Files

1. `scripts/csharp/Infrastructure/Data/Units/UnitDefinition.cs`
2. `scripts/csharp/Infrastructure/Data/Units/Enums.cs`
3. `scripts/csharp/Infrastructure/Data/Units/UnitDefinitions.cs`
4. `scripts/csharp/Battle/Simulation/Data/SimCardData.cs` (`SimUnitTemplate`)
5. `scripts/csharp/Battle/Simulation/Data/UnitData.cs`
6. `scripts/csharp/Battle/Simulation/Simulation.cs`
7. `scripts/csharp/Battle/Simulation/Combat/SimBehavior.cs`
8. `tests/csharp/Simulation/UnitDefinitionsTargetingProfileTest.cs`
9. `tests/csharp/Simulation/SimBehaviorTest.cs`
10. `tests/csharp/Simulation/SimulationIntegrationTest.cs`

## Pass Gate Status

Current state:
1. `PASS 1: USE CASES + VALIDATION` complete
2. `PASS 2: STUBS + WIRING` complete
3. `PASS 3: IMPLEMENTATION + TESTS` complete
4. `PR REVIEW: READY` pending review execution

Gate note:
1. Use explicit approval text to advance.
2. If waiting, state: `blocked waiting approval`.
