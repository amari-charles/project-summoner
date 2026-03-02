# Meta-Game Service Migration Plan

Migration plan for services, domain objects, and GDScript facades that exist outside the battle loop. Covers ~47 C# service files (9,545 LOC), 22 domain files (~1,200 LOC), and 14 GDScript facades (4,123 LOC).

**Key finding: most meta-game services stay unchanged.** They don't interact with the simulation directly. Only 3 boundaries change (HPBarService retirement, battle launch path, battle completion path), plus a GameStateEvents replacement plan for Phase 8.

For the full layer map, see [layer-map.md](layer-map.md). For session design specs, see [../architecture/gameplay/session/design-specs.md](../architecture/gameplay/session/design-specs.md).

---

## 1. Service Assessment Table

One-line assessment per service. All 11 C# service items from the planning checklist.

| Service | Files | Assessment | Action |
|---------|-------|-----------|--------|
| CampaignService | `Services/Campaign/` (14 files) | Meta-game. No sim deps. Launches battles via BattleContext. | **Stays** |
| CardService | `Services/Cards/` (3 files) | Meta-game. Card ownership/progression. No sim deps. | **Stays** |
| DeckService | `Services/Deck/` (5 files) | Meta-game. Deck CRUD. No sim deps. | **Stays** |
| EconomyService | `Services/Economy/EconomyService.cs` | Meta-game. Currency management. No sim deps. | **Stays** |
| ItemService | `Services/Items/` (3 files) | Meta-game. Item ownership/equipment. No sim deps. | **Stays** |
| RewardService | `Services/Rewards/` (5 files) | Meta-game. Battle rewards. Called by BattleResultHandler post-battle (see §6). | **Stays** (caller changes) |
| ShopService | `Services/Shop/` (7 files) | Meta-game. Shop logic. No sim deps. | **Stays** |
| SummonerProgressionService | `Services/Summoner/SummonerProgressionService.cs` | Meta-game. Summoner leveling. No sim deps. | **Stays** |
| SummonerSelectionService | `Services/Summoner/SummonerSelectionService.cs` | Meta-game. Summoner selection. No sim deps. | **Stays** |
| HPBarService | `Services/HPBarService.cs` (autoload) | View-layer service. Retire as autoload. | **Retire** (see below) |
| LevelCapService | `Services/LevelCapService.cs` | Cross-cutting data lookup. Used by BattleConfig construction. | **Stays** |

### Service Interfaces

| Interface | Assessment | Action |
|-----------|-----------|--------|
| `IDamageSystem` | DamageSystem being deleted (Phase 7, Tier 1). Interface serves no purpose. | **Delete** with DamageSystem |
| `IModifierService` | ModifierService being deleted (Phase 7, Tier 1). Interface serves no purpose. | **Delete** with ModifierService |
| `ICardFactory` | CardFactory shrinks to stats-only utility (cross-cutting-plan.md §6). Review interface when CardFactory shrinks. | **Review later** |

### HPBarService Retirement

**Current:** `HPBarService` is a `Node` autoload. Battle code calls `HPBarService.create_bar_for_unit(unit)` to create floating HP bars. The service owns a pool of HP bar instances.

**Problem:** HPBarService as a global autoload creates a View-layer dependency that any code can reach. It also couples HP bar lifecycle to a singleton rather than to the entity it belongs to.

**Target:** Each visual shell (`UnitVisual`, `SummonerVisual`) creates and owns its own HP bar at construction. The HP bar reads health from `UnitData.Hp / UnitData.MaxHp` via the session's `MatchState`.

**Migration steps:**
1. `UnitVisual` creates its HP bar in `_Ready()` — positioned above the unit mesh
2. `SummonerVisual` creates its HP bar similarly
3. HP bars update each frame by reading their entity's data from `MatchState`
4. Remove `HPBarService` autoload from `project.godot`
5. Delete `HPBarService.cs` and `HPBarService.tscn`

**Pooling:** Not needed initially. <50 units on screen means <50 HP bars. If profiling shows HP bar creation is a bottleneck, add pooling inside `EntityManager` (not a global autoload).

---

## 2. Domain Layer

