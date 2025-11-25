# HERO PROGRESSION SYSTEM — FINAL DESIGN SPEC
Hero Leveling (1-10), Traits (Story + Level + Ultimate), Boons, and Global Event Cards

**Version:** 2.0
**Date:** 2025-01-24
**Status:** DESIGN SPEC (Not Yet Implemented)

---

## 1. Overview

This document defines the persistent progression system for heroes in Project Summoner, including:

- **Hero Leveling** (Levels 1-10 with XP progression)
- **Level Traits** (9 traits chosen at levels 1-9, following Trait Lines)
- **Ultimate Traits** (Powerful active abilities unlocked at level 10)
- **Story Traits** (Permanent narrative consequences from campaign events)
- **Boons** (Slotted mechanical bonuses with 3 default slots)
- **Global Event Cards** (Account-wide rewards)
- **Hero-bound decks** (Each hero maintains their own deck)
- **Hero unlock rules** (Progressive unlocking through gameplay)
- **Campaign replay design** (Different story paths for different heroes)

### Core Philosophy

**Heroes are identity-bearing, semi-permanent characters.**

Heroes should feel like long-term "files" the player invests in, not disposable runs. Each hero:
- Has an elemental affinity (Fire, Water, Wind, Earth, etc.)
- Has their own deck (decks are hero-bound)
- Accumulates **Traits** (permanent identity)
- Equips **Boons** (swappable power choices)
- Becomes harder to obtain as you unlock more

This system balances:
- Meaningful narrative consequences
- Replayability
- Long-term hero identity
- Fairness to new heroes
- Sustainable power growth
- Global progression without power creep

**Every piece of this design was chosen to resolve contradictions in earlier models while maximizing player satisfaction and longevity.**

---

## 2. Goals of the System

The progression system aims to:

### 2.1 Provide structured hero growth (Levels 1-10)
Heroes level through battles and events, unlocking trait choices at each level.

### 2.2 Create meaningful build choices (Level Traits + Trait Lines)
Players choose traits at each level, following prerequisite chains for long-term planning.

### 2.3 Deliver capstone hero fantasy (Ultimate Traits)
Level 10 unlocks a powerful signature active ability unique to each hero.

### 2.4 Support meaningful, permanent choices (Story Traits)
Heroes can gain permanent positive or negative traits based on story decisions.

### 2.5 Reward replaying the campaign with different heroes
Every hero carves a unique story and build path through different trait combinations.

### 2.6 Prevent power creep (Slot-Limited Boons)
Boons (the power-granting layer) are slot-limited to 3-5 slots maximum.

### 2.7 Avoid punishing players for trying new heroes
New heroes start fresh at level 1 but benefit from global event cards.

### 2.8 Keep event/campaign rewards exciting (Global Event Cards)
Event Cards are global, not hero-bound, avoiding repetition fatigue.

### 2.9 Keep the system scalable long-term
Level cap (10), boon slots (3-5), and trait separation prevent runaway growth.

---

## 3. Core Concepts

This system splits persistent hero progression into **three layers**:

⭐ **A. TRAITS** (Permanent Identity)
⭐ **B. BOONS** (Slotted Power)
⭐ **C. HERO LEVELS** (Progression Framework)

Event cards are handled separately (global).

### The Critical Distinction

**Traits = Story Expression** (identity, consequences, access)
**Boons = Power Expression** (build tuning, mechanical choices)
**Levels = Growth Framework** (structured progression, trait unlocks)

- **Builders** tune their Boons
- **Storytellers** earn their Traits
- **Progressors** level their Heroes

This creates a clean separation between:
- **Who a hero is** (Traits) — unlimited accumulation
- **What a hero can do** (Boons) — slot-limited choices
- **How a hero grows** (Levels) — structured advancement
- **What the player account has earned globally** (Event Cards) — profile-level rewards

---

## 3.5. HERO LEVELS AND XP

### 3.5.1 Level Progression

**Heroes level from 1 to 10.**

- **Starting Level:** 1
- **Maximum Level:** 10
- **XP Sources:** Battles, campaign events, challenges
- **Level Cap:** No prestige or level reset — 10 is the endgame

### 3.5.2 What Leveling Unlocks

**Levels 1-9:** Unlock **Level Traits**
- At each level (1-9), the player chooses ONE trait from a curated list
- Each level offers ~3-5 trait options
- Options are drawn from **Trait Lines** (prerequisite chains)
- Players build toward more powerful traits by following trait lines

