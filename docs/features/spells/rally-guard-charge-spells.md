# Rally, Guard, and Charge Tactical Spell Cards

## Overview

Rally, Guard, and Charge are **0-mana neutral tactical spell cards** that provide unit control without resource cost. They replaced the old redirect button system, making tactical commands opt-in rather than innate abilities.

**Key Design Principles:**
- **Opt-in Tactical Play:** Players must include these cards in their deck to use them
- **Zero Mana Cost:** All three spells cost 0 mana, encouraging frequent tactical decision-making
- **Mixed Expiration:** Rally is event-based (zone cleared), Guard and Charge are time-based (25s and 30s)
- **No Refunds on Failure:** Casting with no units in range still consumes the card

---

## Two-Stage Targeting System

All three tactical spells use a **two-stage targeting system** implemented via `SpellTargetingManager` singleton:

**Stage 1: Circle Placement (First Click)**
- Player clicks to place selection circle
- Circle shows which units will be selected (8.0 unit radius)
- Selected units are tinted green

**Stage 2: Direction Arrow (Drag and Release)**
- Player drags to show direction arrow
- Arrow endpoint determines:
  - **Rally:** Where units move to and defend
  - **Guard:** Which direction the formation faces
  - **Charge:** Location to find nearest enemy target
- Release mouse to cast spell

**Technical Implementation:**
- `SpellTargetingManager` (autoload singleton) stores the arrow destination
- Cards retrieve destination via `SpellTargetingManager.get_rally_destination()`
- Destination is cleared after spell cast to prevent reuse

---

## Rally Spell

### Description
> "Command nearby units to move to a point and defend that zone until enemies are cleared."

### Behavior

**When Cast:**
1. Select all friendly alive units within **8.0 units** of the circle center
2. If no units in range:
   - Show fizzle VFX (purple puff)
   - Card consumed (no refund)
3. If units in range:
   - Set rally point to arrow destination
   - Set rally radius to **5.0 units**
   - Enable rally mode for selected units
   - Show blue circle VFX at rally point

**Unit Behavior (Rally Mode Active):**
1. **Move to Rally Point:**
   - Unit moves to rally point using normal pathfinding
   - If distance > 1.0 units from rally point, keep moving

2. **Defend the Zone:**
   - Once at rally point, scan for enemies within rally radius
   - If enemies found: Attack closest enemy (may chase outside zone)
   - If no enemies: Idle at rally point

3. **Clear Timer (Event-Based Expiration):**
   - Timer increments ONLY when no enemies are in the rally zone
   - Timer resets immediately when enemies enter zone
   - After **5 seconds** with no enemies in zone, rally mode ends
   - Unit resumes normal AI (push forward behavior)

### Tactical Use Cases

**Defensive Holding:**
- Rally units to defend a chokepoint
- Hold a position while you build up forces elsewhere
- Prevent units from overextending into dangerous areas

**Zone Control:**
- Clear and hold a specific area of the battlefield
- Deny enemy advancement through a region
- Protect a weak flank or vulnerable position

**Anti-Kiting:**
- Prevent ranged units from being pulled too far forward
- Keep units focused on objectives rather than chasing
- Stop units from walking into traps

### Technical Details

**Card Definition (`card_catalog.gd:425-462`):**
```gdscript
"rally": {
    "catalog_id": "rally",
    "card_name": "Rally",
    "card_type": 1,  # SPELL
    "mana_cost": 0,
    "cooldown": 1.0,
    "command_type": "rally",
    "selection_radius": 8.0,   # How far from cast position to select units
    "rally_radius": 5.0,        # Size of defend zone
}
```

**Unit State Variables (`unit_3d.gd`):**
```gdscript
var rally_point: Vector3 = Vector3.ZERO
var rally_radius: float = 5.0
var rally_mode: bool = false
var rally_clear_timer: float = 0.0
```

---

## Guard Spell

### Description
> "Command nearby units to form a defensive formation for 25 seconds. Melee units protect ranged units in the back line."

### Behavior

**When Cast:**
1. Select all friendly alive units within **8.0 units** of the circle center
2. If no units in range:
   - Show fizzle VFX (purple puff)
   - Card consumed (no refund)
