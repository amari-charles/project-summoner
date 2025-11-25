# Hero System Documentation

This directory contains all documentation for the Hero system in Project Summoner.

**Note:** Previously outdated `system.md` has been removed. All current documentation is now consistent and accurate.

---

## 📚 Documentation Index

### 1. [architecture.md](architecture.md) — **BASE HERO SYSTEM (MVP)**
**Status:** Design Complete, Implementation Pending

Defines the foundational hero system for MVP:
- Hero selection during onboarding (4 core elements + Random)
- Hero stats (base_health, max_mana, mana_regen)
- Fortune Favors the Bold (Story Trait) granted to the starting hero only when choosing Random (hero-bound, not profile-level)
- Hero-deck binding
- Profile ownership vs deck selection model
- Data structures and service layer design
- Battle integration

**Note:** Traits (beyond Fortune Favors the Bold) are introduced only in the full progression system, not MVP.

**Start here for implementation.**

---

### 2. [progression-system.md](progression-system.md) — **FULL PROGRESSION SYSTEM**
**Status:** Design Spec, Post-MVP (Updated with final design including leveling)

Defines the complete hero progression system (all features below are Post-MVP):
- **Hero Leveling (1-10)** - Structured progression framework
  - Heroes gain XP from battles and events
  - Level cap at 10 prevents endless grind
  - Each level unlocks trait choices
- **Level Traits** - Build-focused progression (9 traits, levels 1-9)
  - Chosen by player from curated lists
  - Follow Trait Lines with prerequisites (e.g., Pyromancy I → II → III)
  - **Trait Lines are unlocked by prerequisites only; players may choose any trait they qualify for at any level from 1-9**
  - Create long-term build planning
  - Heroes always end with exactly 9 Level Traits (one per level 1-9)
- **Ultimate Traits** - Capstone abilities (level 10)
  - Powerful active abilities (player-triggered, usable once per battle)
  - Tied to hero affinity
  - Choose from 2-3 options at level 10
  - Defines hero's "final form"
- **Story Traits** - Permanent narrative consequences (hero-bound)
  - Earned from campaign events and story decisions
  - Can be positive, negative, or mixed
  - Unlock/block boon families and affinity paths
  - **Story Traits have no explicit limit; they are practically limited by campaign content**
  - ~5 story traits expected per campaign run
- **Boons** - Slotted mechanical bonuses (hero-bound, 3 default slots, swappable outside combat)
  - Can give % modifiers and strong effects
  - Removable and configurable
  - Main power tuning surface
- **Global Event Cards** - Account-wide rewards (the only cross-hero power; prevents repetition fatigue)
- **Campaign Structure** - One world with branching paths (not 4 separate campaigns)
- Hero unlocking through gameplay
- Campaign replay mechanics
- Power scaling and balance philosophy

**This builds on top of the base system.** Implement after MVP is complete.

---

## 🔄 Implementation Order

### Phase 1: Base Hero System (MVP)
Follow `architecture.md`:
1. Create HeroCatalog service with 4 core heroes
2. Add ProfileRepo methods for hero unlocking
3. Update Deck service to store hero_id
4. Update DeckLoader to load hero data
5. Apply hero bonuses in BattleContext
6. Add hero selection UI to deck builder
7. Create onboarding hero selection screen
8. Add localization entries

### Phase 2: Progression System
Follow `progression-system.md`:
1. Implement Hero Leveling (XP, level-up flow)
2. Implement Level Traits system (trait lines, prerequisites, selection UI)
3. Implement Ultimate Traits (level 10 active abilities)
4. Implement Story Traits system (campaign event acquisition)
5. Implement Boon system with slotting
6. Implement Global Event Cards
7. Add hero unlocking mechanics
8. Support campaign replay

---

## 🎯 Key Design Decisions

### Why Profile + Deck Model?
**Profile = Ownership** ("Which heroes do I have?")
**Deck = Selection** ("Which hero does this deck use?")

