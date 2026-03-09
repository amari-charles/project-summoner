# Initiative Validation Cases Template

**Status:** PASS 1 BASELINE  
**Initiative:** `<initiative-name>`  
**Domain:** `<domain>`  
**Last Updated:** `<YYYY-MM-DD>`  
**Companion Plan:** `<initiative>-plan.md`

## How To Use

1. Define all baseline scenarios in Pass 1.
2. Map each case to a planned test type and file.
3. Update status in Pass 3.

Allowed status values:
1. `Design-Covered`
2. `Implemented`
3. `Deferred`

## Case Matrix

| Case ID | Scenario | Expected Result | Test Type | Test File | Status |
|---|---|---|---|---|---|
| C01 | `<scenario>` | `<expected-result>` | `unit` | `<path>` | Design-Covered |
| C02 | `<scenario>` | `<expected-result>` | `integration` | `<path>` | Design-Covered |
| C03 | `<scenario>` | `<expected-result>` | `simulation` | `<path>` | Design-Covered |
| C04 | `<scenario>` | `<expected-result>` | `multiplayer` | `<path>` | Design-Covered |

## Determinism Cases (If Applicable)

| Case ID | Seed | Inputs | Checkpoints | Hash/State Assertions | Status |
|---|---|---|---|---|---|
| D01 | `<seed>` | `<input-stream>` | `<init/mid/end>` | `<assertion>` | Design-Covered |

## Deferred Cases

| Case ID | Reason Deferred | Planned Follow-up |
|---|---|---|
| `<case-id>` | `<reason>` | `<target-pass/issue>` |

## Exit Criteria Mapping

### Pass 2

1. Every required case has a planned test type and file target.
2. Every case has a status value.

### Pass 3

1. Every required case is `Implemented` or `Deferred`.
2. Each deferred case has explicit rationale and follow-up target.

