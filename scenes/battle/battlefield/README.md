# Battlefield Scenes

This directory contains all battlefield-related scenes for Fateforged.

## Structure

```
battlefield/
├── battle_3d.tscn              # Shared production battle scene
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
The main battle scene used by encounter, authored, practice, and multiplayer
battles. It is configured through the `BattleContext` singleton before loading.
It contains:
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
# Configure a quest/world encounter after EncounterApi resolves its authored data
var encounter_id := "intro_spell_practice"
var config := EncounterApi.resolve_battle_config(encounter_id)
BattleContext.configure_encounter_battle(encounter_id, config)
SceneManager.transition_to(SceneManager.SCENE_BATTLE_3D)

# Configure the default practice battle
BattleContext.configure_practice_battle()
SceneManager.transition_to(SceneManager.SCENE_BATTLE_3D)
```

Direct authored debug battles use `configure_authored_battle()`. Multiplayer
uses `configure_multiplayer_battle()`. This keeps one production battle scene
behind explicit, mode-specific configuration entry points.

## Biome System

Visual themes (ground texture, lighting, fog) are defined in `resources/biomes/` as `BiomeConfig` resources. The battlefield loads the biome specified in `BattleContext.biome_id` at runtime.

See `scripts/battle/battlefield/biome_config.gd` for details.
