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

## Networking & Multiplayer

Transport and session networking live under `Battle/Session/` (they're session implementation details).
Matchmaking and ranking live under `Meta/` (they're persistent-state services like Economy or Campaign).
Backend connectivity lives under `Infrastructure/` (like ProfileRepository).

| Component | Location | Responsibility |
|---|---|---|
| **Transport/** | `Battle/Session/Transport/` | `IMatchTransport`, P2P, Nakama transport |
| **Protocol/** | `Battle/Session/Protocol/` | Message serialization, wire format |
| **Sync/** | `Battle/Session/Sync/` | Synchronization utilities |
| **Client/** | `Battle/Session/Client/` | State interpolation for remote play |
| **MatchmakingService** | `Meta/Matchmaking/` | Match queue, lobby (lobby-time service) |
| **RankingService** | `Meta/Ranking/` | Elo calculation, rank tracking (post-battle service) |
| **MatchReporter** | `Meta/Ranking/` | Post-match result reporting |
| **LeaderboardService** | `Meta/Ranking/` | Leaderboard queries |
| **NakamaGameClient** | `Infrastructure/Backend/` | Server communication (infrastructure) |

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

## Folder ↔ Namespace Map

### C# (`scripts/csharp/`)

```
scripts/csharp/
  Battle/
    Simulation/     → Fateforged.Simulation.*       Deterministic game logic
    Session/        → Fateforged.Session             Command routing, session types
      Transport/    → Fateforged.Multiplayer.*        Session networking (transport, protocol)
      Protocol/     → Fateforged.Multiplayer.*        Wire format
      Client/       → Fateforged.Multiplayer.*        State interpolation
    View/           → Fateforged.View.*              Visual representation
    Input/          → Fateforged.Input               Player input collection
  Meta/
    Services/       → Fateforged.Meta.*              Meta-game services (Economy, Cards, etc.)
    Matchmaking/    → Fateforged.Multiplayer.*        Match queue (lobby-time service)
    Ranking/        → Fateforged.Multiplayer.*        Elo, leaderboards (post-battle)
    Domain/         → Fateforged.Domain.Profile.*    Profile data model
  Infrastructure/
    Persistence/    → Fateforged.Infrastructure.*    ProfileRepository
    Backend/        → Fateforged.Multiplayer.*        NakamaGameClient (server connection)
    Data/           → Fateforged.Data.*, Cards.*     Game data catalogs
    Constants/      → Fateforged.Constants           Shared constants
  Debug/            → Fateforged.Debug               Performance counters
```

Note: Namespaces were NOT changed during the folder restructure. Some folders have namespaces
that don't match their folder path (e.g., `Meta/Ranking/` uses `Fateforged.Multiplayer.Ranking`).
This is intentional — the folder structure reflects architectural grouping while namespaces
preserve API compatibility.

### GDScript (`scripts/`)

```
scripts/
  battle/                           Battle domain (View + Input layer GDScript)
    animations/                       Unit animation configs and rig scripts
    battlefield/                      Battlefield setup, biome config, camera
    ui/                               Battle HUD (hand, stat bars, speed/pause buttons)
      debug/                          Debug spawner panel, debug buttons
    vfx/                              VFX manager, spell effects, shaders
    player_camera.gd                  Battle camera control
    battle_dialogue_controller.gd     In-battle dialogue sequencing
  meta/                             Meta-game domain
    screens/                          All meta-game screens (campaign, collection, shop, etc.)
    components/                       Meta-specific UI widgets (card_widget, offering_card, etc.)
      node_panels/                    Campaign map node detail panels
    modals/                           Meta modals (summoner reveal, card level up, etc.)
  shared/                           Reusable UI components (used by both battle and meta)
    card_visual.gd                    Card rendering
    styled_button.gd                  Styled button component
    dialogue_box.gd                   Dialogue display
    raised_panel.gd                   Panel styling
    ...
  application/                      Application layer (lifecycle, orchestration)
    scene_manager.gd                  Scene transitions
    scene_coordinator.gd              Cross-domain handoffs
    battle_context.gd                 Battle config singleton
    event_context.gd                  Event config singleton
    capability_manager.gd             Feature capability toggling
    event_sequencer.gd                Tutorial/event step sequencing
    dialogue_manager.gd               Dialogue orchestration
    navigation_context.gd             Navigation state
  infrastructure/                   Shared data, platform services, utilities
    data/                             ID constants, type enums, content bindings
    billing/                          Platform billing (IAP integration)
    dialogue/                         Dialogue data structures
    element_types.gd                  Element type definitions
    fonts.gd                          Font resource registry
    physics_layers.gd                 Physics layer constants
    audio_manager.gd                  Audio playback service
    ...
  debug/                            Debug tools (console, snapshots, menu)
  tools/                            Editor tooling (dialogue generator)
  csharp/                           C# codebase (see above)
```

The GDScript folder structure mirrors the same architectural layers as C#:
- **battle/** and **meta/** are peer domains
- **shared/** holds reusable components consumed by both domains
- **application/** orchestrates domain handoffs (depends on battle + meta)
- **infrastructure/** provides shared data and platform services (no domain dependencies)

## How to Add a New Service

1. **Determine the domain**: Does it manage persistent player state? → `Meta/Services/`. Does it handle battle logic? → `Battle/Simulation/`. Is it shared data? → `Infrastructure/Data/`. Is it networking for sessions? → `Battle/Session/Transport/`.

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
   MyFeature="*res://scripts/csharp/Meta/Services/MyFeature/MyFeatureService.tscn"
   ```

5. **Add a constant** in `scripts/infrastructure/csharp_autoloads.gd` (for optional `get_node_or_null()` access):
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
- **File location**: `scripts/csharp/Meta/Services/<Feature>/` for meta services
- **Methods**: PascalCase from both C# and GDScript
- **Signals**: PascalCase delegate naming (e.g., `CampaignGoldChangedEventHandler`)
