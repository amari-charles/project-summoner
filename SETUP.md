# Project Summoner - Setup Guide

## Core Structure

The project is organized as follows:

```
project-summoner/
├── scripts/
│   ├── units/
│   │   └── unit.gd              # Base unit class
│   ├── cards/
│   │   └── card.gd              # Card resource class
│   ├── core/
│   │   ├── game_controller.gd   # Match flow manager
│   │   ├── summoner.gd          # Player base/summoner
│   │   ├── simple_ai.gd         # Basic AI controller
│   │   └── player_input.gd      # Player input handler
│   └── ui/
│       └── game_ui.gd           # UI manager
├── scenes/
│   ├── battlefield/
│   │   ├── battlefield.tscn     # Main battlefield area
│   │   ├── main_game.tscn       # Basic game scene
│   │   └── test_game.tscn       # Test scene with AI
│   └── units/
│       └── basic_unit.tscn      # Basic unit scene
└── resources/
    └── cards/
        └── warrior_card.tres     # Test warrior card
```

## How to Set Up in Godot Editor

### 1. Open the Project
- Launch Godot 4
- Open the `project-summoner` folder

### 2. Set the Main Scene
1. Open `scenes/battlefield/test_game.tscn`
2. Go to **Project > Project Settings > Application > Run**
3. Set **Main Scene** to `res://scenes/battlefield/test_game.tscn`

### 3. Configure the Test Scene

Open `test_game.tscn` and verify the following connections:

#### PlayerSummoner Node:
1. Select `PlayerSummoner` in the scene tree
2. In the Inspector, scroll to **Script Variables**
3. Assign test cards to `starting_deck`:
   - Click the Array dropdown
   - Add elements and load `res://resources/cards/warrior_card.tres`
   - Add 10-15 copies for testing

#### EnemySummoner Node:
1. Select `EnemySummoner`
2. Assign the same warrior cards to its `starting_deck`

#### GameController Node (TestGame root):
1. Select the root `TestGame` node
2. In Inspector, assign references:
   - **Battlefield**: drag `Battlefield` node from tree
   - **Player Summoner**: drag `PlayerSummoner` node
   - **Enemy Summoner**: drag `EnemySummoner` node

#### Add Player Input Handler:
1. Right-click `PlayerSummoner` node
2. **Add Child Node** > search for `Node` > Create
3. Rename it to `PlayerInput`
4. Attach script: `res://scripts/core/player_input.gd`

### 4. Add Collision Shapes to Units

Open `scenes/units/basic_unit.tscn`:
1. Select `CollisionShape2D` child node
2. In Inspector, under **Shape**, create a new **CircleShape2D**
3. Set radius to **16**

### 5. Configure Project Physics (Optional but Recommended)

1. **Project > Project Settings > Physics > 2D**
2. Set **Default Gravity** to `0` (top-down game, no gravity)

## How to Run a Test Battle

### Play the Scene:
1. Open `test_game.tscn`
2. Press **F5** (or click Play Scene)

### Controls:
- **Left Click**: Spawn a unit at cursor position (uses selected card)
- **Right Click**: Cycle through cards in hand
- **1, 2, 3, 4 Keys**: Select card from hand directly

### What Should Happen:
1. Match timer starts at 3:00 (countdown)
2. Both summoners (bases) appear at top and bottom
3. Player and enemy start with mana regenerating
4. Enemy AI automatically spawns units every 3-6 seconds
5. Units automatically move toward enemies and attack
6. HP bars show above units and summoners
7. Game ends when either summoner reaches 0 HP or time runs out

## Next Steps

### Create More Units:
1. Duplicate `basic_unit.tscn`
2. Rename and adjust stats in Inspector (HP, damage, range, speed)
3. Modify `_setup_visuals()` in script for different colors

### Create More Cards:
1. Right-click in FileSystem > **New Resource**
2. Search for **Card** > Create
3. Set properties:
   - Card Name
   - Card Type (Summon or Spell)
   - Mana Cost
   - Unit Scene (for summons)
   - Spell values (for spells)
4. Save as `.tres` file in `resources/cards/`

### Implement Card UI:
- Create card hand display at bottom of screen
- Show card icons and mana costs
- Visual feedback for selected card

### Add Visual Effects:
- Attack animations
- Damage numbers
- Spell effects
- Death particles

## Troubleshooting

### Units not moving/attacking:
- Ensure collision shapes are added to units
- Check that units are in correct groups (player_units/enemy_units)

### Cards not spawning units:
- Verify `unit_scene` is assigned in card resource
- Check that battlefield is in "battlefield" group

### No mana regeneration:
- Confirm `mana_regen_rate` > 0 in Summoner nodes

### AI not playing cards:
- Check `SimpleAI` node is child of `EnemySummoner`
- Verify enemy summoner has cards in starting_deck

## Core Systems Explained

### Unit System (`unit.gd`):
- Auto-targets nearest enemy
- Moves within attack range
- Attacks on cooldown
- Dies at 0 HP

### Card System (`card.gd`):
- Single-use resource
- Summon units or cast spells
- Validated by mana cost

### Summoner System (`summoner.gd`):
- Player base with HP
- Manages deck, hand, and mana
- Draws cards automatically
- Game ends when destroyed

### Game Controller (`game_controller.gd`):
- 3-minute match timer
- Victory conditions (base destroyed or timeout)
- Overtime system for ties

## File Paths Reference

- Unit script: `scripts/units/unit.gd:1`
- Card script: `scripts/cards/card.gd:1`
- Summoner script: `scripts/core/summoner.gd:1`
- GameController script: `scripts/core/game_controller.gd:1`
- AI script: `scripts/core/simple_ai.gd:1`
- Player input: `scripts/core/player_input.gd:1`
- Test scene: `scenes/battlefield/test_game.tscn`

---

**You're ready to test!** Open `test_game.tscn` and press F5 to start your first battle!
