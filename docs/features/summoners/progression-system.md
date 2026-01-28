# SUMMONER PROGRESSION SYSTEM — FINAL DESIGN SPEC
Summoner Leveling (1-10), Traits (Story + Level + Ultimate), Items, and Global Event Cards

**Version:** 3.0
**Date:** 2026-01-19
**Status:** DESIGN SPEC (Phase 2 Foundation Implemented)

---

## Implementation Status

### ✅ Phase 2: Foundation (Implemented)
- XP and level tracking (1-10)
- Level-up mechanics (XP-based, no gold cost)
- TraitCatalog with innate traits
- SummonerProgressionService and SummonerSelectionService
- Per-summoner campaign progress
- Summoner management UI (panel, roster, icon widget)
- Summoner stat modifiers applied in battle

### 🔲 Phase 3: Level Traits (Not Yet Implemented)
- Trait Lines (prerequisite chains)
- Level-up trait selection UI
- Trait tree visualization

### 🔲 Phase 4: Ultimate Traits (Not Yet Implemented)
- Level 10 capstone abilities
- Ultimate trait activation mechanics

See [architecture.md](architecture.md) for current implementation details.

---

## 1. Overview

This document defines the persistent progression system for summoners in Fateforged, including:

- **Summoner Leveling** (Levels 1-10 with XP progression)
- **Level Traits** (9 traits chosen at levels 1-9, following Trait Lines)
- **Ultimate Traits** (Powerful active abilities unlocked at level 10)
- **Story Traits** (Permanent narrative consequences from campaign events)
- **Items** (Equippable gear with 4 slots — see [Item System](../items/system.md))
- **Global Event Cards** (Account-wide rewards)
- **Summoner-bound decks** (Each summoner maintains their own deck)
- **Summoner unlock rules** (Progressive unlocking through gameplay)
- **Campaign replay design** (Different story paths for different summoners)

### Core Philosophy

**Summoners are identity-bearing, semi-permanent characters.**

Summoners should feel like long-term "files" the player invests in, not disposable runs. Each summoner:
- Has an elemental affinity (Fire, Water, Wind, Earth, etc.)
- Has their own deck (decks are summoner-bound)
- Accumulates **Traits** (permanent identity)
- Equips **Items** (swappable tactical choices)
- Becomes harder to obtain as you unlock more

This system balances:
- Meaningful narrative consequences
- Replayability
- Long-term summoner identity
- Fairness to new summoners
- Sustainable power growth
- Global progression without power creep

**Every piece of this design was chosen to resolve contradictions in earlier models while maximizing player satisfaction and longevity.**

---

## 2. Goals of the System

The progression system aims to:

### 2.1 Provide structured summoner growth (Levels 1-10)
Summoners level through battles and events, unlocking trait choices at each level.

### 2.2 Create meaningful build choices (Level Traits + Trait Lines)
Players choose traits at each level, following prerequisite chains for long-term planning.

### 2.3 Deliver capstone summoner fantasy (Ultimate Traits)
Level 10 unlocks a powerful signature active ability unique to each summoner.

### 2.4 Support meaningful, permanent choices (Story Traits)
Summoners can gain permanent positive or negative traits based on story decisions.

### 2.5 Reward replaying the campaign with different summoners
Every summoner carves a unique story and build path through different trait combinations.

### 2.6 Provide tactical flexibility (Items)
Items are equippable gear that can be swapped between battles, allowing tactical adaptation.

### 2.7 Avoid punishing players for trying new summoners
New summoners start fresh at level 1 but benefit from global event cards.

### 2.8 Keep event/campaign rewards exciting (Global Event Cards)
Event Cards are global, not summoner-bound, avoiding repetition fatigue.

### 2.9 Keep the system scalable long-term
Level cap (10), item slots (4), and trait separation prevent runaway growth.

---

## 3. Core Concepts

This system splits persistent summoner progression into **three layers**:

