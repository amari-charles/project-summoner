# Implementation Plan: Shop System Refactor

> **Status: HISTORICAL / PARTIALLY SUPERSEDED.** Caravan-specific ownership,
> events, purchase history, and UI are retired. Use the current Campus Shop,
> `ShopService`, universal reward, and commerce-authority documentation for
> active work. This refactor record will be reviewed for `docs/archive/`.

## Current State & Problems

### What Was Built
An initial shop system was created with:
- `ShopOffering` - Resource for defining purchasable items
- `ShopService` - Autoload managing shops and purchases
- Gold currency already in ProfileRepository

### Architectural Violations Found

#### 1. Currency Management in Wrong Layer
**Problem**: ShopService implements `get_player_gold()`, `add_gold()`, `subtract_gold()` methods that directly manipulate profile data.

**Why It's Wrong**:
- Bypasses the data layer (ProfileRepo)
- No WAL logging for currency transactions
- Violates established pattern where services READ from ProfileRepo but WRITE through it

**Existing Pattern**:
```gdscript
// CollectionService doesn't manage resources directly
// DeckService calls _repo.upsert_deck()
// CampaignService calls _repo methods for state changes
```

#### 2. In-Memory Purchase History
**Problem**: `_purchase_history` Dictionary stored in memory with TODOs for persistence.

**Why It's Wrong**:
- Lost on game restart
- Not backed up or synced
- Doesn't follow profile schema pattern

**Existing Pattern**:
```gdscript
// CampaignService stores completed_battles in ProfileRepo
// Loads in _ready(), saves through ProfileRepo
```

#### 3. No Shop Catalog System
**Problem**: Empty `_init_caravan_shops()` with no actual shop definitions.

**Why It's Wrong**:
- ShopOffering resources created ad-hoc
- No central definition of shops
- Inconsistent with CampaignService's `_init_battles()` pattern

#### 4. ShopOffering Mixes Definition with Runtime State
**Problem**: `purchases_made` field in ShopOffering resource.

**Why It's Wrong**:
- Resources are templates/definitions, not runtime state
- Like putting "times_completed" on a battle definition
- Purchase counts are player state, not shop definition

#### 5. Duplicated Reward Logic
**Problem**: `_grant_offering_rewards()` reimplements card granting that CampaignService already has.

**Why It's Wrong**:
- Two places with same logic for granting cards
- Maintenance burden
- Inconsistent behavior risk

---

## Proposed Architecture

### Layer Separation

```
┌─────────────────────────────────────────┐
│         UI Layer (Shop Screens)         │
│  - Caravan Shop UI                      │
│  - General Shop UI                      │
└─────────────────┬───────────────────────┘
                  │ Reads offerings, calls purchase
                  ▼
┌─────────────────────────────────────────┐
│     Service Layer (ShopService)         │
│  - Shop catalog (_init_shops)           │
│  - Purchase logic & validation          │
│  - Signals for UI reactivity            │
└─────────────────┬───────────────────────┘
                  │ Calls methods, no direct access
                  ▼
┌─────────────────────────────────────────┐
│   Data Layer (ProfileRepository)        │
│  - resources.gold                       │
│  - shop_purchases: {id -> count}        │
│  - WAL logging                          │
│  - Persistence                          │
└─────────────────────────────────────────┘
```

### ProfileRepository Changes

**Add to schema** (`_create_fresh_profile()`):
```gdscript
"shop_purchases": {},  // "shop_id::offering_id::refresh_epoch" -> purchase_count
"shop_refresh_state": {  // Per-shop refresh tracking
    "general": {
        "refresh_epoch": 0,
        "last_refresh_at": ""
    }
}
```

