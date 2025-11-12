# Project Summoner - Development History

**Purpose:** Internal record of development progress, technical decisions, and lessons learned.

**Note:** This is not a public changelog. For release notes, see [changelog.md](changelog.md).

---

## Table of Contents

- [Visual Polish & Game Feel Improvements (2025-11-12)](#visual-polish--game-feel-improvements-2025-11-12)
- [Documentation & Asset Reorganization (2025-11-10)](#documentation--asset-reorganization-2025-11-10)
- [3D Architecture & Visual Systems (2025-11-08)](#3d-architecture--visual-systems-2025-11-08)
- [Collection & Deck Building (2025-11-05)](#collection--deck-building-2025-11-05)
- [Save System Architecture (2025-11-05)](#save-system-architecture-2025-11-05)
- [Core Card System Expansion (2025-11-04)](#core-card-system-expansion-2025-11-04)
- [Foundation: Core Gameplay (2025-11-04)](#foundation-core-gameplay-2025-11-04)
- [Initial Project Setup (2025-11-03)](#initial-project-setup-2025-11-03)

---

## Visual Polish & Game Feel Improvements (2025-11-12)

### What We Built
- Predictive targeting system for projectiles
- HP bar positioning fix
- Building hit/damage feedback animations
- Pause menu implementation with ESC key support
- Speed control toggle (1x/2x) for campaign mode
- Reward screen bug fix (preventing duplicate rewards on replay)
- Card visual redesign with element-based theming

### Technical Decisions

**Predictive Targeting:**
- **Decision:** Implement projectile prediction to lead moving targets
- **Why:** Projectiles were missing moving targets frequently, making ranged units feel weak and unreliable
- **Approach:** Calculate target's future position based on velocity and projectile speed, aim at intercept point
- **Impact:** Significantly improved ranged unit effectiveness and combat feel

**HP Bar Positioning:**
- **Decision:** Fix HP bars floating too high above units
- **Why:** Visual bug affecting all combat readability
- **Approach:** Adjusted HPBarManager positioning logic to better align with unit sprites
- **Result:** HP bars now properly positioned above units

**Building Hit Feedback:**
- **Decision:** Add flash animation with dynamic speed based on attack intensity
- **Why:** Players needed clear visual feedback when bases take damage
- **Approach:** Flash effect that scales with damage amount, making heavy hits more noticeable
- **Impact:** Critical game state changes (base damage) now have appropriate visual weight

**Pause Menu:**
- **Decision:** Add pause functionality with menu overlay
- **Why:** Players need ability to pause during battles, especially in campaign mode
- **Features:**
  - ESC key to pause/unpause
  - Pause button in battle HUD
  - Pause menu with Resume/Settings/Quit options
  - Proper game state freezing (pause tree)
- **Challenge:** Fixed timing issues with tween cleanup and lambda capture errors

**Speed Control:**
- **Decision:** Add 1x/2x speed toggle for campaign battles
- **Why:** Players wanted to speed through easier campaign battles
- **Approach:** Toggle button in battle HUD, uses Engine.time_scale
- **Constraint:** Only available in campaign mode, not PvP (to prevent fairness issues)

**Card Visual Redesign:**
- **Decision:** Implement element-based visual theming for cards
- **Why:** Cards needed more visual distinction and polish
- **Approach:** Element-colored borders, gradients, and visual effects based on card element type
- **Impact:** Cards now have more personality and are easier to distinguish at a glance

**Reward Screen Fix:**
- **Decision:** Fix bug where reward screen showed "already completed" on first-time wins
- **Why:** Confusing player experience, made it unclear if rewards were granted
- **Approach:** Fixed reward callback timing and state management
- **Result:** Reward screen now correctly shows rewards for first-time battle completions

### Lessons Learned
- Predictive targeting is essential for feel in real-time combat games
- Visual feedback for critical events (base damage) can't be understated
- Pause menu is table-stakes for single-player content
- Speed control increases player agency and reduces friction in repetitive content
- Small visual bugs (HP bar positioning) have outsized impact on perceived polish

---

## Documentation & Asset Reorganization (2025-11-10)

### What We Built
- Complete documentation restructure into organized folders (api/, design/, art/, technical/)
- Extracted system specs from monolithic PROJECT_DOC.md into separate files
- Standardized all filenames to lowercase-with-hyphens convention
- Created comprehensive asset reorganization with snake_case naming
- Established dual documentation system (changelog + dev history)

### Technical Decisions

**Documentation Structure:**
- **Decision:** Split PROJECT_DOC.md into api/hero-system.md, api/combat-system.md, api/battlefield-system.md
- **Why:** Monolithic doc was becoming unmaintainable. Separate files easier to find, update, and reference.
- **Approach:** Keep PROJECT_DOC.md as index pointing to organized docs

**Naming Convention:**
- **Decision:** Use lowercase-with-hyphens for all documentation files
- **Why:** Consistency with api/ and design/ subdirectories. More web-friendly than CAPS or snake_case.
- **Changed:** CURRENT_STATE.md → current-state.md, 00-START-HERE.md → start-here.md

**Asset Organization:**
- **Decision:** Flatten asset hierarchy, move all character sprites to characters/, use snake_case naming
- **Why:** Deep nesting (character_packs/tiny_rpg/Characters/...) was cumbersome. Flat structure easier to navigate.
- **Approach:** characters/, projectiles/, _source/ for clean separation

### Lessons Learned
- Documentation debt accumulates fast - better to organize early
- Consistent naming prevents confusion and broken links
- Git mv preserves history during reorganization

---

## 3D Architecture & Visual Systems (2025-11-08)

### What We Built
- Transitioned from 2D to 3D battlefield with orthographic camera (35° tilt)
- Implemented ground shadow system using Decal projections
- Added dynamic feet-based sprite positioning for accurate shadow alignment
- Created summer tileset battlefield with painted art style
- Implemented pannable camera with boundary constraints and multiple control schemes

### Technical Decisions

**2D vs 3D Architecture:**
- **Decision:** Use 3D world with orthographic camera instead of pure 2D
- **Why:** Needed depth for visual appeal (shadows, layering) while maintaining gameplay readability
- **Problem Solved:** 2D sprites looked flat. 3D allows depth without perspective distortion.
- **Approach:** Sprites on 3D planes at Y=0 (ground), orthographic camera at 35° preserves "2.5D" aesthetic

**Shadow System:**
- **Decision:** Use Decal-based blob shadows instead of sprite shadows or shaders
- **Why:** Industry standard for real-time games. Performant, visually clean, works with sprite-based units.
- **Iterations:**
  1. Tried sprite shadows → clipping issues
  2. Tried shader-based → positioning problems
  3. Settled on Decal projection → robust solution
- **Challenge:** Aligning shadows to sprite feet required manual offset system (`feet_offset_y`)

**Camera System:**
- **Decision:** Player-controlled panning with drag, edge scroll, and keyboard controls
- **Why:** Battlefield wider than screen. Players need tactical overview.
- **Constraints:** Bounded camera prevents seeing beyond battlefield edges

### Lessons Learned
- Coordinate system architecture matters early - changing from 2D→3D mid-project was painful
- Shadow positioning is harder than it looks - feet-based alignment requires per-sprite tuning
- Orthographic camera eliminates many 3D headaches while keeping depth benefits

---

## Collection & Deck Building (2025-11-05)

### What We Built
- Collection screen UI for viewing owned cards
- Deck builder interface with drag/double-click card management
- Tab system separating "Collection" and "My Decks" views
- Individual card instance display (not just counts)
- Real-time deck validation and card filtering

### Technical Decisions

**Instance-Based Cards:**
- **Decision:** Store cards as individual instances with unique IDs, not aggregated counts
- **Why:** Supports future card variance system (each card can have unique stats/modifiers)
- **Trade-off:** More complex data structure, but enables core design pillar (uniqueness)

**UI Flow:**
- **Decision:** Click to select, double-click to add to deck
- **Why:** Single-click felt too easy to accidentally add cards. Double-click requires intent.
- **Alternative Considered:** Drag-only system (rejected as too cumbersome for many cards)

**Deck Management:**
- **Decision:** Hide deck cards from collection panel while editing
- **Why:** Prevents confusion about what's available. Clear visual separation.
- **Refresh on Switch:** Deck changes reflected immediately when switching between decks

### Lessons Learned
- UI affordances matter - users need clear feedback on available actions
- Instance-based design pays off for flexibility but requires more careful data management
- Tab systems scale better than single-view overload

---

## Save System Architecture (2025-11-05)

### What We Built
- Repository pattern for data persistence
- ProfileService, CollectionService, DeckService as autoloaded singletons
- JSON-based save format with UUID support
- Card catalog system with validation

### Technical Decisions

**Repository Pattern:**
- **Decision:** Use repository pattern with duck typing instead of abstract base classes
- **Why:** Godot's GDScript doesn't support true interfaces/abstracts. Duck typing more idiomatic.
- **Approach:** JsonProfileRepo, JsonCollectionRepo implement expected methods without inheritance

**Service Layer:**
- **Decision:** Autoload singletons for ProfileService, CollectionService, DeckService
- **Why:** Global access needed across scenes. Godot autoload is standard pattern.
- **Caveat:** Removed `class_name` from autoloaded scripts (Godot limitation)

**Data Format:**
- **Decision:** JSON for save files, UUIDs for all entities
- **Why:** Human-readable for debugging. UUIDs prevent ID conflicts, support future networking.
- **Structure:** Row-oriented (players table, cards table, decks table) for DB-ready schema

### Lessons Learned
- Fight Godot idioms at your peril - duck typing beats rigid OOP in GDScript
- Early schema design matters - UUID-based system future-proofs data layer
- Autoload + singleton pattern works well for game services but has quirks (no class_name)

---

## Core Card System Expansion (2025-11-04)

### What We Built
- Ranged unit system with projectile tracking and homing
- Spell cards (Fireball with area damage)
- Structure cards (Wall for defense, Tower for area control)
- Drag-and-drop hand UI replacing click-to-play
- Win/loss enhancement screens

### Technical Decisions

**Drag-and-Drop vs Click:**
- **Decision:** Remove click-to-play entirely, use drag-and-drop only
- **Why:** Drag feels more tactile and intentional. Mobile-friendly. Reduces misclicks.
- **Philosophy:** No backwards compatibility - commit to new UX fully

**Projectile System:**
- **Decision:** Light homing for ranged attacks instead of straight-line projectiles
- **Why:** Straight projectiles miss too often with moving targets. Light homing feels better without looking "unfair"
- **Parameters:** 120-180 px/s speed, 2s timeout, 8px hit radius

**Structure Units:**
- **Decision:** Structures are units with special behavior (stationary, high HP)
- **Why:** Reuse Unit base class instead of separate entity type. Keeps code unified.
- **Tags:** `is_structure` flag for special cases

### Lessons Learned
- UX changes should be decisive - hybrid systems create confusion
- Projectile feel matters more than strict realism
- Inheritance vs composition: Sometimes "special unit" beats "new entity type"

---

## Foundation: Core Gameplay (2025-11-04)

### What We Built
- Base objects as attackable win condition structures
- Mana regeneration system with float accumulation
- Unit AI with always-advance behavior and base targeting
- Horizontal battlefield orientation (left vs right)
- Player-controlled camera with drag panning
- Type-safe team system using enums

### Technical Decisions

**Battlefield Orientation:**
- **Decision:** Horizontal (left-to-right) instead of vertical
- **Why:** Matches Clash Royale UX. More natural for wide screens. Better camera panning.
- **Changed:** Player base at left (100, 540), enemy at right (1820, 540)

**Base System:**
- **Decision:** Separate Base class from Summoner, bases are attackable structures
- **Why:** Bases are physical targets, Summoners are logical controllers. Type separation prevents confusion.
- **Challenge:** Fixed multiple "trying to assign Base to Summoner" type errors

**Unit AI:**
- **Decision:** Always-advance AI - units move toward enemy base when no targets in aggro range
- **Why:** Prevents stalemates. Forces tempo. Matches design goal of "3-5 minute matches"
- **Aggro Radius:** Units acquire targets within radius, chase if out of attack range, else advance

**Mana System:**
- **Decision:** Float-based with per-frame accumulation (was int(delta) = 0 bug)
- **Why:** Smooth regeneration. Allows fractional mana gain rates.
- **Signal:** Emit mana_changed every frame for real-time UI updates

### Lessons Learned
- Type safety in GDScript requires discipline (enum Team instead of int)
- UI anchors > absolute positioning for responsive layouts
- Always-advance AI simple but effective for maintaining game pace

---

## Initial Project Setup (2025-11-03)

### What We Built
- Basic Godot 4.5 project structure
- Core game controller framework
- Team-based unit spawning
- Simple battlefield with zones

### Technical Decisions

**Engine Choice:**
- **Decision:** Godot 4.5
- **Why:** Open source, good 2D/3D support, GDScript fast to iterate
- **Trade-offs:** Less mature than Unity/Unreal but no licensing concerns

**Project Structure:**
- **Decision:** Separate scenes/, scripts/, data/, assets/ directories
- **Why:** Clear separation of concerns. Scales as project grows.

### Lessons Learned
- Start with clear folder structure - harder to reorganize later
- Godot's scene system powerful but requires understanding node hierarchy early

---

## Key Design Principles

Throughout development, we've maintained these core principles:

1. **No Backwards Compatibility** - When making changes, commit fully (drag-drop replaced click entirely)
2. **Type Safety Matters** - Use enums and type hints to prevent runtime errors
3. **Godot Idioms First** - Fight the engine's patterns at your own risk
4. **3-5 Minute Matches** - All design decisions support this pacing goal
5. **Quality Over Content** - Polish foundation before adding more battles/cards

---

## Architecture Evolution

### Phase 1: Pure 2D (Nov 3-4)
- Top-down 2D battlefield
- Sprite-based everything
- Simple but flat visually

### Phase 2: 3D Hybrid (Nov 8)
- 3D world with orthographic camera
- Sprites on 3D planes
- Decal shadows for depth
- **Why Changed:** Visual appeal needed depth without losing readability

### Phase 3: Data Layer (Nov 5)
- Repository pattern for saves
- Instance-based card storage
- UUID everything
- **Why Changed:** Needed scalable persistence for collection system

---

*This document is living - updated as we build and learn.*

**Last Updated:** 2025-11-12
