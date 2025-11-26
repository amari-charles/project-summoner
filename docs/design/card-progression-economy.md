# Project Summoner - Card Progression, Resources & Economy

> High-Level Design Document (Premise-Only Version)

## 1. Core Goal of Progression

Project Summoner's progression must emphasize:

- **Player uniqueness** through meaningful choices
- **Scoped choice pools** that prevent card convergence
- **Fairness** for F2P and paid players
- **Power that is earned** through gameplay, not bought directly
- **Monetization** through extra attempts at power-granting activities, not power itself

The entire progression system supports those pillars.

---

## 2. Card Progression Model

Cards do not gain power automatically. All power comes from player-selected upgrades.

### 2.1 Card Levels

- Each card has a small number of upgrade levels, capped at 10
- Reaching each level unlocks a choice of upgrades
- Upgrades may include core stat increases and/or identity-flavored effects, depending on the card
- Players pick exactly one upgrade per level

### 2.2 XP-Based Unlocking

- Cards gain XP from battles and events
- XP progression unlocks the ability to choose an upgrade at the next level
- XP cannot be purchased directly for power; it must be primarily earned through gameplay
- Optional: XP boosters or pass progress can exist, but they only accelerate access to choice tiers, not grant upgrades directly

### 2.3 Resource Gating

- Unlocking the chosen upgrade at a level also requires resources
- Resources act as an additional gating mechanism, shaping which upgrade directions are available
- If the player lacks the required resources for a specific upgrade, that upgrade remains locked, even if XP for that level is earned
- This makes resource collection part of build identity

---

## 3. Resource Model

Resources exist to gate upgrade options, not to be sold as raw power.

### 3.1 Resource Categories

- Resources come in types, which may be themed around elements, archetypes, roles, or events
- Different upgrades require different resource types
- This organically creates build direction based on what the player collects

### 3.2 Acquisition of Resources

- Resources are earned naturally through gameplay, especially via events, trials, gauntlets, and other special modes
- These resources are the primary reason to run events repeatedly

### 3.3 Purchasing Resources

- The meta shop may sell limited quantities of certain resources
- These purchases must not allow players to brute-force a perfect build; they function as supplements, not replacements for gameplay
- Specialized, event-exclusive resources must come from the corresponding event, not the meta shop

---

## 4. Two-Shop System

To keep complexity manageable and monetization clean, the game uses two shops with distinct roles.

### 4.1 Campaign Caravan (Run-Bound Shop)

**Purpose:** Supports the story/campaign run

**Characteristics:**
- Appears during campaign progression in a fixed order
- Cannot be revisited
- Offers campaign-bound or run-specific items
- May offer resource clusters relevant to the campaign's theme
- Does not sell permanent progression items
- All decisions here affect only this specific run

This keeps the base campaign simple and prevents new players from being overwhelmed.

### 4.2 Meta / Seasonal Shop (Global Shop)

**Purpose:** Long-term progression and monetization

**Characteristics:**
- Accessible outside the campaign, anytime
- Sells access to events (passes, keys, entries)
- May include seasonal XP, temporary boosters, or capped resource bundles
- Never sells direct power or guaranteed upgrades
- Power is still earned by playing events that the passes unlock
- Fully separated from the campaign's structure

This shop is where the monetization primarily lives.

---

## 5. Events, Trials, Gauntlets (Endgame/Side Activities)

These modes are the heart of power progression and monetization.

### 5.1 Event Rewards

Events drop:
- XP
- Resources
- Upgrade-choice opportunities
- Themed, scoped card-choice options

### 5.2 Scoped Pools

Every event, trial, or gauntlet limits its rewards to a specific pool. This preserves uniqueness and prevents convergence.

**Examples:**
- Fire Trial → Fire-themed resources + Fire-pool card choices
- Beast Gauntlet → Beast-themed resources
- Summoner Path → Summoner-aligned resources and choices

### 5.3 Monetization Through Attempts

Players may:
- Get free daily/weekly attempts
- Use the meta shop to purchase 1–2 extra attempts (capped)

Players are not buying power. They are buying extra shots at earning power through gameplay.

This mirrors proven fair monetization patterns used by Pokemon Go raids, TFT labs, and Hades-style roguelike modes.

---

## 6. Progression Gating

Progression toward advanced upgrade tiers is gated by:

1. **XP** — earned primarily from battles/events
2. **Gold** — required to finalize the upgrade
3. **Resources** — required to unlock specific upgrade choices

This triple-gate ensures:
- Players must play to progress
- Upgrades remain meaningful
- Paid shortcuts cannot directly purchase power
- Build direction emerges from XP + resources + player choices

---

## 7. Philosophy Summary

### 7.1 Power Must Come From Choices

All player power is acquired through scoped, meaningful choices, not randomness, not duplicates, not infinite grinding.

### 7.2 Uniqueness Must Be Preserved

Because choices come from theme-scoped pools, players naturally diverge even with similar starting cards.

### 7.3 Monetization Sells Access, Not Power

Players buy extra attempts at high-value content, not the rewards themselves.

### 7.4 Campaign Stays Simple

All complex resource interactions are introduced in optional events, not the main campaign.

### 7.5 Resources Guide Build Identity

What you collect determines which stat upgrades you can afford — giving identity without power selling.

---

## One-Paragraph Summary

Project Summoner uses a choice-driven progression system where cards gain levels capped at 10, and each level unlocks a meaningful upgrade chosen by the player. Upgrades require XP, gold, and themed resources, which tie your build direction to the activities you engage in. Resources come primarily from events, trials, and gauntlets—optional modes outside the main campaign—while the campaign itself stays simple. A two-shop system separates run-bound decisions (Caravan) from long-term monetization (Meta Shop). Monetization revolves around selling extra attempts at events that yield XP, resources, and scoped upgrade-choice opportunities, ensuring paying players never buy power directly and all progression remains earned. This preserves deck uniqueness, fairness, and long-term engagement while keeping development scope low.

---

## Implementation Notes

### XP Thresholds (Exponential Curve)
- Level 2: 30 XP
- Level 3: 75 XP
- Level 4: 150 XP
- Level 5: 300 XP
- Level 6: 500 XP
- Level 7: 800 XP
- Level 8: 1200 XP
- Level 9: 1800 XP
- Level 10: 2500 XP

### Battle Rewards (Per-Event Configuration)
Each battle/event defines its own rewards in `campaign_service.gd`:
- `gold_reward`: Gold granted on victory (e.g., 30-50 for tutorials)
- `card_xp_reward`: XP granted to each card played in battle (e.g., 15-25)

Example values:
- Tutorial battles: gold=30-40, xp=15
- Standard battles: gold=50-100, xp=20-30 (scale with difficulty)
- Boss battles: gold=150+, xp=40+
- Non-combat events (shops): gold=0, xp=0

### Upgrade Choices
- 2-3 upgrade options per level
- Each upgrade has different resource requirements
- Player picks exactly ONE per level

---

*Last Updated: 2025-11-26*
