# Coordinate System Documentation

## 3D Axes Reference

**CRITICAL: Axis definitions for the battlefield**

- **X-axis**: Left/Right (horizontal)
  - Negative X (-): Player side (left)
  - Positive X (+): Enemy side (right)

- **Y-axis**: Up/Down (vertical height)
  - Y=0: Ground level
  - Positive Y (+): Higher in the air
  - Negative Y (-): Below ground

- **Z-axis**: Forward/Back (depth across ground plane)
  - Negative Z (-): Toward camera (closer to viewer)
  - Positive Z (+): Away from camera (further from viewer)

## Ground Plane

The battlefield ground is the **XZ plane at Y=0**.

- Ground dimensions: **200 × 150** world units (X × Z)
- Ground center: **(0, 0, 0)**
- Moving in X or Z = sliding across the ground
- Moving in Y = going up/down in the air

## Camera Setup

**Position:** `(0, 30, -42.85)`
- X=0: Centered on battlefield width
- Y=30: 30 units above ground
- Z=-42.85: Offset backward from origin

**Rotation:** 35° tilt downward
- Transform basis: `(1, 0, 0, 0, 0.819152, 0.573576, 0, 0.573576, -0.819152)`
- This creates the isometric-style angled view

**Projection:** Orthographic
- Size: 40.0
- View height: ~80 units
- View width: ~71 units (varies with aspect ratio)

## Visual Screen-Space Behavior

Due to the 35° camera tilt:

### Z-axis affects vertical screen position
- **More negative Z** (e.g., -10) → Appears **lower** on screen (toward bottom)
- **More positive Z** (e.g., +10) → Appears **higher** on screen (toward top)
- **Z=0** → Appears in **middle-to-upper** portion of screen

### Y-axis affects both screen height AND depth
- Higher Y values make objects appear:
  - Higher on screen (obvious)
  - Slightly further back due to perspective (subtle)

## Current Spawn Positions

### Player Base (Left Side)
- Position: **(-80, 0, -7.5)**
  - X=-80: Far left
  - Y=0: On ground
  - Z=-7.5: Shifted toward camera for visual centering

### Enemy Base (Right Side)
- Position: **(80, 0, -7.5)**
  - X=80: Far right
  - Y=0: On ground
  - Z=-7.5: Shifted toward camera for visual centering

### Why Z=-7.5?

The castle sprites are 6 units tall (400px × 0.015 pixel_size). From the tilted camera view:
- The sprite's vertical height appears as "depth" in screen space
- Without Z offset, castles appear too high in the viewport
- **Z=-7.5 shifts them forward** to center them vertically in the view
- This is approximately half the sprite height adjusted for camera angle

**Note:** This value may need fine-tuning based on aesthetic preferences.

## Unit Spawning

Units spawn at the marker positions, which currently match the base positions:
- Player units: (-80, 0, -7.5)
- Enemy units: (80, 0, -7.5)

Units then move along the **X-axis** toward their targets (player units move right +X, enemy units move left -X).

## Camera Bounds

The camera controller (`camera_controller_3d.gd`) constrains panning to keep the ground visible:
- X bounds: Calculated based on view width and ground width
- Z bounds: Calculated by projecting view edges to ground plane (complex due to tilt)

The camera can pan within these bounds but cannot see beyond the 200×150 ground plane edges.

## Visual Diagram

```
         Camera at (0, 30, -42.85)
              ↓ (Looking down at 35°)

    Z-axis (depth)
    ↑ +Z (further away, appears higher on screen)
    |
    |   [Enemy Base]     (+80, 0, -7.5)
    |
    |----+----[Origin]----+---- X-axis (left/right)
    |  [Player Base]
    |    (-80, 0, -7.5)
    |
    ↓ -Z (toward camera, appears lower on screen)

    Y-axis (height) goes into/out of page:
    - Y=0 is ground level (bases sit here)
    - Y+ goes "up into the air"
```

## Related Files

- `scenes/battlefield/base_battlefield_3d.tscn` - Battlefield scene with spawn markers
- `scripts/battlefield/base_battlefield_3d.gd` - Spawn position configuration
- `scripts/battlefield/camera_controller_3d.gd` - Camera bounds and panning
- `scenes/battlefield/campaign_battle_3d.tscn` - Campaign battle setup
- `scenes/battlefield/test_battle_vfx.tscn` - VFX test battle setup
