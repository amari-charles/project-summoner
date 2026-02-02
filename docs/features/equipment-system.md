# Equipment System

The equipment system allows summoners to equip items that provide stat modifiers and bonuses.

## Overview

- **Replaced**: The previous "boons" system has been replaced with a proper equipment slot system
- **Slots**: Each summoner has 4 equipment slots: Wand, Ring1, Ring2, Robes
- **Ownership**: Items use content binding (AccountWide vs SummonerBound) to control access

## Equipment Slots

| Slot | Purpose | Example Items |
|------|---------|---------------|
| Wand | Attack/damage items | Training Blade, Battle-Hardened Badge |
| Ring1 | Utility items | Simple Ring, Fortune's Charm |
| Ring2 | Utility items | Lucky Band |
| Robes | Defense/survivability | Traveler's Cloak, Veteran's Medal |

## Content Binding

Items and cards use a binding system to determine ownership:

- **AccountWide**: Any summoner on the account can use the item (e.g., cosmetics, premium purchases)
- **SummonerBound**: Only the bound summoner can use it (e.g., caravan card purchases, progression rewards)

### How Binding Works

1. Items purchased from the **Premium Store** are `AccountWide`
2. Cards purchased from the **Caravan** (in-campaign shop) are `SummonerBound` to the active summoner
3. Starter items are `AccountWide`

## Architecture

### Services

- **ItemService (C#)**: `scripts/csharp/Services/Items/ItemService.cs` - Core item logic
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
    AccountWide,      // Any summoner can use
    SummonerBound     // Only bound summoner can use
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
- **EquipmentSlotModal**: Click a slot to view available items and equip/unequip

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
