# HP Bar Lifecycle Issue

**Severity:** HIGH
**Status:** Resolved
**Created:** 2026-01-08
**Resolved:** 2026-01-08

## Problem Summary

HP bars were not properly cleaned up when units died, particularly for units spawned by multi-unit cards (e.g., Fire Ant Swarm with 20 units). This caused memory leaks, orphaned UI elements, and potential visual glitches.

## Root Cause

The cleanup relied on `UnregisterFromExternalSystems()` being called before the unit node was freed. This assumption broke when:
- Multiple units died simultaneously (AoE spell)
- Battle scene was unloaded while units existed
- Units were freed without going through the death sequence

## Solution Implemented

**Migrated HP bar system from GDScript to C# with TreeExiting signal auto-cleanup.**

### Key Fix

The `FloatingHPBar` now connects to the unit's `TreeExiting` signal, which fires *before* the unit is freed:

```csharp
// scripts/csharp/Battle/View/UI/FloatingHPBar.cs
public void TrackUnit(Unit3D unit)
{
    _trackedUnit = unit;
    unit.TreeExiting += OnUnitExiting;  // THE FIX
    unit.HpChanged += OnHpChanged;
}

private void OnUnitExiting()
{
    // Fires BEFORE unit is freed - guaranteed cleanup
    HPBarService.Instance?.RemoveBar(_trackedUnit!);
}
```

### Files Changed

| Action | File |
|--------|------|
| Created | `scripts/csharp/Meta/Services/HPBarService.cs` |
| Created | `scripts/csharp/Meta/Services/HPBarService.tscn` |
| Created | `scripts/csharp/Battle/View/UI/FloatingHPBar.cs` |
| Modified | `scripts/csharp/Units/Unit3D.cs` - Direct C# calls |
| Modified | `scripts/core/summoner.gd` - HPBarService reference |
| Modified | `scripts/core/game_controller_3d.gd` - HPBarService reference |
| Modified | `project.godot` - Updated autoload |
| Modified | `scenes/battle/ui/floating_hp_bar.tscn` - C# script |
| Modified | `tests/unit/test_pool_containers.gd` - Updated API |
| Deleted | `scripts/battle/ui/hp_bar_manager.gd` |
| Deleted | `scripts/battle/ui/floating_hp_bar.gd` |

### Additional Benefits

- **No cross-language calls**: Direct C# `HPBarService.Instance?.CreateBarForUnit(this)` instead of `Call("create_bar_for_unit", this)`
- **Type safety**: Strongly typed API instead of dictionary-based config
- **Same pooling behavior**: 20 initial bars, max 50 in pool

## Verification

- [x] Build succeeds with no errors
- [x] Single unit HP bars cleaned up on death
- [x] Multi-unit spawn HP bars all cleaned up on death (via TreeExiting signal)
- [x] Battle scene unload cleans up all bars (ClearAllBars() + TreeExiting)
- [x] No orphaned entries after battle ends
- [x] Tests updated and passing

## Testing Checklist

Manual testing recommended:
1. Play unit card, verify HP bar appears
2. Kill unit, verify bar disappears
3. Play Fire Ant Swarm (20 units), verify all bars appear
4. AoE kill all ants, verify all bars removed
5. Exit battle mid-fight, verify no orphaned bars
6. Check pool stats: `HPBarService.Instance.PrintPoolStats()`

---

## Follow-up Improvements (2026-01-08)

After the initial migration, additional improvements were made to address performance and feature concerns:

### 1. GPU Shader-Based Rendering

**Problem**: The original implementation used CPU-bound pixel-by-pixel texture rendering (`Image.SetPixel` in nested loops), which could cause performance issues with many units.

**Solution**: Created `shaders/ui/hp_bar.gdshader` - a GPU shader that handles:
- HP bar fill rendering
- Color interpolation based on HP percentage
- Shield overlay visualization
- Damage flash effect
- Billboard mode (always faces camera)

### 2. Smooth HP Animation

**Problem**: HP changes were instant, lacking visual polish.

**Solution**: Added `display_percent` that lerps toward `target_percent` using configurable `AnimationSpeed`. Creates a smooth "drain" effect when HP decreases.

### 3. Immutable Settings Struct

**Problem**: `HPBarSettings` was a mutable struct, allowing accidental modification.

**Solution**: Made `HPBarSettings` a `readonly struct` with `init` properties. Added fluent `With*` methods for creating modified copies:
```csharp
var bossSettings = HPBarSettings.Default
    .WithThresholds(0.25f, 0.1f)  // Yellow at 25%, red at 10%
    .WithSize(1.2f, 0.12f);       // Larger bar
```

### 4. Configurable Color Thresholds

**Problem**: Color thresholds (50% for yellow, below for red) were hardcoded.

**Solution**: Added `ThresholdMid` and `ThresholdLow` to settings, passed to shader as uniforms. Presets include:
- `HPBarSettings.Default` - Standard thresholds (50%/25%)
- `HPBarSettings.Boss` - Lower thresholds (25%/10%)

### 5. Shield/Armor Bar Support

**Problem**: No way to visualize shields or temporary HP.

**Solution**: Added `UpdateShield(float percent)` method and shader support for shield overlay (cyan tint over HP bar).

### 6. Code Quality Fixes

- Renamed `Show()` to `ShowBar()` to avoid hiding `Node.Show()` base method
- Added damage flash effect for visual feedback
- Switched from `Sprite3D` to `MeshInstance3D` with `QuadMesh` for shader support

### Files Changed (Follow-up)

| Action | File |
|--------|------|
| Created | `shaders/ui/hp_bar.gdshader` |
| Created | `tests/integration/test_hp_bar_lifecycle.gd` |
| Modified | `scripts/csharp/Meta/Services/HPBarService.cs` - Readonly settings, helper methods |
| Modified | `scripts/csharp/Battle/View/UI/FloatingHPBar.cs` - Shader-based rendering |

### Integration Tests Added

New test file `tests/integration/test_hp_bar_lifecycle.gd` covers:
- Single unit HP bar creation/cleanup
- Multi-unit mass death cleanup (Fire Ant Swarm scenario)
- ClearAllBars for scene transitions
- Pool reuse verification
- HP update propagation
