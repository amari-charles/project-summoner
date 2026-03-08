# Unit Movement, Collision, and Facing

**Status:** Implemented (Simulation layer)  
**Last Updated:** 2026-03-08

## Overview

Unit movement uses a deterministic simulation pipeline:

1. **Behavior** produces a movement mode and optional target (`SimBehavior`)
2. **Intent generation** produces a `MovementIntent` (`desiredVelocity`, `desiredFacingDirection`)
3. **Blocked navigation assist** may inject temporary yield/escape intent during low-progress pursuit (`BlockedNavigationController`)
4. **ORCA** resolves collision-safe velocity (`OrcaAvoidance`)
5. **Position integration** applies safe velocity and battlefield bounds clamp
6. **Facing controller** updates orientation with hysteresis/hold-time (`FacingController`)
7. **Overlap correction** performs a position-only safety pass (`OverlapCorrection`)

This keeps responsibilities separated:
- behavior decides intent,
- ORCA decides collision-safe velocity,
- facing is stabilized independently from velocity jitter.

## Files

- `scripts/csharp/Battle/Simulation/Movement/SimMovement.cs`
- `scripts/csharp/Battle/Simulation/Movement/MovementIntent.cs`
- `scripts/csharp/Battle/Simulation/Movement/MovementIntentResolver.cs`
- `scripts/csharp/Battle/Simulation/Movement/DirectIntentGenerator.cs`
- `scripts/csharp/Battle/Simulation/Movement/ContextIntentGenerator.cs` (optional strategy)
- `scripts/csharp/Battle/Simulation/Movement/OrcaAvoidance.cs`
- `scripts/csharp/Battle/Simulation/Movement/OverlapCorrection.cs`
- `scripts/csharp/Battle/Simulation/Movement/FacingController.cs`

## Facing Stability

Facing updates no longer read raw `velocity.X > 0` directly.  
`FacingController` applies:

- X dead-zones (ignore tiny directional noise)
- strafe target-X dead-zone near vertical alignment
- short facing switch hold timer

This prevents rapid left-right flipping when units are close and ORCA/overlap corrections oscillate.

## Blocked Clump Handling

ORCA is local collision avoidance, not route planning. In dense clumps, units can repeatedly
receive safe velocities that stall progress. `BlockedNavigationController` addresses this by:

- tracking progress toward current target over time
- entering a short deterministic **yield** when blocked
- then issuing a temporary deterministic **escape side-step** intent
- returning to normal intent generation once escape timer expires
- clearing transient blocked state when behavior is `MovementResult.None` (attack/idle/stun)

This avoids indefinite pushback ping-pong while keeping ORCA as the single collision-safety layer.

## Notes

- `ContextSteering` is now an **intent generation strategy**, not a separate collision solver layer.
- Strategy selection is data-driven via `MovementIntentStrategy` on `SimUnitTemplate`/`UnitData` and assigned from `UnitDefinitions` targeting profiles.
- ORCA remains the single collision-avoidance authority.
- Overlap correction is a safety net and does not feed correction displacement back into next-frame desired intent.
