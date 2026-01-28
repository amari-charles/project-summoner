# Campaign Data Architecture

## Overview

Campaign data defines battles, events, and progression structure. Data is defined in GDScript files rather than JSON to enable compile-time validation of card references.

## File Structure

```
scripts/data/campaigns/
├── onboarding_data.gd      # Tutorial/shared campaign
├── combat_arena_data.gd    # Dev testing arena
└── academy_trials_data.gd  # Main per-summoner campaign
```

## Data Pattern

Each campaign data file follows this pattern:

```gdscript
class_name OnboardingData
extends RefCounted

static func get_campaign() -> Dictionary:
    return {
        "campaign_id": CampaignIDs.ONBOARDING,
        "name_key": "campaign.onboarding.name",
        "description_key": "campaign.onboarding.description",
        "sort_order": 0,
        "is_shared": true,  # true = account-wide, false = per-summoner
        "unlock_requirements": [],
        "battles": _get_battles(),
    }

static func _get_battles() -> Array[Dictionary]:
    return [
        {
            "id": BattleIDs.FIRST_TRIAL,
            "event_type": EventTypeIDs.BATTLE,
            "dev_player_deck": [
                {"catalog_id": CardIDs.FIRE_WISP, "count": 2},
            ],
            "enemy_deck": [
                {"catalog_id": CardIDs.EARTH_SPRITE, "count": 1},
            ],
            # ... other battle properties
        },
    ]
```

## Why GDScript Instead of JSON?

**Compile-time validation**: Using `CardIDs.FIRE_WISP` instead of `"fire_wisp"` means:
- Typos are caught at load time (Godot fails to parse if constant doesn't exist)
- IDE autocomplete works for card names
- Refactoring card IDs updates all references automatically
- No runtime "card not found" errors from campaign data

**Type safety**: `Array[Dictionary]` return types enforce structure.

## ID Constants Used

| Constant Class | Purpose | Example |
|---------------|---------|---------|
| `CampaignIDs` | Campaign identifiers | `CampaignIDs.ONBOARDING` |
| `BattleIDs` | Battle/event identifiers | `BattleIDs.FIRST_TRIAL` |
| `CardIDs` | Card catalog references | `CardIDs.FIRE_WISP` |
| `EventTypeIDs` | Event type enum | `EventTypeIDs.BATTLE` |
| `RewardTypeIDs` | Reward type enum | `RewardTypeIDs.FIXED` |
| `BiomeIDs` | Battlefield biome | `BiomeIDs.SUMMER_PLAINS` |
| `RarityIDs` | Card rarity | `RarityIDs.COMMON` |

## Adding a New Campaign

1. Create `scripts/data/campaigns/my_campaign_data.gd`
2. Follow the pattern above with `get_campaign()` and `_get_battles()`
3. Add campaign ID to `scripts/data/campaign_ids.gd`
4. Register in `CampaignService._load_campaigns()`:
   ```gdscript
   var campaign_data_sources: Array[Callable] = [
       OnboardingData.get_campaign,
       MyCampaignData.get_campaign,  # Add here
   ]
   ```
5. Add localization entries to `localization/data/en.json`

## Adding a New Battle

1. Add battle ID to `scripts/data/battle_ids.gd`
2. Add battle dictionary to the campaign's `_get_battles()` array
3. Use `CardIDs` constants for all deck references
4. Add localization entries for name/description keys

## Battle Properties Reference

```gdscript
{
    "id": BattleIDs.MY_BATTLE,
    "biome_id": BiomeIDs.SUMMER_PLAINS,
    "name_key": "campaign.battle.my_battle.name",
    "description_key": "campaign.battle.my_battle.description",
    "difficulty": 1,
    "event_type": EventTypeIDs.BATTLE,
    "requires_deck": true,
    "repeatable": false,
    "is_tutorial": false,
    "reward_type": RewardTypeIDs.FIXED,
    "reward_cards": [
        {"catalog_id": CardIDs.CHARGE, "rarity": RarityIDs.COMMON, "count": 1},
    ],
    "gold_reward": 50,
    "card_xp_reward": 15,
    "summoner_xp_reward": 75,
    "dev_player_deck": [...],  # For battles with requires_deck=false
    "enemy_deck": [
        {"catalog_id": CardIDs.EARTH_SPRITE, "count": 2},
    ],
    "enemy_hp": 100.0,
    "unlock_requirements": [BattleIDs.PREVIOUS_BATTLE],
    "ai_type": "heuristic",  # or "scripted"
    "event_sequence": "res://resources/sequences/tutorial.tres",  # optional
}
```

---

*Last Updated: 2026-01-01 - Migrated from JSON to GDScript data files*
