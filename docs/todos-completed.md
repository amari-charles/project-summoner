# Completed TODOs Archive

This document archives TODOs that have been completed. For active tasks, see [todos.md](todos.md).

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

*Last Updated: 2025-11-25*