3. If units in range:
   - Separate units into melee and ranged groups
   - Calculate formation positions using arc distribution
   - Set formation positions and enable guard mode
   - Show red markers at each formation position

**Formation Calculation:**
- **Melee Front Arc:**
  - 180-degree arc in front of formation center
  - Dynamic radius: `max(2.0, (unit_count × 1.5) / π)`
  - Units distributed evenly across the arc
  - Arc faces the direction indicated by arrow

- **Ranged Back Arc:**
  - 180-degree arc behind melee line
  - Dynamic radius: `melee_radius + 2.0` (minimum offset)
  - Also scales with unit count to prevent overlap

**Unit Behavior (Guard Mode Active):**
1. **Move to Formation Position:**
   - Unit moves to assigned formation_position
   - Uses normal pathfinding

2. **Hold Position:**
   - Once at position, unit attacks nearby enemies
   - Minimal movement (only to attack range)
   - Does NOT push forward

3. **Time-Based Expiration:**
   - Guard mode lasts **25 seconds** from activation
   - Timer counts down regardless of combat state
   - After expiration, units resume normal AI

### Tactical Use Cases

**Defensive Stance:**
- Create a defensive wall to hold a line
- Protect ranged units behind melee tanks
- Set up ambush positions before enemy engagement

**Regrouping:**
- Pull scattered units back into formation
- Reorganize after a messy fight
- Consolidate forces before a push

**Protecting Ranged Units:**
- Melee units shield ranged units from enemy melee
- Ensures ranged units maintain optimal distance
- Prevents fragile ranged units from being focused

### Technical Details

**Card Definition (`card_catalog.gd:465-502`):**
```gdscript
"guard": {
    "catalog_id": "guard",
    "card_name": "Guard",
    "card_type": 1,  # SPELL
    "mana_cost": 0,
    "cooldown": 1.0,
    "command_type": "guard",
    "selection_radius": 8.0,
    "formation_duration": 25.0,  # Seconds
}
```

**Unit State Variables (`unit_3d.gd`):**
```gdscript
var guard_mode: bool = false
var guard_timer: float = 0.0
var formation_position: Vector3 = Vector3.ZERO
```

**Formation Calculator (`unit_3d.gd:994-1041`):**
```gdscript
static func calculate_formation_positions(units: Array[Unit3D], center: Vector3)
```
- Static method - no instance required
- Separates units by type (melee vs ranged)
- Calculates dynamic radii based on unit count
- Distributes units across 180-degree arcs
- Sets `formation_position`, `guard_mode`, and `guard_timer` for each unit

---

## Charge Spell

### Description
> "Command nearby units to launch a coordinated attack on the closest enemy (unit, structure, or Incarnation) to the target location for 30 seconds."

### Behavior

**When Cast:**
1. Select all friendly alive units within **8.0 units** of the circle center
2. If no units in range:
   - Show fizzle VFX (purple puff)
   - Card consumed (no refund)
3. If units in range:
   - Find closest enemy to arrow destination (searches units, structures, AND Incarnations)
   - If no valid target found: Spell fizzles
   - Set `forced_target` on each selected unit
   - Set `forced_target_timer` to **30 seconds**

**Unit Behavior (Charge Mode Active):**
1. **Immediate Target Switching:**
   - Units immediately switch to forced_target, even if already in combat
   - Overrides current target and target lock timer
   - Units will pursue forced_target regardless of distance

2. **Attack Forced Target:**
   - Unit moves toward and attacks forced_target
   - Normal combat behavior applies (attack range, damage, etc.)
   - Will chase target across the battlefield

3. **Time-Based Expiration:**
   - Charge mode lasts **30 seconds** from activation
   - Timer counts down regardless of combat state
   - After expiration, units resume normal AI (acquire new targets)
   - If forced_target dies before timer expires, unit resumes normal AI immediately

### Tactical Use Cases

**Focused Fire:**
- Coordinate multiple units to quickly eliminate a high-priority target
- Overwhelm tanky enemies with concentrated damage
- Ensure scattered units all attack the same target

