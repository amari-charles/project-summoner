# View & Input Layer — Design Specs

Decomposition plans for existing components that span View and Input boundaries. Each section covers what stays, what moves, the migration sequence, and edge cases.

For the View layer overview, see [README.md](README.md). For the Input layer overview, see [../input/README.md](../input/README.md). For Session specs that View/Input interact with, see [../session/design-specs.md](../session/design-specs.md).

---

## 1. HandUI Split

**Current:** `scripts/battle/ui/hand_ui.gd` (813 lines) — handles both card rendering and drag gesture capture.

### Responsibility Breakdown

| Responsibility | Layer | Lines (approx) |
|---------------|-------|----------------|
| Card rendering, layout, fan positioning | View | ~350 |
| Hover animation, glow, 3D perspective shader | View | ~200 |
| Draw/entrance animation | View | ~80 |
| Drag-and-drop gesture (`_get_drag_data`) | Input | ~50 |
| Mana-based affordance glow (playable cards pulse) | View | ~60 |
| Card availability checking | View (reads MatchState) | ~30 |

### Target Design

**HandUI stays in View.** It renders the hand, animates cards, and manages visual effects. ~85% of current code stays.

**Drag gesture moves to InputCollector.** The `_get_drag_data()` method on `CardDisplay` is the only Input concern — it initiates the drag operation that eventually produces a `PlayCardCommand`. In the target architecture:

```
HandUI (View)                    InputCollector (Input)
  |                                    |
  |-- renders cards from MatchState    |-- receives drag start event
  |-- hover/glow/animation            |-- receives drop on BattlefieldDropZone
  |-- fan layout                      |-- calls session.SubmitCommand(PlayCardCommand)
```

### Migration Steps

1. **No split needed yet.** `HandUI` can keep `_get_drag_data()` during migration — Godot's drag-and-drop system requires it on the source Control. The conceptual boundary is: `_get_drag_data()` packages data, `BattlefieldDropZone._drop_data()` produces the Command.
2. When `InputCollector` is implemented, it owns the Command-production side (currently in `BattlefieldDropZone._drop_data()` → `summoner.play_card_3d()`).
3. `HandUI` continues to read `IGameSession.GetState()` for hand contents, mana, and card availability.

### Key Invariant

HandUI never calls `SubmitCommand`. It only provides visual affordances (which cards are playable) and initiates Godot drag data. The Command is produced by InputCollector when the drop completes.

---

## 2. SpellTargetingManager Retirement

**Current:** `scripts/battle/ui/spell_targeting_manager.gd` (375 lines) — autoload managing two-stage spell targeting (click to place circle, drag to set destination).

### Responsibility Breakdown

| Responsibility | Layer | Lines (approx) |
|---------------|-------|----------------|
| State machine (INACTIVE → AWAITING_FIRST_CLICK → DRAGGING_ARROW) | Input | ~60 |
| Mouse input processing (`_process`, `_unhandled_input`) | Input | ~100 |
| Raycast from screen to battlefield plane | Input | ~30 |
| Circle preview visual + arrow visual | View | ~80 |
| Unit selection in radius | Input (reads MatchState) | ~40 |
| Card data lookup (selection_radius from catalog) | Cross-cutting | ~20 |
| Signal emission (`targeting_cancelled`) | Input | ~10 |

### Target Design

SpellTargetingManager is **retired as an autoload**. Its responsibilities split:

**InputCollector** absorbs the state machine and gesture handling:
- `start_targeting()`, `cancel_targeting()` become InputCollector methods
- State machine (INACTIVE, AWAITING_FIRST_CLICK, DRAGGING_ARROW) lives in InputCollector
- On confirmation: `InputCollector.OnSpellTargetConfirmed(cardIndex, position, targetUnitId)` → `session.SubmitCommand(CastSpellCommand)`

**View** absorbs preview visuals:
- Circle preview and arrow visual are View components
- InputCollector tells View "show preview at X" / "hide preview"
- View reads targeting state from InputCollector's public state (e.g., `InputCollector.SpellTargetPosition`)

### Communication Pattern

