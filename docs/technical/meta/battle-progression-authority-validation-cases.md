# Battle Progression Authority Validation Cases

**Status:** PASS 3 IMPLEMENTED
**Initiative:** `battle-progression-authority`
**Domain:** `meta`
**Last Updated:** `2026-08-05`
**Companion Plan:** `battle-progression-authority-plan.md`

These cases validate local correctness and adapter portability. They do not claim that client-reported outcomes or local saves are tamper-resistant.

## Case Matrix

| Case ID | Scenario and expected result | Test File | Status |
|---|---|---|---|
| BPA-C01 | Launch persists a non-empty attempt ID plus exact summoner, campaign, battle, and deck-card identity before navigation. | `tests/csharp/Meta/Progression/LocalProgressionAuthorityTest.cs` | Implemented |
| BPA-C02 | Victory atomically applies attempt XP, eligible first-clear state, and offers once. | `tests/csharp/Meta/Progression/LocalProgressionAuthorityTest.cs` | Implemented |
| BPA-C03 | Retrying the same victory returns persisted state without another grant or save-side mutation. | `tests/csharp/Meta/Progression/LocalProgressionAuthorityTest.cs` | Implemented |
| BPA-C04 | A replay earns XP once for its new attempt and cannot repeat first-clear rewards. | `tests/csharp/Meta/Progression/LocalProgressionAuthorityTest.cs` | Implemented |
| BPA-C05 | Defeat records its terminal result and grants no XP or first-clear reward. | `tests/csharp/Meta/Progression/LocalProgressionAuthorityTest.cs` | Implemented |
| BPA-C06 | Leaving an unfinished battle records abandonment and grants nothing. | `tests/csharp/Meta/Progression/LocalProgressionAuthorityTest.cs`, `tests/csharp/Meta/Progression/BattleOutcomeIntegrationTest.cs` | Implemented |
| BPA-C07 | Starting a new battle abandons a stale active attempt without rewards. | `tests/csharp/Meta/Progression/LocalProgressionAuthorityTest.cs` | Implemented |
| BPA-C08 | Attempt, completion, frozen offer, pending selection, and receipts survive reload unchanged. | `tests/csharp/Serialization/BattleAttemptPersistenceTest.cs` | Implemented |
| BPA-C09 | A selectable reward commits once and retry returns its existing receipt. | `tests/csharp/Meta/Progression/LocalProgressionAuthorityTest.cs` | Implemented |
| BPA-C10 | Unknown, stale, or conflicting identities/outcomes are rejected without mutation. | `tests/csharp/Meta/Progression/LocalProgressionAuthorityTest.cs` | Implemented |
| BPA-C11 | Failed attempt persistence prevents launch. | `tests/csharp/Meta/Progression/LocalProgressionAuthorityTest.cs` | Implemented |
| BPA-C12 | Failed completion persistence leaves attempt, XP, completion, rewards, and receipts unchanged. | `tests/csharp/Meta/Progression/LocalProgressionAuthorityTest.cs` | Implemented |
| BPA-C13 | Competing completion or claim calls can commit value at most once. | `tests/csharp/Meta/Progression/LocalProgressionAuthorityTest.cs` | Implemented |
| BPA-C14 | Two summoners have independent seeds, attempts, first clears, XP, and claims. | `tests/csharp/Meta/Progression/LocalProgressionAuthorityTest.cs` | Implemented |
| BPA-C15 | Automatic, selectable, mixed, and absent first-clear rewards use universal definitions and normalized output. | `tests/csharp/Meta/Progression/BattleRewardAuthorityTest.cs` | Implemented |
| BPA-C16 | Runtime reports at game over; later UI confirmation cannot grant progression. | `tests/csharp/Meta/Progression/BattleOutcomeIntegrationTest.cs` | Implemented |
| BPA-C17 | Authority contracts contain no Godot, repository, JSON-store, Nakama, or other provider types. | `tests/csharp/Meta/Progression/ProgressionAuthorityBoundaryTest.cs` | Implemented |
| BPA-C18 | Production battle paths contain no battle reward spec/pending type, legacy flags, or direct XP methods. | `tests/csharp/Meta/Progression/BattleProgressionLegacyRemovalTest.cs` | Implemented |

## Determinism and Identity Cases

| Case ID | Assertion | Test File | Status |
|---|---|---|---|
| BPA-D01 | One attempt and its derived claim IDs remain stable across completion, reload, and retry. | `tests/csharp/Meta/Progression/LocalProgressionAuthorityTest.cs`, `tests/csharp/Serialization/BattleAttemptPersistenceTest.cs` | Implemented |
| BPA-D02 | Two launches create distinct attempt/XP identities while retaining the stable first-clear occurrence. | `tests/csharp/Meta/Progression/LocalProgressionAuthorityTest.cs` | Implemented |
| BPA-D03 | A persisted resolved offer remains unchanged after reload and cannot be rerolled by changed catalog input. | `tests/csharp/Serialization/BattleAttemptPersistenceTest.cs` | Implemented |

## Deferred Cases

None.

## Verification

1. Focused progression/persistence tests: 17 passed.
2. Full C# suite: 1,174 passed.
3. Full Godot/GUT suite: 237 passed with 1,746 assertions.
