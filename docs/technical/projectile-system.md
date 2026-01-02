# Projectile System

Technical documentation for the projectile system including movement, acceleration, and visual effects.

## Overview

Projectiles are managed by `ProjectileManager` (autoload) and use pooling for performance. Each projectile type is defined in JSON files under `data/projectiles/`.

## Projectile Data Properties

### Movement

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `movement_type` | string | "straight" | Movement pattern: "straight", "homing", "arc", "ballistic" |
| `speed` | float | 15.0 | Initial velocity in units/second |
| `acceleration` | float | 0.0 | Speed change per second (negative = decelerate) |
| `min_speed` | float | 1.0 | Floor for deceleration - prevents projectiles from stopping |
| `lifetime` | float | 5.0 | Max time before despawn |
| `rotate_to_direction` | bool | true | Whether projectile rotates to face movement direction |

### Visual Effects

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `model_scene_path` | string | "" | Path to visual scene (.tscn) |
| `fade_in_duration` | float | 0.0 | Time in seconds to fade from invisible to visible |
| `trail_effect_id` | string | "" | VFX ID for trail particles |
| `impact_effect_id` | string | "" | VFX ID for hit effect |

### Combat

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `pierce_count` | int | 0 | Number of targets to pierce through (0 = hit first target) |
| `aoe_radius` | float | 0.0 | Area of effect radius (0 = single target) |

## Acceleration/Deceleration

The acceleration system allows projectiles to speed up or slow down over time.

### Tuning Guide

- **speed**: Initial velocity (typical range: 10-30 units/s)
- **acceleration**: Speed change per second
  - Positive = accelerate (speed up over time)
  - Negative = decelerate (slow down over time)
- **min_speed**: Prevents projectiles from stopping completely when decelerating

### Example: Wind Puff (starts fast, slows down)

```json
{
  "speed": 25.0,
  "acceleration": -12.0,
  "min_speed": 5.0
}
```

This starts at 25 units/s and decelerates to 5 units/s over ~1.67 seconds:
- Time to reach min_speed = (25 - 5) / 12 = 1.67 seconds

## Fade-In Effect

Projectiles can fade in over time using the `fade_in_duration` property. This is useful for:
- Charge-up attacks where the projectile "materializes"
- Projectiles that spawn close to the unit and need to appear smoothly

The fade-in works with both `StandardMaterial3D` (tweens `albedo_color.a`) and `ShaderMaterial` (tweens `alpha` uniform).

### Shader Requirements

For custom shaders to support fade-in, include an `alpha` uniform:

```glsl
uniform float alpha : hint_range(0.0, 1.0) = 1.0;

void fragment() {
    ALPHA = base_alpha * alpha;
}
```

## Delayed Projectile Spawning

Some units (like Puff) have charge-up attacks where the projectile should spawn partway through the animation rather than immediately.

### Configuration (Unit3D)

```gdscript
@export var delayed_projectile: bool = false
@export var projectile_delay: float = 0.5  # seconds
```

### Calculating Delay

The delay should match when the projectile visually "fires" in the attack animation:

```
projectile_delay = target_frame / animation_fps
```

Example: Puff's attack animation fires at frame 14, running at 12fps:
- `projectile_delay = 14 / 12 = 1.17` seconds

## Ground Collision

Projectiles check for ground collision and explode when hitting the ground. A grace period (`GROUND_COLLISION_GRACE_PERIOD = 0.1s`) prevents false positives for projectiles that spawn near ground level.

## Pooling

Projectiles are pooled by `ProjectileManager` for performance. Key reset behaviors:
- `current_speed` resets to initial `speed`
- Material alpha resets to 1.0
- Particle emitters restart
- Transform resets to origin