```
InputCollector                          View (SpellPreview component)
  |                                        |
  |-- SpellTargetingState changed -------->|-- reads state each frame
  |   (position, radius, phase)            |-- renders circle + arrow
  |                                        |
  |-- SubmitCommand(CastSpellCommand) ---> IGameSession
```

InputCollector exposes read-only targeting state. View polls it. No direct coupling.

### Migration Steps

1. Move state machine + gesture logic into `InputCollector`
2. Extract circle/arrow rendering into a View component (or let EntityManager handle it)
3. Remove SpellTargetingManager autoload from `project.godot`
4. Update `BattlefieldDropZone` to route through InputCollector instead of SpellTargetingManager

---

## 3. RedirectManager → Command

**Current:** `scripts/managers/redirect_manager.gd` (402 lines) — autoload for tactical unit redirection. Actual mouse handling lives in `game_controller_3d.gd:676-789`.

### Responsibility Breakdown

| Component | Responsibility | Layer |
|-----------|---------------|-------|
| RedirectManager: `select_units_in_radius()` | Query units near click | Input (reads scene tree) |
| RedirectManager: `find_nearest_enemy()` | Find redirect target | Input (reads scene tree) |
| RedirectManager: `apply_forced_targets()` | Apply redirect to units | **Simulation** (mutates unit behavior) |
| RedirectManager: cooldown tracking | Cooldown timers | **Simulation** (game rule) |
| RedirectManager: mode state + signals | UI mode management | Input |
| GameController3D: `_unhandled_input()` | Mouse click/drag handling | Input |
| GameController3D: unit tinting | Visual feedback | View |
| RedirectIndicator: circle + arrow | Visual feedback | View |

### Target Design

**RedirectCommand** — new Command type in the simulation:

```csharp
public record RedirectCommand(
    int PlayerIndex,
    Vector3 SelectionCenter,
    float SelectionRadius,
    Vector3 TargetPosition,
    bool IsAttack          // true = attack redirect, false = defend redirect
) : ICommand;
```

**InputCollector** absorbs gesture handling:
- Redirect button press → InputCollector enters redirect mode
- Click/drag/release → InputCollector collects selection center + target position
- On release: `session.SubmitCommand(new RedirectCommand(...))`

**Simulation** handles redirect logic:
- `CommandRouter` validates: cooldown check, valid player index, valid positions
- Simulation selects units in radius, finds nearest enemy, applies forced targets
- Cooldown tracking moves to `MatchState` (per-player cooldown timers)

**View** handles visuals:
- RedirectIndicator (circle + arrow) becomes a View component
- Unit tinting on selection is View responding to InputCollector's redirect state

### Migration Steps

1. Define `RedirectCommand` in simulation
2. Move `apply_forced_targets()` + cooldown logic into simulation (tick-driven)
3. Move gesture handling from `GameController3D._unhandled_input()` into InputCollector
4. Keep RedirectIndicator as a View component that reads InputCollector state
5. Remove RedirectManager autoload

---

## 4. SummonPreview Migration

**Current:** `scripts/csharp/Battle/Input/SummonPreview.cs` + `UnitGhost.cs` (namespace `Fateforged.Input`) — shows ghost units at spawn location during card drag.

### Decision: Input-Layer Component

