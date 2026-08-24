# Item System

**Status:** CURRENT
**Last Updated:** 2026-08-23

## Overview

Items are equippable gear for summoners that provide tactical flexibility. Unlike cards (which represent permanent fate-forged choices), items can be swapped between battles to adapt strategy.

**Note:** The item system replaces the former "boon" system. Items serve the same strategic role (tactical customization) but as tangible gear rather than abstract bonuses.

---

## Core Concept

| Aspect | Description |
|--------|-------------|
| **What they are** | Equippable gear that modifies summoner capabilities |
| **Permanence** | Swappable between battles |
| **Slots** | 4 total (see below) |
| **Strategic role** | Tactical adaptation for different matchups/situations |

Items complement the trait system:
- **Traits** = "Who your summoner IS" (permanent identity)
- **Items** = "How they fight today" (tactical choice)

---

## Item Slots

Summoners have 4 item slots:

| Slot | Name | Thematic Role |
|------|------|---------------|
| 1 | **Grimoire** | Source of magical knowledge |
| 2 | **Weapon/Staff** | Channel of power |
| 3 | **Ring** | Focus of will |
| 4 | **Vestments** | Protection and presence |

Each slot can hold one item. Players own many items but equip only 4 at a time.

---

## Item Binding

Normal gameplay items are owned by the summoner who acquires them. Event-exclusive
content is the deliberate exception: each event-exclusive item may be authored as
either summoner-bound or account-wide according to that event's reward design.

| Source | Binding | UI Indicator |
|--------|---------|--------------|
| Quest, encounter, shop, or ordinary world reward | Summoner-bound | (none) |
| Event-exclusive reward | Configurable: summoner-bound or account-wide | `[Shared]` only when account-wide |

### Summoner-Bound Items
- Default for all normal gameplay-item acquisition
- Part of that summoner's forged fate
- Cannot be used by other summoners

### Account-Wide (Shared) Items
- Reserved for event-exclusive items explicitly authored as shared
- Any summoner on the account can equip them
- Can prevent an event-exclusive reward from requiring duplicate acquisition for every summoner

---

## Item Acquisition

Normal grant APIs require the target Summoner explicitly. Missing ownership
context fails without creating an account-wide item. Inventory queries combine
that Summoner's items with explicitly shared event-exclusive items, and equip
validation applies the same accessibility rule.

### Quest and Encounter Rewards
Items offered through quest or encounter progression become summoner-bound.

### Event Rewards
Events and trials can drop exclusive items. Their authored reward definition
decides whether each item is summoner-bound or account-wide. Account-wide event
items are tagged `[Shared]`.

### Shop
Ordinary purchased gameplay items belong to the active summoner.

---

## Design Intent

The item system exists to provide:

1. **Tactical flexibility** - Adapt loadout to different opponents/situations
2. **Horizontal progression** - Own many items, equip few, choose based on strategy
3. **Intentional event reach** - Specific event-exclusive items may be shared when repeating that event for every summoner would undermine the event design
4. **Build expression** - Items + Traits + Cards = unique summoner identity

---

## Item Rarity

Items may have rarity tiers (Common through Legendary). This is lower priority and not yet finalized.

---

## Open Questions

- Specific items and their effects (to be designed)
- Exact slot category names (Grimoire/Weapon/Ring/Vestments are working names)
- Item rarity distribution and balance
- Crafting or upgrade mechanics for items

---

*Related Documents:*
- [Summoner System](../summoners/README.md)
- [Card System](../cards/system.md)
- [Card Progression & Economy](../../design/card-progression-economy.md)