**Add methods** (typed, no stringly-typed `call()`):
```gdscript
func get_shop_purchases() -> Dictionary:
    return _data.get("shop_purchases", {})

func get_purchase_count(purchase_key: String) -> int:
    var purchases: Dictionary = _data.get("shop_purchases", {})
    return purchases.get(purchase_key, 0)

func increment_purchase_count(purchase_key: String) -> bool:
    var purchases: Dictionary = _data.get("shop_purchases", {})
    var count: int = purchases.get(purchase_key, 0)
    purchases[purchase_key] = count + 1
    _data["shop_purchases"] = purchases
    _append_to_wal({"op": "shop_purchase", "key": purchase_key})
    return save_profile(true)

func get_resources() -> Dictionary:
    return _data.get("resources", {})

func update_resources(delta: Dictionary) -> bool:
    var resources: Dictionary = _data.get("resources", {})
    for key in delta:
        var current: int = resources.get(key, 0)
        resources[key] = current + delta[key]
    _data["resources"] = resources
    _append_to_wal({"op": "update_resources", "delta": delta})
    return save_profile(true)

func get_shop_refresh_state(shop_id: String) -> Dictionary:
    var state: Dictionary = _data.get("shop_refresh_state", {})
    return state.get(shop_id, {"refresh_epoch": 0, "last_refresh_at": ""})

func increment_shop_refresh_epoch(shop_id: String) -> bool:
    var state: Dictionary = _data.get("shop_refresh_state", {})
    var shop_state: Dictionary = state.get(shop_id, {"refresh_epoch": 0, "last_refresh_at": ""})
    shop_state["refresh_epoch"] = shop_state.get("refresh_epoch", 0) + 1
    shop_state["last_refresh_at"] = Time.get_datetime_string_from_system()
    state[shop_id] = shop_state
    _data["shop_refresh_state"] = state
    _append_to_wal({"op": "shop_refresh", "shop_id": shop_id})
    return save_profile(true)
```

**Note**: Hero affinity for pricing should be obtained from the active deck's hero via `DeckService` → `HeroCatalog.get_hero(deck.hero_id)` to support affinity-based pricing and conditional caravan content.

### Design Pattern: Resource vs Dictionary

**ShopOffering as Resource** (current approach):
- **Pros**: Type safety, inspector integration, reusable assets
- **Cons**: Runtime state confusion (removed `purchases_made`), harder to generate dynamically
- **Best for**: Static shop definitions (tutorial caravan, fixed offerings)

**Dictionary-based shop definitions** (alternative):
- **Pros**: Easy to generate from JSON, flexible, no resource file clutter
- **Cons**: No type checking, more boilerplate validation
- **Best for**: Rotating shops, procedurally generated offerings

**Current Decision**: Hybrid approach
- **ShopOffering Resource**: Use for well-defined, reusable offerings (tutorial packs, special bundles)
- **Dictionary catalog**: Use for shop definitions in `_init_shops()` that create ShopOffering instances at runtime
- **Future**: When shop count grows (20+ shops), migrate catalog to JSON files

This keeps the benefits of typed ShopOffering while allowing data-driven shop configuration.

**Bridge: Dictionary → ShopOffering**

Here's how dictionary definitions become typed ShopOffering instances:

```gdscript
func _find_offering(offering_id: String, shop_id: String) -> ShopOffering:
    var shop: Dictionary = _shops.get(shop_id)
    if shop == null:
        return null

    for offering_def in shop["offerings"]:
        if offering_def.get("offering_id") == offering_id:
            return _build_offering_from_dict(offering_def)

    return null

func _build_offering_from_dict(def: Dictionary) -> ShopOffering:
    var offering := ShopOffering.new()
    offering.offering_id = def.get("offering_id", "")
    offering.type = def.get("type", "CARD")
    offering.card_catalog_id = def.get("card_catalog_id", "")
    offering.base_price = def.get("base_price", 0)
    offering.purchase_limit_type = def.get("purchase_limit_type", "none")
    offering.purchase_limit = def.get("purchase_limit", 0)
    # For CARD_PACK types:
    if def.has("cards"):
        offering.cards = def["cards"]
    return offering
```

---

### ShopService Refactor

**Remove these methods**:
- `get_player_gold()` ❌
- `add_gold()` ❌
- `subtract_gold()` ❌
- `_set_player_gold()` ❌
- `_load_purchase_history()` ❌
- `_get_purchase_count()` ❌
- `_increment_purchase_count()` ❌

