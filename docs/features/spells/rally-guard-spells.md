# Rally, Guard, and Charge Tactical Spell Cards

## Overview

Rally, Guard, and Charge are **neutral tactical spell cards** that provide unit control. Rally and Guard cost 0 mana, while Charge costs 1 mana. They replaced the old redirect button system, making tactical commands opt-in rather than innate abilities.

**Key Design Principles:**
- **Opt-in Tactical Play:** Players must include these cards in their deck to use them
- **Zero Mana Cost:** Encourages frequent tactical decision-making
- **Event-Based Expiration:** Commands expire based on combat state, not arbitrary timers
- **No Refunds on Failure:** Casting with no units in range still consumes the card

---

## Rally Spell

### Description
> "Command nearby units to move to a point and defend that zone. Units guard the area until all enemies are cleared."

### Behavior

**When Cast:**
1. Select all friendly alive units within **8.0 units** of the cast position
2. If no units are in range:
   - Show fizzle VFX (purple puff)
   - Card is consumed (no refund)
   - Print warning to console
3. If units are in range:
   - Set rally point to cast position
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
    "categories": {
        "elemental_affinity": ElementTypes.NEUTRAL
    }
}
```

**Unit State Variables (`unit_3d.gd:90-94`):**
```gdscript
var rally_point: Vector3 = Vector3.ZERO
var rally_radius: float = 5.0
var rally_mode: bool = false
var rally_clear_timer: float = 0.0
```

**AI State Machine:**
- Priority: Rally mode overrides normal AI when active
- Location: `unit_3d.gd:849-935`
- Key Functions:
  - `_update_rally_behavior(delta)`: Main rally mode logic
  - `_check_enemies_in_rally_zone()`: Separate enemy detection for timer
  - `_defend_rally_zone(delta)`: Combat logic while defending
  - `_clear_rally_mode()`: Cleanup and return to normal AI

**Critical Implementation Detail:**
Rally timer tracks enemy presence in the zone **independently** of unit position. This prevents the timer from resetting when units chase enemies outside the zone, ensuring rally mode expires properly once the zone is cleared.

---

## Guard Spell

### Description
> "Command nearby units to form a defensive formation. Melee units protect ranged units in the back line."

### Behavior

**When Cast:**
1. Select all friendly alive units within **8.0 units** of the cast position
2. If no units are in range:
   - Show fizzle VFX (purple puff)
   - Card is consumed (no refund)
   - Print warning to console
3. If units are in range:
   - Separate units into melee and ranged groups
   - Calculate formation positions using arc distribution
   - Set formation positions and enable guard mode
   - Show red markers at each formation position

**Formation Calculation:**
- **Melee Front Arc:**
  - 180-degree arc in front of formation center
  - Dynamic radius: `max(2.0, (unit_count × 1.5) / π)`
  - Units distributed evenly across the arc

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
   - Guard mode lasts **10 seconds** from activation
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

**Formation Scaling:**
- Small groups (1-5 units): Tight formation (2-3 unit radius)
- Medium groups (6-10 units): Medium spread (4-6 unit radius)
- Large groups (10+ units): Wide formation (7+ unit radius)
- Formula prevents unit overlap regardless of army size

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
    "formation_duration": 10.0,  # Seconds
    "categories": {
        "elemental_affinity": ElementTypes.NEUTRAL
    }
}
```

**Unit State Variables (`unit_3d.gd:96-99`):**
```gdscript
var guard_mode: bool = false
var guard_timer: float = 0.0
var formation_position: Vector3 = Vector3.ZERO
```

**Formation Calculator (`unit_3d.gd:994-1041`):**
```gdscript
static func calculate_formation_positions(units: Array[Unit3D], center: Vector3)
```
- **Static method** - No instance required, utility function
- Separates units by type (melee vs ranged)
- Calculates dynamic radii based on unit count
- Distributes units across 180-degree arcs using trigonometry
- Sets `formation_position`, `guard_mode`, and `guard_timer` for each unit