**Structure Destruction:**
- Charge enemy towers or walls to quickly break defenses
- Direct assault on enemy Incarnation for final push
- Ignore distractions and focus on objective

**Breaking Enemy Lines:**
- Punch through enemy formations to reach high-value targets
- Bypass frontline tanks to eliminate ranged threats
- Force units to ignore nearby enemies and push to backline

**Incarnation Rush:**
- Direct all units to attack enemy Incarnation
- Ignore all other enemies and structures
- Win condition: Destroy the Incarnation before timer expires

### Technical Details

**Card Definition (`card_catalog.gd:503-540`):**
```gdscript
"charge": {
    "catalog_id": "charge",
    "card_name": "Charge",
    "card_type": 1,  # SPELL
    "mana_cost": 0,
    "cooldown": 1.0,
    "command_type": "charge",
    "selection_radius": 8.0,
}
```

**Unit State Variables (`unit_3d.gd`):**
```gdscript
var forced_target: Node3D = null
var forced_target_timer: float = 0.0
```

**Forced Target System:**
- Units check `forced_target` BEFORE normal target acquisition
- Forced targets bypass distance validation (will chase across entire battlefield)
- Forced targets override target lock timer (immediate switch)
- Timer decrements in `_physics_process()` via `forced_target_timer -= delta`
- When timer expires or forced_target dies, unit resumes normal AI

**Target Finding (`card.gd:_apply_charge_command`):**
```gdscript
var closest_enemy: Node3D = RedirectManager.find_nearest_enemy(
    charge_dest,  # Arrow endpoint
    caster_team,
    RedirectManager.TARGET_SEARCH_RADIUS
)
```
- Uses `RedirectManager` to search for ANY enemy (units, structures, Incarnations)
- Finds closest enemy to arrow destination, not to unit positions
- This allows precise targeting of specific structures or backline units

---

## Visual Feedback (VFX)

### Current Implementation (Placeholder)

All VFX currently use **procedural geometry placeholders** to provide immediate feedback during foundation phase.

**Rally VFX:**
- Blue translucent cylinder at rally point
- Radius matches rally_radius (5.0 units)
- Auto-cleans up after 1.5 seconds
- Color: `Color(0.2, 0.5, 1.0, 0.6)`

**Guard VFX:**
- Red translucent boxes at each formation position
- Size: 0.5 × 0.2 × 0.5 units
- Auto-cleans up after 1.0 seconds
- Color: `Color(0.8, 0.3, 0.2, 0.6)`

**Charge VFX:**
- Currently reuses Rally VFX system
- Shows blue circle at target location
- Future: Orange/red aggressive marker

**Failed Cast VFX:**
- Purple sphere when spell fails (no units in range)
- Size: 0.3 radius sphere
- Auto-cleans up after 0.5 seconds
- Color: `Color(0.6, 0.4, 0.6, 0.8)`

---

## Implementation Architecture

### Spell Casting Flow

**1. Player Begins Targeting**
- `BattlefieldDropZone` detects card drag-over
- Activates `SpellTargetingManager._start_targeting()`
- Targeting manager takes over input handling

**2. Player Places Circle (First Click)**
- `SpellTargetingManager._handle_targeting_input()` places circle
- Circle position determines unit selection radius
- Enters drag state, waiting for arrow direction

**3. Player Drags Arrow (Hold and Drag)**
- Targeting manager draws arrow from circle to cursor
- Arrow endpoint stored in `rally_destination`
- Visual feedback shows direction and destination

**4. Player Releases to Cast**
- `SpellTargetingManager._cast_spell()` called
- CRITICAL: `set_process(false)` called BEFORE `play_card_3d()` to prevent race condition
- Destination stored in singleton, ready for card to retrieve

**5. Summoner Validates and Executes**
- Checks mana cost (all tactical spells are 0 mana)
- Applies cooldown (1.0 second default)
- Calls `card.play_3d(circle_position, team, battlefield)`

**6. Card Routes to Command Handler**
- `card.gd:_cast_spell_3d()` checks for `command_type` in card definition
- Routes to `_cast_command_spell()` instead of normal spell logic

**7. Command Spell Retrieves Destination**
- Card calls `SpellTargetingManager.get_rally_destination()`
- Gets arrow endpoint (NOT circle center)
- Uses destination for spell-specific logic

