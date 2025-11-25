# Implementation Plan: Hero System (MVP)

## Overview

Heroes are deck leaders that provide passive bonuses and define core battle parameters. They do not fight directly but influence the player's capabilities through their stats (base health, mana, mana regen).

**This document covers the MVP/base hero system.** For the full progression system (Traits, Boons, Global Event Cards), see [Hero Progression System](progression-system.md).

### Key Principles
- **Non-combat entities**: Heroes don't appear on the battlefield
- **Passive bonuses**: Affect base health, mana system, and unit performance
- **Ownership vs Selection**:
  - **Profile** stores which heroes the player has unlocked (ownership)
  - **Deck** stores which one of those heroes they are currently using (selection)
  - Each deck references exactly one unlocked hero as its leader
- **Elemental identity**: Each hero is associated with a core element
- **Starting hero selection**: Players choose their first hero during onboarding (see below)

---

## Starting Hero Assignment

When a new profile is created, the player must select their first hero.

### Available Starting Choices

They may choose one of the **four core elemental heroes**:
- **Fire** (Pyralis)
- **Water** (Aquira)
- **Wind** (Zephyrion)
- **Earth** (Terravorn)

Or choose the **Random** option.

### Random Option Behavior

Choosing **Random** selects from a larger pool:
- The four core heroes
- Several additional rare starter-only heroes (≈ 4 extra)

**This guarantees that "Random" may grant a hero otherwise unavailable as a starting choice.**

Examples of starter-only heroes (future):
- **Shadow Initiate** (Shadow element)
- **Lightning Adept** (Lightning element)
- **Verdant Sage** (Life element)
- **Void Walker** (Death element)

### Fortune Favors the Bold

If the player selects **Random**, their starting hero gains the special Story Trait:

**Fortune Favors the Bold**
— A unique Story Trait granted only to heroes created via Random selection.

**Important:** This trait is added to the **starting hero's traits**, not stored as a profile field.

**MVP Note:** While the full trait system is post-MVP, Fortune Favors the Bold is the one trait granted at character creation. In MVP, the hero will have a simple `traits: ["fortune_favors_the_bold"]` array, even though the trait system mechanics are not yet implemented. The trait's effects will be applied in battle similar to how hero stats are applied.

**In MVP, this trait is treated as a simple flag on the hero; its effect is hard-coded in BattleContext (no generic trait engine yet).** No other traits are exposed or selectable in MVP.

The trait provides a permanent benefit (exact effect TBD, examples: +5% gold earnings, +1 card reward from battles, etc.)

### Relationship to Decks

After onboarding:
1. The player's profile contains a list of `unlocked_heroes` (starting with their chosen hero)
2. Decks select one of the unlocked heroes as their leader
3. Battles load the hero from the deck and apply bonuses

**Key Point:** The starting hero is added to `unlocked_heroes`, not set as a "current hero" in the profile.

---

## System Design

### Data Ownership Model

**Profile-Level: Ownership**
- Stores the list of unlocked heroes available to the player
- Answers: "Which heroes do I have?"
- Persists across all decks and sessions

**Deck-Level: Selection**
- Stores `hero_id`, which must be one of the player's unlocked heroes
- Answers: "Which hero does this deck use?"
- Each deck can use a different hero

**Battle-Level: Application**
- Loads the deck's chosen hero and applies its bonuses
- Answers: "What are my stats for this battle?"
- Temporary state, not persisted

**Key Invariant:** `deck.hero_id` must exist in `profile.unlocked_heroes`

---

### What is a Hero?

A hero is a special card-like entity with the following characteristics:

**Core Attributes:**
- `hero_id` - Unique identifier (e.g., "hero_fire", "hero_water")
- `hero_name` - Display name (e.g., "Pyralis the Eternal Flame")
- `element` - Associated elemental affinity (Fire, Water, Wind, Earth)
- `description` - Flavor text describing the hero

