# HP Bar Visibility Fix - Complete Analysis and Solution

## Problem Identified
HP bars were not showing for units even after taking damage due to a **pooling bug** where settings from one entity type (bases) were contaminating bars reused for other entities (units).

## Root Cause Analysis

### The Bug Flow:
1. **HP bars are pooled** for performance - they get reused between different entities
2. **Base3D explicitly sets** `show_on_damage_only = false` (bases always show HP bars)
3. **When a base is destroyed**, its HP bar returns to the pool via `reset()`
4. **The bug**: `reset()` was NOT resetting `show_on_damage_only` back to default
5. **When that bar is reused for a unit**, it still has `show_on_damage_only = false`
6. **Result**: Units get bars that never show when damaged because the visibility logic is broken

### Evidence from Logs:
```
FloatingHPBar.update_hp: current=990.000000, max=1000.000000, percent=0.990000, show_on_damage_only=false, offset_y=24.337500, global_pos=(10.000000, 24.337500, 0.000000), visible=false
FloatingHPBar: show_on_damage_only is false, not changing visibility
```
The bar thought it should always be visible (`show_on_damage_only=false`) but the visibility toggle logic wasn't triggering.

## Solution Implemented

### 1. Fixed `FloatingHPBar.reset()` function
**File**: `/Users/amaricharles/Code/project-summoner/scripts/ui/floating_hp_bar.gd`

Added critical property resets to prevent setting contamination:
```gdscript
# CRITICAL: Reset all configurable properties to defaults
# This prevents settings from one entity (e.g., bases) from leaking to others
show_on_damage_only = true  # Units should hide bars when at full HP
bar_width = 0.8  # Default unit bar width
bar_height = 0.08  # Default bar height
offset_y = 3.2  # Default height above unit
fade_delay = 3.0  # Default fade delay
fade_duration = 0.5  # Default fade duration
```

### 2. Added safeguard in `HPBarManager`
**File**: `/Users/amaricharles/Code/project-summoner/scripts/ui/hp_bar_manager.gd`

Explicitly ensures units get correct defaults when no settings provided:
```gdscript
# IMPORTANT: Ensure defaults are set for units
# The reset() function handles this, but we explicitly set show_on_damage_only
# here if not provided to guarantee units get the right behavior
if not settings.has("show_on_damage_only"):
    # Default: units hide bars when at full HP, bases always show them
    bar.show_on_damage_only = true
```

## Testing Instructions

1. **Start the game** and enter any battle
2. **Let enemies damage your units** - HP bars should appear when units take damage
3. **Wait 3 seconds** after damage stops - HP bars should fade out
4. **Heal units to full HP** - HP bars should hide immediately
5. **Attack enemy base** - Base HP bar should always be visible
6. **After base is destroyed**, spawn new units - their HP bars should work correctly

## Why This Solution Is Robust

1. **Pooling-aware**: Properly resets ALL configurable properties when bars return to pool
2. **Double safeguard**: Both `reset()` and `create_bar_for_unit()` ensure correct defaults
3. **Explicit documentation**: Comments explain the critical nature of these resets
4. **Future-proof**: Any new properties added to HP bars should also be added to reset()

## Additional Notes

The scene file (`floating_hp_bar.tscn`) already has `show_on_damage_only = true`, but this doesn't help with pooled bars since they bypass scene instantiation after initial creation.

The fix ensures that regardless of how a bar was previously used, it will always have the correct defaults when assigned to a new entity.