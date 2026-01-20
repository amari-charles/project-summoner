# Fateforged - Current State

**Last Updated:** 2026-01-19
**Version:** Pre-Alpha (Phase 2 - Core Loop Build)

## Project Overview

Fateforged is a 1v1 real-time tactical battler where players summon elemental creatures to fight for them. Built in Godot 4.5 with a 2.5D perspective, players use cards to spawn units, cast spells, and deploy structures on a 3D battlefield.

---

## Recent Design Updates (2026-01-19)

See **[Ideation Session](../design/ideation-session-2026-01-19.md)** for all finalized design decisions.

**Key Changes:**
- **Campaign Structure**: One campaign for all summoners, elite vs standard paths, level caps
- **Items Replace Boons**: 4 item slots (Grimoire, Weapon/Staff, Ring, Vestments)
- **Level Cap System**: Cards floored to cap, prevents grinding from trivializing elite content
- **XP Distribution**: Only cards in deck gain XP; replay battles for XP only

**New Documentation:**
- [Campaign Structure](../features/campaign/structure.md) - Paths, level caps, grinding
- [Item System](../features/items/system.md) - Equippable gear
- [Features Index](../features/README.md) - All game systems

---

## Architecture

### Core Systems (Implemented)

#### Game Controller (`scripts/core/game_controller_3d.gd`)
- Two-phase battle system (PREPARATION → BATTLE)
- Preparation phase: 30 seconds for army building
- Battle phase: units activate and fight until victory
- Win/loss detection (Incarnation destruction)
- Game pause and restart functionality
- **Signals:** `phase_changed`, `time_updated`, `game_ended`

#### Unit System (`scripts/units/unit_3d.gd`)
- Base class for all battlefield entities
- Team-based (PLAYER, ENEMY, NEUTRAL)
- **Activation states:** INACTIVE (during prep), ACTIVE (during battle)
- Movement with collision detection
- Attack system with range checking
- Health and death handling
- **Unit Types:** Melee, Ranged, Flying

#### Card System (`scripts/cards/card.gd`)
- Base card class with drag-and-drop
- Card types: UNIT, SPELL, STRUCTURE
- **Summon time** — delay before unit appears (with ghost spawn reveal effect)
- **Rarity system** — Affects spawn count per card: Common (up to 12 units), Uncommon (6), Epic (3), Legendary (1)
- Hand management with visual layout
- Mana cost system

#### Summoner System (`scripts/core/summoner.gd`)
- **Unified entity** — both commander and attack target
- **Fixed mana pool** (100 mana by default, no regeneration)
- **HP system** (300 HP by default, destruction triggers game end)
- Card hand management with summon time delay
- Hit feedback animation (flash + shake on damage)
- **Signals:** `mana_changed`, `hand_changed`, `hp_changed`, `summoner_destroyed`, `casting_started`, `casting_completed`

### Persistence Systems (Implemented - Phase 1)

#### Repository Layer (`scripts/data/`)
**Architecture:** Repository pattern with swappable implementations

**IProfileRepo** (`profile_repository.gd`) - Abstract base class
- Defines contract for all profile storage implementations
- Methods: load_profile, save_profile, grant_cards, update_resources, upsert_deck, etc.
- Signals: profile_loaded, profile_saved, save_failed, data_changed
- **Purpose:** Abstraction layer allowing DB swap without code changes

**JsonProfileRepo** (`json_profile_repository.gd`) - JSON file implementation
- Atomic writes (temp file → rename) for corruption prevention
- Dual backup system (profile.bak1, profile.bak2)
- Debounced autosave (0.5s idle + immediate checkpoints)
- Write-ahead log (WAL) for future sync support
- DB-ready schema (UUIDs, row-oriented data)
- **File structure:** `user://profiles/{profile_id}/profile.json`
- **Data shape:** See "Data Schema" section below

#### Service Layer (`scripts/services/`)
**Architecture:** Domain services that wrap repository for business logic