**Battle Parameters:**
- `base_health` - Starting health for the player in battle
- `max_mana` - Maximum mana capacity
- `mana_regen` - Mana regeneration rate per second

**Progression (MVP):**
- `traits` - Array of trait IDs (e.g., `["fortune_favors_the_bold"]`)
  - Used in MVP only for Fortune Favors the Bold
  - Full trait system is post-MVP, but this field is needed now
  - Most heroes have empty array `[]` in MVP

**Visual:**
- `hero_icon_path` - Path to hero portrait/icon
- `card_frame_style` - Special visual treatment (gold border, effects, etc.)

---

## Architecture

### Layer Separation

```
┌─────────────────────────────────────────┐
│      UI Layer (Deck Builder)            │
│  - Hero selection dropdown/grid         │
│  - Display hero stats                   │
│  - Visual preview of bonuses            │
└─────────────────┬───────────────────────┘
                  │ Reads heroes, updates deck
                  ▼
┌─────────────────────────────────────────┐
│     Service Layer (HeroCatalog)         │
│  - Hero definitions (_init_catalog)     │
│  - Lookup methods (get_hero, list)      │
│  - Hero validation                      │
└─────────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────┐
│   Data Layer (ProfileRepo & Decks)      │
│  - Deck data: hero_id field             │
│  - Profile: unlocked_heroes Array       │
└─────────────────────────────────────────┘
```

### Battle Integration

```
┌─────────────────────────────────────────┐
│      BattleContext / Battlefield        │
│  1. Load player deck via DeckLoader     │
│  2. Get hero_id from deck data          │
│  3. Fetch hero from HeroCatalog         │
│  4. Apply hero bonuses to battle state  │
│     - Set player base_health            │
│     - Set max_mana / mana_regen         │
└─────────────────────────────────────────┘
```

---

## Data Structures

### Hero Resource/Data

```gdscript
class Hero:
    var hero_id: String              # "hero_fire"
    var hero_name: String            # "Pyralis the Eternal Flame"
    var description: String          # Flavor text
    var element: ElementTypes.Element  # Fire, Water, Wind, Earth

    # Battle parameters
    var base_health: float           # e.g., 1000.0
    var max_mana: float              # e.g., 10.0
    var mana_regen: float            # e.g., 1.0 per second

    # Visual
    var hero_icon_path: String       # "res://assets/heroes/pyralis.png"
    var card_frame_style: String     # "legendary", "mythic", etc.

    # Metadata
    var unlock_condition: String     # "default", "complete_fire_campaign", etc.
```

### HeroCatalog Service

Similar to `CardCatalog`, this autoload service manages all hero definitions:

