# Resolved Bugs Archive

This document archives bugs that have been fixed. For active bugs, see [bugs.md](bugs.md).

---

## 2026-01 Fixes

### Projectiles Cannot Hit Summoner Properly
**Resolved:** 2026-01-13
**Component:** Combat / Projectiles

**Description:**
Projectiles were unable to properly hit or damage the summoner (player character). Ranged units could not effectively attack the summoner, breaking intended combat balance.

**Root Cause:**
The Summoner node has its collision shape on a child node (`CollisionBody` - StaticBody3D), not on the Summoner itself. When projectiles detected collision with `CollisionBody`, they tried to apply damage to it. However, `CollisionBody` doesn't have a `take_damage` method - only the parent Summoner does.

**Solution Implemented:**
Added `_resolve_damageable_target()` function in `projectile_3d.gd` that traverses up the node hierarchy to find the actual damageable entity:
1. Check if the collided body itself has `take_damage`/`TakeDamage` method or IDamageable properties
2. If not, check the parent node for the same
3. Return the damageable node or null if none found
4. Updated both `_on_body_entered()` and `_on_area_entered()` to use this resolution

**Related Files:**
- `scripts/projectiles/projectile_3d.gd` - Added _resolve_damageable_target(), updated collision handlers

---

### Mana Bolt Bounces on Ground Impact
**Resolved:** 2026-01-13
**Component:** Projectiles / Spells

**Description:**
When mana bolt was cast with no enemies in range, it targeted a position but upon hitting the ground, the projectile bounced instead of disappearing on impact.

**Root Cause:**
`DamageEffect.cs` sets `targetPos.Y = ProjectileFlightHeight` (1.5) for all projectiles to enable straight-line travel. However, for homing projectiles with arc (like mana bolt), this prevented proper descent to ground level. When start_position.Y equals target_position.Y, the arc formula creates an up-and-down arc that returns to the same height (1.5), never reaching ground level (0).

**Solution Implemented:**
Added logic in `projectile_3d.gd` `initialize()` function:
- For arc projectiles (arc_height > 0) with no target unit
- If target Y was artificially elevated (within 0.5 of start Y)
- Set target Y to ground level (BattlefieldConstants.GROUND_Y)
This ensures arc projectiles properly arc down to ground where ground collision triggers expiration.

**Related Files:**
- `scripts/projectiles/projectile_3d.gd` - Added arc target Y adjustment in initialize()

---

### Units Can Move/Fly Out of Bounds
**Resolved:** 2026-01-13
**Component:** Unit Movement / Boundaries

**Description:**
Units could move or fly outside the battlefield boundaries. There was no boundary enforcement for unit movement.

**Solution Implemented:**
Added unified boundary enforcement system:
1. Created `BattlefieldBounds.cs` - C# boundary constants and utilities (X: -50 to +50, Z: -40 to +40)
2. Added `EnforceBattlefieldBounds()` in `Unit3D.ApplyMovementResult()` - clamps position after all physics
3. Added boundary clamping in `UnitSteering.CorrectOverlaps()` - prevents pushing units out of bounds

**Related Files:**
- `scripts/csharp/Constants/BattlefieldBounds.cs` - New boundary constants and utilities
- `scripts/csharp/Units/Unit3D.cs` - Added EnforceBattlefieldBounds() call
- `scripts/csharp/Movement/UnitSteering.cs` - Added boundary clamping in push logic

---

### Small Units Can Push Large Units Off Screen
**Resolved:** 2026-01-13
**Component:** Unit Movement / Collision

**Description:**
Spawning many small units (Ants) around a large unit (Fire Titan) caused the large unit to be pushed off screen. The pushed unit then got stuck perpetually trying to move back into attack range.

