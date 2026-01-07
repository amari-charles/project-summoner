# Summoner System Architecture

## Overview

Summoners are deck leaders that provide passive bonuses and define core battle parameters. They do not fight directly but influence the player's capabilities through their stats (Incarnation health, max mana) and traits.

**This document covers the implemented summoner system.** For the full progression design (Level Traits, Ultimate Traits, etc.), see [Summoner Progression System](progression-system.md).

### Key Principles
- **Non-combat entities**: Summoners don't appear on the battlefield (their Incarnation does)
- **Passive bonuses**: Affect Incarnation health, mana pool, and unit performance via traits
- **Trait-based modifiers**: All summoner bonuses come from TraitCatalog
- **Per-summoner campaign progress**: Each summoner has separate campaign state
- **Active summoner selection**: Profile tracks which summoner is currently active

---

## System Architecture

### Service Layer

```
┌─────────────────────────────────────────────────────────────┐
│                    UI Layer                                  │
│  - SummonerScreen (full-screen summoner roster, level-up, traits)│
│  - SummonerIconWidget (persistent summoner button on screens) │
│  - SummonerRosterItem (individual summoner display in roster) │
└─────────────────────┬───────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────┐
│                 Service Layer (Autoloads)                    │
│                                                              │
│  SummonerSelection      SummonerProgression   TraitCatalog   │
│  - get_active_summoner_id - grant_xp         - get_trait     │
│  - switch_summoner      - level_up_summoner  - get_modifiers │
│  - get_unlocked_ids     - can_level_up       - has_trait     │
└─────────────────────┬───────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────┐
│                    Data Layer                                │
│                                                              │
│  SummonerCatalog        ProfileRepo           SummonerInstance│
│  - Summoner configs     - summoner_instances[]- level, xp    │
│  - innate_trait_ids     - campaign_progress   - boon_ids     │
│  - base stats           - meta.selected_summoner- computed stats│
└─────────────────────────────────────────────────────────────┘
```

### Key Services

#### SummonerSelection (autoload: `/root/SummonerSelection`)
Manages which summoner is currently active.

```gdscript
# Get active summoner
var summoner_id: String = SummonerSelection.get_active_summoner_id()
var config: SummonerConfig = SummonerSelection.get_active_summoner_config()

# Switch summoners
SummonerSelection.switch_summoner("summoner_water")

# List unlocked summoners
var ids: Array[String] = SummonerSelection.get_unlocked_summoner_ids()
```

**Signals:**
- `summoner_changed(old_summoner_id, new_summoner_id)` - Emitted when active summoner changes

#### SummonerProgression (autoload: `/root/SummonerProgression`)
Manages XP and level-up mechanics.

```gdscript
# Grant XP
SummonerProgression.grant_summoner_xp("summoner_fire", 50)
SummonerProgression.grant_active_summoner_xp(100)

# Level up
if SummonerProgression.can_level_up("summoner_fire"):
    SummonerProgression.level_up_summoner("summoner_fire")

# Query progression
var info: Dictionary = SummonerProgression.get_summoner_progression_info("summoner_fire")
# Returns: {level, xp, xp_for_next_level, xp_progress, can_level_up, ...}
```

**Signals:**
- `summoner_xp_changed(summoner_id, new_xp, new_level)`
- `summoner_leveled_up(summoner_id, new_level)`

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

### SummonerConfig (Resource)
Static summoner configuration from SummonerCatalog.

```gdscript
class_name SummonerConfig extends Resource

var summoner_id: String          # "summoner_fire"
var summoner_name: String        # "Pyralis"
var description: String          # Flavor text
var element_id: int              # ElementRegistry.ElementId.FIRE

# Base Stats (before traits)
var base_health: float           # 1000.0 (flows to Incarnation HP)
var max_mana: float              # 100.0 (fixed pool, no regen during battle)

# Traits from TraitCatalog
var innate_trait_ids: Array[String]  # ["trait_fire_affinity", "trait_burning_spirit"]

# Visual
var summoner_icon_path: String
var card_frame_style: String     # "legendary"

# Unlock
var unlock_condition: String     # "starting_choice", "random_starter_only"
```

### SummonerInstance (RefCounted)
Runtime summoner state with progression.

```gdscript
class_name SummonerInstance extends RefCounted

var config: SummonerConfig       # Reference to static config
var level: int = 1               # Current level (1-10)
var xp: int = 0                  # Current XP

# Acquired Boons (from gameplay, stored as trait IDs)
var acquired_boon_ids: Array[String] = []

# Get all trait IDs (innate + acquired)
func get_all_trait_ids() -> Array[String]

# Get computed stats (base + all trait modifiers)
func get_computed_stats() -> Dictionary
# Returns: {health, max_mana, fire_damage_bonus, damage_reduction, ...}
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
        # Summoner stat modifier
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
- **Innate Traits**: Come with the summoner (defined in SummonerConfig.innate_trait_ids)
- **Acquired Boons**: Earned through gameplay (stored in SummonerInstance.acquired_boon_ids)

### Modifier Types

**Summoner Stat Modifiers** (applied in SummonerInstance._recompute_stats):
```gdscript
{"stat": "max_health", "type": "percent", "value": 10.0}  # +10% Incarnation health
{"stat": "max_mana", "type": "flat", "value": 10.0}       # +10 mana pool
{"stat": "fire_damage_bonus", "type": "percent", "value": 10.0}  # Sets 10% bonus
```

**Unit Modifiers** (passed to ModifierSystem via SummonerModifierProvider):
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
SummonerInstance.get_all_trait_ids()
        │
        ▼
TraitCatalog.get_trait(trait_id)
        │
        ├──► Summoner modifiers → SummonerInstance._apply_trait_modifiers()
        │                            │
        │                            ▼
        │                     Computed summoner stats
        │                     (health, mana, damage_bonus, etc.)
        │
        └──► Unit modifiers → SummonerModifierProvider.get_modifiers()
                                     │
                                     ▼
                              ModifierSystem
                                     │
                                     ▼
                              Applied to spawned units
```

