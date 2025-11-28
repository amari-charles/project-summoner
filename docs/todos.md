# Project TODOs

This document tracks planned features, improvements, and tasks for Project Summoner.

For completed tasks, see [todos-completed.md](todos-completed.md).

**Status Legend:**
- ⬜ Not Started
- 🔄 In Progress
- ✅ Completed
- 🚫 Blocked

**Priority Levels:**
- 🔴 High Priority
- 🟡 Medium Priority
- 🟢 Low Priority

---

## Units & Combat

### 🟡 MEDIUM PRIORITY

#### Investigate Pathfinding & Targeting System Robustness
**Status:** ⬜ Not Started
**Category:** Units & Combat / Performance
**Effort:** Medium

**Description:**
Audit the current pathfinding and targeting systems for robustness and efficiency. Identify potential issues with edge cases, performance bottlenecks, and areas for improvement.

**Areas to Investigate:**
- Target acquisition logic (`_acquire_target()` in unit_3d.gd)
- Target lock timer and re-acquisition behavior
- Flanking/pathfinding when blocked
- Performance with large unit counts (N² targeting checks?)
- Edge cases: targets dying mid-attack, multiple units targeting same enemy
- Redirect system robustness (forced targets, guard mode)

**Questions to Answer:**
- How does targeting scale with 50+ units on screen?
- Are there race conditions in target switching?
- Is the blocked detection / flanking logic reliable?
- Should we use spatial partitioning for target queries?

**Notes:**
- Related to lane-based movement todo (may affect targeting behavior)
- Consider profiling with large battles before optimizing

---

#### Add Flying Unit Type
**Status:** ⬜ Not Started
**Category:** Units & Combat
**Effort:** Medium

**Description:**
Create a new flying unit type that can move over obstacles and other units.

**Requirements:**
- Design flying unit visuals/models
- Define flying unit stats and behavior
- Implement air layer combat mechanics

**Notes:**
- Requires flying movement logic (see below)
- May need separate targeting rules for ground vs air

---

#### Implement Flying Movement Logic
**Status:** ⬜ Not Started
**Category:** Units & Combat
**Effort:** Medium
**Dependencies:** Add Flying Unit Type

**Description:**
Implement the movement system for flying units including pathfinding and collision rules.

**Requirements:**
- Flying units can move over obstacles
- Flying units ignore ground unit collision during movement
- Proper animation/visual feedback for flying

**Notes:**
- Consider height/elevation for 2.5D visual effect
- May need separate pathfinding layer

---

#### Improve Unit Hitboxes
**Status:** ⬜ Not Started
**Category:** Units & Combat
**Effort:** Medium

**Description:**
Flesh out and refine unit hitboxes for better collision detection and combat interactions.

**Requirements:**
- Review current hitbox sizes and shapes
- Adjust hitboxes to better match visual models
- Test with various unit types (melee, ranged, large, small)
- Ensure proper interaction with projectiles and melee attacks

**Notes:**
- Important for combat feel and fairness
- May need different hitbox sizes for different unit types
- Consider separate hitboxes for collision vs damage

---

#### Lane-Based Unit Movement (Walk Forward Until Target in Range)
**Status:** ⬜ Not Started
**Category:** Units & Combat
**Effort:** Medium

**Description:**
Units should not path directly towards the enemy base. Instead, they should walk forward across the map on a line parallel to the battlefield (like lanes) until an enemy unit or the base comes into attack range, at which point they retarget.

**Current Behavior:**
- Units path directly towards the enemy base from spawn
- Creates a funneling effect where all units converge on one point

**Expected Behavior:**
- Units walk forward in their "lane" (parallel to map axis)
- When enemy unit enters attack range → retarget and engage
- When base enters attack range → retarget and attack base
- Creates more spread-out, strategic combat

**Requirements:**
- Modify unit movement to walk forward (towards enemy side) rather than path to base
- Implement range-based target acquisition during forward march
- Maintain current targeting behavior once engaged
- Consider what happens after killing a target (resume forward march?)

