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
- scripts/vfx/vfx_manager.gd
- scripts/csharp/Services/HPBarService.cs
- scripts/csharp/Projectiles/ProjectileService.cs

---

#### HP Bar Management Issues
**Status:** Open
**Reported:** 2026-01-04
**Component:** UI / HP Bar Manager

**Description:**
HP bars have multiple issues related to their lifecycle and positioning.

**Issues:**
1. **Swarm cleanup crash/bug:** When clicking "Clear Units" in debug mode, HP bars for swarm units don't clean up properly. May cause errors or orphaned UI elements.
2. **Positioning relative to units:** HP bars are not positioned correctly relative to their units. The offset or anchor point appears to be wrong.

**Expected Behavior:**
- HP bars should be removed cleanly when their units are removed
- HP bars should appear at a consistent, correct position above each unit's head

**Current Behavior:**
- Swarm unit HP bars misbehave on debug clear
- HP bar positions don't match unit visual positions

**Impact:**
Visual bugs and potential errors during development/testing.

**Related Files:**
- scripts/systems/hp_bar_manager.gd
- scripts/ui/battle/hp_bar.gd (if exists)

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

#### Puff Units Get Stuck in Idle When Blocked by Other Units
**Status:** Open
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

**Related Files:**
- scripts/csharp/Units/Unit3D.cs (movement/pathfinding logic)
- scripts/csharp/Units/RangedUnit3D.cs (Puff-specific behavior)
- Blocked detection / flanking logic

---

#### Mana Bolt Bounces on Ground Impact
**Status:** Open
**Reported:** 2026-01-11
**Component:** Projectiles / Spells

**Description:**
When mana bolt is cast with no enemies in range, it targets a position and arcs toward it. Upon hitting the ground, the projectile bounces instead of disappearing on impact.

**Expected Behavior:**
Mana bolt should disappear immediately upon hitting the ground, with appropriate impact effects.

**Current Behavior:**
The projectile bounces off the ground and continues moving, which looks unnatural.

**Impact:**
Visual bug - breaks immersion and looks unprofessional.

**Proposed Solution:**
Ensure ground collision detection properly triggers projectile expiration. May need to check the ground collision logic in `Projectile3D.cs` to ensure it triggers `ExpireWithFade()` or `ExpireImmediate()` correctly for homing projectiles with arc.

**Related Files:**
- scripts/csharp/Projectiles/Projectile3D.cs (ground collision and expiration logic)
- data/projectiles/mana_bolt.json (projectile configuration)

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

#### Camera Scroll Wheel Boundary Issues
**Status:** Open
**Reported:** 2026-01-13
**Component:** Camera / Input

**Description:**
When scrolling with the scroll wheel to zoom the camera, the camera can go past the boundary limits.

**Expected Behavior:**
Camera should respect battlefield boundaries at all zoom levels.

**Current Behavior:**
Scroll wheel zoom allows the camera view to extend past the intended battlefield boundaries.

**Impact:**
Players can see outside the play area, breaking immersion.

**Related Files:**
- Camera controller scripts
- Battlefield boundary system

---

#### Camera Right-Click Drag Boundary Issues
**Status:** Open
**Reported:** 2026-01-13
**Component:** Camera / Input

**Description:**
When panning the camera with right-click and drag, the camera can go past the boundary limits and behaves erratically.

**Expected Behavior:**
Camera panning should respect battlefield boundaries smoothly.

**Current Behavior:**
Right-click drag panning allows the camera to go past boundaries and may exhibit buggy behavior.

**Impact:**
Players can see outside the play area and experience jarring camera movement.

**Related Files:**
- Camera controller scripts
- Battlefield boundary system

---

#### Enemy Spawn Debug Mode Issues
**Status:** Open
**Reported:** 2026-01-13
**Component:** Debug Tools / Spawning

**Description:**
When using the debug unit spawner panel with "Spawn as Enemy" toggled on, units spawn incorrectly.

**Expected Behavior:**
Units should spawn on the enemy side of the battlefield when "Spawn as Enemy" is enabled.

**Current Behavior:**
Spawning is "messed up" when spawning as enemy in debug mode.

**Impact:**
Debug tool doesn't work correctly for testing enemy units.

**Related Files:**
- scripts/ui/debug/unit_spawner_panel.gd
- Battlefield spawn logic

---

*Last Updated: 2026-01-14 - Moved "Summoner Combat Interactions Broken" to resolved*