**Add shop catalog**:
```gdscript
func _init_shops() -> void:
    # Tutorial caravan
    _shops["caravan_tutorial"] = {
        "id": "caravan_tutorial",
        "shop_type": "caravan",
        "name": "Merlin's Trading Post",
        "offerings": [
            {
                "offering_id": "tutorial_fire_recruit",
                "type": "CARD",
                "card_catalog_id": "fire_recruit",
                "base_price": 25,
                "purchase_limit_type": "account",
                "purchase_limit": 3
            },
            {
                "offering_id": "tutorial_spell_pack",
                "type": "CARD_PACK",
                "base_price": 50,
                "cards": [
                    {"catalog_id": "charge", "count": 1}
                ],
                "purchase_limit_type": "account",
                "purchase_limit": 1
            }
        ]
    }

    print("ShopService: Initialized %d shops" % _shops.size())
```

**Add in-memory cache for performance**:
```gdscript
var _purchase_cache: Dictionary = {}  # "shop_id::offering_id::refresh_epoch" -> count

func _ready() -> void:
    # ... existing code ...

    # Load purchase history into cache
    _purchase_cache = ProfileRepo.get_shop_purchases()
```

**Add helper for purchase key generation**:
```gdscript
func _build_purchase_key(shop_id: String, offering_id: String, refresh_epoch: int) -> String:
    # Per-refresh and account-limited offerings both include the epoch
    # Account-limited offerings can ignore epoch changes or pass 0
    return "%s::%s::%d" % [shop_id, offering_id, refresh_epoch]
```

**Refactor purchase flow with typed calls and atomicity**:
```gdscript
func purchase_offering(offering_id: String, shop_id: String = "general") -> bool:
    var offering: ShopOffering = _find_offering(offering_id, shop_id)
    if not offering:
        _emit_purchase_failed(offering_id, "Offering not found")
        return false

    # Get shop refresh state
    var shop_refresh_state: Dictionary = ProfileRepo.get_shop_refresh_state(shop_id)
    var refresh_epoch: int = int(shop_refresh_state.get("refresh_epoch", 0))

    # Build namespaced key with refresh epoch
    var purchase_key: String = _build_purchase_key(shop_id, offering_id, refresh_epoch)

    # Get state from ProfileRepo (typed calls, no .call())
    var resources: Dictionary = ProfileRepo.get_resources()
    var gold: int = resources.get("gold", 0)
    var purchase_count: int = _purchase_cache.get(purchase_key, 0)

    # Build context for validation
    var context: ShopPurchaseContext = ShopPurchaseContext.new()
    context.player_gold = gold
    context.purchase_count = purchase_count
    # Get hero affinity from active deck
    var active_deck = Decks.get_active_deck()
    var hero_data = HeroCatalog.get_hero(active_deck.hero_id) if active_deck else {}
    context.hero_affinity = hero_data.get("element", ElementTypes.NEUTRAL)
    context.refresh_epoch = refresh_epoch

    # Validate
    if not offering.can_purchase(context):
        var reason: String = _get_failure_reason(offering, context)
        _emit_purchase_failed(offering_id, reason)
        return false

    # Transaction atomicity: All-or-nothing guarantee
    var price: int = offering.get_price()

    # Step 1: Deduct gold
    if not ProfileRepo.update_resources({"gold": -price}):
        _emit_purchase_failed(offering_id, "Failed to deduct gold")
        return false

    # Step 2: Grant rewards via RewardService
    var rewards: Dictionary = _build_reward_dict(offering)
    if not RewardService.grant_rewards(rewards):
        # Rollback: Refund gold
        ProfileRepo.update_resources({"gold": price})
        _emit_purchase_failed(offering_id, "Failed to grant rewards")
        return false

    # Step 3: Track purchase (namespaced key)
    if not ProfileRepo.increment_purchase_count(purchase_key):
        push_warning("ShopService: Failed to track purchase count")
        # Don't rollback - player got their items, tracking is non-critical
    else:
        # Update cache
        _purchase_cache[purchase_key] = _purchase_cache.get(purchase_key, 0) + 1

    purchase_completed.emit(offering_id, shop_id)
    return true
```