**Notes:**
- Similar to lane-based games like Clash Royale / auto-battlers
- May need to define "lanes" or just use spawn X position as the lane
- Charge spell should still override this behavior

---

#### Implement Single Target vs Multi Target Attack System
**Status:** ⬜ Not Started
**Category:** Units & Combat
**Effort:** Medium

**Description:**
Add system to differentiate between single target attacks and multi target/AoE attacks.

**Requirements:**
- Define attack target type in unit data (single, multi, aoe)
- Implement multi-target selection logic
- Add AoE damage radius for area attacks
- Visual indicators for AoE attacks (ground circles, splash effects)
- Balance damage for multi-target vs single-target

**Notes:**
- Foundation for spell variety and unit diversity
- Multi-target may need reduced damage per target
- Consider different AoE shapes (circle, cone, line)
- Important for strategic depth

---

#### Add Death Animations for Units
**Status:** ⬜ Not Started
**Category:** Visual Polish
**Effort:** Medium

**Description:**
Create death animations for all unit types to improve visual feedback when units are defeated.

**Requirements:**
- Design death animation for each unit type
- Implement animation triggers on unit death
- Add fade-out or removal timing

**Notes:**
- Consider particle effects (blood, sparks, etc.)
- Should not block gameplay flow

---

#### Add More Summon Unit Cards
**Status:** ⬜ Not Started
**Category:** Content
**Effort:** Variable (per card)

**Description:**
Design and implement additional summon cards to expand unit variety.

**Requirements:**
- Design unit stats and abilities
- Create unit models/visuals
- Balance against existing units
- Create card art and data

**Notes:**
- Follow existing unit creation patterns
- Test balance before adding to decks

---

#### Add More Spell Cards
**Status:** ⬜ Not Started
**Category:** Content
**Effort:** Variable (per card)

**Description:**
Design and implement additional spell cards for more strategic variety.

**Requirements:**
- Design spell effects and mechanics
- Implement spell logic
- Create VFX for spells
- Create card art and data

**Notes:**
- Consider direct damage, buffs, debuffs, board manipulation
- Balance mana costs carefully

---

## Database & Data Layer

### 🟢 LOW PRIORITY

#### Clean Up Redundant/Unused Profile Data Fields
**Status:** ⬜ Not Started
**Category:** Database / Cleanup
**Effort:** Small

**Description:**
The profile data structure has redundant and unused fields that waste storage and create confusion.

**Issues:**
1. **Duplicated `profile_id`**: Stored at root AND inside `resources`
   ```json
   "profile_id": "default",
   "resources": {
     "profile_id": "default",  // Redundant
   }
   ```

2. **Unused `roll_json` field**: Every card instance has `"roll_json": null`
   - Intended for future stat rolls
   - Currently unused, wastes space

**Requirements:**
- Remove `profile_id` from inside `resources`
- Consider removing `roll_json` until actually implemented
- Add migration if needed for existing saves

**Related Files:**
- `scripts/data/json_profile_repository.gd:879-924` - `_create_fresh_profile()`
- `scripts/data/json_profile_repository.gd:427-438` - Card instance creation

---

## Core Game Systems

### 🟢 LOW PRIORITY

#### Support Upgrade-Specific Resource Costs
**Status:** ⬜ Not Started
**Category:** Core Game Systems / Progression
**Effort:** Small
**Dependencies:** Card Level System (implemented)

**Description:**
Currently all card upgrades cost a flat gold amount. Add support for upgrade-specific resource costs defined in CardUpgradeCatalog.

**Current Behavior:**
- All level-ups cost gold only (amount scales with level)
- Cost is calculated in `CardProgressionService.get_card_progression_info()`

**Future Enhancement:**
- Individual upgrades can specify resource costs (essence, fragments, etc.)
- CardUpgradeCatalog already has structure to support this
- Would allow rare/powerful upgrades to require special resources