**Level 10:** Unlocks **Ultimate Trait**
- The Ultimate Trait is a powerful, active ability tied to hero affinity
- Acts as the hero's "signature ultimate"
- Only one Ultimate Trait per hero (chosen from 2-3 options)

### 3.5.3 Level Traits vs Story Traits

**Level Traits = Earned through XP progression**
- Chosen by player at level-up
- Predictable, curated lists
- Build-focused, mechanical benefits
- Follow Trait Lines with prerequisites

**Story Traits = Earned through narrative choices**
- Acquired from campaign events
- Unpredictable, consequence-driven
- Often mixed (positive + negative)
- Unlock/block affinity paths

**Both are permanent and hero-bound.**
- **Level Traits** are capped by levels (9 total: one per level 1-9).
- **Story Traits** are not explicitly capped, but in practice are limited by how many story events a hero can encounter.
- Both contribute to hero identity.

The separation allows:
- **Mechanical progression** (Level Traits) to be structured and reliable
- **Narrative progression** (Story Traits) to be surprising and consequential

### 3.5.4 Trait Lines

**Trait Lines = Prerequisite chains that guide trait selection.**

Example Trait Line (Fire Affinity):
```
Pyromancy I
  ↓ Requires Pyromancy I
Pyromancy II
  ↓ Requires Pyromancy II
Pyromancy III
  ↓ Requires Pyromancy III
Inferno Mastery
```

Example Trait Line (Universal):
```
Mana Battery I
  ↓ Requires Mana Battery I
Mana Battery II
  ↓ Requires Mana Battery II
Mana Battery III
```

**Trait Lines are gated by prerequisites only, not by specific levels.**
At each level-up (1-9), the player may choose any Level Trait for which they meet the prerequisites.

**Why Trait Lines?**
- Create long-term build planning
- Reward commitment to a strategy
- Provide clear progression fantasy
- Enable powerful endgame traits without frontloading power
- Allow players to hybridize (split between multiple trait lines)

### 3.5.5 XP and Pacing

**XP is hero-specific.**
- Each hero levels independently
- No shared XP pool across heroes
- XP is NOT a global account resource

**Expected pacing:**
- Levels 1-5: Fast progression (teach the system)
- Levels 6-9: Moderate progression (build commitment)
- Level 10: Major milestone (ultimate unlock)

Exact XP curves TBD during implementation.

---

## 4. TRAITS

### 4.1 What Traits Are

**Traits = Permanent, hero-bound identity markers.**

Traits represent permanent additions to a hero's identity. They come in two forms:

**Level Traits** (Section 3.5.3)
- Earned through XP progression (levels 1-9)
- Chosen by player from curated lists
- Primarily mechanical and build-focused
- Follow Trait Lines with prerequisites

**Story Traits** (This section)
- Earned through campaign narrative choices
- Acquired from story events and decisions
- Can be positive, negative, or mixed
- Unlock/block affinity paths and content

**Ultimate Traits** (Section 4.9)
- Earned at level 10
- Powerful active abilities
- Hero's signature ultimate

All trait types are **permanent, unlimited, and contribute to hero identity**.

### 4.2 Story Trait Characteristics

Story Traits represent irreversible narrative consequences.

They can be:
- Positive
- Negative
- Mixed (positive + negative effects)
- Unlocking affinities
- Altering story routes
- Cosmetic identifiers
- Reputation / alignment / corruption markers

### 4.3 Story Trait Acquisition

Story Traits are acquired through:
- Major story decisions
- Corrupted events
- Powerful blessings
- Rare branching narrative moments
- Starting hero selection (e.g., "Fortune Favors the Bold")

**Expected accumulation:** ~5 story traits over a full campaign run (varies by choices). There is no explicit cap on Story Traits; this ~5 value is a practical expectation per full campaign, based on available events.
**Expected accumulation:** ~9 level traits (one per level 1-9).
**Expected accumulation:** 1 ultimate trait (at level 10).

### 4.4 All Traits (Story + Level + Ultimate) NEVER:
- ❌ Count toward Boon slot limits
- ❌ Get removed (unless specific story events allow)
- ❌ Rotate or expire
- ❌ Get balanced around swapping
- ❌ Apply to other heroes
- ❌ Become global

**They belong ONLY to the hero who earned them.**