```gdscript
extends Node
# Registered as autoload "HeroCatalog"

var _catalog: Dictionary = {}  # hero_id -> hero_data

func _ready() -> void:
    _init_catalog()
    print("HeroCatalog: Loaded %d heroes" % _catalog.size())

func _init_catalog() -> void:
    # Fire Hero - Pyralis
    _catalog["hero_fire"] = {
        "hero_id": "hero_fire",
        "hero_name": "Pyralis",
        "description": "Master of flame and passion",
        "element": ElementTypes.FIRE,
        "base_health": 1000.0,
        "max_mana": 10.0,
        "mana_regen": 1.0,
        "traits": [],  # Empty in catalog; populated when hero is created
        "hero_icon_path": "",
        "card_frame_style": "legendary",
        "unlock_condition": "starting_choice"
    }

    # Water Hero - Aquira
    _catalog["hero_water"] = {
        "hero_id": "hero_water",
        "hero_name": "Aquira",
        "description": "Embodiment of adaptability and flow",
        "element": ElementTypes.WATER,
        "base_health": 1200.0,  # Higher health, lower regen
        "max_mana": 10.0,
        "mana_regen": 0.8,
        "traits": [],  # Empty in catalog; populated when hero is created
        "hero_icon_path": "",
        "card_frame_style": "legendary",
        "unlock_condition": "starting_choice"
    }

    # Wind Hero - Zephyrion
    _catalog["hero_wind"] = {
        "hero_id": "hero_wind",
        "hero_name": "Zephyrion",
        "description": "Spirit of freedom and motion",
        "element": ElementTypes.WIND,
        "base_health": 900.0,  # Lower health
        "max_mana": 12.0,      # Higher mana pool
        "mana_regen": 1.2,     # Faster regen
        "traits": [],  # Empty in catalog; populated when hero is created
        "hero_icon_path": "",
        "card_frame_style": "legendary",
        "unlock_condition": "starting_choice"
    }

    # Earth Hero - Terravorn
    _catalog["hero_earth"] = {
        "hero_id": "hero_earth",
        "hero_name": "Terravorn",
        "description": "Guardian of stability and endurance",
        "element": ElementTypes.EARTH,
        "base_health": 1500.0,  # Highest health
        "max_mana": 8.0,        # Lower mana
        "mana_regen": 0.7,      # Slower regen
        "traits": [],  # Empty in catalog; populated when hero is created
        "hero_icon_path": "",
        "card_frame_style": "legendary",
        "unlock_condition": "starting_choice"
    }

    # =========================================================================
    # STARTER-ONLY HEROES (Future / Optional for MVP)
    # These heroes are only available through the "Random" starting option.
    # They do NOT need to be implemented for the first MVP pass.
    # MVP can ship with only the 4 core heroes; Random just picks among those.
    # =========================================================================

    # Shadow Initiate (starter-only)
    _catalog["hero_shadow_initiate"] = {
        "hero_id": "hero_shadow_initiate",
        "hero_name": "Shadow Initiate",
        "description": "A mysterious figure cloaked in darkness",
        "element": ElementTypes.SHADOW,
        "base_health": 950.0,
        "max_mana": 11.0,
        "mana_regen": 1.1,
        "traits": [],  # Empty in catalog; populated when hero is created
        "hero_icon_path": "",
        "card_frame_style": "rare",
        "unlock_condition": "random_starter_only"
    }

    # TODO: Add 3-4 more starter-only heroes for other outer elements
    # - Lightning Adept
    # - Verdant Sage (Life)
    # - Void Walker (Death)

func get_hero(hero_id: String) -> Dictionary:
    return _catalog.get(hero_id, {})

func get_starting_choice_heroes() -> Array[Dictionary]:
    """Get heroes available as direct starting choices (4 core heroes)"""
    var result: Array[Dictionary] = []
    for hero_data: Dictionary in _catalog.values():
        if hero_data.get("unlock_condition") == "starting_choice":
            result.append(hero_data)
    return result

func get_random_pool_heroes() -> Array[Dictionary]:
    """Get heroes available in the Random starting pool (core + starter-only)"""
    var result: Array[Dictionary] = []
    for hero_data: Dictionary in _catalog.values():
        var condition: String = hero_data.get("unlock_condition", "")
        if condition == "starting_choice" or condition == "random_starter_only":
            result.append(hero_data)
    return result

func get_all_heroes() -> Array[Dictionary]:
    return _catalog.values()

func get_heroes_by_element(element: ElementTypes.Element) -> Array[Dictionary]:
    var result: Array[Dictionary] = []
    for hero_data: Dictionary in _catalog.values():
        if hero_data.get("element") == element:
            result.append(hero_data)
    return result

func is_valid_hero(hero_id: String) -> bool:
    return _catalog.has(hero_id)
```

---

## ProfileRepository Changes

### Schema Updates

**IMPORTANT:** Profile creation now happens in two stages:

1. **Initial profile creation** (`_create_fresh_profile()`):
   - Profile is created with empty `unlocked_heroes`
   - Player is directed to hero selection screen

2. **After hero selection** (called by onboarding flow):
   - Add selected hero to `unlocked_heroes`
   - If Random was chosen, the hero will have the "fortune_favors_the_bold" trait in its traits array

