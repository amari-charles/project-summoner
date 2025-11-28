# Hero System Architecture

## Overview

Heroes are deck leaders that provide passive bonuses and define core battle parameters. They do not fight directly but influence the player's capabilities through their stats (base health, mana, mana regen) and traits.

**This document covers the implemented hero system.** For the full progression design (Level Traits, Ultimate Traits, etc.), see [Hero Progression System](progression-system.md).

### Key Principles
- **Non-combat entities**: Heroes don't appear on the battlefield
- **Passive bonuses**: Affect base health, mana system, and unit performance via traits
- **Trait-based modifiers**: All hero bonuses come from TraitCatalog
- **Per-hero campaign progress**: Each hero has separate campaign state
- **Active hero selection**: Profile tracks which hero is currently active

---

## System Architecture

### Service Layer

```
┌─────────────────────────────────────────────────────────────┐
│                    UI Layer                                  │
│  - HeroManagementPanel (hero roster, level-up, traits)      │
│  - HeroIconWidget (persistent hero button on screens)        │
│  - HeroRosterItem (individual hero display in roster)        │
└─────────────────────┬───────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────┐
│                 Service Layer (Autoloads)                    │
│                                                              │
│  HeroSelection          HeroProgression       TraitCatalog   │
│  - get_active_hero_id   - grant_xp           - get_trait     │
│  - switch_hero          - level_up_hero      - get_modifiers │
│  - get_unlocked_ids     - can_level_up       - has_trait     │
└─────────────────────┬───────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────┐
│                    Data Layer                                │
│                                                              │
│  HeroCatalog            ProfileRepo           HeroInstance   │
│  - Hero configs         - hero_instances[]    - level, xp    │
│  - innate_trait_ids     - campaign_progress   - boon_ids     │
│  - base stats           - meta.selected_hero  - computed stats│
└─────────────────────────────────────────────────────────────┘
```

### Key Services

#### HeroSelection (autoload: `/root/HeroSelection`)
Manages which hero is currently active.

```gdscript
# Get active hero
var hero_id: String = HeroSelection.get_active_hero_id()
var config: HeroConfig = HeroSelection.get_active_hero_config()

# Switch heroes
HeroSelection.switch_hero("hero_water")

# List unlocked heroes
var ids: Array[String] = HeroSelection.get_unlocked_hero_ids()
```

**Signals:**
- `hero_changed(old_hero_id, new_hero_id)` - Emitted when active hero changes

#### HeroProgression (autoload: `/root/HeroProgression`)
Manages XP and level-up mechanics.

```gdscript
# Grant XP
HeroProgression.grant_hero_xp("hero_fire", 50)
HeroProgression.grant_active_hero_xp(100)

# Level up
if HeroProgression.can_level_up("hero_fire"):
    HeroProgression.level_up_hero("hero_fire")

# Query progression
var info: Dictionary = HeroProgression.get_hero_progression_info("hero_fire")
# Returns: {level, xp, xp_for_next_level, xp_progress, can_level_up, ...}
```

**Signals:**
- `hero_xp_changed(hero_id, new_xp, new_level)`
- `hero_leveled_up(hero_id, new_level)`

#### TraitCatalog (autoload: `/root/TraitCatalog`)
Central registry for all trait definitions.

```gdscript
# Get trait data
var trait: Dictionary = TraitCatalog.get_trait("trait_fire_affinity")

# Get unit modifiers for a trait (used by ModifierSystem)
var mods: Array = TraitCatalog.get_unit_modifiers_for_trait("trait_fire_affinity")

# Query traits
var all_ids: Array[String] = TraitCatalog.get_all_trait_ids()
var innate: Array[Dictionary] = TraitCatalog.get_innate_traits()
```

---

## Data Structures

### HeroConfig (Resource)
Static hero configuration from HeroCatalog.