**Formation Scaling Math:**
```
arc_length = unit_count × 1.5  # 1.5 units spacing between each unit
radius = max(MIN_RADIUS, arc_length / π)  # π because 180° = π radians

For melee:
  MIN_RADIUS = 2.0
  front_radius = max(2.0, (melee_count × 1.5) / π)

For ranged:
  back_radius = front_radius + 2.0  # Always behind melee
  back_radius = max(back_radius, (ranged_count × 1.5) / π)
```

**AI State Machine:**
- Priority: Guard mode overrides normal AI when active
- Location: `unit_3d.gd:945-987`
- Key Functions:
  - `_update_guard_behavior(_delta)`: Main guard mode logic
  - Timer decrements in `_physics_process()` via guard_timer -= delta
  - When `guard_timer <= 0`, guard mode disabled

---

## Visual Feedback (VFX)

### Current Implementation (Placeholder)

All VFX currently use **procedural geometry placeholders** to provide immediate feedback while the game is in foundation-building phase. These will be replaced with proper VFX scenes during the polish phase.

**Rally VFX:**
- Blue translucent cylinder at rally point
- Radius matches rally_radius (5.0 units default)
- Auto-cleans up after 1.5 seconds
- Color: `Color(0.2, 0.5, 1.0, 0.6)` (blue, 60% opacity)

**Guard VFX:**
- Red translucent boxes at each formation position
- Size: 0.5 × 0.2 × 0.5 units
- Auto-cleans up after 1.0 seconds
- Color: `Color(0.8, 0.3, 0.2, 0.6)` (red, 60% opacity)

**Failed Cast VFX:**
- Purple sphere when spell fails (no units in range)
- Size: 0.3 radius sphere
- Auto-cleans up after 0.5 seconds
- Color: `Color(0.6, 0.4, 0.6, 0.8)` (purple, 80% opacity)

### Future VFX Integration

The system is designed to integrate with VFXManager for proper particle effects:

**Code Structure:**
```gdscript
func _spawn_rally_vfx(rally_point: Vector3, rally_radius: float) -> void:
    # Try to use VFXManager first
    if VFXManager and VFXManager.has_effect("rally_circle"):
        VFXManager.play_effect("rally_circle", rally_point, {"radius": rally_radius})
        return

    # Fallback to placeholder
    _spawn_placeholder_circle(rally_point, rally_radius, Color(0.2, 0.5, 1.0, 0.6))
```

**Required VFX Assets (Future):**
1. `rally_circle.tres` - VFXDefinition for rally selection circle
2. `guard_marker.tres` - VFXDefinition for formation position markers
3. `spell_fizzle.tres` - VFXDefinition for failed cast feedback

**VFX Scenes to Create:**
1. `rally_circle.tscn` - Expanding circle particle effect with trail
2. `guard_marker.tscn` - Glowing position marker with fade-in
3. `spell_fizzle.tscn` - Sparkle/puff particle effect for failures

---

## Implementation Architecture

### Spell Casting Flow

**1. Player Drops Card on Battlefield**
- `battlefield_drop_zone.gd:_drop_data()` converts screen to world position
- Calls `summoner_3d.play_card_3d(card_index, world_pos_3d)`

**2. Summoner Validates and Executes**
- Checks mana cost (Rally/Guard are 0 mana)
- Applies cooldown (1.0 seconds default)
- Calls `card.play_3d(world_pos_3d, team, battlefield)`

**3. Card Routes to Command Handler**
- `card.gd:_cast_spell_3d()` checks for `command_type` in card definition
- If found, routes to `_cast_command_spell()` instead of normal spell logic

**4. Command Spell Selection**
- `_cast_command_spell()` scans for friendly units in selection radius (8.0)
- Filters by team and is_alive
- If no units found: Show fizzle VFX and return (card consumed, no refund)

**5. Apply Command Effect**
- **Rally:** `_apply_rally_command()` sets rally state on each unit, spawns VFX
- **Guard:** `_apply_guard_command()` calls static formation calculator, spawns VFX

**6. Unit AI Responds**
- `unit_3d.gd:_physics_process()` checks priority hierarchy:
  1. Rally mode (if `rally_mode == true`)
  2. Guard mode (if `guard_mode == true`)
  3. Normal AI (default push-forward behavior)
- Units execute special behavior until mode expires

### Key Design Patterns

