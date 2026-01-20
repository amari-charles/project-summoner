# Campaign Structure

**Status:** CURRENT
**Last Updated:** 2026-01-19

## Overview

This document defines the campaign structure for Fateforged, including path types, level caps, decision points, and progression mechanics.

---

## Core Principles

### One Campaign, All Summoners

There is **one campaign** that all summoners play through. The campaign structure (battles, events, story beats) is identical regardless of which summoner you're playing. This is critical for development scope — we build one campaign, not one per summoner.

### No Runs, No Restarts

The campaign is a **one-time permanent journey** per summoner. There are no "runs" in the roguelike sense. You play through once, your choices permanently shape your collection, and that's your forged fate.

**Replayability comes from:**
- Purchasing a new summoner (different element = different offers = different deck)
- Not from restarting the same summoner's campaign

### Exclusivity is Core

You can NEVER collect all cards on a single summoner. Every choice permanently excludes alternatives. This is non-negotiable — it's the core identity of "Fateforged."

### Summoner-Specific Guaranteed Offers

At choice nodes, at least one option matches the summoner's elemental theme. This ensures elemental identity without separate campaigns.

**Important:** Summoners are NOT restricted to their element. A Fire-themed summoner can use any element's cards. The "guaranteed offer" just ensures they always have the OPTION to pick their themed element.

---

## Path System

### Two Path Types

| Path Type | Rewards | Level Cap | Purpose |
|-----------|---------|-----------|---------|
| **Elite** | Better rewards (better cards, traits) | Has cap (skill check) | For confident/skilled players |
| **Standard** | Lesser rewards | No cap (can grind) | Escape valve for struggling players |

### Elite Path Mechanics

- Elite path has **backloaded rewards** (nothing along the way, big payout at the end IF you win)
- Standard path has **front-loaded rewards** (steady gains along the way)
- Lose elite battle → routed to standard path END (missed all journey rewards)
- **Real risk:** you could end up with LESS than if you'd played safe

### Path Selection

Elite vs standard is a **major decision** at branch points. These are rare and meaningful — not every node requires this choice.

---

## Level Cap System

### How Level Caps Work

Every battle has a visible level cap. This is transparent — players can assess difficulty before committing.

### Cards Floored to Cap

Cards are brought UP or DOWN to the cap level:

```
BATTLE: "Stone Golem" (Level Cap: 5)

Your cards:
- Level 8 card → treated as Level 5 (capped down)
- Level 5 card → stays Level 5
- Level 3 card → treated as Level 5 (floored up)
```

### Upgrades Capped Too

Only upgrades from levels 1 through the cap apply. If your card is level 8 but cap is 5, only upgrades from levels 1-5 are active for that battle.

### Standard Path Exception

Standard path battles have **NO level cap**. Players can grind infinitely to overlevel and trivialize standard content. This is intentional — it's the escape valve.

### UI Display

- Show **level cap** for each battle
- Show **recommended level** so players know where they stand
- Optional setting: fight standard battles WITH caps to test skill/see how you're doing against intended difficulty

---

## Decision Types

### Major Decisions (Rare)

Elite vs standard path branch points.

- High stakes: elite offers better rewards but has level caps
- Requires confidence in skill level
- Failing elite routes you to standard path (missed elite rewards)

### Minor Decisions (Regular)

Standard battles with card choices (pick 1 of 3).

- At least one option matches summoner's elemental theme
- Other options may be different elements or neutral
- Choice permanently excludes alternatives

### Filler Battles (Common)

Battles for XP and minor rewards with no real decision.

- Used to pace the campaign
- Still provide progression value
- Can be replayed for grinding

---

## Grinding and XP

### Replay Rules

| What | Available on Replay |
|------|---------------------|
| XP | Yes |
| Gold | No |
| Card rewards | No |

### XP Distribution

**Only cards IN YOUR DECK gain XP from battles.**

This means:
- Commitment to deck choices matters
- Want to level a new card? Put it in your deck and grind
- Since standard path has no cap, grinding is always possible

### Standard Path Trivializing is OK

