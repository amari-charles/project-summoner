# Fateforged - Card Progression, Resources & Economy

> High-Level Design Document (Premise-Only Version)

> **Current-scope notice:** The campaign map, campaign-run gold, and Caravan
> sections below are superseded and retained only until the documentation
> archive audit moves their historical material. Fateforged now uses
> professor-led quests, reusable encounters, and a permanent Campus Shop owned
> by Mr. and Mrs. Merriweather. Do not implement the retired campaign sections.

## 1. Core Goal of Progression

Fateforged's progression must emphasize:

- **Player uniqueness** through meaningful choices
- **Scoped choice pools** that prevent card convergence
- **Fairness** for F2P and paid players
- **Power that is earned** through gameplay, not bought directly
- **Monetization** through extra attempts at power-granting activities, not power itself

The entire progression system supports those pillars.

---

## 2. Card Progression Model

Cards gain modest baseline stat growth automatically with level. Meaningful
specialization and behavioral change come from player-selected upgrades.

Card progression now follows the shared discovery-driven development model in
[Discovery-Driven Development](discovery-driven-development.md). Card levels
provide reliable, card-bound development capacity, while quests, rituals,
materials, and other discoveries can determine which branches are available.
The older assumption that every level automatically exposes a complete choice
set is superseded.

### 2.1 Card Levels

- Each card has a small number of upgrade levels, capped at 10
- Reaching each level awards bankable development capacity for that card
- Reaching each level also applies modest configured baseline stat growth
- Upgrades may include core stat increases and/or identity-flavored effects, depending on the card
- Each level grants a globally configured number of bankable Card Points;
  spending is not forced at level-up

Each owned card instance has its own XP, level, Card Points, acquired traits,
and chosen upgrades. A card development surface contains the card's native Core
path and a collection of acquired trait paths; it renders one selected path at a
time rather than one unbounded dynamically growing graph.

The native Core is an explicitly authored path for that Card, visible from the
time the Card is acquired. It is not a shared menu of generic health, damage, or
speed purchases. Core branches begin with behavior that changes the Card's role
or use; smaller stat developments may support the identity established by the
branch. Hidden or discovered development normally arrives as an acquired trait
path beside Core rather than being concealed inside the Card's natural path.
The Card itself is the Core's single already-owned root node; all native branches
connect outward from it.

Automatic growth is configured per Card rather than forced through one universal
formula. Stronger Cards may also become more mana-intensive, using mana cost as
part of the tradeoff against their increased effectiveness. Exact curves and
cost changes are tuning work, not a prerequisite for the progression foundation.

### 2.2 XP-Based Unlocking

- Cards gain XP from battles and events
- **Only cards IN YOUR DECK gain XP** — cards not in your active deck receive no XP
- XP progression awards card-bound development capacity; discovery and authored
  requirements determine which upgrades are available
- XP cannot be purchased directly for power; it must be primarily earned through gameplay
- Optional: XP boosters or pass progress can exist, but they only accelerate
  level and point acquisition, not grant a specific upgrade directly

### 2.3 Battle Level Caps

All battles have a **level cap** that normalizes card power:

- **Cards are floored to the cap**: If your card is above the cap, it's treated as the cap level. If below, it's treated as the cap level (floored up).
- **Acquired build is preserved**: The cap limits the Card's effective level and
  automatic level-scaled stats. It does not disable acquired Core or trait
  upgrades based on acquisition order.
- **Standard path exception**: Standard path battles have NO level cap — players can grind infinitely to overlevel and trivialize standard content. This is the escape valve for stuck players.
- **Elite path maintains caps**: Elite battles keep their level caps as skill checks.

This ensures:
- Elite content stays challenging regardless of grinding
- Standard content serves as an accessible fallback
- Player skill matters more than raw card levels for elite content

See [Campaign Structure](../features/campaign/structure.md) for full path system details.

### 2.4 Resource Gating

- Acquiring a configured upgrade may also require resources
- Resources act as an additional gating mechanism, shaping which upgrade directions are available
- If the player lacks the configured costs for a specific upgrade, that upgrade
  remains unavailable even when the card has an unspent point
- This makes resource collection part of build identity

### 2.5 Permanent Behavioral Branches

Card upgrade trees may contain mutually exclusive branches that change how the
card functions rather than only increasing its statistics. Choosing one branch
permanently closes its sibling branch and any descendants owned by that sibling.
The player cannot eventually unlock both branches and swap between them.

