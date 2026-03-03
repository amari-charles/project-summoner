# Architecture Overview

## Top-Level Structure: Hybrid Domain/Layer Model

The codebase uses a **hybrid architecture** — layer-based for battle, domain-based for meta-game:

```
┌──────────────────────────────────────────────────────┐
│                    Application                        │
│   (SceneCoordinator, lifecycle, domain handoffs)      │
└──────┬─────────────┬────────────────┬────────────────┘
       │             │                │
  ┌────▼────┐  ┌─────▼─────┐  ┌──────▼──────┐
  │ Battle  │  │   Meta     │  │ Multiplayer │   (peer domains)
  │(layered)│  │ (services) │  │ (networking)│
  └────┬────┘  └─────┬─────┘  └──────┬──────┘
       │             │                │
  ┌────▼─────────────▼────────────────▼──────┐
  │             Infrastructure                │
  │  (ProfileRepository, NakamaClient,        │
  │   Catalogs, Platform billing)             │
  └──────────────────────────────────────────┘
```

## Why Hybrid?

**Layer-based for Battle** — The simulation is one cohesive unit. Units, summoners, projectiles, and combat all share deeply interconnected runtime state (MatchState). Splitting battle code by feature (e.g., all "unit" code together) would create circular cross-references between every feature's Simulation/ subfolder.

**Domain-based for Meta-game** — Meta services are mostly independent. Economy doesn't touch Decks, Shop doesn't touch Campaign directly. Each can be a self-contained vertical slice without cross-referencing.

## Battle Domain (Layer-Based)

Four layers with strict downward-only dependencies:

```
Simulation  →  Session  →  View  →  Input
```

| Layer | Namespace | Responsibility |
|---|---|---|
| **Simulation** | `Fateforged.Simulation` | Deterministic game logic. Tick-based, no Godot dependencies. Owns MatchState. |
| **Session** | `Fateforged.Session` | Command routing, session management (Local/Host/Client/Network). Bridges input to simulation. |
| **View** | `Fateforged.View` | Visual representation. EntityManager spawns UnitVisual/ProjectileVisual shells that read UnitData each frame. |
| **Input** | `Fateforged.Input` | Player input collection. InputCollector handles drag-drop, click targeting. |

### Key Patterns
- `SimulationNode.Current` provides the active simulation instance
- `IGameSession` abstracts Local vs Network play
- View layer reads state from `IGameSession.GetState()` — never writes to it
- `SimulationNode.Current.SimToLocal()` converts simulation coordinates to Godot 3D space

## Meta Domain (Service-Based)

All persistent player-state mutations live here. One domain, not artificially split.

| Service | Namespace | Responsibility |
|---|---|---|
| **EconomyService** | `Fateforged.Meta.Economy` | Currency balances (gold, gems, campaign gold) |
| **CardService** | `Fateforged.Meta.Cards` | Card ownership, leveling, progression |
| **DeckService** | `Fateforged.Meta.Deck` | Deck composition, validation, CRUD |
| **SummonerProgressionService** | `Fateforged.Meta.Summoner` | Summoner XP, levels, traits |
| **SummonerSelectionService** | `Fateforged.Meta.Summoner` | Active summoner selection |
| **RewardService** | `Fateforged.Meta.Rewards` | Reward generation and distribution |
| **ItemService** | `Fateforged.Meta.Items` | Item ownership, equipment |
| **ShopService** | `Fateforged.Meta.Shop` | Purchase orchestration, shop definitions |
| **CampaignService** | `Fateforged.Meta.Campaign` | Campaign progress, node unlocking, events |

### Autoload Registration

Meta services are registered as Godot autoloads in `project.godot`, accessible directly from GDScript by name:

```gdscript
# GDScript accesses C# autoloads directly — PascalCase methods
var gold: int = Economy.GetCampaignGold()
var info: Dictionary = SummonerProgression.GetSummonerProgressionInfo(id)
Campaign.CompleteBattle(battle_id)
```

### Note on GDScript Autoloads

All meta-game services are fully C#. No GDScript wrappers remain.

## Multiplayer Domain

| Service | Namespace | Responsibility |
|---|---|---|
| **NakamaGameClient** | `Fateforged.Multiplayer.Backend` | Server communication |
| **MatchmakingService** | `Fateforged.Multiplayer.Matchmaking` | Match queue, lobby |
| **RankingService** | `Fateforged.Multiplayer.Ranking` | Elo calculation, rank tracking |
| **MatchReporter** | `Fateforged.Multiplayer.Ranking` | Post-match result reporting |
| **LeaderboardService** | `Fateforged.Multiplayer.Ranking` | Leaderboard queries |

