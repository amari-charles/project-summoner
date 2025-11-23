# High-Level Design: Shop & Caravan System

## Overview

Two distinct shop systems serving different gameplay purposes:
1. **Caravan Shop** - Campaign event-driven, narrative shops with fixed offerings
2. **General Shop** - UI-accessible shop with potentially rotating inventory

**Critical Design Rule**: Any item critical to campaign progression must come from Caravan shops or campaign rewards, not the General shop. This ensures the General shop never bypasses story progression.

---

## Caravan Shop (Campaign Events)

### Purpose
- Story-driven shopping experiences during campaign progression
- Tutorial purchases (e.g., "buy your first tactical spell")
- Contextual offerings based on player choices/progress
- NPC merchant encounters with personality/dialogue

### Characteristics
- **Fixed offerings** per event (not randomized)
- **Conditional content** - offerings/dialogue change based on:
  - Player's chosen affinity (fire/ice/nature)
  - Previous campaign decisions
  - Completed battles
  - Current collection state
- **One-time events** - most caravan visits are unique encounters
- **Narrative integration** - dialogue before/during shopping
- **No time pressure** - available until player leaves event

### Technical Requirements
- Defined as campaign events (like battles)
- Integrated with EventSequencer system
- Supports pre-shop and post-shop dialogue
- Can check player state for conditional offerings
- Purchase history tracked permanently

### Example Use Cases
```
Tutorial Caravan:
- Appears after First Trial battle
- Merlin: "You've earned your first gold. Let me show you the traveling merchant."
- Offers: 1x Fire Recruit (25g), 1x Tactical Spell Pack (50g)
- One-time only

Affinity-Specific Caravan:
- Fire player sees: Flame cards, fire-themed items
- Ice player sees: Frost cards, ice-themed items
- Different merchant dialogue based on affinity
```

---

## General Shop (UI Access)

### Purpose
- Standard card acquisition outside campaign
- Repeatable purchasing for deck building
- Potential daily/weekly rotation system

### Characteristics
- **Accessible anytime** from main menu
- **Potentially rotating** inventory (TBD)
- **No narrative context** - pure commerce
- **Repeatable purchases** (with limits if rotating)

### Technical Requirements
- Separate UI scene (shop_screen.tscn)
- May refresh on timer/manual trigger
- Purchase limits configurable (per refresh, lifetime, etc.)
- Filtering/sorting for browsing

### Design Decisions (TBD)
- Rotation frequency (daily/weekly/manual?)
- Fixed catalog vs rotating selection
- Purchase limits per card
- Pricing tiers

---

## Currency System

### Gold (Primary Currency)

**Sources** (To Be Determined):
- [ ] Battle victory rewards
- [ ] Starting allowance (currently 100g in profile)
- [ ] Daily login bonuses
- [ ] Selling duplicate cards
- [ ] Campaign event rewards

**Management**:
- Stored in ProfileRepository: `resources.gold`
- All gold operations go through ProfileRepo (data layer)
- ShopService reads gold, but doesn't mutate directly
- WAL logging for all currency transactions
- `ProfileRepo.update_resources(delta)` is additive and can be negative
  - Example: `{"gold": -50}` deducts 50 gold
  - Example: `{"gold": 100}` adds 100 gold

---

## Pricing System

### Base Strategy: Power-Based Pricing
Cards cost more based on their actual strength/utility, not just rarity.

### Pricing Factors
- **Card stats** - Higher ATK/HP = higher price
- **Utility** - Tactical spells may cost more than units
- **Rarity** - Influences price but not solely determinant
- **Pack discounts** - Bundles cheaper than individual cards

### Configurable Per Offering
Each `ShopOffering` can use:
- `base_price` - Manual price override
- `price_formula: "power"` - Calculate from card stats
- `price_formula: "rarity"` - Rarity tier pricing
- `discount_percent` - Sale pricing

### Power Formula Implementation

