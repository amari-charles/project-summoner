# Known Bugs

This document tracks known bugs and issues in Project Summoner.

For resolved bugs, see [bugs-resolved.md](bugs-resolved.md).

**Note:** When resolving a bug, move it to `bugs-resolved.md` with the resolution date and details.

---

## Active Bugs

#### Orphaned Nodes from Autoload Object Pools During Unit Tests
**Status:** Open
**Reported:** 2025-01-28
**Component:** Unit Testing / Object Pools

**Description:**
GUT reports ~155 orphaned nodes during test runs from autoload object pools.

**Current Behavior:**
Test output shows orphaned nodes:
- FireballExplosion, FireballTrail, FireballSpell (VFXManager pool)
- FloatingHPBar (HPBarManager pool)
- Projectile3D (ProjectileManager pool)

**Impact:**
Cosmetic - tests pass but output is noisy. Does not affect game runtime.

**Root Cause:**
Autoload managers pre-instantiate object pools at startup. These pooled objects exist outside the scene tree and are never freed during test runs.

**Proposed Solution:**
- Add cleanup methods to pool managers that can be called during test teardown
- Or configure GUT to ignore autoload-created nodes

**Related Files:**
- scripts/systems/vfx_manager.gd
- scripts/systems/hp_bar_manager.gd
- scripts/systems/projectile_manager.gd

---

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

*Last Updated: 2025-01-28 - Added unit test warning bugs (orphans, headless RID leaks)*