## Infrastructure (Shared)

Services and catalogs that all domains depend on. Never depends on domains.

| Component | Namespace | Responsibility |
|---|---|---|
| **ProfileRepository** | `Fateforged.Infrastructure.Persistence` | Read/write player profile data |
| **CardCatalog** | `Fateforged.Cards` | Card definitions (read-only) |
| **SummonerCatalog** | `Fateforged.Data.Summoners` | Summoner definitions (read-only) |
| **TraitCatalog** | `Fateforged.Data.Traits` | Trait definitions (read-only) |
| **ProjectileCatalog** | `Fateforged.Data.Projectiles` | Projectile definitions (read-only) |
| **BillingCatalog** | GDScript | Platform billing product catalog |
| **PlatformBilling** | GDScript | Platform-specific IAP integration |

## Communication Rules

1. **Domains depend on Infrastructure, never the reverse.** ProfileRepository doesn't know about EconomyService.
2. **Domains don't call each other directly.** No `EconomyService → CampaignService` calls.
3. **Cross-domain coordination** uses context objects (`BattleContext`, `EventContext`) mediated by the Application layer (`SceneCoordinator`).
4. **Shared identity** via catalog IDs — a `cardCatalogId` ties card data (catalog) → card ownership (CardService) → simulation spawn (Simulation) → visual display (View).

## Namespace Map

```
Fateforged.Simulation.*       — Deterministic game logic
Fateforged.Session            — Command routing, session types
Fateforged.View.*             — Visual representation
Fateforged.Input              — Player input collection
Fateforged.Meta.*             — Meta-game services (Economy, Cards, Deck, etc.)
Fateforged.Multiplayer.*      — Networking, matchmaking, ranking
Fateforged.Infrastructure.*   — Persistence layer
Fateforged.Cards.*            — Card definitions, configs, formations
Fateforged.Data.*             — Game data catalogs (summoners, traits, events, items, projectiles)
Fateforged.Domain.Profile.*   — Profile data model (resources, collection, decks, etc.)
Fateforged.Stats              — Stat calculation, modifiers
Fateforged.Constants          — Shared constants (battlefield bounds, element colors)
Fateforged.Combat             — Combat-related types (damage, spells)
```

## How to Add a New Service

1. **Determine the domain**: Does it manage persistent player state? → `Fateforged.Meta`. Does it handle multiplayer? → `Fateforged.Multiplayer`. Is it shared data? → `Fateforged.Data` or `Fateforged.Infrastructure`.

2. **Create the C# class** in the appropriate folder under `scripts/csharp/`:
   ```csharp
   namespace Fateforged.Meta.MyFeature;

   [GlobalClass]
   public partial class MyFeatureService : Node
   {
       public static MyFeatureService? Instance { get; private set; }

       public override void _Ready()
       {
           Instance = this;
       }
   }
   ```

3. **Create a .tscn wrapper** (required for C# autoloads in Godot):
   - Create a scene with a root Node
   - Attach the C# script to it

4. **Register in project.godot** under `[autoload]`:
   ```
   MyFeature="*res://scripts/csharp/Services/MyFeature/MyFeatureService.tscn"
   ```

5. **Add a constant** in `scripts/core/csharp_autoloads.gd` (for optional `get_node_or_null()` access):
   ```gdscript
   const MY_FEATURE: String = "/root/MyFeature"
   ```

6. **Access from GDScript** directly by autoload name:
   ```gdscript
   MyFeature.SomeMethod()
   MyFeature.SomeSignal.connect(_on_something)
   ```

### Naming Conventions

- **Autoload name**: Clean name without suffix (e.g., `Economy`, not `EconomyServiceCS`)
- **C# class**: `PascalCase` with `Service` suffix (e.g., `EconomyService`)
- **Namespace**: `Fateforged.<Domain>.<Feature>` (e.g., `Fateforged.Meta.Economy`)
- **File location**: `scripts/csharp/Services/<Feature>/` for meta services
- **Methods**: PascalCase from both C# and GDScript
- **Signals**: PascalCase delegate naming (e.g., `CampaignGoldChangedEventHandler`)
