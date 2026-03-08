class_name ItemsApi
extends RefCounted

static func list_items_for_slot_dict(slot: String, summoner_id: String) -> Array:
	return SafeTypeUtils.array(Items.call("ListItemsForSlotDict", slot, summoner_id))

static func get_equipped_items_dict(summoner_id: String) -> Dictionary:
	return SafeTypeUtils.dict(Items.call("GetEquippedItemsDict", summoner_id))

static func grant_item(item_id: String) -> String:
	return SafeTypeUtils.string(Items.call("GrantItem", item_id), "")

static func equip_item_str(summoner_id: String, instance_id: String, slot: String) -> bool:
	return SafeTypeUtils.bool_val(Items.call("EquipItemStr", summoner_id, instance_id, slot), false)

static func unequip_item_str(summoner_id: String, slot: String) -> bool:
	return SafeTypeUtils.bool_val(Items.call("UnequipItemStr", summoner_id, slot), false)

static func clear_all_items() -> void:
	Items.call("ClearAllItems")
