# Getting Started with Fateforged

**Status:** CURRENT
**Last Updated:** 2026-03-04
**Purpose:** New developer onboarding and documentation guide

Welcome to Fateforged! This document will help you get oriented with the project structure, key systems, and where to find information.

## Quick Links

| What do you want to do? | Where to go |
|-------------------------|-------------|
| See all documentation | [Documentation Index](README.md) |
| Understand the project vision | [Design Vision](project/vision.md) |
| See what's currently implemented | [Current State](project/current-state.md) |
| Learn the core game systems | [Features Index](features/README.md) |
| Understand campaign structure | [Campaign Structure](features/campaign/structure.md) |
| Learn the summoner system | [Summoner README](features/summoners/README.md) |
| Create a new card | [Card System](features/cards/system.md) |
| Add a new unit type | [Combat System](features/combat/system.md) |
| Find art asset specs | [UI Assets](art/ui-assets.md) |
| Check known bugs | [Bug Tracker](tracking/bugs.md) |
| See latest design decisions | [Ideation Session](design/ideation-session-2026-01-19.md) |

## Project Overview

**Genre:** Real-time tactical card battler
**Inspiration:** Mini Warriors, Clash Royale, Cult of the Lamb
**Engine:** Godot 4.5
**Perspective:** 2.5D (3D world with orthographic camera at 35° tilt)

### Core Gameplay Loop

Fateforged uses a two-phase battle system designed to create the fantasy of two armies clashing:

**Phase 1: PREPARATION (30 seconds)**
1. Both players start with a fixed mana pool (100 mana)
2. Summon units to build your army formation
3. Units spawn but remain **inactive** (they wait, don't fight yet)
4. Plan your strategy before the clash

**Phase 2: BATTLE (until victory)**
1. All units **activate** and begin fighting autonomously
2. Players can still summon reinforcements with remaining mana
3. Battle continues until one side's **Incarnation** is destroyed

**Win Condition:** Destroy the enemy's Incarnation — a magical manifestation of the summoner's power on the battlefield.

## Documentation Structure

```
docs/
├── README.md                  ← Central documentation index
├── start-here.md              ← You are here (onboarding)
├── project/                   Vision & planning
│   ├── vision.md              Core game vision
│   ├── brief.md               Studio-ready pitch doc
│   ├── current-state.md       Implementation reference
│   └── changelog.md           Public release notes
├── features/                  Feature specifications
│   ├── README.md              ← Features index
│   ├── cards/system.md        Card mechanics, level caps
│   ├── combat/system.md       Unit AI, damage
│   ├── campaign/
│   │   ├── structure.md       Paths, level caps, grinding
│   │   └── narrative.md       Story, writing guidelines
│   ├── summoners/
│   │   ├── README.md          Summoner overview
│   │   ├── architecture.md    Base system (MVP)
│   │   └── progression-system.md  Full progression (Post-MVP)
│   ├── items/system.md        Item slots (replaces boons)
│   ├── elemental-system.md    Elements and affinities
│   └── modifier-system.md     Stat modifications
├── design/                    Design decisions
│   ├── ideation-session-2026-01-19.md  Latest decisions
│   └── card-progression-economy.md     XP, resources
├── technical/                 Technical references
│   ├── unit-stat-pipeline.md
│   └── vfx/
├── architecture/              Code architecture
│   └── system-architecture.md
├── migration/                 Active migration guidance
│   ├── README.md              Migration status + archive links
│   └── documentation-guide.md Architecture doc principles
├── workflows/                 Development workflows
│   └── pr-review-guidelines.md
├── art/                       Art specifications
├── lore/                      Worldbuilding
│   ├── world.md               The Academy setting
│   └── characters/            Fateforgers and NPCs
├── tracking/                  Bugs and todos
├── elements/                  Elemental content
└── archive/                   Historical docs (including migrated plans/checklists)
```

## Project Structure

### Key Directories

**`assets/`** - Game assets (sprites, tilesets, sounds)
- `assets/characters/` - Character sprites and animations
- `assets/tilesets/` - Environment tilesets
- `assets/README.md` - Asset organization guide

**`data/`** - JSON data files
- `data/cards/` - Card definitions
- `data/battles/` - Campaign battle configs
- `data/animations/` - Animation frame data

**`scenes/`** - Godot scene files (.tscn)
- `scenes/battle/battlefield/` - Battle scenes and battlefield components
- `scenes/battle/units/` - Unit scene templates
- `scenes/` - Scene files (battle/, meta/, shared/)

**`scripts/`** - GDScript and C# code
- `scripts/battle/` - Battle domain (animations, battlefield, VFX, battle UI)
- `scripts/meta/` - Meta-game domain (screens, components, modals)
- `scripts/shared/` - Reusable UI components (card_visual, styled_button, etc.)
- `scripts/application/` - Lifecycle and orchestration (scene_manager, battle_context, etc.)
- `scripts/infrastructure/` - Shared data, billing, audio, constants
- `scripts/csharp/` - C# codebase (see `docs/architecture/system-architecture.md`)

**`resources/`** - Godot resources
- `resources/animations/` - SpriteFrames and animation data
- `resources/materials/` - Visual materials and shaders

**`docs/`** - Documentation (you're reading it!)

## Essential Reading

### For New Developers

1. **[Documentation Index](README.md)** - Central navigation for all docs
2. **[Features Index](features/README.md)** - Overview of all game systems
3. **[Card System](features/cards/system.md)** - How cards and units work
4. **[Combat System](features/combat/system.md)** - Unit AI and battle mechanics
5. **[Campaign Structure](features/campaign/structure.md)** - Paths, level caps, grinding

### For Understanding Recent Design

1. **[Ideation Session](design/ideation-session-2026-01-19.md)** - All finalized design decisions
2. **[Vision](project/vision.md)** - Core game identity and pillars
3. **[Summoner README](features/summoners/README.md)** - Summoner system overview

### For Artists

1. **[UI Assets](art/ui-assets.md)** - Technical requirements for assets
2. **[Visual Style References](art/visual-style-references.md)** - Art style guidelines
3. **`assets/README.md`** - How assets are organized

### For Designers

1. **[Design Vision](project/vision.md)** - Project goals and philosophy
2. **[Campaign Structure](features/campaign/structure.md)** - Path system, level caps
3. **[Item System](features/items/system.md)** - Equippable gear

### For Writers

1. **[Lore Overview](lore/README.md)** - All worldbuilding and narrative docs
2. **[World & Setting](lore/world.md)** - The Academy, locations, tone
3. **[Campaign Narrative](features/campaign/narrative.md)** - Writing guidelines for campaign content
4. **[Fateforger Bios](lore/characters/fateforgers/)** - Character templates to fill in

## Development Workflow

### Running the Project

1. Open project in Godot 4.5
2. Run one of these test scenes:
   - `scenes/battle/battlefield/test_battle_vfx.tscn` - VFX sandbox (infinite mana/HP)
   - `scenes/battle/battlefield/campaign_battle_3d.tscn` - Real battle with progression

### Making Changes

1. Create a feature branch: `git checkout -b feature/your-feature-name`
2. Make your changes
3. Test thoroughly
4. Create a pull request
5. Wait for approval before merging

See [`.claude/CLAUDE.md`](../.claude/CLAUDE.md) for detailed git workflow.

### Testing

**Manual Testing:**
- VFX Test Scene: `test_battle_vfx.tscn` (sandbox with infinite resources)
- Campaign Battle: `campaign_battle_3d.tscn` (real battle)
- Main Menu: `main_menu.tscn` (full game flow)

**Key Things to Test:**
- Card playing (drag and drop)
- Unit spawning and behavior
- Combat (melee, ranged, abilities)
- Camera panning (mouse, touch, keyboard)
- Win/loss conditions

## Current Development Status

**Phase:** Alpha - Foundation Systems
**Focus:** Core mechanics and visual polish
**Priority:** Quality over content

### What's Implemented

✅ Two-phase battle system (PREPARATION → BATTLE)
✅ Fixed mana pool (no regeneration during battle)
✅ Summon time mechanics with ghost spawn reveal effect
✅ Unit activation states (inactive during prep, active during battle)
✅ Card playing system with drag-and-drop
✅ Unit spawning and autonomous AI
✅ Melee and ranged combat
✅ Incarnation (win condition target)
✅ Pannable camera with boundaries
✅ VFX system for abilities
✅ Campaign progression
✅ Save/load system

### What's Next

See [Todos](tracking/todos.md) for planned features and tasks.

**Current priorities:**
1. Polish existing VFX and animations
2. Improve UI/UX feel
3. Enhance unit AI behaviors
4. Add more visual feedback (damage numbers, impact effects)

## Common Tasks

### Adding a New Card

1. Create JSON in `data/cards/` (see [Card System](features/cards/system.md))
2. Add sprite to `assets/characters/` or `assets/vfx/`
3. Test in VFX test scene

### Creating a New Unit Type

1. Define behavior in unit script (extend `Unit3D`)
2. Create sprite animations
3. Add to card system
4. Test combat behavior

### Modifying the Camera

See [Camera Controller](../scripts/battle/battlefield/camera_controller_3d.gd) - heavily commented for learning

## Getting Help

- **Questions about code?** Check inline comments (heavily documented)
- **Questions about design?** See [Design Vision](project/vision.md)
- **Found a bug?** Add to [Bug Tracker](tracking/bugs.md)
- **Want to contribute?** Follow git workflow in [`.claude/CLAUDE.md`](../.claude/CLAUDE.md)

## Next Steps

1. Read [Current State](project/current-state.md) for a complete overview
2. Run the VFX test scene to see the game in action
3. Explore the codebase - scripts are heavily commented
4. Check [Todos](tracking/todos.md) to see what's planned
5. Pick a task and create a feature branch!

---

**Welcome to the team! Let's build something great.**

*Related Documents:*
- [Current State](project/current-state.md)
- [Card System](features/cards/system.md)
- [Combat System](features/combat/system.md)
- [Design Vision](project/vision.md)
