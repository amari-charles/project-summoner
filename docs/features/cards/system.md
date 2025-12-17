# Card System API

**Status:** CURRENT
**Last Updated:** 2025-12-16

## Overview

This document defines how cards work in Fateforged, from data model to variance systems, generation rules, and lifecycle. Cards are the only way to act in battle, and all cards are single-use per match.

## Card Taxonomy

### Types

- **Unit** — Summons one entity or formation (squad)
- **Spell** — Instant or timed effect
- **Structure** — Stationary summon with HP, aura, or attack
- **Tactic** *(optional future)* — Modifies deck or hero for that match

### Tags (Multi-Select)

Tags drive synergy and affinity bias:

- **Element:** `fire | water | nature | storm | earth | neutral`
- **Role:** `tank | assault | support | ranged | air | structure | spell`
- **Family:** e.g., `pyre`, `thorn`, `wisp`
- **Mechanics:** `burn | freeze | heal | shield | root | silence | dash | stealth | summon_on_death | lifesteal | taunt`

## Army Rarity System

Card rarity is designed to make battles *feel* like real army warfare with clear hierarchy:

| Rarity | Max Copies in Deck | Role in Army |
|--------|--------------------|--------------|
| **Common** | 12 | Low individual impact, strength in numbers |
| **Uncommon** | 6 | Moderate impact, noticeable presence |
| **Epic** | 3 | High impact, battle-shifting |
| **Legendary** | 1 | Decisive, game-defining |

### Design Philosophy

The rarity system creates natural army composition:
- **Many units** that collectively matter but individually don't decide battles
- **Fewer units** with real individual impact
- **One or two key pieces** that demand attention

**Key principle:** Higher rarity = more individual impact and battlefield influence, not just bigger stats. A common unit can be frontline melee or backline support — they're just not individually decisive.

---

## Core Balance Fields

Each card has a baseline before variance and modifiers.

### Shared Fields

- `mana_cost` (1-10 typical)
- `summon_time` (seconds) — delay before unit appears after playing card
- `rarity` (`common | uncommon | epic | legendary`)
- `element`, `tags`, and derived `power_rating`

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

## Implementation Status

**Current:** Card system with rarity, summon times, and drag-and-drop
**Planned:** Variance system, crafting, visual variance

---

*Related Documents:*
- [Combat System](../combat/system.md)
- [Summoner System](../summoners/README.md)
- [Current State](../current-state.md)
