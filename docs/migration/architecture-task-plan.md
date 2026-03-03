# Architecture Task Plan

> Post-migration audit: structural failures, duplicate systems, 1:1 port risks, and ordered remediation plan.

---

## A. Target Architecture Summary

**Source**: `docs/technical/architecture.md`, `docs/migration/README.md`, `docs/migration/layer-map.md`

### Backbone

C# is the backbone. It owns:

| Concern | Owner | Namespace |
|---------|-------|-----------|
| Simulation (tick, state, determinism) | `Simulation.cs`, `MatchState` | `Fateforged.Simulation` |
| Session (command routing, SP/MP abstraction) | `LocalSession`, `HostSession`, `ClientSession` | `Fateforged.Session` |
| View (visual shells, entity lifecycle) | `BattleScene`, `EntityManager`, `UnitVisual` | `Fateforged.View` |
| Input (gesture → command) | `InputCollector` | `Fateforged.Input` |
| Meta services (economy, cards, decks, etc.) | C# autoloads | `Fateforged.Meta.*` |

### Boundaries

```
Input → Session → Simulation (downward only)
                ↑
          View reads state (polls MatchState each frame)
```

- **Simulation**: Zero Godot imports. Pure C#. Deterministic via `DeterministicRng`.
- **Session**: Bridges SP/MP. Validates commands via `CommandRouter`. Owns `IGameSession` contract.
- **View**: Read-only. Never writes state. `EntityManager` diffs entity lists for spawn/destroy.
- **Input**: Produces `ICommand` objects. No state ownership.

### GDScript Role

GDScript should be **thin glue**:
- Scene/screen scripts (UI, menus, dialogs)
- Input capture → forward to C# `InputCollector`
- High-level scene transitions
- No domain state ownership
- No orchestration of game logic

---

## B. Autoload Reduction Plan

### Current Autoloads (46 total)

#### Keep As-Is (20) — Correct Architecture

| Autoload | Type | Reason |
|----------|------|--------|
| CardCatalog | C# | Source of truth for card definitions |
| SummonerCatalog | C# | Source of truth for summoner definitions |
| TraitCatalog | C# | Source of truth for trait definitions |
| ProjectileCatalog | C# | Source of truth for projectile paths |
| Economy | C# | Meta service — currency |
| Decks | C# | Meta service — deck management |
| SummonerProgression | C# | Meta service — summoner XP/levels |
| SummonerSelection | C# | Meta service — active summoner |
| CardServiceCS | C# | Meta service — card collection |
| RewardService | C# | Meta service — battle rewards |
| Items | C# | Meta service — equipment |
| Campaign | C# | Meta service — campaign state |
| LevelCapService | C# | Meta service — progression caps |
| NakamaGameClient | C# | Multiplayer backend |
| RankingService | C# | ELO rating |
| MatchReporter | C# | Match result reporting |
| MatchmakingService | C# | Player matchmaking |
| LeaderboardService | C# | Leaderboards |
| Loc | GDScript | Localization — no domain state |
| Fonts | GDScript | Font preloading — no domain state |

#### Keep But Evaluate Later (8) — Correct for Now

| Autoload | Type | Reason | Future |
|----------|------|--------|--------|
| SceneManager | GDScript | Scene path registry + transition API, 21 callers | Could become C# constants |
| SceneCoordinator | GDScript | Scene cleanup/init orchestration | Pure glue, no domain state |
| AudioManager | GDScript | Music/SFX — no domain state | Keep indefinitely |
| VFXManager | GDScript | VFX pooling — no domain state | Keep indefinitely |
| ElementTypes | GDScript | Element type registry | Could migrate to C# Data |
| PhysicsLayers | GDScript | Collision layer constants | Keep indefinitely |
| DevConsole | GDScript | Debug commands | Keep indefinitely |
| DebugMenu | GDScript | Debug UI | Keep indefinitely |

#### Convert / Absorb Into C# (9) — Architecture Violations

