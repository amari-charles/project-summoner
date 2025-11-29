# Completed TODOs Archive

This document archives TODOs that have been completed. For active tasks, see [todos.md](todos.md).

---

## Summoner System

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

*Last Updated: 2025-11-28*
