# Changelog

All notable changes to Project Summoner will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

---

## [Unreleased]

### Documentation
- Complete documentation reorganization into proper folder structure
- Extract system specs from PROJECT_DOC.md into separate API docs
- Fix all Godot version references (4.3 → 4.5)
- Fix 2D/3D architecture descriptions throughout docs
- Remove broken links and update cross-references

### Assets
- Rename all assets to snake_case naming convention
- Flatten asset hierarchy for better organization
- Separate source files from game-ready assets
- Move all character sprites to characters/ directory

---

## [2025-11-10] - Battlefield Visual Update

### Added
- Summer tileset battlefield with painted art style
- Pannable camera system for battlefield navigation
- Camera boundary constraints
- Touch and mouse camera controls

### Fixed
- Shadow system with PlaneMesh ground architecture
- Dynamic feet-based sprite positioning for accurate shadows
- Manual feet offset system for precise shadow alignment
- Shadow visibility and clipping issues

---

## [2025-11-08] - Combat System Improvements

### Added
- Multi-axis attack range system
- Unit classification (Ground/Air, Melee/Ranged)
- Shader-based ground shadows for units
- Decal-based shadow projection (industry standard)

### Fixed
- Attack animation interruption when units collide
- Animation state management during combat
- HP bar positioning
- Coordinate system architecture (ground units at Y=0)
- Shadow visibility and positioning

---

## [2025-11-05] - Collection & Deck Building

### Added
- Collection screen UI
- Deck builder interface
- Card catalog system with validation
- Tab system for Collection and My Decks views
- Click and double-click behaviors for deck building
- Individual card instance display

### Changed
- Deck building rules and card management
- Collection panel refreshes when switching decks
- Deck cards hidden from collection panel

---

## [2025-11-05] - Save System

### Added
- Scalable save system with repository pattern
- Profile service for player data
- Collection service for card management
- Deck service for deck persistence
- JSON-based save format with UUID support

### Technical
- Duck typing approach for Godot service architecture
- Autoloaded singleton services
- Repository pattern for data persistence

---

## [2025-11-04] - UI & Visual Polish

### Added
- Main menu with navigation
- Win/loss enhancement screen
- Phase 1 ground visual system
- Drag-and-drop hand UI with animations
- Player-controlled camera
- Git workflow rules to CLAUDE.md

### Changed
- Base HP reduced from 1000 to 300
- Improved wall visuals

### Fixed
- Game over UI visibility when paused
- Timer update issues
- FogOverlay white rectangle bug
- UI rendering issues

---

## [2025-11-04] - Card System Expansion

### Added
- Ranged unit system with archer card
- Spell cards (Fireball)
- Structure cards (Wall, Tower)
- Projectile system for ranged attacks

---

## [2025-11-04] - Core Gameplay

### Added
- Base objects as attackable structures
- Mana regeneration system
- Card playing mechanics
- Unit AI and autonomous behavior
- Win/loss conditions

### Changed
- Battlefield orientation from vertical to horizontal
- UI positioning using anchors for responsive layout

### Fixed
- Critical mana regeneration bugs
- Unit AI targeting issues
- Type errors in base HP checks
- Type confusion between Base and Summoner classes
- Team parameter type mismatches

---

## [2025-11-03] - Initial Release

### Added
- Initial Godot project setup
- Basic battlefield structure
- Core game controller
- Unit spawning system
- Team-based gameplay

---

## Legend

- **Added** - New features or content
- **Changed** - Changes to existing functionality
- **Fixed** - Bug fixes and corrections
- **Removed** - Removed features or files
- **Technical** - Under-the-hood improvements
- **Documentation** - Documentation changes

---

*This changelog is maintained manually and reflects major milestones and changes. For complete commit history, see `git log`.*
