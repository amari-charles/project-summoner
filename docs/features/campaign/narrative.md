# Campaign Narrative Guide

## Overview

This document preserves the narrative context and story arc for Fateforged's campaign mode. Use this as a reference when designing battles, events, and campaign content.

---

## Campaign Structure

### One Campaign, All Summoners

There is **one campaign** that all summoners play through. The campaign structure (battles, events, story beats) is identical regardless of which summoner you're playing. This is critical for development scope — we build one campaign, not one per summoner.

### What Differs: Choice Node Offers

At choice nodes where players select cards, **at least one option is guaranteed to match the summoner's elemental theme**. This ensures:
- Fire-themed summoner naturally builds toward fire without separate content
- Player still has agency (can choose to diversify into other elements)
- Different summoners experience different decks from the same campaign

**Important:** Summoners are NOT restricted to their element. A Fire-themed summoner can pick Earth cards, Water cards, etc. The theme just influences what's guaranteed to be offered, not what's allowed.

### No Runs, No Restarts

The campaign is a **one-time permanent journey** per summoner. There are no "runs" in the roguelike sense. Replayability comes from purchasing new summoners, each starting their own fresh journey.

### Exclusivity is Core

You can NEVER collect all cards on a single summoner. Every choice permanently excludes alternatives. This is the core identity of "Fateforged."

---

## Path System

For detailed path mechanics (elite vs standard, level caps, grinding), see [Campaign Structure](structure.md).

### Path Types at a Glance

| Path Type | Rewards | Level Cap | Purpose |
|-----------|---------|-----------|---------|
| **Elite** | Better rewards (better cards, traits) | Has cap (skill check) | For confident/skilled players |
| **Standard** | Lesser rewards | No cap (can grind) | Escape valve for struggling players |

### Decision Types

| Type | Frequency | Description |
|------|-----------|-------------|
| **Major decisions** | Rare | Elite vs standard path branch points |
| **Minor decisions** | Regular | Standard battles with card choices (pick 1 of 3) |
| **Filler battles** | Common | Just for XP/minor rewards, no real decision |

---

## Setting

**The Academy of Summoning Arts**

The world is a bright, welcoming magical academy that outwardly treats every student with warmth and encouragement, yet quietly operates on a ruthless meritocratic truth: only the strongest are nurtured and valued, while others are left to slip through the cracks. Strength is both a practical necessity against the threats beyond the academy and an ingrained cultural belief about whose path deserves to continue.

The Academy is overseen by Headmaster Merlin, who guides students through their journey from novice to master summoner with measured patience—though even his mentorship reflects the institution's deeper values.

### Key Locations

- **Crystal Chamber**: Where the Trial of Affinities takes place; ancient stones measure a summoner's connection to elemental forces
- **Training Grounds**: Where initiates prove their combat prowess in controlled trials
- *More locations to be added as campaign expands*

---

## Characters

### Headmaster Merlin
- **Role**: Mentor and guide to the player
- **Personality**: Wise, patient, formal but caring
- **Speech Pattern**: Formal, mystical ("initiate," "the art of summoning," "all choices endure")
- **Function**: Introduces trials, explains mechanics, provides context for challenges

### The Player
- **Role**: Initiate at the Academy of Summoning Arts
- **Journey**: Learning to master summoning, making meaningful choices, proving their worth
- **Title Progression**: Initiate → [Future titles as campaign progresses]

---

## Narrative Themes

### Core Themes
1. **Meaningful Choice**: Decisions have permanence ("what you bind, stays bound," "all choices endure")
2. **Growth Through Trial**: Progress through structured tests of skill and understanding
3. **Elemental Connection**: Affinity to elemental forces is personal and defining
4. **Mastery Through Practice**: Theory must translate to practical application

### Tone Guidelines
- **Formal but accessible**: Elevated language without being pretentious
- **Mystical academy**: Magic is real but treated as a learned art, not random
- **Warm surface, hard truths**: Outwardly supportive, but merit determines who truly advances
- **Stakes are personal**: Not world-ending, but meaningful to the player's growth and survival

---

## Campaign Progression

### Act 1: The Initiate's Path (Tutorial Arc)

**Narrative Arc**: Player arrives at the Academy, discovers their affinity, binds their first companion, and proves combat readiness through initial trials.

