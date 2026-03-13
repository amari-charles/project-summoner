# Projectile System

Technical documentation for the projectile system including movement, acceleration, and visual effects.

## Overview

Projectile simulation is managed by `SimProjectile` (in the simulation layer) and visual presentation by `ProjectileVisual` (in the view layer). Each projectile type is defined in `ProjectileDefinitions.cs` (static C# definitions) and accessed via `ProjectileCatalog` (C# autoload).

## Multiplayer Sync Model

In multiplayer, projectile gameplay remains host-authoritative in simulation, but projectile visuals on clients now use lifecycle messages instead of full per-snapshot projectile state.

- Host keeps full `MatchState.Projectiles` and runs `SimProjectile` for hit/damage truth.
- Regular `StateSnapshot` sync focuses on summoners/units and does not carry active projectile arrays.
- Host broadcasts projectile lifecycle messages:
  - `ProjectileSpawned` (seed visual flight state)
  - `ProjectileImpact` (trigger impact reaction)
  - `ProjectileDespawned` (cleanup fallback)
- On reconnect, host sends `ProjectileSeedSnapshot` (`ActiveProjectileSeed[]`) so clients can rebuild currently active projectile visuals.
- Client reconstructs visual projectile state from these messages and advances flight locally for render smoothness.

## Spell Projectile Execution Path

Damage spells with `SpellProjectileId` now route through simulated projectiles instead of immediate direct damage.

- Entry point: `Simulation.ExecuteSpellEffects()`
- Projectile spawn gate: `TrySpawnSpellProjectile()`
- Runtime simulation: `SimProjectile.TickAll()`

Current targeting behavior:

- `SpellTargetingMode.Position`
- Spawns one projectile from summoner position to the cast position.
- Damage is applied on projectile impact/expire (AoE), not on cast frame.

- `SpellTargetingMode.NearestEnemy`
- Resolves one target and spawns one projectile toward that target.
- Damage is applied when the projectile hits, not immediately.

Notes:

- A one-tick startup hold (`TimeAlive = -1`) is applied at spawn so new projectiles are visible for at least one render frame before simulation movement/expiry.
- Sim-side projectile speed now honors both acceleration (`speed + acceleration * delta`, with `min_speed` floor on deceleration) and speed easing (`speed_start/speed_end/speed_transition_duration/speed_easing/speed_ease_exponent`), matching projectile definition behavior.
- Position-targeted spell projectiles now use `targetUnitId = int.MaxValue` as an invalid non-summoner sentinel so they cannot be misinterpreted as summoner target IDs.

## Unit Attack Projectile Path

Ranged unit attacks now use one projectile path for both unit and summoner targets.

- Entry point: `SimBehavior.TickBehavior()` / `SimBehavior.TickPendingDamage()`
- Projectile spawn: `SimProjectile.Spawn(...)`
- Impact resolution:
  - Unit targets: existing unit contact checks and `ApplyHit(...)`
  - Summoner targets: segment/endpoint checks and `ApplySummonerHitAtImpact(...)`

Implementation details:

- `UnitDefinitions.BuildSimTemplate(...)` now copies the ranged `ProjectileId` into `SimUnitTemplate.ProjectileCatalogId`.
- `Simulation` carries that into `UnitData.ProjectileCatalogId` at spawn time.
- `SimBehavior.TryResolveProjectileData(...)` resolves from `UnitData.ProjectileCatalogId` first, then falls back to unit definition lookup.
- Projectile start position uses catalog `Visual.TargetPointOffset` as muzzle offset, mirroring X by facing direction.

Summoner contact behavior:

- Summoner-target projectiles use the same per-projectile hit radius plus a small summoner contact radius (`0.75f`) for segment checks.
- Summoner aim/hit position now resolves through `SummonerData.TargetPointPosition` (set from scene target point at registration), rather than raw summoner base position.
- Summoner damage and lifecycle events are emitted through simulation events (`SummonerHpChangedEvent`, `SummonerDamagedEvent`, optional `SummonerDestroyedEvent`, and `ProjectileHitEvent`).

## Projectile Data Properties

### Movement

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `movement_type` | string | "straight" | Movement pattern: "straight", "homing", "arc", "ballistic" |
| `tracking` | bool | false | Whether to continuously update target position (for moving targets) |
| `speed` | float | 15.0 | Initial velocity in units/second (legacy, prefer speed_start/speed_end) |
| `acceleration` | float | 0.0 | Speed change per second (legacy, prefer speed easing) |
| `min_speed` | float | 1.0 | Floor for deceleration - prevents projectiles from stopping |
| `speed_start` | float? | null | Starting speed for eased transitions |
| `speed_end` | float? | null | Final speed for eased transitions |
| `speed_transition_duration` | float | 1.0 | Time to transition from start to end speed (seconds) |
| `speed_easing` | string | "linear" | Easing type: "linear", "ease_in", "ease_out", "ease_in_out" |
| `speed_ease_exponent` | float | 2.0 | Exponent for ease_in/ease_out curves (higher = steeper) |
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
| `hit_radius` | float | 2.5 | Projectile contact radius used for overlap checks |
| `hit_space` | string | "ground_cylinder" | Hit-space model: `ground_cylinder` or `sphere_3d` |

Projectile contact uses first-contact math with target size:

`effective_contact_radius = hit_radius + target.separation_radius`

### Weaving Homing Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `veer_delay` | float | 0.15 | Time flying straight before veering (seconds) |
| `veer_angle` | float | 25 | Angle to veer off course (degrees) |
| `veer_duration` | float | 0.25 | Time spent veering before homing (seconds) |
| `steer_strength` | float | 180 | Steering force when homing (degrees/second) |

## Speed Transitions

The projectile system supports two methods for changing speed over time:

### Method 1: Eased Speed Curves (Recommended)

For smooth, organic speed transitions, use the speed easing system:

| Property | Description |
|----------|-------------|
| `speed_start` | Starting speed |
| `speed_end` | Final speed |
| `speed_transition_duration` | Time to complete transition |
| `speed_easing` | Curve type |
| `speed_ease_exponent` | Curve steepness (for EaseIn/EaseOut) |

**Easing Types:**

| Type | Formula | Feel |
|------|---------|------|
| `linear` | `t` | Constant rate of change |
| `ease_in` | `t^exponent` | Starts slow, ends fast |
| `ease_out` | `1 - (1-t)^exponent` | Starts fast, ends slow |
| `ease_in_out` | `(1 - cos(t*π))/2` | Smooth S-curve, slow start and end |

**Example: WeavingBolt (slow start, accelerates into target)**

```csharp
public static readonly ProjectileData WeavingBolt = new()
{
    MovementType = ProjectileMovementType.WeavingHoming,
    SpeedStart = 28.0f,
    SpeedEnd = 60.0f,
    SpeedTransitionDuration = 1.0f,
    SpeedEasing = SpeedEasingType.EaseIn,
    SpeedEaseExponent = 2.5f,
    // ...
};
```

This creates a projectile that:
- Starts at 28 units/s
- Accelerates along an EaseIn curve (slow → fast)
- Reaches 60 units/s after 1 second
- The exponent 2.5 makes the acceleration curve steeper

### Method 2: Linear Acceleration (Legacy)

For simple linear speed changes, use the legacy acceleration system:

- **speed**: Initial velocity (typical range: 10-30 units/s)
- **acceleration**: Speed change per second
  - Positive = accelerate (speed up over time)
  - Negative = decelerate (slow down over time)
- **min_speed**: Prevents projectiles from stopping completely when decelerating

**Example: Wind Puff (starts fast, slows down)**

```csharp
public static readonly ProjectileData WindPuff = new()
{
    Speed = 25.0f,
    Acceleration = -12.0f,
    MinSpeed = 5.0f,
    // ...
};
```

This starts at 25 units/s and decelerates to 5 units/s over ~1.67 seconds:
- Time to reach min_speed = (25 - 5) / 12 = 1.67 seconds

**Note:** If both `speed_start`/`speed_end` and `acceleration` are set, the eased speed system takes precedence.

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

```csharp
public static readonly ProjectileData WindPuff = new()
{
    MovementType = ProjectileMovementType.Straight,
    Tracking = true,
    Speed = 25.0f,
    Acceleration = -12.0f,
    MinSpeed = 5.0f,
    // ...
};
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

```csharp
public static readonly ProjectileData WindPuff = new()
{
    FadeOnHit = true,
    FadeDuration = 0.1f,
    // ...
};
```

This creates a quick 0.1 second fade-out, useful for fast projectiles like wind puffs that shouldn't linger visually.

## Delayed Projectile Spawning

Some units (like Puff) have charge-up attacks where the projectile should spawn partway through the animation rather than immediately.

### Configuration (UnitVisual)

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
| `weaving_homing` | (velocity-based) | Three-phase movement: straight → veer → home |
| `ballistic` | `BallisticPath` | Pre-computed parabolic trajectory with gravity |

Note: Any movement type can have `tracking: true` to enable target following. Homing always tracks regardless of the tracking flag. WeavingHoming uses its own velocity-based movement system.

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

### Weaving Homing (Cult of the Lamb Style)

`WeavingHoming` creates dynamic, serpentine projectile motion using a three-phase velocity-based system:

```
Phase 1: Straight     Phase 2: Veer      Phase 3: Home
    |                    /                   \
    |                   /                     \
Start ------>         /          then         \-----> Target
                     /                          \
```

**How it works:**
1. **Straight phase**: Fly directly toward target for `veer_delay` seconds
2. **Veering phase**: Turn off course by `veer_angle` degrees (randomly left or right) for `veer_duration` seconds
3. **Homing phase**: Steer back toward target using `steer_strength` (degrees/second)

**Parameters:**
- `veer_delay`: Time flying straight before veering (0.15 = quick start)
- `veer_angle`: How sharply to turn off course (20° = gentle curve, 45° = sharp)
- `veer_duration`: Time spent veering (0.25 = brief detour)
- `steer_strength`: How fast the missile can turn when homing (180 = moderate, 300 = snappy)

**Random variation:**
Each projectile randomly veers left or right, so multiple projectiles create varied curved paths.

**Example configuration:**
```csharp
public static readonly ProjectileData WeavingBolt = new()
{
    MovementType = ProjectileMovementType.WeavingHoming,
    SpeedStart = 28.0f,
    SpeedEnd = 60.0f,
    SpeedTransitionDuration = 1.0f,
    SpeedEasing = SpeedEasingType.EaseIn,
    SpeedEaseExponent = 2.5f,
    VeerDelay = 0.3f,
    VeerAngle = 55f,
    VeerDuration = 0.5f,
    SteerStrength = 360f,
    // ...
};
```

### Predictive Targeting

For moving targets, the system predicts where the target will be:

```csharp
intercept = targetPos + (targetVelocity * timeToTarget)
```

Prediction disables when close to target (`< 2.0 units`) to prevent oscillation.

## Damage Routing

All projectile damage is routed through the simulation damage pipeline for consistent hit handling. In the simulation layer, `SimProjectile.ApplyHit()` calls `SimDamage.Calculate()` and emits `UnitDamagedEvent` / `ProjectileHitSimEvent`. The view layer (`ProjectileVisual`) reacts to these events for VFX, audio, and UI feedback.

### Architecture

```
SimProjectile.ApplyHit() ──► SimDamage.Calculate() ──► UnitDamagedEvent
                                                         │
                                                         ▼
ProjectileVisual ◄── ProjectileHitSimEvent ──► VFX/Audio systems
```

### Methods

| Projectile Method | Description |
|-------------------|-------------|
| `HitTarget()` | Direct projectile hit (path completion or body collision) |
| `HitTargetViaHurtbox()` | Hit detected via HurtboxComponent collision |
| `ApplyAoeDamage()` | Area-of-effect damage to all enemies in radius |

All three methods route through `SimProjectile.ApplyHit()` which calls `SimDamage.Calculate()`.

### Unified Damage Pipeline

Previously, projectiles called `DamageSystem.ApplyDamage()` directly. Now all damage (melee and ranged) flows through the same simulation pipeline (`SimDamage.Calculate()`), which:

1. **Emits consistent events**: `UnitDamagedEvent` and `ProjectileHitSimEvent` for VFX/audio
2. **Unified with melee**: Both melee pending damage and projectile hits use the same damage calculation
3. **Proper kill detection**: Death checks happen uniformly in the simulation layer

## Pooling

Projectile visuals are pooled by `EntityManager` via `NodePool<ProjectileVisual>` (see `Infrastructure/Pooling/NodePool.cs`).

`ProjectileVisual` implements `IPoolable`:
- `OnAcquired()` — enables physics/process ticks
- `OnReleased()` — hides node, disables ticks
- `ResetState()` — clears session, ID, debug markers, frees visual model child

Lifecycle is managed exclusively by `EntityManager`:
- Spawn: `_projectilePool.Acquire()` → `Initialize()` → `AddChild()`
- Cleanup: `Deactivate()` → `_projectilePool.Release()`
- `ProjectileVisual` never self-destructs (no `QueueFree` in `_PhysicsProcess`)