**Solution Implemented:**
Added mass-based push resistance to `UnitSteering.CorrectOverlaps()`:
1. Mass derived from CollisionRadius^3 (2x radius = 8x mass)
2. Push ratio calculated as `otherMass / totalMass` - lighter units pushed more
3. Example: Fire Titan (r=1.5, mass=3.375) vs Fire Ant (r=0.3, mass=0.027) = 125:1 ratio
4. Combined with boundary enforcement to prevent any unit from leaving battlefield

**Related Files:**
- `scripts/csharp/Movement/UnitSteering.cs` - Mass-based push resistance + boundary clamping
- `scripts/csharp/Constants/BattlefieldBounds.cs` - Boundary utilities

---

### Unit Spawn Boundary Can Be Bypassed When Blocked
**Resolved:** 2026-01-13
**Component:** Unit Spawning / Boundaries

**Description:**
When spawning a unit on your half of the battlefield, if there were already units blocking the intended spawn location, the system found the "closest available point." However, this closest point could end up past the player's half boundary (on the enemy's side), effectively bypassing the spawn restriction.

**Solution Implemented:**
Added team boundary enforcement to `SpawnPositionCalculator`:
1. Added `team` parameter to `CalculateFormationPositions()`, `FindSafeSpawnPosition()`, and `IsSpawnPositionSafe()`
2. `IsSpawnPositionSafe()` now checks team spawn boundary (player: X <= 0, enemy: X > 0) and battlefield bounds
3. Fallback now clamps to team's valid zone instead of returning invalid position
4. Updated `CardFactory.get_safe_spawn_positions()` and `execute_summon()` to pass team

**Related Files:**
- `scripts/csharp/Summons/SpawnPositionCalculator.cs` - Added team boundary enforcement
- `scripts/csharp/Cards/CardFactory.cs` - Updated to pass team parameter
- `scripts/csharp/Services/Interfaces/ICardFactory.cs` - Updated interface
- `scripts/csharp/Constants/BattlefieldBounds.cs` - Team spawn validation utilities

---

### Unit Spawns at Cursor Position Instead of Preview Position
**Resolved:** 2026-01-06
**Component:** Spawn System / Card Playing

**Description:**
When spawning a unit in an occupied location, the spawn preview correctly snapped to the nearest available position. However, the actual unit spawned at the original cursor position instead of the preview position, causing existing units to be displaced.