| Autoload | Type | Problem | Target |
|----------|------|---------|--------|
| **BattleContext** | GDScript | GDScript singleton owns battle config; C# reads via 22+ `Call()` invocations | Replace with typed `BattleSessionConfig` passed to `BattleScene.Initialize()` |
| **EventContext** | GDScript | Same pattern as BattleContext for campaign events | Replace with typed parameter to event screens |
| **ProfileRepo** | GDScript | Source-of-truth for all persistence; C# wraps it mechanically | Implement persistence in C# natively |
| **ProfileRepositoryCS** | C# | 548-line mechanical adapter; 42 `Call()` delegations to GDScript | Becomes the real implementation when ProfileRepo migrates |
| **ShopServiceCS** | C# | Core logic | Absorb GDScript Shop wrapper |
| **Shop** | GDScript | 635-line wrapper with catalog defs + purchase flow | Merge into ShopService.cs |
| **CapabilityManager** | GDScript | Permission system — isolated, generic, 3 callers | Migrate to C# (best candidate) |
| **EventSequencer** | GDScript | Event execution engine with mutable state | Evaluate for C# migration |
| **DialogueManager** | GDScript | Dialogue state + UI coordination | Evaluate for C# migration |

#### Delete (9) — Dead or Obsolete

| Autoload | Type | Reason |
|----------|------|--------|
| **GameStateEvents** | GDScript | Dead signal bus — no emitters, replaced by SimEvents |
| **NavigationContext** | GDScript | Scene return stack — evaluate if SceneCoordinator supersedes |
| **NetworkState** | GDScript | Minimal connection state display — keep only if MP UI needs it |
| **DebugSnapshots** | GDScript | Test utility — evaluate necessity |
| **CosmeticsCatalog** | GDScript | Should be C# Data catalog for consistency |
| **EmotesCatalog** | GDScript | Should be C# Data catalog for consistency |
| **BillingCatalog** | GDScript | Should be C# Data if/when billing is implemented |
| **PlatformBilling** | GDScript | Platform routing — keep only if billing is active |
| **card_trait_catalog** | GDScript | 230 LOC — overlaps with C# TraitCatalog; only 1 caller |

### Highest-Risk Global State

1. **BattleContext** — 22 C# `Call()` invocations, dict-based config with magic string keys, mixes configuration + runtime state + authority
2. **ProfileRepo** — Foundation for ALL meta services; bidirectional GDScript↔C# dependency via `Call()` pattern
3. **Shop** — Split ownership between GDScript catalog/billing and C# purchase logic

---

## C. Duplicate Stack Elimination Plan

| Feature | Old Location(s) | New Location(s) | Which Runs Today | Decision | Steps |
|---------|-----------------|-----------------|------------------|----------|-------|
| **Profile Persistence** | `scripts/data/json_profile_repository.gd` (560 LOC) + `scripts/data/profile_repository.gd` (250 LOC) | `scripts/csharp/Infrastructure/Persistence/ProfileRepository.cs` (548 LOC adapter) | **Both** — GDScript owns state, C# delegates via 42 `Call()` invocations | **Consolidate to C#** | 1. Port JSON persistence to C# 2. Migrate all GDScript callers to C# service APIs 3. Delete GDScript ProfileRepo |
| **Shop Service** | `scripts/services/shop_service.gd` (635 LOC) | `scripts/csharp/Services/Shop/ShopService.cs` (550 LOC) | **Both** — GDScript owns catalog/billing, C# owns purchase validation | **Merge into C#** | 1. Move catalog definitions to C# Data 2. Move billing integration to C# 3. Delete GDScript shop_service.gd |
| **Trait Catalogs** | `scripts/data/card_trait_catalog.gd` (230 LOC) | `scripts/csharp/Data/Traits/TraitCatalog.cs` | **Both** — GDScript for card traits, C# for summoner traits | **Merge into C# TraitCatalog** | 1. Add card trait definitions to C# TraitCatalog 2. Update card_detail_modal.gd to use C# bridge 3. Delete card_trait_catalog.gd |
| **Battle Config** | `scripts/core/battle_context.gd` (421 LOC) — dict-based | No C# equivalent exists | **GDScript only** — C# reads via `GetNode()` + `Call()` | **Replace with typed C# class** | 1. Create `BattleSessionConfig.cs` 2. Refactor BattleScene to accept config parameter 3. Make BattleContext a thin setter that creates the C# config |
| **Event Config** | `scripts/core/event_context.gd` (121 LOC) | No C# equivalent exists | **GDScript only** | **Replace with typed C# class** | 1. Create `EventSessionConfig.cs` 2. Refactor event screens to accept config parameter |
| **Game State Events** | `scripts/services/game_state_events.gd` (118 LOC) | `BattleScene.OnSimEventsEmitted()` + SimEvents | **GDScript exists but has zero emitters** — SimEvents replaced it | **Delete** | 1. Remove autoload 2. Delete file |
| **Cosmetics/Emotes** | `scripts/data/cosmetics_catalog.gd`, `scripts/data/emotes_catalog.gd` | No C# equivalent | **GDScript only** | **Migrate to C# Data** | Create C# catalog classes |

