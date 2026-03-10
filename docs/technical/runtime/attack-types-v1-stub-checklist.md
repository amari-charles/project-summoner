# Attack Types V1 Stub Checklist

**Status:** PASS 3 COMPLETE (Checklist Closed)  
**Initiative:** `attack-types-v1`  
**Domain:** `runtime`  
**Last Updated:** `2026-03-10`

## Approval Evidence

1. PASS 1 -> PASS 2 approval captured on 2026-03-10 in delivery thread: `proceed`.
2. PASS 2 -> PASS 3 approval captured on 2026-03-10 in delivery thread: `ok next phase`.

## Artifact Checklist

1. [x] `attack-types-v1-plan.md` present
2. [x] `attack-types-v1-validation-cases.md` present
3. [x] `attack-types-v1-stub-checklist.md` present

## Types Created

1. `AttackPreset` - authoring preset mapped to vector defaults.
2. `AttackSelectionMode` - recipient selection vector.
3. `AttackAreaShape` - area geometry vector.
4. `AttackPropagationMode` - propagation vector.
5. `AttackDeliveryMode` - delivery vector.
6. `AttackTriggerMode` - trigger policy vector.
7. `AttackVectorConfig` + grouped config records (`AttackTimingConfig`, `AttackSelectionConfig`, `AttackAreaConfig`, `AttackPropagationConfig`, `AttackRulesConfig`) - grouped authoring schema.
8. `AttackVectorState` + grouped state classes (`AttackTimingState`, `AttackSelectionState`, `AttackAreaState`, `AttackPropagationState`, `AttackRulesState`) - grouped simulation/runtime schema.

## Interfaces Created

1. none (PASS 2 uses data-shape stubs only).

## Wiring Points Updated

1. `UnitDefinition` now carries grouped `AttackVectorConfig` instead of a long flat attack field list.
2. `AttackVectorStateBuilder` introduced in simulation data slice to own preset/default mapping into runtime state.
3. `SimUnitTemplate` and `UnitData` now carry grouped `AttackVectorState`.
4. `Simulation.SpawnUnitsFromCard(...)` deep-clones grouped attack state into spawned units.
5. `SimBehavior.ApplyMeleeDamageToUnit(...)` now delegates recipient selection to `AttackRecipientResolver`.

## Legacy Paths Removed or Disabled

1. `SimBehavior` inline recipient-selection helper block removed and replaced by dedicated resolver class.
2. `UnitDefinitions` inline attack-preset mapping block removed and replaced by dedicated mapper class.

## PASS 3 Implementation Behavior Checks

1. Default units map to compatibility single-target vectors via `AttackPreset.SingleTarget` (`LegacySingleTarget` kept as alias).
2. Vector recipient-resolution is deterministic across area/line/chain expansion with stable tie-breaks.
3. Non-single selection modes now apply implemented recipient collection (sphere/box/capsule, line corridor, chain hops).
4. Secondary-recipient damage path keeps default primary-only trigger behavior, with `AttackTriggerMode.EveryRecipient` opt-in support.

## Coverage Map

