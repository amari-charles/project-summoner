# Shop & Caravan System Architecture

## Overview

Two distinct shop systems serving different gameplay purposes:
1. **Caravan Shop** - Campaign event-driven, narrative shops with fixed offerings
2. **General Shop** - UI-accessible shop with potentially rotating inventory

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

### Purchase History
- Tracked in ProfileRepository: `shop_purchases: {offering_id -> count}`
- Persisted permanently (not reset on shop refresh)
- Loaded on ShopService initialization

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

### Shared Logic Extraction

Both ShopService and CampaignService grant card rewards. Extract to shared utility:

**Option A: Extend CollectionService**
```gdscript
# CollectionService gains:
func grant_cards(card_grants: Array[Dictionary]) -> bool
```

**Option B: New RewardService**
```gdscript
# New autoload:
RewardService.grant_rewards({
    "cards": [{catalog_id: "fire_recruit", count: 2}],
    "gold": 50,
    "special": ["cosmetic_id"]
})
```

Both services call the same underlying logic.

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

EventSequencer gains new step type: `OPEN_CARAVAN`

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