**Files:** `Domain/Profile/` (22 files) — `ProfileData`, `ProfileId`, Account types (7 files), `CampaignProgress`, Collection types (2 files), `Deck`, Inventory types (2 files), `ShopRefreshState`, `SummonerInstance`, Enum/ID types.

**Assessment: Pure C# domain objects. No Godot dependencies. No migration needed.**

These are data structures representing the player profile — campaign progress, card collection, deck configurations, inventory, shop state, owned summoners. They're consumed by meta-game services and persisted by the infrastructure layer.

| Concern | Status |
|---------|--------|
| Godot dependencies | None — pure C# classes and records |
| Simulation dependencies | None — domain objects don't reference sim types |
| Service dependencies | Services read/write domain objects; domain objects don't reference services |
| Persistence | `Infrastructure/Persistence/` (3 files: `IProfileRepository`, `ProfileRepository`, `DtoConverters`) — stays as infrastructure |

**Action: No changes needed.** Domain layer is already clean.

---

## 3. GDScript Service Facades

### C#-Wrapping Facades (7 files)

| Facade | Wraps | Action |
|--------|-------|--------|
| `campaign_service.gd` | `CampaignServiceCS` | **Stays** |
| `deck_service.gd` | `DeckServiceCS` | **Stays** |
| `economy_service.gd` | `EconomyServiceCS` | **Stays** |
| `item_service.gd` | `ItemServiceCS` | **Stays** |
| `reward_service.gd` | `RewardServiceCS` | **Stays** |
| `shop_service.gd` | `ShopServiceCS` | **Stays** |
| `summoner_progression_service.gd` | `SummonerProgressionCS` | **Stays** |

**Rationale:** GDScript UI screens (campaign map, shop, collection, deck editor, reward screen) call these facades to interact with C# services. The facades translate between GDScript Dictionary/Variant types and C# typed APIs. Removing them would require migrating all GDScript UI callers to C# — that's Phase 8 scope.

### GDScript-Native Services (3 files)

| Service | Role | Action |
|---------|------|--------|
| `DialogueManager` | Orchestrates dialogue sequences for campaign events | **Stays as GDScript** |
| `EventSequencer` | Drives campaign event step sequences | **Stays as GDScript** |
| `CapabilityManager` | Manages feature flags/capabilities | **Stays as GDScript** |

**Rationale:** These orchestrate GDScript UI flows. They don't wrap C# services — they're native GDScript services consumed by GDScript screens. No C# equivalent needed. They migrate to C# only if/when their UI consumers migrate (Phase 8).

### Infrastructure Services (3 files)

| Service | Role | Action |
|---------|------|--------|
| `SceneCoordinator` | Scene flow coordination | **Stays** — infrastructure |
| `SceneManager` | Scene transitions | **Stays** — infrastructure |
| `NavigationContext` | Navigation state between scenes | **Stays** — infrastructure |

**Rationale:** Infrastructure layer. Scene navigation is orthogonal to the four-layer architecture. No migration needed.

---

## 4. GameStateEvents Replacement Plan

**Current:** `GameStateEvents` (`scripts/services/game_state_events.gd`) is an autoload that broadcasts untyped signals for meta-game state changes: campaign progress, card unlocked, economy changed, summoner leveled, etc.

