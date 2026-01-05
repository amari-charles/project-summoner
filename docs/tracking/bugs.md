# Known Bugs

This document tracks known bugs and issues in Fateforged.

For resolved bugs, see [bugs-resolved.md](bugs-resolved.md).

**Note:** When resolving a bug, move it to `bugs-resolved.md` with the resolution date and details.

---

## Active Bugs

#### RID/Resource Leaks at Exit in Headless Mode
**Status:** Open
**Reported:** 2025-01-28
**Component:** Unit Testing / Godot Headless

**Description:**
When running tests via `--headless`, Godot reports resource leaks at exit.

**Current Behavior:**
After "All tests passed", Godot outputs:
- RID allocations leaked (GodotArea3D, GodotShape3D, textures, meshes, materials)
- "Leaked instance dependency" warnings
- ObjectDB instances leaked
- Resources still in use

**Impact:**
Cosmetic only - doesn't affect test results or game runtime.

**Root Cause:**
Godot's headless renderer doesn't fully clean up resources when autoloads create 3D objects (meshes, materials, etc.) that persist until exit.

**Proposed Solution:**
- May require explicit cleanup in autoload `_exit_tree()` methods
- May be unfixable Godot behavior in headless mode

**Related Files:**
- scripts/systems/vfx_manager.gd
- scripts/systems/hp_bar_manager.gd
- scripts/systems/projectile_manager.gd

---

#### Unit Spawns at Cursor Position Instead of Preview Position
**Status:** Open
**Reported:** 2026-01-04
**Component:** Spawn System / Card Playing

**Description:**
When spawning a unit in an occupied location, the spawn preview correctly snaps to the nearest available position. However, the actual unit spawns at the original cursor position instead of the preview position, causing existing units to be displaced.

**Expected Behavior:**
Unit should spawn at the same position shown by the spawn preview (the snapped/adjusted position).

**Current Behavior:**
- Spawn preview shows correct snapped position when cursor is on an occupied spot
- Actual unit spawns at the raw cursor position
- Existing units get pushed around to make room

**Impact:**
Confusing UX - players expect the unit to appear where the preview showed.

**Proposed Solution:**
Ensure the spawn logic uses the same position calculation as the spawn preview, not the raw cursor position.

**Related Files:**
- scripts/battlefield/spawn_preview.gd (preview position calculation)
- scripts/battlefield/base_battlefield_3d.gd (actual spawn logic)

---

#### Fire Titans Cannot Attack Each Other
**Status:** Open
**Reported:** 2026-01-04
**Component:** Combat / Unit Configuration

**Description:**
Fire Titans are unable to attack each other in combat. The issue appears to be that their attack range does not extend outside their collision bodies, so when two Fire Titans stand next to each other, neither can reach the other.

**Expected Behavior:**
Fire Titans should be able to attack enemies within their attack range, including other Fire Titans.

**Current Behavior:**
Two Fire Titans adjacent to each other cannot attack one another - they appear to be perpetually out of range.

**Impact:**
Gameplay breaking for Fire Titan vs Fire Titan matchups.

**Root Cause (Suspected):**
Attack range is measured from unit center, but large units like Fire Titan have large collision radii. If `AttackRange <= CollisionRadius * 2`, the unit cannot reach outside its own body to hit adjacent units.

**Proposed Solutions:**
1. **Quick fix:** Ensure every unit's attack range exceeds its collision radius (e.g., `AttackRange > CollisionRadius + 1.0`)
2. **Structural fix:** Redefine attack range to measure from collision edge rather than center, so range represents "reach beyond body"

**Related Files:**
- scenes/units/fire_titan_3d.tscn (AttackRange and CollisionRadius values)
- scripts/csharp/Units/MeleeUnit3D.cs (attack range check logic)

---

## Bug Report Template

```markdown
#### Bug Title
**Status:** Open/In Progress/Resolved
**Reported:** YYYY-MM-DD
**Component:** System/Feature

**Description:**
Brief description of the bug

**Expected Behavior:**
What should happen

**Current Behavior:**
What actually happens

**Impact:**
How this affects gameplay/experience

**Reproduction Steps:**
1. Step 1
2. Step 2
3. ...

**Proposed Solution:**
Potential fixes or approaches

**Related Files:**
- file1.gd
- file2.gd

**Notes:**
Additional context
```

---

*Last Updated: 2025-12-17 - Moved Hand UI blocking bug to resolved*