---

## Battle Integration

### Summoner Stats in Battle

1. **Summoner loads summoner data** via DeckLoader
2. **Summoner._apply_summoner_bonuses()** computes final stats
3. **BattleContext.set_player_summoner_stats()** caches stats for DamageSystem
4. **DamageSystem** reads cached stats for damage bonuses/reduction

```gdscript
# In Summoner._apply_summoner_bonuses():
var computed_stats: Dictionary = summoner_instance.get_computed_stats()
BattleContext.set_player_summoner_stats(computed_stats)

# In DamageSystem._apply_summoner_damage_bonuses():
var summoner_stats: Dictionary = BattleContext.get_player_summoner_stats()
var damage_bonus: float = summoner_stats.get("damage_bonus", 0.0)
```

### Unit Modifiers in Battle

1. **GameController** registers SummonerModifierProvider with ModifierSystem
2. **When units spawn**, ModifierSystem queries all providers
3. **SummonerModifierProvider** returns unit modifiers from summoner's traits
4. **Modifiers are applied** to matching units (by element, tags, etc.)

```gdscript
# In GameController._register_summoner_provider():
var summoner_instance: SummonerInstance = _load_summoner_instance()
# Use factory method - GDScript can't instantiate C# classes directly
var modifier_service: Node = get_node_or_null("/root/ModifierService")
if modifier_service and modifier_service.has_method("register_summoner_provider"):
    modifier_service.call("register_summoner_provider", summoner_instance, summoner_id)
```

---

## Per-Summoner Campaign Progress

Campaign progress is stored per-summoner in ProfileRepo:

```gdscript
"campaign_progress": {
    "summoner_fire": {
        "completed_battles": ["battle_tutorial", "battle_first_slime"],
        "current_battle": null,
        "pending_reward": null
    },
    "summoner_water": {
        "completed_battles": ["battle_tutorial"],
        "current_battle": "battle_first_slime",
        "pending_reward": null
    }
}
```

### Accessing Progress

```gdscript
# Get progress for active summoner
var progress: Dictionary = ProfileRepo.get_campaign_progress()

# Get progress for specific summoner
var progress: Dictionary = ProfileRepo.get_campaign_progress("summoner_fire")

# Update progress (uses active summoner by default)
ProfileRepo.update_campaign_progress({"current_battle": "battle_02"})
```

---

## UI Components

### SummonerScreen
Full-screen summoner management interface:
- Summoner list panel (left) for switching between summoners
- Enhanced portrait with element-themed gradients and glow
- Stats display (HP, Mana with trait bonuses)
- XP progress and level-up with gold cost
- Traits section showing innate traits and acquired boons

### SummonerIconWidget
Persistent summoner portrait button:
- Shows active summoner's element color and level
- Click to open SummonerScreen
- Auto-updates when summoner changes

### SummonerRosterItem
Individual summoner row in roster:
- Portrait, name, level, stats
- XP progress bar
- Select/Level-up buttons

---

## Starting Summoner Assignment

### Available Starting Choices
- **Fire** (Pyralis) - trait_fire_affinity, trait_burning_spirit
- **Water** (Aquira) - trait_water_affinity, trait_tidal_resilience
- **Wind** (Zephyrion) - trait_wind_affinity, trait_swift_casting
- **Earth** (Terravorn) - trait_earth_affinity, trait_stone_fortitude
- **Random** - Picks from above + grants "Fortune Favors the Bold" boon

### Fortune Favors the Bold
Special boon granted only to summoners created via Random selection:
- Stored in `SummonerInstance.acquired_boon_ids`
- Provides +50 max health
- Trait ID: `trait_fortune_favors_bold`

---

## Implementation Files

### Services
- `scripts/services/summoner_selection_service.gd` - SummonerSelection autoload
- `scripts/services/summoner_progression_service.gd` - SummonerProgression autoload
- `scripts/data/trait_catalog.gd` - TraitCatalog autoload

### Data
- `scripts/core/summoner_config.gd` - SummonerConfig resource class
- `scripts/core/summoner_instance.gd` - SummonerInstance runtime class
- `scripts/data/summoner_catalog.gd` - SummonerCatalog autoload (static configs)

### Modifier Integration (C#)
- `scripts/csharp/Systems/Modifiers/SummonerModifierProvider.cs` - Provides unit modifiers from traits
- `scripts/csharp/Systems/Modifiers/ModifierService.cs` - Central modifier service (autoload)

### UI
- `scripts/ui/summoner_management_panel.gd` - Main summoner panel
- `scripts/ui/summoner_roster_item.gd` - Individual summoner in roster
- `scripts/ui/summoner_icon_widget.gd` - Persistent summoner button

### Scenes
- `scenes/ui/summoner_management_panel.tscn`
- `scenes/ui/summoner_roster_item.tscn`
- `scenes/ui/summoner_icon_widget.tscn`

---

## Localization

All summoner UI uses the localization system:

```json
{
  "ui": {
    "summoner_panel": {
      "title": "SUMMONER ROSTER",
      "level_display": "Lv.{level}",
      "xp_progress": "XP: {current} / {required}",
      "level_up_button": "LEVEL UP ({cost}g)",
      "stats_summary": "HP: {hp} | Mana: {mana}"
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
