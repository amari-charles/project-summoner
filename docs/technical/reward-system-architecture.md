# Battle Reward System Architecture

## Overview

The battle reward system handles granting gold, XP, and cards to players after winning battles. It supports three reward types (FIXED, FLEXIBLE, NONE) and uses a type-safe pool system for dynamic card rewards.

---

## How Battles Configure Rewards

```gdscript
# Example from summoners_path_data.gd
{
    "id": BattleIDs.FIRST_TRIAL,
    "data": {
        "reward_type": RewardTypeIDs.FLEXIBLE,      # FIXED | FLEXIBLE | NONE
        "reward_options": [CardIDs.CHARGE, CardIDs.FIRE_WISP, CardIDs.PUFF],
        "player_selects": true,
        "gold_reward": 30,
        "card_xp_reward": 15,
        "summoner_xp_reward": 20,
    }
}
```

---

## Architecture: Service-Driven Design

```mermaid
flowchart TB
    subgraph Config["Battle Configuration"]
        Battle["Battle Data
        reward_type: FIXED | FLEXIBLE | NONE
        reward_pool / reward_filters / reward_options
        gold_reward: int
        xp_rewards: {...}"]
    end

    subgraph Services["Service Layer"]
        RS["RewardService
        ✅ get_reward_spec(battle) → unified spec
        ✅ grant_battle_rewards() → grants & guards
        ✅ Pool resolution via C#"]

        CS["CampaignService
        ✅ claim_battle_rewards() with completion guard
        ✅ Delegates to RewardService"]
    end

    subgraph Screen["RewardScreen (Thin)"]
        UI["Display Layer
        - Receives spec from service
        - Renders choices
        - Returns player selection
        - No business logic"]
    end

    Battle --> RS
    RS -->|"reward spec"| UI
    UI -->|"player choice"| RS
    RS --> CS

    style Services fill:#ccffcc
    style Screen fill:#ccffcc
```

---

## Request Flow

```mermaid
sequenceDiagram
    participant Screen as RewardScreen
    participant RS as RewardService
    participant CS as CampaignService

    Note over Screen,CS: Service makes decisions, Screen just displays

    Screen->>RS: get_reward_spec(battle_id)

    Note over RS: Service determines:<br/>- Is this a replay?<br/>- What type?<br/>- What options?

    RS-->>Screen: RewardSpec {<br/>  is_replay: false,<br/>  gold: 30,<br/>  xp: 20,<br/>  card_options: [...],<br/>  requires_choice: true<br/>}

    Screen->>Screen: Display spec (no logic, just render)
    Screen->>Screen: Player selects option[1]

    Screen->>CS: claim_battle_rewards(battle_id, card_reward)

    CS->>CS: Check: is_battle_completed?

    alt Already completed
        CS-->>Screen: {} (no rewards)
    else First time
        CS->>RS: grant_battle_rewards(battle, card_reward)
        RS->>RS: Grant gold + card
        CS-->>Screen: {granted: true, card: "fire_wisp", gold: 30}
    end
```

---

## FLEXIBLE Reward Configuration Options

Battles with `reward_type: FLEXIBLE` can configure card options in three ways:

### Option 1: Predefined Pool (Enum-Based)

```gdscript
{
    "reward_type": RewardTypeIDs.FLEXIBLE,
    "reward_pool": RewardConstants.PoolId.FIRE_COMMON_UNITS,
    "draw_count": 3,
    "exclude_owned": true,
    "player_selects": true,
}
```

### Option 2: Inline Filters

```gdscript
{
    "reward_type": RewardTypeIDs.FLEXIBLE,
    "reward_filters": {
        "element": RewardConstants.Element.FIRE,
        "rarity": RewardConstants.Rarity.COMMON,
        "card_type": RewardConstants.CardType.SUMMON,
    },
    "draw_count": 3,
}
```

### Option 3: Explicit Options

```gdscript
{
    "reward_type": RewardTypeIDs.FLEXIBLE,
    "reward_options": [CardIDs.CHARGE, CardIDs.FIRE_WISP, CardIDs.PUFF],
    "player_selects": true,
}
```

---

## Type-Safe Pool System (C#)

### Pool Types

| Type | Description | Example |
|------|-------------|---------|
| **Curated** | Explicit card lists | TutorialRewards, BossLoot |
| **Filter-Based** | Element + rarity + type filters | FireCommonUnits, AllSpells |
| **Composite** | Union of other pools | ElementalStarters |

### Key Files

- `RewardPoolId.cs` - Enum defining all pool IDs
- `RewardPoolCatalog.cs` - Pool definitions and card resolution
- `RewardConstants.gd` - GDScript mirror enums for type safety

### GDScript Interop

```gdscript
# From GDScript, use integer enum values
var drawn_ids: Array = _cs_service.DrawFromPoolEnum(
    RewardConstants.PoolId.FIRE_COMMON_UNITS,  # int
    3,      # count
    true,   # exclude_owned
    true    # unique_only
)
```

---

## RewardSpec Format

The `get_reward_spec()` method returns a unified structure:

```gdscript
{
    "is_replay": bool,           # True if battle already completed
    "reward_type": StringName,   # FIXED | FLEXIBLE | NONE
    "gold_reward": int,          # Gold amount
    "summoner_xp": int,          # Summoner XP reward
    "card_xp": int,              # Card XP reward
    "card_options": Array[Dictionary],  # Normalized card options
    "requires_choice": bool,     # True if player must select
    "chosen_index": int,         # Index from pending reward (-1 if not chosen)
}
```

### Normalized Card Option Format

All card options are normalized to:

```gdscript
{
    "catalog_id": String,    # Card ID
    "rarity": String,        # "common", "rare", etc.
    "count": int,            # Number of copies
    "display_name": String,  # Localized card name
}
```

---

## Guards and Safety

### Replay Prevention

`claim_battle_rewards()` checks if the battle is already completed before granting rewards:

```gdscript
func claim_battle_rewards(battle_id: String, card_reward: Dictionary) -> Dictionary:
    # Guard against replay
    if is_battle_completed(battle_id):
        print("CampaignService: Battle '%s' already completed, skipping rewards" % battle_id)
        return {}
    # ... grant rewards
```

### Validation

`CampaignService._validate_battle_rewards()` validates both `reward_cards` (FIXED) and `reward_options` (FLEXIBLE) at startup.

---

## Summary

| Aspect | Implementation |
|--------|----------------|
| **Who decides reward type?** | RewardService via `get_reward_spec()` |
| **Who checks if replay?** | CampaignService (guard in `claim_battle_rewards()`) |
| **Data format** | Unified via `_normalize_card_options()` |
| **RewardScreen responsibility** | Display only |
| **Pool resolution** | C# `RewardPoolCatalog` with type-safe enums |
