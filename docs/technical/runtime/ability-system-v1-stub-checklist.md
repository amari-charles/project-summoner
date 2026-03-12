# Ability System V1 Stub Checklist

**Status:** PASS 2+3 COMPLETED  
**Initiative:** `ability-system-v1`  
**Domain:** `runtime`  
**Last Updated:** 2026-03-12

## Types Created
1. `UnitAbilityConfig` - authoring contract for unit ability behavior.
2. `ProjectileImpactConfig` + `ProjectileStatusConfig` - ranged impact payload contract.
3. `UnitAbilityState` - runtime state for deterministic per-unit ability ticking.
4. `AbilityActivatedEvent` + `StatusAppliedEvent` - simulation event surface for ability/status feedback.
5. `SimAbilityOrchestrator` - simulation subsystem to execute unit abilities each tick.

## Interfaces Created
1. `ISimEventVisitor.Visit(AbilityActivatedEvent)` - visitor contract for ability activation events.
2. `ISimEventVisitor.Visit(StatusAppliedEvent)` - visitor contract for status payload events.

## Wiring Points Updated
1. `UnitDefinition -> UnitDefinitions.BuildSimTemplate` now maps abilities + ranged impact payloads.
2. `SimUnitTemplate -> Simulation.SpawnUnitsFromCard -> UnitData` now carries ability/runtime payload fields.
3. `Simulation.Tick(...)` now calls `SimAbilityOrchestrator.Tick(...)` before projectile tick.
4. `SimBehavior` projectile spawn now forwards affinity/impact/status payload fields.
5. `SimProjectile` now applies affinity filters, heal impacts, and status payload stacking calls.

## Legacy Paths Removed or Disabled
1. No legacy deletion in this initiative - retained by design.
2. Conflicting behavior prevented by defaults (`Enemies` + `Damage` + `Status=None`), preserving old runtime semantics.

## Compile-Safe Stub Behavior Checks
1. All new fields default to backward-compatible behavior when unset.
2. Ability subsystem no-ops for units with empty loadouts.
3. Visitor/event additions compile across view/session dispatch.

## Test Skeleton Coverage Map

| Case ID | Skeleton Test File | Test Name | Notes |
|---|---|---|---|
| ABIL-001 | `tests/csharp/Simulation/Abilities/AbilityProjectileIntegrationTest.cs` | `RockThrower_ArcProjectile_LandsAfterFlight` | Implemented |
| ABIL-002 | `tests/csharp/Simulation/Abilities/AbilityAllyTargetingTest.cs` | `HealerProjectile_TargetsLowestHpAlly_AndHealsOnHit` | Implemented |
| ABIL-003 | `tests/csharp/Simulation/Abilities/AbilitySpellHealTest.cs` | `HealingField_HealsAlliesInRadius_AndDoesNotAffectEnemies` | Implemented |
| ABIL-004 | `tests/csharp/Simulation/Abilities/AbilityTauntTest.cs` | `TauntPulse_UsesSoftOverride_WhenExistingForcedTargetIsActive` | Implemented |
| ABIL-005 | `tests/csharp/Simulation/Abilities/AbilityStatusDotTest.cs` | `PoisonProjectile_ReapplyStacksPotency_UpToMaxStacks` | Implemented |
| ABIL-006 | `tests/csharp/Simulation/Abilities/AbilityPierceLineTest.cs` | `LaserProjectile_PiercesMultipleEnemies` | Implemented |
| ABIL-007 | `tests/csharp/Session/AbilityEventEmissionTest.cs` | `LocalSession_EmitsAbilityAndStatusEvents_FromSimulationTick` | Implemented |
| ABIL-008 | `tests/csharp/Simulation/Abilities/AbilityProjectileIntegrationTest.cs` | `LegacyMeleeAttack_NoAbilityLoadout_StillDamagesTarget` | Implemented |
