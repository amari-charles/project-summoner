# Battlefield Dev Scenes

Development-only scenes for testing, debugging, and iteration.

## What Goes Here

**Dev scenes are:**
- Test environments for specific features
- Debugging sandboxes with special conditions
- VFX/mechanic prototyping scenes
- Performance testing setups
- Scenes that should NOT be accessible in production builds

**Dev scenes are NOT:**
- Production battle scenes (those go in parent directory)
- Reusable components (those go in `components/`)
- Actual game content players will see

## Current Dev Scenes

### test_battle_vfx.tscn
VFX and card testing sandbox. Extends the main battle scene with special test conditions:

**Features:**
- **Infinite Mana**: `starting_mana = 999999`
- **Infinite HP**: `enemy_hp = 999999.0` (unkillable enemy)
- **Hardcoded Test Deck**: Predefined cards for testing specific interactions
- **Custom Labels**: Shows FPS, unit count, active card count
- **Fast Iteration**: Test VFX without playing through campaign

**Usage:**
1. Open `dev/test_battle_vfx.tscn` in Godot
2. Run scene (F6) to launch directly
3. Test card abilities, VFX, unit behavior
4. Tweak values and re-run instantly

**Configuration:**
Configured via `TestGameController` (extends `GameController3D`). See `scripts/core/test_game_controller.gd`.

## Creating New Dev Scenes

When creating a new dev/test scene:

1. **Name it clearly**: `test_<feature>.tscn` (e.g., `test_ai_behavior.tscn`)
2. **Document its purpose**: Add comment in scene script explaining what it tests
3. **Use test controllers**: Extend game controllers with test-specific overrides
4. **Configure via BattleContext**: Use `configure_practice_battle()` with test settings
5. **Add to this README**: Document what it does and how to use it

## Example: Creating a New Test Scene

```gdscript
# scripts/core/test_ai_controller.gd
extends GameController3D

func _ready() -> void:
    # Configure test battle
    var battle_context = get_node_or_null("/root/BattleContext")
    if battle_context:
        battle_context.configure_practice_battle({
            "enemy_deck": [{"catalog_id": "warrior", "count": 50}],
            "enemy_hp": 1000.0
        })

    super._ready()

    # Override AI for testing
    _enable_ai_debug_mode()
```

## Test Scene Categories

As the project grows, consider organizing by category:

```
dev/
├── test_vfx/              # Visual effects testing
│   └── test_battle_vfx.tscn
├── test_ai/               # AI behavior testing
│   ├── test_ai_aggression.tscn
│   └── test_ai_pathfinding.tscn
├── test_performance/      # Performance profiling
│   └── test_1000_units.tscn
└── test_mechanics/        # Game mechanic testing
    ├── test_card_abilities.tscn
    └── test_unit_spawning.tscn
```

## Best Practices

1. **Keep them fast**: Minimize setup time for quick iteration
2. **Isolate features**: Test one thing at a time
3. **Add debug UI**: Show relevant stats (FPS, counts, state)
4. **Use infinite resources**: Remove barriers to testing (infinite mana, HP)
5. **Document shortcuts**: If scene has special controls, document them
6. **Don't commit secrets**: Test scenes should use dummy data, not real player profiles

## Running Dev Scenes

**Method 1: Direct Scene Run**
- Open scene in Godot editor
- Press F6 to run the scene directly
- Fastest for rapid iteration

**Method 2: Scene Launcher (Future)**
- Could create a dev menu scene that lists all test scenes
- Useful when there are many test scenes
- Allows passing parameters to test scenes

## Excluding from Production Builds

When creating export presets, ensure dev scenes are excluded:

```
# export_presets.cfg
[preset.0]
exclude_filter="*.dev.*, dev/*, test_*"
```

This prevents dev scenes from being included in player builds.
