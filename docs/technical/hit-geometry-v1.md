# Hit Geometry v1

**Status:** Implemented (simulation + debug tooling)  
**Last Updated:** 2026-03-05

This doc defines the current gameplay hit model in plain language, then maps it to code.

## Plain-English Summary

- A **unit basic attack** is an auto-hit once targeting/range says it can attack. No collision sweep is required for that one target.
- A **projectile hit** is now "first contact" based:
  - projectile size (`HitRadius`)
  - plus target size (`SeparationRadius`)
- A projectile can no longer damage the same unit multiple times during one flight.
- For multiple possible contacts in the same tick, the closest point along the projectile path is resolved first (deterministic).

## Implemented Rules

### 1. Unit-to-unit basic attacks (single target)

Single-target melee/ranged attacks resolve damage directly when behavior says "in range + attackable".

Code path:

- `SimBehavior.TickBehavior(...)`
- `ApplyMeleeDamageToUnit(...)` / `SpawnProjectileOrApplyDirect(...)`

This is the "auto hit target in range" case.

### 2. Projectile contact geometry

Projectile contact now uses:

`effective_contact_radius = projectile.HitRadius + target.SeparationRadius`

Code path:

- `SimProjectile.CheckHits(...)`
- `SimProjectile.TryGetSegmentDistanceAndT(...)`
- `SimProjectile.CanHitUnitAtPoint(...)`

### 3. Hit space modes

`ProjectileHitSpace` supports:

- `GroundCylinder`: grounded targets use XZ-distance checks (2.5D). Air targets still use 3D checks.
- `Sphere3D`: all targets use full 3D distance checks.

Config location:

- `ProjectileData.HitSpace`
- parsed from `hit_space` in projectile data.

### 4. Anti-repeat-hit guard

Each projectile tracks hit unit IDs and skips already hit units.

Code path:

- `SimProjectileData.HitUnitIds`
- `SimProjectile.CheckHits(...)`
- `SimProjectile.ApplyHit(...)`

### 5. Deterministic multi-contact resolution

Candidates in a tick are sorted by:

1. path contact `t` (nearer first),  
2. contact distance,  
3. unit ID tie-break.

Code path:

- `SimProjectile.CheckHits(...)` sort block.

## Scenario Coverage

### Works now

- **Arrow single hit:** first contacted unit takes damage, projectile dies if no pierce.
- **Piercing bolt:** can continue to next unit(s), but cannot hit the same unit twice.
- **Fireball splash:** AoE applies using radius + target separation size.
- **Grounded gameplay readability:** larger units feel easier to contact because target size matters.

### Not yet in v1 (future work)

- Pole/capsule melee sweeps in front of unit.
- Circle-at-offset melee swings (point in front of attacker).
- Chain-from-primary-hit ("hit target, then nearby units around target").
- True beam/line persistent damage volumes.

## Debug Tooling

Debug Menu now has `Projectile Hit Geometry` toggle.

When enabled:

- each projectile shows live hit shape:
  - disc for `GroundCylinder`
  - sphere for `Sphere3D`
- AoE radius marker is also shown when `AoeRadius > 0`.

Existing `Separation Radius` debug remains relevant because target size is part of contact math.

## QA Checklist

- [ ] Projectile grazes target edge and still hits when `HitRadius + SeparationRadius` is enough.
- [ ] Same projectile cannot repeatedly damage one stationary target over multiple ticks.
- [ ] Two targets in one segment: nearer contact resolves first regardless of insertion order.
- [ ] `GroundCylinder` vs `Sphere3D` visibly differ in debug markers.
- [ ] AoE affects edge targets based on radius + separation size.
