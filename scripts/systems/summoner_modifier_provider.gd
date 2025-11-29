extends RefCounted
class_name SummonerModifierProvider

## SummonerModifierProvider - Provides unit modifiers from summoner traits
##
## Reads unit modifiers from TraitCatalog based on the summoner's traits.
## This provider feeds into ModifierSystem to apply trait bonuses to spawned units.

var _summoner_instance: SummonerInstance

func _init(summoner_inst: SummonerInstance) -> void:
	_summoner_instance = summoner_inst

## Get all unit modifiers from summoner traits
func get_modifiers() -> Array:
	var modifiers: Array = []

	if not _summoner_instance:
		return modifiers

	# Get TraitCatalog autoload
	var trait_catalog: Node = Engine.get_main_loop().root.get_node_or_null("/root/TraitCatalog")
	if not trait_catalog:
		push_warning("SummonerModifierProvider: TraitCatalog not found")
		return modifiers

	if not trait_catalog.has_method("get_unit_modifiers_for_trait"):
		push_warning("SummonerModifierProvider: TraitCatalog.get_unit_modifiers_for_trait() not available")
		return modifiers

	# Collect unit modifiers from all summoner traits
	var trait_ids: Array[String] = _summoner_instance.get_all_trait_ids()
	for trait_id: String in trait_ids:
		var trait_mods: Array = trait_catalog.call("get_unit_modifiers_for_trait", trait_id)
		modifiers.append_array(trait_mods)

	return modifiers  # Already in ModifierSystem format!
