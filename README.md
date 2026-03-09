# Fateforged

A 1v1 real-time tactical battler built in Godot 4.5.

## Overview

Fateforged is a 1v1 real-time tactical battler where players summon elemental creatures to fight for them. Throughout the campaign, players encounter finite, non-replayable events that offer small sets of cards. Choosing one card permanently closes off the others for that run, and because the campaign has only a limited number of these junctions, every player's deck naturally diverges in composition and strategy.

This asymmetry — and the player's responsibility for shaping it — is the core of the game's identity and the reason it's called Fateforged: your fate is literally forged by the choices you make at each branching point.

The game features a 2.5D perspective with an angled perspective battle camera, creating a modern take on tactical card battlers.

## Quick Start

**Prerequisites:**
- Godot 4.5 or later
- Git

**Installation:**
```bash
git clone https://github.com/amari-charles/project-summoner.git
cd project-summoner
```

Open the project in Godot 4.5 and run the VFX test scene:
- `scenes/battle/battlefield/test_battle_vfx.tscn` - Sandbox for testing abilities and combat

## Documentation

**New to the project?** Start here:
- [📘 Getting Started](docs/start-here.md) - Comprehensive introduction for new developers

**Core Documentation:**
- [📊 Current State](docs/current-state.md) - Complete project overview and architecture
- [📜 Development History](docs/development-history.md) - Progress tracking, decisions, and context
- [📝 Changelog](docs/changelog.md) - Public release notes (for future versions)

**Developer Guides:**
- [Feature Documentation](docs/features/) - Card, combat, battlefield, and coordinate systems
- [Design Documents](docs/design/) - Vision, roadmap, and visual style references

## Project Structure

```
project-summoner/
├── assets/          # Game assets (characters, tilesets, UI)
├── data/            # JSON data files (cards, battles, animations)
├── docs/            # Documentation
├── resources/       # Godot resources (sprite frames, materials)
├── scenes/          # Godot scene files (.tscn)
└── scripts/         # GDScript code files (.gd)
```

## Key Features

- **Card-Based Combat** - Summon units using a deck of cards with mana costs
- **Real-Time Strategy** - Units move and attack autonomously with smart AI
- **Multiple Unit Types** - Melee, ranged, and special ability units
- **Campaign Mode** - Progress through battles with varying difficulty
- **Pannable Camera** - Explore the battlefield with mouse, touch, or keyboard

## Development

**Current Focus:** Foundation systems and visual polish
- Core combat mechanics
- VFX and animations
- UI/UX improvements
- Camera and battlefield systems

**Status:** Active development - Alpha stage

## Contributing

This is a personal project, but feedback and suggestions are welcome! Please see:
- [Technical Documentation](docs/technical/) - Integration status and bug tracking
- [Art Specifications](docs/art/asset-specifications.md) - Asset requirements and guidelines

## Tech Stack

- **Engine:** Godot 4.5
- **Language:** GDScript
- **Art Style:** Pixel art with 2.5D perspective
- **Target Platforms:** Desktop (PC/Mac), Mobile (iOS/Android)

## License

See [LICENSE](LICENSE) file for details.

## Links

- **Repository:** https://github.com/amari-charles/project-summoner
- **Issues:** https://github.com/amari-charles/project-summoner/issues
- **Discussions:** https://github.com/amari-charles/project-summoner/discussions

---

**Built with Godot 4.5** | **Last Updated:** 2025-12-11
