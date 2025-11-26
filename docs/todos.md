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

#### Prevent Units from Stacking on Same Coordinates
**Status:** ⬜ Not Started
**Category:** Units & Combat
**Effort:** Small

**Description:**
Add collision/placement validation to prevent multiple units from occupying the same grid position.

**Requirements:**
- Check for existing unit before placement
- Block movement to occupied tiles
- Handle edge cases (unit death, teleportation)

**Notes:**
- Should work for both player and AI units
- May need visual feedback for invalid placement

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

### 🔴 HIGH PRIORITY

#### Consolidate Dual Catalog System (CardCatalog vs ContentCatalog)
**Status:** ⬜ Not Started
**Category:** Database / Architecture
**Effort:** Medium

**Description:**
The codebase has TWO card catalog systems with incompatible data formats, creating confusion and potential bugs.

**Current State:**
| Feature | CardCatalog | ContentCatalog |
|---------|-------------|----------------|
| Source | Hardcoded GDScript | JSON files |
| Card Type | `int` (0, 1) | `String` ("summon") |
| Cards | ~21 cards | 4 cards |
| Used By | All gameplay | Rarely |

**Problems:**
- `CardCatalog` uses `card_type: int` (0 = SUMMON, 1 = SPELL)
- `ContentCatalog/CardData` uses `card_type: String` ("summon", "spell")
- Type mismatches if systems are used together
- Duplicate maintenance burden

**Requirements:**
- Decide: keep CardCatalog (hardcoded) OR migrate to ContentCatalog (JSON)
- Remove the unused system entirely
- Ensure consistent type format across remaining system

**Recommendation:**
Keep `CardCatalog` for now (it has all the cards), remove card-loading from `ContentCatalog`. Later migrate CardCatalog to JSON when content volume grows.

**Related Files:**
- `scripts/data/card_catalog.gd` - Primary system (26KB, 21 cards)
- `scripts/data/content_catalog.gd` - Secondary system (loads JSON)
- `scripts/data/card_data.gd` - JSON card format

---

### 🟡 MEDIUM PRIORITY

---

#### Add Schema Validation for JSON Content Loading
**Status:** ⬜ Not Started
**Category:** Database / Data Validation
**Effort:** Medium

**Description:**
`ContentCatalog` loads JSON files without validating required fields. Missing fields silently use defaults which can cause bugs later.

**Current:**
```gdscript
func _load_unit_from_file(file_path: String) -> UnitData:
    # ... parse JSON ...
    return UnitData.from_dict(data_dict)  # No validation!
```

**Problems:**
- Missing `unit_id` silently becomes empty string
- Invalid stats (negative HP) not caught at load time
- Errors surface much later during gameplay

**Requirements:**
- Add required field validation in `from_dict()` methods
- Return null and log error if required fields missing
- Validate stat ranges (HP > 0, etc.)

**Related Files:**
- `scripts/data/content_catalog.gd:57-83`
- `scripts/data/unit_data.gd:50-107` - `from_dict()`
- `scripts/data/card_data.gd:43-78` - `from_dict()`

---

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

### 🔴 HIGH PRIORITY

#### Fix Hardcoded UI Strings - Add Localization
**Status:** ⬜ Not Started
**Category:** Core Game Systems / Localization
**Effort:** Medium

**Description:**
Many UI files have hardcoded user-facing strings instead of using the `Loc.t()` localization pattern. All user-facing text must be localized.

**Files Requiring Updates:**
- `reward_screen.gd` - "Battle Already Completed", "No rewards for replaying battles", "Unknown Card", rarity text
- `collection_screen.gd` - Card stats labels, deck info labels, empty state messages
- `campaign_map.gd` - Event labels, difficulty stars, "REPLAY (no reward)", button states
- `game_ui.gd` - Timer format
- `player_input_3d.gd` - Mana/hand debug labels

**Requirements:**
- Replace all hardcoded strings with `Loc.t("key.path")` calls
- Add corresponding entries to `localization/data/en.json`
- Follow naming convention: `category.subcategory.item`

**Notes:**
- Critical for future localization support
- Defined in CLAUDE.md as a project requirement
- Should be addressed systematically file by file

---

#### Research and Implement Framerate Independence
**Status:** ⬜ Not Started
**Category:** Core Game Systems / Performance
**Effort:** Medium

**Description:**
Research and implement proper framerate-independent game mechanics to ensure consistent gameplay across different hardware and frame rates.

