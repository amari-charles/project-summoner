# Item System

**Status:** CURRENT
**Last Updated:** 2026-01-19

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

Items have different binding rules based on how they're acquired:

| Source | Binding | UI Indicator |
|--------|---------|--------------|
| Campaign reward | Summoner-bound | (none) |
| Event reward | Account-wide | `[Shared]` tag |
| Shop purchase | Account-wide | `[Shared]` tag |

### Summoner-Bound Items
- Acquired through campaign progression
- Part of that summoner's forged fate
- Cannot be used by other summoners

### Account-Wide (Shared) Items
- Acquired from events, shop, or other non-campaign sources
- Any summoner on the account can equip them
- Prevents forcing players to grind events X times for X summoners

---

## Item Acquisition

### Campaign Rewards
Items can be offered as rewards during campaign progression. These become summoner-bound.

### Event Rewards
Events and trials can drop items. These are account-wide and tagged `[Shared]`.

### Shop
The meta shop may sell items. These are account-wide and tagged `[Shared]`.

---

## Design Intent

The item system exists to provide:

1. **Tactical flexibility** - Adapt loadout to different opponents/situations
2. **Horizontal progression** - Own many items, equip few, choose based on strategy
3. **Accessible farming** - Account-wide items mean one grind benefits all summoners
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
