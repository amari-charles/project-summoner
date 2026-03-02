# BattleHUD

GDScript. 2D battle overlay — mana, timers, HP, hand, game-over.

## What It Is

An independent set of UI components that read `IGameSession` directly. BattleHUD is NOT part of EntityManager — EntityManager owns the 3D battlefield, BattleHUD owns the 2D overlay. Both read the same state source but have no dependency on each other.

## Responsibilities

### Self-Polling (continuous)
Each component polls `IGameSession.GetState()` for its own data:

| Component | Data Polled |
|-----------|-------------|
| `PhaseTimerDisplay` | Game phase, timer countdown |
| `PlayerManaDisplay` | Current mana, max mana |
| `SummonerHPDisplay` | Summoner HP totals |
| `HandUI` | Cards in hand, playable state |

### Event Subscription (discrete)
For discrete HUD events, components subscribe to `IGameSession.SimEventsEmitted` directly:

| Component | Events |
|-----------|--------|
| `GameOverOverlay` | Game-over event |
| Casting overlay | SpellCastEvent (if applicable) |

EntityManager does NOT mediate HUD events — it only handles 3D battlefield visuals.

## What It Does NOT Do

- Go through EntityManager for any data
- Know whether the game is singleplayer or multiplayer
- Contain game logic

## Sub-Components

| Component | Role |
|-----------|------|
| `PhaseTimerDisplay` | Shows current phase name and countdown |
| `PlayerManaDisplay` | Shows mana bar and numeric value |
| `SummonerHPDisplay` | Shows summoner HP for both players |
| `HandUI` | Card hand display, drag-and-drop source |
| `GameOverOverlay` | Victory/defeat screen |

## Dependencies

| Direction | Component | Relationship |
|-----------|-----------|-------------|
| Reads | `IGameSession` | Polls `GetState()` + subscribes to `SimEventsEmitted` |
| Independent of | `EntityManager` | No dependency in either direction |

## Today

`GameUI` (283 lines) + state pushed manually by `GameController3D._process()` with different codepaths for host vs client. Self-polling eliminates that branching — each HUD component reads from `IGameSession` without knowing whether it's singleplayer or multiplayer.
