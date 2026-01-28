# Projectile Targeting System

Technical documentation for how projectiles determine where to aim and how to hit moving targets.

## Overview

The targeting system involves three key calculations:
1. **Spawn position** - Where the projectile starts
2. **Target position** - Where the projectile aims
3. **Path tracking** - How the projectile follows moving targets

## Target Position Calculation

### How It Works

Target position is calculated via `Unit3D.get_projectile_target_position()`:

```csharp
public Vector3 get_projectile_target_position()
{
    float height = VisualComponent?.GetSpriteHeight() ?? 1.0f;
    return GlobalPosition + new Vector3(0, height * CenterMassHeightFraction, 0);
}
```

This uses `VisualComponent.GetSpriteHeight()` which correctly accounts for:
- `FeetOffsetPixels` - pixels from texture bottom to feet
- `HeadOffsetPixels` - pixels from texture top to head
- `ScaleFactor` - sprite scale in viewport
- `PixelSize` - world units per pixel (0.0122)

### Why Manual Markers Don't Work

Previously, units had `ProjectileTargetPoint` markers placed manually in scenes. This caused targeting bugs because:

1. **Complex sprite positioning**: The 2.5D sprite system positions visuals based on FeetOffset, HeadOffset, viewport size, and pixel size
2. **Markers don't auto-update**: When sprite configuration changes, markers become stale
3. **Flying units compound the issue**: Flight altitude adds to GlobalPosition.Y, making marker math error-prone

**Do NOT use manual ProjectileTargetPoint markers for targeting.** The automatic calculation from VisualComponent is always correct.

### Flying Units

Flying units (e.g., Puff) have:
- `MovementLayer = 1` (AIR)
- `FlightAltitude = 2.5` (or other value)

Their `GlobalPosition.Y` includes the flight altitude. The targeting calculation correctly handles this because it adds to `GlobalPosition`, not to a fixed world Y.

## Spawn Position

Spawn position uses `RangedUnit3D.GetProjectileSpawnPosition()`:

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

## Path Tracking

### The `tracking` Property

Projectiles can have `"tracking": true` in their JSON config. This enables continuous target updates for any movement type.

```json
{
  "movement_type": "straight",
  "tracking": true,
  "speed": 18.0
}
```

### How Tracking Works

Every 0.1 seconds, `Projectile3D.UpdatePathTarget()` is called:

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

### 4. DamageSystem Method Names

**Problem**: Projectiles call `damageSystem.Call("apply_damage", ...)` but C# uses PascalCase.

**Solution**: DamageSystem has snake_case aliases: `apply_damage()`, `apply_healing()`.

### 5. Collision Shape Missing

**Problem**: Area3D needs a CollisionShape3D to detect overlaps.

**Solution**: `base_projectile_3d.tscn` includes a SphereShape3D (radius 0.2).

## Key Files

| File | Purpose |
|------|---------|
| `scripts/csharp/Units/Unit3D.cs` | `get_projectile_target_position()` |
| `scripts/csharp/Units/RangedUnit3D.cs` | `SpawnProjectile()`, `GetProjectileSpawnPosition()` |
| `scripts/csharp/Projectiles/Projectile3D.cs` | Path movement, tracking, collision |
| `scripts/csharp/Projectiles/ProjectileData.cs` | JSON config parsing |
| `scripts/csharp/Visual/SpriteVisualComponent.cs` | `GetSpriteHeight()`, sprite positioning |
| `scenes/projectiles/base_projectile_3d.tscn` | Base scene with collision shape |
| `data/projectiles/*.json` | Projectile configurations |

## Testing Checklist

When modifying the targeting system:

- [ ] Test ranged unit vs stationary target
- [ ] Test ranged unit vs moving target
- [ ] Test ground unit vs flying unit
- [ ] Test flying unit vs ground unit
- [ ] Test flying unit vs flying unit
- [ ] Verify damage is applied (check HP bars)
- [ ] Check projectile visual matches path (not offset)
