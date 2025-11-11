# Battlefield Scenes

This directory contains all battlefield-related scenes for Project Summoner.

## Structure

```
battlefield/
├── battle_3d.tscn              # Main battle scene (campaign/arena/practice)
├── components/                 # Reusable battlefield building blocks
│   └── base_battlefield_3d.tscn
└── dev/                        # Development and testing scenes
    └── test_battle_vfx.tscn
```

## Main Battle Scenes (Root Level)

**What goes here:**
- Complete, playable battle scenes that users can enter
- Currently just `battle_3d.tscn` (used by all game modes)
- Future biome-specific variants would go here (e.g., `battle_3d_desert.tscn`)

**What does NOT go here:**
- Components/building blocks (those go in `components/`)
- Test scenes (those go in `dev/`)

### battle_3d.tscn
The main battle scene used by Campaign, Arena, and Practice modes. Configured via the `BattleContext` singleton before loading. Contains:
- GameController3D (battle logic)
- Player and enemy summoners
- UI layer (hand, labels, drop zone)
- Instances `components/base_battlefield_3d.tscn` for environment

## Subdirectories

### [components/](./components/README.md)
Reusable 3D building blocks that are instanced by main battle scenes. See `components/README.md` for details.

### [dev/](./dev/README.md)
Development-only scenes for testing and debugging. See `dev/README.md` for details.

## BattleContext System

All battle scenes are configured through the `BattleContext` singleton (autoload). Before loading a battle:

```gdscript
# Configure for campaign mode
var battle_context = get_node("/root/BattleContext")
battle_context.configure_campaign_battle("battle_00")
get_tree().change_scene_to_file("res://scenes/battlefield/battle_3d.tscn")

# Configure for practice mode
battle_context.configure_practice_battle({
    "enemy_deck": [...],
    "enemy_hp": 150.0
})
get_tree().change_scene_to_file("res://scenes/battlefield/battle_3d.tscn")
```

This allows a single battle scene to work across all game modes.

## Biome System

Visual themes (ground texture, lighting, fog) are defined in `resources/biomes/` as `BiomeConfig` resources. The battlefield loads the biome specified in `BattleContext.biome_id` at runtime.

See `scripts/battlefield/biome_config.gd` for details.
