# Skeletal Visual Component Configuration Guide

> **Note:** For an overview of when to use `SkeletalVisualComponent` vs `SpriteVisualComponent`, see [Visual Components Guide](visual-components.md).

This guide explains how to configure `SkeletalVisualComponent` for 2.5D skeletal characters.

## Parameter Overview

| Parameter | Type | Purpose |
|-----------|------|---------|
| `SkeletalScene` | PackedScene | The 2D rig scene to render |
| `ScaleFactor` | Vector2 | Controls unit size in world space |
| `ViewportSize` | Vector2I | SubViewport dimensions (default: 1200x1200) |
| `ContentSize` | Vector2 | Body dimensions for shadow sizing |
| `FeetLocalPosition` | Vector2 | Feet position in rig local space (also determines horizontal centering) |

## How Parameters Work Together

```
┌─────────────────────────────────────┐
│         SubViewport (1200x1200)     │
│              ↓                      │
│         Viewport Center             │
│              │                      │
│    ┌─────────│─────────────┐        │
│    │   Rig   │   Content   │        │
│    │   (scaled by ScaleFactor)      │
│    │         │             │        │
│    │      ○  │  Body       │        │
│    │     /│\ │             │        │
│    │      │  │             │        │
│    │     / \ │  ← Feet     │        │
│    └─────────│─────────────┘        │
│              ↑                      │
│    FeetLocalPosition.X              │
│    (auto-centered here)             │
└─────────────────────────────────────┘

The system automatically positions the rig so that FeetLocalPosition.X
aligns with the viewport center. This ensures:
- Content is horizontally centered
- Flipping works correctly in both directions
- Shadow appears under the character
```

## Calculating FeetLocalPosition

`FeetLocalPosition` specifies where the feet are in the **rig's local coordinate space** (before scaling). This is used to position the sprite so feet touch the ground at world Y=0.

### For Pivot-Based Skeletal Rigs

1. **Find the leg pivots** in your rig scene:
   ```
   LeftLegPivot position = (468, 770)
   RightLegPivot position = (150, 770)
   ```

2. **Check leg sprite offsets** - sprites may extend below pivots:
   ```
   LeftLegSprite position = (43, 90)  // 90 pixels below pivot
   ```

3. **Calculate actual feet Y position**:
   ```
   Feet Y = LegPivot.Y + LegSprite.Y offset + half sprite height
   Example: 770 + 90 + ~80 = 940
   ```

4. **Add buffer for walk animations** - check walk animation keyframes for maximum leg Y:
   ```
   Walk animation moves leg to Y = 785 (15 pixels lower than idle)
   Add buffer: 940 + 50 = 990
   Final FeetLocalPosition.Y ≈ 1000
   ```

5. **For X position**, use the horizontal center of the body:
   ```
   Body pivot at X = 300
   FeetLocalPosition.X = 300
   ```

### For Frame-Based Sprites (like Water Frog)

1. **Check sprite offset** in the rig - if using offset to center content:
   ```
   AnimatedSprite2D offset = (450, 0)  // Centers frog body at rig origin
   ```

2. **Estimate feet position** relative to the centered body:
   - Body centered at (0, 0)
   - Feet are below body center
   - If texture is 512 tall and body center is at texture Y=256, feet at Y=450
   - Feet in rig space: 450 - 256 = 194
   - With buffer: `FeetLocalPosition = (0, 250)`

### Quick Reference Formula

```
FeetLocalPosition.Y = max(
    LegPivotY + LegSpriteOffsetY + HalfLegSpriteHeight,
    WalkAnimationMaxLegY + LegSpriteOffsetY + HalfLegSpriteHeight
) + Buffer(30-50)
```

## Calculating ContentSize

`ContentSize` determines shadow size. It should represent the **visual body bounds** (not the full rig bounds which may include extended arms/weapons).

### Shadow Size Formula
```
ShadowDiameter = ContentSize.X * ScaleFactor.X * 0.01 * 0.8
```

### Guidelines

1. **Measure the body width** in the rig (not extended limbs)
2. **Measure the body height** (head to feet, not including raised arms)
3. Use these as `ContentSize`

### Example Calculations

**Fire Wisp:**
- Body width in rig: ~500 pixels
- Body height in rig: ~800 pixels
- `ContentSize = (500, 800)`
- With ScaleFactor 0.15: Shadow = 500 * 0.15 * 0.01 * 0.8 = 0.6 world units

**Fire Ant:**
- Body width: ~400 pixels (just the body, not legs spread)
- Body height: ~300 pixels
- `ContentSize = (400, 300)`
- With ScaleFactor 0.2: Shadow = 400 * 0.2 * 0.01 * 0.8 = 0.64 world units

## Common Issues

### Feet Sinking Into Ground
**Cause:** `FeetLocalPosition.Y` is too low
**Fix:** Increase FeetLocalPosition.Y - account for sprite offsets below pivots

### Feet Floating Above Ground
**Cause:** `FeetLocalPosition.Y` is too high
**Fix:** Decrease FeetLocalPosition.Y

### Content Clipped at Viewport Edge
**Cause:** Rig content extends outside viewport bounds
**Fix:** Increase `ViewportSize` to fit all content (content is auto-centered based on FeetLocalPosition.X)

### Shadow Too Big/Small
**Cause:** `ContentSize` doesn't match actual body dimensions
**Fix:** Measure body bounds in rig and update ContentSize

### Shadow in Wrong Position
**Cause:** Shadow uses `HurtboxOffset` for positioning, which may not match visual
**Fix:** Adjust `HurtboxOffset` in the unit scene to match visual center

## Complete Example: Fire Wisp

```
# In fire_wisp_3d.tscn

[node name="Visual" parent="." instance=ExtResource("2_skeletal_component")]
SkeletalScene = ExtResource("3_rig")          # fire_wisp_rig.tscn
ScaleFactor = Vector2(0.15, 0.15)             # Small scale for this unit
ContentSize = Vector2(500, 800)               # Body is ~500x800 pixels
FeetLocalPosition = Vector2(300, 1150)        # Body center X, feet Y with buffer
```

**Calculation breakdown:**
- Rig has BodyPivot at (300, 300), legs at ~(300, 1000)
- Leg sprites extend ~100 pixels below pivots
- Walk animation moves legs ~50 pixels lower
- FeetLocalPosition.Y = 1000 + 100 + 50 buffer = 1150
- ContentSize.X = body width ~500 pixels (not including extended legs)
