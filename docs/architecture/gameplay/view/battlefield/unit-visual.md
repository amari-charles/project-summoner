# UnitVisual

C# class extending `Node3D`. Visual shell for one unit.

**Old name:** `Unit3D` (renamed to describe role, not engine type)

## What It Is

A passive visual shell. Reads its own state each frame and exposes reaction methods for EntityManager to call on discrete events.

## Responsibilities

### Self-Sync (continuous)
Each frame in `_PhysicsProcess`, reads its own `UnitState` from `IGameSession.GetState()`. Positions the model, updates the HP bar, and sets the animation state. The shell decides how to render the data — EntityManager doesn't know about sprites or animations.

### Event Reactions (discrete)
Exposes methods that EntityManager calls when events arrive:

| Method | Triggered By |
|--------|-------------|
| `PlayAttackAnimation()` | UnitAttackedEvent |
| `FlashDamage()` | UnitDamagedEvent |
| `BeginDeath()` | UnitDiedSimEvent |
| `ShowBuffIcon(buff)` | BuffAppliedSimEvent |
| `ShowEvadeText()` | AttackEvadedEvent |

### Sub-Components
Owns visual sub-components:
- `IVisualComponent` — sprite or skeletal rig
- `ShadowComponent` — ground shadow
- `SpawnRevealComponent` — spawn-in animation
- HP bar display

## What It Does NOT Do

- Targeting logic
- Behavior state machine
- Attack cooldowns
- Damage calculation
- Signal subscriptions to SimulationNode
- No `IsSimDriven` flag — all units are sim-driven

## API

| Method | Purpose |
|--------|---------|
| `_PhysicsProcess(delta)` | Self-sync: read UnitState, position model, update HP/animation |
| `PlayAttackAnimation()` | Trigger attack visual (called by EntityManager) |
| `FlashDamage()` | Trigger damage flash (called by EntityManager) |
| `BeginDeath()` | Start death animation sequence (called by EntityManager) |
| `SetUnitId(id)` | Bind to a specific unit in MatchState |

## Dependencies

| Direction | Component | Relationship |
|-----------|-----------|-------------|
| Reads | `IGameSession` | Polls `GetState()` for own UnitState each frame |
| Created by | `EntityManager` | Lifecycle managed externally |
| Owns | `IVisualComponent` | Sprite or skeletal visual |
| Owns | `ShadowComponent` | Ground shadow |

## Today

`Unit3D` is 2304 lines mixing game logic with rendering (issue #23):

**Keeps (~1100 lines):**
- Visual component setup (IVisualComponent, shadow, HP bar)
- Animation updates
- Position sync from state

**Loses (~1200 lines):**
- `UpdateTargeting()` — moves to SimTargeting
- `UpdateBehavior()` — moves to SimBehavior
- `UpdateCooldowns()` — moves to sim subsystems
- Trigger system — moves to SimAbility
- `TakeDamage()` game logic path — moves to SimDamage
- Signal subscriptions to SimulationNode
- `IsSimDriven` flag — all units are sim-driven, no branching

## Stub

```csharp
// scripts/csharp/Battle/View/UnitVisual.cs
public partial class UnitVisual : Node3D
{
    private IGameSession? _session;
    private int _unitId;
    private bool _isAlive = true;
    private bool _loggedMissing;

    // Sub-components (already exist in codebase)
    // IVisualComponent  — scripts/csharp/Battle/View/Visual/IVisualComponent.cs
    // ShadowComponent   — scripts/csharp/Battle/View/Visual/ShadowComponent.cs
    // SpawnRevealComponent — scripts/csharp/Units/Components/SpawnRevealComponent.cs
    // FloatingHPBar via HPBarService — scripts/csharp/Meta/Services/HPBarService.cs

    public void Initialize(IGameSession session, int unitId) { throw new NotImplementedException(); }

    public override void _PhysicsProcess(double delta)
    {
        // Read UnitData from _session.GetState().Units[_unitId]
        // Sync: position, facing, HP bar, animation from BehaviorState
        throw new NotImplementedException();
    }

    // --- Event Reactions (called by EntityManager) ---
    public void PlayAttackAnimation() { throw new NotImplementedException(); }
    public void FlashDamage(float damage, bool isCrit) { throw new NotImplementedException(); }
    public void BeginDeath() { throw new NotImplementedException(); }
    public void ShowBuffIcon(EffectType effectType) { throw new NotImplementedException(); }
    public void ShowEvadeText() { throw new NotImplementedException(); }
}
```
