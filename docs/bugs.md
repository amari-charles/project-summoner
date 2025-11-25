# Known Bugs

This document tracks known bugs and issues in Project Summoner.

---

## Active Bugs

### 🔴 HIGH PRIORITY


#### Projectile Targeting on Moving Units
**Status:** Open
**Reported:** 2025-01-11
**Component:** Combat / Projectiles
**Type:** Gameplay Bug

**Description:**
Projectiles do not properly track or predict the position of moving units, causing misses or incorrect targeting.

**Expected Behavior:**
- Projectiles should accurately hit moving targets
- Should use prediction or continuous tracking
- Hit detection should be reliable

**Current Behavior:**
- Projectiles may miss moving units
- Targeting appears to use initial position only
- Inconsistent hit detection on mobile targets

**Impact:**
- Affects combat reliability and feel
- Makes ranged units less effective than intended
- Creates frustrating player experience with missed shots

**Proposed Solution:**
- Implement target position prediction based on movement velocity
- Add continuous target tracking for homing projectiles
- Improve hit detection for fast-moving targets

**Related Files:**
- `scenes/projectiles/` - Projectile scripts
- Combat system targeting logic

**Notes:**
- High priority - affects core combat mechanics
- May require physics adjustments

#### Ranged Units Perpetually Miss Targets at Melee Range
**Status:** Open
**Reported:** 2025-01-12
**Component:** Combat / Ranged Attacks
**Type:** Gameplay Bug

**Description:**
When a melee unit (e.g., slime) gets directly on top of a ranged unit (e.g., archer), the archer perpetually misses even though the target is stationary and extremely close.

**Expected Behavior:**
- Ranged units should be able to hit targets at any distance, including very close range
- Projectiles should hit stationary targets reliably
- Close-range targets should be easier to hit, not harder

**Current Behavior:**
- Archer misses slime repeatedly when slime is on top of archer
- Target is stationary but projectiles still miss
- Appears to be a targeting or projectile spawn issue at close range

**Impact:**
- Makes ranged units ineffective against melee attackers
- Creates frustrating combat scenarios
- Breaks core game balance (ranged units should have weakness but not be completely useless)

**Proposed Solution:**
- Check projectile spawn position and initial trajectory at close range
- May need minimum projectile travel distance before hit detection
- Could be collision layer issue or spawn point inside target hitbox
- Consider adding melee fallback attack for ranged units at very close range

**Related Files:**
- `scripts/units/unit_3d.gd` - Unit combat logic
- `scripts/projectiles/projectile_3d.gd` - Projectile spawn and movement
- Ranged unit scenes (archer_3d.tscn, etc.)

**Notes:**
- High priority - severely impacts combat balance
- Affects all ranged vs melee matchups
- May be related to ProjectileTargetPoint position or projectile collision setup

#### Battles Not Working on First Play with Dialogue
**Status:** FIXED
**Reported:** 2025-01-24
**Resolved:** 2025-01-24
**Component:** Battle System / Dialogue / Event Sequencer
**Type:** Gameplay Bug

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

**Workarounds (before fix):**
- Pausing and unpausing seems to trigger deck reload (partial fix)
- Quitting to menu and restarting temporarily fixes it
- Quitting again and loading brings back the bugged state

**Related Files:**
- `scripts/services/dialogue_manager.gd:305` - Added `_is_system_ready = false` in reset()
- `scripts/core/battle_dialogue_controller.gd` - Calls EventSequencer.play_sequence()
- `scripts/services/event_sequencer.gd:196-207` - Checks is_system_ready() before dialogue

### 🟡 MEDIUM PRIORITY

#### Charge Spell Not Attacking - Only Moving to Destination
**Status:** FIXED
**Reported:** 2025-01-24
**Resolved:** 2025-11-24
**Component:** Spells / Charge Ability
**Type:** Gameplay Bug

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

#### Battle Marked Complete When Starting Event Sequence
**Status:** FIXED (Safeguard Added)
**Reported:** 2025-01-22
**Resolved:** 2025-11-24
**Component:** Campaign / Battle System
**Type:** Progression Bug

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

**Verification:**
- Battles use BattleContext, NOT EventContext
- RewardScreen only loads after verified victory (via game_controller.gd or battle_context.gd)
- complete_battle() is only called in RewardScreen after win condition is met

**Related Files:**
- `scripts/ui/event_screen.gd:46-51` - Added _exit_tree cleanup
- `scripts/ui/reward_screen.gd:74-75` - Where battle completion is triggered
- `scripts/core/battle_context.gd:119-127` - Where victory triggers reward screen