⭐ **A. TRAITS** (Permanent Identity)
⭐ **B. ITEMS** (Tactical Flexibility)
⭐ **C. SUMMONER LEVELS** (Progression Framework)

Event cards are handled separately (global).

### The Critical Distinction

**Traits = Identity Expression** (permanent, uncapped)
**Items = Tactical Flexibility** (swappable, slot-limited)
**Levels = Growth Framework** (structured progression, trait unlocks)

- **Builders** plan their Level Traits
- **Storytellers** earn their Story Traits
- **Progressors** level their Summoners
- **Tacticians** swap their Items

This creates a clean separation between:
- **Who a summoner is** (Traits) — permanent accumulation
- **How a summoner adapts** (Items) — swappable gear
- **How a summoner grows** (Levels) — structured advancement
- **What the player account has earned globally** (Event Cards) — profile-level rewards

---

## 3.5. SUMMONER LEVELS AND XP

### 3.5.1 Level Progression

**Summoners level from 1 to 10.**

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
- The Ultimate Trait is a powerful, active ability tied to summoner affinity
- Acts as the summoner's "signature ultimate"
- Only one Ultimate Trait per summoner (chosen from 2-3 options)

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

**Both are permanent and summoner-bound.**
- **Level Traits** are capped by levels (9 total: one per level 1-9).
- **Story Traits** are not explicitly capped, but in practice are limited by how many story events a summoner can encounter.
- Both contribute to summoner identity.

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

**XP is summoner-specific.**
- Each summoner levels independently
- No shared XP pool across summoners
- XP is NOT a global account resource

**Expected pacing:**
- Levels 1-5: Fast progression (teach the system)
- Levels 6-9: Moderate progression (build commitment)
- Level 10: Major milestone (ultimate unlock)

Exact XP curves TBD during implementation.

---

## 4. TRAITS

### 4.1 What Traits Are

**Traits = Permanent, summoner-bound identity markers.**

Traits represent permanent additions to a summoner's identity. They come in three forms:

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
- Summoner's signature ultimate

All trait types are **permanent and contribute to summoner identity**.

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
- Starting summoner selection (e.g., "Fortune Favors the Bold")

**Expected accumulation:** ~5 story traits over a full campaign run (varies by choices). There is no explicit cap on Story Traits; this ~5 value is a practical expectation per full campaign, based on available events.
**Expected accumulation:** ~9 level traits (one per level 1-9).
**Expected accumulation:** 1 ultimate trait (at level 10).

### 4.4 All Traits (Story + Level + Ultimate) NEVER:
- ❌ Get removed (unless specific story events allow)
- ❌ Rotate or expire
- ❌ Get balanced around swapping
- ❌ Apply to other summoners
- ❌ Become global

**They belong ONLY to the summoner who earned them.**

**Trait Limitations:**
- Level Traits are limited to 9 (one per level 1-9) plus 1 Ultimate at level 10.
- Story Traits have no explicit cap but are practically limited by campaign content.

### 4.5 All Traits (Story + Level + Ultimate) DO:
- ✅ Stack over time (within their respective limits)
- ✅ Create long-term identity through synergy
- ✅ Unlock card pools and affinity access
- ✅ Alter campaign events and story branches (mostly Story Traits)
- ✅ Permanently modify story outcomes (mostly Story Traits)
- ✅ Give percentage modifiers and mechanical effects

### 4.6 Trait Synergy and Identity

**Trait stacking is intended, not avoided.**

Summoners will accumulate many traits over time (5 story + 9 level + 1 ultimate = ~15 total). Trait sets create emergent identity:

- **Fire summoner** stacked with fire level traits + fire story traits → Pyromaniac identity
- **Corrupted summoner** stacked with occult level traits + occult story traits → Dark summoner identity
- **Nature summoner** stacked with regeneration level traits + growth story traits → Druidic identity

This synergy is the core long-term fantasy of summoner progression.

### 4.7 Trait Lines (Level Traits)

**Trait Lines = Prerequisite chains for level traits.**