**Decision (Phase 1, Decision #15):** Keep for non-battle events. Battle events go through `SimEventsEmitted`. Revisit scope and typing in Phase 6.

### Replacement Design

**Rename:** `GameStateEvents` → `MetaGameEvents` to clarify scope. Battle events go through `SimEventsEmitted` on `IGameSession`; meta-game events go through `MetaGameEvents`.

**Replace untyped signals with typed events:**

Current (untyped):
```gdscript
signal card_unlocked(card_id)
signal economy_changed(currency, old_amount, new_amount)
signal summoner_leveled(summoner_id, new_level)
```

Target (typed C# events):
```csharp
public static class MetaGameEvents
{
    // Campaign
    public static event Action<CampaignProgressEvent> CampaignProgressed;
    public static event Action<string> CampaignCompleted;

    // Cards
    public static event Action<CardId> CardUnlocked;
    public static event Action<CardId, int> CardXPGained;

    // Economy
    public static event Action<CurrencyType, int, int> EconomyChanged;

    // Summoner
    public static event Action<SummonerId, int> SummonerLeveled;
    public static event Action<SummonerId> SummonerUnlocked;

    // Inventory
    public static event Action<ItemId> ItemAcquired;
    public static event Action<ItemId> ItemEquipped;
}
```

**Event categories:**
- **Campaign progress** — node completed, campaign started/ended, choice made
- **Card collection** — card unlocked, card XP gained, card upgraded
- **Economy** — currency added/spent, transaction completed
- **Summoner progression** — summoner leveled, trait unlocked, boon selected
- **Inventory** — item acquired, item equipped/unequipped
- **Shop** — purchase completed, shop refreshed

**Scoped listeners:** Services subscribe only to events they care about (e.g., RewardService listens to battle completion, not economy changes). This replaces the current pattern where all signals are on a single global autoload.

### Implementation Timeline

**Phase 8 execution.** The replacement requires GDScript UI consumers to either:
1. Call C# `MetaGameEvents` through a bridge (adds complexity), or
2. Migrate to C# themselves (eliminates the need for GDScript signals entirely)

Option 2 is cleaner — migrate UI to C#, then typed events are directly consumable. Until then, the GDScript `GameStateEvents` autoload continues to work.

### Interim: No Changes

`GameStateEvents` stays as-is during Phases 6-7. The rename to `MetaGameEvents` and typed event migration happens in Phase 8 when GDScript UI begins migrating to C#.

---

## 5. Battle Launch Path Change

### Current Flow

```
CampaignService → BattleContext.configure_campaign_battle(event_data)
               → BattleContext stores config as Dictionary properties
               → Scene transition to battle scene
               → GameController3D reads BattleContext properties at _ready()
               → GameController3D configures SimulationNode with extracted values
```

### Target Flow

```
CampaignService → BattleContext.configure_campaign_battle(event_data)
               → BattleContext internally builds typed BattleConfig
               → Scene transition to battle scene
               → BattleScene reads BattleConfig from BattleContext
               → BattleScene constructs IGameSession with BattleConfig
```

### What Changes

| Component | Change |
|-----------|--------|
| CampaignService | **No change** — still calls `BattleContext.configure_*()` |
| BattleContext | **Internal change** — builds `BattleConfig` instead of storing loose Dictionary properties |
| Battle scene | **Structural change** — `BattleScene` replaces `GameController3D`, reads `BattleConfig` |
| SimulationNode | **No change at launch** — session constructs Simulation, not the battle scene directly |

### Key Insight

Services don't change. Only `BattleContext`'s internal implementation changes (Dictionary storage → typed `BattleConfig`), and the battle scene's initialization changes (reads `BattleConfig` instead of loose properties).

The `BattleConfig` structure is defined in [session design-specs.md §5](../architecture/gameplay/session/design-specs.md#5-battleconfig--session-initialization).

---

## 6. Battle Completion Path Change

### Current Flow

```
Battle ends → BattleContext._handle_campaign_completion()
           → Calls RewardService.grant_rewards()
           → Calls CampaignService.complete_node()
           → Calls CardService.grant_xp()
           → Scene transition to reward screen
```

`BattleContext` currently orchestrates post-battle logic through completion callbacks that are set up during `configure_*_battle()`.

### Target Flow

```
Battle ends → IGameSession fires GamePhase.GameOver
           → BattleResultHandler detects GameOver
           → BattleResultHandler.HandleCompletion():
               → Calls RewardService.grant_rewards()
               → Calls CampaignService.complete_node()
               → Calls CardService.grant_xp()
               → Calls RankingService.report_match() (MP only)
               → Scene transition to reward screen / campaign map / lobby
```

### What Changes

| Component | Change |
|-----------|--------|
| RewardService | **No change** — same API, different caller |
| CampaignService | **No change** — same API, different caller |
| CardService | **No change** — same API, different caller |
| RankingService | **No change** — same API, different caller |
| BattleContext | **Loses** completion callbacks. Becomes pure config builder (§5). |
| BattleResultHandler | **New** — watches `IGameSession` for GameOver, orchestrates aftermath |

### BattleResultHandler Design

`BattleResultHandler` is a session-layer component (documented in [session design-specs.md §5](../architecture/gameplay/session/design-specs.md#5-battleconfig--session-initialization)). It:

1. Watches `IGameSession` for `GamePhase.Victory` or `GamePhase.Defeat`
2. Reads `BattleConfig.Mode` to determine which aftermath to run:
   - **Campaign:** Grant rewards, grant XP, advance campaign progress, transition to reward screen
   - **Multiplayer ranked:** Report match result, update Elo, transition to lobby
   - **Practice:** No rewards, transition to menu
3. Calls the same meta-game services that `BattleContext` calls today

### Key Insight

Services stay unchanged. Only the orchestration point moves from `BattleContext` completion callbacks to `BattleResultHandler`. This is cleaner because `BattleResultHandler` reacts to session state rather than relying on callbacks set up during configuration.

---

## 7. Billing

**Files:** `scripts/billing/` (5 files) — `billing_catalog.gd`, `billing_product.gd`, `billing_provider.gd`, `platform_billing.gd`, `stub_billing_provider.gd`.

**Assessment: Infrastructure layer. No migration needed.**

| File | Role |
|------|------|
| `billing_catalog.gd` (autoload) | Product catalog — defines purchasable items |
| `billing_product.gd` | Product data structure |
| `billing_provider.gd` | Abstract provider interface |
| `platform_billing.gd` (autoload) | Platform-specific billing integration |
| `stub_billing_provider.gd` | Stub implementation for development |

Billing is a platform integration concern. It doesn't interact with the simulation, session, or view layers. It connects to the economy service when purchases complete (via `EconomyService.add_currency()`).

**Action: No changes needed.**

---

## 8. Phase 8 Stub — Future: GDScript UI → C# Migration

Phase 8 is a future migration phase. It is **not blocked by** and **does not block** the current migration work (Phases 6-7).

### Scope

Migrate all GDScript UI screens to C#:
- Campaign map, collection screen, deck editor, shop screen, caravan screen
- Event screen, reward screen, summoner screens, settings
- Title screen, multiplayer lobby, online screen, premium store

### What It Enables

1. **Eliminate facade wrappers** — 7 GDScript service facades deleted. C# UI calls C# services directly.
2. **Consolidate to C#-only services** — No more bridge pattern for catalogs or services.
3. **Typed MetaGameEvents** — C# UI consumes typed C# events directly (§4 replacement).
4. **Retire GDScript data catalogs** — `card_catalog.gd`, `summoner_catalog.gd`, etc. replaced by C# catalogs.
5. **Retire GDScript constants** — `*_ids.gd` files replaced by C# enum/const references.
6. **Retire GDScript-native services** — `DialogueManager`, `EventSequencer`, `CapabilityManager` rewritten in C#.

### Prerequisites

- Phases 6-7 complete (meta-game services assessed, old battle systems deleted)
- Clear C# UI framework pattern established (how to build Godot UI in C#)
- One pilot screen migrated successfully to validate the approach

### Planning

Phase 8 planning will happen after Phase 7 execution begins. It will produce:
- Screen-by-screen migration order (dependency-based)
- Facade elimination checklist
- Catalog consolidation plan
- MetaGameEvents implementation plan

---

## Summary

| Category | Count | Action |
|----------|-------|--------|
| C# services that stay as-is | 9 of 11 | No changes |
| C# services that change | 1 (RewardService caller changes) | Caller moves to BattleResultHandler |
| C# services that retire | 1 (HPBarService) | Shell-owned HP bars |
| Service interfaces to delete | 2 (IDamageSystem, IModifierService) | Delete with their implementations |
| Domain layer | 22 files | No changes |
| GDScript facades (C#-wrapping) | 7 | Stay until Phase 8 |
| GDScript-native services | 3 | Stay as GDScript |
| Infrastructure services | 3 | Stay |
| Billing | 5 files | No changes |
| GameStateEvents | 1 | Rename + type in Phase 8 |
| Battle launch path | BattleContext internal | BattleConfig replaces loose properties |
| Battle completion path | BattleContext callbacks | BattleResultHandler replaces callbacks |