**EconomyService** (`economy_service.gd`) - Autoload: `Economy`
- Resource operations: add_gold, spend, grant_rewards
- Validation: can_afford
- Queries: get_gold, get_essence, get_fragments
- **Signals:** resources_changed, transaction_completed, transaction_failed
- **Usage:** `Economy.add_gold(50)`, `Economy.spend({"gold": 100})`

**CollectionService** (`collection_service.gd`) - Autoload: `Collection`
- Card operations: grant_cards, remove_card, dismantle_card
- Queries: list_cards, get_card_count, get_collection_summary
- **Signals:** collection_changed, cards_granted, card_removed
- **Usage:** `Collection.grant_card("fireball", "rare")`

**DeckService** (`deck_service.gd`) - Autoload: `Decks`
- Deck operations: create_deck, update_deck, delete_deck
- Validation: validate_deck, get_validation_errors, clean_deck
- Deck size: 30 cards (MIN_DECK_SIZE = MAX_DECK_SIZE = 30)
- **Signals:** deck_changed, deck_created, deck_deleted, validation_failed
- **Usage:** `Decks.create_deck("My Deck", [instance_ids...])`

#### Debug Tools (`scripts/debug/`)

**DevConsole** (`dev_console.gd`) - Autoload: `DevConsole`
- Commands: `/save_wipe`, `/save_grant_cards <N>`, `/save_add_gold <amount>`
- Test corruption recovery: `/save_corrupt`, `/save_reload`
- Inspection: `/save_info` (prints full save state)
- Deck testing: `/save_create_deck <name>`
- **Hotkey:** F12 (future console UI)

### UI Systems (Implemented)

#### Main Menu (`scenes/ui/main_menu.tscn`)
- Entry point for the game
- **Buttons:**
  - PLAY → Launches test_game.tscn
  - COLLECTION → Placeholder (not implemented)
  - SETTINGS → Placeholder (not implemented)
  - QUIT → Exits game

#### Game UI (`scripts/ui/game_ui.gd`)
- Timer display (3:00 countdown)
- Victory/defeat screen
- Restart button (process_mode = ALWAYS for pause compatibility)
- **Signals:** Connected to GameController for updates

#### Hand UI (`scripts/ui/hand_ui.gd`)
- Visual card layout with spacing
- Drag-and-drop interaction
- Animated card movement
- Drop zone validation

### Visual Systems (Implemented)

#### Battlefield (`scenes/battlefield/battlefield.tscn`)
- 1920x1080 3D orthographic camera view with 2.5D perspective
- Tiled grass ground texture (z-index: -100)
- Player zone (left, blue tint)
- Enemy zone (right, red tint)
- Midline and ground line markers

#### Ground System (`scripts/battlefield/ground.gd`)
- Texture-based ground with configurable scale
- 16x16 grass tile extracted from commission tileset
- Region-enabled sprite for efficient tiling

### Project Structure

See `docs/start-here.md` for a current overview of the codebase organization.

Key directories:
- `scripts/` - GDScript source code
- `scenes/` - Godot scene files
- `assets/` - Art, audio, and other resources
- `docs/` - Documentation
- `tests/` - GUT unit tests

## Data Schema

The save system uses a DB-ready schema with UUIDs and row-oriented data structures:

```gdscript
{
  # Profile root
  "version": 1,
  "profile_id": "uuid",
  "updated_at": 1234567890,
  "catalog_version": "1.0.0",

  # Resources (single row per profile)
  "resources": {
    "profile_id": "uuid",
    "gold": 100,
    "essence": 0,
    "fragments": 0,
    "updated_at": 1234567890
  },

  # Card instances (array of rows)
  "collection": [
    {
      "id": "card-instance-uuid",
      "profile_id": "uuid",
      "catalog_id": "fireball",
      "rarity": "common",
      "roll_json": null,  # Future: stat rolls {"stat_delta": "+3%"}
      "created_at": 1234567890
    }
  ],

  # Decks (array of rows)
  "decks": [
    {
      "id": "deck-uuid",
      "profile_id": "uuid",
      "name": "Starter Deck",
      "created_at": 1234567890
    }
  ],

  # Deck cards (junction table)
  "deck_cards": [
    {
      "deck_id": "deck-uuid",
      "card_instance_id": "card-instance-uuid",
      "slot_index": 0
    }
  ],

  # Write-ahead log (for future sync)
  "wal": [
    {
      "op_id": "uuid-timestamp",
      "profile_id": "uuid",
      "action": "grant_card",
      "params": {"catalog_id": "fireball", "rarity": "rare"},
      "timestamp": 1234567890
    }
  ],

  # Metadata
  "meta": {
    "tutorial_flags": {},
    "achievements": {},
    "analytics_opt_in": false
  },

  # Last match info
  "last_match": {
    "seed": null,
    "result": null,
    "duration_s": null
  },

  # User settings
  "settings": {
    "sfx_volume": 1.0,
    "music_volume": 1.0,
    "lang": "en"
  }
}
```

**Key Design Choices:**
- **UUID-based:** All entities use stable UUIDs (profile_id, card instance IDs, deck IDs)
- **Row-oriented:** Data shaped like database tables (easy to migrate to SQL)
- **Instance-based collection:** Cards stored as individual instances, not counts (supports variants)
- **Junction table:** deck_cards relates decks to card instances (normalized design)
- **Timestamps:** created_at and updated_at for audit trail
- **WAL:** Write-ahead log captures operations for future sync

## Design Decisions

### 1. No Backwards Compatibility
**Decision:** When implementing new features, always remove old code paths completely. Never maintain dual implementations or fallback mechanisms.

**Rationale:** Keeps codebase clean and maintainable. Example: When drag-and-drop was implemented, click-to-play was removed entirely.

### 2. Git Workflow - Feature Branches + PR Approval
**Decision:** All non-trivial work must be done on feature branches with PR approval before merging.

**Process:**
1. Create feature branch
2. Make commits on branch
3. Push and create PR
4. **Wait for user approval**
5. User reviews and tests
6. Merge only after explicit approval

**Exceptions:** Trivial changes (typos, minor tweaks) can go straight to main if explicitly approved.

### 3. 3D with Orthographic Camera (2.5D Style)
**Decision:** Use 3D architecture with orthographic camera and 35° tilt for 2.5D aesthetic.

**Rationale:** Provides depth and visual appeal while maintaining readability. Orthographic camera eliminates perspective distortion, keeping gameplay clear at all depths.

### 4. Instance-Based Card Collection
**Decision:** Store cards as individual instances with unique IDs, not just counts.

**Rationale:** Future-proofs for card variants, stat rolls, and progression systems. More complex now but prevents major refactoring later.

**Implementation:** Each card in collection has:
```gdscript
{
  "instance_id": "uuid-1",
  "card_id": "fireball",
  "rarity": "common",
  "variant": null  // Future: stat mods, effect changes
}
```

### 5. Repository Pattern for Data Persistence
**Decision:** Use repository pattern with abstract interface and swappable implementations.

**Architecture:**
```
UI/Gameplay
    ↓
Domain Services (Economy, Collection, Decks)
    ↓
Repository Interface (IProfileRepo)
    ↓
Implementation (JsonProfileRepo → future: DbProfileRepo, SupabaseRepo)
```

**Rationale:**
- **Zero-refactor DB migration:** Swap JSON → DB by implementing `DbProfileRepo`, no game logic changes
- **Testability:** Mock repository for unit tests
- **Clear boundaries:** UI never touches storage directly, only services
- **Industry standard:** Repository pattern is proven for data abstraction
- **Future-proof:** Ready for cloud sync, multi-backend, offline-first patterns

**Implementation:**
- `IProfileRepo` (abstract base class) defines contract
- `JsonProfileRepo` implements JSON file storage with atomic writes
- `EconomyService`, `CollectionService`, `DeckService` call repo via interface
- All autoloads registered in `project.godot` for global access

### 6. JSON Save Format
**Decision:** Use JSON for save files, not binary or Godot's var2str format.

