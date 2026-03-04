# EntityManager

C# class. Central coordinator for all 3D battlefield entities — lifecycle, event dispatch, and registry.

**Old names:** `GameView` → `EntityManager` → `EntityManager` (renamed to describe role within `BattleScene`)

## What It Is

A coordinator that manages the lifecycle of visual shells, dispatches discrete events to the correct shell, and maintains a registry for O(1) lookup.

## Responsibilities

### Lifecycle
At battle init, EntityManager registers existing `SummonerVisual` shells (summoners are always present — they aren't spawned mid-battle). During gameplay, when a unit appears in MatchState, it spawns a `UnitVisual` shell. When a projectile appears, it spawns a `ProjectileVisual`. When entities are removed from MatchState, it destroys the corresponding shells. One place for all battlefield spawn/despawn logic.

### Event Dispatch
Subscribes to `IGameSession.SimEventsEmitted`. When a discrete event arrives, looks up the target shell in the registry and calls the appropriate visual method.

**Dispatch table:**

| Event | Action |
|-------|--------|
| UnitAttackedEvent | attacker shell -> `PlayAttackAnimation()` |
| UnitDamagedEvent | target shell -> `FlashDamage()` |
| UnitDiedSimEvent | shell -> `BeginDeath()` + VFXManager death VFX |
| SummonerDamagedEvent | summoner shell -> `FlashDamage()` |
| SummonerDestroyedEvent | summoner shell -> `BeginDeath()` |
| ProjectileHitSimEvent | projectile shell -> `PlayImpactAndDestroy()` |
| SpellCastEvent | VFXManager spell VFX at position |
| BuffAppliedSimEvent | target shell -> show buff icon |
| AttackEvadedEvent | target shell -> floating "MISS" text |
| Phase/timer/mana/HP events | No-op (BattleHUD handles independently) |

### Registry
Maintains `EntityId -> shell` mappings for O(1) event routing.

### Global Control
Single place to pause, slow-mo, or freeze all visuals.

## What It Does NOT Do

- Per-frame sync (shells do that themselves via `_PhysicsProcess`)
- Know about sprites, animations, or HP bars
- Display HUD elements (that's BattleHUD)
- Game logic of any kind

## API

| Method | Purpose |
|--------|---------|
| `RegisterSummonerVisual(SummonerVisual)` | Register existing summoner shell at battle init |
| `SpawnUnitVisual(UnitState)` | Create shell for new unit |
| `SpawnProjectileVisual(ProjectileState)` | Create shell for new projectile |
| `DestroyShell(EntityId)` | Remove shell when entity leaves MatchState |
| `GetShell(EntityId)` | Registry lookup for event routing |
| `Pause() / Resume()` | Global visual control |

## Dependencies

| Direction | Component | Relationship |
|-----------|-----------|-------------|
| Reads | `IGameSession` | MatchState for lifecycle, SimEvents for dispatch |
| Owns | `UnitVisual` | Spawns and destroys |
| Owns | `ProjectileVisual` | Spawns and destroys |
| Owns | `SummonerVisual` | Registered at battle init (not dynamically spawned) |
| Calls | `VFXManager` | Environmental VFX (death, spell, AoE) |

## Today

This behavior is scattered across:
- `Summoner._spawn_visual_unit()` — unit spawning
- `GameController3D._on_remote_unit_spawned()` — network unit spawning
- `SimulationNode._unit3DBySimId` — registry
- `SimEventSignalEmitter` — event-to-signal conversion (partially; VFXManager isn't wired to it)

The current approach has every Unit3D subscribing to SimulationNode signals and filtering by ID — N*M filter checks per tick. EntityManager replaces this with one O(1) registry lookup per event.

## Stub

```csharp
// scripts/csharp/Battle/View/EntityManager.cs
public partial class EntityManager : Node3D, ISimEventVisitor
{
    private IGameSession? _session;
    private readonly Dictionary<int, UnitVisual> _unitRegistry = new();
    private readonly Dictionary<int, ProjectileVisual> _projectileRegistry = new();
    private readonly Dictionary<int, SummonerVisual> _summonerRegistry = new();

    // --- Initialization ---

    public void Initialize(IGameSession session)
    {
        throw new NotImplementedException();
    }

    public void RegisterSummonerVisual(SummonerVisual shell, int teamIndex)
    {
        throw new NotImplementedException();
    }

    // --- Lifecycle (called each frame) ---

    public override void _PhysicsProcess(double delta)
    {
        // Diff MatchState entity lists against registries.
        // Spawn shells for new IDs, destroy shells for removed IDs.
        throw new NotImplementedException();
    }

    // --- Shell Factory ---

    private UnitVisual SpawnUnitShell(UnitData unitData) { throw new NotImplementedException(); }
    private ProjectileVisual SpawnProjectileShell(SimProjectileData projData) { throw new NotImplementedException(); }
    private void DestroyShell(int entityId) { throw new NotImplementedException(); }

    // --- ISimEventVisitor (event dispatch to shells) ---

    public void Visit(UnitAttackedEvent e) { throw new NotImplementedException(); }
    public void Visit(UnitDamagedEvent e) { throw new NotImplementedException(); }
    public void Visit(UnitDiedSimEvent e) { throw new NotImplementedException(); }
    public void Visit(ProjectileHitSimEvent e) { throw new NotImplementedException(); }
    public void Visit(SummonerDamagedEvent e) { throw new NotImplementedException(); }
    public void Visit(SummonerDestroyedEvent e) { throw new NotImplementedException(); }
    public void Visit(SummonerHpChangedEvent e) { } // HUD handles via polling
    public void Visit(AttackEvadedEvent e) { throw new NotImplementedException(); }
    public void Visit(BuffAppliedSimEvent e) { throw new NotImplementedException(); }
    public void Visit(SpellCastEvent e) { throw new NotImplementedException(); }
    public void Visit(DelayedEffectFiredSimEvent e) { throw new NotImplementedException(); }

    // --- No-op visitors (HUD handles these, or no visual action needed) ---
    public void Visit(PhaseChangedEvent e) { }
    public void Visit(PrepTimerUpdatedEvent e) { }
    public void Visit(MatchTimeUpdatedEvent e) { }
    public void Visit(SummonerManaChangedEvent e) { }
    public void Visit(CastingStartedEvent e) { }
    public void Visit(CastingCompletedEvent e) { }
    public void Visit(CardDrawnEvent e) { }
    public void Visit(HandChangedEvent e) { }
    public void Visit(DeckRecycledEvent e) { }
    public void Visit(UnitRegisteredEvent e) { }
    public void Visit(UnitRemovedEvent e) { }
    public void Visit(GameOverEvent e) { }
    public void Visit(UnitActivationChangedEvent e) { }
    public void Visit(BuffExpiredSimEvent e) { }

    // --- Global Control ---

    public void Pause() { throw new NotImplementedException(); }
    public void Resume() { throw new NotImplementedException(); }
}
```