**Root Cause:**
DRY violation - safe spawn position calculation had two separate implementations:
1. `BattlefieldConstants.find_safe_spawn_position()` (GDScript) - used by preview
2. `CardFactory.FindSafeSpawnPosition()` (C#) - used by actual spawn

Additionally, preview calculated all positions at once, but actual spawn calculated sequentially (each spawned unit affected the next position).

**Solution Implemented:**
- Added `CardFactory.get_safe_spawn_positions()` as single source of truth
- Updated `BattlefieldDropZone` to call C# method for preview
- Updated `execute_summon()` to pre-calculate all positions before spawning
- Deleted `BattlefieldConstants.find_safe_spawn_position()` (GDScript duplicate)

**Related Files:**
- `scripts/csharp/Cards/CardFactory.cs` - Added get_safe_spawn_positions(), refactored execute_summon()
- `scripts/ui/battle/battlefield_drop_zone.gd` - Now calls C# method
- `scripts/battlefield/battlefield_constants.gd` - Removed duplicate functions

---

### Spawn Preview and Actual Spawning Use Separate Formation Systems
**Resolved:** 2026-01-06
**Component:** Architecture / Formation System

**Description:**
Formation logic was duplicated across multiple files (Card.gd, CardFactory.cs, FormationHelper.cs). Adding a new formation type required updating 4+ separate implementations.

**Solution Implemented:**
- CardFactory.get_formation_offset() is now the single source of truth
- Card.gd now delegates to CardFactory instead of having duplicate methods
- Deleted FormationHelper.cs (redundant)
- SpawnPreview.cs uses simple inline default for initial positioning

**Architecture Document:** See `docs/architecture/transformation-roadmap.md` for full details.

---

### Fire Swarm Units Get Stuck on Spawn
**Resolved:** 2026-01-04
**Component:** Spawning / SpatialGrid / Multi-Unit Spawn

**Description:**
When playing the Fire Swarm card (spawns 12 fire elementals), units would get stuck and not behave correctly after the spawn reveal animation completed.

**Root Cause:**
Two related issues in the multi-unit spawn flow:

1. **SpatialGrid stale cell data during spawn reveal:**
   - Units register with SpatialGrid at (0,0,0) during `_Ready()` before position is set
   - After position is set, SpatialGrid cell is not updated
   - During spawn reveal (2.5s), units are inactive so `_PhysicsProcess` returns early
   - `UpdateSpatialGridPosition()` never runs until unit activates
   - First frame after activation uses stale cell data for steering/targeting

2. **Safe spawn position checking against self:**
   - Each newly spawned unit joins UNITS group at (0,0,0) before position is set
   - `is_spawn_position_safe()` checks ALL units including the one being spawned
   - The unit could be checking against itself at the wrong position

**Solution Implemented:**
1. In `scripts/cards/card.gd`: Call `SpatialGrid.update_unit_position(unit)` immediately after setting `unit.global_position`
2. In `scripts/battlefield/battlefield_constants.gd`: Added `exclude_unit` parameter to `find_safe_spawn_position()` and `is_spawn_position_safe()` to skip the unit being spawned

**Related Files:**
- `scripts/cards/card.gd:293-297` - SpatialGrid update after position set
- `scripts/battlefield/battlefield_constants.gd:63,87` - exclude_unit parameter

---

### Aggro Manipulation Exploit - Units Can Be Permanently Occupied
**Resolved:** 2026-01-03
**Component:** AI / Combat / Targeting

**Description:**
Players could permanently keep enemy units occupied by spawning new units, as all enemies would immediately switch aggro to the newly spawned unit.

**Solution Implemented:**
Multi-layered defensive system in `scripts/csharp/Units/Unit3D.cs`:

1. **Target Lock Mechanism** (Line 38): `TargetLockDuration = 0.5f`
   - Units lock onto their current target for 0.5 seconds
   - Cannot switch targets during this window even if new units spawn

2. **Health-Weighted Scoring** (`scripts/csharp/Targeting/HealthScorer.cs`):
   - Weight = 10.0 (high priority)
   - Prioritizes damaged targets over fresh spawns
   - Cheap fodder units score lower than engaged targets

3. **UpdateTargeting Logic** (Lines 728-752):
   - Respects target lock timer before re-evaluating
   - Only switches when current target is invalid or lock expires

**Related Files:**
- `scripts/csharp/Units/Unit3D.cs` - Target lock implementation
- `scripts/csharp/Targeting/HealthScorer.cs` - Health-weighted scoring
- `scripts/csharp/Targeting/DistanceScorer.cs` - Distance scoring (weight 1.0)

---

### Large Units Render In Front of Smaller Units Despite Z-Position
**Resolved:** 2026-01-03
**Component:** Rendering / Sprite3D / Depth Sorting

**Description:**
Large units (e.g., Fire Titan with 800px ViewportPadding) rendered in front of smaller units even when positioned behind them on the Z-axis.

**Root Cause:**
`SetupSpriteAlignment()` in `SkeletalVisualComponent` positioned the Sprite3D assuming feet were at the viewport bottom. However, `ViewportPadding` creates empty space below the feet. This caused the Sprite3D origin (used for depth sorting) to be below the visual feet position, making large units sort as "in front" of where they appeared.

**Solution Implemented:**
Added `FeetOffsetPixels` property and updated `SetupSpriteAlignment()` to account for viewport padding:

```csharp
// Calculate feet offset from viewport bottom
float feetOffsetPx = FeetOffsetPixels >= 0 ? FeetOffsetPixels : ViewportPadding;
float feetOffsetWorld = feetOffsetPx * ScaleFactor.Y * _sprite3D.PixelSize;

// Position Sprite3D so feet (not viewport bottom) are at Y=0
pos.Y = (worldHeight / 2.0f) - feetOffsetWorld;
```

**Related Files:**
- `scripts/csharp/Visual/SkeletalVisualComponent.cs` - Added FeetOffsetPixels, fixed SetupSpriteAlignment()

---

### Hand UI Area Blocks Unit Spawning
**Resolved:** 2025-12-17
**Component:** UI / Battlefield Drop Zone

**Description:**
The card hand UI area at the bottom of the screen prevented unit spawning in that region of the battlefield. Attempting to drag and drop a summon card to spawn a unit where the hand UI was rendered failed because the hand UI intercepted the drop.

**Solution Implemented:**
Hide the entire hand UI when dragging a card. This ensures the battlefield drop zone receives all drop events during card drag operations. The hand reappears when the drag ends (drop or cancel).

**Related Files:**
- `scripts/ui/hand_ui.gd:261` - Hide hand on drag start
- `scripts/ui/hand_ui.gd:274-276` - Show hand on drag end via NOTIFICATION_DRAG_END

---

### Summoner Stats Not Cached in Campaign Mode
**Resolved:** 2025-12-17
**Component:** DamageSystem / Summoner / SummonerCatalog

**Description:**
Warning appeared during battles: "DamageSystem: No summoner stats cached in campaign mode - trait bonuses not applied"

**Root Cause:**
Three issues:
1. **String/StringName type mismatch**: `SummonerCatalog._catalog` uses `StringName` keys, but `get_summoner_config()` was receiving `String` parameters. GDScript 4 treats these as different types.
2. **Summoner loading coupled to deck loading**: `DeckLoader.load_player_deck()` required a deck to exist to get the summoner_id, but summoners exist independently in the profile.
3. When no decks existed (common in dev/test scenarios), summoner instance loading was skipped entirely.

**Solution Implemented:**
1. Fixed `SummonerCatalog.get_summoner_config()` to convert String to StringName before lookup
2. **Decoupled summoner loading from deck loading**: New `_load_summoner_from_profile()` function loads summoner directly via `SummonerSelection.get_active_summoner_id()` and `ProfileRepo.get_summoner_instance()`, independent of deck data
3. Summoner bonuses now applied even when using `dev_player_deck` or when no decks exist

**Related Files:**
- scripts/core/summoner.gd - New `_load_summoner_from_profile()` function
- scripts/data/summoner_catalog.gd - String to StringName conversion in lookup methods
- scripts/core/deck_loader.gd - Removed bandaid fallback, now focuses only on card loading

---

### Orphaned Nodes from Autoload Object Pools During Unit Tests
**Resolved:** 2025-11-28
**Component:** Unit Testing / Object Pools

**Description:**
GUT reported ~155 orphaned nodes during test runs from autoload object pools (VFXManager, HPBarManager, ProjectileManager).

**Root Cause:**
Autoload managers pre-instantiated object pools at startup. These pooled objects were stored in arrays *outside* the scene tree, making them "orphans" by Godot's definition. GUT detects orphans using `Node.get_orphan_node_ids()` which finds any node not in the scene tree.

**Solution Implemented:**
Keep pooled objects IN the scene tree by adding them to a dedicated pool container node:

1. **Added `pool_container: Node3D`** to each manager - a hidden child node that holds pooled objects
2. **On pool creation:** Add instances to `pool_container` instead of just storing in arrays
3. **On retrieval:** Remove from `pool_container` before adding to active container
4. **On return:** Add back to `pool_container` after removing from active container

This ensures pooled objects are always in the scene tree (either in `pool_container` or `active_container`), eliminating orphan warnings. The scene tree also automatically handles cleanup when the autoload exits.

**Related Files:**
- `scripts/vfx/vfx_manager.gd` - Added pool_container, updated _init_pools, _get_from_pool, _on_effect_finished
- `scripts/ui/hp_bar_manager.gd` - Added pool_container, updated _init_pool, create_bar_for_unit, _return_to_pool
- `scripts/projectiles/projectile_manager.gd` - Added pool_container, updated _create_pool_for, _return_to_pool

---

### Exiting Battle Mid-Fight Incorrectly Completes Event
**Resolved:** 2025-11-28
**Component:** Campaign / Battle System

**Description:**
When a player exits a battle in the middle of it (via pause menu), the event/battle could be incorrectly marked as completed, breaking campaign progression.

**Root Cause:**
The battle system lacked explicit state tracking for the battle lifecycle. When quitting mid-battle:
1. `current_battle` in profile persisted (never cleared on exit)
2. No distinction between quit/loss/victory/crash states
3. Stale state could cause confusion on subsequent battle attempts

**Solution Implemented:**
Added explicit battle state machine to BattleContext:

1. **BattleState enum** - Tracks lifecycle: `NONE → CONFIGURED → IN_PROGRESS → VICTORY/DEFEAT/ABANDONED`
2. **`abandon_battle()`** - Called on pause menu quit, clears `current_battle` from profile and pending rewards
3. **`origin_scene`** tracking - Returns player to correct scene (campaign map, game mode menu, etc.)
4. **State transitions** - GameController3D calls `start_battle()`, `end_battle_victory()`, `end_battle_defeat()`
5. **RewardScreen guard** - Validates `BattleState.VICTORY` before showing rewards, redirects otherwise

**Related Files:**
- `scripts/core/battle_context.gd` - BattleState enum, abandon_battle(), origin_scene tracking
- `scripts/ui/pause_menu.gd` - Calls abandon_battle() on quit
- `scripts/core/game_controller_3d.gd` - Sets battle states on start/end
- `scripts/ui/reward_screen.gd` - State validation guard

---

### Slimes Getting Stuck Between Life and Death
**Resolved:** 2025-11-26
**Component:** Units / Combat

**Description:**
Slime units sometimes got stuck in a state between being alive and dead. They would stop functioning properly but not fully die or despawn.

**Root Cause:**
Race condition in `unit_3d.gd:_die()`. Multiple damage events in the same frame could call `_die()` multiple times before `is_alive` was set to false. Additionally, using `await` for death animation could fail silently if the scene tree changed.

**Solution Implemented:**
1. Added `is_dying` guard flag to prevent multiple `_die()` calls
2. Changed from `await get_tree().create_timer()` to a `Tween` for more reliable cleanup
3. Updated `take_damage()` to check `is_dying` flag
4. Updated `_is_valid_target()` to exclude dying units
5. Updated `_acquire_target()` to skip dying units

**Related Files:**
- `scripts/units/unit_3d.gd:97` - Added `is_dying` flag
- `scripts/units/unit_3d.gd:957-974` - Improved `_die()` function
- `scripts/units/unit_3d.gd:935-937` - Updated `take_damage()`
- `scripts/units/unit_3d.gd:525-533` - Updated `_is_valid_target()`
- `scripts/units/unit_3d.gd:582-586` - Updated target acquisition

---

### Mana Bar Uses Hardcoded Values Instead of Hero System
**Resolved:** 2025-11-26
**Component:** UI / Mana System

**Description:**
The mana bar had hardcoded values and MANA_MAX was a constant in Summoner3D instead of using HeroInstance stats.

**Root Cause:**
`summoner_3d.gd` defined `const MANA_MAX: float = 10.0` instead of a variable that could be set from HeroInstance. The `_apply_hero_bonuses()` function had a TODO to apply max_mana but it was never implemented.

**Solution Implemented:**
1. Changed `const MANA_MAX` to `var max_mana` in summoner_3d.gd
2. Updated `_apply_hero_bonuses()` to set max_mana from HeroInstance stats
3. Updated all references from `MANA_MAX` to `max_mana`
4. Updated `mana_bar.gd:update_mana()` to update `progress_bar.max_value` when maximum changes
5. Added `DEFAULT_MAX_MANA` constant to mana_bar.gd for clarity

**Related Files:**
- `scripts/core/summoner_3d.gd:28` - Changed const to var
- `scripts/core/summoner_3d.gd:365-388` - Updated `_apply_hero_bonuses()`
- `scripts/ui/mana_bar.gd:29-30` - Added DEFAULT_MAX_MANA constant
- `scripts/ui/mana_bar.gd:123-127` - Update max_value in update_mana()

---

### Projectile Pooling Race Condition with Deferred Removal
**Resolved:** 2025-11-26
**Component:** Projectiles / Pooling System

**Description:**
Projectiles spawned rapidly in succession would cause errors: "Parent node is busy setting up children, cannot add child". This happened because pooled projectiles were being reused before their deferred removal from the scene tree had completed.

**Root Cause:**
In `ProjectileManager._return_to_pool()`, projectiles were removed from the scene tree using `remove_child.call_deferred()` to avoid physics callback issues. However, the projectile was immediately returned to the pool and could be grabbed by `_get_from_pool()` before the deferred removal completed.

**Solution Implemented:**
Added synchronous parent check in `_get_from_pool()`:
```gdscript
# Ensure projectile is removed from any parent (handles deferred removal race condition)
if pooled_projectile.get_parent():
    pooled_projectile.get_parent().remove_child(pooled_projectile)
```

This ensures that if a projectile is retrieved from the pool while still technically parented (due to pending deferred removal), it gets synchronously unparented before being added to the new container.

**Related Files:**
- `scripts/projectiles/projectile_manager.gd:153-156` - Added parent check in `_get_from_pool()`

---

### Mission Rewards Auto-Accepted Without Player Choice
**Resolved:** 2025-11-25
**Component:** Campaign / Rewards

**Description:**
If a mission finished and the player didn't explicitly accept rewards (e.g., closed the game or crashed), the rewards could be auto-accepted or lost. This was problematic for reward screens requiring player choice.

**Root Cause:**
The RewardScreen called `complete_battle()` immediately when loading, BEFORE the player had a chance to make a choice for "choice" type rewards. If the game exited before the player clicked Continue, the battle was marked complete but no reward was granted.

**Solution Implemented:**
Added pending reward state tracking:
1. Added `pending_reward` field to profile campaign_progress schema
2. Added CampaignService methods: `set_pending_reward()`, `get_pending_reward()`, `update_pending_choice()`, `clear_pending_reward()`, `claim_pending_reward()`
3. RewardScreen now:
   - Sets pending reward on first load (doesn't complete battle yet)
   - Checks for pending reward on load (resumes if found)
   - Only grants reward AND completes battle when Continue is pressed
   - Saves choice to pending state immediately when player picks (for choice rewards)

**Related Files:**
- `scripts/data/json_profile_repository.gd` - Added `pending_reward` to schema
- `scripts/services/campaign_service.gd` - Added pending reward management methods
- `scripts/ui/reward_screen.gd` - Complete rewrite of reward flow
- `localization/data/en.json` - Added ui.reward localization keys

---

### Cards Cannot Be Played in Campaign Battles
**Resolved:** 2025-11-25
**Component:** Cards / Battle System

**Description:**
Cards could not be played during campaign battles - dragging cards to the battlefield did nothing.

**Root Cause:**
`BattlefieldDropZone._can_drop_data()` was checking `summoner.get("is_alive")`, but a previous refactor renamed this property to `is_enabled` in Summoner3D. Since the property didn't exist, `get()` returned null, which defaulted to `false`, blocking all drops.

**Solution Implemented:**
Changed `is_alive` to `is_enabled` in `battlefield_drop_zone.gd:116-118`.

**Related Files:**
- `scripts/ui/battlefield_drop_zone.gd`

---

### Charge Spell Causes Units to Bounce When Targeting Above Base
**Resolved:** 2025-11-25
**Component:** Spells / Unit Movement

**Description:**
When using the Charge spell with a target location visually "above" the enemy base, units bounced back and forth instead of attacking. The issue was that `find_nearest_enemy()` found the EnemySummoner (at Z=0) instead of EnemyBase (at Z=-7.5), and Summoner had no collision shape for unit spreading.

**Root Cause:**
Summoner3D was in the "bases" group, making it a valid attack target. This was legacy code from when the Summoner was intended to be attackable, but in the actual game design only the Nexus (Base3D) should be attackable.

**Solution Implemented:**
1. Removed `add_to_group("bases")` from Summoner3D - summoners are no longer found as attack targets
2. Removed vestigial HP/death code from Summoner3D (max_hp, current_hp, take_damage, _die, summoner_died signal)
3. Removed `_on_summoner_died` handler from GameController3D
4. Documented intended architecture in `docs/design/hero-and-nexus.md`

**Related Files:**
- `scripts/core/summoner_3d.gd` - Removed HP/death code and bases group membership
- `scripts/core/game_controller_3d.gd` - Removed summoner_died signal handling
- `docs/design/hero-and-nexus.md` - New architecture documentation

---

### AI Scoring Magic Numbers Extracted to Constants
**Resolved:** 2025-11-25
**Component:** AI System

**Description:**
The HeuristicAI class used many hardcoded magic numbers for card scoring and decision-making thresholds, making AI tuning difficult.

**Solution Implemented:**
Extracted ~41 magic numbers to named class-level constants organized by category:
- Card scoring (SCORE_BASE_SUMMON, SCORE_MANA_EFFICIENCY_BASE, etc.)
- Enemy count thresholds (ENEMY_COUNT_THRESHOLD_LOSING_BADLY, etc.)
- Personality bonuses (PERSONALITY_AGGRESSIVE_SUMMON_BONUS, etc.)
- Battlefield state thresholds (STATE_LOSING_BADLY_THRESHOLD, etc.)
- Difficulty/randomness (DIFFICULTY_RANDOMNESS_MULTIPLIER, etc.)
- Play timing multipliers (TIMING_LOSING_BADLY_MULTIPLIER, etc.)
- Spawn zones (SPAWN_ENEMY_DEFENSIVE_MIN, SPAWN_PLAYER_NEUTRAL_MAX, etc.)

**Related Files:**
- `scripts/ai/heuristic_ai.gd` - All constants added at top of file

---

### WAL Uses Inconsistent Key Names
**Resolved:** 2025-11-25
**Component:** Database / ProfileRepository

**Description:**
The Write-Ahead Log used inconsistent key formats - some entries used "action"/"params" while others used "op".

**Solution Implemented:**
Standardized all WAL entries to use `{"action": "...", "params": {...}}` format:
- `unlock_hero` - changed from `"op"` to `"action"/"params"`
- `set_starting_hero` - changed from `"op"` to `"action"/"params"`

**Related Files:**
- `scripts/data/json_profile_repository.gd:246, 269-272`

---

### UUID Generation Weak Entropy
**Resolved:** 2025-11-25
**Component:** Database / ProfileRepository

**Description:**
The `_generate_uuid()` function used weak entropy sources (only ticks_msec and single randi) that could cause collisions.

**Solution Implemented:**
Added more entropy sources:
- `Time.get_unix_time_from_system()` - absolute timestamp
- `Time.get_ticks_usec()` - microsecond precision
- Two `randi()` calls instead of one
- Format: `"%x-%x-%x-%x"` with 4 components

**Related Files:**
- `scripts/data/json_profile_repository.gd:1013-1019`

---

### Backup Rotation Happens After Write Success
**Resolved:** 2025-11-25
**Component:** Database / ProfileRepository

**Description:**
Backup files were rotated after the main write succeeded, meaning a crash between write and rotation could lose a backup generation.

**Solution Implemented:**
Reordered operations: rotate backups BEFORE writing new data. This ensures old data is preserved in backup chain before being overwritten.

**Related Files:**
- `scripts/data/json_profile_repository.gd:833-842`

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