SummonPreview is **Input**. Although it renders visual feedback, its lifecycle is gesture-driven (created on drag start, destroyed on drag end). MatchState has no "drag in progress" concept, so EntityManager can't manage SummonPreview's lifecycle — InputCollector must own it. See [documentation-guide.md principle #10](../../migration/documentation-guide.md).

### Target Design

```
InputCollector (Input)              SummonPreview (Input)
  |                                     |
  |-- owns lifecycle:                  |-- created on drag start
  |   DraggedCardIndex, DragPosition   |-- destroyed on drag end
  |                                    |-- reads session.GetState() for card data
  |                                    |-- colors valid/invalid based on position
```

SummonPreview reads `InputCollector.DragPosition` and `InputCollector.DraggedCardIndex` to know what to show. It reads `IGameSession.GetState()` to determine if the position is valid (player's half of battlefield).

### Migration Steps

1. SummonPreview reads from InputCollector state instead of BattlefieldDropZone
2. Remove direct coupling to BattlefieldDropZone's internal state
3. No structural changes needed — it's already in `scripts/csharp/Battle/Input/`

---

## 5. Summoner Decomposition

**Current:** `scripts/core/summoner.gd` (979 lines) — mixes deck management, sim registration, mana/HP state, command production, visual rendering, and hit feedback.

### Responsibility Breakdown

| Responsibility | Layer | Lines (approx) |
|---------------|-------|----------------|
| Deck loading (strategy pattern, profile, BattleContext) | Session init | ~150 |
| `RegisterSummoner()` call + sim signal connections | Session init | ~60 |
| `init()` / `init_as_client()` orchestration | Session init | ~80 |
| `play_card_3d()` — command production | Input | ~30 |
| `_poll_match_state()` — HP, mana, hand, casting polling | View (reads MatchState) | ~100 |
| `_spawn_visual_unit()` — unit spawn presentation | View | ~20 |
| Hit feedback (flash, shake) | View | ~60 |
| HP bar creation | View | ~15 |
| Hurtbox setup + collision shape | View (combat visuals) | ~40 |
| Summoner bonuses application | Session init | ~40 |
| State variables (mana, HP, hand, deck, casting) | Today: local state; Target: MatchState | ~50 |
| Signals (card_played, mana_changed, hp_changed, etc.) | View (event dispatch) | ~20 |

### Target Design

Summoner decomposes into **four pieces**:

#### a) Session Init (absorbed into Session construction)

Deck loading, `RegisterSummoner()`, summoner bonus application — all happen at session construction time via `BattleConfig.PlayerConfig`. The session constructs the `Simulation` with the right initial state. No `Summoner` object needed for this.

```
BattleConfig.PlayerConfig:
  - SummonerId, Deck, SummonerData
  → Session passes to Simulation constructor
  → Simulation initializes MatchState.Summoners[i] with HP, mana, deck, hand
```

#### b) MatchState (already exists)

Mana, HP, hand, deck, casting state — these already live in `MatchState.Summoners[]`. Summoner's local state variables become redundant once View reads from MatchState.

#### c) SummonerVisual (View)

Visual shell for a summoner. Self-syncs from `MatchState.Summoners[playerIndex]` each frame.

```
SummonerVisual:
  - Reads HP, mana, casting state from MatchState each frame
  - Owns visual: Sprite3D, hit flash, shake
  - Owns HP bar (via HPBarService or inline)
  - Owns hurtbox mesh (visual only — sim handles combat)
  - Fires visual events (casting_started, etc.) for BattleHUD
```

#### d) InputCollector (Input)

`play_card_3d()` logic moves to InputCollector. The pre-validation (mana check, casting check) is done by `CommandRouter` in the session. InputCollector just packages the Command.

### Migration Steps

1. `BattleConfig.PlayerConfig` already carries deck + summoner data (Phase 3, §5)
2. Session construction initializes `MatchState.Summoners[]` from `BattleConfig`
3. Create `SummonerVisual` that reads `MatchState.Summoners[playerIndex]` each frame
4. Move command production (`play_card_3d`) to InputCollector
5. Remove `summoner.gd` — its responsibilities are distributed

### Edge Cases

- **Multiplayer client init:** Currently `init_as_client()` does lightweight setup. In target: `ClientSession` applies host snapshots to `MatchState.Summoners[]`, `SummonerVisual` reads it. No client-specific init path.
- **Deferred deck loading:** For event_sequence battles, the session loads the deck when the event triggers, not at construction.

---

## 6. GameController3D Decomposition

**Current:** `scripts/core/game_controller_3d.gd` (1048 lines) — top-level battle scene controller mixing session init, game flow, view wiring, and input handling.

### Responsibility Breakdown

| Responsibility | Layer | Lines (approx) |
|---------------|-------|----------------|
| `_ready()` init sequence (phases 1-6.5) | Session construction | ~120 |
| SimulationNode creation + initialization | Session construction | ~30 |
| Summoner initialization orchestration | Session construction | ~80 |
| Win condition setup | Session construction | ~60 |
| Multiplayer setup (`_setup_multiplayer()`) | Session construction | ~50 |
| AI loading | Session construction | ~30 |
| `start_game()` / `end_game()` | Session (game flow) | ~40 |
| `_process()` — timer updates, phase transitions | Session (ticking) | ~60 |
| `_poll_match_state()` — client state polling | View (reads MatchState) | ~30 |
| UI initialization + game over display | View | ~80 |
| Unit tinting for redirect | View | ~30 |
| `_unhandled_input()` — redirect mouse handling | Input | ~120 |
| Raycast helper | Input utility | ~30 |
| Scene preloading | Infrastructure | ~50 |
| Signal connections + cleanup | Various | ~80 |
| Modifier provider registration | Meta-game | ~20 |

### Target Design

GameController3D becomes **BattleScene** (View facade) after extracting Session and Input concerns.

#### What moves to Session

The entire init sequence becomes session construction:

```csharp
// Session constructor handles:
// - SimulationNode creation (or Simulation directly)
// - Summoner registration from BattleConfig
// - Win condition configuration
// - AI controller setup (Input peer)
// - Multiplayer transport wiring
// - Game flow (start, tick, end)
```

`_process()` timer logic, phase transitions, and game flow (`start_game`, `end_game`) move to Session's `Tick()`.

#### What stays as BattleScene (View)

```
BattleScene (Node3D, top-level):
  - Initialize(IGameSession session)
  - Wires session to EntityManager, BattleHUD
  - Owns Camera, Environment (state-independent)
  - Game over display
  - Scene preloading (convenience, not logic)
```

This is already documented in [README.md](README.md).

#### What moves to InputCollector (Input)

Redirect mouse handling (`_unhandled_input`, lines 676-789) and the raycast helper move to InputCollector.

### Migration Steps

1. Session construction absorbs init phases 1-6.5 (SimNode, summoners, AI, MP, win conditions)
2. Session `Tick()` absorbs `_process()` timer/phase logic
3. `BattleScene.Initialize(session)` wires View components
4. Redirect input moves to InputCollector
5. Rename file from `game_controller_3d.gd` to `battle_scene.gd` (or migrate to C#)

---

## 7. SimEventSignalEmitter Retirement

**Current:** `scripts/csharp/Battle/Simulation/SimEventSignalEmitter.cs` (109 lines) — visitor that converts `SimEvent`s into Godot signals on `SimulationNode`.

### Why It's Retired

In the target architecture, `SimulationNode` is replaced by `IGameSession`. View components read events via `IGameSession.SimEventsEmitted`, not via Godot signals. The visitor pattern converting to signals becomes unnecessary.

### What Replaces It

**EntityManager** subscribes to `IGameSession.SimEventsEmitted` and routes events to the correct visual shell:

```csharp
// EntityManager._Ready()
_session.SimEventsEmitted += OnSimEvents;

void OnSimEvents(IReadOnlyList<SimEvent> events)
{
    foreach (var e in events)
    {
        switch (e)
        {
            case UnitAttackedEvent attack:
                GetShell(attack.AttackerUnitId)?.OnAttack(attack);
                break;
            case UnitDamagedEvent damage:
                GetShell(damage.TargetUnitId)?.OnDamaged(damage);
                break;
            case UnitDiedSimEvent death:
                GetShell(death.UnitId)?.OnDeath(death);
                break;
            case GameOverEvent gameOver:
                // BattleHUD handles this independently
                break;
            // ...
        }
    }
}
```

### Currently No-Op Events

These events have no signal today (lines 100-108): `UnitActivationChanged`, `SpellCast`, `ProjectileHit`, `AttackEvaded`, `BuffApplied`, `BuffExpired`, `DelayedEffectFired`, `SummonerDamaged`, `SummonerDestroyed`. EntityManager should handle all of them for visual feedback (VFX, sound cues, etc.).

### Migration Steps

1. EntityManager subscribes to `IGameSession.SimEventsEmitted`
2. Route each event type to the appropriate visual shell
3. BattleHUD subscribes independently for HUD-relevant events (phase, timer, HP, mana)
4. Remove `SimEventSignalEmitter.cs`
5. Remove all signal declarations from `SimulationNode` that were only used by the emitter

---

## 8. BattlefieldDropZone Migration

**Current:** `scripts/battle/ui/battlefield_drop_zone.gd` (515 lines) — handles card drop detection, spawn preview, and spell targeting forwarding.

### Responsibility Breakdown

| Responsibility | Layer | Lines (approx) |
|---------------|-------|----------------|
| `_can_drop_data()` — drop validation (mana, position, card type) | Input | ~90 |
| `_drop_data()` — execute card play (calls `summoner.play_card_3d()`) | Input | ~80 |
| SummonPreview management (create, update, cleanup) | View | ~100 |
| SpellPreview management | View | ~60 |
| SpawnZoneOverlay management | View | ~40 |
| Spell targeting forwarding to SpellTargetingManager | Input | ~30 |
| Position validation (player's half check) | Input | ~30 |
| `_gui_input()` — forwarding to SpellTargetingManager | Input | ~20 |
| Debug spawn handling | Debug | ~60 |

### Target Design

**InputCollector** absorbs drop validation and Command production:
- `_can_drop_data()` validation logic → InputCollector checks (or delegates to CommandRouter pre-validation)
- `_drop_data()` → `InputCollector.OnCardDropped(cardIndex, position)` → `session.SubmitCommand(PlayCardCommand)`
- Spell targeting forwarding → InputCollector owns targeting directly

**View** absorbs preview management:
- SummonPreview reads InputCollector drag state (§4 above)
- SpellPreview reads InputCollector targeting state
- SpawnZoneOverlay reads InputCollector state to show valid zones

### Migration Steps

1. Move drop validation + Command production to InputCollector
2. SummonPreview/SpellPreview/SpawnZoneOverlay become View components reading InputCollector state
3. Remove BattlefieldDropZone as a Godot Control — the "drop zone" concept becomes InputCollector listening for Godot drag events on the viewport

---

## 9. GameUI Migration

**Current:** `scripts/battle/ui/game_ui.gd` (283 lines) — manages all battle HUD elements (timers, HP/mana bars, phase labels, game over display).

### Assessment: ~95% View, No Decomposition Needed

| Responsibility | Layer |
|---------------|-------|
| Timer display | View |
| HP/mana stat bars | View |
| Phase label | View |
| Prep timer (dynamic creation + color thresholds) | View |
| Game over label + restart button | View |
| Signal connections to GameController3D / Summoner | View (event wiring) |

### Target Design

GameUI becomes (or stays as) **BattleHUD**. It reads `IGameSession.GetState()` each frame for continuous data (mana, HP) and subscribes to `IGameSession.SimEventsEmitted` for discrete events (phase change, game over).

```
BattleHUD:
  - Reads MatchState.Summoners[0].Mana, .Hp each frame → updates bars
  - Reads MatchState.MatchTime, .Phase each frame → updates timer/phase
  - Receives GameOverEvent via SimEventsEmitted → shows game over display
```

### Migration Steps

1. Rename `GameUI` to `BattleHUD` (already planned in View README)
2. Replace signal connections with `IGameSession.GetState()` polling + `SimEventsEmitted` subscription
3. Remove dependencies on `GameController3D` and `Summoner` objects — reads MatchState instead

---

## 10. SpawnZoneOverlay Migration

**Current:** `scripts/battle/ui/spawn_zone_overlay.gd` (40 lines) — simple 3D mesh overlay showing invalid spawn zones.

### Assessment: Pure View

SpawnZoneOverlay is entirely View. It creates a semi-transparent red plane over the enemy's half of the battlefield to indicate where units cannot be spawned.

### Target Design

No decomposition needed. SpawnZoneOverlay becomes a View component managed by BattleScene (or by a container that shows/hides it based on InputCollector's drag state).

```
InputCollector.IsDraggingSummonCard = true → SpawnZoneOverlay.visible = true
InputCollector.IsDraggingSummonCard = false → SpawnZoneOverlay.visible = false
```

### Migration Steps

1. SpawnZoneOverlay stays as-is
2. Visibility driven by InputCollector's drag state instead of BattlefieldDropZone's internal state