---

## D. Control Inversion Plan (C# Backbone)

### Current State (Problems)

```
┌─────────────────────────────────────────────────────────┐
│ GDScript World (STILL IN CONTROL)                       │
│                                                         │
│ SceneCoordinator → SceneManager → loads battle scene    │
│ BattleContext (autoload) ← holds config as dict         │
│                   ↓ GetNode() + Call()                   │
│         ┌─────────────────────┐                         │
│         │ BattleScene.cs      │                         │
│         │ (1,031 LOC)         │                         │
│         │ - reads BattleCtx   │                         │
│         │ - reads ProfileRepo │                         │
│         │ - reads Decks       │                         │
│         │ - loads decks       │ ← business logic in View│
│         │ - applies stats     │                         │
│         │ - manages authority │                         │
│         └────────┬────────────┘                         │
│                  ↓                                       │
│         SimulationNode → LocalSession → Simulation      │
└─────────────────────────────────────────────────────────┘
```

**Problems:**
1. BattleScene (View) does ~400 lines of deck loading, profile querying, and stat application
2. Configuration flows via untyped dicts through GDScript singletons
3. 22+ `Call()` invocations from C# to GDScript autoloads with magic string keys
4. No typed boundary between "configure battle" and "run battle"

### Target State

```
┌──────────────────────────────────────────────────────────┐
│                                                          │
│  GDScript (Thin)           C# Backbone                   │
│  ┌──────────────┐          ┌──────────────────────┐      │
│  │ UI Screen     │ ──────→ │ BattleSessionFactory  │     │
│  │ (click Play)  │         │ • load battle config   │    │
│  └──────────────┘          │ • load player deck     │    │
│                            │ • load summoner stats  │    │
│                            │ • apply level caps     │    │
│                            │ → BattleSessionConfig  │    │
│                            └────────┬───────────────┘    │
│                                     ↓                    │
│                            ┌──────────────────────┐      │
│                            │ BattleScene (thin)    │     │
│                            │ • receive config      │     │
│                            │ • create SimNode      │     │
│                            │ • create EntityMgr    │     │
│                            │ • bind UI signals     │     │
│                            └────────┬──────────────┘     │
│                                     ↓                    │
│                            ┌──────────────────────┐      │
│                            │ SimulationNode        │     │
│                            │ • owns LocalSession   │     │
│                            │ • drives tick          │    │
│                            │ • exposes IGameSession │    │
│                            └────────────────────────┘    │
│                                                          │
└──────────────────────────────────────────────────────────┘
```

### What Becomes the Single C# Root Authority

**`BattleSessionFactory`** (new) — responsible for assembling a fully-configured `BattleSessionConfig` before the battle scene loads. This replaces the current pattern where BattleScene reads from 5+ autoloads via `GetNode()`.

```csharp
namespace Fateforged.Session;

public class BattleSessionConfig
{
    public string BattleId { get; set; }
    public BattleMode Mode { get; set; }  // Campaign, Arena, Multiplayer, Practice
    public float PrepDuration { get; set; }
    public float MatchDuration { get; set; }
    public string WinCondition { get; set; }
    public long BattleSeed { get; set; }

    // Player
    public List<SimCardData> PlayerDeck { get; set; }
    public SummonerStats PlayerSummoner { get; set; }

    // Enemy
    public List<SimCardData> EnemyDeck { get; set; }
    public SummonerStats EnemySummoner { get; set; }
    public AiConfig AiConfig { get; set; }

    // Multiplayer
    public bool IsMultiplayer { get; set; }
    public bool HasAuthority { get; set; }

    // Post-battle
    public CardInstanceId[] DeckCardIds { get; set; }  // For XP rewards
    public Action<BattleResult> CompletionCallback { get; set; }
}
```

### How Session is Created

1. **Caller** (campaign_map.gd, arena.gd, etc.) calls `BattleSessionFactory.CreateConfig()` with battle ID
2. Factory queries Campaign, ProfileRepo, Decks, SummonerSelection — all in C#
3. Factory returns `BattleSessionConfig` (typed, validated)
4. Scene transition carries config to battle scene
5. `BattleScene._Ready()` receives config, creates `SimulationNode`, calls `Initialize(config)`
6. `SimulationNode` creates `LocalSession` (or `HostSession`/`ClientSession` for MP)