**Event-Based Expiration (Rally):**
- Timer driven by game state (enemy presence) not arbitrary time
- More tactical: Players can predict when rally ends
- Prevents confusing scenarios where timer expires mid-combat

**Time-Based Expiration (Guard):**
- Fixed duration regardless of combat
- Simpler mental model for formation holding
- Prevents infinite guard camping

**Static Formation Calculator:**
- Utility function, no instance needed
- Can be called from anywhere (cards, AI, future systems)
- Pure function: Input = units + center, Output = modified unit states

**VFX Fallback Pattern:**
- Try VFXManager first (for proper particle effects)
- Fall back to procedural geometry if VFX not available
- Ensures functionality works during foundation phase
- Easy to upgrade later by adding VFX scenes to resources/vfx/

**No Refunds on Failure:**
- Teaches players to check unit positioning before casting
- Adds skill expression: Good players waste fewer cards
- Matches design: These are 0-mana, so wasting one isn't punishing

---

## Testing Checklist

When testing Rally and Guard spells:

- [ ] Rally selects units within 8.0 radius
- [ ] Rally shows fizzle VFX when no units in range (card consumed)
- [ ] Rally shows blue circle VFX on successful cast
- [ ] Units move to rally point and defend the zone
- [ ] Rally timer only increments when zone is empty of enemies
- [ ] Rally mode clears after 5 seconds with no enemies
- [ ] Units resume normal AI after rally ends
- [ ] Guard selects units within 8.0 radius
- [ ] Guard shows fizzle VFX when no units in range (card consumed)
- [ ] Guard shows red markers at formation positions
- [ ] Melee units form front arc, ranged units form back arc
- [ ] Formation scales properly with 10+ units (no overlap)
- [ ] Guard mode lasts exactly 10 seconds
- [ ] Units resume normal AI after guard expires
- [ ] Both spells cost 0 mana
- [ ] Both spells have 1 second cooldown between casts

---

## Known Limitations & Future Work

### Current Limitations

**VFX:**
- Placeholder procedural geometry instead of particle effects
- No persistent rally zone indicator (circle disappears after 1.5s)
- Formation markers don't scale with formation size

**UI Feedback:**
- No on-screen indication of rally/guard status
- Players can't see how many units are in rally/guard mode
- No visual timer for guard duration or rally clear timer

**Selection Feedback:**
- No visual indication of which units will be selected before casting
- Players must mentally estimate the 8.0 radius
- No way to see selection radius in-game

### Planned Improvements (Polish Phase)

**VFX Enhancements:**
- Proper particle effects for rally/guard
- Persistent rally zone indicator (glowing circle on ground)
- Unit-attached VFX showing rally/guard status
- Selection preview circle when hovering card over battlefield

**UI Additions:**
- Rally/Guard status icons above units
- On-screen counter: "5 units in Rally mode"
- Visual timer bar for guard duration
- Rally clear timer indicator

**Gameplay Tweaks:**
- Player-adjustable rally radius (card upgrade system?)
- Formation shape options (arc, line, box, wedge)
- Rally mode: Allow player to move rally point mid-defense
- Guard mode: Add rotation parameter for facing direction

**Quality of Life:**
- Selection preview when hovering card over battlefield
- Rally point marker that persists until mode ends
- Sound effects for spell casting and mode expiration
- Tutorial tooltips explaining behavior

---

## Code Reference

**Primary Files:**
- `scripts/cards/card.gd:282-571` - Command spell casting and VFX
- `scripts/units/unit_3d.gd:90-99` - Rally/Guard state variables
- `scripts/units/unit_3d.gd:848-987` - Rally/Guard AI behavior
- `scripts/units/unit_3d.gd:994-1041` - Formation calculator
- `scripts/data/card_catalog.gd:425-502` - Rally/Guard card definitions
- `scripts/data/card_ids.gd:24-25` - CardIDs.RALLY and CardIDs.GUARD constants

**Key Constants:**
- Selection Radius: 8.0 units
- Rally Radius: 5.0 units
- Rally Clear Time: 5.0 seconds
- Guard Duration: 10.0 seconds
- Formation Unit Spacing: 1.5 units
- Formation Min Radius: 2.0 units
- Mana Cost: 0
- Cooldown: 1.0 seconds