```gdscript
# In _create_fresh_profile():
"unlocked_heroes": [],  # Empty - populated after onboarding hero selection

# After onboarding, profile will have:
"unlocked_heroes": [starting_hero_id],  # Exactly one hero (e.g., "hero_fire" or "hero_shadow_initiate")

# Note: If Random was chosen, the hero itself will have:
# hero_data = {
#   "hero_id": "hero_fire",  # or whatever was randomly selected
#   "traits": ["fortune_favors_the_bold"],  # Special trait for random selection
#   ...
# }
```

### New Methods

```gdscript
## Get list of unlocked hero IDs
func get_unlocked_heroes() -> Array:
    return _data.get("unlocked_heroes", [])

## Check if a specific hero is unlocked
func is_hero_unlocked(hero_id: String) -> bool:
    var unlocked: Array = _data.get("unlocked_heroes", [])
    return hero_id in unlocked

## Unlock a new hero
func unlock_hero(hero_id: String) -> bool:
    var unlocked: Array = _data.get("unlocked_heroes", [])
    if hero_id not in unlocked:
        unlocked.append(hero_id)
        _data["unlocked_heroes"] = unlocked
        _append_to_wal({"op": "unlock_hero", "hero_id": hero_id})
        return save_profile(true)
    return false

## Set starting hero (called during onboarding)
## chosen_random: whether player selected "Random" option (passed to WAL for tracking)
func set_starting_hero(hero_id: String, chosen_random: bool) -> bool:
    # Validate this is called on a fresh profile
    var unlocked: Array = _data.get("unlocked_heroes", [])
    if not unlocked.is_empty():
        push_error("ProfileRepo: Cannot set starting hero - heroes already unlocked")
        return false

    # Add hero
    unlocked.append(hero_id)
    _data["unlocked_heroes"] = unlocked

    # Note: If chosen_random is true, the hero itself should have
    # "fortune_favors_the_bold" in its traits array. This is handled
    # by the hero creation/initialization logic, not by ProfileRepo.

    _append_to_wal({
        "op": "set_starting_hero",
        "hero_id": hero_id,
        "chosen_random": chosen_random
    })
    return save_profile(true)
```

**Note:** There is no `has_fortune_favors_bold()` method because Fortune Favors the Bold is a hero trait, not a profile field. To check if a hero has this trait, query the hero's traits array (post-MVP functionality).

---

## Deck Service Changes

### Schema Updates

Add `hero_id` field to deck data:

```gdscript
func create_deck(deck_name: String, hero_id: String = "") -> Dictionary:
    # Default to first unlocked hero if not specified
    var final_hero_id := hero_id
    if final_hero_id.is_empty():
        var unlocked := ProfileRepo.get_unlocked_heroes()
        if unlocked.is_empty():
            push_error("Cannot create deck: no heroes unlocked")
            return {}
        final_hero_id = unlocked[0]

    # Validate hero is unlocked
    if not ProfileRepo.is_hero_unlocked(final_hero_id):
        push_error("Cannot create deck with locked hero: %s" % final_hero_id)
        return {}

    var deck_id: String = _generate_deck_id()
    var deck: Dictionary = {
        "id": deck_id,
        "name": deck_name,
        "hero_id": final_hero_id,  # Store hero with deck
        "card_instance_ids": [],
        "created_at": Time.get_datetime_string_from_system(),
        "modified_at": Time.get_datetime_string_from_system()
    }
    return _repo.upsert_deck(deck)
```

### New Methods