**Trait Limitations:**
- Level Traits are limited to 9 (one per level 1-9) plus 1 Ultimate at level 10.
- Story Traits have no explicit cap but are practically limited by campaign content.

### 4.5 All Traits (Story + Level + Ultimate) DO:
- ✅ Stack over time (within their respective limits)
- ✅ Create long-term identity through synergy
- ✅ Unlock/block certain Boons (e.g., "Occult Initiate" unlocks occult boons)
- ✅ Unlock card pools and affinity access
- ✅ Alter campaign events and story branches (mostly Story Traits)
- ✅ Permanently modify story outcomes (mostly Story Traits)
- ✅ Give percentage modifiers and mechanical effects
- ✅ Sometimes modify maximum Boon slot count (+1 or +2 from very rare traits)

### 4.6 Trait Interactions with Boons

Traits can unlock or block Boon families:

**Examples:**
- **Purified Soul** → Blocks occult boons
- **Occult Initiate** → Unlocks occult boons
- **Naturebound** → Unlocks growth boons, blocks mechanical boons
- **Cold Soul** → Blocks fire boons, unlocks frost boons

This ensures tradeoffs and asymmetry between heroes.

### 4.7 Trait Synergy and Identity

**Trait stacking is intended, not avoided.**

Heroes will accumulate many traits over time (5 story + 9 level + 1 ultimate = ~15 total). Trait sets create emergent identity:

- **Fire hero** stacked with fire level traits + fire story traits → Pyromaniac identity
- **Corrupted hero** stacked with occult level traits + occult story traits → Dark summoner identity
- **Nature hero** stacked with regeneration level traits + growth story traits → Druidic identity

This synergy is the core long-term fantasy of hero progression.

### 4.8 Trait Lines (Level Traits)

**Trait Lines = Prerequisite chains for level traits.**

See Section 3.5.4 for detailed explanation.

Trait Lines guide level-up choices and create long-term build planning. Each trait line offers increasing power at higher levels, rewarding commitment to a strategy.

**Example progression:**
- Early level: Choose Pyromancy I (fire damage bonus)
- Later level: Choose Pyromancy II (requires Pyromancy I)
- Another later level: Choose Pyromancy III (requires Pyromancy II)
- Final level before 10: Choose Inferno Mastery (requires Pyromancy III)

Players can hybridize by splitting points between multiple trait lines, or specialize by committing to one line for maximum power.

### 4.9 Ultimate Traits (Level 10)

**Ultimate Traits = Powerful active abilities unlocked at level 10.**

Unlike other traits (which are passive), Ultimate Traits are **active abilities** that function like signature moves.

**Characteristics:**
- Unlocked at level 10 only
- Active abilities (player-triggered, not passive)
- Tied to hero affinity (Fire, Water, Wind, Earth, etc.)
- Only one Ultimate Trait per hero
- Player chooses from 2-3 ultimate options at level 10
- Cannot be changed once selected

**Ultimates are chosen at level 10 from 2-3 options, and the chosen Ultimate is permanent for that hero.**

**Example Ultimates (Fire Affinity):**
- **Phoenix Rebirth**: Once per battle, resurrect all dead units at 50% HP
- **Inferno Nova**: Deal massive AoE damage to all enemy units
- **Flamestrike**: Summon a powerful fire elemental for 30 seconds

Ultimate Traits define the hero's "final form" and provide a capstone fantasy for the progression journey.

### 4.10 Example Story Traits

**Tainted Blood**
Permanent HP reduction, unlocks occult path.

**Fortune Favors the Bold**
Story Trait granted only when the player chooses "Random Hero" at start. Permanent, hero-bound trait applied to that starting hero.

**Marked by the Phoenix**
Alters dialogue, unlocks Phoenix events.

**Oathbreaker**
Adds new confrontation events, restricts some sanctified paths.

**Occult Initiate**
Unlocks occult boon family and occult cards. May have negative reputation effects.

**Purified Soul**
Blocks all occult content, unlocks sanctified boons.

**Naturebound**
+10% unit regeneration, unlocks growth boons, blocks mechanical boons.

### 4.11 Example Level Traits

**Pyromancy I** (Level 1)
+5% fire damage. Prerequisite for Pyromancy II.

**Mana Battery I** (Level 1)
+10% max mana. Prerequisite for Mana Battery II.

