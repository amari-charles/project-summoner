# Equipment System

The equipment system allows summoners to equip items that provide stat modifiers and bonuses.

## Overview

- **Replaced**: The previous "boons" system has been replaced with a proper equipment slot system
- **Slots**: Each summoner has 4 equipment slots: Weapon, Ring1, Ring2, Vestments
- **Ownership**: Items use content binding (AccountWide vs SummonerBound) to control access

## Equipment Slots

| Slot | Purpose | Example Items |
|------|---------|---------------|
| Weapon | Attack/damage items | Training Blade, Battle-Hardened Badge |
| Ring1 | Utility items | Simple Ring, Fortune's Charm |
| Ring2 | Utility items | Lucky Band |
| Vestments | Defense/survivability | Traveler's Cloak, Veteran's Medal |

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
Items.equip_item(summoner_id, instance_id, "weapon")

# Get equipped items
var equipped: Dictionary = Items.get_equipped_items(summoner_id)
# Returns: {"weapon": "item_001", "ring1": "", "ring2": "", "vestments": ""}

# List items available for a slot
var weapons: Array[Dictionary] = Items.list_items_for_slot("weapon", summoner_id)
```

### Slot Constants

Available via `Items` autoload:

```gdscript
Items.SLOT_WEAPON     # "weapon"
Items.SLOT_RING1      # "ring1"
Items.SLOT_RING2      # "ring2"
Items.SLOT_VESTMENTS  # "vestments"
Items.ALL_SLOTS       # ["weapon", "ring1", "ring2", "vestments"]

# Display helpers
Items.SLOT_DISPLAY_NAMES  # {"weapon": "Weapon", "ring1": "Ring", ...}
Items.SLOT_ICONS          # {"weapon": "⚔", "ring1": "💍", ...}
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

- `boon_veteran` → `item_veterans_medal` (Vestments)
- `boon_battle_hardened` → `item_battle_hardened_badge` (Weapon)
- `boon_fortune_favors` → `item_fortunes_charm` (Ring1)
- `fortune_favors_bold` → `item_bold_fortune_amulet` (Vestments)

Removed items (no longer in catalog):
- `mana_well_orb` (Grimoire slot removed)
- `apprentice_grimoire` (Grimoire slot removed)