```gdscript
## Update the hero for a specific deck
func set_deck_hero(deck_id: String, hero_id: String) -> bool:
    var deck: Dictionary = get_deck(deck_id)
    if deck.is_empty():
        push_error("Deck not found: %s" % deck_id)
        return false

    # Validate hero exists
    if not HeroCatalog.is_valid_hero(hero_id):
        push_error("Invalid hero_id: %s" % hero_id)
        return false

    # Validate hero is unlocked (enforce key invariant: deck.hero_id must exist in profile.unlocked_heroes)
    if not ProfileRepo.is_hero_unlocked(hero_id):
        push_error("Hero not unlocked for this profile: %s" % hero_id)
        return false

    deck["hero_id"] = hero_id
    deck["modified_at"] = Time.get_datetime_string_from_system()
    _repo.upsert_deck(deck)
    deck_updated.emit(deck_id)
    return true

## Get hero ID for a deck
func get_deck_hero(deck_id: String) -> String:
    var deck: Dictionary = get_deck(deck_id)
    return deck.get("hero_id", "hero_fire")  # Default to fire hero
```

---

## DeckLoader Changes

Update `load_deck_for_battle()` to include hero data:

```gdscript
static func load_deck_for_battle(deck_id: String) -> Dictionary:
    var result: Dictionary = {
        "cards": [],
        "hero_id": "hero_fire",  # Default fallback
        "hero_data": {}
    }

    # ... existing card loading logic ...

    # Load hero
    var hero_id_variant: Variant = deck.get("hero_id", "hero_fire")
    var hero_id: String = hero_id_variant if hero_id_variant is String else "hero_fire"

    result["hero_id"] = hero_id
    result["hero_data"] = HeroCatalog.get_hero(hero_id)

    if result["hero_data"].is_empty():
        push_warning("DeckLoader: Hero not found '%s', using default" % hero_id)
        result["hero_id"] = "hero_fire"
        result["hero_data"] = HeroCatalog.get_hero("hero_fire")

    return result
```

---

## Battle Integration

### BattleContext Changes

Apply hero bonuses when initializing battle:

```gdscript
func _ready() -> void:
    # ... existing setup ...

    # Load deck with hero
    var deck_data: Dictionary = DeckLoader.load_player_deck()
    var cards: Array[Card] = deck_data.get("cards", [])
    var hero_data: Dictionary = deck_data.get("hero_data", {})

    # Apply hero bonuses
    if not hero_data.is_empty():
        _apply_hero_bonuses(hero_data)

    # ... continue with battle setup ...

func _apply_hero_bonuses(hero_data: Dictionary) -> void:
    # Set base health
    var base_health: float = hero_data.get("base_health", 1000.0)
    player_health = base_health
    max_player_health = base_health

    # Set mana parameters
    var max_mana: float = hero_data.get("max_mana", 10.0)
    var mana_regen: float = hero_data.get("mana_regen", 1.0)

    # Apply to mana system (assuming there's a mana manager)
    if has_node("ManaManager"):
        var mana_mgr: Node = get_node("ManaManager")
        if mana_mgr.has_method("set_max_mana"):
            mana_mgr.call("set_max_mana", max_mana)
        if mana_mgr.has_method("set_mana_regen"):
            mana_mgr.call("set_mana_regen", mana_regen)

    print("BattleContext: Applied hero bonuses - Health: %.0f, Mana: %.0f, Regen: %.1f" % [base_health, max_mana, mana_regen])
```

---

## UI Integration

### Deck Builder Updates

Add hero selection UI to the deck builder screen:

**Visual Design:**
- Hero portrait/card at the top of the deck builder
- Dropdown or grid selection for choosing hero
- Display hero stats (health, mana, regen) prominently
- Filter/highlight cards matching hero's element (future)

**Implementation:**