### How Simulation Tick is Driven

No change needed — current design is correct:
- `SimulationNode._PhysicsProcess()` runs at priority `-100`
- Fixed timestep accumulator (60 FPS)
- Calls `LocalSession.Tick(delta)` → `Simulation.Tick(delta)`
- Returns `List<SimEvent>` → emitted via `SimEventsEmitted` signal

### How View Sync Works

No change needed — current design is correct:
- `EntityManager` subscribes to `SimEventsEmitted` for discrete events (spawn, death, damage)
- `UnitVisual`, `ProjectileVisual`, `SummonerVisual` poll `IGameSession.GetState()` each frame
- `BattleScene` forwards sim events as Godot signals for GDScript UI consumers

### How GDScript Scenes Bind Without Owning State

1. **UI screens** receive typed config objects as parameters (not `GetNode()`)
2. **Battle HUD** connects to BattleScene Godot signals (`PhaseChanged`, `TimeUpdated`, `GameEnded`)
3. **Card hand UI** reads card data from `IGameSession.GetState().Hands[team]`
4. **No GDScript code** calls `Simulation.Tick()`, mutates `MatchState`, or validates commands

---

## E. Ordered Task List (Migration Sequence)

### Phase 1: Delete Dead Code + Consolidate Trivial Duplicates

#### Task 1.1: Delete GameStateEvents ✅

- **Status**: Complete
- **Goal**: Remove dead signal bus that has zero emitters

#### Task 1.2: Merge card_trait_catalog.gd into C# TraitCatalog ✅

- **Status**: Complete
- **Goal**: Eliminate GDScript trait catalog; single source of truth in C#

#### Task 1.3: Migrate CosmeticsCatalog + EmotesCatalog to C# Data — DEFERRED

- **Status**: Deferred — zero production callers (only test files reference them)
- **Goal**: Consolidate all catalogs under C# Data namespace
- **Rationale**: Both catalogs are placeholder content with zero production usage. Migration would reduce autoload count by 2 but has no architectural impact. Will migrate when these catalogs gain production callers.

---

### Phase 2: Introduce Typed Battle Configuration

#### Task 2.1: Create BattleSessionConfig ✅

- **Status**: Complete — `scripts/csharp/Session/BattleSessionConfig.cs` (140 LOC)
- **Goal**: Replace untyped dict-based battle configuration with typed C# class

#### Task 2.2: Create BattleSessionFactory ✅

- **Status**: Complete — `scripts/csharp/Session/BattleSessionFactory.cs` (362 LOC), BattleScene 916→644 LOC
- **Goal**: Centralize battle setup logic currently spread across BattleScene.cs

#### Task 2.3: Reduce BattleContext to Thin Setter — DEFERRED

- **Status**: Deferred — BattleSessionConfig.FromBattleContext() reads from GDScript autoload once at init. Full elimination of BattleContext requires updating all GDScript battle entry points (campaign_map, arena, online_screen, etc.)
- **Goal**: BattleContext becomes a temporary bridge that creates `BattleSessionConfig` from its dict
- **Risk**: Medium — all battle entry points must be updated

#### Task 2.4: Create EventSessionConfig — SKIPPED

- **Status**: Skipped — all 5 EventContext callers are GDScript UI screens with existing TypedEventData wrapper. Zero C# callers. Creating a C# config would add unnecessary interop complexity.

---

### Phase 3: Consolidate Profile Persistence

#### Phase 3A: Redirect GDScript Callers to C# Services ✅

- **Status**: Complete
- **What was done**:
  - Added bridge methods to SummonerSelectionService (IsSummonerUnlocked, SetStartingSummoner, GetSummonerInstanceDict, SaveSummonerInstanceDict)
  - Added DataChangedGodot signal + settings methods to ProfileRepository.cs
  - Redirected 10 summoner callers across 8 GDScript files from ProfileRepo → SummonerSelection
  - Redirected audio_manager settings from ProfileRepo → ProfileRepositoryCS
  - Simplified defensive has_method() checks in collection_screen and first_card_selection