This allows:
- Multiple decks with different heroes
- Clear separation of concerns
- Future: Multiple heroes per profile

### Why Hero-Bound Decks?
- Supports identity-driven progression
- Enables story choices to shape deck contents
- Increases replayability
- Allows branching narrative paths

### Why Global Event Cards?
- Prevents repetition fatigue
- Makes new heroes viable immediately
- Preserves sense of world progression
- Keeps events exciting

### Why Hero Leveling (1-10)?
**Leveling** = Structured Progression Framework
- Provides clear advancement path (levels 1-10)
- Creates tangible sense of progress from battles/events
- Level cap (10) prevents endless grinding
- Unlocks trait choices at each level

**Why Level Traits?**
- Predictable, player-driven build choices
- Follow Trait Lines with prerequisites
- Create long-term planning (commit to a line or hybridize)
- ~9 total choices = meaningful without overwhelming

**Why Ultimate Traits at Level 10?**
- Capstone fantasy (hero's "final form")
- Active abilities (not passive like other traits)
- Clear endgame goal
- Memorable culmination of hero progression

This system provides:
- Structure (clear progression path)
- Agency (player chooses which traits to take)
- Depth (trait lines with prerequisites)
- Replayability (different build each hero)

### Why Separate Story Traits and Level Traits?
**Story Traits** = Unpredictable narrative consequences
- Earned from campaign events
- Can be positive, negative, or mixed
- Unlock/block affinities and paths

**Level Traits** = Predictable build progression
- Chosen at level-up
- Always positive mechanical bonuses
- Follow structured trait lines

This separation ensures:
- Narrative consequences feel impactful (not just "missed power")
- Progression feels fair (level traits are guaranteed)
- Builds have both planned elements (level) and emergent elements (story)

### Why Traits vs Boons?
**Traits** (Story + Level + Ultimate) = Identity Expression
- Unlimited accumulation (~15 total per hero)
- Permanent and immutable
- Can give % modifiers and effects
- Create synergy through stacking
- Unlock/block boon families

**Boons** = Power Expression (build tuning, mechanical choices)
- Slot-limited (3 default, max ~5 with rare traits)
- Swappable outside combat
- Can give % modifiers and strong effects

This separation prevents:
- Power creep (boons slot-capped, level cap at 10)
- New hero punishment (traits are hero-bound, event cards are global)
- Choice regret (story traits are narrative, boons are flexible)
- Balance explosions (boons are the main tuning surface)

---

## 🔗 Related Documentation

- [Card System](../cards/system.md)
- [Campaign Narrative](../campaign/narrative.md)
- [Elemental System](../elemental-system.md)
- [Modifier System](../modifier-system.md)
- [Shop Architecture](../shop/architecture.md)

---

## 📝 Future Documents

These companion documents should be created as the system is implemented:

### Core Catalogs
1. **level-trait-catalog.md** - All level traits with prerequisites and trait line structure
2. **trait-line-catalog.md** - All trait lines organized by affinity
3. **ultimate-trait-catalog.md** - All ultimate traits with detailed ability mechanics
4. **story-trait-catalog.md** - All story traits with acquisition conditions
5. **boon-catalog.md** - All boons with effects, slot costs, and requirements
6. **hero-catalog.md** - All heroes with stats and unlock conditions

### Flow and UX Documents
7. **hero-level-up-flow.md** - UX for level-up screens and trait selection
8. **ultimate-ability-ux.md** - UI/UX for ultimate ability activation
9. **hero-unlock-flow.md** - Detailed hero creation and unlocking mechanics
10. **campaign-replay-flow.md** - Story branching with multiple heroes

### Balance and Tuning Documents
11. **xp-curve-pacing.md** - XP values and pacing for levels 1-10
12. **trait-balancing-guide.md** - Guidelines for balanced traits (story/level/ultimate)
13. **boon-balancing-guide.md** - Guidelines for boon power levels
14. **trait-line-design-guide.md** - How to design cohesive trait lines

---

*Last Updated: 2025-01-24*
