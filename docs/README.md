# Fateforged Documentation

**Last Updated:** 2026-03-04

This is the central index for all Fateforged documentation. Start here to find what you need.

---

## Quick Navigation

| I want to... | Go to... |
|--------------|----------|
| Get started as a new developer | [Start Here](start-here.md) |
| Understand the game vision | [Vision](project/vision.md) |
| See what's implemented | [Current State](project/current-state.md) |
| Learn the core game systems | [Features Index](features/README.md) |
| Check design decisions | [Design Decisions](#design-decisions) |
| Find technical details | [Technical Docs](#technical-documentation) |

---

## Core Design Documents

These define what the game IS. Read these first to understand Fateforged.

### Vision & Identity
- **[Vision](project/vision.md)** - Core fantasy, design pillars, what makes Fateforged unique
- **[Brief](project/brief.md)** - Studio-ready pitch document
- **[Current State](project/current-state.md)** - What's actually implemented

### Recent Design Decisions (2026-01-19)
- **[Ideation Session](design/ideation-session-2026-01-19.md)** - All finalized decisions from latest session

Key decisions captured:
- One campaign for all summoners (different offers by element)
- Elite vs Standard path system with level caps
- Items replace boons (4 slots: Grimoire, Weapon/Staff, Ring, Vestments)
- XP only for cards in deck; replay battles for XP only
- Shared content is a lever, not a blanket rule

---

## Feature Documentation

Complete system specifications. See [Features Index](features/README.md) for full list.

### Core Gameplay
| System | Description | Status |
|--------|-------------|--------|
| [Card System](features/cards/system.md) | Cards, rarity, spawning, level caps | Current |
| [Combat System](features/combat/system.md) | Unit AI, targeting, damage | Implemented |
| [Battlefield](features/battlefield/system.md) | Map, zones, coordinates | Implemented |

### Progression Systems
| System | Description | Status |
|--------|-------------|--------|
| [Campaign Structure](features/campaign/structure.md) | Paths, level caps, grinding | Current |
| [Campaign Narrative](features/campaign/narrative.md) | Story, writing guidelines | Current |
| [Summoner System](features/summoners/README.md) | Summoner architecture & progression | Current |
| [Item System](features/items/system.md) | Equippable gear (replaces boons) | Current |
| [Card Progression](design/card-progression-economy.md) | XP, leveling, resources | Current |

### Other Systems
| System | Description | Status |
|--------|-------------|--------|
| [Elemental System](features/elemental-system.md) | Elements and affinities | Reference |
| [Modifier System](features/modifier-system.md) | Stat modifications | Implemented |
| [Events](features/events/architecture.md) | Campaign events | In Progress |
| [Shop](features/shop/architecture.md) | Caravan & meta shop | Reference |

---

## Technical Documentation

Implementation details for developers.

### Architecture
- **[System Architecture](architecture/system-architecture.md)** - Overall code architecture
- **[Graph-Of-Graphs Model](architecture/graph-of-graphs.md)** - Shared architecture vocabulary and navigation model
- **[Application Layer](architecture/application-layer.md)** - Scene/lifecycle orchestration boundaries
- **[Layered Architecture Migration](migration/README.md)** - Migration reference + archive entrypoint
- **[Game Requirements](architecture/game-requirements.md)** - Comprehensive gameplay requirements spec

### Technical References
- **[Technical Index](technical/README.md)** - Scope and map for implementation-focused docs
- **[Unit Stat Pipeline](technical/unit-stat-pipeline.md)** - How stats flow from catalog to battle
- **[Campaign Data](technical/campaign-data.md)** - Campaign data structures
- **[Multiplayer Architecture](multiplayer/architecture.md)** - Current session-layer architecture and boundaries
- **[Simulation Architecture](technical/simulation-architecture.md)** - Deterministic sim runtime + multiplayer coordination overview

### Workflows
- **[PR Review Guidelines](workflows/pr-review-guidelines.md)** - Code review process
- **[Running Tests](workflows/running-tests.md)** - Test execution
- **[Creating Dialogue](workflows/creating-dialogue.md)** - Dialogue system usage

---

## Design Decisions

Major design choices and their rationale.

### Campaign & Progression
- **One Campaign, All Summoners** - Same structure, different card offers by element
- **No Runs** - Permanent journey per summoner, replayability via new summoners
- **Elite vs Standard Paths** - Elite has level caps (skill check), Standard has no cap (escape valve)
- **Level Cap System** - Cards floored to cap, prevents grinding from trivializing elite content
- **XP Distribution** - Only cards in deck gain XP

### Customization Layers
- **Traits** - Permanent identity (Level Traits, Story Traits, Ultimate Traits)
- **Items** - Tactical flexibility (4 slots, swappable between battles)
- **Cards** - Permanent fate-forged choices through campaign

### Card & Binding Rules
- Campaign rewards = summoner-bound
- Event/shop rewards = optionally account-wide `[Shared]`
- Shared content locked for campaign use

---

## Content Documentation

### Lore & Narrative
- **[World & Setting](lore/world.md)** - The Academy of Summoning Arts
- **[Narrative Arc](lore/narrative-arc.md)** - Story progression
- **[Characters](lore/characters/)** - Fateforgers and NPCs

### Art & Assets
- **[Visual Style](art/visual-style-references.md)** - Art direction
- **[Art Brief](art/art-brief.md)** - Overall art guidelines

### Elements
- **[Elements Index](elements/README.md)** - All elemental content
- Individual element docs in `elements/{element}/`

---

## Tracking

- **[Bugs](tracking/bugs.md)** - Known issues
- **[Todos](tracking/todos.md)** - Planned features
- **[Docs Reorg Audit (2026-03-04)](archive/doc-reorg-2026-03/tracking/docs-reorg-audit-2026-03-04.md)** - Documentation structure audit + archive log
- **[Changelog](project/changelog.md)** - Release notes
- **[Development History](project/development-history.md)** - Internal progress log

---

## Documentation Conventions

### Status Labels
| Status | Meaning |
|--------|---------|
| **CURRENT** | Up to date with latest design |
| **IMPLEMENTED** | Feature is built and working |
| **DESIGN SPEC** | Design complete, implementation pending |
| **IN PROGRESS** | Currently being worked on |
| **DRAFT** | Incomplete, may change |
| **ARCHIVED** | Outdated, kept for history |

### File Organization
```
docs/
├── README.md           ← You are here (central index)
├── start-here.md       ← New developer onboarding
├── project/            ← Vision, roadmap, state
├── features/           ← System specifications
│   └── README.md       ← Features index
├── design/             ← Design decisions
├── technical/          ← Implementation details
├── architecture/       ← Code architecture
├── workflows/          ← Development processes
├── art/                ← Art specifications
├── lore/               ← Worldbuilding
├── elements/           ← Elemental content
├── multiplayer/        ← Multiplayer design
├── migration/          ← Active migration guidance + doc principles
├── tracking/           ← Bugs and todos
└── archive/            ← Historical docs (execution logs, superseded references)
```

---

## For AI Assistants

When working with this codebase:

1. **Start with recent decisions**: Check [ideation-session-2026-01-19.md](design/ideation-session-2026-01-19.md) for latest finalized design
2. **Feature docs are canonical**: The `features/` folder contains authoritative system specs
3. **Check "Status" headers**: Documents marked "CURRENT" or "IMPLEMENTED" are reliable
4. **Items replaced boons**: Any "boon" references are outdated; use "items" instead
5. **Key concepts**:
   - Incarnation = summoner's attack target on battlefield
   - Level caps = normalize card power in elite battles
   - Shared content = account-wide items/cards locked for campaign

---

*For detailed onboarding, see [Start Here](start-here.md)*