**Swift Summoner I** (Level 2)
+5% summon speed. Prerequisite for Swift Summoner II.

**Pyromancy II** (Level 3)
+10% fire damage. Requires Pyromancy I. Prerequisite for Pyromancy III.

**Regenerative Field I** (Level 4)
Units regenerate 1% HP per second. Prerequisite for Regenerative Field II.

**Inferno Mastery** (Level 9)
+25% fire damage, fire units gain immunity to burn. Requires Pyromancy III.

---

## 5. BOONS

### 5.1 What Boons Are

**Boons = Slotted, mechanical, build-defining power bonuses.**

Boons are not identity — they are loadout choices.

They provide:
- Numerical buffs
- Temporary enhancements
- Synergy power
- Combat properties
- Stat tuning
- Build archetypes

### 5.2 Boon Slot System

Heroes have:
- **Default: 3 active Boon slots**
- Very rare traits can grant +1 or +2 additional slots (max ~5 slots total)

**Only boons in active slots apply their effects.**

All other boons remain inactive but available for swapping.

Boons can be swapped **outside combat** (e.g., in menu, before battle).

### 5.3 Why Boons Are Slotted

Slotting ensures:
- ✅ No infinite stacking
- ✅ No runaway power creep
- ✅ Old heroes do not become unbeatable
- ✅ New heroes can catch up with a few key boons
- ✅ Balance stays manageable
- ✅ Build depth stays high

### 5.4 Boon Acquisition

Boons are unlocked through:
- Progression milestones
- Campaign events
- Affinity routes
- Trait conditions (some traits unlock boon families)
- Achievements and challenges

### 5.5 Boons ARE:
- ✅ Removable and swappable (outside combat)
- ✅ Earned through gameplay
- ✅ Primarily mechanical effects
- ✅ The main balancing surface
- ✅ Can give percentage modifiers and strong effects
- ✅ Constrained by slot count

