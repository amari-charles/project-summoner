# Shop System Architecture Refactor Proposal

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
"shop_purchases": {},  // offering_id -> purchase_count
```

**Add methods**:
```gdscript
func get_shop_purchases() -> Dictionary:
    return _data.get("shop_purchases", {})

func increment_purchase_count(offering_id: String) -> bool:
    var purchases: Dictionary = _data.get("shop_purchases", {})
    var count: int = purchases.get(offering_id, 0)
    purchases[offering_id] = count + 1
    _data["shop_purchases"] = purchases
    _append_to_wal({"op": "shop_purchase", "offering_id": offering_id})
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
```

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

**Refactor purchase flow to use ProfileRepo**:
```gdscript
func purchase_offering(offering_id: String, shop_id: String = "general") -> bool:
    var offering: ShopOffering = _find_offering(offering_id, shop_id)
    if not offering:
        _emit_purchase_failed(offering_id, "Offering not found")
        return false

    # Get state from ProfileRepo
    var resources: Dictionary = _profile_repo.call("get_resources")
    var gold: int = resources.get("gold", 0)
    var purchases: Dictionary = _profile_repo.call("get_shop_purchases")
    var purchase_count: int = purchases.get(offering_id, 0)

    # Validate
    if not offering.can_purchase(gold, purchase_count):
        var reason: String = _get_failure_reason(offering, gold, purchase_count)
        _emit_purchase_failed(offering_id, reason)
        return false

    # Deduct gold via ProfileRepo
    var price: int = offering.get_price()
    if not _profile_repo.call("update_resources", {"gold": -price}):
        _emit_purchase_failed(offering_id, "Failed to deduct gold")
        return false

    # Grant rewards
    if not _grant_offering_rewards(offering):
        # Refund
        _profile_repo.call("update_resources", {"gold": price})
        _emit_purchase_failed(offering_id, "Failed to grant rewards")
        return false

    # Track purchase via ProfileRepo
    if not _profile_repo.call("increment_purchase_count", offering_id):
        push_warning("ShopService: Failed to track purchase count")

    purchase_completed.emit(offering_id, shop_id)
    return true
```

**Load purchase history in _ready()**:
```gdscript
func _ready() -> void:
    # ... existing code ...

    # Purchase history now loaded from ProfileRepo when needed
    # No in-memory cache required
```

### ShopOffering Changes

**Remove runtime state**:
```gdscript
# REMOVE:
@export var purchases_made: int = 0  ❌

# KEEP:
@export var purchase_limit: int = 0
@export var purchase_limit_type: String = "none"
```

**Update methods to take purchase_count as parameter**:
```gdscript
func can_purchase(player_gold: int, purchase_count: int) -> bool:
    if player_gold < get_price():
        return false

    if purchase_limit_type != "none" and purchase_limit > 0:
        if purchase_count >= purchase_limit:
            return false

    return true

func get_remaining_stock(purchase_count: int) -> int:
    if purchase_limit_type == "none":
        return -1  # Unlimited

    return max(0, purchase_limit - purchase_count)
```

### Reward Granting

**Option A: Extract to CollectionService**
```gdscript
// In collection_service.gd
func grant_cards(card_grants: Array[Dictionary]) -> bool:
    for grant in card_grants:
        var catalog_id: String = grant.get("catalog_id", "")
        var count: int = grant.get("count", 1)
        var rarity: String = grant.get("rarity", "common")

        for i in range(count):
            if not add_card(catalog_id, rarity):
                return false
    return true
```

**Option B: New RewardService** (future consideration)
```gdscript
// Unified reward granting for shop, campaign, achievements, etc.
```

**For now**: Keep duplicate logic but extract to shared utility function within ShopService and CampaignService.

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

1. **Reward granting**: Extract now or later?
   - **Recommendation**: Later, during reward system refactor

2. **Shop catalog storage**: Keep in code or move to JSON?
   - **Recommendation**: Code for now, JSON when we have 10+ shops

3. **Purchase history reset**: Should per-refresh limits reset?
   - **Recommendation**: Add `shop_refresh_state` to track this separately

4. **Resource management**: Generic or gold-specific?
   - **Recommendation**: Keep `update_resources()` generic for future currencies

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
