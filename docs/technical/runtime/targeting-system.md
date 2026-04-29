# Targeting System

## Overview

Targeting is simulation-owned and deterministic. Unit scenes do not own combat targeting behavior.

At match start:
- `UnitDefinitions.BuildSimTemplate()` maps each `UnitTargetingProfile` to sim fields
- `Simulation.SpawnUnitsFromCard()` copies those fields into `UnitData`

At runtime, targeting and movement decisions are split:
- `SimCombatStateMachine` commits to *who* to fight using `SimTargeting.AcquireTargetCommit()`
- `SimBehavior` decides *what to do* (attack, chase, strafe, idle)
- `SimMovement` executes the chosen movement through intent generation + ORCA

## Runtime Flow

1. `SimCombatStateMachine.Tick()`
- Applies forced target overrides
- Keeps the committed target while it remains valid and reachable
- Reacquires with `SimTargeting.AcquireTargetCommit()` when the current target is invalid, out of aggro, or unreachable

2. `SimBehavior.TickBehavior()`
- If no valid target: `MovementResult.Forward`
- If target in attack range and constraints pass: attack (`MovementResult.None`)
- If target in range but constraints fail: use `FallbackMovement`
- If out of range: `MovementResult.TowardTarget`

3. `SimMovement.Tick()`
- Resolves movement intent (`Direct` or `Context` strategy)
- Applies blocked-navigation assist for stalled chases
- Runs ORCA for collision-safe velocity
- Applies facing hysteresis and overlap safety correction

## Constraint Checks

Constraint validation is done by `SimTargeting`:
- Range check: horizontal distance <= `AttackRange`
- Cone check (optional): `CanAttackPosition()` using `IsFacingRight` and `ConeHalfAngle`
- Reachability guard (for cone users): `CanEverReach()` for vertical/horizontal geometry edge cases

## Fallback Movement

When a target is in range but cone/facing constraints fail, `FallbackMovement` determines behavior:

- `MoveToward`: close/reposition directly
- `Strafe`: orbit to bring target into cone
- `Idle`: do not reposition

`FallbackMovement` is evaluated in `SimBehavior`, not in presentation code.

## Configuration Surface

Primary knobs live in `UnitDefinition` and are baked into `SimUnitTemplate`:
- `TargetingProfile`
- `TargetingLayerFilter`
- `TargetingDistanceScorerWeight`
- `TargetingHealthScorerWeight`
- `TargetingConeHalfAngle`
- `TargetingCloseRangeThreshold`

Those map into `UnitData` fields such as:
- `FallbackMovement`
- `HasConeConstraint`
- `ConeHalfAngle`
- `TargetLayerFilter`
- `MovementIntentStrategy`

## Notes

- This system no longer relies on `TargetingConfigRegistry` or `UnitVisual` movement methods.
- Presentation reads simulation outcomes; it does not decide target policy or fallback behavior.
