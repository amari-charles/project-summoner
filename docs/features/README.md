# Feature Documentation Index

**Last Updated:** 2026-01-19

This index lists all feature documentation for Fateforged. Each document describes a game system's design, mechanics, and implementation status.

---

## Quick Reference

| If you need to understand... | Read... |
|------------------------------|---------|
| How cards work | [Card System](cards/system.md) |
| How combat works | [Combat System](combat/system.md) |
| How the campaign is structured | [Campaign Structure](campaign/structure.md) |
| How summoners progress | [Summoner README](summoners/README.md) |
| How items work | [Item System](items/system.md) |

---

## Core Gameplay Systems

### [Card System](cards/system.md)
**Status:** CURRENT

Defines how cards work in Fateforged:
- Card types (Unit, Spell, Structure)
- Rarity system and spawn counts
- Deck building rules
- Card binding (summoner-bound vs shared)
- **Level cap mechanics** (cards floored to cap)
- XP distribution (only deck cards gain XP)

### [Combat System](combat/system.md)
**Status:** IMPLEMENTED

Unit AI, targeting, and damage mechanics:
- Unit types (Melee, Ranged, Flying)
- Targeting priorities
- Attack and damage calculations
- Death and cleanup

### [Battlefield System](battlefield/system.md)
**Status:** IMPLEMENTED

Map layout and spatial systems:
- Player and enemy zones
- Spawn areas
- Camera and boundaries

### [Coordinate System](coordinates/system.md)
**Status:** IMPLEMENTED

3D positioning and coordinate conversions:
- World space
- Screen space conversions
- Spawn point calculations

---

## Progression Systems

### [Campaign Structure](campaign/structure.md)
**Status:** CURRENT (New - 2026-01-19)

Full campaign mechanics:
- **One campaign, all summoners** (different offers by element)
- **Path system** (Elite vs Standard)
- **Level cap system** (cards floored to cap)
- **Decision types** (Major, Minor, Filler)
- **Grinding rules** (replay for XP only)

### [Campaign Narrative](campaign/narrative.md)
**Status:** CURRENT

Story and writing guidelines:
- Setting (Academy of Summoning Arts)
- Characters (Headmaster Merlin)
- Writing tone and voice
- Battle naming conventions

### Summoner System

#### [Summoner README](summoners/README.md)
**Status:** CURRENT

Overview and implementation guide:
- Architecture (MVP)
- Progression system (Post-MVP)
- Key design decisions
- Implementation order

#### [Summoner Architecture](summoners/architecture.md)
**Status:** IMPLEMENTED

Base summoner system (MVP):
- Summoner selection
- Stats (health, mana)
- Trait system
- Service layer design

#### [Summoner Progression](summoners/progression-system.md)
**Status:** DESIGN SPEC

Full progression system (Post-MVP):
- Summoner leveling (1-10)
- Level Traits, Story Traits, Ultimate Traits
- Item system integration
- Global event cards

### [Item System](items/system.md)
**Status:** CURRENT (New - 2026-01-19)

Equippable gear for summoners:
- **Replaces former "boon" system**
- 4 slots: Grimoire, Weapon/Staff, Ring, Vestments
- Swappable between battles
- Binding rules (summoner-bound vs shared)

---

## Supporting Systems

### [Elemental System](elemental-system.md)
**Status:** REFERENCE

Element definitions and interactions:
- Core elements (Fire, Water, Earth, Wind, Lightning)
- Element affinities
- Summoner element theming

### [Modifier System](modifier-system.md)
**Status:** IMPLEMENTED

Stat modification pipeline:
- Modifier types (flat, percent, multiplicative)
- Modifier sources (traits, items, spells)
- Application order

### [Events Architecture](events/architecture.md)
**Status:** IN PROGRESS

Campaign event system:
- Event types (Battle, Shop, Story)
- Event flow
- Reward distribution

### [Shop Architecture](shop/architecture.md)
**Status:** REFERENCE

Shop systems design:
- Campaign Caravan (in-run)
- Meta Shop (persistent)

### [Spells](spells/)
**Status:** PARTIAL

Spell mechanics:
- [Rally, Guard, Charge](spells/rally-guard-charge-spells.md) - Tactical command spells

---

## Key Concepts Quick Reference

### Campaign Structure
- **One Campaign**: All summoners play the same campaign structure
- **Elite Path**: Level-capped, skill check, better rewards
- **Standard Path**: No level cap, escape valve for stuck players
- **Level Cap**: Cards floored to cap (up or down), upgrades also capped

### Customization Layers
| Layer | Permanence | Purpose |
|-------|------------|---------|
| **Traits** | Permanent | Identity ("who the summoner is") |
| **Items** | Swappable | Tactical flexibility |
| **Cards** | Permanent | Fate-forged choices |

### Binding Rules
| Source | Binding | Campaign Usable |
|--------|---------|-----------------|
| Campaign reward | Summoner-bound | Yes |
| Event reward | Account-wide `[Shared]` | No |
| Shop purchase | Account-wide `[Shared]` | No |

### XP Rules
- Only cards **in deck** gain XP from battles
- Replay battles for **XP only** (no gold/cards on replay)
- Standard path grinding = escape valve

---

## Document Status Legend

| Status | Meaning |
|--------|---------|
| **CURRENT** | Reflects latest design decisions |
| **IMPLEMENTED** | Feature is built and matches doc |
| **DESIGN SPEC** | Design complete, awaiting implementation |
| **IN PROGRESS** | Currently being developed |
| **REFERENCE** | Stable reference, may not reflect all recent changes |
| **PARTIAL** | Some content documented, more to come |

---

## Related Documentation

- [Vision Document](../project/vision.md) - Core game vision
- [Card Progression Economy](../design/card-progression-economy.md) - Detailed progression design
- [Ideation Session](../design/ideation-session-2026-01-19.md) - Latest design decisions

---

*Back to [Documentation Index](../README.md)*
