# Known Bugs

This document tracks known bugs and issues in Fateforged.

For resolved bugs, see [bugs-resolved.md](bugs-resolved.md).

**Note:** When resolving a bug, move it to `bugs-resolved.md` with the resolution date and details.
**Tracker Sync (2026-03-05):** Reviewed against `bugs-resolved.md`; moved Puff target-switch and Wisp multi-target bugs to resolved based on post-refactor validation.
**Audit Sync (2026-03-05, evening):** Re-opened Puff pivot/flip bug after repro confirmation; migrated to metadata-driven pivot alignment and moved to resolved after validation. Blocked-idle and headless leak issues remain active pending explicit repro closure.
**Tracker Sync (2026-03-08):** Blocked-idle issue moved to verification after movement pipeline + blocked-nav reset fixes and deterministic repro coverage landed; headless leak remains open/cosmetic.
**Tracker Sync (2026-03-08, late):** Added resolved biome/checkerboard regression caused by StringName coercion mismatch to `bugs-resolved.md` (PR `#290`).

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
- scripts/battle/vfx/vfx_manager.gd
- scripts/csharp/Battle/View/EntityManager.cs (HP bar lifecycle)
- scripts/csharp/Battle/Simulation/Combat/SimProjectile.cs

---

#### Puff Units Get Stuck in Idle When Blocked by Other Units
**Status:** In Verification
**Reported:** 2026-01-05
**Component:** Units / Pathfinding / Movement

![Units stuck in idle when blocked](images/bug-units-stuck-idle-blocked.png)

**Description:**
Puff units get stuck in idle state when other characters are blocking their path. They don't attempt to move forward or find an alternate route to get into attack range. Affects units at both top and bottom of formations - possibly stuck in pathfinding mode.

**Expected Behavior:**
Units should navigate around obstacles or push forward to find a valid attack position.

**Current Behavior:**
- Units remain stuck in idle animation
- They don't attempt to path around blocking units
- Affects both top and bottom units in formation (not just back units)
- Units may be stuck in pathfinding state rather than truly idle
- Units have valid targets but can't reach them

**Impact:**
Reduces effective army size as blocked units don't contribute to combat.

**Possible Causes:**
- Pathfinding giving up too early when blocked
- No "push through" or flanking behavior when stuck
- Collision detection preventing movement entirely
- Target acquisition succeeding but movement failing

**Verification Update (2026-03-08):**
- Merged movement pipeline + blocked-nav reset edge-case fix (`27462750`)
- Added deterministic regression test coverage (`tests/csharp/Simulation/BlockedUnitReproTest.cs`)
- Remaining closure criteria: explicit manual repro confirmation in live battle scenarios (mixed formations/top-bottom lanes)

**Related Files:**
- scripts/csharp/Battle/View/UnitVisual.cs (visual shell / movement sync)
- scripts/csharp/Battle/Simulation/SimBehavior.cs (behavior logic, formerly in RangedUnit3D)
- Blocked detection / flanking logic

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
*Last Updated: 2026-03-08 (late) - Added missing resolved regression entry for biome/checkerboard visuals (PR #290)*