**Related Code:**
- `scripts/services/card_progression_service.gd:247` - TODO comment marking this location
- `scripts/data/card_upgrade_catalog.gd` - upgrade definitions

**Notes:**
- Low priority - current gold-only system works fine
- Implement when adding resource variety to progression

---

## Visual Polish

### 🟡 MEDIUM PRIORITY

#### Add 2.5D Character Rotation for Correct Perspective
**Status:** ⬜ Not Started
**Category:** Visual Polish
**Effort:** Small-Medium

**Description:**
Rotate in-game characters/units to face the camera at the correct angle for 2.5D perspective. This gives the proper "billboard" or angled sprite effect common in 2.5D games where characters appear to face the player while maintaining the isometric/angled view.

**Current Behavior:**
- Characters may not be rotated to account for the camera's viewing angle
- Can look flat or incorrect from the game's perspective

**Expected Behavior:**
- Characters should be rotated to face the camera appropriately for 2.5D aesthetic
- Maintains the illusion of depth while keeping characters readable
- Similar to how games like Octopath Traveler, Diablo, or classic RTS games handle sprite orientation

**Requirements:**
- Determine the correct rotation angle based on camera setup
- Apply rotation to all unit types (player units, enemy units, summoners)
- Ensure rotation works with existing animations
- Test with different camera angles if camera can move

**Notes:**
- Common approaches: billboard sprites (always face camera), fixed rotation offset, or Y-axis rotation only
- May need to adjust based on whether using 3D models or 2D sprites
- Consider if buildings/structures also need this treatment

---

#### Improve Card Visual UI
**Status:** ⬜ Not Started
**Category:** UI/UX
**Effort:** Medium

**Description:**
Enhance the visual design of card display including layout, typography, and effects.

**Requirements:**
- Refine card frame and borders
- Improve text readability
- Add card hover effects
- Polish card animations

**Notes:**
- Should work with existing 3D tilt effect
- Consider glow/highlight for playable cards

---

## Audio

### 🟡 MEDIUM PRIORITY

#### Add Background Music System
**Status:** ⬜ Not Started
**Category:** Audio
**Effort:** Medium

**Description:**
Implement core music system with playback, volume control, and transitions.

**Requirements:**
- Audio bus setup for music
- Fade in/out transitions
- Settings integration for volume control
- Looping support

**Notes:**
- Foundation for all music features below
- Consider dynamic music system for future

---

#### Add Battle Music Tracks
**Status:** ⬜ Not Started
**Category:** Audio
**Effort:** Small (per track)
**Dependencies:** Add Background Music System

**Description:**
Source and implement music tracks for active battle gameplay.

**Requirements:**
- Find/commission suitable battle music
- Integrate with music system
- Set appropriate looping points

**Notes:**
- Should be energetic but not overwhelming
- Consider multiple tracks for variety

---

#### Add Victory/Defeat Music
**Status:** ⬜ Not Started
**Category:** Audio
**Effort:** Small
**Dependencies:** Add Background Music System

**Description:**
Add musical stings or short tracks for win/loss conditions.

**Requirements:**
- Victory fanfare
- Defeat music
- Integrate with battle end screens

**Notes:**
- Should be short and impactful
- Clear emotional distinction between victory/defeat

---

#### Add Unit Attack Sounds
**Status:** ⬜ Not Started
**Category:** Audio
**Effort:** Medium

**Description:**
Add sound effects for all unit attack actions.

**Requirements:**
- Source/create attack sounds for each unit type
- Integrate with attack animations
- Vary sounds to avoid repetition

**Notes:**
- Different sounds for melee vs ranged
- Consider unique sounds per unit type

---

#### Add Unit Movement Sounds
**Status:** ⬜ Not Started
**Category:** Audio
**Effort:** Medium

**Description:**
Add footstep and movement sound effects for units.

