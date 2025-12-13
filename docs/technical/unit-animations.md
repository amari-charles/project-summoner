# Unit Animation System

This document covers the animation systems available for unit sprites, including bobbing animations for floating units and attack effects for single-frame sprites.

## Bobbing Animation

Floating units (spirits, elementals, ghosts) can use a bobbing animation to simulate hovering. This creates a gentle up-and-down bounce with side-to-side tilting.

### Enabling Bobbing

In your unit scene (`.tscn`), set these properties on the `Unit3D` node:

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

## Example: Fire Elemental

The Fire Elemental (`fire_elemental_3d.tscn`) demonstrates both systems:

```gdscript
# In scene properties:
enable_bobbing = true
attack_style = 1  # Lunge
```

This creates a floating fire spirit that bobs gently while idle and lunges forward when attacking.

## Adding to New Units

1. Create your unit scene extending `Unit3D`
2. Set `enable_bobbing = true` if the unit should float
3. Choose an `attack_style` (1-4) if using single-frame sprites
4. The visual component handles the rest automatically