See Section 3.5.4 for detailed explanation.

Trait Lines guide level-up choices and create long-term build planning. Each trait line offers increasing power at higher levels, rewarding commitment to a strategy.

**Example progression:**
- Early level: Choose Pyromancy I (fire damage bonus)
- Later level: Choose Pyromancy II (requires Pyromancy I)
- Another later level: Choose Pyromancy III (requires Pyromancy II)
- Final level before 10: Choose Inferno Mastery (requires Pyromancy III)

Players can hybridize by splitting points between multiple trait lines, or specialize by committing to one line for maximum power.

### 4.8 Ultimate Traits (Level 10)

**Ultimate Traits = Powerful active abilities unlocked at level 10.**

Unlike other traits (which are passive), Ultimate Traits are **active abilities** that function like signature moves.

**Characteristics:**
- Unlocked at level 10 only
- Active abilities (player-triggered, not passive)
- Tied to summoner affinity (Fire, Water, Wind, Earth, etc.)
- Only one Ultimate Trait per summoner
- Player chooses from 2-3 ultimate options at level 10
- Cannot be changed once selected

**Ultimates are chosen at level 10 from 2-3 options, and the chosen Ultimate is permanent for that summoner.**

**Example Ultimates (Fire Affinity):**
- **Phoenix Rebirth**: Once per battle, resurrect all dead units at 50% HP
- **Inferno Nova**: Deal massive AoE damage to all enemy units
- **Flamestrike**: Summon a powerful fire elemental for 30 seconds

Ultimate Traits define the summoner's "final form" and provide a capstone fantasy for the progression journey.

### 4.9 Example Story Traits

**Tainted Blood**
Permanent HP reduction, unlocks occult path.

**Fortune Favors the Bold**
Story Trait granted only when the player chooses "Random Summoner" at start. Permanent, summoner-bound trait applied to that starting summoner.

**Marked by the Phoenix**
Alters dialogue, unlocks Phoenix events.

**Oathbreaker**
Adds new confrontation events, restricts some sanctified paths.

**Occult Initiate**
Unlocks occult cards. May have negative reputation effects.

**Purified Soul**
Blocks all occult content, unlocks sanctified paths.

**Naturebound**
+10% unit regeneration, unlocks growth paths.

### 4.10 Example Level Traits

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

## 5. ITEMS

### 5.1 Items Replace the Former Boon System

Items are the tactical customization layer for summoners. They provide the flexibility that the old "boon" system provided, but as equippable gear rather than abstract bonuses.

**For full item system details, see [Item System](../items/system.md).**

### 5.2 Item Slots

Summoners have **4 item slots**:

| Slot | Name | Focus |
|------|------|-------|
| 1 | Grimoire | Spell/magic focused |
| 2 | Weapon/Staff | Offense focused |
| 3 | Ring | Utility focused |
| 4 | Vestments | Defense focused |

### 5.3 Item vs Trait Distinction

| Aspect | Traits | Items |
|--------|--------|-------|
| **Permanence** | Permanent, cannot be changed | Swappable between battles |
| **Accumulation** | Uncapped (~15 total over time) | Fixed 4 slots |
| **Identity** | "Who the summoner is" | "How they approach this battle" |
| **Acquisition** | Leveling, story events | Campaign rewards, events, shop |

### 5.4 Item Binding

| Source | Binding | Campaign Usable |
|--------|---------|-----------------|
| Campaign reward | Summoner-bound | Yes |
| Event reward | Account-wide `[Shared]` | No |
| Shop purchase | Account-wide `[Shared]` | No |

See [Item System](../items/system.md) for complete details.

---

## 6. DECK STRUCTURE

### 6.1 Decks Are Summoner-Bound

**Each summoner maintains their own deck.**

Starting a new summoner = fresh deck creation.

**Why?**
- Supports identity-driven progression
- Allows campaign choices to shape deck contents
- Ensures replayability
- Supports branching paths and unique reward trees
- Summoners are meant to be commitments, not disposable runs

