# XP Progression Redo Validation Cases

**Status:** PASS 3 COMPLETE
**Initiative:** `xp-progression-redo`
**Domain:** `runtime`
**Last Updated:** `2026-03-09`
**Companion Plan:** `xp-progression-redo-plan.md`

## How To Use

1. This matrix defines the progression behavior and contract coverage for this initiative.
2. Case status tracks implementation completion and any explicit deferrals.
3. Test file mapping is the source of truth for ongoing regression coverage.

Allowed status values:
1. `Design-Covered`
2. `Implemented`
3. `Deferred`

## Case Matrix

| Case ID | Scenario | Expected Result | Test Type | Test File | Status |
|---|---|---|---|---|---|
| XP-C01 | Card single level-up consumes exact XP cost | Level increments by 1, `xp` decreases by exact cost, `can_level_up` becomes false when leftover is insufficient | unit | `tests/csharp/Services/ProgressionXpSpendTest.cs` | Implemented |
| XP-C02 | Card multi-level carryover | Repeated level-up calls consume per-level costs and preserve carryover `xp` | unit | `tests/csharp/Services/ProgressionXpSpendTest.cs` | Implemented |
| XP-C03 | Summoner single level-up consumes exact XP cost | Level increments by 1, XP consumed exactly, trait points +1 | unit | `tests/csharp/Services/ProgressionXpSpendTest.cs` | Implemented |
| XP-C04 | Summoner multi-level carryover | Repeated successful calls level multiple times while XP remains sufficient | unit | `tests/csharp/Services/ProgressionXpSpendTest.cs` | Implemented |
| XP-C05 | Max-level no-op behavior (both domains) | `can_level_up=false`, level-up returns false, `xp_for_next_level=0`, `xp_progress=1` | unit | `tests/csharp/Services/CardProgressionContractTest.cs`, `tests/csharp/Services/SummonerProgressionServiceTest.cs` | Implemented |
| XP-C06 | Deterministic parity for same inputs | Shared engine returns identical outputs for repeated identical input state/curve | unit | `tests/csharp/Services/ProgressionCoreTest.cs` | Implemented |
| XP-C07 | UI contract: card payload fields | `GetCardProgressionInfoDict` includes correct `xp`, `xp_for_next_level`, `xp_progress`, `can_level_up` values | integration | `tests/csharp/Services/CardProgressionContractTest.cs` | Implemented |
| XP-C08 | UI contract: summoner payload fields | `GetSummonerProgressionInfo` includes correct `xp`, `xp_for_next_level`, `xp_progress`, `can_level_up` values | integration | `tests/csharp/Services/SummonerProgressionServiceTest.cs` | Implemented |
| XP-C09 | Failure: insufficient XP | Level-up returns false, no XP/level/trait-point mutation | unit | `tests/csharp/Services/ProgressionXpSpendTest.cs`, `tests/csharp/Services/ProgressionCoreTest.cs` | Implemented |
| XP-C10 | Failure: invalid progression state | Invalid state (level < 1, level > max, negative xp) returns explicit failure/no-op result deterministically | unit | `tests/csharp/Services/ProgressionCoreTest.cs` | Implemented |
| XP-C11 | Trait-point grant per successful level-up | Exactly +1 trait point for each successful level-up and never on failed attempts | unit | `tests/csharp/Services/ProgressionXpSpendTest.cs` | Implemented |
| XP-C12 | No drift between card and summoner math for equivalent curves | Given equivalent curve policies and same state, engine outputs identical progression results regardless of adapter domain | unit | `tests/csharp/Services/ProgressionCoreParityTest.cs` | Implemented |
| XP-C13 | Card rarity policy preserved | Card level-up cost/progress still applies rarity multiplier exactly as before | unit | `tests/csharp/Services/CardProgressionContractTest.cs` | Implemented |
| XP-C14 | Summoner threshold policy preserved | Summoner cost/progress matches existing thresholds and carryover behavior | unit | `tests/csharp/Services/SummonerProgressionServiceTest.cs` | Implemented |

## Determinism Cases

| Case ID | Seed | Inputs | Checkpoints | Hash/State Assertions | Status |
|---|---|---|---|---|---|
| XP-D01 | `N/A` | Fixed progression state + curve policy + repeated operation sequence | pre-level-up, post-level-up, post-second-call | State snapshot tuple `(level, xp, can_level_up, xp_progress)` is identical across repetitions | Implemented |
| XP-D02 | `N/A` | Equivalent card/summoner curve definitions with same state | same checkpoints in both adapters | Shared engine output metadata and final state are byte-for-byte equivalent in assertions | Implemented |

## Deferred Cases

| Case ID | Reason Deferred | Planned Follow-up |
|---|---|---|
| None | N/A | N/A |

## Exit Criteria Mapping

### Pass 2

1. Every required case has a test target file and skeleton mapping.
2. Stub checklist references all `XP-C*` and `XP-D*` IDs.

### Pass 3

1. Every required case is `Implemented` or `Deferred`.
2. Any deferred case includes explicit rationale and follow-up issue/target.