**Notes:**
- If this bug reoccurs, check for additional signal connections to EventSequencer
- Consider adding debug logging around complete_battle() calls

#### Battle Rewards Not Validated Against Configuration
**Status:** FIXED
**Reported:** 2025-01-22
**Resolved:** 2025-11-24
**Component:** Rewards / Campaign System
**Type:** Data Validation Bug

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

**Notes:**
- Validation logs push_error() which shows in debug console
- Invalid rewards will still be displayed but grant will fail gracefully
- Future: Consider adding editor-time validation for faster feedback

#### VFX Pooling System Lacks Resource Isolation
**Status:** Open
**Reported:** 2025-01-15
**Component:** VFX / Pooling System
**Type:** Architecture Issue

**Description:**
The VFX pooling system doesn't properly isolate shared resources (meshes, materials) between pooled instances. Modifying properties like mesh.size affects all instances using that resource, causing bugs when VFX objects are reused.

**Expected Behavior:**
- Pooled VFX instances should be completely independent
- Modifying resources on one instance shouldn't affect others
- Resources should be properly duplicated or instances should use scaling/transforms instead
- Reset logic should restore all modified properties

**Current Behavior:**
- Shared resources (QuadMesh, materials) are modified directly
- Changes persist across pooling cycles
- Workarounds required (using scale instead of mesh.size)
- Each VFX needs careful consideration of what can/can't be modified

**Impact:**
- Creates subtle bugs that only appear on second+ use of pooled VFX
- Developers must remember to use workarounds
- Makes VFX system error-prone and harder to maintain
- Increases cognitive load for VFX development

**Proposed Solution:**
- Implement resource duplication for pooled instances (mesh.duplicate())
- Create VFXInstance base class guidelines for safe resource modification
- Add validation/warnings when shared resources are modified
- Consider using node properties (scale, modulate) instead of resource modification
- Document best practices for VFX pooling

**Related Files:**
- `scripts/vfx/vfx_instance.gd` - Base class for pooled VFX
- `scripts/vfx/fireball_spell_vfx.gd` - Example workaround using scale
- `scripts/vfx/vfx_manager.gd` - Pooling system

**Notes:**
- Current workaround: Use node transforms (scale, modulate) instead of modifying resources
- Need comprehensive solution before creating many more VFX
- Consider making mesh/material unique per instance on spawn

#### Mana Bar Uses Hardcoded Values Instead of Hero System
**Status:** Open
**Reported:** 2025-01-14
**Component:** UI / Mana System
**Type:** Architecture Issue

**Description:**
The mana bar currently has hardcoded default values in the scene file and uses Summoner as the mana source. When the Hero system is implemented, mana should be a Hero property, not Summoner.

**Expected Behavior:**
- Mana bar should display values from Hero.mana and Hero.max_mana
- Hero should emit mana_changed signal
- No hardcoded mana values in scene files
- Mana max should be determined by Hero stats/equipment

**Current Behavior:**
- Mana is managed by Summoner class
- MANA_MAX is a constant (15.0) in Summoner
- Scene file has hardcoded "Mana: 15/15" text
- No hero system implemented yet

**Impact:**
- Creates technical debt for future Hero implementation
- Mana system needs refactoring when Hero is added
- Not critical for current functionality

**Proposed Solution:**
- Create Hero system with mana as a property
- Move mana management from Summoner to Hero
- Update ManaBar to listen to Hero.mana_changed signal
- Remove hardcoded values from mana_bar.tscn

**Related Files:**
- `scripts/ui/mana_bar.gd` - Has TODO comments
- `scripts/core/summoner_3d.gd:29` - MANA_MAX constant
- `scenes/ui/mana_bar.tscn` - Hardcoded display values

**Notes:**
- Can be deferred until Hero system implementation
- TODOs added to relevant files
- Part of larger Hero system feature work

#### Projectile Cleanup Not Working Properly
**Status:** Open
**Reported:** 2025-01-14
**Component:** Projectiles / Memory Management
**Type:** Memory Leak / Cleanup Issue

**Description:**
Projectiles are not being cleaned up properly after impact or expiration, potentially causing memory leaks or visual artifacts.

**Expected Behavior:**
- Projectiles should be returned to pool or destroyed after hitting target
- No lingering projectile nodes in scene tree
- Clean visual feedback (no ghost projectiles)
- Proper memory management with object pooling

