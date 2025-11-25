# Completed TODOs Archive

This document archives TODOs that have been completed. For active tasks, see [todos.md](todos.md).

---

## Database & Data Layer

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

*Last Updated: 2025-11-25*