**Rationale:**
- Human-readable for debugging
- Cross-platform compatible
- Easy version migration
- Can be edited manually in dev
- Good balance of simplicity vs functionality

**Trade-offs:**
- Larger file size than binary (acceptable for card game)
- Easier to hack (can add anti-cheat layer later if needed)
- Slightly slower parsing (negligible for our data size)

### 7. Debounced Autosave
**Decision:** Autosave after 0.5-1.0s of inactivity, with immediate checkpoint saves.

**Rationale:**
- Prevents performance issues from constant disk writes
- Immediate saves on critical events (match end, deck save)
- Industry standard pattern (GDQuest, Godot RPG tutorials)

### 8. Dual Backup System
**Decision:** Maintain two backup files (save.bak1, save.bak2) in addition to main save.

**Rationale:**
- Protects against corruption during write
- Provides recovery from cascading failures
- Minimal disk space cost (~3x file size)
- Industry best practice for critical data

### 9. DB Migration Path
**Decision:** Design data schema to be DB-ready from day one, even though currently using JSON.

**Migration Strategy:**
1. Stand up database tables matching current schema (resources, collection, decks, deck_cards)
2. Implement `DbProfileRepo` with same `IProfileRepo` interface
3. Add `SyncService` to sync JSON ↔ DB using WAL
4. Flip config flag: `StorageBackend = "json" | "db"`
5. Zero changes to game logic or UI code

**Schema Readiness:**
- UUID-based IDs (stable across migrations)
- Row-oriented data (maps directly to SQL tables)
- Timestamps for audit trail
- Normalized design (junction tables for many-to-many)
- WAL captures operations for conflict resolution

**Future Backends:**
- PostgreSQL (via Supabase, PocketBase, or direct connection)
- SQLite (local embedded DB)
- Cloud sync (Firebase, AWS, custom server)

## Current Limitations

### Not Yet Implemented
- **Sound/Music** (Future)
- **Card Variants/Progression** (Future)
- **AI Opponent Logic** (Current: Random cards only)

### Technical Debt
- No sound effects
- AI opponent uses random card selection

### Known Issues
- See `docs/bugs.md` for current issues

## Development Phases

See [todos.md](../tracking/todos.md) for current development priorities.

### Completed
- ✅ Save system with repository pattern
- ✅ Card catalog and collection system
- ✅ Deck building (inline in Collection screen)
- ✅ Campaign progression system
- ✅ Summoner screen with carousel
- ✅ VFX system (fireball, lightning)
- ✅ Premium Store UI

## Future Considerations

### Features Under Consideration
- **Checksum Validation:** SHA1 hash for corruption detection (production-grade)
- **RNG Seed Tracking:** Deterministic matches for debugging
- **Multi-Profile Support:** Multiple save slots
- **Cloud Save Sync:** Cross-device progression
- **Save Compression:** For larger collections
- **Undo/Redo System:** Accidental operation recovery
- **Card Variants:** Stat rolls and effect modifiers
- **Summoner System:** Persistent commander with abilities
- **Progression System:** Level up cards and summoners
- **Achievement System:** Unlock conditions and rewards

### Performance Considerations
- **Unit Count:** Currently no spawn limit (can cause performance issues)
- **Particle Systems:** Not yet implemented (will need pooling)
- **Audio:** Not yet implemented (will need resource management)

## Testing Strategy

### Current Testing
- Manual playtesting in Godot editor
- Console logging for debugging
- Dev console commands for edge cases

### Planned Testing
- Automated tests for save/load integrity
- Corruption recovery tests
- Performance profiling for large unit counts
- Memory leak detection

## References

### External Resources
- [GDQuest - Godot Save System](https://www.gdquest.com)
- [Godot 4 RPG Tutorial - Persistent Saving](https://docs.godotengine.org)
- [Mini Warriors Reborn](https://play.google.com) - Design inspiration

### Internal Documentation
- `PROJECT_DOC.md` - Original design document
- `.claude/CLAUDE.md` - Development workflow guidelines