When `price_formula: "power"`, uses `card.power_rating` from Card System:
- **Base calculation**: `power_rating × price_per_power_point`
- **Rarity modifier**: +0-50% based on card rarity
- **Future extensions**:
  - Affinity discounts (hero's main element -10%, off-element +10%)
  - Seasonal modifiers ("Fire Week: all fire cards -10% gold")

This keeps shop pricing consistent with the game's balance system.

---

## Purchase Limits

### Three Limit Types (Configurable)

1. **No Limits** (`purchase_limit_type: "none"`)
   - Buy unlimited copies if you have gold
   - Use for: Basic common cards in general shop

2. **Per-Refresh Limits** (`purchase_limit_type: "per_refresh"`)
   - Limited stock that replenishes when shop refreshes
   - Use for: Rotating shop inventory, featured deals

3. **Account Limits** (`purchase_limit_type: "account"`)
   - Hard cap on total purchases ever
   - Use for: Caravan one-time offerings, special items

### Purchase History Keys

Purchase history uses namespaced keys to avoid conflicts:

```
shop_purchases: {
  "caravan_tutorial::fire_recruit::0": 1,
  "general::basic_fire_recruit::7": 5
}
```

Format: `"<shop_id>::<offering_id>::<refresh_epoch>"` ensures:
- Account-level limits don't bleed across shops
- Same card can appear in multiple shops with separate limits
- Clear tracking of where purchases were made
- Per-refresh limits use the current epoch
- Lifetime stats can be computed by summing across all epochs for a given (shop_id, offering_id) if needed later
- Persisted permanently in ProfileRepository
- Loaded on ShopService initialization

### Shop Refresh State

Each rotating shop tracks refresh epochs for per-refresh limits:

```
shop_refresh_state: {
  "general": {
    "refresh_epoch": 7,
    "last_refresh_at": "2025-11-22T10:00:00Z"
  }
}
```

Per-refresh limits are keyed by `(shop_id, offering_id, refresh_epoch)`.
When shop rotates, `refresh_epoch` increments, resetting per-refresh
limits while preserving lifetime purchase history for account-limited offerings.

**Implementation**: In code, this triple `(shop_id, offering_id, refresh_epoch)` is flattened into a single string key via `_build_purchase_key(shop_id, offering_id, refresh_epoch)`.

---

## Data Architecture

### Layer Separation

**ProfileRepository (Data Layer)**:
- Owns: `resources.gold`, `shop_purchases`
- Provides: Read/write methods for all persistent state
- Handles: WAL logging, backup, persistence

**ShopService (Business Logic)**:
- Owns: Shop catalog definitions, purchase flow logic
- Reads from: ProfileRepo (gold, purchase history)
- Writes via: ProfileRepo methods (no direct profile mutation)
- Emits: Signals for UI reactivity

**ShopOffering (Configuration)**:
- Pure definition/template (immutable)
- No runtime state (no `purchases_made` field)
- Used by: ShopService to create shop instances

### Shop Catalog Pattern

Like CampaignService's `_init_battles()`, ShopService has `_init_shops()`:

```gdscript
func _init_shops() -> void:
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
            }
        ]
    }
```

Later: Migrate to JSON files for easier editing.

---

## Reward Granting

### RewardService Pattern

We will introduce a **RewardService** autoload which grants gold, cards, and cosmetics. Both Campaign and Shop call into this shared service so reward logic is never duplicated.

**RewardService API**:
```gdscript
# New autoload in project.godot
RewardService.grant_rewards({
    "cards": [{"catalog_id": "fire_recruit", "count": 2}],
    "gold": 50,
    "cosmetics": ["banner_flame_01"]
}) -> bool
```

**Benefits**:
- Single source of truth for reward granting
- Used by: ShopService, CampaignService, future achievement/daily quest systems
- Centralized validation and error handling
- Consistent reward feedback/animations across all sources

**Implementation Location**: `scripts/services/reward_service.gd`

---

## Event Integration

### Caravan as Campaign Event

Caravan shops are special campaign events:

```gdscript
# In campaign_service.gd:
_battles["event_caravan_tutorial"] = {
    "id": "event_caravan_tutorial",
    "event_type": "caravan",
    "shop_id": "caravan_tutorial",  # Links to shop definition
    "unlock_requirements": ["first_trial"],
    "pre_shop_dialogue": "res://resources/dialogue/caravan_intro.tres",
    "post_shop_dialogue": "res://resources/dialogue/caravan_thanks.tres"
}
```

### Caravan Event Completion

When a caravan event completes, it returns purchase results to the campaign system:

```gdscript
# EventSequencer OPEN_CARAVAN step returns:
{
    "event_completed": true,
    "purchases_made": [
        {"offering_id": "tutorial_fire_recruit", "count": 2},
        {"offering_id": "tutorial_spell_pack", "count": 1}
    ],
    "gold_spent": 100
}
```

This allows:
- Campaign to track player shopping behavior
- Conditional event chains based on purchases ("if bought fire cards, show fire mentor")
- Analytics/telemetry for balancing shop offerings
- Post-shop dialogue that references what player bought

**Note**: `purchases_made` uses the same `offering_id` as in the shop catalog, so it's easy to do follow-up logic without extra mapping.

EventSequencer gains new step type: `OPEN_CARAVAN` which shows shop UI and waits for player to exit.

---

## UI/UX Requirements

### Caravan Shop UI
- Dialogue box for merchant intro
- Grid/list of offerings with:
  - Card preview
  - Price
  - Stock remaining (if limited)
  - "Purchase" button (disabled if can't afford)
- Gold display (current amount)
- "Leave Caravan" button
- Post-purchase confirmation/feedback

### General Shop UI
- Tab/filter by card type or affinity
- Sort by: Price, Name, Rarity, Power
- Search/filter functionality
- "Owned" indicator (show collection count)
- Bulk purchase UI for packs
- Refresh button (if rotating)

---

## Open Questions / TBD

1. **Gold Earning**: Which sources should be implemented first?
   - Victory rewards? Starting allowance? Daily bonuses?

2. **General Shop Rotation**:
   - Time-based (daily reset)?
   - Manual refresh cost?
   - Persistent catalog?

3. **Pricing Formulas**:
   - Exact formula for power-based pricing?
   - How to balance economy?

4. **Card Selling**:
   - Can players sell cards back?
   - What's the sell price? (50% of purchase price?)

5. **Special Items**:
   - Cosmetics? Alternate art?
   - Deck slots? Profile customization?

---

## Implementation Phases

### Phase 1: Core Infrastructure
- [x] ShopOffering resource definition
- [x] ShopService autoload skeleton
- [x] Gold currency in ProfileRepo
- [ ] Fix GDScript type errors
- [ ] Refactor to proper architecture (data layer separation)

### Phase 2: Caravan Shop (Minimum Viable)
- [ ] Caravan shop catalog in ShopService
- [ ] Caravan UI scene
- [ ] EventSequencer OPEN_CARAVAN step
- [ ] First tutorial caravan event
- [ ] Purchase flow testing

### Phase 3: General Shop (Basic)
- [ ] General shop UI scene
- [ ] Main menu integration
- [ ] Basic catalog (all cards available)
- [ ] Purchase flow

### Phase 4: Advanced Features
- [ ] Conditional offerings (affinity-based)
- [ ] Shop rotation system
- [ ] Power-based pricing formulas
- [ ] Card selling mechanics
- [ ] Special items support

---

## Success Criteria

**Caravan Shop MVP**:
- Player can visit caravan during campaign event
- See dialogue before shopping
- Purchase cards with gold
- Gold deducted, cards added to collection
- Purchase limits enforced
- One-time purchases persist (can't buy again)

**General Shop MVP**:
- Access from main menu
- Browse all available cards
- Purchase cards with gold
- See owned count
- Simple pricing (manual or rarity-based)

---

## Architectural Issues Found

During initial implementation, the following architectural violations were identified:

1. **Currency management in ShopService** - Should be in ProfileRepo
2. **Purchase history in-memory** - Should persist to profile
3. **Missing shop catalog** - No `_init_shops()` pattern
4. **ShopOffering runtime state** - `purchases_made` mixes definition with state
5. **Duplicated reward logic** - Both Shop and Campaign grant cards

These need to be fixed to align with existing service layer patterns (Collection, Decks, Campaign).

---

## Notes

- Keep it simple initially - complex rotation/pricing can be added later
- Focus on caravan first (needed for tutorial flow)
- General shop can be placeholder/WIP
- All systems must be data-driven (no hardcoded shops in UI code)
- Maintain clean architecture (data layer separation)
- Follow established patterns from Collection/Deck/Campaign services
