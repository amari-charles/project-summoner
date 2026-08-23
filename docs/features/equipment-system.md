# Equipment System

The equipment system allows summoners to equip items that provide stat modifiers and bonuses.

## Overview

- **Replaced**: The previous "boons" system has been replaced with a proper equipment slot system
- **Slots**: Each summoner has 4 equipment slots: Wand, Ring1, Ring2, Robes
- **Ownership**: Gameplay items belong to one summoner and are not shared across the roster

## Equipment Slots

| Slot | Purpose | Example Items |
|------|---------|---------------|
| Wand | Attack/damage items | Training Blade, Battle-Hardened Badge |
| Ring1 | Utility items | Simple Ring, Fortune's Charm |
| Ring2 | Utility items | Lucky Band |
| Robes | Defense/survivability | Traveler's Cloak, Veteran's Medal |

## Ownership

Gameplay Inventory is summoner-scoped. An item acquired by one summoner is not
available to another summoner. Account-level cosmetics and purchases may still
exist, but they are not gameplay items in this Inventory.

The current persistence layer still contains the older `AccountWide` item-binding
path. Migrating existing definitions, grant call sites, and saved instances to the
summoner-owned rule is tracked separately from the Inventory UI prototype.

## Architecture

### Services

- **ItemService (C#)**: `scripts/csharp/Meta/Services/Items/ItemService.cs` - Core item logic
- **ItemService (GDScript)**: `scripts/services/item_service.gd` - GDScript wrapper, autoloaded as `Items`
- **ItemCatalog**: Static item definitions with slots, modifiers, and binding types

### Data Structures

```csharp
// ItemInstanceData - stored in profile
public class ItemInstanceData
{
    public string Id { get; set; }           // Unique instance ID
    public string CatalogId { get; set; }    // Reference to ItemCatalog
    public string? EquippedBySummonerId { get; set; }
    public string? EquippedSlot { get; set; }
    public string? BoundToSummonerId { get; set; }
}

// ContentBinding enum
public enum ContentBinding
{
    AccountWide,      // Legacy gameplay-item path pending migration
    SummonerBound     // Intended ownership for gameplay items
}
```

## Usage

### GDScript (UI Code)

```gdscript
# Grant an item
var instance_id = Items.grant_item("item_training_blade")

# Equip to summoner
Items.equip_item(summoner_id, instance_id, "wand")

# Get equipped items
var equipped: Dictionary = Items.get_equipped_items(summoner_id)
# Returns: {"wand": "item_001", "ring1": "", "ring2": "", "robes": ""}

# List items available for a slot
var wands: Array[Dictionary] = Items.list_items_for_slot("wand", summoner_id)
```

### Slot Constants

Available via `Items` autoload:

```gdscript
Items.SLOT_WAND       # "wand"
Items.SLOT_RING1      # "ring1"
Items.SLOT_RING2      # "ring2"
Items.SLOT_ROBES      # "robes"
Items.ALL_SLOTS       # ["wand", "ring1", "ring2", "robes"]

# Display helpers
Items.SLOT_DISPLAY_NAMES  # {"wand": "Wand", "ring1": "Ring", ...}
Items.SLOT_ICONS          # {"wand": "🪄", "ring1": "💍", ...}
```

## UI Components

- **SummonerScreen**: Shows equipped items in a 4-slot horizontal layout
- **InventoryOverlay (prototype)**: The bag opens owned items; selecting an
  equipment slot opens the same large overlay filtered to compatible items. The
  large-modal presentation remains subject to user evaluation.

### Inventory Presentation

The Inventory surface is primarily a large item grid with horizontal filters for
`All`, `Equipment`, `Materials`, `Consumables`, and `Quest Items`. Selecting an
item opens a smaller inspection modal for its icon, quantity, description,
effects, and equipped state. The normal bag context is browse-only; an equipment
slot context adds equip and unequip actions. Empty inventories and empty category
filters retain the square slot field rather than replacing it with a blank-state
message. The prototype uses a fixed 12-column by 5-row visible field of 88x88
design-space slots. Additional owned items continue in the scroll area; display
resolution does not change the grid composition or imply an inventory cap.

Transformative item actions belong to their associated world experiences rather
than the bag: rituals happen in the ritual space, card cracking through the
underground contact, quest delivery through its objective or NPC, and commerce
through a merchant. Whether Inventory ultimately uses a large overlay or a
dedicated screen remains under evaluation.

## Console Commands (Debug)

```
/items_grant <item_id>   - Grant an item to inventory
/items_grant_all         - Grant all starter items
/items_list              - List player's items and equipment
/items_equip <slot> <id> - Equip an item to a summoner
/items_clear             - Clear all items from inventory
```

## Migration from Boons

The v5 to v6 profile migration converts legacy boons to items:

- `boon_veteran` → `item_veterans_medal` (Robes)
- `boon_battle_hardened` → `item_battle_hardened_badge` (Wand)
- `boon_fortune_favors` → `item_fortunes_charm` (Ring1)
- `fortune_favors_bold` → `item_bold_fortune_amulet` (Robes)

Removed items (no longer in catalog):
- `mana_well_orb` (Grimoire slot removed)
- `apprentice_grimoire` (Grimoire slot removed)

## Item Modifiers and Unit Stats (Phase 4 - ✅ Complete)

Items now affect unit stats during battle via the modifier system:

### How It Works

1. **ItemModifierProvider** is registered at battle start (alongside SummonerModifierProvider)
2. Items with stat modifiers (e.g., +2% damage) are converted to `StatModifier` objects
3. When units spawn, they receive modifiers from all registered providers
4. Item bonuses are applied using the same two-phase (adds then mults) calculation

### Supported Item Stats

| Item Stat | Unit Stat | Effect |
|-----------|-----------|--------|
| `max_health` | `max_hp` | Increases unit HP |
| `damage_bonus` | `attack_damage` | Increases attack damage |
| `attack_speed` | `attack_speed` | Increases attack speed |
| `move_speed` | `move_speed` | Increases movement speed |
| `gold_bonus` | - | Summoner-only (no unit effect) |
| `xp_bonus` | - | Summoner-only (no unit effect) |

### Example Flow

```
Player equips Training Blade (+2% damage)
    ↓
Battle starts → ItemModifierProvider registered
    ↓
Player plays Fire Wisp card
    ↓
ModifierService collects from all providers:
  - SummonerModifierProvider: +10% fire damage (Fire Affinity)
  - ItemModifierProvider: +2% damage (Training Blade)
    ↓
Unit3D spawns with:
  - Base damage: 10
  - After mods: 10 * 1.10 * 1.02 = 11.22 damage
```

### GDScript API

```gdscript
# Get item modifiers as StatModifier dictionaries
var mods: Array[Dictionary] = Items.get_equipped_item_modifiers(summoner_id)

# Each modifier contains:
# {
#   "source": "item_summoner_123",
#   "stat_adds": {"max_hp": 25.0},
#   "stat_mults": {"attack_damage": 1.02}
# }
```

See `docs/features/modifier-system.md` for full modifier system documentation.
