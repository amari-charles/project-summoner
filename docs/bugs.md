# Known Bugs

This document tracks known bugs and issues in Fateforged.

For resolved bugs, see [bugs-resolved.md](bugs-resolved.md).

**Note:** When resolving a bug, move it to `bugs-resolved.md` with the resolution date and details.

---

## Active Bugs

#### Hand UI Area Blocks Unit Spawning
**Status:** Open
**Reported:** 2025-12-12
**Component:** UI / Battlefield Drop Zone

**Description:**
The card hand UI area at the bottom of the screen prevents unit spawning in that region of the battlefield.

**Expected Behavior:**
Players should be able to spawn units anywhere on their half of the battlefield, including areas that visually overlap with the hand UI.

**Current Behavior:**
Attempting to drag and drop a summon card to spawn a unit in the bottom portion of the battlefield (where the hand UI is rendered) fails because the hand UI intercepts the drop.

**Impact:**
Restricts valid spawn positions, limiting tactical options for players.

**Proposed Solution:**
Allow drops to pass through the hand UI to the battlefield when a card is being dragged. The hand should not block spawning during active card drag operations.

**Related Files:**
- scripts/ui/battlefield_drop_zone.gd
- scenes/ui/hand_ui.tscn

---

#### Aggro Manipulation Exploit - Units Can Be Permanently Occupied
**Status:** Open
**Reported:** 2025-12-12
**Component:** AI / Combat / Targeting

**Description:**
Players can permanently keep enemy units occupied by spawning new units, as all available enemies change aggro to target the newly spawned unit.

**Expected Behavior:**
Enemy units should maintain focus on existing threats or use intelligent target prioritization that prevents trivial aggro manipulation.

**Current Behavior:**
When a player spawns a new unit, all nearby enemy units immediately switch aggro to target it, abandoning their current targets. This allows players to repeatedly spawn cheap units to keep expensive enemy units permanently distracted.

**Impact:**
Significant balance issue. Players can exploit this to neutralize high-value enemy units with a stream of cheap fodder units.

**Proposed Solution:**
Consider one or more of:
1. Add aggro stickiness - units don't immediately switch targets
2. Implement threat priority based on unit value/danger
3. Add a cooldown before units can switch targets
4. Only allow target switching when current target dies or moves out of range

**Related Files:**
- scripts/units/unit_3d.gd (targeting logic)
- scripts/combat/ (combat system)

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

#### Large Units Render In Front of Smaller Units Despite Z-Position
**Status:** Open
**Reported:** 2025-12-13
**Component:** Rendering / Sprite3D / Depth Sorting

**Description:**
Large units (e.g., Fire Titan with 4x viewport scale) render in front of smaller units even when they are positioned behind them on the Z-axis (further from camera).

**Expected Behavior:**
Units should be sorted by their ground position (feet/base) on the Z-axis. A unit standing further back should render behind units standing in front, regardless of sprite size.

**Current Behavior:**
Large sprites appear to "pop" in front of smaller units, breaking depth perception. The sorting seems to be based on sprite center or some other point rather than the unit's ground position.

**Impact:**
Breaks visual coherence and depth perception on the battlefield. Large units look wrong when mixed with normal-sized units.

**Possible Causes:**
1. Sprite3D sorting uses sprite center rather than base/feet position
2. Larger viewport size affects the render order calculation
3. Billboard rendering interferes with depth sorting
4. Sprite not anchored to bottom of bounding box

**Related Files:**
- scripts/units/sprite_character_2d5_component.gd (sprite positioning, `_setup_sprite_alignment()`)
- scripts/units/unit_3d.gd (unit positioning)
- scenes/units/fire_titan_3d.tscn (large unit example)

**Notes:**
- First observed with Fire Titan (viewport_scale = 4.0)
- May need to adjust Sprite3D render priority or sorting offset
- Godot's Sprite3D has `render_priority` and sorting properties that may help

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

*Last Updated: 2025-12-13 - Added large unit depth sorting bug*
