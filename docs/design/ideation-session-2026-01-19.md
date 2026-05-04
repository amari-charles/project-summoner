# Design Ideation Session - January 19, 2026

**Status:** FINALIZED
**Purpose:** Capture design decisions from ideation session - now integrated into official docs

---

## Table of Contents

1. [Campaign Structure](#campaign-structure)
2. [Path System (Elite vs Standard)](#path-system-elite-vs-standard)
3. [Level Cap System](#level-cap-system)
4. [Grinding and XP](#grinding-and-xp)
5. [Gold Economy](#gold-economy)
6. [Summoner System](#summoner-system)
7. [Item System (Replaces Boons)](#item-system-replaces-boons)
8. [Traits](#traits)
9. [Card System Clarifications](#card-system-clarifications)
10. [Shared Content](#shared-content)
11. [Expansion Model](#expansion-model)
12. [For Later](#for-later)

---

## Campaign Structure

### ✅ DECIDED: One Campaign, All Summoners

**The campaign is NOT per-summoner.** All summoners play through the same campaign structure (same battles, same events, same story beats). What differs is what choices are offered at each node based on summoner's element theme.

This is critical for development scope - we build ONE campaign, not five.

### ✅ DECIDED: No Runs, No Restarts

The campaign is a **one-time permanent journey** per summoner. There are no "runs" in the roguelike sense. You play through once, your choices permanently shape your collection, and that's your forged fate.

**Replayability comes from:**
- Purchasing a new summoner (different element = different offers = different deck)
- Not from restarting the same summoner's campaign

### ✅ DECIDED: Campaign Forges Online Summoners

After the campaign, players can take that summoner into online battles. The campaign is therefore a character-forging process, not content that resets into a neutral PvP template.

PvP is **not** intended to be perfectly fair, symmetrical, or fully normalized. If a player made poor choices in PvE, skipped stronger rewards, or failed to earn certain upgrades, that summoner can be weaker online. This is acceptable and part of the permanence fantasy.

Design guardrails:
- Weakness should come from understandable decisions and outcomes, not unclear traps.
- Stronger campaign results can produce stronger online tools.
- Online balance should be mindful and intentional, not perfectly equalized.
- The game should communicate when a choice affects long-term/online power.

### ✅ DECIDED: Exclusivity is Core

The whole premise is that you CAN'T get all the cards. Every choice excludes alternatives permanently. This is non-negotiable - it's the core identity of "Fateforged."

### ✅ DECIDED: Summoner-Specific Guaranteed Offers

At choice nodes, at least one option matches the summoner's themed element. This ensures elemental identity without separate campaigns.

**Important:** Summoners are NOT restricted to their element. A Fire-themed summoner can use any element's cards. The "guaranteed offer" just ensures they always have the OPTION to pick their themed element.

---

## Path System (Elite vs Standard)

### ✅ DECIDED: Two Path Types

| Path Type | Rewards | Level Cap | Purpose |
|-----------|---------|-----------|---------|
| **Elite** | Better rewards (better cards, traits) | Has cap (skill check) | For confident/skilled players |
| **Standard** | Lesser rewards | No cap (can grind) | Escape valve for struggling players |

### ✅ DECIDED: Elite Path Structure

- Elite path has backloaded rewards (nothing along the way, big payout at the end IF you win)
- Standard path has front-loaded rewards (steady gains along the way)
- Lose elite battle → routed to standard path END (missed all journey rewards)
- Real risk: you could end up with LESS than if you'd played safe

### ✅ DECIDED: Decision Types

| Type | Frequency | Description |
|------|-----------|-------------|
| **Major decisions** | Rare | Elite vs standard path branch points |
| **Minor decisions** | Regular | Standard battles with card choices (pick 1 of 3) |
| **Filler battles** | Common | Just for XP/minor rewards, no real decision |

---

## Level Cap System

### ✅ DECIDED: All Battles Have Level Caps

Every battle has a visible level cap. This is transparent - players can assess difficulty.

### ✅ DECIDED: Cards Floored to Cap

Cards are brought UP or DOWN to the cap level:

```
BATTLE: "Stone Golem" (Level Cap: 5)

Your cards:
- Level 8 card → treated as Level 5 (capped down)
- Level 5 card → stays Level 5
- Level 3 card → treated as Level 5 (floored up)
```

### ✅ DECIDED: Upgrades Capped Too

Only upgrades from levels 1 through the cap apply. If your card is level 8 but cap is 5, only upgrades from levels 1-5 are active for that battle.

### ✅ DECIDED: Standard Path Exception

Standard path battles have NO level cap. Players can grind infinitely to overlevel and trivialize standard content. This is intentional - it's the escape valve.

### ✅ DECIDED: Recommended Level Display

Show recommended level for each battle so players know where they stand.

### ✅ DECIDED: Optional Capped Practice

Setting to fight standard battles WITH caps to test skill/see how you're doing against intended difficulty.

---

## Grinding and XP

### ✅ DECIDED: Replay for XP Only

- Can replay battles for XP
- NO gold or card rewards on replays
- XP unlocks level-ups → unlocks upgrade choices

### ✅ DECIDED: Only Deck Cards Gain XP

Only cards IN YOUR DECK gain XP from battles. Cards not in deck don't level.

This means:
- Commitment to deck choices matters
- Want to level a new card? Put it in your deck and grind
- Since standard path has no cap, grinding is always possible

### ✅ DECIDED: Standard Becoming Trivial is OK

If players overlevel standard content, that's fine. It's the escape valve. Elite content stays challenging (capped).

---

## Gold Economy

### ✅ DECIDED: Gold is Campaign-Scoped

Gold has **no value outside the specific campaign** it was earned in. When a campaign ends (victory or defeat), unspent gold is lost.

**Key implications:**
- No hoarding gold across campaigns
- No transferring gold between summoners
- Must spend it or lose it
- Forces real economic decisions

### ✅ DECIDED: Caravan Visibility

The Caravan (in-campaign shop) appears as **visible nodes on the campaign graph**. Players can see upcoming caravan stops and plan spending accordingly.

This creates informed economic choices, not blind gambles.

### ✅ DECIDED: Economic Tension Philosophy

The skill of gold management lies in balancing immediate power vs. future purchases:

| Strategy | Advantage | Risk |
|----------|-----------|------|
| **Spend early** | Immediate power boost | Miss expensive items later |
| **Save up** | Access to powerful late-game items | May die before spending |

Since gold dies with the campaign, there's no "optimal" hoarding strategy. Players must commit.

### ✅ DECIDED: Gold Penalties as Levers

Gold loss can be used as a penalty for various events (failing elite paths, story events, risk/reward choices). This is a **tunable lever**, not a hard rule — specific penalties designed per-event.

### ✅ DECIDED: Final Spending Opportunity

The campaign ends with a **final chance to spend remaining gold**. Prevents "died with full pockets" frustration.

---

## Summoner System

### ✅ DECIDED: New Summoner = New Campaign Playthrough

Purchasing a new summoner = starting a fresh campaign journey with:
- Different elemental theme (different guaranteed offers)
- Fresh choices through the same campaign structure

### ✅ DECIDED: Summoners Can Use Any Element

Summoners are THEMED around an element but NOT restricted to it. The theme just influences what's guaranteed to be offered, not what's allowed.

### ✅ DECIDED: Summoner Customization Layers

| Layer | Description | Permanence |
|-------|-------------|------------|
| **Element Theme** | Fire, Water, Earth, Air, Lightning, etc. | Fixed at purchase |
| **Traits** | Innate growth (stat bonuses) | Permanent, uncapped |
| **Items** | Equippable gear (replaces boons) | Swappable between battles |
| **Deck** | Cards acquired through choices | Permanent per summoner, carried into online play |

---

## Item System (Replaces Boons)

### ✅ DECIDED: Items Replace Boons

The old "boon" system is replaced by items. Items are the summoner tactical customization layer.

### ✅ DECIDED: Items Go on Summoners (Not Cards)

Items are equippable gear for summoners that provide tactical flexibility. Unlike cards (permanent fate), items can be swapped between battles.

### ✅ DECIDED: 4 Item Slots

- **Grimoire** - Spell/magic focused
- **Weapon/Staff** - Offense focused
- **Ring** - Utility focused
- **Vestments** - Defense focused

### ✅ DECIDED: Item Binding

| Source | Binding | Tag |
|--------|---------|-----|
| Campaign reward | Summoner-bound | (none) |
| Event reward | Account-wide | `[Shared]` |
| Shop purchase | Account-wide | `[Shared]` |

---

## Traits

### ✅ DECIDED: Traits are Simple Stat Bonuses

Traits are permanent summoner growth. Simple bonuses like:
- +damage
- +unit HP
- +fire damage
- etc.

### ✅ DECIDED: Traits Never Compete with Cards

Traits and cards are separate reward tracks. Player should never choose directly between "get a trait" OR "get a card" at the same node. Traits come from different sources (leveling, achievements, story events).

---

## Card System Clarifications

### ✅ DECIDED: No Items on Individual Cards

Items go on summoners, not cards. Card system already has variants + rarity + progression.

### ✅ DECIDED: Card Binding

| Source | Binding | Campaign Usable |
|--------|---------|-----------------|
| Campaign choices | Summoner-bound | Yes |
| Event rewards | Can be `[Shared]` (see below) | No (locked for campaign) |

---

## Shared Content

### ✅ DECIDED: Shared is a Lever, Not a Rule

Making event cards/items account-wide `[Shared]` is an OPTION we can use, not a blanket policy. Not all event content needs to be shared.

**When we use it:**
- Prevents forcing players to grind events X times for X summoners
- Use for content where multi-summoner grind would feel bad

### ✅ DECIDED: Shared Content Locked for Campaign

Shared cards/items appear in summoner's view but are **locked for campaign use**. Available for PvP and events only. This prevents trivializing campaign with farmed content.

---

## Expansion Model

### ✅ DECIDED: Years = New Content

Expansions add new campaign content over time. Year 1, Year 2, etc.

### ✅ DECIDED: Card Pool Sanctity

- No overlap between expansion card pools (or extremely careful if any)
- Each card belongs to its era
- Maintains uniqueness and avoids dilution

### 💭 DISCUSSED: Continuation vs New Class

Two options considered, not yet decided:
- **Continuation:** Year 2 = new content continuing the same story
- **New Class:** Year 2 = new summoners, new campaign, set X years in future

Leaning toward continuation but not finalized.

---

## For Later

These concepts were discussed and deemed interesting but deferred:

- **Battle modifiers** - Optional handicaps for bonus rewards
- **Double or nothing** - Risk reward quality after wins
- **Specific campaign map** - Will design after documenting this approach

---

## Resolved Questions

- [x] Items on cards? → No, items go on summoners
- [x] Campaign per summoner? → No, one campaign for all
- [x] Roguelike runs? → No, permanent journey per summoner
- [x] Item binding? → Hybrid (campaign = bound, events = optionally shared)
- [x] Summoner element restriction? → None, just themed
- [x] What replaces boons? → Items
- [x] How to handle stuck players? → Standard path has no level cap (grind escape valve)
- [x] How to prevent trivializing elite content? → Level caps
- [x] XP distribution? → Only deck cards gain XP
- [x] Gold persistence? → Campaign-scoped only, lost when campaign ends
- [x] Caravan visibility? → Visible nodes on campaign graph
- [x] Gold loss mechanics? → Tunable lever, not hard rule

---

*Last Updated: 2026-01-19*
