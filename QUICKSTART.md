# Project Summoner - Quick Start

## Parse Error Fix

If you're getting "Parse error" messages, this is likely due to how Godot handles script loading order. Here's what to do:

### Fix Steps:

1. **Open Godot Project**
2. **Reimport Scripts**: Go to Project > Reload Current Project
3. **Check Script Order**: Godot needs to load base classes first
   - `unit.gd` should load first (defines Unit.Team enum)
   - `card.gd` depends on Unit
   - `summoner.gd` depends on both

4. **Manually verify each script**:
   - Open each `.gd` file in Godot's script editor
   - Look for red error markers
   - Common issues:
     - Missing `Unit.Team` reference (needs unit.gd loaded first)
     - Typos in class names

### Quick Test Setup:

1. **Set Project Settings**:
   - Project > Project Settings > General > Run
   - Set Main Scene to `res://scenes/battlefield/test_game.tscn`

2. **Open test_game.tscn**

3. **Add Card Resources to Summoners**:
   - Select `PlayerSummoner` node
   - In Inspector, find `Starting Deck` (Array of Card)
   - Click to expand array
   - Click "+" to add element
   - Click "Load" and select `res://resources/cards/warrior_card.tres`
   - Add 10 copies (or duplicate the array element)
   - Repeat for `EnemySummoner`

4. **Fix Unit Collision**:
   - Open `scenes/units/basic_unit.tscn`
   - Select `CollisionShape2D` node
   - In Inspector, under Shape, create new CircleShape2D
   - Set Radius to 16

5. **Add Player Input**:
   - In test_game.tscn, right-click `PlayerSummoner`
   - Add Child Node > Node > Rename to "PlayerInput"
   - Attach script: `res://scripts/core/player_input.gd`

6. **Press F5 to Run!**

## How It Should Work

- Timer counts down from 3:00
- Enemy AI spawns units automatically every 3-6 seconds
- You click to spawn units (if you have mana)
- Units auto-attack enemies
- Game ends when base HP reaches 0

## Common Issues

### "Unknown identifier 'Unit'" in card.gd
**Fix**: Make sure unit.gd is saved and doesn't have errors

### Units not spawning
**Fix**: Check that starting_deck has cards assigned in Inspector

### No collision detection
**Fix**: Add CircleShape2D to CollisionShape2D in basic_unit.tscn

### Player can't spawn units
**Fix**: Make sure PlayerInput node is added and script is attached

## File Reference

Core scripts:
- `scripts/units/unit.gd` - Base unit with HP, attack, AI
- `scripts/cards/card.gd` - Card resource for summons/spells
- `scripts/core/summoner.gd` - Player base, manages deck/mana
- `scripts/core/game_controller.gd` - Match timer and victory
- `scripts/core/simple_ai.gd` - Enemy AI behavior
- `scripts/core/player_input.gd` - Mouse/keyboard controls

Scenes:
- `scenes/battlefield/test_game.tscn` - Main test scene
- `scenes/units/basic_unit.tscn` - Basic unit prefab

Resources:
- `resources/cards/warrior_card.tres` - Test summon card

## Controls

- **Left Click**: Spawn selected card at cursor
- **Right Click**: Cycle through hand
- **1-4 Keys**: Select card in hand

## Next Steps

After getting the test working:
1. Create more unit types (duplicate basic_unit.tscn, adjust stats)
2. Create spell cards (set CardType to SPELL, set damage/radius)
3. Add UI for card hand display
4. Add visual effects for attacks and spells
5. Create different unit behaviors (ranged, tank, support)
