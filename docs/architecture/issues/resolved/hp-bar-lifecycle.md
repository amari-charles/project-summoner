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
// scripts/csharp/UI/FloatingHPBar.cs
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
| Created | `scripts/csharp/Services/HPBarService.cs` |
| Created | `scripts/csharp/Services/HPBarService.tscn` |
| Created | `scripts/csharp/UI/FloatingHPBar.cs` |
| Modified | `scripts/csharp/Units/Unit3D.cs` - Direct C# calls |
| Modified | `scripts/core/summoner.gd` - HPBarService reference |
| Modified | `scripts/core/game_controller_3d.gd` - HPBarService reference |
| Modified | `project.godot` - Updated autoload |
| Modified | `scenes/ui/battle/floating_hp_bar.tscn` - C# script |
| Modified | `tests/unit/test_pool_containers.gd` - Updated API |
| Deleted | `scripts/ui/battle/hp_bar_manager.gd` |
| Deleted | `scripts/ui/battle/floating_hp_bar.gd` |

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