### 5.6 Boons ARE NOT:
- ❌ Tied to story identity (that's traits)
- ❌ Tied to alignment or narrative
- ❌ Permanent narrative consequences
- ❌ Negative (usually — they're power choices)
- ❌ Unlimited (slot-capped)

### 5.7 Example Boons

**Phoenix Pact**
Units revive once with 20% HP. (Requires Fire affinity or Phoenix trait)

**Arcane Rush**
+20% mana regeneration rate.

**Gale Acceleration**
+15% summon speed, units spawn faster. (Requires Wind affinity)

**Stoneform**
Units gain +10 armor on spawn. (Requires Earth affinity)

**Occult Sacrifice**
When a unit dies, gain +1 mana. (Requires Occult Initiate trait)

**Nature's Bounty**
Units regenerate 2% HP per second. (Requires Life affinity or Naturebound trait)

---

## 6. DECK STRUCTURE

### 6.1 Decks Are Hero-Bound

**Each hero maintains their own deck.**

Starting a new hero = fresh deck creation.

**Why?**
- Supports identity-driven progression
- Allows campaign choices to shape deck contents
- Ensures replayability
- Supports branching paths and unique reward trees
- Heroes are meant to be commitments, not disposable runs

### 6.2 Deck Acquisition

A hero gains cards from:
- Campaign arc decisions
- Non-repeatable branches
- Affinity unlocks (via traits)
- Hero-specific events
- General card drops
- Crafting systems (optional)
- **Global event cards** (profile-level unlocks)

---

## 7. GLOBAL EVENT CARDS

### 7.1 What Event Cards Are

**Event Cards = Special, rare, account-wide rewards earned from major game events or story milestones.**

These include:
- Finishing major boss fights
- Clearing large regions
- Completing weekly/seasonal challenges
- Winning special tournaments
- High-difficulty feats

### 7.2 Why They Must Be Global

This solves major design problems:

✔ **Prevents repetition fatigue**
Players don't redo the same events 3+ times to unlock the same card.

✔ **Makes new heroes viable**
A new hero starts with access to global event cards.

✔ **Preserves world progression**
Your profile's world has moved forward; all heroes benefit.

✔ **Keeps event cards exciting**
They don't feel like mundane, repeatable tasks.

---

## 8. CAMPAIGN STRUCTURE AND REPLAY

### 8.1 Campaign Design

**The campaign is NOT "4 separate elemental campaigns."**

It is **one world** with regions, routes, and branching paths.

Players can:
- Bring any hero into any region
- Gain traits based on their choices
- Unlock boons and affinity bonuses
- Accumulate identity over time
- Make different choices with different heroes

### 8.2 Campaign Replay

**Campaign replay is a feature, not a punishment.**

When starting a new hero:
- The world is the same, but your story differs
- Different traits lead to different events
- Different affinities unlock different paths
- Event Cards are already unlocked (global)
- Fresh deck building experience
- New narrative branches and outcomes

### 8.3 Why This Structure Works

✔ **Replayability:** Each hero carves a unique path
✔ **Agency:** Player choices shape hero identity
✔ **Freshness:** New heroes feel distinct
✔ **Respect:** Event Cards prevent repetition
✔ **Investment:** Heroes accumulate meaningful history

---

## 9. WHAT PROBLEMS THIS SYSTEM SOLVES

This section captures the design reasoning explicitly — critical for future contributors.

### 9.1 Provides Clear Hero Progression (Leveling System)
- Level 1-10 gives players a clear progression path
- XP from battles/events creates tangible sense of advancement
- Level cap prevents endless grinding
- Predictable structure makes progression feel fair

### 9.2 Creates Build Depth (Level Traits + Trait Lines)
- 9 trait choices (levels 1-9) create meaningful build diversity
- Trait Lines with prerequisites reward long-term planning
- Players can specialize (commit to one line) or hybridize (split between lines)
- Build decisions feel impactful without being overwhelming

### 9.3 Delivers Capstone Fantasy (Ultimate Traits)
- Level 10 ultimate provides clear endgame goal
- Active abilities give heroes distinct playstyles
- Choosing between 2-3 ultimates creates meaningful final decision
- Provides satisfying "final form" moment

### 9.4 Prevents Power Creep (Slot-Limited Boons + Level Cap)
- Slot-limited Boons prevent heroes from stacking unlimited buffs
- Level cap (10) prevents endless trait accumulation
- Traits are unlimited but balanced (most are % modifiers, not flat)

### 9.5 Prevents New-Hero Punishment
- Traits are hero-specific, but Event Cards are global — new heroes aren't 30 cards behind
- New heroes start at level 1, but leveling is fast early on
- Boon slots are capped (3-5), so old heroes don't become unstoppable
- New heroes start clean (no bad traits from previous heroes)
- New heroes always start with 0 Level Traits and no Story Traits, but can gain Story Traits and Level Traits over time just like the first hero

### 9.6 Enables Meaningful Story Decisions (Story Traits)
- Story Traits being permanent and immutable make choices matter
- Separate from leveling system, so narrative consequences don't feel like "missed power"
- Negative traits don't prevent level-up choices

### 9.7 Enables Replayability
Because:
- Level Traits chosen differently on each hero
- Story Traits are hero-bound
- Decks are hero-bound
- Campaign choices differ
- Different affinities unlock different trait lines and content

**Every hero's story is unique.**

### 9.8 Avoids Repetition Fatigue (Global Event Cards)
Global Event Cards remove the need to grind same content for multiple heroes.

### 9.9 Supports Narrative Consequences Without Mechanical Punishment
- Negative Story Traits don't prevent Level Trait choices
- Negative traits don't enter Boon slot limits
- Negative traits don't ruin new heroes (hero-bound)
- Many traits are identity/story, not pure power

### 9.10 Supports Build Crafting (Swappable Boons)
Boons can be swapped freely outside combat, giving players control over power expression.

### 9.11 Supports Long-Term Scalability
- Can add new trait lines without affecting existing heroes
- Can add new boons, story traits, or heroes without breaking ecosystem
- Level cap (10) prevents infinite scaling
- Trait Lines are modular and expandable

### 9.12 Supports Hero Identity Through Trait Stacking
- 15 total traits (5 story + 9 level + 1 ultimate) create rich emergent identity
- Trait synergy builds fantasy without infinite power
- Level Traits + Story Traits combine for unique builds

---

## 10. HERO UNLOCK STRATEGY

Heroes must be:
- Rare
- Meaningful
- Expensive
- Part of progression

### Guidelines:
- Start with **1 hero** (chosen during onboarding)
- Unlock second hero at **major arc completion**
- Subsequent heroes require **rarer feats or substantial investment**
- Maximum reasonable heroes: **3–5 in early game**

This ensures each hero feels like a genuine build.

---

## 11. DATA MODEL (Simplified)

### Profile
```json
{
  "unlocked_heroes": ["hero_fire", "hero_water"],
  "global_event_cards": ["card_occult_ascension", "card_phoenix_blessing"],
  "cosmetics": [],
  "unlocked_hero_slots": 2
}
```

**Note on Hero IDs:**
- **MVP Phase**: Uses template IDs (`"hero_fire"`, `"hero_water"`) in `unlocked_heroes` array
  - Simpler model: one instance per hero template
  - Profile tracks which hero templates are unlocked
- **Post-MVP (Future)**: May use instance IDs (`"hero_fire_001"`, `"hero_fire_002"`) if supporting multiple instances per template
  - Allows players to have multiple Fire heroes with different builds
  - More complex, but enables greater customization
  - This is an expected evolution of the data model

For now, **use template IDs** as shown above to match `architecture.md`.

### Hero
```json
{
  "id": "hero_fire",
  "affinity": "fire",

  "level": 5,
  "xp": 2400,
  "xp_to_next_level": 3000,

  "story_traits": [
    "tainted_blood",
    "fortune_favors_the_bold",
    "occult_initiate"
  ],

  "level_traits": [
    "pyromancy_i",
    "pyromancy_ii",
    "mana_battery_i",
    "regenerative_field_i"
  ],

  "ultimate_trait": null,  // Unlocked at level 10

  "boons": {
    "active_slots": 3,
    "active": ["phoenix_pact", "arcane_rush"],
    "available": ["phoenix_pact", "arcane_rush", "gale_acceleration"]
  },

  "deck": ["card_flamewall", "card_firebolt", "card_pyroblast"],
  "story_flags": ["drank_potion", "fire_arc_complete"]
}
```

### Data Model Notes

**Traits Structure:**
- `story_traits`: Array of story trait IDs (earned from campaign events)
- `level_traits`: Array of level trait IDs (earned from leveling, follows trait lines)
- `ultimate_trait`: String (single ultimate trait ID, or null if not level 10)

**Level and XP:**
- `level`: Current level (1-10)
- `xp`: Current XP amount
- `xp_to_next_level`: XP required to reach next level

**Why Separate Trait Arrays?**
- Allows different validation rules (story traits can be negative, level traits follow prerequisites)
- Makes it clear where each trait came from
- Simplifies UI display (show story vs level progression separately)
- Enables different trait type mechanics (ultimate is active ability)

---

## 12. RELATIONSHIP TO BASE HERO SYSTEM

This progression system builds on top of the **base hero system** defined in `architecture.md`.

### Base System (MVP)
- Hero selection during onboarding
- 4 core elemental heroes + Random option
- Basic stats (base_health, max_mana, mana_regen)
- Hero-bound decks
- `unlocked_heroes` in profile

### Progression System (Post-MVP)
- **Traits** (permanent narrative consequences)
- **Boons** (slotted mechanical bonuses)
- **Global Event Cards** (account-wide rewards)
- Hero unlocking through progression
- Campaign replay with different heroes
- Story-driven hero customization

**The base system must be implemented first.** This progression system extends it.

---

## 13. IMPLEMENTATION PHASES

### Phase 1: Base Hero System ✅ (See architecture.md)
- Hero selection
- Basic hero stats (base_health, max_mana, mana_regen)
- Hero-deck binding
- Profile unlocked_heroes

### Phase 2: Hero Leveling Foundation
- Hero level and XP data structure
- XP gaining system (from battles, events)
- Level-up UI and flow
- XP curve configuration

### Phase 3: Level Traits System
- Level trait data structure and catalog
- Trait Lines catalog with prerequisites
- Level-up trait selection UI
- Trait prerequisite validation
- Level trait effect application

### Phase 4: Ultimate Traits System
- Ultimate trait data structure and catalog
- Level 10 ultimate selection UI
- Ultimate ability activation system
- Ultimate ability effects and VFX
- Ultimate cooldown/usage tracking

### Phase 5: Story Traits Foundation
- Story trait data structure and catalog
- Story trait acquisition from campaign events
- Trait unlock/block logic for boons and affinities
- Story flag integration
- Trait effect application

### Phase 6: Boon System
- Boon data structure and catalog
- Boon catalog with affinity requirements
- Slot management UI
- Boon swapping (outside combat)
- Boon effect application
- Trait-based boon family unlocking

### Phase 7: Global Event Cards
- Event card catalog
- Profile-level card storage
- Event card availability to all heroes
- Event card reward flow

### Phase 8: Hero Unlocking
- Hero unlock conditions
- Hero slot management
- Hero creation flow (with level 1 start)
- Campaign replay support

### Phase 9: Polish and Balance
- Trait balance tuning
- Boon balance tuning
- XP curve adjustments
- Trait Line refinement
- Ultimate ability balancing

---

## 14. FINAL NOTES / DESIGN PHILOSOPHY

This hybrid hero progression system strikes a balance between:
- Structured progression (Levels 1-10)
- Narrative integrity (Story Traits)
- Build depth (Level Traits + Trait Lines)
- Capstone fantasy (Ultimate Traits)
- Long-term hero growth (Unlimited trait accumulation)
- Fairness to new heroes (Level cap, global event cards)
- Content replayability (Different builds each time)
- Mechanical depth (Swappable boons, trait synergies)
- Sustainable balance (Slot limits, level cap)

### It accomplishes ALL of the following:
- ✔ Heroes feel like RPG characters with clear progression
- ✔ Leveling provides tangible advancement (1-10)
- ✔ Level Traits create meaningful build choices (9 decisions)
- ✔ Ultimate Traits deliver capstone fantasy (active abilities at level 10)
- ✔ Story Traits make narrative choices matter (permanent consequences)
- ✔ Boons enable flexible power tuning (swappable slots)
- ✔ Trait stacking creates emergent identity (~15 total traits)
- ✔ Replay matters (different trait combinations each hero)
- ✔ Players are not punished for experimentation (new heroes start fresh)
- ✔ The world progresses globally (event cards are account-wide)
- ✔ Balance does not explode over time (level cap, boon slots)
- ✔ The system scales indefinitely (can add new trait lines and boons)

**This is the most stable, flexible, and expressive progression structure for Project Summoner.**

### The Three-Layer Design

**Layer 1: Leveling (1-10)**
- Provides structure and clear goals
- Unlocks trait choices at each level
- Caps at level 10 to prevent endless grind

**Layer 2: Traits (Story + Level + Ultimate)**
- Story Traits: Narrative consequences (unpredictable)
- Level Traits: Build choices (predictable, planned)
- Ultimate Traits: Capstone ability (level 10 only)
- All permanent, all unlimited, all contribute to identity

**Layer 3: Boons (Slotted Power)**
- Swappable mechanical bonuses
- 3-5 slots maximum
- Main balancing surface
- Does not interfere with trait identity

This separation ensures:
- **Structure** comes from leveling
- **Identity** comes from traits
- **Flexibility** comes from boons

---

## 15. FUTURE CATALOG DOCUMENTS

The following companion documents should be created:

### Core Catalogs
1. **Level Trait Catalog** - All level traits with effects, prerequisites, and trait line structure
2. **Trait Line Catalog** - All trait lines organized by affinity (Fire, Water, Wind, Earth, Universal, Occult, etc.)
3. **Ultimate Trait Catalog** - All ultimate traits organized by affinity with detailed ability mechanics
4. **Story Trait Catalog** - All story traits with acquisition conditions and narrative effects
5. **Boon Catalog** - All boons with effects, slot costs, and affinity/trait requirements
6. **Hero Catalog** - All heroes with stats, starting affinities, and unlock conditions

### Flow and UX Documents
7. **Hero Level-Up Flow** - UX for level-up screens, trait selection, and XP gain feedback
8. **Ultimate Ability UX** - UI/UX for ultimate ability activation and cooldowns
9. **Hero Unlock Flow** - Detailed flow for hero creation and unlocking
10. **Campaign Replay Flow** - How story branches work with multiple heroes

### Balance and Tuning Documents
11. **XP Curve and Pacing** - Recommended XP values for levels 1-10 with pacing guidelines
12. **Trait Balancing Guide** - Guidelines for creating balanced level traits, story traits, and ultimates
13. **Boon Balancing Guide** - Guidelines for boon power levels and slot costs
14. **Trait Line Design Guide** - How to design cohesive trait lines with good progression curves

---

*Related Documents:*
- [Hero System Architecture](architecture.md) - Base hero system (MVP)
- [Campaign Narrative](../campaign/narrative.md) - Story integration points
- [Card System](../cards/system.md) - Card acquisition and deck building