### 6.2 Deck Acquisition

A summoner gains cards from:
- Campaign arc decisions
- Non-repeatable branches
- Affinity unlocks (via traits)
- Summoner-specific events
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

✔ **Makes new summoners viable**
A new summoner starts with access to global event cards.

✔ **Preserves world progression**
Your profile's world has moved forward; all summoners benefit.

✔ **Keeps event cards exciting**
They don't feel like mundane, repeatable tasks.

### 7.3 Shared Content is a Lever, Not a Rule

Making event cards account-wide `[Shared]` is an **option** we can use, not a blanket policy. Not all event content needs to be shared.

**When we use it:**
- Prevents forcing players to grind events X times for X summoners
- Use for content where multi-summoner grind would feel bad

**Shared cards are locked for campaign use** — they appear in the summoner's view but cannot be used in campaign battles. Available for PvP and events only.

---

## 8. CAMPAIGN STRUCTURE AND REPLAY

### 8.1 Campaign Design

**The campaign is NOT "4 separate elemental campaigns."**

It is **one world** with regions, routes, and branching paths.

Players can:
- Bring any summoner into any region
- Gain traits based on their choices
- Unlock items and affinity bonuses
- Accumulate identity over time
- Make different choices with different summoners

### 8.2 Campaign Replay

**Campaign replay is a feature, not a punishment.**

When starting a new summoner:
- The world is the same, but your story differs
- Different traits lead to different events
- Different affinities unlock different paths
- Event Cards are already unlocked (global)
- Fresh deck building experience
- New narrative branches and outcomes

### 8.3 Why This Structure Works

✔ **Replayability:** Each summoner carves a unique path
✔ **Agency:** Player choices shape summoner identity
✔ **Freshness:** New summoners feel distinct
✔ **Respect:** Event Cards prevent repetition
✔ **Investment:** Summoners accumulate meaningful history

---

## 9. WHAT PROBLEMS THIS SYSTEM SOLVES

This section captures the design reasoning explicitly — critical for future contributors.

### 9.1 Provides Clear Summoner Progression (Leveling System)
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
- Active abilities give summoners distinct playstyles
- Choosing between 2-3 ultimates creates meaningful final decision
- Provides satisfying "final form" moment

### 9.4 Prevents Power Creep (Item Slots + Level Cap)
- 4 item slots prevent summoners from stacking unlimited gear
- Level cap (10) prevents endless trait accumulation
- Traits are permanent but balanced (most are % modifiers, not flat)

### 9.5 Prevents New-Summoner Punishment
- Traits are summoner-specific, but Event Cards are global — new summoners aren't cards behind
- New summoners start at level 1, but leveling is fast early on
- Item slots are fixed (4), so old summoners don't become unstoppable
- New summoners start clean (no bad traits from previous summoners)
- New summoners always start with 0 Level Traits and no Story Traits, but can gain them over time

### 9.6 Enables Meaningful Story Decisions (Story Traits)
- Story Traits being permanent and immutable make choices matter
- Separate from leveling system, so narrative consequences don't feel like "missed power"
- Negative traits don't prevent level-up choices

### 9.7 Enables Replayability
Because:
- Level Traits chosen differently on each summoner
- Story Traits are summoner-bound
- Decks are summoner-bound
- Campaign choices differ
- Different affinities unlock different trait lines and content

**Every summoner's story is unique.**

### 9.8 Avoids Repetition Fatigue (Global Event Cards)
Global Event Cards remove the need to grind same content for multiple summoners.

### 9.9 Supports Narrative Consequences Without Mechanical Punishment
- Negative Story Traits don't prevent Level Trait choices
- Negative traits don't ruin new summoners (summoner-bound)
- Many traits are identity/story, not pure power

### 9.10 Supports Tactical Adaptation (Items)
Items can be swapped freely between battles, giving players control over tactical approach.