If players overlevel standard content, that's fine. Standard path is the escape valve for players who are struggling. Elite content stays challenging due to level caps.

---

## Gold Economy

### Gold is Campaign-Scoped

Gold has **no value outside the specific campaign** it was earned in. When a campaign ends (victory or defeat), unspent gold is lost. This is intentional — it forces real economic decisions.

**Key implications:**
- No hoarding gold across campaigns
- No transferring gold between summoners
- Must spend it or lose it

### The Caravan

The Caravan is the in-campaign shop where players spend gold. Caravans appear as **visible nodes on the campaign graph**, allowing players to:

- See upcoming caravan stops before reaching them
- Plan spending decisions with full information
- Make informed economic choices, not blind gambles

### Economic Tension (Core Philosophy)

The skill of gold management lies in balancing immediate power vs. future purchases:

| Strategy | Advantage | Risk |
|----------|-----------|------|
| **Spend early** | Immediate power boost | Miss expensive items later |
| **Save up** | Access to powerful late-game items | May die before spending |

Since gold dies with the campaign, there's no "optimal" hoarding strategy. Players must commit.

### Gold Penalties as Levers

Gold loss can be used as a penalty for various events:

- Failing elite paths
- Certain story events
- Optional risk/reward choices

This is a **tunable lever**, not a hard rule. The specific penalties will be designed per-event based on desired difficulty and pacing.

### Final Spending Opportunity

The campaign ends with a **final chance to spend remaining gold**. This prevents the frustration of "died with full pockets" and ensures players can always use their earnings.

---

## Campaign Flow Example

```
[Start]
    ↓
[Tutorial Battles] - No caps, learn the game
    ↓
[First Choice Node] - Pick 1 of 3 cards (minor decision)
    ↓
[Standard Battle] - XP and minor rewards
    ↓
[Path Branch] - Elite vs Standard (major decision)
    ↓
   ╱ ╲
Elite   Standard
  ↓       ↓
(capped) (uncapped)
  ↓       ↓
[Win?]  [Progress]
  ↓       ↓
 Yes/No  [More nodes]
  ↓
[Elite rewards OR route to standard end]
```

---

## Design Rationale

### Why Level Caps?

| Problem | Solution |
|---------|----------|
| Grinding trivializes elite content | Level caps normalize card power |
| Players feel punished for leveling | Cards are floored UP too, not just down |
| Upgrade system becomes meaningless | Upgrades still matter within the cap |
| Skilled players want a challenge | Elite path provides consistent difficulty |

### Why Standard Has No Cap?

| Problem | Solution |
|---------|----------|
| Players can get stuck | Can always grind to progress |
| Less skilled players feel punished | Standard is accessible escape valve |
| No safety net | Standard path always available |

### Why Visible Caps?

| Problem | Solution |
|---------|----------|
| Difficulty is opaque | Players see exact cap before battle |
| Surprises feel unfair | No hidden scaling |
| Can't plan builds | Know exactly what levels matter |

---

## Implementation Notes

### Battle Data Structure

Each battle should include:

```
battle_id: "battle_elite_01"
level_cap: 5  # null for standard path (uncapped)
recommended_level: 5
path_type: "elite"  # or "standard"
```

### Level Cap Application

When loading a battle:
1. Read level cap from battle data
2. For each card in player deck:
   - If card level > cap: treat as cap level
   - If card level < cap: treat as cap level (floored up)
   - Apply only upgrades from levels 1 through cap
3. Display effective card levels in battle UI

### XP Distribution

After battle completion:
1. Calculate base XP from battle
2. Distribute XP only to cards that were in the deck
3. Cards not in deck receive 0 XP

---

## Related Documents

- [Campaign Narrative](narrative.md) — Story and writing guidelines
- [Card System](../cards/system.md) — Card mechanics
- [Card Progression & Economy](../../design/card-progression-economy.md) — XP and leveling
- [Shop System](../shop/requirements.md) — Caravan and shop mechanics
- [Summoner System](../summoners/README.md) — Summoner mechanics
- [Vision Document](../../project/vision.md) — Game vision and pillars

---

*Last Updated: 2026-01-19*
