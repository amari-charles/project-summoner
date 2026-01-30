# Battle Reward System Architecture

## The Real Problem: Too Many Paths, Wrong Responsibilities

---

## How Battles Configure Rewards Today

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

## Current Architecture: Three Reward Paths

```mermaid
flowchart TB
    subgraph Config["Battle Configuration"]
        FIXED["FIXED
        reward_cards: [{catalog_id, rarity, count}]
        Predetermined card"]

        FLEX["FLEXIBLE
        reward_options: [CardIDs.X, CardIDs.Y]
        player_selects: true
        Predefined choices"]

        NONE["NONE
        No card reward
        Gold/XP only"]
    end

    subgraph Screen["RewardScreen (700+ lines)"]
        Match["match reward_type:"]

        FixedPath["FIXED path
        - Display reward_cards[0]
        - Grant on continue"]

        FlexPath["FLEXIBLE path
        - Check for reward_options?
        - If yes: show choice buttons
        - If no: call RewardService.generate()
        - Handle player selection
        - Store choice in pending state"]

        NonePath["NONE path
        - Show 'Victory!'
        - Skip card grant"]
    end

    subgraph Services["Service Layer"]
        RS["RewardService
        - generate_reward_options() (unused)
        - grant_battle_rewards()"]

        CS["CampaignService
        - claim_battle_rewards()
        - NO completion check!"]
    end

    FIXED --> Match
    FLEX --> Match
    NONE --> Match

    Match --> FixedPath
    Match --> FlexPath
    Match --> NonePath

    FixedPath --> CS
    FlexPath --> CS
    NonePath --> CS
    CS --> RS

    style Screen fill:#ffcccc
    style CS fill:#ffcccc
```

### The Problems

| Issue | Description |
|-------|-------------|
| **Screen does too much** | RewardScreen determines reward type, renders UI, manages state, AND initiates granting |
| **Three data formats** | FIXED uses `{catalog_id, rarity}`, FLEXIBLE uses raw IDs `["charge"]`, dynamic uses `{id, type, amount}` |
| **Unused code paths** | Dynamic generation (`guaranteed_count`, `pool_count`) is built but never used |
| **No service guard** | `claim_battle_rewards()` grants even if battle already completed |
| **Scattered validation** | `reward_cards` validated, but `reward_options` never checked |

---

## Current Sequence: Why It's Confusing

```mermaid
sequenceDiagram
    participant Data as Battle Config
    participant Screen as RewardScreen
    participant CS as CampaignService
    participant RS as RewardService

    Note over Data,RS: Screen makes ALL the decisions

    Screen->>Data: get reward_type
    Data-->>Screen: FLEXIBLE

    Screen->>Screen: Does battle have reward_options?

    alt Has reward_options (legacy path)
        Screen->>Screen: Convert to choice buttons
        Screen->>Screen: Wait for player click
        Screen->>Screen: Store choice index
    else No reward_options (dynamic path - UNUSED)
        Screen->>RS: generate_reward_options(config)
        RS-->>Screen: generated options
        Screen->>Screen: Show generated choices
    end

    Screen->>Screen: Player clicks Continue
    Screen->>Screen: Build card_reward from chosen option
    Screen->>CS: claim_battle_rewards(battle_id, card_reward)

    Note over CS: No completion check!

    CS->>RS: grant_battle_rewards(battle, card_reward)
    RS->>RS: Grant gold + card
```

---

## Proposed Architecture: Service Owns Logic

```mermaid
flowchart TB
    subgraph Config["Battle Configuration (Simplified)"]
        Battle["Battle Data
        reward_type: FIXED | FLEXIBLE | NONE
        reward_config: {...}
        gold_reward: int
        xp_rewards: {...}"]
    end

    subgraph Services["Service Layer (Smart)"]
        RS["RewardService
        ✅ get_reward_spec(battle) → what to show
        ✅ claim_rewards(battle, choice) → guards & grants
        ✅ Handles all formats internally"]

        CS["CampaignService
        ✅ Delegates fully to RewardService
        ✅ Just tracks completion"]
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

### Proposed Sequence: Service-Driven

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

    Screen->>RS: claim_rewards(battle_id, choice_index: 1)

    RS->>RS: Check: is_battle_completed?

    alt Already completed
        RS-->>Screen: {granted: false, reason: "replay"}
    else First time
        RS->>RS: Grant gold + card
        RS->>CS: mark_complete(battle_id)
        RS-->>Screen: {granted: true, card: "fire_wisp", gold: 30}
    end
```

---

## The Two Bugs (Simple Fixes)

### Bug 1: Replay Grants Gold

```mermaid
flowchart LR
    subgraph Current["❌ Current"]
        C1["claim_battle_rewards()"]
        C2["Always grants"]
    end

    subgraph Fixed["✅ Fixed"]
        F1["claim_battle_rewards()"]
        F2["if is_battle_completed: return {}"]
        F3["Then grant"]
    end

    C1 --> C2
    F1 --> F2 --> F3

    style C2 fill:#ffcccc
    style F2 fill:#ccffcc
```

**Fix**: Add 3 lines to `campaign_service.gd`:
```gdscript
func claim_battle_rewards(battle_id, card_reward):
    if is_battle_completed(battle_id):  # ADD THIS
        return {}                         # ADD THIS
    # ... existing code
```

### Bug 2: Summoner Screen Shows Wrong Gold

```mermaid
flowchart LR
    subgraph Current["❌ Current"]
        S1["Economy.get_gold()"]
        S2["Account gold (100)"]
        S3["gold_changed signal"]
        S4["Doesn't exist!"]
    end

    subgraph Fixed["✅ Fixed"]
        F1["Economy.get_campaign_gold()"]
        F2["Campaign gold (actual)"]
        F3["campaign_gold_changed"]
        F4["Works correctly"]
    end

    S1 --> S2
    S3 -.-> S4
    F1 --> F2
    F3 --> F4

    style S2 fill:#ffcccc
    style S4 fill:#ffcccc
    style F2 fill:#ccffcc
    style F4 fill:#ccffcc
```

**Fix**: Change `summoner_screen.gd`:
```gdscript
# Change get_gold() to get_campaign_gold()
# Connect to campaign_gold_changed instead of gold_changed
```

---

## What We Should Do

### Phase 1: Fix the Bugs (Now)
1. Add completion guard in `claim_battle_rewards()` - 3 lines
2. Fix summoner screen gold display - 5 lines

### Phase 2: Clean Up RewardScreen (Later)
1. Move reward type determination into RewardService
2. Create `get_reward_spec(battle_id)` that returns a unified structure
3. RewardScreen becomes a thin display layer

### Phase 3: Unify Data Formats (Later)
1. Standardize on single reward option format
2. Remove unused dynamic generation code OR actually use it
3. Add validation for `reward_options`

---

## Summary

| What | Current State | Ideal State |
|------|--------------|-------------|
| **Who decides reward type?** | RewardScreen | RewardService |
| **Who checks if replay?** | Nobody (bug!) | RewardService |
| **Data format** | 3 different formats | 1 unified format |
| **RewardScreen responsibility** | Everything | Just display |
| **Dynamic generation** | Built, unused | Either use or remove |

**Files to modify for bug fixes:**
1. `scripts/services/campaign_service.gd` - Add completion guard (3 lines)
2. `scripts/ui/screens/summoner_screen.gd` - Fix gold display (5 lines)

**The "dynamic rewards" and "specific_options" aren't the bug** - they're complexity that makes the code harder to understand, but the actual bugs are just missing guards.
