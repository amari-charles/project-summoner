# SummonerVisual

C# class extending `Node3D`. Visual shell for one summoner.

**Old name:** Visual code currently embedded in `summoner.gd` (no separate visual class exists today)

## What It Is

A registered visual shell. Same self-sync model as UnitVisual — reads its own `SummonerData` from `IGameSession.GetState()` each frame. Registered by EntityManager at battle init rather than dynamically spawned, since summoners are always present for the entire battle.

## Responsibilities

### Self-Sync (continuous)
Each frame in `_PhysicsProcess`, reads its own `SummonerData` from `IGameSession.GetState()`. Updates visual state and internal HP tracking. Unlike UnitVisual, position is fixed (summoners don't move), so only HP and alive status need syncing.

### Event Reactions (discrete)
Exposes methods that EntityManager calls when events arrive:

| Method | Triggered By |
|--------|-------------|
| `FlashDamage()` | SummonerDamagedEvent |
| `BeginDeath()` | SummonerDestroyedEvent |

### Sub-Components
Owns visual sub-components:
- `Sprite3D` — summoner character sprite
- `FloatingHPBar` — internal HP sync bar object (world visibility optional; HUD is default for summoner HP display)
- `HurtboxComponent` — combat hit detection capsule (radius 2.0, height 6.25)

## What It Does NOT Do

- Deck management (hand, draw, discard)
- Mana tracking
- Card play orchestration
- Casting state machine
- Summoner stats/progression
- Summoner selection or profile loading

All of the above is game state that lives in the Simulation layer (`SummonerData` in MatchState) or orchestration that lives in Session/Input layers.

## API

| Method | Purpose |
|--------|---------|
| `_PhysicsProcess(delta)` | Self-sync: read SummonerData, update visual/internal HP state |
| `FlashDamage()` | Trigger damage flash (called by EntityManager) |
| `BeginDeath()` | Start death animation sequence (called by EntityManager) |
| `SetSummonerId(id)` | Bind to a specific summoner in MatchState |

## Dependencies

| Direction | Component | Relationship |
|-----------|-----------|-------------|
| Reads | `IGameSession` | Polls `GetState()` for own SummonerData each frame |
| Registered by | `EntityManager` | Registered at battle init, not dynamically spawned |
| Owns | `Sprite3D` | Character sprite |
| Owns | `FloatingHPBar` | Internal HP sync; world visibility controlled by `ShowWorldHpBar` |
| Owns | `HurtboxComponent` | Combat hit detection |

## Today

`summoner.gd` is a god class mixing three concerns:

**Becomes SummonerVisual (~100 lines):**
- Sprite3D setup and display
- FloatingHPBar management (width 1.5, offset Y 2.5, hidden in-world by default; HUD owns summoner HP presentation)
- HurtboxComponent (capsule radius 2.0, height 6.25)
- Damage flash animation (flash duration, color tween, shake)
- Death visual sequence
- HP sync from state

**Moves to Simulation layer (SummonerData in MatchState):**
- HP tracking (`current_hp`, `max_hp`, `is_alive`)
- Mana state (`mana`, `max_mana`)
- Deck/hand/discard state
- Casting state machine (`is_casting`, `casting_time_remaining`)
- Summoner stats (`cast_speed`)

**Moves to Input/Session layers:**
- Card play orchestration (`play_card_3d()`)
- Deck loading strategies
- SimulationNode reference and interaction

## Stub

```csharp
// scripts/csharp/Battle/View/SummonerVisual.cs
public partial class SummonerVisual : Node3D
{
    private IGameSession? _session;
    private int _teamIndex;
    private bool _isAlive = true;

    // Sub-components
    private Sprite3D? _sprite;
    // FloatingHPBar (width 1.5, offset Y 2.5, always visible)
    // HurtboxComponent (capsule radius 2.0, height 6.25)

    public void Initialize(IGameSession session, int teamIndex) { throw new NotImplementedException(); }

    public override void _PhysicsProcess(double delta)
    {
        // Read SummonerData from _session.GetState().Summoners[_teamIndex]
        // Sync: HP bar, alive status (position is fixed)
        throw new NotImplementedException();
    }

    // --- Event Reactions (called by EntityManager) ---
    public void FlashDamage() { throw new NotImplementedException(); }
    public void BeginDeath() { throw new NotImplementedException(); }
}
```