| Case ID | Test File | Test Name | Notes |
|---|---|---|---|
| ATK-001 | `tests/csharp/Simulation/SimBehaviorTest.cs` | `TickBehavior_MeleeAttack_DealsDamage` | Legacy baseline guard |
| ATK-002 | `tests/csharp/Simulation/UnitDefinitionsTargetingProfileTest.cs` | `BuildSimTemplate_AttackVectorDefaults_MapFromDefinition` | Vector field mapping |
| ATK-003 | `tests/csharp/Simulation/SimulationIntegrationTest.cs` | `SpawnedUnit_RetainsAttackVectorFields` | Spawn wiring |
| ATK-004 | `tests/csharp/Simulation/UnitDefinitionsTargetingProfileTest.cs` | `BuildSimTemplate_AttackVectorDefaults_MapFromDefinition` | Preset default compatibility |
| ATK-005 | `tests/csharp/Simulation/SimBehaviorTest.cs` | `TickBehavior_AttackVector_SingleMode_DamagesPrimaryOnly` | Single-target guard |
| ATK-006 | `tests/csharp/Simulation/SimBehaviorTest.cs` | `TickBehavior_AttackVector_AreaCollectSphere_DamagesEnemiesInRadius` | Sphere area collect |
| ATK-007 | `tests/csharp/Simulation/SimBehaviorTest.cs` | `TickBehavior_AttackVector_AreaCollectBox_OnlyHitsForwardFacingRecipients` | Forward-facing box |
| ATK-008 | `tests/csharp/Simulation/SimBehaviorTest.cs` | `TickBehavior_AttackVector_AreaCollectCapsule_BoundaryDeterministic` | Capsule boundary determinism |
| ATK-009 | `tests/csharp/Simulation/SimBehaviorTest.cs` | `TickBehavior_AttackVector_LineCollectPierce_DamagesCorridorRecipientsInOrder` | Line corridor ordering |
| ATK-010 | `tests/csharp/Simulation/SimBehaviorTest.cs` | `TickBehavior_AttackVector_LineCollectPierce_ExcludesOffCorridorRecipients` | Line corridor exclusion |
| ATK-011 | `tests/csharp/Simulation/SimBehaviorTest.cs` | `TickBehavior_AttackVector_ChainHops_DamagesNearestHops` | Chain hop progression |
| ATK-012 | `tests/csharp/Simulation/SimBehaviorTest.cs` | `TickBehavior_AttackVector_ChainHops_SkipsDeadAlliesAndOutOfRadius` | Chain filters |
| ATK-013 | `tests/csharp/Simulation/SimBehaviorTest.cs` | `TickBehavior_AttackVector_TargetLimit_CapsRecipientCount` | Limit enforcement |
| ATK-013 | `tests/csharp/Simulation/SimBehaviorTest.cs` | `TickBehavior_AttackVector_TargetLimitZero_HitsUnlimitedRecipients` | `0 = unlimited` |
| ATK-014 | `tests/csharp/Simulation/SimBehaviorTest.cs` | `TickBehavior_ConeNotSatisfied_FallbackStrafe` | Existing fallback guard |
| ATK-015 | `tests/csharp/Simulation/SimBehaviorTest.cs` | `TickBehavior_RangedAttack_NoDelay_SpawnsProjectile` | Existing behavior guard |
| ATK-016 | `tests/csharp/Simulation/SimBehaviorTest.cs` | `TickBehavior_AttackVector_SecondaryDeaths_EmitEvents_PrimaryAttackEventRemainsSingle` | Secondary death event semantics |
| ATK-016 | `tests/csharp/Simulation/SimBehaviorTest.cs` | `TickBehavior_AttackVector_PrimaryOnlyTriggerMode_DoesNotFireSecondaryOnDamagedTriggers` | Default trigger policy guard |
| ATK-016 | `tests/csharp/Simulation/SimBehaviorTest.cs` | `TickBehavior_AttackVector_EveryRecipientTriggerMode_FiresSecondaryOnDamagedTriggers` | Trigger-mode opt-in fanout |
| ATK-017 | `tests/csharp/Simulation/SimBehaviorTest.cs` | `TickBehavior_AttackVector_SummonerTarget_IgnoresNonSingleExpansionInV1` | Summoner guard |
| DATK-001 | `tests/csharp/Simulation/SimBehaviorTest.cs` | `AttackVector_Determinism_DATK001_RepeatedRunTargetsMatch` | Repeated-run consistency |
| DATK-002 | `tests/csharp/Simulation/SimBehaviorTest.cs` | `AttackVector_Determinism_DATK002_MirroredFacingConsistent` | Mirrored facing consistency |
| DATK-003 | `tests/csharp/Simulation/SimBehaviorTest.cs` | `AttackVector_Determinism_DATK003_ChainTieBreakStable` | Chain tie-break consistency |
| DATK-004 | `tests/csharp/Simulation/SimBehaviorTest.cs` | `AttackVector_Determinism_DATK004_MixedVectorsStableOutcome` | Mixed-vector consistency |

## Notes

1. PASS 2 compatibility guarantees remain intact for default single-target units.
2. PASS 3 implemented vector recipient selection and determinism cases; no deferred items in this initiative.

## Next Gate

1. Proceed to `PR REVIEW: READY` with the `pr-review` skill.
