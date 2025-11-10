# Getting Started with Project Summoner

**Status:** CURRENT
**Last Updated:** 2025-01-10
**Purpose:** New developer onboarding and documentation guide

Welcome to Project Summoner! This document will help you get oriented with the project structure, key systems, and where to find information.

## Quick Links

| What do you want to do? | Where to go |
|-------------------------|-------------|
| Understand the project vision | [Design Vision](design/vision.md) |
| See what's currently implemented | [Current State](current-state.md) |
| Learn the 3D architecture | [Current State - Architecture](current-state.md#architecture) |
| Create a new card | [Card System API](api/card-system.md) |
| Add a new unit type | [Combat System API](api/combat-system.md) |
| Understand the coordinate system | [Coordinate System](api/coordinate-system.md) |
| Find art asset specs | [Asset Specifications](art/asset-specifications.md) |
| Check known bugs | [Bug Tracker](technical/bugs.md) |

## Project Overview

**Genre:** Real-time tactical card battler
**Inspiration:** Mini Warriors, Clash Royale, Cult of the Lamb
**Engine:** Godot 4.5
**Perspective:** 2.5D (3D world with orthographic camera at 35° tilt)

### Core Gameplay Loop

1. Player builds a deck of unit/spell cards
2. Cards have mana costs; mana regenerates over time
3. Play cards to summon units on the battlefield
4. Units move and attack autonomously
5. Destroy the enemy base to win

## Documentation Structure

```
docs/
├── start-here.md             ← You are here
├── current-state.md           Main project reference
├── changelog.md               Public release notes (for future)
├── development-history.md     Internal progress tracking
├── api/                       System APIs and references
│   ├── card-system.md
│   ├── hero-system.md
│   ├── combat-system.md
│   ├── battlefield-system.md
│   └── coordinate-system.md
├── design/                    Design documents
│   ├── vision.md
│   ├── roadmap.md
│   └── visual-style-references.md
├── art/                       Art specifications
│   └── asset-specifications.md
└── technical/                 Technical references
    ├── bugs.md
    └── integration-status.md
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
- `scenes/battlefield/` - Battle scenes and battlefield components
- `scenes/units/` - Unit scene templates
- `scenes/ui/` - UI components

**`scripts/`** - GDScript code (.gd)
- `scripts/core/` - Core game systems
- `scripts/units/` - Unit behavior
- `scripts/battlefield/` - Battlefield and camera logic
- `scripts/ui/` - UI controllers

**`resources/`** - Godot resources
- `resources/animations/` - SpriteFrames and animation data
- `resources/materials/` - Visual materials and shaders

**`docs/`** - Documentation (you're reading it!)

## Essential Reading

### For New Developers

1. **[Current State](current-state.md)** - Read this first! Complete overview and architecture
2. **[Card System API](api/card-system.md)** - How cards and units work
3. **[Combat System API](api/combat-system.md)** - Unit AI and battle mechanics
4. **[Coordinate System](api/coordinate-system.md)** - Understanding 3D positioning

### For Artists

1. **[Asset Specifications](art/asset-specifications.md)** - Technical requirements for assets
2. **[Visual Style References](design/visual-style-references.md)** - Art style guidelines
3. **`assets/README.md`** - How assets are organized

### For Designers

1. **[Design Vision](design/vision.md)** - Project goals and philosophy
2. **[Roadmap](design/roadmap.md)** - Planned features and milestones
3. **[Combat System](api/combat-system.md)** - Battle mechanics and AI behavior

## Development Workflow

### Running the Project

1. Open project in Godot 4.5
2. Run one of these test scenes:
   - `scenes/battlefield/test_battle_vfx.tscn` - VFX sandbox (infinite mana/HP)
   - `scenes/battlefield/campaign_battle_3d.tscn` - Real battle with progression

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

✅ Card playing system with mana costs
✅ Unit spawning and autonomous AI
✅ Melee and ranged combat
✅ Base HP and destruction
✅ Pannable camera with boundaries
✅ VFX system for abilities
✅ Campaign progression
✅ Save/load system

### What's Next

See [Roadmap](design/roadmap.md) for detailed plans.

**Current priorities:**
1. Polish existing VFX and animations
2. Improve UI/UX feel
3. Enhance unit AI behaviors
4. Add more visual feedback (damage numbers, impact effects)

## Common Tasks

### Adding a New Card

1. Create JSON in `data/cards/` (see [Card System API](api/card-system.md))
2. Add sprite to `assets/characters/` or `assets/vfx/`
3. Test in VFX test scene

### Creating a New Unit Type

1. Define behavior in unit script (extend `Unit3D`)
2. Create sprite animations
3. Add to card system
4. Test combat behavior

### Modifying the Camera

See [Camera Controller](../scripts/battlefield/camera_controller_3d.gd) - heavily commented for learning

## Getting Help

- **Questions about code?** Check inline comments (heavily documented)
- **Questions about design?** See [Design Vision](design/vision.md)
- **Found a bug?** Add to [Bug Tracker](technical/bugs.md)
- **Want to contribute?** Follow git workflow in [`.claude/CLAUDE.md`](../.claude/CLAUDE.md)

## Next Steps

1. Read [Current State](current-state.md) for a complete overview
2. Run the VFX test scene to see the game in action
3. Explore the codebase - scripts are heavily commented
4. Check [Roadmap](design/roadmap.md) to see what's planned
5. Pick a task and create a feature branch!

---

**Welcome to the team! Let's build something great.**

*Related Documents:*
- [Current State](current-state.md)
- [Card System](api/card-system.md)
- [Combat System](api/combat-system.md)
- [Design Vision](design/vision.md)
