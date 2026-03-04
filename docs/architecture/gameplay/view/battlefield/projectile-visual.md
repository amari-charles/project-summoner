# ProjectileVisual

C# class extending `Node3D`. Visual shell for one projectile.

**Old name:** `ProjectileView` (renamed for consistency with role-based naming)

## What It Is

A passive visual shell. Reads its own state each frame and exposes an impact method for EntityManager to call on hit events.

## Responsibilities

### Self-Sync (continuous)
Each frame in `_PhysicsProcess`, reads its own `ProjectileState` from `IGameSession.GetState()`. Positions and rotates the projectile model to match.

### Event Reactions (discrete)

| Method | Triggered By |
|--------|-------------|
| `PlayImpactAndDestroy()` | ProjectileHitSimEvent |

### Visual Components
- Holds the visual scene instance (the projectile model)
- Trail VFX (particles, ribbons)

## What It Does NOT Do

- Collision detection
- Damage dealing
- Pierce logic
- HitResolver calls
- Homing target tracking (logic)
- Ground collision detection

All of the above lives in `SimProjectile`.

## API

| Method | Purpose |
|--------|---------|
| `_PhysicsProcess(delta)` | Self-sync: read ProjectileState, position/rotate model |
| `PlayImpactAndDestroy()` | Play impact VFX then queue free (called by EntityManager) |
| `SetProjectileId(id)` | Bind to a specific projectile in MatchState |

## Dependencies

| Direction | Component | Relationship |
|-----------|-----------|-------------|
| Reads | `IGameSession` | Polls `GetState()` for own ProjectileState each frame |
| Created by | `EntityManager` | Lifecycle managed externally |

## Today

`Projectile3D` is 1128 lines mixing collision/damage with visual effects (issue #24):

**Becomes ProjectileVisual (~150 lines):**
- Visual scene instantiation
- Material setup
- Position/rotation sync from ProjectileState
- Impact VFX via VFXManager
- Fade-out tweens
- Particle management

**Moves to SimProjectile:**
- Ground collision logic
- Homing target tracking
- Direct hit detection
- Damage dealing via HitResolver
- Pierce logic
- AoE damage calculation

## Stub

```csharp
// scripts/csharp/Battle/View/ProjectileVisual.cs
public partial class ProjectileVisual : Node3D
{
    private IGameSession? _session;
    private int _projectileId;

    private Node3D? _visualModel;
    private GpuParticles3D? _trail;

    public void Initialize(IGameSession session, int projectileId) { throw new NotImplementedException(); }

    public override void _PhysicsProcess(double delta)
    {
        // Read SimProjectileData from _session.GetState().Projectiles[_projectileId]
        // Sync: position, rotation toward movement direction
        throw new NotImplementedException();
    }

    public void PlayImpactAndDestroy()
    {
        // 1. Stop trail emission
        // 2. VFXManager impact VFX at position
        // 3. Hide model
        // 4. Timer for trail fade, then QueueFree
        throw new NotImplementedException();
    }
}
```
