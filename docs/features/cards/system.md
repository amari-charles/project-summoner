# Card System API

**Status:** CURRENT
**Last Updated:** 2026-01-19

## Overview

This document defines how cards work in Fateforged, from data model to variance systems, generation rules, and lifecycle. Cards are the only way to act in battle, and all cards are single-use per match.

## Card Taxonomy

### Types

- **Unit** — Summons one entity or formation (squad)
- **Spell** — Instant or timed effect
- **Structure** — Stationary summon with HP, aura, or attack
- **Tactic** *(optional future)* — Modifies deck or hero for that match

### Typed Card Properties

Cards use strongly-typed enums for classification instead of string tags:

- **CreatureType** (flags): `Elemental | Spirit | Insect | Amphibian | Nature | Aerial`
- **SummonRole** (flags): `Swarm | Fast | Tank | Giant | Stationary`
- **SpellCategory**: `Damage | Command`
- **SpellTargeting**: `SingleTarget | AreaOfEffect | SelectionRadius`
- **CardFlags**: `DevOnly | Dummy`
- **VisualTraits**: `UsesWispVisuals` (for wisp element tinting)

## Army Rarity System

Card rarity is designed to make battles *feel* like real army warfare with clear hierarchy:

| Rarity | Units Per Card (Max Level) | Role in Army |
|--------|---------------------------|--------------|
| **Common** | 12 | Low individual impact, strength in numbers |
| **Uncommon** | 6 | Moderate impact, noticeable presence |
| **Epic** | 3 | High impact, battle-shifting |
| **Legendary** | 1 | Decisive, game-defining |

### Design Philosophy

The rarity system creates natural army composition through **spawn counts**:
- **Common cards** spawn many units (up to 12) — strength in numbers
- **Uncommon cards** spawn moderate groups (up to 6) — balanced presence
- **Epic cards** spawn small squads (up to 3) — elite forces
- **Legendary cards** spawn a single powerful unit — decisive champions

**Key principle:** Higher rarity = fewer but more impactful units per card, not deck building restrictions. A common card spawns a swarm; a legendary card spawns one game-changer.

---

## Card Instances & Deck Building

### Instance-Based Ownership

Every card in a player's collection is a **unique instance** with its own ID, even if multiple cards share the same catalog definition:

- **Card Instance** = A specific card you own (unique ID, level, XP, upgrades)
- **Catalog Card** = The template/definition (stats, abilities, art)

Example: If you own 3 Fire Elementals, you have 3 separate card instances. Each can be leveled independently.

### Deck Rules

| Rule | Description |
|------|-------------|
| **Instance Uniqueness** | Each card instance can only appear **once** per deck |
| **Cross-Deck Sharing** | The same card instance **can** be in multiple decks |
| **No Copy Limits** | No restrictions on how many cards of the same type you can put in a deck (limited only by what you own) |
| **Deck Size** | 1-30 cards per deck |

### Example

If you own:
- Fire Elemental #1 (Level 3)
- Fire Elemental #2 (Level 1)
- Fire Elemental #3 (Level 2)

You can put all 3 in the same deck. You cannot put Fire Elemental #1 in the deck twice.

---

## Card Binding & Shared Cards

### Binding Rules

Cards have different binding rules based on how they're acquired:

| Source | Binding | UI Indicator | Campaign Usable |
|--------|---------|--------------|-----------------|
| Campaign choices | Summoner-bound | (none) | Yes |
| Event rewards | Account-wide | `[Shared]` tag | No |
| Shop purchase | Account-wide | `[Shared]` tag | No |

### Summoner-Bound Cards

Cards acquired through campaign progression are **summoner-bound**:
- Part of that summoner's forged fate
- Cannot be used by other summoners
- Represent permanent choices made during the campaign

### Account-Wide (Shared) Cards

Cards acquired from events, shop, or other non-campaign sources are **account-wide**:
- Any summoner on the account can use them
- Tagged with `[Shared]` in the UI for visual distinction
- Prevents forcing players to grind events X times for X summoners

### Campaign Lock for Shared Cards

**Important:** Shared cards are **locked during campaign play**. They appear in the summoner's deck view but cannot be used in campaign battles.

Shared cards are available for:
- PvP matches
- Event battles
- Post-campaign content

This prevents trivializing campaign difficulty with farmed cards while still rewarding event participation.

### Shared Content is a Lever, Not a Rule

Making event cards account-wide `[Shared]` is an **option** we can use, not a blanket policy. Not all event content needs to be shared.

**When we use it:**
- Prevents forcing players to grind events X times for X summoners
- Use for content where multi-summoner grind would feel bad

---

## Battle Level Caps

### How Level Caps Work

All battles have a **level cap** that normalizes card power. This is transparent — players can assess difficulty before committing.

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

Standard path battles have **NO level cap**. Players can grind infinitely to overlevel and trivialize standard content. This is intentional — it's the escape valve for struggling players. Elite content stays challenging due to level caps.

### XP Distribution

**Only cards IN YOUR DECK gain XP from battles.** Cards not in your active deck receive no XP.

This means:
- Commitment to deck choices matters
- Want to level a new card? Put it in your deck and grind
- Since standard path has no cap, grinding is always possible

See [Campaign Structure](../campaign/structure.md) for full path system details.

---

## Core Balance Fields

Each card has a baseline before variance and modifiers.

### Shared Fields

- `mana_cost` (1-10 typical)
- `summon_time` (seconds) — delay before unit appears after playing card
- `rarity` (`common | uncommon | epic | legendary`)
- `element` and derived `power_rating`

### Per Type

**Unit:**
- HP, attack, attack_rate, move_speed, range
- targets (`ground | air | both`), aggro radius
- optional `on_death_effect`

**Spell:**
- effect_ref, radius, projectile_speed, duration