**Requirements:**
- Source/create movement sounds
- Integrate with movement animations
- Handle different terrain types (optional)

**Notes:**
- Should be subtle, not overwhelming
- Consider speed-based variation

---

#### Add Unit Death Sounds
**Status:** ⬜ Not Started
**Category:** Audio
**Effort:** Small
**Dependencies:** Add Death Animations for Units

**Description:**
Add sound effects when units are defeated.

**Requirements:**
- Source/create death sounds for each unit type
- Integrate with death animations
- Mix appropriately with other sounds

**Notes:**
- Should be clear but not overly gory
- Vary by unit type

---

#### Add Spell Cast Sounds
**Status:** ⬜ Not Started
**Category:** Audio
**Effort:** Medium

**Description:**
Add sound effects for spell casting actions.

**Requirements:**
- Source/create spell cast sounds
- Integrate with spell card play
- Unique sounds for different spell types

**Notes:**
- Should feel magical and impactful
- Coordinate with spell VFX

---

#### Add Projectile Impact Sounds
**Status:** ⬜ Not Started
**Category:** Audio
**Effort:** Small

**Description:**
Add sound effects when projectiles hit their targets.

**Requirements:**
- Source/create impact sounds
- Integrate with projectile hit detection
- Vary by projectile type

**Notes:**
- Should sync with visual impact
- Consider different sounds for hit vs miss

---

#### Add Building Damage Sounds
**Status:** ⬜ Not Started
**Category:** Audio
**Effort:** Small
**Dependencies:** Add Building Hit/Damage Animation

**Description:**
Add sound effects when buildings take damage.

**Requirements:**
- Source/create building impact sounds
- Integrate with damage events
- Should feel weighty and important

**Notes:**
- Should be distinct from unit damage
- Critical audio feedback for game state

---

#### Add UI Click/Interaction Sounds
**Status:** ⬜ Not Started
**Category:** Audio
**Effort:** Small

**Description:**
Add sound feedback for UI interactions (button clicks, menu navigation, etc.).

**Requirements:**
- Source/create UI sound set
- Integrate with all buttons and interactive elements
- Consistent sound design across UI

**Notes:**
- Should be subtle and pleasant
- Avoid annoying repetitive sounds

---

#### Add Card Play Sounds
**Status:** ⬜ Not Started
**Category:** Audio
**Effort:** Small

**Description:**
Add sound effects when cards are played from hand.

**Requirements:**
- Card draw/shuffle sounds
- Card play confirmation sound
- Integrate with card system

**Notes:**
- Should feel satisfying
- Consider different sounds for different card types

---

#### Add Mana Gain Sounds
**Status:** ⬜ Not Started
**Category:** Audio
**Effort:** Small

**Description:**
Add sound effect for mana regeneration/gain events.

**Requirements:**
- Source/create mana gain sound
- Integrate with mana system
- Should be noticeable but not intrusive

**Notes:**
- Helps players track mana availability
- Consider subtle vs prominent sound

---

## UI Revamp

### 🟡 MEDIUM PRIORITY

#### Revamp Main Menu UI
**Status:** ⬜ Not Started
**Category:** UI/UX
**Effort:** Medium

**Description:**
Redesign the main menu with improved visual style and layout.

**Requirements:**
- Modern, polished visual design
- Clear button hierarchy
- Proper spacing and alignment
- Background art/effects

**Notes:**
- First impression matters
- Should set tone for game quality

---

#### Add Hero Select UI
**Status:** ✅ Completed
**Category:** UI/UX
**Effort:** Medium

**Description:**
Create a hero selection screen allowing players to choose their hero before battle.

**Requirements:**
- Display available heroes with icons/portraits
- Show hero stats (health, mana, mana regen)
- Show hero element/affinity
- Indicate locked/unlocked heroes
- Preview hero abilities or bonuses

**Implementation:**
- HeroManagementPanel provides full hero roster view
- HeroIconWidget provides persistent hero button on screens
- HeroRosterItem shows individual hero details with stats