### 9.11 Supports Long-Term Scalability
- Can add new trait lines without affecting existing summoners
- Can add new items, story traits, or summoners without breaking ecosystem
- Level cap (10) prevents infinite scaling
- Trait Lines are modular and expandable

### 9.12 Supports Summoner Identity Through Trait Stacking
- 15 total traits (5 story + 9 level + 1 ultimate) create rich emergent identity
- Trait synergy builds fantasy without infinite power
- Level Traits + Story Traits combine for unique builds

---

## 10. HERO UNLOCK STRATEGY

Summoners must be:
- Rare
- Meaningful
- Expensive
- Part of progression

### Guidelines:
- Start with **1 summoner** (chosen during onboarding)
- Unlock second summoner at **major arc completion**
- Subsequent summoners require **rarer feats or substantial investment**
- Maximum reasonable summoners: **3–5 in early game**

This ensures each summoner feels like a genuine build.

---

## 11. DATA MODEL (Simplified)

### Profile
```json
{
  "unlocked_summoners": ["summoner_cole", "summoner_selene"],
  "global_event_cards": ["card_occult_ascension", "card_phoenix_blessing"],
  "cosmetics": [],
  "unlocked_summoner_slots": 2
}
```

**Note on Summoner IDs:**
- **MVP Phase**: Uses template IDs (`"summoner_cole"`, `"summoner_selene"`) in `unlocked_summoners` array
  - Simpler model: one instance per summoner template
  - Profile tracks which summoner templates are unlocked
- **Post-MVP (Future)**: May use instance IDs (`"summoner_cole_001"`, `"summoner_cole_002"`) if supporting multiple instances per template
  - Allows players to have multiple Fire summoners with different builds
  - More complex, but enables greater customization
  - This is an expected evolution of the data model

For now, **use template IDs** as shown above to match `architecture.md`.

