# Unit Animation System

This document covers the animation systems available for unit sprites, including bobbing animations for floating units and attack effects for single-frame sprites.

## Bobbing Animation

Floating units (spirits, elementals, ghosts) can use a bobbing animation to simulate hovering. This creates a gentle up-and-down bounce with side-to-side tilting.

### Enabling Bobbing

In your unit scene (`.tscn`), set these properties on the `UnitVisual` node:

```
enable_bobbing = true
```

Or configure via `unit_3d.gd` exports:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `enable_bobbing` | bool | false | Enable the bobbing animation |

The bobbing parameters can be tuned in `sprite_character_2d5_component.gd`:

| Property | Default | Description |
|----------|---------|-------------|
| `bob_speed` | 8.0 | Speed of walk cycle (higher = faster steps) |
| `bob_amplitude` | 6.0 | Vertical bounce in pixels |
| `bob_rotation_amplitude` | 4.0 | Side-to-side tilt in degrees |

### How It Works

- Uses `abs(sin(time))` for bouncy vertical motion (simulates feet hitting ground)
- Uses `sin(time)` for side-to-side tilt (waddle effect)
- Each unit starts with randomized phase to prevent synchronized bobbing
- Bobbing pauses automatically during attack animations

## Attack Animation Styles

For single-frame sprites that don't have dedicated attack animations, four procedural attack effects are available:

### Attack Styles

| Style | Description |
|-------|-------------|
| `NONE` (0) | No attack effect |
| `LUNGE` (1) | Quick thrust forward and snap back |
| `SQUASH_SPRING` (2) | Cartoon-style compression then spring forward |
| `SPIN` (3) | Full rotation with scale pulse |
| `PULSE` (4) | Rapid expand then shrink with brightness flash |

### Configuration

In your unit scene, set:

```
attack_style = 1  # 0=None, 1=Lunge, 2=Squash&Spring, 3=Spin, 4=Pulse
```

For testing all styles:
```
cycle_attack_styles = true  # Cycles through styles
attacks_per_style = 2       # Attacks before switching
```

### Style Details

**Lunge (1)**
- Thrusts 20px toward enemy
- 0.1s forward, 0.15s return
- Good for melee units

**Squash & Spring (2)**
- Compresses (1.3x wide, 0.7x tall)
- Springs forward 15px while stretching (0.85x wide, 1.2x tall)
- Cartoon/bouncy feel

**Spin (3)**
- 15° wind-up, then full 360° rotation
- Slight scale pulse (1.1x) during spin
- Good for magical/elemental units

**Pulse (4)**
- Expands to 1.4x scale
- Brightness flash during expansion
- Good for ranged/energy attacks

## Skeletal Rigging System

For units that need more expressive animations than sprite frames can provide, a skeletal rigging system is available using Node2D pivots and AnimationPlayer.

### How It Works

- Uses `SkeletalCharacter2D5Component` instead of `SpriteCharacter2D5Component`
- Rig scenes contain Node2D pivot nodes for body parts (body, legs, eyes, etc.)
- AnimationPlayer animates pivot positions, rotations, and scales
- Viewport dynamically sizes to fit the character bounds
- Animation phase is randomized so swarms don't animate in sync

### Creating a Skeletal Rig

1. Create a rig scene (`*_rig.tscn`) with:
   - Root Node2D with a script extending Node2D
   - AnimationPlayer child with animations (idle, attack, etc.)
   - Pivot nodes (Node2D) for each body part
   - Sprite2D children under each pivot

2. Add a script to bridge animation events:
```gdscript
extends Node2D

signal attack_impact

func _on_attack_impact() -> void:
    attack_impact.emit()
```

3. Use method tracks in AnimationPlayer to fire events at specific times

### Using a Skeletal Rig in a Unit

In your unit scene, add a `SkeletalCharacter2D5Component`:

```
[node name="Visual" parent="." instance=ExtResource("skeletal_component")]
skeletal_scene = ExtResource("your_rig.tscn")
scale_factor = Vector2(0.15, 0.15)
```

### Animation Speed Control

Both sprite and skeletal components support animation speed scaling:

```gdscript
# Speed up animation when moving faster
visual_component.set_animation_speed(2.0)

# Get current speed
var speed = visual_component.get_animation_speed()
```

## Example: Fire Wisp (Skeletal)

The Fire Wisp (`fire_wisp_3d.tscn`) uses skeletal rigging for expressive bouncy hop animation:

- **Rig**: `fire_wisp_rig.tscn` with body, eye, and leg pivots
- **Idle**: 0.8s bouncy hop with squash/stretch and alternating legs
- **Attack**: Forward lunge with eye scale pulse, impact at 0.4s

```
# In fire_wisp_3d.tscn:
[node name="Visual" parent="." instance=ExtResource("skeletal_component")]
skeletal_scene = ExtResource("fire_wisp_rig.tscn")
scale_factor = Vector2(0.15, 0.15)
```

The Fire Titan uses the same rig at larger scale (`scale_factor = Vector2(0.6, 0.6)`).

## Example: Soldier (Sprite-based)

For simpler units, sprite-frame animation with procedural effects works well:

```gdscript
# In scene properties:
enable_bobbing = true
attack_style = 1  # Lunge
```

This creates a unit that bobs gently while idle and lunges forward when attacking.

## Adding to New Units

### Sprite-based Units (simpler)
1. Create your unit scene extending `UnitVisual`
2. Set `enable_bobbing = true` if the unit should float
3. Choose an `attack_style` (1-4) for procedural attack effects
4. The visual component handles the rest automatically

### Skeletal Units (more expressive)
1. Create a rig scene with pivots and AnimationPlayer
2. Add a script to bridge animation events (attack_impact signal)
3. Create your unit scene extending `UnitVisual`
4. Add `SkeletalCharacter2D5Component` as "Visual" child
5. Set `skeletal_scene` and `scale_factor` exports
