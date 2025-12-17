# Fateforged — Vision Document

**Status:** CURRENT
**Last Updated:** 2025-12-16

## Game Premise

Fateforged is a 1v1 real-time tactical battler where players summon elemental creatures to fight for them. Throughout the campaign, players encounter finite, non-replayable events that offer small sets of cards. Choosing one card permanently closes off the others for that run, and because the campaign has only a limited number of these junctions, every player's deck naturally diverges in composition and strategy.

This asymmetry — and the player's responsibility for shaping it — is the core of the game's identity and the reason it's called **Fateforged**: your fate is literally forged by the choices you make at each branching point.

## Core Fantasy: Army Warfare

You are a **wizard-commander**, leading a living army of summons and spells into battle. The fantasy is **two armies clashing on a battlefield** — not a Clash Royale-style lane pusher, but the feel of real warfare with formations, tactics, and decisive moments.

### Battle Flow

Each match follows a two-phase structure that reinforces the army fantasy:

1. **PREPARATION (30 seconds):** Both commanders have their full mana pool. Summon units to build your army formation. Units spawn but remain inactive — planning before the clash.

2. **BATTLE (until victory):** All units activate and charge. The armies collide. Commanders can still summon reinforcements, but the initial formation often decides the outcome.

### The Incarnation

Victory requires destroying the enemy's **Incarnation** — the summoner's magical presence on the battlefield. It's not the summoner themselves (they command from elsewhere), but a projection of their power. Breaking it severs their connection to this battle.

This works for any battle context: duels, sparring matches, or all-out war.

### Why This Design?

| Old Approach | Problem | New Approach |
|--------------|---------|--------------|
| Mana regenerates over time | Felt like a trickle, reactive gameplay | Fixed mana pool — all resources available upfront for strategic planning |
| Units spawn and fight immediately | No army-building fantasy, just constant skirmishing | Preparation phase lets you build formations before battle |
| Instant card plays | No weight to summoning powerful units | Summon times add anticipation and counterplay |
| Base/Nexus as target | Arbitrary structure with no narrative meaning | Incarnation — the summoner's magical presence on the field |

You win not by outspending, but by **out-summoning**: using timing, positioning, and courage to turn your finite resources into victory. Every choice you've made to build that army matters.

## Design Pillars

### 1. Real-Time Strategy on One Screen

- 3-5-minute duels on a fixed horizontal battlefield
- Pannable camera with boundary constraints
- Pure tactical tension

### 2. Every Card Counts

- Single-use cards — every deployment matters
- Decks up to 30 cards, creating pacing from skirmish to all-out war
- Fixed mana pool forces upfront strategic decisions

### 3. Army Hierarchy Through Rarity

Card rarity creates natural army composition that *feels* like real warfare:

| Rarity | Max Copies | Role in Army |
|--------|-----------|--------------|
| **Common** | 12 | Low individual impact, strength in numbers |
| **Uncommon** | 6 | Moderate impact, noticeable presence |
| **Epic** | 3 | High impact, battle-shifting |
| **Legendary** | 1 | Decisive, game-defining |

*Key principle: Higher rarity = more individual impact, not just bigger stats. A common unit can be frontline or backline — they're just not individually decisive.*

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
- **Summoners as commanders** — Magical presence projected onto the battlefield
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
- [Roadmap](roadmap.md)
- [Current State](../current-state.md)
- [Visual Style References](visual-style-references.md)