**8. Command Spell Selection**
- Scans for friendly units in selection radius (8.0)
- Filters by team and is_alive
- If no units found: Show fizzle VFX and return (card consumed)

**9. Apply Command Effect**
- **Rally:** `_apply_rally_command()` sets rally state, spawns VFX
- **Guard:** `_apply_guard_command()` calls formation calculator, spawns VFX
- **Charge:** `_apply_charge_command()` finds target, sets forced_target on units

**10. Unit AI Responds**
- `unit_3d.gd:_physics_process()` checks priority hierarchy:
  1. Rally mode (if `rally_mode == true`)
  2. Guard mode (if `guard_mode == true`)
  3. Forced target (if `forced_target != null and forced_target_timer > 0`)
  4. Normal AI (default push-forward behavior)
- Units execute special behavior until mode expires

### Key Design Patterns

**Two-Stage Targeting:**
- Separates unit selection from command direction
- Allows precise tactical control
- Circle = "who", Arrow = "where/what"
- Prevents accidental misclicks

**Event-Based Expiration (Rally):**
- Timer driven by game state (enemy presence) not arbitrary time
- More tactical: Players can predict when rally ends
- Prevents confusing scenarios where timer expires mid-combat

**Time-Based Expiration (Guard & Charge):**
- Fixed duration regardless of combat
- Simpler mental model for temporary effects
- Prevents infinite camping (Guard) or permanent redirects (Charge)
- Guard: 25 seconds (defensive hold duration)
- Charge: 30 seconds (enough time to cross battlefield and demolish targets)

**Forced Target System (Charge):**
- Overrides normal targeting AI
- Bypasses distance checks (will chase across map)
- Immediately switches targets (breaks target lock)
- Clean expiration: Timer-based + target death fallback