---

#### Revamp Battle HUD
**Status:** ⬜ Not Started
**Category:** UI/UX
**Effort:** Medium

**Description:**
Redesign the in-battle HUD elements for better clarity and visual appeal.

**Requirements:**
- Improve HP display for summoners
- Better resource (mana) visibility
- Turn indicator clarity
- Proper information hierarchy

**Notes:**
- Must not obstruct battlefield
- Critical information should be immediately readable

---

#### Revamp Card Hand Display
**Status:** ⬜ Not Started
**Category:** UI/UX
**Effort:** Medium

**Description:**
Improve the visual presentation of cards in the player's hand.

**Requirements:**
- Better card spacing and layout
- Smooth card hover/selection feedback
- Clear playability indicators
- Handle varying hand sizes

**Notes:**
- Already has 3D tilt effect - build on that
- Should feel like holding physical cards

---

#### Revamp Settings Screen UI
**Status:** ⬜ Not Started
**Category:** UI/UX
**Effort:** Small

**Description:**
Redesign settings/options screen for better usability and visual consistency.

**Requirements:**
- Clear option categories
- Intuitive controls
- Visual consistency with other UI
- Proper feedback for changes

**Notes:**
- Should be functional first, pretty second
- Consider accessibility options

---

## Hero System

### 🔴 HIGH PRIORITY

#### Standardize "Hero" vs "Summoner" Language
**Status:** ⬜ Not Started
**Category:** Heroes / Architecture
**Effort:** Medium

**Description:**
The codebase inconsistently uses "Summoner" and "Hero" to refer to the same concept (the player character). This should be standardized to one term throughout codebase, docs, and UI.

**Current State:**
- Class is named `Summoner3D` but represents the "Hero"
- `HeroInstance` exists for hero progression/stats
- Design doc uses "Hero (Summoner3D)" as mapping
- Variables use `player_summoner`, `enemy_summoner`
- Groups use `summoners`, `player_summoners`

**Decision Needed:**
- Pick ONE canonical term: "Hero" or "Summoner"
- Recommendation: **Hero** (more intuitive for players, "Summoner" is a genre term)

**Requirements:**
- Rename `Summoner3D` → `Hero3D` (or keep and document why)
- Update all variable names, signals, groups
- Update UI text and documentation
- Update scene node names

**Notes:**
- See `docs/design/hero-and-nexus.md` for architecture context
- This is a refactor - no gameplay changes
- Consider doing alongside Hero System implementation

---

#### Design Hero Data Structure
**Status:** ✅ Completed
**Category:** Heroes / Architecture
**Effort:** Medium

**Description:**
Define the data structure and resource format for hero characters.

**Implementation:**
- HeroConfig: Static hero configuration (base stats, innate traits)
- HeroInstance: Runtime state (level, xp, acquired boons, computed stats)
- TraitCatalog: Central trait/boon registry with modifiers
- See `docs/features/heroes/architecture.md` for details

---

#### Implement Hero Stats System
**Status:** ✅ Completed
**Category:** Heroes
**Effort:** Medium

**Description:**
Implement the technical system for hero-specific stats and attributes.

**Implementation:**
- HeroInstance.get_computed_stats() applies trait modifiers to base stats
- BattleContext.set_player_hero_stats() caches stats for DamageSystem
- Trait modifiers support flat and percent bonuses
- Element-specific damage bonuses (fire_damage_bonus, etc.)

---

#### Implement Hero Special Abilities
**Status:** ⬜ Not Started (Phase 3/4)
**Category:** Heroes
**Effort:** Large

**Description:**
Implement the system for hero active and passive abilities.

**Notes:**
- Phase 3: Level Traits (trait selection at level-up)
- Phase 4: Ultimate Traits (level 10 capstone abilities)
- Foundation is ready via TraitCatalog modifier system

---