- **Remaining ProfileRepo callers** (deferred):
  - Shop operations (get_resources, update_resources, is_cosmetic_owned, is_emote_owned, get_shop_refresh_state, etc.) → Phase 4
  - Profile lifecycle (get_active_profile, save_profile) → Phase 3B
  - data_changed signal connections → stays on GDScript ProfileRepo (signal chain flows through GDScript)

#### Phase 3B: Port JSON Persistence to C#

- **Status**: Not started
- **Goal**: Move profile read/write from GDScript to native C# implementation
- **Files**: `scripts/csharp/Infrastructure/Persistence/ProfileRepository.cs` (rewrite from adapter to real implementation), new `scripts/csharp/Infrastructure/Persistence/JsonProfileStore.cs`
- **Success criteria**: ProfileRepository.cs has zero `Call()` invocations; reads/writes JSON directly; all existing tests pass
- **Dependencies**: Phase 3A complete ✅
- **Risk**: High — ProfileRepo is foundational; ALL meta services depend on it

#### Phase 3C: Delete GDScript ProfileRepo ✅

- **Status**: Complete
- **Goal**: Remove GDScript persistence files and autoload
- **Files**: `scripts/data/json_profile_repository.gd` (deleted), `scripts/data/profile_repository.gd` (deleted), `tests/mocks/mock_profile_repo.gd` (deleted), `project.godot` (renamed autoload `ProfileRepositoryCS` → `ProfileRepo`, removed GDScript autoload)
- **Changes**: 13 GDScript callers updated to PascalCase C# methods, 2 test files updated, `audio_manager.gd` inlined `safe_float()`, `battle_context.gd` removed `_profile_repo` injectable
- **Success criteria**: `grep -r "ProfileRepositoryCS" --include="*.gd"` returns zero hits; `dotnet build` passes
- **Dependencies**: Phase 3B + all remaining callers migrated ✅
- **Risk**: Medium — ensure no transitive GDScript dependencies remain ✅

---

### Phase 4: Consolidate Shop Service

#### Task 4.1: Move Shop Catalog Definitions to C# Data

- **Goal**: Shop item definitions, caravan offerings, and pricing live in C#
- **Files**: `scripts/csharp/Data/Shop/` (new), extract from `scripts/services/shop_service.gd`
- **Success criteria**: C# ShopService reads catalog from C# Data; no catalog definitions in GDScript
- **Dependencies**: Phase 3 (ProfileRepo consolidated — Shop depends on it)
- **Risk**: Low — data migration

#### Task 4.2: Integrate Billing into C# ShopService

- **Goal**: Platform billing routing moves to C# (or becomes a thin C# interface with platform-specific implementations)
- **Files**: `scripts/csharp/Services/Shop/ShopService.cs` (extend), `scripts/billing/platform_billing.gd` (evaluate)
- **Success criteria**: Purchase flow is end-to-end in C#; GDScript billing files are deleted or reduced to platform stubs
- **Dependencies**: Task 4.1
- **Risk**: Medium — billing is platform-sensitive

#### Task 4.3: Delete GDScript Shop Wrapper

- **Goal**: Remove GDScript shop_service.gd and Shop autoload
- **Files**: `scripts/services/shop_service.gd` (delete), `project.godot` (remove Shop autoload)
- **Success criteria**: Single `ShopServiceCS` autoload (rename to `Shop`); `dotnet build` passes
- **Dependencies**: Task 4.2
- **Risk**: Low — all callers already use C# service

---

### Phase 5: Migrate Remaining GDScript Domain Services

#### Task 5.1: Migrate CapabilityManager to C#

- **Goal**: Permission system in C# — only 3 GDScript callers, isolated, generic
- **Files**: New `scripts/csharp/Services/Capabilities/CapabilityService.cs`, `scripts/services/capability_manager.gd` (delete)
- **Success criteria**: EventSequencer, DialogueManager, and any other callers use C# bridge; `dotnet test` passes
- **Dependencies**: None (can run in parallel with Phase 3-4)
- **Risk**: Low — isolated service

#### Task 5.2: Evaluate EventSequencer + DialogueManager Migration

- **Goal**: Determine if these should move to C# or remain as GDScript orchestration
- **Files**: `scripts/services/event_sequencer.gd` (350+ LOC), `scripts/services/dialogue_manager.gd` (250+ LOC)
- **Success criteria**: Decision documented; if migrating, create detailed sub-tasks
- **Dependencies**: Task 5.1 (CapabilityManager migrated — these depend on it)
- **Risk**: Medium — significant UI coordination; may be better as GDScript