```gdscript
class_name HeroConfig extends Resource

var hero_id: String              # "hero_fire"
var hero_name: String            # "Pyralis"
var description: String          # Flavor text
var element_id: int              # ElementRegistry.ElementId.FIRE

# Base Stats (before traits)
var base_health: float           # 1000.0
var max_mana: float              # 10.0
var mana_regen: float            # 1.0

# Traits from TraitCatalog
var innate_trait_ids: Array[String]  # ["trait_fire_affinity", "trait_burning_spirit"]

# Visual
var hero_icon_path: String
var card_frame_style: String     # "legendary"

# Unlock
var unlock_condition: String     # "starting_choice", "random_starter_only"
```

### HeroInstance (RefCounted)
Runtime hero state with progression.

```gdscript
class_name HeroInstance extends RefCounted

var config: HeroConfig           # Reference to static config
var level: int = 1               # Current level (1-10)
var xp: int = 0                  # Current XP

# Acquired Boons (from gameplay, stored as trait IDs)
var acquired_boon_ids: Array[String] = []

# Get all trait IDs (innate + acquired)
func get_all_trait_ids() -> Array[String]

# Get computed stats (base + all trait modifiers)
func get_computed_stats() -> Dictionary
# Returns: {health, max_mana, mana_regen, fire_damage_bonus, damage_reduction, ...}
```

### Trait Data (Dictionary)
Stored in TraitCatalog.

```gdscript
{
    "id": "trait_fire_affinity",
    "name_key": "trait.fire_affinity.name",
    "description_key": "trait.fire_affinity.description",
    "category": "elemental",
    "is_innate": true,
    "modifiers": [
        # Hero stat modifier
        {"stat": "fire_damage_bonus", "type": "percent", "value": 10.0},
        # Unit modifier (passed to ModifierSystem)
        {
            "target": "unit",
            "source": "trait_fire_affinity",
            "conditions": {"elemental_affinity": "fire"},
            "stat_mults": {"attack_damage": 1.10}
        }
    ]
}
```

---

## Trait System

### Trait Categories
- **Innate Traits**: Come with the hero (defined in HeroConfig.innate_trait_ids)
- **Acquired Boons**: Earned through gameplay (stored in HeroInstance.acquired_boon_ids)

### Modifier Types

**Hero Stat Modifiers** (applied in HeroInstance._recompute_stats):
```gdscript
{"stat": "max_health", "type": "percent", "value": 10.0}  # +10% health
{"stat": "mana_regen", "type": "flat", "value": 0.3}      # +0.3 regen/sec
{"stat": "fire_damage_bonus", "type": "percent", "value": 10.0}  # Sets 10% bonus
```

**Unit Modifiers** (passed to ModifierSystem via HeroModifierProvider):
```gdscript
{
    "target": "unit",
    "source": "trait_fire_affinity",
    "conditions": {"elemental_affinity": "fire"},
    "stat_mults": {"attack_damage": 1.10}
}
```

### Modifier Flow

```
HeroInstance.get_all_trait_ids()
        │
        ▼
TraitCatalog.get_trait(trait_id)
        │
        ├──► Hero modifiers → HeroInstance._apply_trait_modifiers()
        │                            │
        │                            ▼
        │                     Computed hero stats
        │                     (health, mana, damage_bonus, etc.)
        │
        └──► Unit modifiers → HeroModifierProvider.get_modifiers()
                                     │
                                     ▼
                              ModifierSystem
                                     │
                                     ▼
                              Applied to spawned units
```

---

## Battle Integration

### Hero Stats in Battle

1. **Summoner loads hero** via DeckLoader
2. **Summoner._apply_hero_bonuses()** computes final stats
3. **BattleContext.set_player_hero_stats()** caches stats for DamageSystem
4. **DamageSystem** reads cached stats for damage bonuses/reduction

```gdscript
# In Summoner._apply_hero_bonuses():
var computed_stats: Dictionary = hero_instance.get_computed_stats()
BattleContext.set_player_hero_stats(computed_stats)

# In DamageSystem._apply_hero_damage_bonuses():
var hero_stats: Dictionary = BattleContext.get_player_hero_stats()
var damage_bonus: float = hero_stats.get("damage_bonus", 0.0)
```

### Unit Modifiers in Battle

1. **GameController** registers HeroModifierProvider with ModifierSystem
2. **When units spawn**, ModifierSystem queries all providers
3. **HeroModifierProvider** returns unit modifiers from hero's traits
4. **Modifiers are applied** to matching units (by element, tags, etc.)

