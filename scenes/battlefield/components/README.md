# Battlefield Components

Reusable 3D building blocks that are instanced by main battle scenes.

## What Goes Here

**Components are:**
- Modular, reusable scene fragments
- Instanced (via PackedScene) rather than run directly
- Shared infrastructure used by multiple battle scenes
- Usually configured at runtime via script

**Components are NOT:**
- Complete, playable battle scenes (those go in parent directory)
- Test/debug scenes (those go in `dev/`)
- One-off scene elements that aren't reused

## Current Components

### base_battlefield_3d.tscn
The foundation 3D environment for all battles. Contains:
- **Camera3D**: Orthographic camera with 35° tilt
- **DirectionalLight3D**: Main scene lighting
- **WorldEnvironment**: Background color, ambient light, fog
- **Background**: Ground plane (MeshInstance3D with PlaneMesh)
- **Layers**: GroundLayer, GameplayLayer, EffectsLayer (for z-ordering)
- **Spawn Markers**: Player and enemy spawn positions

This scene is instanced by `battle_3d.tscn` and `dev/test_battle_vfx.tscn`.

**Configuration:**
Visual theme is applied at runtime via `BiomeConfig` resources. The script reads `BattleContext.biome_id` and loads the corresponding biome from `resources/biomes/`.

See `scripts/battlefield/base_battlefield_3d.gd` for implementation.

## Future Components

As the game grows, additional components might include:
- Weather effects (rain, snow, fog particles)
- Biome-specific decorations (trees, rocks, cacti)
- Animated backgrounds
- Dynamic lighting systems
- Environmental hazards

## Usage Pattern

Components are typically instanced in main battle scenes like this:

```gdscript
# In battle_3d.tscn scene file
[ext_resource type="PackedScene" path="res://scenes/battlefield/components/base_battlefield_3d.tscn" id="1_battlefield"]

[node name="Battle3D" type="Node3D"]
# ... other nodes ...

[node name="BaseBattlefield3D" parent="." instance=ExtResource("1_battlefield")]
```

The main battle scene can then access component nodes:
```gdscript
var battlefield = $BaseBattlefield3D
var spawn_pos = battlefield.get_player_spawn_position()
```

## Design Principles

1. **Single Responsibility**: Each component handles one aspect (environment, decorations, effects)
2. **Composable**: Components can be mixed and matched
3. **Configurable**: Exposed @export vars for customization
4. **Reusable**: Shared across multiple battle scenes
5. **Scriptable**: Behavior defined in code, not scene tree

## When to Create a New Component

Create a new component when:
- You need the same scene structure in multiple battles
- The element is logically distinct (weather, decorations, etc.)
- It can be configured differently per battle
- It encapsulates complexity that shouldn't pollute the main scene

Don't create a component for:
- One-off elements used in a single scene
- Simple nodes (just add them directly)
- Tightly coupled battle logic (that belongs in controllers)
