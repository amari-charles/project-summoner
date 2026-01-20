# Visual Components Guide

This guide explains the visual component architecture for 2.5D character rendering.

## Overview

All unit visuals implement the `IVisualComponent` interface, which provides a common API for animation, rendering, and spawn preview. There are two implementations for different animation types:

| Component | Use Case | Scene File |
|-----------|----------|------------|
| `SpriteVisualComponent` | Frame-based sprite animations | `sprite_character_2d5_component.tscn` |
| `SkeletalVisualComponent` | Pivot-based skeletal rigs | `skeletal_character_2d5_component.tscn` |

## Which Component Should I Use?

### Use SpriteVisualComponent When:
- Your character uses **frame-by-frame animation** (like traditional 2D games)
- You have a **SpriteFrames resource** with idle/walk/attack animations
- Each animation frame is a complete image of the character
- Example: Water Frog (tongue attack is pre-rendered frames)

### Use SkeletalVisualComponent When:
- Your character uses **skeletal/pivot-based animation**
- You have a **rig scene** with Node2D pivots and AnimationPlayer
- Body parts are separate sprites that transform during animation
- Example: Fire Elemental, Earth Sprite, Fire Ant

## SpriteVisualComponent

### Key Parameters

| Parameter | Purpose |
|-----------|---------|
| `SpriteFramesResource` | The SpriteFrames resource containing animations |
| `SpriteOffsetPixels` | Pixel offset for centering off-center sprites |
| `FeetOffsetPixels` | Pixels from sprite bottom to feet (for grounding) |
| `HeadOffsetPixels` | Pixels from sprite top to head (for height calculation) |
| `ViewportSize` | Viewport dimensions in pixels (default: 512x512) |
| `ScaleFactor` | Scale factor for the sprite (default: 5.12, 5.12) |

### Centering with SpriteOffsetPixels

If your character isn't centered in its texture (common when attack animations need extra space), use `SpriteOffsetPixels` to compensate:

```
Texture Layout:           After SpriteOffsetPixels = (+90, 0):
┌─────────────────┐       ┌─────────────────┐
│ ○               │       │        ○        │
│/│\      →→→→→→→→│  ==>  │       /│\       │
│/ \    (tongue)  │       │       / \       │
└─────────────────┘       └─────────────────┘
 Body on left              Body now centered
```

- **Positive X**: Shifts rendering RIGHT (use when body is on LEFT side of texture)
- **Negative X**: Shifts rendering LEFT (use when body is on RIGHT side of texture)
- The offset automatically flips when the sprite flips direction

### Attack Effects

SpriteVisualComponent supports visual attack effects for single-frame attack animations:
- `Lunge` - Character lunges forward then back
- `SquashSpring` - Squash and spring effect
- `Spin` - Rotation effect
- `Pulse` - Scale pulse effect

Set via `AttackStyleSetting` property.

## SkeletalVisualComponent

### Key Parameters

| Parameter | Purpose |
|-----------|---------|
| `SkeletalScene` | The PackedScene containing the skeletal rig |
| `ScaleFactor` | Scale applied to the rig (e.g., 0.15 for small units) |
| `ViewportSize` | Viewport dimensions in pixels (default: 1200x1200) |
| `ContentSize` | Approximate body dimensions for shadow sizing |
| `FeetLocalPosition` | Position of feet in rig local space |

### Centering with FeetLocalPosition

`FeetLocalPosition.X` determines horizontal centering. The system positions the rig so this X coordinate aligns with the viewport center.

```
Rig Layout:                After FeetLocalPosition.X = 300:
┌─────────────────┐        ┌─────────────────┐
│         ○       │        │        ○        │
│        /│\      │   ==>  │       /│\       │
│        / \      │        │       / \       │
│     (300,800)   │        │    (centered)   │
└─────────────────┘        └─────────────────┘
 Body at X=300              Body now centered
```

### Creating a Skeletal Rig

1. Create a Node2D scene with your character structure
2. Use child Node2D nodes as "pivots" for each body part
3. Add Sprite2D children to each pivot
4. Add an AnimationPlayer with idle/walk/attack animations
5. Optionally add an `attack_impact` signal for attack timing

## Common Mistakes

### Using SkeletalVisualComponent for Frame-Based Sprites

**Symptom:** Shadow position is wrong, centering math doesn't work

**Cause:** SkeletalVisualComponent expects scaled pivot positions, but frame-based sprites use unscaled offsets.

**Fix:** Switch to SpriteVisualComponent and use `SpriteOffsetPixels` for centering.

### Wrong Component Parameters

**Sprite Component:**
- `SpriteOffsetPixels` is in **pixels** (unscaled)
- Values typically range from -200 to 200

**Skeletal Component:**
- `FeetLocalPosition.X` is in **rig local space** (gets scaled)
- Values match pivot positions in the rig (e.g., 300, 1050)

### Sprite Being Clipped

If your sprite is being clipped, increase `ViewportSize`:
```
ViewportSize = Vector2i(768, 768)  // Larger viewport to fit sprite
```

## Interface Methods

Both components implement `IVisualComponent`:

```csharp
// Animation
void PlayAnimation(string animName);
void StopAnimation();
string GetCurrentAnimation();
bool IsPlaying();
void SetAnimationSpeed(float speed);
float GetAnimationDuration(string animName);

// Rendering
float GetSpriteHeight();
float GetSpriteWidth();
float GetHpBarOffsetX();
Vector3 GetShadowOffset();
void FlashWhite();
void SetFlipH(bool flip);
void SetRenderPriority(int priority);
bool IsFullyInitialized();

// Spawn Preview
Node3D CreateGhostVisual();
void ApplyGhostTint(Color tint);
```

## File Locations

| File | Purpose |
|------|---------|
| `scripts/csharp/Visual/IVisualComponent.cs` | Interface definition |
| `scripts/csharp/Visual/SpriteVisualComponent.cs` | Frame-based implementation |
| `scripts/csharp/Visual/SkeletalVisualComponent.cs` | Skeletal rig implementation |
| `scenes/units/sprite_character_2d5_component.tscn` | Sprite component scene |
| `scenes/units/skeletal_character_2d5_component.tscn` | Skeletal component scene |
