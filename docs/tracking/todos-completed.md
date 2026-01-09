# Completed TODOs Archive

This document archives TODOs that have been completed. For active tasks, see [todos.md](todos.md).

---

## Architecture

### HP Bar Lifecycle Fix - GDScript to C# Migration
**Completed:** 2026-01-08
**Category:** Architecture / UI
**Effort:** Medium

**Problem:**
HP bars were not properly cleaned up when units died, particularly for multi-unit cards (Fire Ant Swarm). The cleanup relied on `UnregisterFromExternalSystems()` being called before the unit was freed, which failed in rapid-death or scene-unload scenarios.

**Solution Implemented:**
- Migrated HP bar system from GDScript to C#
- Created `HPBarService.cs` (pooling, lifecycle management)
- Created `FloatingHPBar.cs` (bar logic, rendering)
- Connected to unit's `TreeExiting` signal for guaranteed auto-cleanup
- Direct C# integration (no cross-language `Call()` needed)

**Key Fix:**
```csharp
unit.TreeExiting += OnUnitExiting;  // Fires BEFORE unit is freed
```

**Files Changed:**
- Created: `scripts/csharp/Services/HPBarService.cs`, `HPBarService.tscn`
- Created: `scripts/csharp/UI/FloatingHPBar.cs`
- Modified: `Unit3D.cs`, `summoner.gd`, `game_controller_3d.gd`, `project.godot`
- Deleted: `hp_bar_manager.gd`, `floating_hp_bar.gd`

**Architecture Document:** See `docs/architecture/issues/resolved/hp-bar-lifecycle.md`

---

### DRY Principle Audit - Formation Logic Unified
**Completed:** 2026-01-06
**Category:** Architecture / Code Quality
**Effort:** Medium

**Description:**
Performed comprehensive audit of formation logic duplication and unified into a single source of truth.

**Problem Identified:**
Formation logic was duplicated across 4+ files:
- C# CardFactory.cs
- GDScript Card.gd
- C# FormationHelper.cs (redundant)
- battlefield_drop_zone.gd

**Solution Implemented:**
- Created `SpawnOrchestrator.cs` as the single source of truth for formation positioning
- Deleted redundant `FormationHelper.cs`
- Updated `Card.gd` to delegate to SpawnOrchestrator
- Unified spawn preview and actual spawning to use same formation logic

**Related Bug Fixed:** "Spawn Preview and Actual Spawning Use Separate Formation Systems" (see bugs-resolved.md)

**Architecture Document:** See `docs/architecture/system-architecture.md` for current architecture

---

### CardProgressionService Removal
**Completed:** 2026-01-06
**Category:** Architecture / Services
**Effort:** Small

**Description:**
Removed the deprecated GDScript CardProgressionService, completing the migration to the C# PlayerCardService.

**Solution Implemented:**
- Updated all callers to use only PlayerCardService (no fallback)
- Removed CardProgression autoload from project.godot
- Deleted `scripts/services/card_progression_service.gd`

**Files Updated:**
- `scripts/cards/card.gd` - Removed fallback
- `scripts/core/battle_context.gd` - Removed fallback
- `scripts/ui/screens/collection_screen.gd` - Removed fallback
- `scripts/ui/modals/card_level_up_panel.gd` - Removed fallback (3 places)
- `scripts/ui/modals/card_detail_modal.gd` - Removed fallback (3 places)
- `project.godot` - Removed CardProgression autoload

---

### C# Modifier System Migration
**Completed:** 2026-01-06
**Category:** Architecture / Systems
**Effort:** Medium

**Description:**
Migrated the modifier system from GDScript to C# following the "C# = Systems & Mechanics" principle.

**Solution Implemented:**
- Created `ModifierService.cs` as central C# service (autoload)
- Created `StatModifier.cs` with typed modifier class
- Created `IModifierProvider.cs` interface
- Created `CardModifierProvider.cs` and `SummonerModifierProvider.cs` in C#
- Added factory methods for GDScript interop (`register_summoner_provider`, `register_card_provider`)
- Deleted deprecated GDScript files: `modifier_system.gd`, `card_modifier_provider.gd`, `summoner_modifier_provider.gd`

**Related Files:**
- `scripts/csharp/Systems/Modifiers/ModifierService.cs`
- `scripts/csharp/Systems/Modifiers/StatModifier.cs`
- `scripts/csharp/Systems/Modifiers/IModifierProvider.cs`

---

### Service Interfaces for Dependency Injection
**Completed:** 2026-01-06
**Category:** Architecture / Testing
**Effort:** Medium

**Description:**
Created service interfaces to enable future dependency injection and unit testing.

**Solution Implemented:**
- Created `ICardFactory.cs` interface
- Created `IModifierService.cs` interface
- Created `IPlayerCardService.cs` interface
- Created `IDamageSystem.cs` interface
- Updated all services to implement their respective interfaces
- All interfaces use snake_case for GDScript-compatible method names

**Related Files:**
- `scripts/csharp/Services/Interfaces/`

---

## Card & Spell System

### C# SummonCard Infrastructure
**Completed:** 2026-01-04
**Category:** Cards / Architecture
**Effort:** Medium

**Description:**
Ported summon card logic from GDScript to C# with pluggable formation strategies. All summons now execute via C# `CardFactory`. GDScript `Card.gd` reduced from ~463 to 265 lines.

**Architecture:**
```
SummonCard
├── SpawnConfig (scene path, count, summon time)
└── IFormationStrategy (pluggable: Grid, Ring, Line)
```

**Solution Implemented:**
- `SpawnConfig.cs` - Unit scene path, spawn count, summon time
- `IFormationStrategy.cs` - Interface for formation positioning
- `GridFormation.cs` - Default 2-row staggered formation (ported from GDScript)
- `RingFormation.cs` - Circular formation around spawn point
- `LineFormation.cs` - Horizontal line formation
- `SummonBuilder.cs` - Maps catalog IDs to formation strategies
- `SummonCard.cs` - Card type composing SpawnConfig + IFormationStrategy
- Renamed `SpellCardFactory.cs` → `CardFactory.cs` with unified spell/summon API
- `CardCatalog` sets `_csharp_summon_id` on summon cards
- `Card._summon_unit_3d()` delegates to C# `CardFactory.execute_summon()`
- Removed all GDScript summon logic (unit spawning, modifier integration, safe positioning)

