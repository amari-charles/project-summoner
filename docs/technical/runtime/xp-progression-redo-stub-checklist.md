# XP Progression Redo Stub Checklist

**Status:** PASS 2 CHECKLIST
**Initiative:** `xp-progression-redo`
**Domain:** `runtime`
**Last Updated:** `2026-03-09`

## Types Created

1. `ProgressionState` - Canonical shared progression state (`level`, `xp_toward_next`, `max_level`).
2. `ProgressionApplyResult` - Deterministic level-up result envelope (`status`, previous/next state, spent XP).
3. `ProgressionApplyStatus` - Explicit no-op/failure/success status contract for shared engine calls.

## Interfaces Created

1. `IProgressionCurve` - Domain policy contract for `GetXpCostForNextLevel(currentLevel, maxLevel)`.

## Wiring Points Updated

1. `scripts/csharp/Meta/Services/Cards/Handlers/CardProgressionHandler.cs`
2. `scripts/csharp/Meta/Services/Summoner/SummonerProgressionService.cs`
3. `scripts/csharp/Meta/Progression/Core/ProgressionEngine.cs`

## Legacy Paths Removed or Disabled

1. `CardProgressionHandler` local level-up arithmetic - `disabled` (now delegated to `ProgressionEngine` + `CardProgressionCurve`).
2. `SummonerProgressionService` local level-up arithmetic - `disabled` (now delegated to `ProgressionEngine` + `SummonerProgressionCurve`).
3. Adapter-local progress/can-level-up formulas - `disabled` in both domains; shared engine now computes these.

## Compile-Safe Stub Behavior Checks

1. Shared engine guards invalid state, max-level no-op, invalid cost, and insufficient XP deterministically.
2. Card and summoner adapters call shared engine for cost/progress/can-level-up/apply-level-up while preserving existing payload keys and persistence boundaries.
3. Trait-point grant remains in adapters and still occurs only on successful engine apply.

## Test Skeleton Coverage Map

| Case ID | Skeleton Test File | Test Name | Notes |
|---|---|---|---|
| XP-C05 | `tests/csharp/Services/ProgressionCoreTest.cs` | `MaxLevel_ProgressAndCost_AreStableNoOp` | Shared max-level contract baseline |
| XP-C06 | `tests/csharp/Services/ProgressionCoreParityTest.cs` | `EquivalentCurves_WithSameState_ProduceIdenticalOutputs` | Deterministic parity baseline |
| XP-C08 | `tests/csharp/Services/SummonerProgressionServiceTest.cs` | `GetSummonerProgressionInfo_ExposesUiContractFieldsWithExpectedValues` | Implemented in PASS 3 |
| XP-C09 | `tests/csharp/Services/ProgressionCoreTest.cs` | `ApplyLevelUp_InsufficientXp_IsDeterministicNoOp` | Insufficient XP no mutation |
| XP-C10 | `tests/csharp/Services/ProgressionCoreTest.cs` | `InvalidState_ApplyLevelUp_ReturnsInvalidStateNoOp` | Invalid state deterministic no-op |
| XP-C12 | `tests/csharp/Services/ProgressionCoreParityTest.cs` | `EquivalentCurves_WithSameState_ProduceIdenticalOutputs` | No drift baseline at shared-core level |
| XP-C14 | `tests/csharp/Services/SummonerProgressionServiceTest.cs` | `SummonerThresholdPolicy_LevelToLevelCost_IsPreserved` | Implemented in PASS 3 |
| XP-D01 | `tests/csharp/Services/ProgressionCoreTest.cs` | `ApplyLevelUp_Success_ConsumesExactXpAndIncrementsLevel` | Deterministic transition tuple baseline |
| XP-D02 | `tests/csharp/Services/ProgressionCoreParityTest.cs` | `EquivalentCurves_WithSameState_ProduceIdenticalOutputs` | Equivalent-curve determinism baseline |

## Gate Output Requirement

1. End Pass 2 report with an explicit request for Pass 3 approval.
2. If approval not provided, state: `blocked waiting approval`.
