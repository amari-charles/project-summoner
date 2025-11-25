# Resolved Bugs Archive

This document archives bugs that have been fixed. For active bugs, see [bugs.md](bugs.md).

---

## 2025-11 Fixes

### VFX Pooling System Resource Isolation
**Resolved:** 2025-11-24
**Component:** VFX / Pooling System

**Description:**
The VFX pooling system didn't properly isolate shared resources (meshes, materials) between pooled instances. Modifying properties like mesh.size or material colors affected all instances using that resource, causing bugs when VFX objects were reused.

**Solution Implemented:**
Added resource isolation helpers to `VFXInstance` base class:
- `isolate_mesh_resources(mesh_instance, isolate_mesh, isolate_materials)` - Makes a MeshInstance3D's resources unique
- `isolate_all_mesh_resources()` - Convenience method for all descendant meshes (recursive)
- Documentation in class header explaining safe patterns for pooled VFX
- Updated `fireball_spell_vfx.gd` to use the new helper

Safe patterns documented:
1. Use node transforms (scale, modulate) instead of resource properties
2. Call `isolate_mesh_resources()` in `_ready()` for nodes you'll modify
3. Create resources dynamically in code (they're unique per-instance)

**Related Files:**
- `scripts/vfx/vfx_instance.gd` - Added isolation helpers and documentation
- `scripts/vfx/fireball_spell_vfx.gd` - Uses new helper method

---

### Projectile Cleanup Not Working Properly
**Resolved:** 2025-11-24
**Component:** Projectiles / Memory Management

**Description:**
Projectiles were not being cleaned up properly after impact or expiration, causing memory leaks and orphaned nodes in the scene tree.

**Solution Implemented:**
Fixed projectile lifecycle management in ProjectileManager to ensure proper cleanup on hit/miss/expire. Projectiles are now correctly returned to pool or freed.

**Related Files:**
- `scripts/projectiles/projectile_manager.gd` - Pool management fixes
- `scripts/projectiles/projectile_3d.gd` - Lifecycle logic fixes

**PR:** #65

---

### Projectile Targeting on Moving Units
**Resolved:** 2025-11-24
**Component:** Combat / Projectiles

**Description:**
Projectiles did not properly track or predict the position of moving units, causing misses or incorrect targeting.

**Solution Implemented:**
Added target position prediction - projectiles now calculate where the target will be upon landing based on current velocity, rather than aiming at current position. This allows arc projectiles to lead moving targets.

**Related Files:**
- `scripts/projectiles/projectile_3d.gd` - Target position prediction logic

---

### Ranged Units Perpetually Miss Targets at Melee Range
**Resolved:** 2025-11-24
**Component:** Combat / Ranged Attacks

**Description:**
When a melee unit (e.g., slime) gets directly on top of a ranged unit (e.g., archer), the archer perpetually misses even though the target is stationary and extremely close.

**Root Cause:**
Arc projectiles had a fixed `arc_height` (1.5 units) regardless of distance. At close range, arrows would arc UP and OVER the target, never passing through their hitbox.

**Solution Implemented:**
Scale arc height proportionally to distance in `_move_arc()`:
- `arc_scale = clamp(distance / 5.0, 0.0, 1.0)`
- At 5+ units: full 1.5 unit arc
- At 2.5 units: 0.75 unit arc
- At 1 unit: 0.3 unit arc (essentially flat)
- Added `max(distance, 0.1)` guard against division by near-zero

**Related Files:**
- `scripts/projectiles/projectile_3d.gd:141-170` - Arc movement with scaled height

---

### Battles Not Working on First Play with Dialogue
**Resolved:** 2025-01-24
**Component:** Battle System / Dialogue / Event Sequencer

**Description:**
Battles are not functioning properly the first time they are played when dialogue or event sequences are involved. Dialogue doesn't show and enemies don't spawn on first load.

**Root Cause:**
Race condition between DialogueManager (autoload) and DialogueBox (scene node):
1. HeroSelection scene's DialogueBox calls `DialogueManager.notify_ui_connected()` setting `_is_system_ready = true`
2. When battle scene loads, DialogueManager is autoload so `_is_system_ready` stays true
3. But the OLD DialogueBox from HeroSelection is gone, NEW DialogueBox hasn't connected yet
4. EventSequencer checks `is_system_ready()`, sees true, starts dialogue immediately
5. DialogueBox misses the dialogue_started/dialogue_line_displayed signals

**Solution Implemented:**
Reset `_is_system_ready = false` in `DialogueManager.reset()` so each new scene's DialogueBox must reconnect. This ensures EventSequencer properly waits for the new DialogueBox to be ready.

**Related Files:**
- `scripts/services/dialogue_manager.gd:305` - Added `_is_system_ready = false` in reset()
- `scripts/core/battle_dialogue_controller.gd` - Calls EventSequencer.play_sequence()
- `scripts/services/event_sequencer.gd:196-207` - Checks is_system_ready() before dialogue

---

### Charge Spell Not Attacking - Only Moving to Destination
**Resolved:** 2025-11-24
**Component:** Spells / Charge Ability

**Description:**
The Charge spell (granted in first card selection tutorial) is not working correctly. Units only move to the designated spot but do not attack the nearest enemy upon arrival. Additionally, debug logs incorrectly reference "rally" instead of "charge".

**Root Cause:**
The Charge spell used `RedirectManager.TARGET_SEARCH_RADIUS` (10.0 units) to search for enemies near the charge destination. If no enemy was within 10 units of where the player dragged the arrow, no target was found and the spell did nothing.

**Solution Implemented:**
- Changed Charge spell to use a large search radius (999.0 units) to find the nearest enemy on the entire battlefield
- This differs from regular redirect (which intentionally uses a small radius for local control)
- Also added `original_redirect_point` storage for fallback targeting when the primary target dies

**Related Files:**
- `scripts/cards/card.gd:403-429` - Fixed `_apply_charge_command()` search radius

**Notes:**
- The "rally_destination" variable name in SpellTargetingManager is reused for all command spells (Rally, Guard, Charge) - this is a naming quirk but doesn't affect functionality

---

### Battle Marked Complete When Starting Event Sequence
**Resolved:** 2025-11-24
**Component:** Campaign / Battle System

**Description:**
When a battle with an event sequence is started (like charge_tutorial), the campaign system could potentially mark the battle as completed prematurely if signal connections weren't properly cleaned up.

**Investigation Results:**
- Battle completion is ONLY triggered in `reward_screen.gd` when player wins
- EventScreen was missing `_exit_tree` cleanup for EventSequencer.sequence_finished connection
- If player navigated away mid-event, stale signal connection could persist
- However, signal cleanup in Godot when node is freed should prevent this

**Solution Implemented:**
- Added `_exit_tree()` cleanup to EventScreen to explicitly disconnect from EventSequencer.sequence_finished
- ShopScreen already had proper cleanup in place
- This prevents any potential signal leak when navigating away mid-sequence

**Related Files:**
- `scripts/ui/event_screen.gd:46-51` - Added _exit_tree cleanup
- `scripts/ui/reward_screen.gd:74-75` - Where battle completion is triggered
- `scripts/core/battle_context.gd:119-127` - Where victory triggers reward screen

---

### Battle Rewards Not Validated Against Configuration
**Resolved:** 2025-11-24
**Component:** Rewards / Campaign System

**Description:**
There was no validation that the rewards displayed to the player match the battle configuration, or that reward cards actually exist in the card catalog.

**Solution Implemented:**
Added validation at two points:

1. **Startup Validation** (CampaignService._validate_battle_rewards):
   - Runs when battles are loaded in _init_battles()
   - Validates all reward_cards in all battles exist in CardCatalog
   - Logs errors with battle_id and catalog_id if invalid
   - Counts total invalid rewards and logs summary

2. **Runtime Validation** (RewardScreen._validate_rewards):
   - Runs before displaying rewards to player
   - Double-checks that reward cards still exist in catalog
   - Logs errors if player could receive invalid rewards
   - Acts as safety net for any config that slipped past startup validation

**Related Files:**
- `scripts/services/campaign_service.gd:173-201` - Startup validation
- `scripts/ui/reward_screen.gd:260-298` - Runtime validation

---

### Dialogue Speaker Names Not Properly Localized
**Resolved:** 2025-11-24
**Component:** Dialogue / Localization

**Description:**
The dialogue system had inconsistent formats - some dialogues used localization keys while others used raw text strings, causing `[MISSING:...]` warnings and broken localization.

**Solution Implemented:**
Standardized ALL 17 dialogue files to use localization keys:

1. **Dialogue .tres files** now use consistent format:
   - `character_name = "dialogue.{id}.speaker"`
   - `lines = ["dialogue.{id}.line_1", "dialogue.{id}.line_2", ...]`
   - `choice_text = "dialogue.{id}.choice_1"` (for choices)

2. **en.json** contains all dialogue text:
   ```json
   "dialogue": {
     "first_trial_intro": {
       "speaker": "Headmaster Merlin",
       "line_1": "Welcome to the training grounds, Initiate.",
       "line_2": "Your affinity chosen, your companion bound..."
     }
   }
   ```

3. **dialogue_manager.gd** simplified to just call `Loc.t()`:
   ```gdscript
   var line_text: String = Loc.t(line_key)
   var character: String = Loc.t(current_dialogue.character_name)
   ```

4. **dialogue_box.gd** updated to localize choice text:
   ```gdscript
   button.text = Loc.t(choice.choice_text)
   ```

**Related Files:**
- `scripts/services/dialogue_manager.gd` - Simplified localization
- `scripts/ui/dialogue_box.gd` - Added choice text localization
- `localization/data/en.json` - All dialogue text entries
- `resources/dialogue/*.tres` - All 17 dialogue files standardized

---

## 2025-01 Fixes

### Battle Rewards Re-Granted on Replay
**Resolved:** 2025-01-06
**Component:** Campaign / Rewards System

**Description:**
When replaying a completed battle, the player received reward cards again.

**Solution Implemented:**
- Added `is_replay` detection in `reward_screen.gd`
- Only grants rewards if battle not already completed
- Shows "Battle Already Completed" message on replay
- Uses `campaign.is_battle_completed()` check

---

### Enemy AI Not Spawning in Campaign Battles
**Resolved:** 2025-01-06
**Component:** AI / Campaign System

**Description:**
Enemy summoner was not playing cards during campaign battles, making them impossible to lose.

**Solution Implemented:**
- Fixed autoload name mismatch (CampaignService vs Campaign)
- Fixed AIController type signature to accept both Summoner and Summoner3D
- Added dynamic AI loading in GameController3D
- AI now properly instantiated from campaign config

---

### Cards Reference 2D Units Instead of 3D
**Resolved:** 2025-01-06
**Component:** Cards / Units

**Description:**
Several card resources (archer, warrior, wall, training_dummy) referenced 2D unit scenes, breaking 3D battles.

**Solution Implemented:**
- Created 3D versions of all missing units
- Updated card resources to reference new 3D scenes
- All cards now work in 2.5D battlefield

---

### Debug Print Statements in Production Code
**Resolved:** 2025-01-06
**Component:** Code Quality

**Description:**
Multiple files contained debug print statements that should not be in production.

**Solution Implemented:**
- Removed all debug prints from scripted_ai.gd
- Removed all debug prints from game_controller_3d.gd
- Removed debug helper function `_get_hand_names()`
- Kept only push_warning/push_error for actual issues