**Requirements:**
- Audit all movement and physics calculations
- Ensure delta time is used for all time-dependent calculations (movement speed, attack speed, animations)
- Test on different framerates (30fps, 60fps, 120fps+, variable)
- Fix any framerate-dependent behaviors
- Document best practices for framerate independence

**Examples of Issues to Fix:**
- Movement speed should use `velocity * delta` instead of just `velocity`
- Attack cooldowns should accumulate `delta` instead of frame counts
- Animations should be time-based, not frame-based
- Mana regeneration should scale with delta time

**Notes:**
- Critical for game feel and fairness
- Players with different hardware should have identical gameplay
- Godot provides delta time in `_process(delta)` and `_physics_process(delta)`
- Important foundation - fix early before adding more content

---

#### Audit Codebase for Magic Strings - Replace with Constants/Enums
**Status:** 🔄 In Progress
**Category:** Core Game Systems / Code Quality
**Effort:** Medium

**Description:**
Audit the entire codebase to identify places where magic strings are used instead of constants or enums, and refactor to use type-safe definitions.

**Requirements:**
- Search for hardcoded string literals throughout codebase
- Identify candidates for replacement (element names, stat names, group names, etc.)
- Create or update constant/enum definitions
- Refactor code to use constants instead of strings
- Test to ensure no regressions

**Examples of Magic Strings to Replace:**
- Element names: "fire", "water", "wind", "earth", etc.
- Stat names: "attack_damage", "max_hp", "move_speed", "attack_speed"
- Group names: "player_units", "enemy_units", "bases"
- Card types: "unit", "spell"
- Team identifiers: Team.PLAYER, Team.ENEMY (already enums, but check usage)

**Notes:**
- Improves code maintainability and catches typos at compile time
- Makes refactoring easier (rename in one place)
- Better IDE autocomplete support
- Foundation for type safety across the codebase
- Start with high-impact areas (modifier system, card catalog)

**Progress Tracking:**

##### CardIDs Constants Class ✅ Completed (2025-11-15)
- Created `scripts/data/card_ids.gd` with StringName constants for all 18 cards
- Updated `CardCatalog` API to accept StringName instead of String
- Added validation in `CardCatalog._validate_card_ids_sync()` to ensure sync
- Updated `test_game_controller.gd` and `first_card_selection.gd` to use CardIDs

##### ProjectileIDs Constants Class ✅ Completed (2025-11-15)
- Created `scripts/data/projectile_ids.gd` with FIREBALL, ARROW, EMBER constants
- Updated fireball card in `card_catalog.gd` to use `ProjectileIDs.FIREBALL`
- Fixes fireball damage timing issue (now applies on impact, not on cast)

##### CardTypeIDs or Card.CardType Enum Usage ✅ Completed (2025-11-25)
- Replaced all magic numbers with `Card.CardType.SUMMON` and `Card.CardType.SPELL`
- Updated comparisons in `create_card_resource()` and `print_catalog_summary()`
- See `todos-completed.md` for details

##### VFXIDs Constants Class ✅ Completed (2025-11-25)
- Created `scripts/data/vfx_ids.gd` with StringName constants for 7 VFX effects
- Implemented: FIREBALL_EXPLOSION, FIREBALL_TRAIL, FIREBALL_SPELL
- Placeholders: SPELL_FIZZLE, RALLY_CIRCLE, GUARD_MARKER, CHARGE_MARKER
- Updated `card_catalog.gd` and `card.gd` to use VFXIDs constants
- Added `_validate_vfx_ids_sync()` in VFXManager to ensure sync with .tres files

##### RarityIDs Constants Class ✅ Completed (2025-11-25)
- Created `scripts/data/rarity_ids.gd` with StringName constants for COMMON, RARE, EPIC, LEGENDARY
- Added utility methods: `ALL_RARITIES`, `get_tier()`, `is_valid()`
- Updated `collection_service.gd`, `campaign_service.gd`, `color_palette.gd`, `dev_console.gd`

##### BiomeIDs Constants Class ⬜ Not Started (MEDIUM PRIORITY)
- Currently 1 biome, will expand significantly
- Create `scripts/data/biome_ids.gd` when adding second biome
- Good foundation for campaign/world building

##### BattleIDs Constants Class ⬜ Not Started (MEDIUM PRIORITY)
- ~5-10 battle IDs used in campaign system
- Create `scripts/data/battle_ids.gd`
- Update `campaign_service.gd` to use constants
- Makes campaign content management safer

---

#### Implement Card and Hero Level System
**Status:** ⬜ Not Started
**Category:** Core Game Systems / Progression
**Effort:** Large

