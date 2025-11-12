# Project TODOs

This document tracks planned features, improvements, and tasks for Project Summoner.

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

#### Fix Projectile Aiming on Moving Targets
**Status:** ✅ Completed (2025-11-12)
**Category:** Units & Combat
**Effort:** Medium

**Description:**
Implement predictive targeting for projectiles so they lead moving targets instead of aiming at current position.

**Requirements:**
- Calculate target's future position based on velocity
- Compute intercept point using projectile speed and target movement
- Update projectile direction to aim at intercept point
- Handle edge cases (target stops, changes direction, dies mid-flight)
- Test with different projectile speeds and target velocities

**Notes:**
- Significantly improves ranged unit effectiveness and game feel
- Should work for both homing and straight-line projectiles
- May need to account for acceleration/deceleration curves
- Important for combat balance - currently projectiles miss moving targets frequently

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

## Core Game Systems

### 🔴 HIGH PRIORITY

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
**Status:** ⬜ Not Started
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

### 🔴 HIGH PRIORITY

#### Add Building Hit/Damage Animation
**Status:** ✅ Completed (2025-11-12)
**Category:** Visual Polish
**Effort:** Small

**Description:**
Add visual feedback when buildings (summoner bases) take damage.

**Requirements:**
- Create shake/flash animation
- Add impact particle effects
- Trigger on damage events

**Notes:**
- Should clearly communicate damage to player
- Don't obscure important information
- Implemented with dynamic flash speed based on attack intensity

---

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

#### Revamp Pause Menu
**Status:** ✅ Completed (2025-11-12)
**Category:** UI/UX
**Effort:** Small

**Description:**
Improve pause menu design and functionality.

**Requirements:**
- Clear options (Resume, Settings, Quit, etc.)
- Visual polish
- Proper background overlay
- Smooth transitions

**Notes:**
- Should not feel intrusive
- Easy to resume gameplay
- Implemented with ESC key support and pause button in battle HUD

---

## Campaign System

### 🔴 HIGH PRIORITY

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

#### Implement Hero Roll/Randomization System
**Status:** ⬜ Not Started
**Category:** Heroes
**Effort:** Medium
**Dependencies:** Design Hero Data Structure

**Description:**
Implement the system for random hero selection/roll mechanics (roguelike style).

**Requirements:**
- Random hero generation/selection
- Rarity/tier system (if applicable)
- Unlock progression
- Save/load integration

**Notes:**
- Adds replayability
- Consider if this replaces or complements direct selection
- Balance unlock curve

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

*Last Updated: 2025-11-12 - Marked completed items: Predictive targeting, building hit animations, pause menu*