### Summoner
```json
{
  "id": "summoner_cole",
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

  "items": {
    "grimoire": "item_fire_tome",
    "weapon": "item_flame_staff",
    "ring": null,
    "vestments": "item_ember_cloak"
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

**Items Structure:**
- `items`: Object with 4 slots (grimoire, weapon, ring, vestments)
- Each slot contains an item ID or null if empty
- Items are summoner-bound or account-wide based on acquisition source

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

This progression system builds on top of the **base summoner system** defined in `architecture.md`.

### Base System (MVP)
- Summoner selection during onboarding
- 4 core elemental summoners + Random option
- Basic stats (base_health, max_mana)
- Summoner-bound decks
- `unlocked_summoners` in profile

### Progression System (Post-MVP)
- **Traits** (permanent narrative consequences)
- **Items** (equippable gear for tactical flexibility)
- **Global Event Cards** (account-wide rewards)
- Summoner unlocking through progression
- Campaign replay with different summoners
- Story-driven summoner customization

**The base system must be implemented first.** This progression system extends it.

---

## 13. IMPLEMENTATION PHASES

### Phase 1: Base Summoner System ✅ (See architecture.md)
- Summoner selection
- Basic summoner stats (base_health, max_mana)
- Summoner-deck binding
- Profile unlocked_summoners

### Phase 2: Summoner Leveling Foundation
- Summoner level and XP data structure
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
- Trait unlock/block logic for affinities
- Story flag integration
- Trait effect application

### Phase 6: Item System
- Item data structure and catalog (see [Item System](../items/system.md))
- Item slot management UI
- Item swapping (between battles)
- Item effect application
- Item binding (summoner vs account-wide)

### Phase 7: Global Event Cards
- Event card catalog
- Profile-level card storage
- Event card availability to all summoners
- Event card reward flow

### Phase 8: Summoner Unlocking
- Summoner unlock conditions
- Summoner slot management
- Summoner creation flow (with level 1 start)
- Campaign replay support

### Phase 9: Polish and Balance
- Trait balance tuning
- Item balance tuning
- XP curve adjustments
- Trait Line refinement
- Ultimate ability balancing

---

## 14. FINAL NOTES / DESIGN PHILOSOPHY

This hybrid summoner progression system strikes a balance between:
- Structured progression (Levels 1-10)
- Narrative integrity (Story Traits)
- Build depth (Level Traits + Trait Lines)
- Capstone fantasy (Ultimate Traits)
- Tactical flexibility (Items)
- Fairness to new summoners (Level cap, global event cards)
- Content replayability (Different builds each time)
- Mechanical depth (Swappable items, trait synergies)
- Sustainable balance (Item slots, level cap)

### It accomplishes ALL of the following:
- ✔ Summoners feel like RPG characters with clear progression
- ✔ Leveling provides tangible advancement (1-10)
- ✔ Level Traits create meaningful build choices (9 decisions)
- ✔ Ultimate Traits deliver capstone fantasy (active abilities at level 10)
- ✔ Story Traits make narrative choices matter (permanent consequences)
- ✔ Items enable tactical adaptation (swappable gear)
- ✔ Trait stacking creates emergent identity (~15 total traits)
- ✔ Replay matters (different trait combinations each summoner)
- ✔ Players are not punished for experimentation (new summoners start fresh)
- ✔ The world progresses globally (event cards are account-wide)
- ✔ Balance does not explode over time (level cap, item slots)
- ✔ The system scales indefinitely (can add new trait lines and items)

**This is the most stable, flexible, and expressive progression structure for Fateforged.**

### The Three-Layer Design

**Layer 1: Leveling (1-10)**
- Provides structure and clear goals
- Unlocks trait choices at each level
- Caps at level 10 to prevent endless grind

**Layer 2: Traits (Story + Level + Ultimate)**
- Story Traits: Narrative consequences (unpredictable)
- Level Traits: Build choices (predictable, planned)
- Ultimate Traits: Capstone ability (level 10 only)
- All permanent, all contribute to identity

**Layer 3: Items (Tactical Flexibility)**
- Equippable gear with 4 slots
- Swappable between battles
- Provides tactical customization

This separation ensures:
- **Structure** comes from leveling
- **Identity** comes from traits
- **Flexibility** comes from items

---

## 15. FUTURE CATALOG DOCUMENTS

The following companion documents should be created:

### Core Catalogs
1. **Level Trait Catalog** - All level traits with effects, prerequisites, and trait line structure
2. **Trait Line Catalog** - All trait lines organized by affinity (Fire, Water, Wind, Earth, Universal, Occult, etc.)
3. **Ultimate Trait Catalog** - All ultimate traits organized by affinity with detailed ability mechanics
4. **Story Trait Catalog** - All story traits with acquisition conditions and narrative effects
5. **Item Catalog** - All items organized by slot type
6. **Summoner Catalog** - All summoners with stats, starting affinities, and unlock conditions

### Flow and UX Documents
7. **Summoner Level-Up Flow** - UX for level-up screens, trait selection, and XP gain feedback
8. **Ultimate Ability UX** - UI/UX for ultimate ability activation and cooldowns
9. **Summoner Unlock Flow** - Detailed flow for summoner creation and unlocking
10. **Campaign Replay Flow** - How story branches work with multiple summoners

### Balance and Tuning Documents
11. **XP Curve and Pacing** - Recommended XP values for levels 1-10 with pacing guidelines
12. **Trait Balancing Guide** - Guidelines for creating balanced level traits, story traits, and ultimates
13. **Item Balancing Guide** - Guidelines for item power levels
14. **Trait Line Design Guide** - How to design cohesive trait lines with good progression curves

---

*Related Documents:*
- [Summoner System Architecture](architecture.md) - Base summoner system (MVP)
- [Item System](../items/system.md) - Item slots and mechanics
- [Campaign Narrative](../campaign/narrative.md) - Story integration points
- [Campaign Structure](../campaign/structure.md) - Path system and level caps
- [Card System](../cards/system.md) - Card acquisition and deck building