**Transaction Atomicity Guarantees**:
- If gold deduction fails → No changes made
- If reward granting fails → Gold refunded automatically
- If purchase tracking fails → Warning logged, but player keeps purchase
- All state changes go through ProfileRepo with WAL logging
- ProfileRepo.save_profile() ensures disk persistence

**Note**: `RewardService.grant_rewards()` is best-effort and does not roll back partial success internally. If `grant_rewards()` returns false, ShopService refunds gold but any already-granted rewards may remain. These cases should be rare and are logged for later correction.

**Purchase History Caching**:

ShopService maintains a thin `_purchase_cache` keyed by `"shop_id::offering_id::refresh_epoch"`, loaded from ProfileRepo on `_ready()` and updated after successful purchases. ProfileRepo remains the single source of truth; the cache is purely for read performance during purchase validation.

### ShopOffering Changes

**Remove runtime state**:
```gdscript
# REMOVE:
@export var purchases_made: int = 0  ❌

# KEEP:
@export var purchase_limit: int = 0
@export var purchase_limit_type: String = "none"
```

**Add ShopPurchaseContext class** (future-proof API):
```gdscript
# In scripts/resources/shop_purchase_context.gd
class_name ShopPurchaseContext
extends RefCounted

var player_gold: int = 0
var purchase_count: int = 0
var hero_affinity: String = ""  # For future affinity discounts
var active_bonuses: Array[String] = []  # For future event bonuses ("fire_week", etc.)
var refresh_epoch: int = 0  # For per-refresh limits
```

**Update methods to use context object**:
```gdscript
func can_purchase(context: ShopPurchaseContext) -> bool:
    # Check gold
    var price: int = get_price()
    if context.player_gold < price:
        return false

    # Check purchase limits
    if purchase_limit_type != "none" and purchase_limit > 0:
        if context.purchase_count >= purchase_limit:
            return false

    # Future: Check affinity discounts, event bonuses, etc.
    # All data available in context object

    return true

func get_remaining_stock(context: ShopPurchaseContext) -> int:
    if purchase_limit_type == "none":
        return -1  # Unlimited

    return max(0, purchase_limit - context.purchase_count)

func get_price(context: ShopPurchaseContext = null) -> int:
    var base: int = base_price

    # When we implement affinity/event pricing, callers should pass context
    # so price reflects discounts/bonuses.
    # if context and context.hero_affinity == card_affinity:
    #     base = int(base * 0.9)  # 10% discount for matching affinity

    return base
```

**Benefits of Context Object**:
- Future-proof: Add new fields without breaking API
- Clean signature: One parameter instead of growing list
- Extensible: Easy to add affinity discounts, event bonuses, VIP status, etc.
- Testable: Mock context for unit tests

### Reward Granting

**Implement RewardService autoload** to centralize all reward granting across the game.

**Why Now**:
- Already have: Campaign rewards, Shop rewards
- Coming soon: Achievements, daily quests, event rewards
- Prevents logic duplication and ensures consistency

**RewardService Implementation**:
```gdscript
# scripts/services/reward_service.gd
class_name RewardServiceClass
extends Node

func grant_rewards(rewards: Dictionary) -> bool:
    var success: bool = true

    # Grant gold
    if rewards.has("gold"):
        var gold: int = rewards["gold"]
        if not ProfileRepo.update_resources({"gold": gold}):
            push_error("RewardService: Failed to grant gold")
            success = false

    # Grant cards
    if rewards.has("cards"):
        var cards: Array = rewards["cards"]
        for card_grant in cards:
            var catalog_id: String = card_grant.get("catalog_id", "")
            var count: int = card_grant.get("count", 1)
            var rarity: String = card_grant.get("rarity", "common")

            for i in range(count):
                if not Collection.add_card(catalog_id, rarity):
                    push_error("RewardService: Failed to grant card %s" % catalog_id)
                    success = false

    # Grant cosmetics (future)
    if rewards.has("cosmetics"):
        # TODO: Implement cosmetic granting
        pass

    return success
```