#### Event 1: Trial of Affinities
- **ID**: `event_affinity`
- **Type**: Special Event (Affinity Selection)
- **Narrative Function**: Establish setting, introduce Headmaster Merlin, discover elemental connection
- **Key Quote**: "Choose wisely, for in the art of summoning, all choices endure."
- **Outcome**: Player selects their elemental affinity (Fire, Water, Earth, Air, Lightning)

#### Event 2: First Summon
- **ID**: `event_first_summon`
- **Type**: Special Event (Card Selection)
- **Narrative Function**: Teach permanence of choices, introduce companion bonding concept
- **Key Quote**: "Remember, initiate: what you bind, stays bound. Choose your partner carefully."
- **Outcome**: Player selects their first card/companion from two options

#### Battle 1: The First Trial
- **ID**: `battle_00`
- **Type**: Tutorial Battle
- **Narrative Function**: Test practical application of theory; transition from selection to action
- **Context**: "Your affinity chosen, your companion bound—Headmaster Merlin now calls you to the training grounds."
- **Stakes**: Prove combat readiness and earn place among Academy summoners
- **Challenge**: Controlled combat trial with minimal opposition (single weak enemy)
- **Reward**: First tactical tool (Charge card) - teaches unit control and tactical thinking

#### Future Battles (To Be Designed)
- **battle_01**: [To be designed - likely introduces new mechanic or enemy type]
- **battle_02**: [To be designed]
- **battle_03**: [To be designed]
- **battle_04**: [To be designed]

---

## Writing Guidelines

### For Battle Names
- Keep concise (2-4 words ideal)
- Use academy/trial language: "Trial," "Test," "Proving," "Challenge"
- Reflect the mechanical focus or narrative beat
- Examples: "Trial of Affinities," "The First Trial," "Trial by Fire"

### For Battle Descriptions
- **Opening**: Reference player's progress/state ("Your affinity chosen...")
- **Context**: What's happening narratively ("Headmaster Merlin calls you to...")
- **Stakes**: Why this matters to the player ("Prove your worth...")
- **Call to Action**: End with forward momentum ("earn your place...")
- **Length**: 2-3 sentences ideal
- **Tone**: Formal but encouraging, mystical but clear

### Voice and Language
**Do Use:**
- "Initiate" (formal address for player)
- "The art of summoning" (formal reference to magic)
- "Bind/bound" (for summoning companions)
- "Trial," "test," "proving"
- "All choices endure" (permanence theme)

**Avoid:**
- Overly casual language ("Hey there!")
- Modern slang or anachronisms
- Overly dark/grimdark tone
- World-ending stakes (keep it personal)

---

## Design Notes

### Battle Difficulty Progression
- **battle_00**: Extremely gentle introduction (1 weak enemy, dies in 3 hits)
- **Future battles**: Gradual increase in complexity and challenge
- **Philosophy**: Each battle should teach one new concept while reinforcing previous lessons

### Reward Philosophy
- **Early rewards**: Focus on tactical tools and fundamental cards
- **Progression**: Teach core mechanics through rewards (tactical spells → unit combos → synergies)
- **Meaningful choices**: When offering choice rewards, options should feel meaningfully different

### Enemy Design Philosophy
- **Early enemies**: Simple, predictable, small numbers
- **Progression**: Introduce new enemy types gradually
- **Variety**: Each enemy type should teach a different lesson (melee vs ranged, fast vs tanky, etc.)

---

## Future Expansion

As the campaign grows beyond the tutorial arc, document:
- **Act 2 and beyond**: What happens after Academy graduation?
- **Antagonists**: Who opposes the player, and why?
- **World-building**: Regions beyond the Academy
- **Character development**: How does the player's title/role evolve?
- **Mechanical progression**: New systems introduced through narrative

---

## Localization Notes

All campaign text uses the localization system (`Loc.t()`):
- **Events**: `campaign.event.{event_id}.name` and `campaign.event.{event_id}.description`
- **Battles**: `campaign.battle.{battle_id}.name` and `campaign.battle.{battle_id}.description`

When adding new content, always add corresponding entries to `localization/data/en.json`.

---

## Revision History

- **2026-01-19**: Added Campaign Structure section (one campaign, choice nodes, exclusivity)
- **2025-11-20**: Initial documentation created alongside battle_00 redesign
