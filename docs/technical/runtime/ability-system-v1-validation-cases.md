# Ability System V1 Validation Cases

**Status:** PASS 3 UPDATED
**Initiative:** `ability-system-v1`
**Domain:** `runtime`
**Last Updated:** 2026-03-12
**Companion Plan:** `ability-system-v1-plan.md`

Allowed status values:
1. `Design-Covered`
2. `Implemented`
3. `Deferred`

## Case Matrix

| Case ID | Scenario | Expected Result | Test Type | Test File | Status |
|---|---|---|---|---|---|
| ABIL-001 | Rock artillery attack resolves with arc timing | Damage lands after projectile flight path; no instant hit on first projectile frame | integration | `tests/csharp/Simulation/Abilities/AbilityProjectileIntegrationTest.cs` | Implemented |
| ABIL-002 | Healer bullet selects ally target | Healer projectile prefers lowest-HP ally and applies heal on impact | simulation | `tests/csharp/Simulation/Abilities/AbilityAllyTargetingTest.cs` | Implemented |
| ABIL-003 | Healing field spell on allies in radius | Allies in radius heal; enemies in same radius are unaffected | simulation | `tests/csharp/Simulation/Abilities/AbilitySpellHealTest.cs` | Implemented |
| ABIL-004 | Taunt pulse periodic application | Enemies in pulse radius get forced target when soft-override rule allows | simulation | `tests/csharp/Simulation/Abilities/AbilityTauntTest.cs` | Implemented |
| ABIL-005 | Poison needles apply DoT with stack potency | Reapplies increase stack potency up to cap and emit status events | unit | `tests/csharp/Simulation/Abilities/AbilityStatusDotTest.cs` | Implemented |
| ABIL-006 | Piercing laser hits multiple enemies in line | Same projectile can damage multiple ordered line targets | simulation | `tests/csharp/Simulation/Abilities/AbilityPierceLineTest.cs` | Implemented |
| ABIL-007 | Ability events emitted to view/session | Tick emits `AbilityActivatedEvent` + `StatusAppliedEvent` through session event stream | integration | `tests/csharp/Session/AbilityEventEmissionTest.cs` | Implemented |
| ABIL-008 | Legacy non-ability combat unchanged | Melee combat path without ability loadout still applies expected damage | regression | `tests/csharp/Simulation/Abilities/AbilityProjectileIntegrationTest.cs` | Implemented |

## Determinism Cases

| Case ID | Seed | Inputs | Checkpoints | Hash/State Assertions | Status |
|---|---|---|---|---|---|
| D-ABIL-001 | `1337` | Fixed command stream, repeated run | frame 300/900/1500 | Deferred to dedicated deterministic snapshot-hash suite for this subsystem | Deferred |
| D-ABIL-002 | `9001` | Host/client-equivalent stream | frame 600/1200 | Deferred to multiplayer desync harness follow-up | Deferred |

## Deferred Cases

| Case ID | Reason Deferred | Planned Follow-up |
|---|---|---|
| D-ABIL-001 | No ability-specific hash checkpoint harness exists yet | Ability System V1.1 deterministic harness |
| D-ABIL-002 | Requires cross-session fixture updates beyond this scope | Ability System V1.1 multiplayer determinism pass |
| ABIL-FOLLOWUP-01 | Flaming burn shot content deferred by scope | Immediate follow-up content wave |
| ABIL-FOLLOWUP-02 | Cone cleaver engage-shape polish deferred by scope | Combat geometry follow-up |