**Description:**
Implement leveling system for cards and heroes that allows them to grow stronger through gameplay.

**Requirements:**
- Card level data structure and storage
- Hero level data structure and storage
- Experience/level-up mechanics
- Stat scaling per level (HP, attack, abilities)
- UI display for card/hero levels
- Level-up rewards and feedback
- Max level caps
- Save/load integration

**Notes:**
- Foundation for long-term progression
- Balance carefully - levels shouldn't trivialize content
- Consider different level curves for different rarities
- May need separate systems for card levels vs hero levels
- Important for player retention and sense of progression

---

### 🟡 MEDIUM PRIORITY

#### Implement Deck Recycling After Exhaustion
**Status:** ⬜ Not Started
**Category:** Core Game Systems
**Effort:** Small

**Description:**
When a player's deck is exhausted (all cards drawn), shuffle the discard pile back into the deck to continue play.

**Requirements:**
- Detect when deck is empty
- Shuffle discard pile
- Reset deck with shuffled cards
- Visual/audio feedback for deck recycling
- Log deck recycle events

**Notes:**
- Common mechanic in card games (e.g., Slay the Spire, Hearthstone)
- Prevents deck-out loss condition
- May need to handle edge case where deck AND hand are empty

---

## Visual Polish

### 🟡 MEDIUM PRIORITY

#### Improve Mana Bar UI Design
**Status:** ⬜ Not Started
**Category:** UI/UX
**Effort:** Small

**Description:**
Enhance the visual design of the mana bar to be more polished and readable.

**Requirements:**
- Refine visual style (colors, gradients, borders)
- Improve readability of current/max mana
- Add juice (fill animations, glow effects)

**Notes:**
- Should match overall UI style
- Consider mana regeneration visual feedback

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

## Campaign System

### 🔴 HIGH PRIORITY

#### Implement Win Condition System for Campaign Events
**Status:** ⬜ Not Started
**Category:** Campaign / Battle System
**Effort:** Medium

**Description:**
Campaign battles currently lack proper win/loss conditions tied to the event sequence system. If the player doesn't complete the objective in time (e.g., kill enemy base), they should lose the fight.

**Requirements:**
- Define win condition types: DESTROY_BASE, SURVIVE_TIME, KILL_ALL, PROTECT_ALLY, etc.
- Configure win conditions per battle in battle definitions
- Support time limits (e.g., "destroy base within 2 minutes")
- Proper loss handling when conditions aren't met
- Integration with EventSequencer for tutorial/scripted battles
- UI feedback showing current objective and timer (if applicable)

**Example Use Cases:**
- Tutorial: "Damage the training dummy" (wait for unit_damaged signal)
- Regular battle: "Destroy the enemy base" (no time limit)
- Challenge mode: "Destroy enemy base within 60 seconds" (time limit)
- Defense: "Survive for 90 seconds" (timer-based win)

**Related Files:**
- `scripts/core/game_controller_3d.gd` - Battle end conditions
- `scripts/services/campaign_service.gd` - Battle definitions
- `scripts/services/event_sequencer.gd` - Tutorial event flow

**Notes:**
- Currently battles only check if a base is destroyed
- No support for time-based win/loss conditions
- Event sequences can pause gameplay but don't enforce completion

---

#### Design Campaign Map Interface
**Status:** ⬜ Not Started
**Category:** Campaign / UI
**Effort:** Large

**Description:**
Design the visual and UX approach for the new map-based campaign interface to replace the current list view.

**Requirements:**
- Map layout concept (linear path, branching, open world?)
- Visual style (world map, battle map, abstract?)
- Node/point design for battles
- Progression visualization
- Lock/unlock indicators

**Notes:**
- Major UX change - needs careful design
- Reference: Slay the Spire, FTL, etc.
- Should feel like a journey

---

#### Implement Map Node System for Battles
**Status:** ⬜ Not Started
**Category:** Campaign
**Effort:** Medium
**Dependencies:** Design Campaign Map Interface

**Description:**
Implement the technical system for map nodes representing battles and their connections.

**Requirements:**
- Node data structure
- Node connection/progression logic
- Lock/unlock state management
- Save/load integration

**Notes:**
- Should support future expansion (non-battle nodes)
- Clean data structure for easy content addition

---

#### Add Map Navigation/Selection
**Status:** ⬜ Not Started
**Category:** Campaign / UI
**Effort:** Medium
**Dependencies:** Implement Map Node System for Battles

**Description:**
Implement player interaction with the campaign map - selecting and starting battles.

