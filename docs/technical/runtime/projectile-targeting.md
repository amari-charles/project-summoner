# Projectile Targeting System

Technical documentation for how projectiles determine where to aim and how to hit moving targets.

## Overview

The targeting system involves three key calculations:
1. **Spawn position** - Where the projectile starts
2. **Target position** - Where the projectile aims
3. **Path tracking** - How the projectile follows moving targets

For spell cards, targeting mode now also determines projectile routing:

- `Position` spells (example: Fireball) launch to the cast position and resolve damage on impact.
- `NearestEnemy` spells (example: Mana Bolt) resolve one enemy target at cast time and damage on projectile hit.
- Non-projectile spells still use direct `SimEffects.ApplyEffect()` paths.

## Target Position Calculation

### How It Works

For summoner targets, simulation now resolves target position via `SummonerData.TargetPointPosition`.
`SimulationNode.RegisterSummoner(...)` receives this from `SummonerVisual.GetTargetPointGlobalPosition()`
(scene marker `TargetPoint` when present, otherwise a configured vertical offset).

Target position is calculated via `UnitVisual.get_projectile_target_position()`:

```csharp
public Vector3 get_projectile_target_position()
{
    float height = VisualComponent?.GetSpriteHeight() ?? 1.0f;
    Vector3 baseTarget = GlobalPosition + new Vector3(0, height * CenterMassHeightFraction, 0);

    // Apply configurable offset (X flips with facing direction)
    if (TargetPointOffset != Vector3.Zero)
    {
        float xOffset = _isFacingRight ? TargetPointOffset.X : -TargetPointOffset.X;
        return baseTarget + new Vector3(xOffset, TargetPointOffset.Y, TargetPointOffset.Z);
    }

    return baseTarget;
}
```

This uses `VisualComponent.GetSpriteHeight()` which correctly accounts for:
- `FeetOffsetPixels` - pixels from texture bottom to feet
- `HeadOffsetPixels` - pixels from texture top to head
- `ScaleFactor` - sprite scale in viewport
- `PixelSize` - world units per pixel (0.0122)

### TargetPointOffset Property

For units with off-center bodies, use the `TargetPointOffset` exported property:

```csharp
[Export]
public Vector3 TargetPointOffset { get; set; } = Vector3.Zero;
```

