# Project TODOs

This document tracks planned features, improvements, and tasks for Fateforged.

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

### 🟡 MEDIUM PRIORITY

#### Add Quit Game Functionality
**Status:** ⬜ Not Started
**Category:** Core Game Systems / UI
**Effort:** Small

**Description:**
Add a way for players to quit the entire game from within the application.

**Requirements:**
- Add quit button to title screen or settings menu
- Implement proper cleanup before exit
- Handle unsaved progress (if applicable)

**Notes:**
- Standard feature expected by players
- Should work on both desktop and mobile platforms

---

#### Investigate Mobile/Desktop Compatibility
**Status:** ⬜ Not Started
**Category:** Core Game Systems / Platform
**Effort:** Medium

**Description:**
Review the current game setup to ensure compatibility with both mobile and desktop platforms.

**Areas to Investigate:**
- UI scaling and touch input
- Resolution and aspect ratio handling
- Input method differences (touch vs mouse/keyboard)
- Performance on mobile devices
- Export settings for each platform

**Notes:**
- Important to address early to avoid major refactoring later
- May need platform-specific adjustments

---

#### Card Returns to Pool When Summoned Unit Dies
**Status:** ⬜ Not Started
**Category:** Core Game Systems / Card Mechanics
**Effort:** Medium

**Description:**
Currently, when a summon card is played, it returns to the card pool immediately after being played. This allows players to redraw and replay the same summon multiple times while previous summons are still alive.

Change this so that summon cards only return to the card pool after their summoned unit dies on the battlefield.

**Requirements:**
- Track which card instance spawned each unit
- When a unit dies, return its associated card to the draw pool
- Handle edge cases: what happens if battle ends while units are alive?
- Update card pool/hand logic to exclude "in-play" cards from draw calculations

**Benefits:**
- Prevents spam strategies with powerful summons
- Adds strategic depth (protecting your units = keeping options limited)
- More realistic deck management

**Notes:**
- May need a visual indicator showing which cards are "in play" on the battlefield
- Consider: should this apply to all summon cards or just certain ones?
- Consider: multi-spawn cards (spawn_count > 1) - when does card return?

---

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

#### Improve Hit Flash Feedback for Large Units
**Status:** ⬜ Not Started
**Category:** Visual Polish
**Effort:** Small

**Description:**
Large units (like Fire Titan) appear permanently lit up when taking continuous damage from multiple attackers. The hit flash effect doesn't scale well for tanky units.

**Possible Solutions:**
1. **Threshold-based flashing**: Only show hit flash when damage exceeds a % of max HP (e.g., 5-10%)
2. **Cooldown-based**: Add minimum time between flashes regardless of hits
3. **Damage accumulation**: Accumulate damage over a short window, flash once for the total
4. **Visual variation**: Use different flash intensity based on damage amount
5. **Alternative feedback**: Replace constant flash with damage numbers, screen shake, or other effects for large units

**Notes:**
- Current `flash_white()` in `sprite_character_2d5_component.gd` triggers on every hit
- Problem is most noticeable on high-HP units being attacked by multiple enemies
- Solution should still provide clear feedback that damage is occurring

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

#### Add Victory/Defeat Music
**Status:** ⬜ Not Started
**Category:** Audio
**Effort:** Small
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

#### Add Loading Screen with Asset Preloading
**Status:** ⬜ Not Started
**Category:** UI/UX / Performance
**Effort:** Medium

**Description:**
Create a loading screen that displays during battle transitions and preloads all unit assets asynchronously. This eliminates first-spawn initialization delays and provides a polished user experience.

**Requirements:**
- Loading screen scene with progress bar
- Use `ResourceLoader.load_threaded_request()` for async loading
- Preload all unit scenes from CardCatalog
- Optionally show tips, lore, or artwork during loading

**Technical Notes:**
- Currently using silent preload as stopgap (instantiate/free each unit scene)
- Full solution should use async loading with accurate progress reporting
- Consider caching loaded resources for session duration

**Notes:**
- Part of polish phase
- Professional games use this approach for zero gameplay hitches

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

### 🟢 LOW PRIORITY

#### Standardize .tscn Placeholder Text Pattern
**Status:** ⬜ Not Started
**Category:** UI / Code Style
**Effort:** Trivial

**Description:**
Standardize placeholder text in `.tscn` scene files for UI screens. Currently there's inconsistency:
- Some files use `[ui.nav.menu]` style placeholders
- Some files use actual display text like `"PROJECT SUMMONER"`
- All get overwritten by GDScript `_ready()` with `Loc.t()` calls

**Solution:**
Use empty strings `""` in all `.tscn` files since GDScript sets localized text anyway.

**Files to Update:**
- `scenes/ui/components/nav_drawer.tscn`
- `scenes/ui/title_screen.tscn`
- Any other new UI screens with placeholder text

**Notes:**
- Purely cosmetic - no runtime impact
- Low priority polish item

---

## Summoner System

### 🟢 LOW PRIORITY

#### Implement Summoner Special Abilities
**Status:** ⬜ Not Started (Phase 3/4)
**Category:** Summoners
**Effort:** Large

**Description:**
Implement the system for summoner active and passive abilities.

**Notes:**
- Phase 3: Level Traits (trait selection at level-up)
- Phase 4: Ultimate Traits (level 10 capstone abilities)
- Foundation is ready via TraitCatalog modifier system

---

#### Implement Summoner Unlock System (Post-MVP)
**Status:** ⬜ Not Started (Post-MVP)
**Category:** Summoners / Progression
**Effort:** Medium

**Description:**
Implement the system for unlocking additional summoners beyond the starting summoner.

**Notes:**
- Foundation exists: SummonerInstance persistence, profile summoner_instances array
- Need: Campaign milestone triggers for unlocking new summoners
- Need: UI to show locked summoners and unlock progress

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

*Last Updated: 2025-12-18 - Moved completed audio todos (BGM, battle music, UI clicks, card sounds) to todos-completed.md*