**Requirements:**
- Node click/selection
- Preview battle info
- Path highlighting for available battles
- Smooth camera movement (if needed)

**Notes:**
- Should feel intuitive and responsive
- Clear visual feedback for available vs locked battles

---

#### Integrate Battle Progression on Map
**Status:** ⬜ Not Started
**Category:** Campaign
**Effort:** Small
**Dependencies:** Add Map Navigation/Selection

**Description:**
Connect battle completion to map progression - unlocking next nodes, visual updates.

**Requirements:**
- Mark completed nodes
- Unlock next available nodes
- Update map visuals on completion
- Save progression state

**Notes:**
- Should feel rewarding
- Clear visual feedback for progress

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
**Status:** ⬜ Not Started
**Category:** Heroes / Architecture
**Effort:** Medium

**Description:**
Define the data structure and resource format for hero characters.

**Requirements:**
- Hero stats (HP, mana, abilities)
- Hero passive/active abilities
- Visual/art references
- Deck building constraints (if any)
- Extensible design for future heroes

**Notes:**
- Foundation for entire hero system
- Should support variety (tank, mage, etc.)
- Consider balance implications

---

#### Implement Hero Stats System
**Status:** ⬜ Not Started
**Category:** Heroes
**Effort:** Medium
**Dependencies:** Design Hero Data Structure

**Description:**
Implement the technical system for hero-specific stats and attributes.

**Requirements:**
- Override/modify base summoner stats
- Hero HP pools
- Hero-specific mana rules (if any)
- Stat display integration

**Notes:**
- Should work with existing summoner system
- Clean integration with combat

---

#### Implement Hero Special Abilities
**Status:** ⬜ Not Started
**Category:** Heroes
**Effort:** Large
**Dependencies:** Implement Hero Stats System

**Description:**
Implement the system for hero active and passive abilities.

**Requirements:**
- Ability triggering system
- Ability cooldowns/costs
- Ability effect implementation
- Visual/audio feedback

**Notes:**
- Most complex part of hero system
- Each hero will need unique abilities
- Balance is critical

---

#### Create Hero Selection Screen UI
**Status:** ⬜ Not Started
**Category:** Heroes / UI
**Effort:** Medium
**Dependencies:** Design Hero Data Structure

**Description:**
Design and implement the UI screen where players choose their hero before battle.

**Requirements:**
- Display available heroes
- Show hero stats and abilities
- Locked/unlocked state
- Selection confirmation
- Visual polish

**Notes:**
- Important for player engagement
- Should show off hero variety
- Clear ability descriptions

---

#### Implement Hero Unlock System (Post-MVP)
**Status:** ⬜ Not Started
**Category:** Heroes / Progression
**Effort:** Medium
**Dependencies:** MVP Hero System, Campaign Progression

**Description:**
Implement the system for unlocking additional heroes beyond the starting hero.

**Requirements:**
- Hero unlock conditions (campaign milestones, achievements)
- UI for hero collection/roster management
- Save/load integration for unlocked heroes
- Hero switching between campaigns/decks

**Notes:**
- **MVP**: Player chooses starting hero during onboarding (4 core elements + Random option)
- **Post-MVP**: This system allows unlocking additional heroes through gameplay
- Random option at start grants "Fortune Favors the Bold" profile bonus
- Adds long-term replayability with different hero builds

---

#### Design Hero In-Battle UI Elements
**Status:** ⬜ Not Started
**Category:** Heroes / UI
**Effort:** Medium
**Dependencies:** Implement Hero Stats System, Implement Hero Special Abilities

**Description:**
Design UI elements for displaying hero information and abilities during battle.

**Requirements:**
- Hero portrait/avatar
- Ability buttons/indicators
- Cooldown displays
- Visual integration with battle HUD

**Notes:**
- Should not clutter battlefield
- Abilities should be easy to use
- Clear cooldown/availability feedback

---

#### Integrate Heroes into Battle System
**Status:** ⬜ Not Started
**Category:** Heroes
**Effort:** Large
**Dependencies:** All other hero tasks

**Description:**
Final integration of hero system into the core battle gameplay loop.

**Requirements:**
- Heroes replace or augment base summoner
- All hero abilities functional in battle
- Proper save/load of hero state
- AI integration (if enemies can be heroes)
- Campaign integration

**Notes:**
- Final step - pulls everything together
- Extensive testing required
- May reveal balance issues

---

## Developer Tools

### 🟢 LOW PRIORITY

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

*Last Updated: 2025-11-25 - Added VFXIDs constants class with validation*