**Add to project.godot**:
```
RewardService="*res://scripts/services/reward_service.gd"
```

**Atomicity & Rollback Semantics**:

`grant_rewards()` is **best-effort**: it does not roll back partial success internally. For example, if granting 3 cards and the second fails, the first card is already in the player's collection.

Callers that require atomic behavior (like ShopService) must treat a `false` result as a failure and handle rollback of their part (e.g., refunding gold). We accept that extremely rare partial reward cases may slip through during crashes, and log them aggressively for later correction.

If true transactional atomicity is needed, that's a bigger "transaction log" problem for future consideration.

**Usage in ShopService**:
```gdscript
func _build_reward_dict(offering: ShopOffering) -> Dictionary:
    var rewards: Dictionary = {}

    if offering.type == "CARD":
        rewards["cards"] = [{"catalog_id": offering.card_catalog_id, "count": 1}]
    elif offering.type == "CARD_PACK":
        rewards["cards"] = offering.cards  # Array of card grants

    return rewards
```

---

## Migration Plan

### Phase 1: Fix GDScript Errors (Immediate)
- Fix type inference errors in for loops
- Fix unsafe casts
- Make code compile

### Phase 2: Add ProfileRepo Support (Data Layer)
- Add `shop_purchases` to schema
- Add `get_shop_purchases()` method
- Add `increment_purchase_count()` method
- Add `get_resources()` method (if not exists)
- Add `update_resources()` method (if not exists)

### Phase 3: Refactor ShopService (Business Layer)
- Remove currency management methods
- Remove in-memory purchase history
- Add `_init_shops()` catalog
- Refactor `purchase_offering()` to use ProfileRepo
- Update all ProfileRepo calls to use proper methods

### Phase 4: Clean ShopOffering (Resource Layer)
- Remove `purchases_made` field
- Update `can_purchase()` signature
- Update `get_remaining_stock()` signature

### Phase 5: Testing
- Test purchase flow end-to-end
- Verify gold deduction
- Verify purchase limits
- Verify persistence across restarts

---

## Benefits of This Architecture

### 1. Data Integrity
- All state mutations go through ProfileRepo
- WAL logging for all transactions
- Proper backup/restore support

### 2. Consistency
- Matches Collection/Deck/Campaign patterns
- Clear layer separation
- Single source of truth for data

### 3. Maintainability
- Shop definitions in one place (`_init_shops()`)
- No scattered state management
- Easier to debug

### 4. Extensibility
- Easy to add new shop types
- Pricing formulas can be swapped
- Purchase limits are configurable

---

## Open Questions

1. **Reward granting**: Done now via RewardService
   - Campaign and Shop will call `RewardService.grant_rewards()` going forward
   - Centralized logic prevents duplication across all reward sources

2. **Shop catalog storage**: Keep in code or move to JSON?
   - **Recommendation**: Code for now, JSON when we have 10+ shops

3. **Purchase history reset**: Handled via refresh epochs
   - `shop_refresh_state` tracks per-shop epochs for per-refresh limits
   - Purchase keys include epoch: `"shop_id::offering_id::refresh_epoch"`

4. **Resource management**: Generic or gold-specific?
   - **Decision**: Keep `update_resources()` generic for future currencies
   - `delta` is additive and can be negative: `{"gold": -50}` deducts 50 gold

---

## Success Criteria

- ✅ No GDScript errors
- ✅ Gold operations use ProfileRepo methods
- ✅ Purchase history persists across restarts
- ✅ Shop catalog defined in `_init_shops()`
- ✅ ShopOffering is pure configuration
- ✅ All purchases logged to WAL
- ✅ Matches existing service patterns

---

## Next Steps

1. Get approval on this architecture proposal
2. Implement Phase 1 (fix errors)
3. Implement Phase 2-4 (refactor)
4. Test and validate
5. Document final architecture in shop-caravan-system.md