**Current Behavior:**
- Projectiles may not be cleaned up correctly
- Possible memory leak from unreleased projectile instances
- Scene tree may accumulate orphaned projectile nodes

**Impact:**
- Performance degradation over long play sessions
- Potential memory leaks
- Visual clutter from lingering projectiles
- Affects game polish

**Proposed Solution:**
- Audit projectile lifecycle in ProjectileManager
- Ensure proper cleanup on hit/miss/expire
- Verify pool return logic works correctly
- Add safeguards for orphaned projectiles
- Consider using `queue_free()` for non-pooled projectiles

**Related Files:**
- `scripts/projectiles/projectile_manager.gd` - Pool management
- `scripts/projectiles/projectile_3d.gd` - Lifecycle logic
- `scripts/units/projectile.gd` - Legacy projectile code

**Notes:**
- Should be investigated and fixed soon
- May be related to pooling system
- Test with long battles to observe behavior

#### AI Scoring Magic Numbers Should Be Constants
**Status:** Open
**Reported:** 2025-01-06
**Component:** AI System
**Type:** Code Quality Enhancement

**Description:**
The HeuristicAI class uses many hardcoded magic numbers for card scoring and decision-making thresholds. These should be extracted to class-level constants for easier tuning and balancing.

**Expected Behavior:**
- Scoring values defined as named constants at class level
- Easy to adjust AI difficulty by tweaking a few values
- Clear documentation of what each value controls

**Current Behavior:**
- Magic numbers scattered throughout scoring functions (10.0, 15.0, 20.0, etc.)
- Difficult to tune AI behavior without searching through code
- Not immediately clear what each number represents

**Impact:**
- Low gameplay impact - AI still functions correctly
- Makes AI balancing more difficult for developers
- Harder to maintain and understand AI logic

**Proposed Solution:**
Extract to constants like:
```gdscript
const SCORE_MANA_EFFICIENCY: float = 10.0
const SCORE_SUMMON_BASE: float = 15.0
const SCORE_AGGRESSIVE_BONUS: float = 5.0
```

**Related Files:**
- `scripts/ai/heuristic_ai.gd` - Lines with scoring logic

**Notes:**
- Not urgent - can be done in future PR
- Would make AI easier to balance and tune
- Consider creating AI configuration files for different difficulty levels

---

## Resolved Bugs

### ✅ Battle Rewards Re-Granted on Replay
**Status:** Resolved
**Resolved:** 2025-01-06
**Component:** Campaign / Rewards System

**Description:**
When replaying a completed battle, the player received reward cards again.

**Solution Implemented:**
- Added `is_replay` detection in `reward_screen.gd`
- Only grants rewards if battle not already completed
- Shows "Battle Already Completed" message on replay
- Uses `campaign.is_battle_completed()` check

**Fixed In:** PR #fix/campaign-battle-cards

### ✅ Enemy AI Not Spawning in Campaign Battles
**Status:** Resolved
**Resolved:** 2025-01-06
**Component:** AI / Campaign System

**Description:**
Enemy summoner was not playing cards during campaign battles, making them impossible to lose.

**Solution Implemented:**
- Fixed autoload name mismatch (CampaignService vs Campaign)
- Fixed AIController type signature to accept both Summoner and Summoner3D
- Added dynamic AI loading in GameController3D
- AI now properly instantiated from campaign config

**Fixed In:** PR #fix/campaign-battle-cards

### ✅ Cards Reference 2D Units Instead of 3D
**Status:** Resolved
**Resolved:** 2025-01-06
**Component:** Cards / Units

**Description:**
Several card resources (archer, warrior, wall, training_dummy) referenced 2D unit scenes, breaking 3D battles.

**Solution Implemented:**
- Created 3D versions of all missing units
- Updated card resources to reference new 3D scenes
- All cards now work in 2.5D battlefield

**Fixed In:** PR #fix/campaign-battle-cards

### ✅ Debug Print Statements in Production Code
**Status:** Resolved
**Resolved:** 2025-01-06
**Component:** Code Quality

**Description:**
Multiple files contained debug print statements that should not be in production.

**Solution Implemented:**
- Removed all debug prints from scripted_ai.gd
- Removed all debug prints from game_controller_3d.gd
- Removed debug helper function `_get_hand_names()`
- Kept only push_warning/push_error for actual issues

**Fixed In:** PR #fix/campaign-battle-cards

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

*Last Updated: 2025-01-24 - Added Charge spell not attacking bug*
