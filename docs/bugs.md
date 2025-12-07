# Known Bugs

This document tracks known bugs and issues in Project Summoner.

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

#### Summoner Stats Not Cached in Campaign Mode
**Status:** Open (Pre-existing)
**Reported:** 2025-12-05
**Component:** DamageSystem / Summoner

**Description:**
Warning appears during battles: "DamageSystem: No summoner stats cached in campaign mode - trait bonuses not applied"

**Current Behavior:**
When units deal damage in campaign battles, the DamageSystem tries to apply summoner trait bonuses but finds no cached stats.

**Impact:**
Summoner damage bonuses and damage reduction traits are not being applied to combat.

**Root Cause:**
`_apply_summoner_bonuses()` in `summoner.gd` is only called for `DeckLoadStrategy.PROFILE` (line 89-91). Battles using `dev_player_deck` load the deck via `_load_dev_deck_from_config()` which bypasses summoner bonus application entirely.

**Related Files:**
- scripts/combat/damage_system.gd:290
- scripts/core/battle_context.gd (set_player_summoner_stats)
- scripts/core/summoner.gd:89-91 (_apply_summoner_bonuses only for PROFILE strategy)
- scripts/core/summoner.gd:288-290 (_load_dev_deck_from_config path)

**Fix Required:**
Either:
1. Load SummonerInstance for dev_player_deck battles and apply bonuses, OR
2. Skip summoner bonuses intentionally for dev/test battles (update DamageSystem to not warn)

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

*Last Updated: 2025-12-07 - Added summoner stats caching bug*