```gdscript
# In deck_builder.gd

@onready var hero_display: Panel = %HeroDisplay
@onready var hero_selector: OptionButton = %HeroSelector

var current_hero_id: String = "hero_fire"

func _ready() -> void:
    # ... existing setup ...
    _populate_hero_selector()
    _load_deck_hero()

func _populate_hero_selector() -> void:
    hero_selector.clear()
    var unlocked_heroes: Array = ProfileRepo.get_unlocked_heroes()

    for hero_id: String in unlocked_heroes:
        var hero_data: Dictionary = HeroCatalog.get_hero(hero_id)
        if not hero_data.is_empty():
            var hero_name: String = hero_data.get("hero_name", hero_id)
            hero_selector.add_item(hero_name)
            hero_selector.set_item_metadata(hero_selector.get_item_count() - 1, hero_id)

    hero_selector.item_selected.connect(_on_hero_selected)

func _load_deck_hero() -> void:
    if current_deck_id.is_empty():
        return

    current_hero_id = Decks.get_deck_hero(current_deck_id)
    _update_hero_display()

func _on_hero_selected(index: int) -> void:
    var hero_id: String = hero_selector.get_item_metadata(index)
    if hero_id != current_hero_id:
        current_hero_id = hero_id
        Decks.set_deck_hero(current_deck_id, hero_id)
        _update_hero_display()

func _update_hero_display() -> void:
    var hero_data: Dictionary = HeroCatalog.get_hero(current_hero_id)
    if hero_data.is_empty():
        return

    # Update display with hero info
    # TODO: Set hero portrait, name, stats
    pass
```

---

## Localization

All hero names and descriptions must use the localization system:

### HeroCatalog Updates

```gdscript
_catalog["hero_fire"] = {
    "hero_id": "hero_fire",
    "hero_name": Loc.t("hero.hero_fire.name"),
    "description": Loc.t("hero.hero_fire.description"),
    # ... rest of data
}
```

### Localization Entries (localization/data/en.json)

```json
{
  "hero": {
    "hero_fire": {
      "name": "Pyralis",
      "description": "Master of flame and passion. Pyralis embodies transformation and vitality."
    },
    "hero_water": {
      "name": "Aquira",
      "description": "Embodiment of adaptability and flow. Aquira bends but never breaks."
    },
    "hero_wind": {
      "name": "Zephyrion",
      "description": "Spirit of freedom and motion. Zephyrion moves with boundless energy."
    },
    "hero_earth": {
      "name": "Terravorn",
      "description": "Guardian of stability and endurance. Terravorn stands unmoved."
    }
  }
}
```

---

## Implementation Order

### Phase 1: Core System (MVP)
1. Create `HeroCatalog` service with 4 core element heroes
2. Create `Hero` class/resource for type safety
3. Add `hero_id` field to deck data schema
4. Update ProfileRepo with `unlocked_heroes` field
5. Add localization entries for heroes

### Phase 2: Data Integration
6. Update `DeckService` to handle hero selection
7. Update `DeckLoader` to load hero data with deck
8. Modify `BattleContext` to apply hero bonuses

### Phase 3: UI
9. Add hero display to deck builder
10. Add hero selector dropdown/grid
11. Display hero stats and bonuses
12. Visual polish (hero portraits, card frames)

---

## Testing Checklist

- [ ] HeroCatalog loads 4 heroes correctly
- [ ] All heroes have valid element associations
- [ ] Deck creation includes default hero
- [ ] Hero selection persists when saving deck
- [ ] Battle initializes with correct hero bonuses
- [ ] Base health matches hero definition
- [ ] Mana system uses hero's max_mana and mana_regen
- [ ] Switching heroes updates deck properly
- [ ] Unlocked heroes filter works
- [ ] Localization strings display correctly
- [ ] Hero selection UI is responsive
- [ ] Invalid hero_id falls back to default gracefully

---

## Technical Notes

### Why Not Make Hero a Card?
- Heroes don't appear in hand or battlefield
- Different data structure (no unit_scene_path, spawn_count, etc.)
- Avoid cluttering Card class with hero-specific logic
- Cleaner separation of concerns

### Why Dictionary Instead of Resource?
- Follows existing pattern (CardCatalog uses Dictionaries)
- Easier to serialize for save/load
- Can convert to Resource later if needed for editor integration
- Simpler for initial implementation

### Default Hero Strategy
- Always fall back to "hero_fire" if hero_id is invalid
- Ensures battles never fail due to missing hero
- Graceful degradation for corrupted save data