#### Create Hero Selection Screen UI
**Status:** ✅ Completed
**Category:** Heroes / UI
**Effort:** Medium

**Description:**
Design and implement the UI screen where players choose their hero before battle.

**Implementation:**
- HeroManagementPanel: Full roster view with stats, traits, level-up
- HeroIconWidget: Persistent hero button (click to open panel)
- HeroRosterItem: Individual hero row with select/level-up buttons
- Hero switching via HeroSelection service

---

#### Implement Hero Unlock System (Post-MVP)
**Status:** ⬜ Not Started (Post-MVP)
**Category:** Heroes / Progression
**Effort:** Medium

**Description:**
Implement the system for unlocking additional heroes beyond the starting hero.

**Notes:**
- Foundation exists: HeroInstance persistence, profile hero_instances array
- Need: Campaign milestone triggers for unlocking new heroes
- Need: UI to show locked heroes and unlock progress

---

#### Design Hero In-Battle UI Elements
**Status:** ✅ Completed (Foundation)
**Category:** Heroes / UI
**Effort:** Medium

**Description:**
Design UI elements for displaying hero information and abilities during battle.

**Implementation:**
- HeroIconWidget added to CampaignMap, CollectionScreen, GameModeMenu
- Shows active hero element color and level
- Click opens HeroManagementPanel

**Remaining:**
- Ability buttons/cooldowns (Phase 3/4 - when abilities are added)

---

#### Integrate Heroes into Battle System
**Status:** ✅ Completed (Foundation)
**Category:** Heroes
**Effort:** Large

**Description:**
Final integration of hero system into the core battle gameplay loop.

**Implementation:**
- Summoner loads HeroInstance via DeckLoader
- Hero stats applied via BattleContext.set_player_hero_stats()
- DamageSystem reads hero stats for damage bonuses
- HeroModifierProvider passes unit modifiers to ModifierSystem
- Per-hero campaign progress in ProfileRepo

**Remaining:**
- Hero abilities (Phase 3/4)
- AI heroes for enemies (future)

---

## Developer Tools

### 🟢 LOW PRIORITY

#### Hide/Remove FPS Test Tool Before Release
**Status:** ⬜ Not Started
**Category:** Developer Tools / Release Prep
**Effort:** Trivial

**Description:**
The FPS Test Tool (`scripts/debug/fps_test_tool.gd`) currently shows by default in debug builds. Before release, either:
- Remove the autoload entirely, or
- Ensure it only activates with a specific dev flag/command

**Current Behavior:**
- Panel shows automatically on game start (debug builds only)
- Toggle with ` or F12
- Already disabled in release builds via `OS.is_debug_build()` check

**Notes:**
- Low priority - it's already hidden in release builds
- May want to keep for internal testing but hide from players in beta/early access

---

#### Campaign Level Editor (Dev-Only Tool)
**Status:** ⬜ Not Started
**Category:** Developer Tools
**Effort:** Large

**Description:**
A UI tool for developers to design and configure campaign battles without touching code.

**Purpose:**
- Allow designers to create/edit campaign battles without touching code
- Configure enemy decks, AI behavior, rewards, difficulty
- Test battles directly from the editor

**Requirements:**
- **Access**: Dev-only tool (not accessible to players)
- **Location**: Separate scene, accessible from main menu in debug builds or via dev console
- Drag-and-drop cards to build enemy deck
- Set deck size (no player limits for enemies)
- Configure AI behavior (aggression, card priority, play speed)
- Set battle metadata (name, description, difficulty)
- Define reward structure (fixed/choice/random cards)
- Set unlock requirements (which battles must be completed first)
- Preview/test battle
- Save battle definitions to `campaign_service.gd` or separate JSON files

**Notes:**
- Hardcoded decks in `campaign_service.gd` work fine for now
- Only needed when managing 20+ battles becomes cumbersome

---

*Last Updated: 2025-11-28 - Implemented unit testing infrastructure (moved to completed)*