- **X offset**: Flips with facing direction (positive = forward from unit's perspective)
- **Y offset**: Vertical adjustment (positive = up)
- **Z offset**: Depth adjustment (rarely needed)

Example: Puff has `TargetPointOffset = Vector3(0.7, 0, 0)` because its body is offset horizontally from its ground position.

Use the debug menu "Target Points" option to visualize target positions while configuring.

### Flying Units

Flying units (e.g., Puff) have:
- `MovementLayer = 1` (AIR)
- `FlightAltitude = 2.5` (or other value)

Their `GlobalPosition.Y` includes the flight altitude. The targeting calculation correctly handles this because it adds to `GlobalPosition`, not to a fixed world Y.

## Spawn Position

Spawn position uses `UnitVisual.GetProjectileSpawnPosition()` (ranged spawn logic merged into UnitVisual):

```csharp
public Vector3 GetProjectileSpawnPosition()
{
    if (_projectileSpawnPoint != null)
        return _projectileSpawnPoint.GlobalPosition;

    // Fallback: chest height
    float height = VisualComponent?.GetSpriteHeight() ?? 1f;
    return GlobalPosition + new Vector3(0, height * ChestHeightFraction, 0);
}
```

**Note**: Spawn points CAN use markers (`ProjectileSpawnPoint`) because they're relative to the attacker, not the target. The marker's GlobalPosition correctly includes the parent unit's position.

### Simulation Spawn Source (authoritative path)

Simulation-side ranged attacks now compute projectile spawn from unit catalog data in `SimBehavior.ResolveProjectileStartPosition(...)`:

- Base: attacker `UnitData.Position`
- Offset source: `UnitDefinition.Visual.TargetPointOffset`
- Facing: X offset mirrors for left-facing units

This keeps simulation projectile start points aligned with per-unit visual tuning even when no explicit scene marker is used.

## Path Tracking

### The `tracking` Property

Projectiles can have `Tracking = true` in their definition. This enables continuous target updates for any movement type.

```csharp
public static readonly ProjectileData WindPuff = new()
{
    MovementType = ProjectileMovementType.Straight,
    Tracking = true,
    Speed = 18.0f,
    // ...
};
```

### How Tracking Works

Every 0.1 seconds, `ProjectileVisual.UpdatePathTarget()` is called:

```csharp
private void UpdatePathTarget()
{
    Vector3 currentTargetPos = GetTargetPosition(Target);
    Vector3 predictedPos = CalculateInterceptPoint(currentTargetPos, Target);

    // Recreate path from CURRENT position (not original spawn)
    _startPosition = GlobalPosition;
    _targetPosition = predictedPos;
    _progress = 0f;
    CreatePath();
}
```

**Critical**: The path is recreated from the projectile's CURRENT position, not the original spawn. This prevents the "progress overshooting" bug where shortening the path caused position teleportation.

### Tracking vs Homing

| Property | Tracking (straight) | Homing |
|----------|---------------------|--------|
| Path shape | Linear | Arc (Bézier) |
| Updates target | Yes | Yes |
| Visual feel | Direct line | Curved seeking |
| Use case | Fast projectiles | Magic missiles |

### Deceleration and Tracking

**Avoid deceleration with tracking.** The intercept prediction assumes constant speed:

```csharp
float timeToTarget = distance / CurrentSpeed;
return targetPos + (targetVelocity * timeToTarget);
```

With deceleration, actual flight time is longer than predicted, causing consistent undershooting. Use constant speed for tracking projectiles.

## Common Pitfalls

### 1. Manual ProjectileTargetPoint Markers

**Problem**: Markers placed in editor don't match actual sprite positions.

**Solution**: Don't use markers for targeting. The code calculates from VisualComponent automatically.

### 2. Progress Overshooting

**Problem**: When `UpdateTarget()` shortened the path, progress didn't adjust, causing position jumps.

**Solution**: Reset progress to 0 and recreate path from current position on each tracking update.

### 3. Deceleration with Tracking

**Problem**: Intercept calculation assumes constant speed.

**Solution**: Use constant speed (`acceleration: 0`) for tracking projectiles.

### 4. Damage Pipeline Method Names

**Problem**: Projectiles previously called `DamageSystem.Call("apply_damage", ...)`.

**Solution**: Damage logic now lives in `SimBehavior` + `SimEffects` in the simulation layer. Projectile hits are resolved via `SimProjectile.ApplyHit()` which calls `SimDamage.Calculate()`.

### 5. Summoner Target Routing Mismatch

**Problem**: If ranged summoner-target attacks use direct damage while unit-target attacks use projectiles, visuals/timing diverge and attacks appear to "do damage with no projectile."

**Solution**: Summoner-target ranged attacks now spawn `SimProjectileData` and resolve summoner damage on projectile impact (`SimProjectile.ApplySummonerHitAtImpact()`), matching unit-target projectile flow.

### 6. Collision Shape Missing

**Problem**: Area3D needs a CollisionShape3D to detect overlaps.

**Solution**: `base_projectile_3d.tscn` includes a SphereShape3D (radius 0.2).

## Key Files

| File | Purpose |
|------|---------|
| `scripts/csharp/Battle/View/UnitVisual.cs` | `get_projectile_target_position()`, `GetProjectileSpawnPosition()` (ranged spawn logic merged here) |
| `scripts/csharp/Battle/View/ProjectileVisual.cs` | Path movement, tracking, collision |
| `scripts/csharp/Battle/Simulation/Combat/SimProjectile.cs` | Simulation-layer projectile ticking, hit detection, damage application |
| `scripts/csharp/Infrastructure/Data/Projectiles/ProjectileData.cs` | Projectile configuration data class |
| `scripts/csharp/Infrastructure/Data/Projectiles/ProjectileDefinitions.cs` | Static projectile definitions |
| `scripts/csharp/Battle/View/Visual/SpriteVisualComponent.cs` | `GetSpriteHeight()`, sprite positioning |
| `scenes/battle/projectiles/base_projectile_3d.tscn` | Base scene with collision shape |

## Testing Checklist

When modifying the targeting system:

- [ ] Test ranged unit vs stationary target
- [ ] Test ranged unit vs moving target
- [ ] Test ground unit vs flying unit
- [ ] Test flying unit vs ground unit
- [ ] Test flying unit vs flying unit
- [ ] Verify damage is applied (check HP bars)
- [ ] Check projectile visual matches path (not offset)
