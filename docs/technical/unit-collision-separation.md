# Unit Collision & Separation System

**Status:** IMPLEMENTED (C#)
**Last Updated:** 2026-01-03
**Files:**
- `scripts/csharp/Battle/Simulation/Movement/SimSteering.cs` - Steering logic
- `scripts/csharp/Battle/View/UnitVisual.cs` - Integration

---

## Overview

Prevents units from stacking on top of each other during movement. Uses a steering behavior approach (no pathfinding) optimized for card game unit counts (~20-30 units).

---

## Core Components

### 1. Separation Steering

Soft repulsion from nearby units. Runs every physics frame during movement.

**Constants:**
```csharp
private const float SeparationMultiplier = 1.5f;   // Trigger distance = separation_radius * this
private const float SeparationStrength = 2.0f;     // Base push strength
private const float LateralSeparationBoost = 3.0f; // Extra sideways spread
```

**How it works:**
- Uses SpatialGrid for O(k) proximity queries (k = local density)
- If within `separation_radius * SEPARATION_MULTIPLIER`, apply push force
- Push strength inversely proportional to distance (closer = stronger)
- **Does NOT separate from current attack target** (so units can approach enemies)

### 2. Lateral Separation Boost

Prevents units from bunching up when chasing the same target.

**Problem:** Units moving toward the same target compress sideways, creating tight "crevices" where units get stuck.

**Solution:** Amplify separation force on the axis perpendicular to movement direction.

**Algorithm:**
1. Calculate movement direction (toward target)
2. Calculate lateral direction (perpendicular: `new Vector3(-moveDir.Z, 0, moveDir.X)`)
3. For each nearby unit, check how "aligned" it is (directly ahead/behind vs to the side)
4. Add extra lateral push based on alignment (more push when units are in front/behind)
5. Use instance ID to decide push direction (half go left, half go right) to prevent oscillation

### 3. Overlap Correction

Hard correction after movement for severe overlaps.

**When:** After `MoveAndSlide()` in all movement methods

**How:** If two units overlap (distance < sum of collision radii), push both apart by half the overlap amount.

### 4. Blocked Detection & Flanking

When units can't make progress, they try to go around.

**Constants:**
```csharp
private const float BlockedThreshold = 0.3f;       // Seconds before flanking kicks in
private const float BlockedMoveThreshold = 0.1f;   // Min movement/sec to not be "blocked"
private const float FlankStrength = 1.2f;          // Lateral force when blocked
```

**Flanking behavior:**
1. Track `_blockedTime` - how long unit hasn't moved
2. After 0.3s blocked, calculate which side (left/right) has fewer nearby units
3. Apply lateral force toward the clearer side
4. Progressive angle increase: if still stuck, widen the angle (90° → 105° → 120° → 135°)
5. Reset all state when movement resumes

---

## Architecture

The steering logic is encapsulated in the `SimSteering` class:

```csharp
public class SimSteering
{
    public Vector3 CalculateSeparationForce(UnitVisual unit, Node3D? currentTarget);
    public Vector3 CalculateFlankForce(UnitVisual unit, Node3D? currentTarget, float delta);
    public void CorrectOverlaps(UnitVisual unit);
    public void UpdateBlockedState(UnitVisual unit, float delta);
    public void Reset();
}
```

Each `UnitVisual` has a `_steering` instance and calls it from movement methods:

```csharp
// In MoveTowardTarget():
Vector3 separation = _steering.CalculateSeparationForce(this, CurrentTarget);
Vector3 flank = _steering.CalculateFlankForce(this, CurrentTarget, delta);
Vector3 finalDir = (direction * MoveSpeed + separation + flank).Normalized();
Velocity = finalDir * MoveSpeed;
MoveAndSlide();
_steering.CorrectOverlaps(this);
_steering.UpdateBlockedState(this, delta);
```

---

## Per-Unit Configuration

Each unit has a `SeparationRadius` property (exported) that controls movement spacing:

| Unit Type | Typical Radius | Notes |
|-----------|---------------|-------|
| Small (slimes) | 0.3 | Swarm units |
| Medium (recruits) | 0.4-0.5 | Standard units |
| Large (tanks) | 0.5-0.6 | Tanky units |

**Note:** `SeparationRadius` is distinct from `HurtboxRadius` (combat hit detection). A unit can have a small separation radius (units can get close) but a larger hurtbox (easy to hit).

---

## Performance

- **Complexity:** O(k) per unit where k = local unit density (via SpatialGrid)
- **Acceptable for:** ~50+ units (SpatialGrid uses 10x8 grid cells)
- **Optimizations:**
  - Spatial partitioning via SpatialGrid
  - Squared distance check before sqrt()
  - Skip dead units and self

---

## Tuning Guide

| Problem | Adjust |
|---------|--------|
| Units stack on top of each other | Increase `SeparationStrength` |
| Units spread too far apart | Decrease `SeparationStrength` or `SeparationMultiplier` |
| Units bunch up when chasing | Increase `LateralSeparationBoost` |
| Units oscillate side-to-side | Decrease `LateralSeparationBoost` |
| Units get stuck in clumps | Increase `FlankStrength` or decrease `BlockedThreshold` |
| Flanking looks jittery | Increase `FlankProgressInterval` |

---

## Related Files

- `scripts/csharp/Battle/Simulation/Movement/SimSteering.cs` - Steering implementation
- `scripts/csharp/Battle/View/UnitVisual.cs` - Integration in movement methods
- `scripts/csharp/Battle/Simulation/Subsystems/SimSpatialGrid.cs` - Spatial queries for nearby units
