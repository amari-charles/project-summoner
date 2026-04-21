# Effect Lifetime And Element Mechanics Migration Stub Checklist

**Status:** PASS 2 CHECKLIST  
**Initiative:** `effect-lifetime-and-element-mechanics-migration`  
**Domain:** `runtime`  
**Last Updated:** `2026-03-19`

## Types Created

1. `EffectLifetimeKind` - typed lifetime intent (`Timed`, `Persistent`).
2. `EffectLifetime` - lifetime payload with compatibility bridge helpers.
3. `SpellAreaShape` - typed spell area (`Circle`, `Square`).

## Interfaces Created

1. `EffectLifetime` helpers:
1. `Timed(seconds)`
2. `Persistent()`
3. `FromLegacyDuration(duration)`
4. `ToLegacyDuration()`

## Wiring Points Updated

1. `EffectTypes.cs` - lifetime/effect/area enums and effect carrier fields.
2. `SpellEffectDefinition.cs` -> `SimCardData.FromCardDefinition` - area shape + lifetime wiring.
3. `SimEffects.cs` - typed lifetime tick bridge and compatibility sync.
4. `Simulation.cs` - delayed effect payload wiring for typed lifetime + area shape.
5. `Unit ability config/state` + `SimAbilityOrchestrator` - new passive self-effect ability kind as compile-safe stub.

## Legacy Paths Removed or Disabled

1. Direct reliance on `Duration == -1` in effect tick logic - disabled via typed lifetime bridge.
2. Direct reliance on `Duration > 0` as sole timed indicator - disabled via typed lifetime bridge.
3. Shield permanence sentinel comments/usages - replaced with typed lifetime assignment (legacy mirror retained for compatibility).

## Compile-Safe Stub Behavior Checks

1. Existing tests that still author `Duration` continue to run via compatibility bridge.
2. New fields default to behavior-preserving values (`Circle`, timed zero) and self-effect abilities route through generic effect apply with persistent duplicate guard.
3. New effect enum values compile without forced behavior changes in PASS 2.
4. Legacy duration fields remain mirrored for event/compatibility paths; cleanup removal is tracked as follow-up.

## Test Skeleton Coverage Map

| Case ID | Skeleton Test File | Test Name | Notes |
|---|---|---|---|
| C01 | `tests/csharp/Simulation/SimEffectsLifetimeTest.cs` | `TimedBuff_Expires_ByTypedLifetime` | new |
| C02 | `tests/csharp/Simulation/SimEffectsLifetimeTest.cs` | `PersistentBuff_DoesNotExpire_ByTypedLifetime` | new |
| C03 | `tests/csharp/Simulation/SimEffectsLifetimeTest.cs` | `TriggerPayload_UsesTypedLifetime` | new |
| C04 | `tests/csharp/Simulation/SimEffectsLifetimeTest.cs` | `DelayedPayload_UsesTypedLifetime` | new |
| C05 | `tests/csharp/Simulation/LegacyDurationAdapterTest.cs` | `LegacyDuration_Negative_MapsPersistent` | new |
| C06 | `tests/csharp/Simulation/LegacyDurationAdapterTest.cs` | `Lifetime_RoundTrip_LegacyCompat` | new |
| C07 | `tests/csharp/Simulation/SimDamageMitigationTest.cs` | `FlatDamageReduction_DirectHit_Clamped` | new |
| C08 | `tests/csharp/Simulation/SimDamageMitigationTest.cs` | `FlatDamageReduction_PeriodicHit_Clamped` | new |
| C09 | `tests/csharp/Simulation/AttackSpeedModifierTest.cs` | `AttackSpeedModifier_Ally_IncreasesCadence` | new |
| C10 | `tests/csharp/Simulation/AttackSpeedModifierTest.cs` | `AttackSpeedModifier_Enemy_DecreasesCadence` | new |
| C11 | `tests/csharp/Simulation/SpellAreaShapeResolutionTest.cs` | `SpellAreaShape_Circle_ResolvesByRadius` | new |
| C12 | `tests/csharp/Simulation/SpellAreaShapeResolutionTest.cs` | `SpellAreaShape_Square_ResolvesByBounds` | new |
| C13 | `tests/csharp/Simulation/Abilities/AbilityPassiveSelfEffectTest.cs` | `ApplySelfEffect_Evasion_Persistent` | new |
| C14 | `tests/csharp/Simulation/Abilities/AbilityPassiveSelfEffectTest.cs` | `ApplySelfEffect_FlatReduction_Persistent` | new |
| C15 | `tests/csharp/Cards/CardCatalogWindEarthContentTest.cs` | `WindEarthContentCards_Registered` | new |
| C16 | `tests/csharp/Services/RewardServiceWindEarthTest.cs` | `RewardPools_WindEarth_IncludeNewCommonUnits` | new |
| C17 | `tests/csharp/Simulation/Abilities/AbilityWindEarthSetTest.cs` | `TailWind_AppliesSquareAttackSpeedBuffAndDebuff` | pass3 extension |
| C18 | `tests/csharp/Simulation/Abilities/AbilityWindEarthSetTest.cs` | `Fortify_AppliesFlatDamageReductionWithoutHealing` | pass3 extension |
| C19 | `tests/csharp/Simulation/Abilities/AbilityTargetedKnockbackTest.cs` | `TargetedKnockback_PushesNearestEnemyInRange` | pass3 extension |
| D01 | `tests/csharp/Simulation/Determinism/EffectLifetimeDeterminismTest.cs` | `MixedLifetimeEvents_Deterministic` | new |

## Gate Output Requirement

1. End PASS 2 report with explicit PASS 3 approval request.
2. If approval not provided, state: `blocked waiting approval`.