```gdscript
# In GameController._setup_modifier_system():
var hero_instance: HeroInstance = _load_hero_instance()
var provider: HeroModifierProvider = HeroModifierProvider.new(hero_instance)
ModifierSystem.register_provider("hero", provider)
```

---

## Per-Hero Campaign Progress

Campaign progress is stored per-hero in ProfileRepo:

```gdscript
"campaign_progress": {
    "hero_fire": {
        "completed_battles": ["battle_tutorial", "battle_first_slime"],
        "current_battle": null,
        "pending_reward": null
    },
    "hero_water": {
        "completed_battles": ["battle_tutorial"],
        "current_battle": "battle_first_slime",
        "pending_reward": null
    }
}
```

### Accessing Progress

```gdscript
# Get progress for active hero
var progress: Dictionary = ProfileRepo.get_campaign_progress()

# Get progress for specific hero
var progress: Dictionary = ProfileRepo.get_campaign_progress("hero_fire")

# Update progress (uses active hero by default)
ProfileRepo.update_campaign_progress({"current_battle": "battle_02"})
```

---

## UI Components

### HeroManagementPanel
Full-screen modal for hero management:
- Hero roster with all unlocked heroes
- Active hero details (stats, traits, XP)
- Level-up button (spends gold + requires XP threshold)
- Hero switching

### HeroIconWidget
Persistent hero portrait button:
- Shows active hero's element color and level
- Click to open HeroManagementPanel
- Auto-updates when hero changes

### HeroRosterItem
Individual hero row in roster:
- Portrait, name, level, stats
- XP progress bar
- Select/Level-up buttons

---

## Starting Hero Assignment

### Available Starting Choices
- **Fire** (Pyralis) - trait_fire_affinity, trait_burning_spirit
- **Water** (Aquira) - trait_water_affinity, trait_tidal_resilience
- **Wind** (Zephyrion) - trait_wind_affinity, trait_swift_casting
- **Earth** (Terravorn) - trait_earth_affinity, trait_stone_fortitude
- **Random** - Picks from above + grants "Fortune Favors the Bold" boon

### Fortune Favors the Bold
Special boon granted only to heroes created via Random selection:
- Stored in `HeroInstance.acquired_boon_ids`
- Provides +50 max health
- Trait ID: `trait_fortune_favors_bold`

---

## Implementation Files

### Services
- `scripts/services/hero_selection_service.gd` - HeroSelection autoload
- `scripts/services/hero_progression_service.gd` - HeroProgression autoload
- `scripts/data/trait_catalog.gd` - TraitCatalog autoload

### Data
- `scripts/core/hero_config.gd` - HeroConfig resource class
- `scripts/core/hero_instance.gd` - HeroInstance runtime class
- `scripts/data/hero_catalog.gd` - HeroCatalog autoload (static configs)

### Modifier Integration
- `scripts/systems/hero_modifier_provider.gd` - Provides unit modifiers from traits
- `scripts/systems/modifier_system.gd` - Central modifier registry

### UI
- `scripts/ui/hero_management_panel.gd` - Main hero panel
- `scripts/ui/hero_roster_item.gd` - Individual hero in roster
- `scripts/ui/hero_icon_widget.gd` - Persistent hero button

### Scenes
- `scenes/ui/hero_management_panel.tscn`
- `scenes/ui/hero_roster_item.tscn`
- `scenes/ui/hero_icon_widget.tscn`

---

## Localization

All hero UI uses the localization system:

```json
{
  "ui": {
    "hero_panel": {
      "title": "HERO ROSTER",
      "level_display": "Lv.{level}",
      "xp_progress": "XP: {current} / {required}",
      "level_up_button": "LEVEL UP ({cost}g)",
      "stats_summary": "HP: {hp} | Mana: {mana} | Regen: {regen}/s"
    }
  },
  "trait": {
    "fire_affinity": {
      "name": "Fire Affinity",
      "description": "+10% fire damage. Fire units deal 10% more damage."
    }
  }
}
```
