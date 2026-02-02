extends Node
# ItemService is registered as autoload "Items", no class_name needed

## Item Service - Item Management (GDScript wrapper for C# ItemService)
##
## Handles all item operations (granting, equipping, querying).
## UI code should call this, never the C# service directly.
## Delegates to C# ItemServiceCS for core operations.
##
## Usage:
##   var instance_id = Items.grant_item("item_training_blade")
##   Items.equip_item(summoner_id, instance_id, "wand")
##   var equipped = Items.get_equipped_items(summoner_id)
##   var items = Items.list_items_for_slot("wand", summoner_id)
##
## Emits signals for reactive UI updates.

## Slot names for reference
const SLOT_WAND: String = "wand"
const SLOT_RING1: String = "ring1"
const SLOT_RING2: String = "ring2"
const SLOT_ROBES: String = "robes"

## All available slots
const ALL_SLOTS: Array[String] = [SLOT_WAND, SLOT_RING1, SLOT_RING2, SLOT_ROBES]

## Slot display names (for UI)
const SLOT_DISPLAY_NAMES: Dictionary = {
	"wand": "Wand",
	"ring1": "Ring",
	"ring2": "Ring",
	"robes": "Robes"
}

## Slot icons (unicode symbols as placeholders)
const SLOT_ICONS: Dictionary = {
	"wand": "🪄",
	"ring1": "💍",
	"ring2": "💍",
	"robes": "🧥"
}

## Signals (forwarded from C#)
signal item_granted(instance_id: String, catalog_id: String)
signal item_equipped(summoner_id: String, slot: String, item_instance_id: String)
signal item_unequipped(summoner_id: String, slot: String)

## C# service reference
var _cs_service: Node

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("ItemService: Initializing...")

	# Get C# service reference
	_cs_service = get_node_or_null("/root/ItemServiceCS")
	if _cs_service == null:
		push_error("ItemService: ItemServiceCS autoload not found")
		return

	# Connect to C# service signals
	_cs_service.ItemGranted.connect(_on_cs_item_granted)
	_cs_service.ItemEquipped.connect(_on_cs_item_equipped)
	_cs_service.ItemUnequipped.connect(_on_cs_item_unequipped)

	print("ItemService: Ready")


## =============================================================================
## INTERNAL - Signal forwarding from C#
## =============================================================================

func _on_cs_item_granted(instance_id: String, catalog_id: String) -> void:
	item_granted.emit(instance_id, catalog_id)


func _on_cs_item_equipped(summoner_id: String, slot: String, item_instance_id: Variant) -> void:
	# C# may pass null which becomes Variant in GDScript
	var id: String = item_instance_id if item_instance_id != null else ""
	item_equipped.emit(summoner_id, slot, id)


func _on_cs_item_unequipped(summoner_id: String, slot: String) -> void:
	item_unequipped.emit(summoner_id, slot)


## =============================================================================
## ITEM GRANTING (delegated to C#)
## =============================================================================

## Grant an item to the player's inventory
## Returns: item instance ID, or empty string if failed
func grant_item(catalog_id: String, bound_to_summoner_id: String = "") -> String:
	if _cs_service == null:
		return ""
	var bound_id: Variant = bound_to_summoner_id if not bound_to_summoner_id.is_empty() else null
	var result: Variant = _cs_service.GrantItem(catalog_id, bound_id)
	return result if result != null else ""


## =============================================================================
## EQUIPPING (delegated to C#)
## =============================================================================

## Equip an item to a summoner's slot
## slot: "wand", "ring1", "ring2", or "robes"
## Returns true if successful
func equip_item(summoner_id: String, item_instance_id: String, slot: String) -> bool:
	if _cs_service == null:
		return false
	return _cs_service.EquipItemStr(summoner_id, item_instance_id, slot)


## Unequip an item from a summoner's slot
## slot: "wand", "ring1", "ring2", or "robes"
## Returns true if successful
func unequip_item(summoner_id: String, slot: String) -> bool:
	if _cs_service == null:
		return false
	return _cs_service.UnequipItemStr(summoner_id, slot)


## =============================================================================
## ITEM QUERIES (delegated to C#)
## =============================================================================

## Get equipped items for a summoner
## Returns: Dictionary { "wand": instance_id or "", "ring1": ..., "ring2": ..., "robes": ... }
func get_equipped_items(summoner_id: String) -> Dictionary:
	if _cs_service == null:
		return _empty_equipped_dict()
	var result: Dictionary = _cs_service.GetEquippedItemsDict(summoner_id)
	return result


## Get all items owned by a summoner (AccountWide + SummonerBound items bound to them)
## Returns array of dictionaries with item info
func get_owned_items(summoner_id: String) -> Array[Dictionary]:
	if _cs_service == null:
		return []
	var result: Array = _cs_service.GetOwnedItemsDict(summoner_id)
	var typed_result: Array[Dictionary] = []
	typed_result.assign(result)
	return typed_result


## Get all items available for a specific slot
## Returns array of dictionaries with item info (includes instance_id, equipped_by)
func list_items_for_slot(slot: String, summoner_id: String) -> Array[Dictionary]:
	if _cs_service == null:
		return []
	var result: Array = _cs_service.ListItemsForSlotDict(slot, summoner_id)
	var typed_result: Array[Dictionary] = []
	typed_result.assign(result)
	return typed_result


## Get all items in inventory
func list_all_items() -> Array[Dictionary]:
	if _cs_service == null:
		return []
	# ItemService doesn't have this as a Dict method yet, return empty for now
	# Full inventory view would need additional C# interop
	return []


## Clear all items from inventory (for testing)
func clear_all_items() -> void:
	if _cs_service == null:
		return
	_cs_service.ClearAllItems()


## Get an item definition from the catalog by catalog ID
## Returns dictionary with item info, or empty dict if not found
func get_item_definition(catalog_id: String) -> Dictionary:
	# ItemCatalog is a static C# class, access via bridge
	var catalog_cs: Node = get_node_or_null("/root/CardCatalogCS")
	if catalog_cs == null:
		return {}
	# Use the static method through a helper
	# For now, return an empty dict - full catalog access needs setup
	return {}


## =============================================================================
## MODIFIER QUERIES (for stat computation)
## =============================================================================

## Get all modifiers from equipped items for a summoner as StatModifiers
## Returns array of StatModifier dictionaries that can be applied to units
func get_equipped_item_modifiers(summoner_id: String) -> Array[Dictionary]:
	if _cs_service == null:
		return []
	# Get StatModifiers as dictionaries from C#
	var result: Array = _cs_service.GetEquippedItemStatModifiersDict(summoner_id)
	var typed_result: Array[Dictionary] = []
	typed_result.assign(result)
	return typed_result


## =============================================================================
## HELPERS
## =============================================================================

## Get an empty equipped dictionary with all slots
func _empty_equipped_dict() -> Dictionary:
	return {
		SLOT_WAND: "",
		SLOT_RING1: "",
		SLOT_RING2: "",
		SLOT_ROBES: ""
	}


## Check if an item is equipped by a summoner
func is_item_equipped(summoner_id: String, item_instance_id: String) -> bool:
	var equipped: Dictionary = get_equipped_items(summoner_id)
	for slot: String in ALL_SLOTS:
		if equipped.get(slot, "") == item_instance_id:
			return true
	return false


## Get the slot an item is equipped in, or empty string if not equipped
func get_equipped_slot(summoner_id: String, item_instance_id: String) -> String:
	var equipped: Dictionary = get_equipped_items(summoner_id)
	for slot: String in ALL_SLOTS:
		if equipped.get(slot, "") == item_instance_id:
			return slot
	return ""
