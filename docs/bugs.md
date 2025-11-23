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

### 🟡 MEDIUM PRIORITY

#### Battle Marked Complete When Starting Event Sequence
**Status:** Open
**Reported:** 2025-01-22
**Component:** Campaign / Battle System
**Type:** Progression Bug

**Description:**
When a battle with an event sequence is started (like charge_tutorial), the campaign system incorrectly marks the battle as completed immediately, even if the player quits mid-battle or doesn't complete objectives.

**Expected Behavior:**
- Battles should only be marked complete when won or objectives completed
- Event sequence battles should track completion state properly
- Player should be able to retry event battles without it counting as completed
- If player quits without selecting reward, battle should remain incomplete

**Current Behavior:**
- Starting charge_tutorial marks it as completed immediately
- Battle appears complete even if player quits
- No tracking of whether reward was selected
- Player cannot properly retry the battle

**Impact:**
- Breaks campaign progression for tutorial/event battles
- Players can accidentally skip battles by starting and quitting
- Rewards may be lost if player doesn't select them
- Cannot test event sequences properly

**Proposed Solution:**
- Track battle completion state separately from battle start
- Only mark event battles complete after sequence finishes successfully
- Track reward selection state separately
- If reward not selected, keep battle incomplete or show reward screen again
- Add "battle_in_progress" vs "battle_completed" distinction
- Verify completion state is only set on actual victory/completion condition

**Related Files:**
- `scripts/services/campaign_service.gd` - Campaign completion tracking
- `scripts/ui/reward_screen.gd` - Reward granting logic
- `scripts/core/battle_dialogue_controller.gd` - Event sequence handling
- `scripts/services/event_sequencer.gd` - Sequence lifecycle

**Notes:**
- Likely related to how event sequences interact with battle completion
- Need to differentiate between "sequence started" and "battle completed"
- Reward selection state should be tracked in profile data

#### Battle Rewards Not Validated Against Configuration
**Status:** Open
**Reported:** 2025-01-22
**Component:** Rewards / Campaign System
**Type:** Data Validation Bug

**Description:**
There is no validation that the rewards displayed to the player match the rewards actually granted, or that both match the battle configuration. Battle configs specify rewards, but there's no guarantee the reward screen shows the correct options or that the granted rewards match what was configured.

**Expected Behavior:**
- Rewards shown in reward screen should match battle config exactly
- Rewards granted to player should match what they selected from the displayed options
- System should validate reward_cards in battle config exist in card catalog
- Error/warning if displayed rewards don't match configured rewards
- Error/warning if granted rewards don't match selected rewards

**Current Behavior:**
- No validation between battle config rewards and reward screen display
- No validation between reward screen options and granted rewards
- Possible for configuration errors to go unnoticed
- Player could see different rewards than configured
- Player could receive different rewards than selected

**Impact:**
- Silent data inconsistencies in reward flow
- Players may not receive promised rewards
- Configuration errors not caught during development
- Breaks player trust if rewards don't match promises

**Proposed Solution:**
- Add validation in reward screen: verify displayed rewards match battle config
- Add validation when granting rewards: verify granted rewards match selection
- Add validation in battle config: verify reward card IDs exist in catalog
- Log warnings/errors when mismatches detected
- Consider adding unit tests for reward flow validation

**Related Files:**
- `scripts/ui/reward_screen.gd` - Displays and grants rewards
- `scripts/services/campaign_service.gd` - Battle config with reward definitions
- `scripts/services/reward_service.gd` - Reward granting logic
- `scripts/data/card_catalog.gd` - Card existence validation

**Notes:**
- Should validate at multiple points: config load, display, grant
- Consider adding developer warnings in editor when battle rewards misconfigured
- Related to battle completion tracking bug

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

*Last Updated: 2025-01-22 - Fixed HP bar positioning, added event sequence completion bug*