---

### Phase 6: Autoload Cleanup + Final Architecture Verification

#### Task 6.1: Rename Remaining CS-Suffixed Autoloads

- **Goal**: Clean autoload names (drop CS suffixes where GDScript wrapper is gone)
- **Files**: `project.godot` (rename autoloads), update all GDScript callers
- **Success criteria**: No `*CS` suffixed autoloads remain; all callers updated
- **Dependencies**: Phases 3-4 complete

#### Task 6.2: Final Autoload Audit

- **Goal**: Verify all autoloads are necessary, properly categorized, and follow naming conventions
- **Files**: `project.godot`
- **Success criteria**: Autoload count reduced from 46 to ≤30; documented rationale for each
- **Dependencies**: All previous phases

#### Task 6.3: Update Architecture Documentation

- **Goal**: Reflect final state in docs
- **Files**: `docs/technical/architecture.md`, `docs/migration/README.md`
- **Success criteria**: Docs match reality; no stale references to deleted systems
- **Dependencies**: All previous phases

---

## Appendix: 1:1 Port Risk Register

| Component | Old Assumption | Architecture Conflict | Redesign |
|-----------|---------------|----------------------|----------|
| **ProfileRepository.cs** | "GDScript ProfileRepo is source of truth; C# is typed facade" | C# should own persistence; 42 `Call()` delegations are fragile | Implement `IProfileRepository` natively in C#; port JSON read/write |
| **BattleContext → BattleScene** | "Battle config lives in a GDScript singleton; C# reads via GetNode" | View layer shouldn't query 5+ autoloads for config; untyped dicts cross boundaries | Typed `BattleSessionConfig` assembled by `BattleSessionFactory` |
| **BattleScene.cs** (1,031 LOC) | "View layer assembles its own state from autoloads" | View should receive config, not construct it; 400+ LOC of deck/profile/stat logic belongs in Session | Extract to `BattleSessionFactory`; BattleScene drops to ~400 LOC |
| **Shop dual-stack** | "GDScript owns catalog + billing; C# owns validation" | Split ownership creates fragile callback injection and config duplication | Single C# `ShopService` with typed catalog data |
| **EventContext.gd** | "Event state is separate from battle state; lives in GDScript singleton" | Same problem as BattleContext — should be typed parameter, not GetNode lookup | Typed `EventSessionConfig` passed to event screens |
| **GameStateEvents.gd** | "Signal bus for battle events" | Replaced by SimEvents; zero emitters remain | Delete entirely |

## Appendix: Confirmed Architecture Status

### Who Owns What Today

| Concern | Owner | Language | Correct? |
|---------|-------|----------|----------|
| Simulation tick | SimulationNode._PhysicsProcess → LocalSession.Tick → Simulation.Tick | C# | YES |
| Match state | MatchState (owned by Simulation) | C# | YES |
| Command routing | CommandRouter.Validate() → MatchState.PendingCommandBuffer | C# | YES |
| Entity lifecycle | EntityManager (diffs MatchState) | C# | YES |
| Visual sync | UnitVisual/ProjectileVisual poll GetState() | C# | YES |
| Input collection | InputCollector.cs | C# | YES |
| Battle configuration | BattleContext.gd (dict-based) | GDScript | **NO — should be typed C#** |
| Event configuration | EventContext.gd | GDScript | **NO — should be typed C#** |
| Profile persistence | json_profile_repository.gd | GDScript | **NO — should be C#** |
| Shop catalog/billing | shop_service.gd + ShopService.cs | Both | **NO — should be consolidated** |
| Scene transitions | SceneCoordinator/SceneManager | GDScript | Acceptable (orchestration only) |
| Dialogue flow | DialogueManager | GDScript | Acceptable (UI orchestration) |
| Event sequences | EventSequencer | GDScript | Evaluate (owns mutable state) |

### Major Violations

1. **BattleScene.cs is a 1,031-line View class doing Session-layer work** — deck loading, profile queries, stat application, authority management
2. **22+ `Call()` invocations** from C# to GDScript BattleContext with magic string keys
3. **42 `Call()` delegations** in ProfileRepository.cs — pure mechanical adapter with zero logic
4. **Split Shop ownership** — catalog definitions in GDScript, purchase logic in C#, connected via callback injection
5. **No typed boundary** between "configure battle" and "run battle" — config is an untyped dict
