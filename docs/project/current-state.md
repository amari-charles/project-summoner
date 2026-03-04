# Fateforged - Current State

**Last Updated:** 2026-03-04
**Version:** Pre-Alpha (Host-Authoritative Simulation)

## Project Overview

Fateforged is a 1v1 real-time tactical battler where players summon elemental creatures to fight for them. Built in Godot 4.5 (C# + GDScript), players use cards to spawn units, cast spells, and deploy structures on a 3D battlefield with a 2.5D orthographic perspective.

---

## Architecture

The codebase uses a **hybrid architecture** — layer-based for battle, domain-based for meta-game. See [System Architecture](../architecture/system-architecture.md) and [Graph-Of-Graphs Model](../architecture/graph-of-graphs.md) for canonical boundaries.

### Battle Domain (4-Layer Stack)

```
Simulation  →  Session  →  View  →  Input
```

| Layer | Key Components | Language |
|---|---|---|
| **Simulation** (`Fateforged.Simulation`) | `Simulation`, `MatchState`, `DeterministicRng`, `SimAi`, Commands, Events | C# (pure, no Godot deps) |
| **Session** (`Fateforged.Session`) | `LocalSession`, `CommandRouter`, `IGameSession`, `NetworkSession`, `HostSession`, `ClientSession` | C# |
| **View** (`Fateforged.View`) | `BattleScene`, `EntityManager`, `UnitVisual`, `ProjectileVisual`, `SummonerVisual`, `SpawnPositionCalculator` | C# |
| **Input** (`Fateforged.Input`) | `InputCollector` | C# |

**Scene-tree bridge:** `SimulationNode` (C#) bridges Session+Simulation into the Godot scene tree. It owns `MatchState`, creates `LocalSession` by default, and swaps to `HostSession`/`ClientSession` in multiplayer via `ConfigureMultiplayerSession(...)`. Singleton via `SimulationNode.Current`.

### Meta Domain (Service-Based)

All C# autoloads with clean names (no CS suffix):

| Service | Autoload Name | Responsibility |
|---|---|---|
| EconomyService | `Economy` | Currency (gold, gems, campaign gold) |
| CardService | `CardService` | Card ownership, leveling, XP |
| DeckService | `Decks` | Deck CRUD, validation |
| SummonerProgressionService | `SummonerProgression` | Summoner XP, levels, traits |
| SummonerSelectionService | `SummonerSelection` | Active summoner selection |
| RewardService | `RewardService` | Reward generation |
| ItemService | `Items` | Item ownership, equipment |
| CampaignService | `Campaign` | Campaign progress, events, battles |

**GDScript exceptions:** `Shop` (substantial local logic + billing) and `ProfileRepo` (full GDScript persistence layer).

### Infrastructure

| Component | Responsibility |
|---|---|
| `CardCatalog` | Card definitions (read-only) |
| `SummonerCatalog` | Summoner definitions (read-only) |
| `TraitCatalog` | Trait definitions (read-only) |
| `ProjectileCatalog` | Projectile definitions (read-only) |
| `ProfileRepo` | Read/write player profile data (JSON) |

### Multiplayer (Networking)

| Component | Responsibility |
|---|---|
| `NakamaGameClient` | Server communication |
| `MatchmakingService` | Match queue, lobby |
| `RankingService` | Elo calculation, rank tracking |
| `NakamaMatchTransport` | Ranked relay transport in battle |
| `P2PTransport` | Peer-to-peer transport for local/direct matches |

---

## Key Systems

### Battle Flow
1. `BattleContext` (GDScript autoload) configures battle mode (campaign, practice, multiplayer, arena)
2. `BattleScene` (C# facade) orchestrates initialization: creates `SimulationNode`, `EntityManager`, loads decks, AI, UI
3. `SimulationNode` runs deterministic tick loop at 60 FPS fixed timestep
4. `EntityManager` spawns/despawns visual shells (`UnitVisual`, `ProjectileVisual`) in response to `SimEvent`s
5. `InputCollector` handles drag-drop card play, routes commands through `IGameSession.SubmitCommand()`

### Card System
- `CardCatalog` (C#) provides card definitions from JSON data
- `Card` (C# Resource) is the runtime card instance
- Cards support summon and spell types
- Formation system for multi-unit spawn positioning

### Summoner System
- `SummonerVisual` (C#) is the visual representation
- Summoner data lives in `MatchState.Summoners[]`
- Fixed mana pool, HP system, hand management
- Progression: XP, levels, traits, items

### AI System
- `SimAi` runs inside `Simulation.Tick()` as an internal producer
- Types: Heuristic, Simple, Scripted, None
- Configurable personality, difficulty, play intervals

### Campaign
- Multi-node campaign maps with battle events, caravans, shops
- Level cap system for difficulty scaling
- XP rewards for deck cards and summoners on victory

---

## Testing

```bash
# Headless (461 tests):
dotnet test --settings test.runsettings

# Full suite (including Godot-runtime tests):
# Run via Godot editor gdUnit4 panel
```

---

## Current Limitations

- No sound effects system (AudioManager autoload exists, content pending)
- AI uses basic heuristics (no ML or advanced strategy)
- See `docs/bugs.md` for known issues
- See `docs/todos.md` for development priorities