The first decision in a Core fork should establish the branch's mechanical
identity. Later numerical nodes are valid when they strengthen that authored
play pattern, but the Core must not collapse into direct investment in generic
stats already covered by automatic level growth.

Behavioral branches should begin with the creature or spell identity and express
different strategic uses of that identity. For example, a segmented worm summon
could choose between:

- a fission branch where killing the worm creates two smaller worms; or
- a segmented-survival branch where an otherwise lethal hit removes one body
  segment and the shortened worm survives.

Some branches may require an authored ritual in addition to reaching the required
card level. A ritual can consume materials gathered through battles, quests,
excursions, shops, or other approved sources. The ritual is the unlock method for
that branch, not a separate stackable power layer.

Requiring rarer materials does not automatically justify a stronger branch. The
branches should remain meaningful specializations or sidegrades unless the wider
player-power model explicitly budgets a power difference.

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
- Appears as **visible nodes on the campaign graph** (players can plan ahead)
- Uses **campaign-scoped gold** (no value outside this campaign)
- Offers campaign-bound or run-specific items
- May offer resource clusters relevant to the campaign's theme
- Does not sell permanent progression items
- All decisions here affect only this specific run

**Economic tension:** Spend now for immediate power, or save for expensive items at later caravans. Since gold dies with the campaign, players must commit.

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

Progression toward advanced development is governed by:

1. **XP and levels** — award Card Points and modest automatic baseline growth
2. **Discovery and access** — determine which paths are available
3. **Configured costs** — may require points, resources, other sacrifices, or
   nothing further

This ensures:
- Players must play to progress
- Upgrades remain meaningful
- Paid shortcuts cannot directly purchase power
- Build direction emerges from XP + resources + player choices

**Note:** Card and summoner leveling requires **only XP**, not gold. This allows players to max out their cards over time regardless of campaign outcomes. Gold is used exclusively for in-campaign purchases (see below).

### 6.1 Gold is Campaign-Scoped

Gold has **no value outside the specific campaign** it was earned in. When a campaign ends (victory or defeat), unspent gold is lost.

**Gold is used for:**
- Caravan shop purchases (items, consumables)
- In-campaign event purchases
- NOT for card or summoner leveling (use XP only)

**Design intent:**
- Forces real economic decisions during the campaign
- No hoarding across campaigns or summoners
- Creates strategic tension: spend now vs. save for later caravans

Players can see upcoming Caravan stops on the campaign graph, allowing informed spending decisions. The campaign ends with a **final spending opportunity** to prevent "died with full pockets" frustration.

See [Campaign Structure](../features/campaign/structure.md#gold-economy) for full gold economy details.

---

## 7. Philosophy Summary

### 7.1 Identity Must Come From Choices

Levels provide modest reliable baseline growth. Meaningful differentiation and
behavioral power come through scoped, permanent choices rather than convergence
through infinite grinding.

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

Fateforged uses discovery-driven Card progression. Each unique Card instance gains
XP, levels automatically, receives modest baseline growth, and banks the globally
configured number of Card Points per level. The card spends those points across its native Core path and
acquired trait paths, while quests, rituals, events, materials, and other
authored requirements determine which opportunities are available and what they
cost. Paths are permanent and may include behavioral sidegrades or mutually
exclusive transformations. Campaign-scoped gold remains separate from leveling.

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
- `card_xp_reward`: XP granted to each card in the player's deck (e.g., 15-25)

Example values:
- Tutorial battles: gold=30-40, xp=15
- Standard battles: gold=50-100, xp=20-30 (scale with difficulty)
- Boss battles: gold=150+, xp=40+
- Non-combat events (shops): gold=0, xp=0

### Replay/Grinding Rules

Players can **replay completed battles** with the following rules:

| Reward | First Clear | Replay |
|--------|-------------|--------|
| Gold | Yes | No |
| Card rewards | Yes | No |
| XP | Yes | Yes |

**Why XP-only on replay:**
- Prevents infinite gold farming
- Maintains card scarcity and choice weight
- Still allows leveling up cards through dedicated play
- Standard path (uncapped) + XP replays = escape valve for stuck players

### Upgrade Choices
- Card Points bank until the player chooses to spend them
- Core and acquired-trait paths expose only known opportunities
- Each opportunity defines its own access, costs, and acquisition action
- Permanent branch choices may close sibling opportunities

---

*Last Updated: 2026-08-22*