**Structure:**
- HP, armor, attack, attack_rate, aura_ref, duration

## Variance System — Hybrid Rarity + Variant Framework

### Philosophy

**Variants define behavior. Rarity defines expression.**

Each card archetype has multiple variants that determine what it does, and each variant can exist at any rarity, which determines how far that behavior can be pushed.

This hybrid system preserves both **horizontal diversity** and **vertical mastery**, giving players a sense of discovery and progression.

### Horizontal Variance — Functional Variants

Each card archetype can appear in multiple variants, each representing a different tactical function. Variants share the same fantasy but change playstyle.

**Example (Fireball archetype):**

| Variant | Description | Niche |
|---------|-------------|-------|
| **Focused Fireball** | Single, fast projectile | Precision burst |
| **Scatterburst** | Two splitting orbs | Area control |
| **Lingering Flame** | Leaves burning ground | Zone control |
| **Delayed Meteor** | Delayed multi-impact | Punish stationary foes |

These variants exist across all rarities — they are different cards, not tiers of one.

### Vertical Variance — Rarity Expression

Each variant can appear at any rarity. Rarity does not unlock the variant but amplifies its expression.

| Rarity | What Changes | Feel |
|--------|--------------|------|
| **Common** | Baseline stats, simple FX | Functional |
| **Rare** | Slightly refined mechanics or improved efficiency | Efficient |
| **Epic** | Variant reaches sharper extremes or gains subtle synergy | Refined |
| **Legendary** | Full expression of that variant's fantasy; may include a unique flourish | Mastered |

This means you can have a **common Scatterburst Fireball** and a **legendary Scatterburst Fireball** — same play pattern, different intensity.

### Example: Fireball Variant Grid

| Variant ↓ / Rarity → | Common | Rare | Epic | Legendary |
|----------------------|--------|------|------|-----------|
| **Focused Fireball** | baseline bolt | faster projectile | adds small splash | burst + minor stun |
| **Scatterburst** | twin short-range | wider spread | twin + small DoT | twin + flame trails |
| **Lingering Flame** | short zone | larger zone | longer duration | adds AoE slow |
| **Delayed Meteor** | single drop | shorter delay | adds shockwave | multi-meteor storm |

Every cell represents a valid card roll.

### Supporting Variance Layers

Variants and rarity form the foundation, but each card also has **micro variance** layers for individuality:

| Layer | Description | Impact |
|-------|-------------|--------|
| **Stat Variance** | Minor numeric drift around baseline values | Feel difference |
| **Effect Variance** | Small micro-modifiers (e.g., +1 chain, short burn) | Behavioral nuance |
| **Visual Variance** | Tint, aura, particle tweak | Cosmetic identity |

These stack with the variant/rarity system to create endless individuality without chaos.

### Summary

- Variants = What the card does (horizontal difference)
- Rarity = How far that variant can go (vertical mastery)
- Micro-variance adds texture within that framework
- Players chase both **new expressions** (discovering variants) and **refinement** (upgrading their favorite ones)
- The system supports fate, asymmetry, mastery, and individuality all at once

## Effects System (Compositional)

Effects are **data-driven payloads** attached to cards. These define primary and secondary behaviors, scaled by hero affinity and stats.

## Generation Rules (Drops & Crafting)

1. Roll archetype → variant → rarity → stat/effect/visual variance
2. Player chooses to keep, dismantle, or transmute new cards
3. Higher rarities deepen existing play patterns rather than replace them

## Player Experience Goals

- Discover horizontal variants (new playstyles)
- Master vertical rarity paths (stronger versions of favorite variants)
- Every card feels handcrafted — no duplicates, no grind
- Players develop emotional attachment to their army through uniqueness and expression

## Summon Time Mechanics

When playing a card, there's a delay before the unit appears:

1. Player drags card to battlefield location
2. **Casting begins** — player sees circular cooldown indicator
3. **Summoning circle VFX** appears at spawn location
4. Player cannot play other cards during summon time
5. After delay, unit spawns

### Summon Time by Rarity (Typical)

| Rarity | Typical Summon Time |
|--------|---------------------|
| Common | 0.5 - 1.0 seconds |
| Uncommon | 1.0 - 1.5 seconds |
| Epic | 1.5 - 2.0 seconds |
| Legendary | 2.0 - 3.0 seconds |

**Design Intent:** Adds weight to summoning powerful units. Creates anticipation and windows for counterplay.

---

## Multi-Unit Spawn Formations

When a card spawns multiple units (e.g., Fire Elemental Swarm with 12 units), they form a **staggered row formation** around the target position.

### Formation Layout

- Units arrange in a grid pattern with 2 rows for groups up to 20 units
- Larger swarms (20+) automatically expand to more rows
- Alternating rows are offset (brick pattern) for visual appeal and collision avoidance

### Formation Constants

| Constant | Value | Description |
|----------|-------|-------------|
| `FORMATION_SPACING` | 1.8 | Distance between units (world units) |
| `FORMATION_ROW_OFFSET` | 0.5 | Stagger offset for alternating rows (fraction of spacing) |

### Example: 12-Unit Swarm

```
Row 1:  O   O   O   O   O   O     (6 units)
Row 0:    O   O   O   O   O   O   (6 units, staggered)
```

**Design Intent:** Creates army-like formations that look organized and spread out naturally, avoiding the chaotic overlap of random clump spawning.

---

## Implementation Status

**Current:** Card system with rarity, summon times, and drag-and-drop
**Planned:** Variance system, crafting, visual variance

---

*Related Documents:*
- [Combat System](../combat/system.md)
- [Summoner System](../summoners/README.md)
- [Item System](../items/system.md)
- [Card Progression & Economy](../../design/card-progression-economy.md)
- [Current State](../../project/current-state.md)
