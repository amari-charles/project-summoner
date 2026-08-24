# Fateforged — Vision Document

**Status:** CURRENT
**Last Updated:** 2026-01-19

## Game Premise

Fateforged is a 1v1 real-time tactical battler where players summon elemental creatures to fight for them. Throughout the campaign, players encounter choice nodes that offer small sets of cards. Choosing one card **permanently closes off the others forever** for that summoner. Because the campaign has only a limited number of these junctions, every summoner's deck naturally diverges in composition and strategy.

This asymmetry — and the player's responsibility for shaping it — is the core of the game's identity and the reason it's called **Fateforged**: your fate is literally forged by the choices you make at each branching point.

## Campaign Structure

### One Campaign, All Summoners

There is **one campaign** that all summoners play through. The campaign structure (battles, events, story beats) is identical regardless of which summoner you're playing. What differs is what choices are offered at each node based on the summoner's elemental theme.

### No Runs, No Restarts

The campaign is a **one-time permanent journey** per summoner. There are no "runs" in the roguelike sense. You play through once, your choices permanently shape your collection, and that's your forged fate.

**Replayability comes from purchasing new summoners**, each of which starts their own fresh journey through the campaign with different elemental themes and therefore different card offers.

### Exclusivity is Core

You can NEVER collect all cards on a single summoner. Every choice excludes alternatives permanently. This is non-negotiable — it's the core identity of the game.

## Core Fantasy: Army Warfare

You are a **wizard-commander**, leading a living army of summons and spells into battle. The fantasy is **two armies clashing on a battlefield** — not a Clash Royale-style lane pusher, but the feel of real warfare with formations, tactics, and decisive moments.

### Battle Flow

Each match follows a two-phase structure that reinforces the army fantasy:

1. **PREPARATION (30 seconds):** Both commanders have their full mana pool. Summon units to build your army formation. Units spawn but remain inactive — planning before the clash.

2. **BATTLE (until victory):** All units activate and charge. The armies collide. Commanders can still summon reinforcements, but the initial formation often decides the outcome.

### The Summoner on the Battlefield

Victory requires defeating the enemy **summoner** directly. The summoner is physically present on the battlefield — not commanding from afar, but standing with their army. This creates real stakes: when you lose, YOU lost, not some projection of you.

This personal presence makes every battle feel meaningful, whether it's a duel, sparring match, or all-out war.

### Why This Design?

| Old Approach | Problem | New Approach |
|--------------|---------|--------------|
| Mana regenerates over time | Felt like a trickle, reactive gameplay | Fixed mana pool — all resources available upfront for strategic planning |
| Units spawn and fight immediately | No army-building fantasy, just constant skirmishing | Preparation phase lets you build formations before battle |
| Instant card plays | No weight to summoning powerful units | Summon times add anticipation and counterplay |
| Base/Nexus as target | Arbitrary structure with no narrative meaning | Summoner on the battlefield — defeat the enemy directly |

You win not by outspending, but by **out-summoning**: using timing, positioning, and courage to turn your finite resources into victory. Every choice you've made to build that army matters.

## Design Pillars

### 1. Real-Time Strategy on One Screen

- 3-5-minute duels on a fixed horizontal battlefield
- Pannable camera with boundary constraints
- Pure tactical tension

### 2. Every Card Counts

- Single-use cards — every deployment matters
- Player-built decks contain up to 12 cards, keeping every inclusion and deployment consequential
- Fixed mana pool forces upfront strategic decisions

### 3. Army Hierarchy Through Rarity

Card rarity creates natural army composition that *feels* like real warfare through **spawn counts**:

| Rarity | Units Per Card | Role in Army |
|--------|---------------|--------------|
| **Common** | up to 12 | Swarms with low individual impact, strength in numbers |
| **Uncommon** | up to 6 | Moderate squads, noticeable presence |
| **Epic** | up to 3 | Elite forces, battle-shifting |
| **Legendary** | 1 | Single decisive champion, game-defining |

*Key principle: Higher rarity = fewer but more impactful units per card. A common card spawns a swarm; a legendary card spawns one game-changer.*

### 4. Asymmetric Summoners & Choice

- Collectable summoners with unique mana pools, affinities, and growth potential
- First summoner chosen by player during onboarding to create a unique journey

### 5. Meaningful Risk, Earned Reward

- Optional wagers with emotional stakes
- Power variance matters long-term but never decides early matches

### 6. Collection Pride & Personal Growth

- Rarity equals *potential*, not instant power
- Even common summoners can become legends

## Unique Selling Points

- **Single-use deck system** — Deep tactical decisions unlike any other mobile battler
- **Two-phase battle system** — Preparation + Battle creates real army warfare feel
- **Summoners on the battlefield** — Fight alongside your army, real stakes
- **Optional wagers** — Emotional stakes without gambling
- **Player-chosen starting hero** — Choose your path or embrace randomness for bonus rewards
- **Fast, pannable RTS feel** — Real-time readability built for mobile and desktop

## Tone & Emotion

Competitive yet **mythic** — *Clash Royale meets Hades*.

High-contrast fantasy with distinct elemental identities. Serious, mystical, and proud.

## High-Level Structure

- **Core Loop:** Collect → Build → Battle → Reward → Evolve
- **Session Length:** 3-5 minutes
- **Monetization:** Cosmetics and hero unlocks (no pay-to-win)
- **Platform:** Mobile-first, expandable to PC
- **Engine:** Godot 4.5

## Vision Summary

**Fateforged** is a competitive, emotionally charged dueling game where individuality is built into the rules.

Every match is different. Every army is unique. Every victory is personal.

---

*Related Documents:*
- [Todos](../tracking/todos.md)
- [Current State](current-state.md)
- [Visual Style References](../art/visual-style-references.md)
- [Item System](../features/items/system.md)
- [Summoner System](../features/summoners/README.md)
- [Card System](../features/cards/system.md)
