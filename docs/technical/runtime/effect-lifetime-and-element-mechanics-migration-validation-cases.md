# Effect Lifetime And Element Mechanics Migration Validation Cases

**Status:** PASS 3 IMPLEMENTED  
**Initiative:** `effect-lifetime-and-element-mechanics-migration`  
**Domain:** `runtime`  
**Last Updated:** `2026-03-19`  
**Companion Plan:** `effect-lifetime-and-element-mechanics-migration-plan.md`

## How To Use

1. Baseline scenarios were defined in PASS 1 and expanded for skeleton coverage in PASS 2.
2. PASS 3 converts mapped skeletons into implemented assertions and updates status.
3. Any follow-up scope should append new case IDs instead of rewriting existing IDs.

Allowed status values:
1. `Design-Covered`
2. `Implemented`
3. `Deferred`

## Case Matrix

| Case ID | Scenario | Expected Result | Test Type | Test File | Status |
|---|---|---|---|---|---|
| C01 | Timed `ActiveBuff` ticks down via typed lifetime | Buff expires at expected tick boundary | simulation | `tests/csharp/Simulation/SimEffectsLifetimeTest.cs` | Implemented |
| C02 | Persistent `ActiveBuff` does not decrement | Buff remains until explicit removal | simulation | `tests/csharp/Simulation/SimEffectsLifetimeTest.cs` | Implemented |
| C03 | Trigger payload uses typed lifetime | Trigger-applied buff lifetime matches authored kind | simulation | `tests/csharp/Simulation/SimEffectsLifetimeTest.cs` | Implemented |
| C04 | Delayed effect payload lifetime preserved | Delayed apply uses same typed lifetime as immediate | simulation | `tests/csharp/Simulation/SimEffectsLifetimeTest.cs` | Implemented |
| C05 | Legacy duration bridge compatibility | Old sentinel values map to typed lifetime correctly | unit | `tests/csharp/Simulation/LegacyDurationAdapterTest.cs` | Implemented |
| C06 | Legacy bridge round-trip | Typed lifetime converts back to old values for compatibility paths | unit | `tests/csharp/Simulation/LegacyDurationAdapterTest.cs` | Implemented |
| C07 | `FlatDamageReduction` direct hit behavior | Final damage reduced and clamped `>= 0` | simulation | `tests/csharp/Simulation/SimDamageMitigationTest.cs` | Implemented |
| C08 | `FlatDamageReduction` periodic damage behavior | Periodic damage reduced using same mitigation rule | simulation | `tests/csharp/Simulation/SimDamageMitigationTest.cs` | Implemented |
| C09 | Ally attack speed modifier | Ally attack cadence increases per buff value | simulation | `tests/csharp/Simulation/AttackSpeedModifierTest.cs` | Implemented |
| C10 | Enemy attack speed modifier | Enemy attack cadence decreases per debuff value | simulation | `tests/csharp/Simulation/AttackSpeedModifierTest.cs` | Implemented |
| C11 | Circle area targeting | Circle shape includes units by radius logic | simulation | `tests/csharp/Simulation/SpellAreaShapeResolutionTest.cs` | Implemented |
| C12 | Square area targeting | Square shape includes units by axis-aligned bounds | simulation | `tests/csharp/Simulation/SpellAreaShapeResolutionTest.cs` | Implemented |
| C13 | Passive self ability applies persistent evasion bonus | Unit gains persistent self buff on activation | simulation | `tests/csharp/Simulation/Abilities/AbilityPassiveSelfEffectTest.cs` | Implemented |
| C14 | Passive self ability applies persistent flat reduction bonus | Unit gains persistent flat reduction buff on activation | simulation | `tests/csharp/Simulation/Abilities/AbilityPassiveSelfEffectTest.cs` | Implemented |
| C15 | Wind/Earth authored cards resolve | Catalog returns expected Wind/Earth authored cards | unit | `tests/csharp/Cards/CardCatalogWindEarthContentTest.cs` | Implemented |
| C16 | Wind/Earth reward pool inclusion | Element pools include newly authored Wind/Earth summon cards | integration | `tests/csharp/Services/RewardServiceWindEarthTest.cs` | Implemented |
| C17 | Tail Wind square area behavior | Allies get positive attack speed buff and enemies get negative debuff in square area | simulation | `tests/csharp/Simulation/Abilities/AbilityWindEarthSetTest.cs` | Implemented |
| C18 | Fortify no-heal behavior | Allies receive flat damage reduction buff and no healing | simulation | `tests/csharp/Simulation/Abilities/AbilityWindEarthSetTest.cs` | Implemented |
| C19 | Targeted knockback unit ability | Unit ability knocks back nearest enemy in range deterministically | simulation | `tests/csharp/Simulation/Abilities/AbilityTargetedKnockbackTest.cs` | Implemented |

## Determinism Cases (If Applicable)

| Case ID | Seed | Inputs | Checkpoints | Hash/State Assertions | Status |
|---|---|---|---|---|---|
| D01 | `fixed-seed-1001` | Mixed spell+ability lifetime events | init/mid/end | event ordering + state hash stable | Implemented |

## Deferred Cases

| Case ID | Reason Deferred | Planned Follow-up |
|---|---|---|
| none | n/a | n/a |

## Exit Criteria Mapping

### Pass 2

1. Every required case has a planned test type and file target.
2. Every case has an allowed status value.

### Pass 3

1. Every required case is `Implemented` or `Deferred`.
2. Any deferred case includes explicit rationale and follow-up issue/pass target.
