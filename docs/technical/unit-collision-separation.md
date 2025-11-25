# Unit Collision & Separation System

**Status:** IMPLEMENTED
**Last Updated:** 2025-01-25
**File:** `scripts/units/unit_3d.gd`

---

## Overview

Prevents units from stacking on top of each other during movement. Uses a steering behavior approach (no pathfinding) optimized for card game unit counts (~20-30 units).

---

## Core Components

### 1. Separation Steering

Soft repulsion from nearby units. Runs every physics frame during movement.

**Constants:**
```gdscript
const SEPARATION_MULTIPLIER: float = 1.5   # Trigger distance = collision_radius * this
const SEPARATION_STRENGTH: float = 2.0     # Base push strength
const LATERAL_SEPARATION_BOOST: float = 3.0  # Extra sideways spread
```

**How it works:**
- Each unit checks distance to all other units (O(n²) - acceptable for 20-30 units)
- If within `collision_radius * SEPARATION_MULTIPLIER`, apply push force
- Push strength inversely proportional to distance (closer = stronger)
- **Does NOT separate from current attack target** (so units can approach enemies)

### 2. Lateral Separation Boost

Prevents units from bunching up when chasing the same target.

**Problem:** Units moving toward the same target compress sideways, creating tight "crevices" where units get stuck.

**Solution:** Amplify separation force on the axis perpendicular to movement direction.

**Algorithm:**
1. Calculate movement direction (toward target)
2. Calculate lateral direction (perpendicular: `Vector3(-move_dir.z, 0, move_dir.x)`)
3. For each nearby unit, check how "aligned" it is (directly ahead/behind vs to the side)
4. Add extra lateral push based on alignment (more push when units are in front/behind)
5. Use instance ID to decide push direction (half go left, half go right) to prevent oscillation

```gdscript
# In _calculate_separation_force():
if move_dir.length_squared() > 0.01:
    var lateral_dir: Vector3 = Vector3(-move_dir.z, 0, move_dir.x)
    var forward_alignment: float = abs(push_dir.dot(move_dir))
    var lateral_sign: float = 1.0 if (get_instance_id() % 2 == 0) else -1.0
    separation += lateral_dir * lateral_sign * forward_alignment * strength * LATERAL_SEPARATION_BOOST
```

### 3. Overlap Correction

Hard correction after movement for severe overlaps.

**When:** After `move_and_slide()` in `_move_towards_target()`

**How:** If two units overlap (distance < sum of collision radii), push both apart by half the overlap amount.

### 4. Blocked Detection & Flanking

When units can't make progress, they try to go around.

**Constants:**
```gdscript
const BLOCKED_THRESHOLD: float = 0.3       # Seconds before flanking kicks in
const BLOCKED_MOVE_THRESHOLD: float = 0.1  # Min movement/sec to not be "blocked"
const FLANK_STRENGTH: float = 1.2          # Lateral force when blocked
```

**Flanking behavior:**
1. Track `_blocked_time` - how long unit hasn't moved
2. After 0.3s blocked, calculate which side (left/right) has fewer nearby units
3. Apply lateral force toward the clearer side
4. Progressive angle increase: if still stuck, widen the angle (90° → 105° → 120° → 135°)
5. Reset all state when movement resumes

**State variables:**
```gdscript
var _blocked_time: float = 0.0
var _flank_angle: float = 90.0      # Current flanking angle
var _flank_direction: int = 0       # -1 = left, 1 = right, 0 = not chosen
var _flank_progress_timer: float = 0.0
```

---

## Per-Unit Configuration

Each unit has a `collision_radius` property (exported):

| Unit Type | Typical Radius | Notes |
|-----------|---------------|-------|
| Small (slimes) | 0.3 | Swarm units |
| Medium (recruits) | 0.4-0.5 | Standard units |
| Large (tanks) | 0.5-0.6 | Tanky units |

---

## Performance

- **Complexity:** O(n²) where n = number of units
- **Acceptable for:** ~20-30 units (typical card game)
- **Optimizations:**
  - Early exit when few units
  - Squared distance check before sqrt()
  - Skip dead units and self

---

## Testing

Test scene: `scenes/battlefield/dev/test_collision.tscn`

**Features:**
- Spawn player units (green slimes) and enemy units (pink slimes)
- Cluster spawn buttons (5 units at same position)
- Clear all units button
- Infinite mana for easy testing

---

## Tuning Guide

| Problem | Adjust |
|---------|--------|
| Units stack on top of each other | Increase `SEPARATION_STRENGTH` |
| Units spread too far apart | Decrease `SEPARATION_STRENGTH` or `SEPARATION_MULTIPLIER` |
| Units bunch up when chasing | Increase `LATERAL_SEPARATION_BOOST` |
| Units oscillate side-to-side | Decrease `LATERAL_SEPARATION_BOOST` |
| Units get stuck in clumps | Increase `FLANK_STRENGTH` or decrease `BLOCKED_THRESHOLD` |
| Flanking looks jittery | Increase `FLANK_PROGRESS_INTERVAL` |

---

## Related Files

- `scripts/units/unit_3d.gd` - Main implementation
- `scripts/battlefield/battlefield_constants.gd` - Spawn position utilities
- `scripts/ui/spawn_preview.gd` - Visual preview during card drag

---

## Future Improvements

- Consider spatial partitioning if unit count exceeds ~50
- Add terrain/obstacle awareness (currently only avoids other units)
- Per-unit separation preferences (some units might want to clump)