**No Refunds on Failure:**
- Teaches players to check unit positioning before casting
- Adds skill expression: Good players waste fewer cards
- Balanced by 0 mana cost (wasting one isn't severely punishing)

---

## Testing Checklist

When testing tactical spells:

**Rally:**
- [ ] Two-stage targeting: Circle placement → Arrow drag → Release
- [ ] Arrow destination used (NOT circle center)
- [ ] Selects units within 8.0 radius of circle
- [ ] Fizzle VFX when no units in range (card consumed)
- [ ] Blue circle VFX at arrow destination on successful cast
- [ ] Units move to arrow destination and defend the zone
- [ ] Rally timer only increments when zone empty of enemies
- [ ] Rally mode clears after 5 seconds with no enemies
- [ ] Units resume normal AI after rally ends

**Guard:**
- [ ] Two-stage targeting: Circle placement → Arrow drag → Release
- [ ] Formation faces direction of arrow
- [ ] Selects units within 8.0 radius of circle
- [ ] Fizzle VFX when no units in range (card consumed)
- [ ] Red markers at formation positions
- [ ] Melee units form front arc, ranged units form back arc
- [ ] Formation scales properly with 10+ units (no overlap)
- [ ] Guard mode lasts exactly 25 seconds
- [ ] Units resume normal AI after guard expires

**Charge:**
- [ ] Two-stage targeting: Circle placement → Arrow drag → Release
- [ ] Finds closest enemy to arrow destination (NOT circle)
- [ ] Selects units within 8.0 radius of circle
- [ ] Fizzle VFX when no units in range (card consumed)
- [ ] Fizzle VFX when no valid target found near arrow
- [ ] Can target enemy units, structures, AND Incarnations
- [ ] Units immediately switch targets (even mid-combat)
- [ ] Units chase forced_target across entire battlefield
- [ ] Charge lasts exactly 30 seconds
- [ ] Units resume normal AI after timer expires
- [ ] Units resume normal AI if forced_target dies early

**All Spells:**
- [ ] All cost 0 mana
- [ ] All have 1 second cooldown between casts
- [ ] SpellTargetingManager singleton accessible from cards
- [ ] No multiple instance bugs (single autoload)
- [ ] Destination cleared after cast (no reuse)

---

## Known Limitations & Future Work

### Current Limitations

**VFX:**
- Placeholder procedural geometry instead of particle effects
- No persistent rally zone indicator (circle disappears after 1.5s)
- Formation markers don't scale with formation size
- Charge has no unique VFX (reuses rally visuals)

**UI Feedback:**
- No on-screen indication of rally/guard/charge status
- Players can't see how many units are in each mode
- No visual timer for durations
- No unit-attached indicators showing current command

**Selection Feedback:**
- No preview of which units will be selected before casting
- Players must mentally estimate the 8.0 radius
- No way to see selection radius during targeting

### Planned Improvements (Polish Phase)

**VFX Enhancements:**
- Proper particle effects for all three spells
- Persistent rally zone indicator (glowing circle on ground)
- Unit-attached VFX showing current mode (rally/guard/charge)
- Selection preview circle when holding card
- Charge: Unique aggressive VFX (orange/red theme)

**UI Additions:**
- Status icons above units showing active command
- On-screen counter: "5 units in Rally mode"
- Visual timer bars for Guard (25s) and Charge (30s)
- Rally clear timer indicator

**Gameplay Tweaks:**
- Player-adjustable rally radius (card upgrade system?)
- Formation shape options (arc, line, box, wedge)
- Rally mode: Allow player to move rally point mid-defense
- Guard mode: Manual rotation control for formation facing

**Quality of Life:**
- Selection preview when targeting
- Rally point marker that persists until mode ends
- Sound effects for spell casting and mode expiration
- Tutorial tooltips explaining two-stage targeting
- Charge: Show target indicator on forced_target

---

## Code Reference

**Primary Files:**
- `scripts/ui/spell_targeting_manager.gd` - Two-stage targeting system (singleton)
- `scripts/ui/battlefield_drop_zone.gd` - Card drag-and-drop, forwards to targeting manager
- `scripts/cards/card.gd:282-571` - Command spell casting, VFX, and effect application
- `scripts/units/unit_3d.gd:90-99` - Rally/Guard/Charge state variables
- `scripts/units/unit_3d.gd:413-450` - Forced target system (Charge)
- `scripts/units/unit_3d.gd:849-987` - Rally/Guard AI behavior
- `scripts/units/unit_3d.gd:994-1041` - Guard formation calculator
- `scripts/managers/redirect_manager.gd` - Enemy search for Charge targeting
- `scripts/data/card_catalog.gd:425-540` - Rally/Guard/Charge card definitions
- `scripts/data/card_ids.gd:24-26` - CardIDs constants (RALLY, GUARD, CHARGE)

**Key Constants:**
- Selection Radius: 8.0 units (all spells)
- Rally Radius: 5.0 units
- Rally Clear Time: 5.0 seconds
- Guard Duration: 25.0 seconds
- Charge Duration: 30.0 seconds
- Formation Unit Spacing: 1.5 units
- Formation Min Radius: 2.0 units
- Mana Cost: 0 (all spells)
- Cooldown: 1.0 seconds (all spells)

**Critical Bug Fixes (Historical):**
- **Multiple Singleton Instances:** Rally_guard_test.tscn manually added SpellTargetingManager node, creating duplicate instance separate from autoload. Fixed by removing manual node and using autoload directly.
- **Wrong Singleton Access:** Used `Engine.get_singleton("SpellTargetingManager")` which only works for built-in engine singletons. Fixed by using direct autoload access: `SpellTargetingManager.get_rally_destination()`.
- **Race Condition:** `_end_targeting()` was clearing `rally_destination` before card could retrieve it. Fixed by calling `set_process(false)` BEFORE `play_card_3d()`.
- **Charge Targeting Limitation:** Only searched units groups, couldn't target towers/Incarnations. Fixed by using `RedirectManager.find_nearest_enemy()` which searches all enemy types.
- **Charge Not Switching Targets:** Units didn't switch to forced_target if already in combat. Fixed by adding forced_target as trigger for immediate target reacquisition.
- **Charge Duration Too Short:** 10 seconds wasn't enough to cross battlefield. Increased to 30 seconds.
- **Guard Duration Too Short:** 10 seconds wasn't enough for meaningful defense. Increased to 25 seconds.
