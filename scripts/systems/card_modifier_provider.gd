extends RefCounted
class_name CardModifierProvider

## CardModifierProvider - Provides card upgrade modifiers to ModifierSystem
##
## Each card instance can have upgrades that modify its stats.
## This provider wraps those upgrades in ModifierSystem format with
## instance-scoped filtering (only applies to the specific card).
##
## Usage:
##   var provider = CardModifierProvider.new("card_instance_123")
##   ModifierSystem.register_provider("card_card_instance_123", provider)

var _card_instance_id: String

func _init(card_instance_id: String) -> void:
	_card_instance_id = card_instance_id

## Get modifiers for this card instance
func get_modifiers() -> Array:
	var modifiers: Array = []

	if _card_instance_id.is_empty():
		return modifiers

	# Get CardProgressionService autoload
	var progression_service: Node = Engine.get_main_loop().root.get_node_or_null("/root/CardProgressionService")
	if not progression_service:
		# CardProgressionService may not exist in all contexts (e.g., quick battles)
		return modifiers

	if not progression_service.has_method("get_upgrade_stat_modifiers"):
		return modifiers

	# Get combined stat modifiers from all applied upgrades
	var stat_mods: Variant = progression_service.call("get_upgrade_stat_modifiers", _card_instance_id)
	if not stat_mods is Dictionary:
		return modifiers

	var stat_mods_dict: Dictionary = stat_mods
	if stat_mods_dict.is_empty():
		return modifiers

	# Wrap in ModifierSystem format with instance scope
	modifiers.append({
		"source": "card_upgrades",
		"card_instance_id": _card_instance_id,
		"stat_mults": stat_mods_dict
	})

	return modifiers
