# Campaign Economy & Systems Implementation

**Status:** In Progress
**Started:** 2026-01-21

## Overview

Implementing four interconnected systems to align the codebase with the January 2026 design decisions.

## Phases

| Phase | System | Status | PR |
|-------|--------|--------|-----|
| 1 | Campaign-Scoped Gold | ✅ Complete | #198 |
| 2 | Level Cap System | ✅ Complete | Pending |
| 3 | Flexible Reward System | ⬜ Not Started | - |
| 4 | Boons → Items Refactor | ⬜ Not Started | - |

---

## Phase 1: Campaign-Scoped Gold ✅

**Goal:** Gold earned in a campaign has no value outside that campaign.

### Changes Made

**C# Data Models:**
- `CampaignProgressData.cs` - Added `Gold` field
- `ProfileRepositoryBridge.cs` - Updated serialization

**GDScript Services:**
- `economy_service.gd` - Added campaign gold methods:
  - `get_campaign_gold(summoner_id)`
  - `add_campaign_gold(amount, summoner_id)`
  - `spend_campaign_gold(amount, summoner_id)`
  - `clear_campaign_gold(summoner_id)`
  - `can_afford_campaign_gold(amount, summoner_id)`
  - Signal: `campaign_gold_changed`

- `campaign_service.gd` - Updated to use campaign gold:
  - `grant_battle_reward()` now uses `add_campaign_gold()`
  - Added `get_campaign_gold()`
  - Added `end_campaign(summoner_id, victory)` - clears gold

**Migration:**
- `json_profile_repository.gd` - Version 4→5 migration
  - Moves account gold to active summoner's campaign progress
  - Clears account-wide gold

**Localization:**
- Added keys: `ui.reward.campaign_gold`, `ui.economy.*`

---

## Phase 2: Level Cap System ✅

**Goal:** Battles can have level caps. Cards normalized to cap.

### Changes Made

**C# Service:**
- `LevelCapService.cs` - New service with:
  - `GetEffectiveLevel(cardLevel, cap)` - Returns min(level, cap)
  - `GetEffectiveUpgrades(upgrades, cap)` - Returns upgrades for levels 1..cap
  - `GetCappedUpgradeModifiers(instanceId, cap)` - Computes stat mods with cap
  - `HasLevelCap(config)` / `GetLevelCap(config)` - Battle config helpers
  - `GetPathType(config)` / `GetRecommendedLevel(config)` - Path helpers

**Battle Context:**
- `battle_context.gd` - Added level cap support:
  - `_level_cap` field set from battle config
  - `get_level_cap()` / `has_level_cap()` - Accessors
  - `get_effective_card_level(level)` - Apply cap to card level
  - `get_effective_card_upgrades(upgrades)` - Apply cap to upgrades

**Battle Config Fields:**
- `level_cap` - Max card level (0 = uncapped)
- `path_type` - "standard" or "elite"
- `recommended_level` - Suggested card level

**Tests:**
- `LevelCapServiceTest.cs` - Comprehensive unit tests

**Localization:**
- Added keys: `ui.battle.level_cap`, `ui.battle.recommended_level`, etc.

---

## Phase 3: Flexible Reward System

**Goal:** Configurable reward generation with guaranteed + pool options.

### Planned Changes

- Create `RewardPool.cs` resource type
- Create `RewardPoolCatalog.cs`
- Create `RewardService.cs` with:
  - `GenerateRewardOptions(battleConfig, summoner, ownedCards)`
  - Pool filtering by element, rarity, collection
- Update battle configs with flexible reward settings:
  - `guaranteed_count` - summoner-themed options
  - `pool_count` - drawn from pool
  - `collection_filter` - exclude_owned/exclude_duplicates/none

---

## Phase 4: Boons → Items Refactor

**Goal:** Replace abstract boons with 4-slot equippable items.

### Planned Changes

**New C# Types:**
- `ItemDefinition.cs` - Item catalog entry
- `ItemInstanceData.cs` - Owned item instance
- `ItemSlot` enum - Grimoire, Weapon, Ring, Vestments
- `ItemBinding` enum - SummonerBound, AccountWide

**C# Services:**
- `ItemService.cs` - Item management
- `ItemCatalog.cs` - Item definitions

**Data Migration:**
- Version 5→6: Convert `acquired_boon_ids` to items

**UI:**
- Update summoner screen with equipment panel
- Create equipment management modal

---

## Related Documentation

- [Ideation Session 2026-01-19](ideation-session-2026-01-19.md)
- [Card Progression Economy](card-progression-economy.md)
- [Campaign Structure](../features/campaign/structure.md)