**Related Files:**
- `scripts/csharp/Cards/CardFactory.cs` - Bridge autoload (spells + summons)
- `scripts/csharp/Cards/Formations/` - Formation strategies
- `scripts/csharp/Cards/SummonCard.cs` - Summon card type
- `scripts/cards/card.gd` - Delegation only (265 lines)

---

### C# Spell Effect System - Integration
**Completed:** 2026-01-04
**Category:** Cards / Architecture
**Effort:** Medium

**Description:**
Implemented a C# spell effect system with composition pattern. All spells now execute via C# `CardFactory`. GDScript `Card.gd` reduced from ~966 to 422 lines.

**Solution Implemented (Phase A - C# Foundation):**
- Core interfaces: `ISpellEffect`, `ITargetingStrategy`, `ISpellCondition`, `ITargetFilter`
- Base classes: `SpellEffect`, `SpellContext`, `Affinity` enum
- Concrete effects: `DamageEffect`, `CommandEffect` (Rally/Guard/Charge), `CompositeEffect`, `ConditionalEffect`
- Targeting: `CircleTargeting`
- Conditions: `HPThresholdCondition`
- Card classes: `Card` (abstract), `SpellCard`, `CardConfig`, `SpellCardConfig`
- Factory: `SpellBuilder` with Fireball, Rally, Guard, Charge

**Solution Implemented (Phase B - GDScript→C# Bridge):**
- Created `CardFactory.cs` autoload with `has_effect()` and `execute_spell()`
- `CardCatalog` sets `_csharp_spell_id` on spell cards
- `Card._cast_spell_3d()` delegates to C# `CardFactory`
- Removed all GDScript spell logic (VFX helpers, command spells, AOE damage, projectiles)
- Verified working in editor with all 4 spells (Fireball, Rally, Guard, Charge)

**Related Files:**
- `scripts/csharp/Cards/CardFactory.cs` - Bridge autoload
- `scripts/csharp/Cards/SpellBuilder.cs` - Effect factory
- `scripts/cards/card.gd` - Delegation only, all execution in C#
- `scripts/data/card_catalog.gd` - Sets `_csharp_spell_id` for spell cards

---

## Summoner System

### Implement Summoner Unlock System
**Completed:** 2025-12-23
**Category:** Summoners / Progression
**Effort:** Medium

**Description:**
Implemented the system for unlocking additional summoners beyond the starting summoner.

**Solution Implemented:**
- Premium Store UI with Summoners tab
- ShopOffering SUMMONER type with pricing (750 gold each)
- Purchase limits (1 per account per summoner)
- RewardService summoner unlock granting
- ProfileRepo unlock/instance tracking (`unlock_summoner()`, `is_summoner_unlocked()`, `get_unlocked_summoners()`)
- Shop "already owned" validation
- Purchasable summoners in catalog: Lightning Adept, Verdant Sage, Void Walker
- Dev console commands: `/unlock_summoner`, `/unlock_all_summoners`
- SummonerSwitchScreen shows unlocked summoners

**Related Files:**
- `scripts/services/shop_service.gd` - Summoner offerings with pricing
- `scripts/data/summoner_catalog.gd` - Purchasable summoner configs
- `scripts/data/json_profile_repository.gd` - Unlock tracking
- `scripts/services/reward_service.gd` - Unlock granting
- `scripts/ui/screens/premium_store_screen.gd` - Shop UI
- `scripts/debug/dev_console.gd` - Dev unlock commands

---

### Standardize "Hero" vs "Summoner" Language
**Completed:** 2025-11-28
**Category:** Summoners / Architecture
**Effort:** Medium

**Description:**
The codebase inconsistently used "Summoner" and "Hero" to refer to the same concept (the player character). Standardized to "Summoner" throughout codebase, docs, and UI.

**Solution Implemented:**
- Renamed all `Hero*` classes to `Summoner*` (HeroConfig→SummonerConfig, HeroInstance→SummonerInstance, etc.)
- Updated all services: HeroCatalog→SummonerCatalog, HeroSelection→SummonerSelection, HeroProgression→SummonerProgression
- Updated UI components: HeroManagementPanel→SummonerManagementPanel, HeroIconWidget→SummonerIconWidget, etc.
- Updated all scenes (.tscn) with new script paths and node names
- Updated localization keys in en.json (hero→summoner)
- Updated documentation in docs/features/summoners/
- Created SummonerIDs class for type-safe summoner references

**Files Changed:**
- Renamed: `scripts/core/hero_*.gd` → `scripts/core/summoner_*.gd`
- Renamed: `scripts/services/hero_*.gd` → `scripts/services/summoner_*.gd`
- Renamed: `scripts/ui/hero_*.gd` → `scripts/ui/summoner_*.gd`
- Renamed: `scenes/ui/hero_*.tscn` → `scenes/ui/summoner_*.tscn`
- Updated: `project.godot` autoloads
- Updated: `localization/data/en.json`
- Updated: `docs/features/summoners/*`

---

## Units & Combat

### Add Flying Unit Type
**Completed:** 2025-12-23
**Category:** Units & Combat
**Effort:** Medium

**Description:**
Created flying unit type that can move over obstacles and other units.

**Solution Implemented:**
- Added `MovementLayer` enum (GROUND, AIR) to Unit3D
- Added `TargetLayer` enum (GROUND_ONLY, AIR_ONLY, BOTH) for targeting rules
- Implemented `flight_altitude` export variable for visual height
- Shadow scaling based on altitude (smaller/fainter shadows at higher altitudes)
- Demon Imp card uses AIR movement layer as first flying unit
- Targeting system respects ground vs air layers

**Related Files:**
- `scripts/units/unit_3d.gd` - MovementLayer, TargetLayer enums, flight constants
- `scenes/units/demon_imp_3d.tscn` - Flying unit with movement_layer=1 (AIR)
- `scripts/data/card_catalog.gd` - Demon Imp card definition

---

### Implement Flying Movement Logic
**Completed:** 2025-12-23
**Category:** Units & Combat
**Effort:** Medium
**Dependencies:** Add Flying Unit Type

**Description:**
Implemented movement system for flying units including pathfinding and collision rules.

**Solution Implemented:**
- Flying units set position.y to flight_altitude on spawn
- Shadow scaling: size and opacity reduce with altitude
- Height tolerance for attacks: flying units ignore height differences when attacking
- Collision layers: FLYING_UNITS (layer 2) separate from ground units
- Targeting respects can_target (GROUND_ONLY, AIR_ONLY, BOTH)
- Flying units skip ground-based separation forces

**Related Files:**
- `scripts/units/unit_3d.gd` - Flying movement logic in _ready() and targeting
- `scripts/core/physics_layers.gd` - FLYING_UNITS layer constant

---

### Spatial Partitioning for Unit Targeting
**Completed:** 2025-12-12
**Category:** Units & Combat / Performance
**Effort:** Medium-Large

**Description:**
Replaced O(n²) unit targeting/separation queries with O(k) spatial grid queries for better performance with high unit counts.

**Solution Implemented:**
- Created `SpatialGrid` autoload with 10×10 unit cells (80 cells for 100×80 battlefield)
- Units register on spawn, unregister on death
- Position updates use 2.0 unit threshold to avoid per-frame cell updates
- Replaced 4 O(n²) methods in Unit3D:
  - `_acquire_target()` - enemy targeting
  - `_calculate_separation_force()` - collision avoidance
  - `_calculate_flank_direction_scores()` - flanking direction choice
  - `_correct_overlaps()` - post-movement overlap correction
- Debug visualization toggleable with F11 (grid lines, cell populations, stats)

**Files Changed:**
- New: `scripts/spatial/spatial_grid.gd`
- Modified: `scripts/units/unit_3d.gd`
- Modified: `project.godot` (autoload registration)

**Performance Impact:**
- 30 units: ~900 → ~60 checks/frame (~15x improvement)
- 50 units: ~2500 → ~100 checks/frame (~25x improvement)
- 100 units: ~10000 → ~200 checks/frame (~50x improvement)

---

### Lane-Based Unit Movement
**Completed:** 2025-11-29
**Category:** Units & Combat
**Effort:** Medium

**Description:**
Implemented lane-based movement where units march forward along the X-axis instead of pathfinding directly to the enemy base. Units only engage enemies that enter their attack range.

**Solution Implemented:**
- Units march forward in their lane (along X-axis) rather than pathing to base
- Lane-based targeting: units only consider enemies within their current lane (Z-axis tolerance)
- Turn zone system: units resume normal targeting when near enemy base
- Constants: `PLAYER_TURN_ZONE_X`, `ENEMY_TURN_ZONE_X`, `LANE_WIDTH_MULTIPLIER`
- New method `_move_forward_in_lane()` for lane marching behavior
- New method `_is_in_turn_zone()` to detect when near enemy base

**Behavior:**
1. Spawn → march forward in lane (X-axis movement only)
2. Enemy enters attack range → engage and attack
3. Enter turn zone near enemy base → resume normal target-based pathing
4. After killing target → resume lane marching (unless in turn zone)

**Related Files:**
- `scripts/units/unit_3d.gd`

---

### Prevent Units from Stacking on Same Coordinates
**Completed:** 2025-11-25
**Category:** Units & Combat
**Effort:** Small

**Description:**
Added collision/placement validation to prevent multiple units from occupying the same grid position.

**Solution Implemented:**
- Check for existing unit before placement
- Block movement to occupied tiles
- Handle edge cases (unit death, teleportation)
- Works for both player and AI units

---

## Database & Data Layer

### Consolidate Dual Catalog System (CardCatalog vs ContentCatalog)
**Completed:** 2025-11-25
**Category:** Database / Architecture
**Effort:** Medium

**Description:**
The codebase had TWO card catalog systems with incompatible data formats - `CardCatalog` (hardcoded GDScript, 21+ cards) and `ContentCatalog` (JSON-based, 4 cards). This created confusion and potential bugs due to type mismatches (`card_type: int` vs `card_type: String`).

**Solution Implemented:**
- Kept `CardCatalog` as the single source of truth for card data (it has all the cards)
- Removed card and unit loading from `ContentCatalog` (unused functionality)
- Deleted unused data classes: `CardData`, `UnitData`
- Deleted unused JSON content: `data/cards/`, `data/units/`
- Kept `ContentCatalog` for projectile data only (actively used by projectile system)
- `ContentCatalog` is now a focused "ProjectileCatalog" in function

**Files Changed:**
- `scripts/data/content_catalog.gd` - Removed card/unit loading, simplified to projectiles only
- Deleted: `scripts/data/card_data.gd`, `scripts/data/unit_data.gd`
- Deleted: `data/cards/*.json`, `data/units/*.json`

---

### Fix Services Using Dynamic call() Instead of Typed Access
**Completed:** 2025-11-25
**Category:** Database / Code Quality
**Effort:** Small

**Description:**
Domain services used `has_method()` + `call()` pattern instead of direct typed method calls, defeating the purpose of having a typed interface.

**Solution Implemented:**
Updated EconomyService, CollectionService, DeckService, and CampaignService to use direct `ProfileRepo.method()` calls instead of dynamic `call()` pattern. ShopService was already correct and served as reference.

**Related Files:**
- `scripts/services/economy_service.gd`
- `scripts/services/collection_service.gd`
- `scripts/services/deck_service.gd`
- `scripts/services/campaign_service.gd`

---

### Add CampaignProgress Methods to ProfileRepo
**Completed:** 2025-11-25
**Category:** Database / Architecture
**Effort:** Small

**Description:**
CampaignService was bypassing the service layer and directly mutating `profile["campaign_progress"]`, violating the repository pattern.

**Solution Implemented:**
Added `get_campaign_progress()` and `update_campaign_progress()` methods to both IProfileRepo interface and JsonProfileRepository implementation. Updated CampaignService to use these new methods.

**Related Files:**
- `scripts/data/profile_repository.gd`
- `scripts/data/json_profile_repository.gd`
- `scripts/services/campaign_service.gd`

---

### Fix JsonProfileRepository Not Extending IProfileRepo Interface
**Completed:** 2025-11-25
**Category:** Database / Architecture
**Effort:** Small

**Description:**
`JsonProfileRepository` extended `Node` instead of `IProfileRepo`, making the interface unused and unenforceable.

**Solution Implemented:**
Changed `JsonProfileRepository` to `extends IProfileRepo`. The interface methods are now properly inherited and enforced.

**Related Files:**
- `scripts/data/json_profile_repository.gd`

---

### Add Cascade Delete When Removing Cards from Collection
**Completed:** 2025-11-25
**Category:** Database / Data Integrity
**Effort:** Small

**Description:**
When a card was removed from collection, it wasn't automatically removed from decks, leaving orphaned references.

**Solution Implemented:**
Added cascade delete logic to `Collection.remove_card()` in collection_service.gd. After successfully removing a card from the collection, iterates through all decks and calls `Decks.clean_deck()` to remove any orphaned card references.

**Related Files:**
- `scripts/services/collection_service.gd`

---

### Localize HeroCatalog Names
**Completed:** 2025-11-25
**Category:** Database / Localization
**Effort:** Small

**Description:**
HeroCatalog stored hardcoded English strings for hero names and descriptions instead of using the localization system.

**Solution Implemented:**
Replaced all hardcoded `hero_name` and `description` strings with `Loc.t()` calls:
- `hero_fire.hero_name = Loc.t("hero.hero_fire.name")`
- `hero_fire.description = Loc.t("hero.hero_fire.description")`
- Same pattern for all 5 heroes (fire, water, wind, earth, shadow_initiate)

**Related Files:**
- `scripts/data/hero_catalog.gd`

---

### RarityIDs Constants Class
**Completed:** 2025-11-25
**Category:** Database / Code Quality
**Effort:** Small

**Description:**
Rarity strings ("common", "rare", "epic", "legendary") were used as magic strings throughout the codebase.

**Solution Implemented:**
Created `scripts/data/rarity_ids.gd` with:
- StringName constants: `COMMON`, `RARE`, `EPIC`, `LEGENDARY`
- `ALL_RARITIES` array for iteration
- `get_tier()` method to get rarity index
- `is_valid()` method for validation

Updated all usages in:
- `scripts/services/collection_service.gd` - match statements and default values
- `scripts/services/campaign_service.gd` - reward card definitions
- `resources/visual/color_palette.gd` - rarity color lookup
- `scripts/debug/dev_console.gd` - test data

**Related Files:**
- `scripts/data/rarity_ids.gd` (new)
- `scripts/services/collection_service.gd`
- `scripts/services/campaign_service.gd`
- `resources/visual/color_palette.gd`
- `scripts/debug/dev_console.gd`

---

## Core Game Systems

### Extract Magic Numbers in Hero System to Constants
**Completed:** 2025-11-25
**Category:** Core Game Systems / Code Quality
**Effort:** Small

**Description:**
Default stat values in the hero system were hardcoded without named constants, making them harder to maintain and tune.

**Solution Implemented:**
Added class-level constants to HeroConfig:
- `DEFAULT_BASE_HEALTH: float = 1000.0`
- `DEFAULT_MAX_MANA: float = 10.0`
- `DEFAULT_MANA_REGEN: float = 1.0`

Updated `@export` defaults and `from_dict()` fallbacks to use these constants.

**Related Files:**
- `scripts/core/hero_config.gd`

---

### CardTypeIDs / Card.CardType Enum Usage
**Completed:** 2025-11-25
**Category:** Core Game Systems / Code Quality
**Effort:** Small

**Description:**
CardCatalog used magic numbers `0` and `1` for card types instead of the `Card.CardType` enum, risking silent breakage if enum order changed.

**Solution Implemented:**
Replaced all `"card_type": 0` with `Card.CardType.SUMMON` and `"card_type": 1` with `Card.CardType.SPELL` throughout card_catalog.gd. Also updated comparison logic in `create_card_resource()` and `print_catalog_summary()`.

**Related Files:**
- `scripts/data/card_catalog.gd`

---

### Audit Codebase for Magic Strings - Replace with Constants/Enums
**Completed:** 2025-11-25
**Category:** Core Game Systems / Code Quality
**Effort:** Medium

**Description:**
Comprehensive audit to replace hardcoded string literals with type-safe constants throughout the codebase. This improves maintainability, catches typos at compile time, and provides better IDE autocomplete support.

**Solution Implemented:**
Created 11 constants classes with StringName constants:

1. **CardIDs** - 18 card catalog ID constants
2. **ProjectileIDs** - FIREBALL, ARROW, EMBER constants
3. **VFXIDs** - 7 VFX effect name constants
4. **RarityIDs** - COMMON, RARE, EPIC, LEGENDARY
5. **BiomeIDs** - SUMMER_PLAINS
6. **BattleIDs** - 5 battle/event ID constants
7. **GroupIDs** - 15+ Godot group name constants
8. **EventTypeIDs** - BATTLE, AFFINITY, FIRST_SUMMON, CARAVAN, ONBOARDING
9. **RewardTypeIDs** - FIXED, RANDOM, CHOICE, NONE
10. **UnitTypeIDs** - MELEE, RANGED, STRUCTURE
11. **ElementNameIDs** - 15 element name string constants

Updated 30+ files to use these constants instead of magic strings.

**Related Files:**
- `scripts/data/card_ids.gd`
- `scripts/data/projectile_ids.gd`
- `scripts/data/vfx_ids.gd`
- `scripts/data/rarity_ids.gd`
- `scripts/data/biome_ids.gd`
- `scripts/data/battle_ids.gd`
- `scripts/data/group_ids.gd`
- `scripts/data/event_type_ids.gd`
- `scripts/data/reward_type_ids.gd`
- `scripts/data/unit_type_ids.gd`
- `scripts/data/element_name_ids.gd`

---

## Visual Polish

### Add Building Hit/Damage Animation
**Completed:** 2025-11-12
**Category:** Visual Polish
**Effort:** Small

**Description:**
Added visual feedback when buildings (summoner bases) take damage with dynamic flash speed based on attack intensity.

---

### Fix Projectile Aiming on Moving Targets
**Completed:** 2025-11-12
**Category:** Units & Combat
**Effort:** Medium

**Description:**
Implemented predictive targeting for projectiles so they lead moving targets instead of aiming at current position.

---

## UI Revamp

### Revamp Main Menu UI / Navigation
**Completed:** 2025-12-04
**Category:** UI/UX
**Effort:** Medium
**PR:** #94

**Description:**
Replaced the main menu + game mode menu with a streamlined navigation system centered on the Campaign Map.

**Solution Implemented:**
- Replaced MainMenu scene with TitleScreen (tap-to-start entry point)
- Removed GameModeMenu entirely
- Campaign Map now serves as the central hub
- Added hamburger menu button (top-right) opening slide-in Nav Drawer
- Nav Drawer provides access to: Collection, Events, Shop, Settings
- Added campaign selector banner (top-left) to switch between campaigns
- Added Settings and Special Events placeholder screens
- Added NavigationContext service for proper back button behavior
- Moved campaign battle definitions from hardcoded GDScript to JSON files

**Files Changed:**
- New: `title_screen.tscn/gd`, `nav_drawer.tscn/gd`, `hamburger_button.tscn/gd`
- New: `settings_screen.tscn/gd`, `special_events_screen.tscn/gd`
- New: `campaign_selector_modal.tscn/gd`, `campaign_ids.gd`
- New: `data/campaigns/academy_trials.json`
- Deleted: `main_menu.tscn/gd`, `game_mode_menu.tscn/gd`
- Updated: `campaign_map.gd`, `campaign_service.gd`, `scene_manager.gd`
- Updated: `collection_screen.gd`, `shop_screen.gd` (NavigationContext back navigation)

---

### Revamp Pause Menu
**Completed:** 2025-11-12
**Category:** UI/UX
**Effort:** Small

**Description:**
Improved pause menu design with ESC key support and pause button in battle HUD.

---

## Campaign System

### Add Leave Buttons to Caravan Event
**Completed:** 2025-11-25
**Category:** Campaign / Events
**Effort:** Small

**Description:**
Added proper exit options for players who don't want to make a purchase in caravan events.

**Solution Implemented:**
- Added `LeaveIncompleteButton` ("Leave") - exits without completing, player can return
- Added `LeaveCompleteButton` ("Leave without purchasing") - completes event, allows progression
- Each button has its own confirmation popup with clear messaging
- Localization keys added for all button text and confirmation dialogs

---

## Core Game Systems

### Fix Hardcoded UI Strings - Add Localization
**Completed:** 2025-11-25
**Category:** Core Game Systems / Localization
**Effort:** Medium

**Description:**
Many UI files had hardcoded user-facing strings instead of using the `Loc.t()` localization pattern. All user-facing text must be localized for internationalization support.

**Solution Implemented:**
Updated all UI files to use `Loc.t()` with localization keys from `localization/data/en.json`:
- `game_ui.gd` - Win/lose messages
- `collection_screen.gd` - Card stats labels, deck info labels, empty state messages
- `mana_bar.gd` - Mana display format
- `speed_button.gd` - Tooltips
- `deck_builder.gd` - Validation messages, button labels, card popup labels
- `shop_screen.gd` - Gold label, offering details, price display
- `offering_card.gd` - Type labels, price format
- `hero_card.gd` - HP/Mana/Regen stat labels
- `hero_reveal.gd` - Random hero title text

---

## Core Game Systems

### Implement Deck Recycling After Exhaustion
**Completed:** 2025-11-25
**Category:** Core Game Systems
**Effort:** Small

**Description:**
When a player's deck is exhausted (all cards drawn), shuffle the discard pile back into the deck to continue play.

**Solution Implemented:**
- Added `discard_pile: Array[Card]` variable to track played cards
- Added `deck_recycled(card_count: int)` signal for UI/audio feedback
- Modified `play_card()` / `play_card_3d()` to add played cards to discard pile
- Recycle triggers only when BOTH hand AND deck are empty (not just deck)
- When recycling: shuffle discard into deck, then draw fresh full hand
- Added `_recycle_discard_pile()` helper that shuffles discard pile into deck
- Implemented in both `Summoner` (2D) and `Summoner3D` classes
- Logs deck recycle events for debugging

**Behavior:**
1. Play card → goes to discard pile → try to draw from deck
2. If deck has cards: draw 1 card
3. If deck empty but hand has cards: continue playing without drawing
4. When hand AND deck both empty: recycle discard → draw full new hand

**Edge Cases Handled:**
- Empty deck but cards in hand: keep playing until hand exhausted
- Empty deck AND empty discard pile: draw_card() safely returns

**Related Files:**
- `scripts/core/summoner.gd`
- `scripts/core/summoner_3d.gd`

---

## Campaign System

### Implement Win Condition System for Campaign Events
**Completed:** 2025-11-25
**Category:** Campaign / Battle System
**Effort:** Medium

**Description:**
Campaign battles now support configurable win/loss conditions beyond simple base destruction. Different battle types can have different objectives with time limits.

**Solution Implemented:**
- Created `WinConditionIDs` constants class with type-safe win condition references
- Four win condition types:
  - `DESTROY_BASE` - Default, destroy enemy base to win (no time limit)
  - `SURVIVE_TIME` - Survive for specified duration (win on timeout)
  - `TIMED_DESTROY` - Destroy base within time limit (lose on timeout)
  - `KILL_COUNT` - Kill specified number of enemy units
- Updated `GameController3D` to read win conditions from battle config
- Added kill tracking system for KILL_COUNT objective
- Added `objective_progress` signal for UI updates
- Documented usage in `campaign_service.gd`

**Usage in Battle Definitions:**
```gdscript
"win_condition": WinConditionIDs.TIMED_DESTROY,
"time_limit": 60.0,  # seconds
"kill_target": 10,   # for KILL_COUNT
```

**Related Files:**
- `scripts/data/win_condition_ids.gd` (new)
- `scripts/core/game_controller_3d.gd`
- `scripts/services/campaign_service.gd`

---

### Research and Implement Framerate Independence
**Completed:** 2025-11-25
**Category:** Core Game Systems / Performance
**Effort:** Medium

**Description:**
Audited codebase and implemented proper framerate-independent game mechanics to ensure consistent gameplay across different hardware and frame rates.

**Findings:**
- Codebase was already ~98% framerate-independent (excellent delta usage throughout)
- All movement code properly uses delta or Godot 4's move_and_slide() pattern
- All timers/cooldowns use time-based accumulation, not frame counts
- Mana regeneration correctly uses `mana_regen_rate * delta`

**Solution Implemented:**
- Enabled physics interpolation in project.godot for smooth motion at varying FPS
- Created FPS Test Tool (`scripts/debug/fps_test_tool.gd`) with F5-F8 hotkeys
- Created best practices documentation (`docs/technical/framerate-independence.md`)

**Testing:**
- F5: 30 FPS (mobile simulation)
- F6: 60 FPS (standard)
- F7: 120 FPS (high refresh)
- F8: Uncapped

**Related Files:**
- `project.godot` - Added physics interpolation setting
- `scripts/debug/fps_test_tool.gd` (new)
- `docs/technical/framerate-independence.md` (new)

---

## Visual Polish

### Improve Mana Bar UI Design (Tiered Mana Bar)
**Completed:** 2025-11-26
**Category:** UI/UX
**Effort:** Medium

**Description:**
Implemented a tiered mana bar system that wraps at 10 mana per tier with different colors, rather than growing the bar larger for higher mana values.

**Solution Implemented:**
- Created layered ColorRect system where previous tiers show underneath current tier
- Blue intensity color progression: Light Blue → Royal Blue → Indigo → Purple → Magenta
- Smooth fill animations using Tweens (0.2s duration)
- Tier multiplier label (x2, x3, etc.) for completed tiers
- Localized all UI text (mana label, tier multiplier)
- Extracted magic numbers to named constants (HIGHLIGHT_HEIGHT, FILL_ANIM_DURATION)

**Technical Details:**
- Each tier represents 10 mana (MANA_PER_TIER constant)
- Up to 5 tiers supported (50 max mana)
- Dynamically creates ColorRect fills for each tier
- Lower tiers render first (at bottom), higher tiers on top
- Example: 15/25 mana = full Light Blue (tier 1) + half Royal Blue (tier 2)

**Related Files:**
- `scripts/ui/mana_bar.gd` - Complete rewrite with tiered system
- `scenes/ui/mana_bar.tscn` - Updated scene structure
- `localization/data/en.json` - Added tier_multiplier localization

---

## Core Game Systems

### Implement Card and Hero Level System
**Completed:** 2025-11-27
**Category:** Core Game Systems / Progression
**Effort:** Large

**Description:**
Implemented leveling system for cards and heroes that allows them to grow stronger through gameplay.

**Solution Implemented:**

**Card Progression (PR #85):**
- CardProgressionService autoload with XP and level management
- XP thresholds and gold costs with rarity scaling
- CardUpgradeCatalog with upgrade choices per level
- UI display for card levels and progress
- Level-up with upgrade selection modal

**Hero Progression (Phase 2 Foundation):**
- HeroProgressionService autoload (`scripts/services/hero_progression_service.gd`)
- XP thresholds: 0, 100, 250, 500, 850, 1300, 1900, 2700, 3800, 5200
- Gold costs: 0, 50, 100, 200, 400, 700, 1000, 1500, 2000, 3000
- Max level: 10
- Signals: `hero_xp_changed`, `hero_leveled_up`, `hero_ready_to_level_up`
- Battle completion grants hero XP via `hero_xp_reward` in battle config
- Helper methods: `grant_hero_xp()`, `can_level_up()`, `level_up_hero()`, `get_hero_progression_info()`

**Related Files:**
- `scripts/services/card_progression_service.gd` - Card XP/levels
- `scripts/services/hero_progression_service.gd` - Hero XP/levels (new)
- `scripts/core/battle_context.gd` - Battle completion XP grants
- `scripts/services/campaign_service.gd` - Battle XP reward definitions

**Future Phases (tracked in design spec):**
- Phase 3: Level Traits with Trait Lines
- Phase 4: Ultimate Traits at level 10
- Phase 5: Story Traits from campaign events
- Phase 6: Boon System

---

## Campaign System

### Design Campaign Map Interface
**Completed:** 2025-11-19
**Category:** Campaign / UI
**Effort:** Large
**PR:** #54

**Description:**
Designed and implemented the visual and UX approach for the new map-based campaign interface to replace the old list view.

**Solution Implemented:**
- Visual map-based campaign screen with event nodes
- Linear path layout with sine wave positioning for visual interest
- Node/point design showing completed (✓), unlocked (number), and locked (🔒) states
- Progression visualization with path lines connecting nodes
- Lock/unlock indicators with distinct colors per state

---

### Implement Map Node System for Battles
**Completed:** 2025-11-19
**Category:** Campaign
**Effort:** Medium
**PR:** #54

**Description:**
Implemented the technical system for map nodes representing battles and their connections.

**Solution Implemented:**
- `event_nodes` dictionary for fast lookup by event_id
- `event_render_order` array for explicit draw order
- Lock/unlock state read from Campaign service
- Full save/load integration through profile system
- Supports multiple event types (battle, affinity, first_summon, caravan, onboarding)

---

### Add Map Navigation/Selection
**Completed:** 2025-11-19
**Category:** Campaign / UI
**Effort:** Medium
**PR:** #54

**Description:**
Implemented player interaction with the campaign map - selecting and starting battles.

**Solution Implemented:**
- Node click handler with visual feedback
- Detail panel popup showing event name, difficulty, description, rewards
- 2D panning with drag threshold (5px before panning starts)
- Auto-centering on latest unlocked mission
- Deck selector integration in detail panel

---

### Integrate Battle Progression on Map
**Completed:** 2025-11-19
**Category:** Campaign
**Effort:** Small
**PR:** #54

**Description:**
Connected battle completion to map progression - unlocking next nodes, visual updates.

**Solution Implemented:**
- Completed nodes show checkmark (✓) with green styling
- Automatic refresh of map on event completion
- Progress label showing "X/Y Battles Completed"
- Save progression state through Campaign service
- Signal connections for `battle_completed` and `campaign_progress_changed`

---

## Hero System

### Hero System Phase 2: Foundation Implementation
**Completed:** 2025-11-28
**Category:** Heroes / Architecture
**Effort:** Large

**Description:**
Implemented the foundational hero system with traits, progression services, per-hero campaign progress, and hero management UI.

**Solution Implemented:**

**Services:**
- `HeroSelectionService` - Manages active hero selection, hero switching
- `HeroProgressionService` - XP and level management (1-10), gold costs
- `TraitCatalog` - Central trait/boon registry with hero and unit modifiers

**Data Structures:**
- `HeroConfig` - Static configuration with base stats and innate trait IDs
- `HeroInstance` - Runtime state (level, xp, acquired boons, computed stats)
- Trait data with hero stat modifiers (flat/percent) and unit modifiers

**Battle Integration:**
- `BattleContext.set_player_hero_stats()` caches computed stats for DamageSystem
- `HeroModifierProvider` passes unit modifiers to ModifierSystem
- Element-specific damage bonuses (fire_damage_bonus, damage_reduction, etc.)

**Per-Hero Campaign Progress:**
- ProfileRepo stores campaign_progress per hero ID
- Migration preserves legacy progress in `_legacy_progress` backup
- New profiles start with empty per-hero structure

**UI Components:**
- `HeroManagementPanel` - Full roster view, stats, traits, level-up
- `HeroIconWidget` - Persistent hero button on screens (click to open panel)
- `HeroRosterItem` - Individual hero row with select/level-up buttons
- Element colors and symbols centralized in `ElementTypes`

**Localization:**
- All UI strings use `Loc.t()` pattern
- Trait names/descriptions use localization keys

**Deleted Old System:**
- Removed: ActiveModifier, ModifierConfig, ModifierDatabase, ModifierEffect, ModifierRegistry
- These were replaced by TraitCatalog + HeroInstance trait system

**Related Files:**
- `scripts/services/hero_selection_service.gd` (new)
- `scripts/services/hero_progression_service.gd` (new)
- `scripts/data/trait_catalog.gd` (new)
- `scripts/core/hero_instance.gd` (updated for traits)
- `scripts/core/hero_config.gd` (updated: innate_trait_ids)
- `scripts/data/json_profile_repository.gd` (per-hero campaign progress)
- `scripts/ui/hero_management_panel.gd` (new)
- `scripts/ui/hero_icon_widget.gd` (new)
- `scripts/ui/hero_roster_item.gd` (new)
- `scripts/systems/hero_modifier_provider.gd` (updated)
- `scripts/core/battle_context.gd` (hero stats caching)
- `scripts/combat/damage_system.gd` (hero damage bonuses)
- `docs/features/heroes/architecture.md` (updated)

---

### Add Hero Select UI
**Completed:** 2025-11-28
**Category:** UI/UX
**Effort:** Medium

**Description:**
Created hero selection/management UI for viewing and switching between heroes.

**Solution Implemented:**
- `HeroManagementPanel` - Modal panel showing full hero roster
- `HeroIconWidget` - Clickable hero portrait in corner of screens
- `HeroRosterItem` - Individual hero card with stats, XP, level-up button
- Added to CampaignMap, CollectionScreen, GameModeMenu

---

### Design Hero Data Structure
**Completed:** 2025-11-28
**Category:** Heroes / Architecture
**Effort:** Medium

**Description:**
Defined the data structures for hero configuration and runtime state.

**Solution Implemented:**
- `HeroConfig` resource with base stats, innate_trait_ids, element
- `HeroInstance` runtime class with level, xp, acquired_boon_ids
- `TraitCatalog` for trait definitions with modifiers
- Computed stats via `HeroInstance.get_computed_stats()`

---

### Implement Hero Stats System
**Completed:** 2025-11-28
**Category:** Heroes
**Effort:** Medium

**Description:**
Implemented hero stat computation with trait modifiers applied in battle.

**Solution Implemented:**
- Base stats from HeroConfig (base_health, max_mana, mana_regen)
- Trait modifiers apply flat/percent bonuses to stats
- Element-specific bonuses (fire_damage_bonus, damage_reduction)
- BattleContext caches hero stats for DamageSystem
- DamageSystem applies damage_bonus and damage_reduction

---

### Create Hero Selection Screen UI
**Completed:** 2025-11-28
**Category:** Heroes / UI
**Effort:** Medium

**Description:**
Implemented UI for selecting and switching between heroes.

**Solution Implemented:**
- HeroManagementPanel shows all unlocked heroes
- Hero switching via HeroSelection.switch_hero()
- Active hero highlighted in roster
- Stats, traits, XP progress displayed per hero

---

### Design Hero In-Battle UI Elements (Foundation)
**Completed:** 2025-11-28
**Category:** Heroes / UI
**Effort:** Medium

**Description:**
Added hero UI elements to game screens for battle context.

**Solution Implemented:**
- HeroIconWidget shows active hero element color and level
- Widget added to CampaignMap, CollectionScreen, GameModeMenu
- Click opens HeroManagementPanel for hero management

---

### Integrate Heroes into Battle System (Foundation)
**Completed:** 2025-11-28
**Category:** Heroes
**Effort:** Large

**Description:**
Connected hero system to battle mechanics for stat application.

**Solution Implemented:**
- Summoner loads HeroInstance via DeckLoader
- Hero stats applied via BattleContext.set_player_hero_stats()
- DamageSystem reads hero stats for damage calculations
- HeroModifierProvider passes unit modifiers to ModifierSystem
- Per-hero campaign progress saved and loaded correctly

---

## Developer Tools

### Implement Automated Testing Framework
**Completed:** 2025-11-28
**Category:** Developer Tools / Testing
**Effort:** Medium

**Description:**
Added GUT (Godot Unit Test) framework for automated testing of game services and logic.

**Solution Implemented:**
- Installed GUT v9.3.0 addon
- Created test directory structure (`tests/unit/`, `tests/integration/`, `tests/mocks/`)
- Created MockProfileRepo implementing IProfileRepo interface
- Created MockEconomyService and MockCollectionService for service mocking
- Refactored EconomyService and CampaignService for dependency injection
- Services now have `init_for_testing()` method for mock injection
- Created unit tests for EconomyService (15 tests)
- Created unit tests for CampaignService (20+ tests)
- Created unit tests for BattleContext (20+ tests)
- Added tests/README.md with documentation

**Related Files:**
- `addons/gut/` - GUT framework
- `tests/unit/test_economy_service.gd`
- `tests/unit/test_campaign_service.gd`
- `tests/unit/test_battle_context.gd`
- `tests/mocks/mock_profile_repo.gd`
- `tests/mocks/mock_economy_service.gd`
- `tests/mocks/mock_collection_service.gd`
- `scripts/services/economy_service.gd` - Added DI support
- `scripts/services/campaign_service.gd` - Added DI support

---

## Summoner System

### Add Summoner Select UI
**Completed:** 2025-12-04
**Category:** UI/UX
**Effort:** Medium

**Description:**
Created a summoner selection screen allowing players to choose their summoner before battle.

**Implementation:**
- SummonerManagementPanel provides full summoner roster view
- SummonerIconWidget provides persistent summoner button on screens
- SummonerRosterItem shows individual summoner details with stats

---

### Design Summoner Data Structure
**Completed:** 2025-12-04
**Category:** Summoners / Architecture
**Effort:** Medium

**Description:**
Defined the data structure and resource format for summoner characters.

**Implementation:**
- SummonerConfig: Static summoner configuration (base stats, innate traits)
- SummonerInstance: Runtime state (level, xp, acquired boons, computed stats)
- TraitCatalog: Central trait/boon registry with modifiers
- See `docs/features/summoners/architecture.md` for details

---

### Implement Summoner Stats System
**Completed:** 2025-12-04
**Category:** Summoners
**Effort:** Medium

**Description:**
Implemented the technical system for summoner-specific stats and attributes.

**Implementation:**
- SummonerInstance.get_computed_stats() applies trait modifiers to base stats
- BattleContext.set_player_summoner_stats() caches stats for DamageSystem
- Trait modifiers support flat and percent bonuses
- Element-specific damage bonuses (fire_damage_bonus, etc.)

---

### Create Summoner Selection Screen UI
**Completed:** 2025-12-04
**Category:** Summoners / UI
**Effort:** Medium

**Description:**
Designed and implemented the UI screen where players choose their summoner before battle.

**Implementation:**
- SummonerManagementPanel: Full roster view with stats, traits, level-up
- SummonerIconWidget: Persistent summoner button (click to open panel)
- SummonerRosterItem: Individual summoner row with select/level-up buttons
- Summoner switching via SummonerSelection service

---

### Design Summoner In-Battle UI Elements (Foundation)
**Completed:** 2025-12-04
**Category:** Summoners / UI
**Effort:** Medium

**Description:**
Designed UI elements for displaying summoner information and abilities during battle.

**Implementation:**
- SummonerIconWidget added to CampaignMap, CollectionScreen, GameModeMenu
- Shows active summoner element color and level
- Click opens SummonerManagementPanel

**Notes:**
- Ability buttons/cooldowns deferred to Phase 3/4 when abilities are added

---

### Integrate Summoners into Battle System (Foundation)
**Completed:** 2025-12-04
**Category:** Summoners
**Effort:** Large

**Description:**
Final integration of summoner system into the core battle gameplay loop.

**Implementation:**
- Summoner loads SummonerInstance via DeckLoader
- Summoner stats applied via BattleContext.set_player_summoner_stats()
- DamageSystem reads summoner stats for damage bonuses
- SummonerModifierProvider passes unit modifiers to ModifierSystem
- Per-summoner campaign progress in ProfileRepo

**Notes:**
- Summoner abilities deferred to Phase 3/4
- AI summoners for enemies planned for future

---

## UI/UX

### Card Replacement Should Happen In-Place
**Completed:** 2025-12-17
**Category:** UI/UX / Card System
**Effort:** Small

**Description:**
When a card was played and a new card was drawn to replace it, the hand reordered with the new card appearing at the end. This was disorienting as players couldn't remember card positions.

**Solution Implemented:**
- Modified `draw_card()` in summoner.gd to accept optional `target_index` parameter
- When target_index is provided, inserts new card at that position instead of appending
- Modified `_complete_card_play()` to pass the played card's index to draw_card()
- New card now appears in the same slot as the played card
- Other cards maintain their positions

**Related Files:**
- `scripts/core/summoner.gd` - Modified draw_card() and _complete_card_play()

---

## Audio

### Add Background Music System
**Completed:** 2025-12-18
**Category:** Audio
**Effort:** Medium

**Description:**
Implemented core music system with playback, volume control, and transitions.

**Solution Implemented:**
- Created AudioManager autoload (`scripts/services/audio_manager.gd`)
- Audio bus setup (Master, Music, SFX) with dynamic creation
- Crossfade transitions between music tracks (DEFAULT_CROSSFADE: 1.0s)
- Volume control with linear-to-dB conversion
- Settings persistence via ProfileRepo (music_volume, sfx_volume)
- Process mode set to PROCESS_MODE_ALWAYS for pause menu support

**Related Files:**
- `scripts/services/audio_manager.gd` (new)
- `project.godot` - AudioManager autoload registration

---

### Add Battle Music Tracks
**Completed:** 2025-12-18
**Category:** Audio
**Effort:** Small
**Dependencies:** Add Background Music System

**Description:**
Added battle music that plays during combat gameplay.

**Solution Implemented:**
- Added `battle.mp3` from freesound.org (humanoide9000, CC BY 4.0)
- Music starts on `start_game()` in GameController3D
- Music stops on battle end or quit with fade out
- Proper attribution in `resources/audio/ATTRIBUTION.md`

**Related Files:**
- `resources/audio/bgm/battle.mp3` (new)
- `resources/audio/ATTRIBUTION.md` (new)
- `scripts/core/game_controller_3d.gd` - play_music/stop_music calls
- `scripts/ui/pause_menu.gd` - stop music on quit

---

### Add UI Click/Interaction Sounds
**Completed:** 2025-12-18
**Category:** Audio
**Effort:** Small

**Description:**
Added sound feedback for UI interactions (button clicks, menu navigation).

**Solution Implemented:**
- Added `ui_click.wav` from freesound.org (Jaszunio15, CC0)
- `AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)` pattern
- Applied to all major UI buttons across screens:
  - Campaign map, Nav drawer, Deck builder
  - Settings screen, Shop screen, Pause menu
  - Card detail modal, Reward screen
  - Title screen, Summoner selection, Special events

**Related Files:**
- `resources/audio/sfx/ui_click.wav` (new)
- Multiple UI scripts updated with play_ui_sound() calls

---

### Add Card Play Sounds
**Completed:** 2025-12-18
**Category:** Audio
**Effort:** Small

**Description:**
Added sound effects when cards are played and drawn.

**Solution Implemented:**
- Added `card_draw.mp3` from freesound.org (Geoff-Bremner-Audio, CC0)
- Added `card_play.wav` from freesound.org (theplax, CC BY 4.0)
- Sounds triggered via `_on_card_played()` and `_on_card_drawn()` in hand_ui.gd
- Proper attribution in `resources/audio/ATTRIBUTION.md`

**Related Files:**
- `resources/audio/sfx/card_draw.mp3` (new)
- `resources/audio/sfx/card_play.wav` (new)
- `scripts/ui/hand_ui.gd` - sound triggers on card events

---

## UI Revamp

### Revamp Card Hand Display
**Completed:** 2025-12-23
**Category:** UI/UX
**Effort:** Medium

**Description:**
Improved the visual presentation of cards in the player's hand.

**Solution Implemented:**
- Card spacing and layout (CARD_WIDTH = 120, CARD_SPACING = 10)
- Smooth hover animations (rises 40px, scales to 1.2x, 0.25s transition)
- 3D rotation shader with velocity tracking
- Playability indicators (glow for affordable cards, visual feedback for insufficient mana)
- Pulsing glow effect for playable cards
- Draw animation when cards enter hand (0.4s duration)
- Handles varying hand sizes dynamically

**Related Files:**
- `scripts/ui/battle/hand_ui.gd` - Complete hand display implementation

---

*Last Updated: 2026-01-06 - Added Architecture section (DRY Audit, Modifier Migration, Service Interfaces)*
