# Projectile System

Technical documentation for the projectile system including movement, acceleration, and visual effects.

## Overview

Projectiles are managed by `ProjectileService` (C# autoload) and use pooling for performance. Each projectile type is defined in JSON files under `data/projectiles/` and loaded by `ProjectileCatalog` (C# autoload).

## Projectile Data Properties

### Movement

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `movement_type` | string | "straight" | Movement pattern: "straight", "homing", "arc", "ballistic" |
| `tracking` | bool | false | Whether to continuously update target position (for moving targets) |
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
| `fade_on_hit` | bool | true | Whether to fade out on hit (true) or despawn immediately (false) |
| `fade_duration` | float | 0.5 | Duration of fade-out animation in seconds |
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

## Target Tracking

The `tracking` property enables continuous target updates for any movement type. This is essential for hitting moving targets.

### When to Use Tracking

| Scenario | Use Tracking? |
|----------|---------------|
| Fast projectile, stationary targets | No |
| Slow projectile, moving targets | Yes |
| Decelerating projectile | Yes (flight time is longer than predicted) |
| Fire-and-forget AOE | No |

### How It Works

When `tracking: true`, the projectile updates its path endpoint every 0.1 seconds to follow the target:

1. Get target's current position
2. Apply predictive targeting (intercept calculation)
3. Update path endpoint via `IProjectilePath.UpdateTarget()`

Prediction disables when close to target (`< 2.0 units`) to prevent oscillation.

### Tracking vs Homing

| Property | Tracking (straight) | Homing |
|----------|---------------------|--------|
| Path shape | Linear | Arc (Bézier) |
| Updates target | Yes | Yes |
| Visual feel | Direct, straight line | Curved, seeking |
| Use case | Bullets, wind puffs | Magic missiles, seeking orbs |

### Example: Tracking Straight Projectile

```json
{
  "movement_type": "straight",
  "tracking": true,
  "speed": 25.0,
  "acceleration": -12.0,
  "min_speed": 5.0
}
```

This creates a straight projectile that continuously adjusts its trajectory to hit moving targets.

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

## Fade-Out Effect

When a projectile hits a target or expires, it can fade out smoothly using the `fade_on_hit` and `fade_duration` properties.

- `fade_on_hit = true` (default): Projectile fades out over `fade_duration` seconds before despawning
- `fade_on_hit = false`: Projectile despawns immediately on hit

The fade-out works with both `StandardMaterial3D` (tweens `albedo_color.a`) and `ShaderMaterial` (tweens `alpha` uniform). Custom shaders must include the same `alpha` uniform as described in [Shader Requirements](#shader-requirements) above.

### Example: Quick Fade

```json
{
  "fade_on_hit": true,
  "fade_duration": 0.1
}
```

This creates a quick 0.1 second fade-out, useful for fast projectiles like wind puffs that shouldn't linger visually.

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

## Path-Based Movement Architecture

Projectiles use a **path-based movement system** with the Strategy pattern. Instead of updating position via direction vectors, projectiles follow parameterized curves from start (progress=0) to end (progress=1).

### IProjectilePath Interface

All paths implement `IProjectilePath`:

```csharp
public interface IProjectilePath
{
    Vector3 GetPosition(float progress);  // 0 = start, 1 = end
    void UpdateTarget(Vector3 newTarget); // Track moving targets
    float GetLength();                     // For speed calculation
    Vector3 GetDirection(float progress);  // For rotation
}
```

### Path Implementations

| Movement Type | Path Class | Description |
|--------------|------------|-------------|
| `straight` | `StraightPath` | Linear interpolation from start to end |
| `arc` | `ArcPath` | Quadratic Bézier curve with configurable arc height |
| `homing` | `ArcPath` | Same as arc, but periodically updates endpoint to track target |
| `ballistic` | `BallisticPath` | Pre-computed parabolic trajectory with gravity |

Note: Any movement type can have `tracking: true` to enable target following. Homing always tracks regardless of the tracking flag.

### How It Works

1. **Initialization**: `CreatePath()` creates the appropriate path based on `movement_type`
2. **Each frame**: `_progress` advances based on `speed / path_length`
3. **Position update**: `GlobalPosition = _path.GetPosition(_progress)`
4. **Tracking**: If `tracking: true` or `movement_type: homing`, every 0.1s `UpdatePathTarget()` recalculates the path endpoint using predictive targeting

### Homing with Arc

Homing projectiles use `ArcPath` with endpoint tracking:
- The path endpoint updates to the target's predicted position
- The Bézier control point recalculates to maintain arc shape
- This avoids oscillation that occurs with direction-based homing + arc overlay

### Predictive Targeting

For moving targets, the system predicts where the target will be:

```csharp
intercept = targetPos + (targetVelocity * timeToTarget)
```

Prediction disables when close to target (`< 2.0 units`) to prevent oscillation.

## Pooling

Projectiles are pooled by `ProjectileService` for performance. Key reset behaviors:
- `current_speed` resets to initial `speed`
- Material alpha resets to 1.0
- Particle emitters restart
- Transform resets to origin
