extends RefCounted
class_name HeroModifierProvider

## HeroModifierProvider - Provides unit modifiers from hero traits
##
## Reads unit modifiers from TraitCatalog based on the hero's traits.
## This provider feeds into ModifierSystem to apply trait bonuses to spawned units.

var _hero_instance: HeroInstance

func _init(hero_inst: HeroInstance) -> void:
	_hero_instance = hero_inst

## Get all unit modifiers from hero traits
func get_modifiers() -> Array:
	var modifiers: Array = []

	if not _hero_instance:
		return modifiers

	# Get TraitCatalog autoload
	var trait_catalog: Node = Engine.get_main_loop().root.get_node_or_null("/root/TraitCatalog")
	if not trait_catalog:
		push_warning("HeroModifierProvider: TraitCatalog not found")
		return modifiers

	if not trait_catalog.has_method("get_unit_modifiers_for_trait"):
		push_warning("HeroModifierProvider: TraitCatalog.get_unit_modifiers_for_trait() not available")
		return modifiers

	# Collect unit modifiers from all hero traits
	var trait_ids: Array[String] = _hero_instance.get_all_trait_ids()
	for trait_id: String in trait_ids:
		var trait_mods: Array = trait_catalog.call("get_unit_modifiers_for_trait", trait_id)
		modifiers.append_array(trait_mods)

	return modifiers  # Already in ModifierSystem format!
