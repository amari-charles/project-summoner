extends Node
# TraitCatalog is registered as autoload "TraitCatalog", no class_name needed

## Trait Catalog - Thin wrapper delegating to C# TraitCatalogCS
##
## Traits are passive abilities that modify summoner stats or provide special effects.
## - Innate traits: Come with the summoner (defined in SummonerConfig)
## - Acquired boons: Earned through gameplay (stored in SummonerInstance)
##
## All trait data is defined in C# (scripts/csharp/Data/Traits/TraitCatalog.cs)
## This GDScript wrapper provides backwards-compatible API for existing code.
##
## Usage:
##   var trait_data = TraitCatalog.get_trait("trait_fire_affinity")
##   var all_traits = TraitCatalog.get_all_traits()

## Reference to the C# bridge (set in _ready)
var _cs_catalog: Node = null

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	# Wait for C# autoload to be available
	call_deferred("_connect_to_cs_catalog")

func _connect_to_cs_catalog() -> void:
	_cs_catalog = get_node_or_null("/root/TraitCatalogCS")
	if _cs_catalog:
		print("TraitCatalog: Connected to C# catalog with %d traits" % _cs_catalog.get_trait_count())
	else:
		push_error("TraitCatalog: Failed to connect to TraitCatalogCS - C# autoload not found")

## =============================================================================
## QUERIES (delegates to C#)
## =============================================================================

## Get a trait by ID
func get_trait(trait_id: String) -> Dictionary:
	if _cs_catalog:
		var result: Variant = _cs_catalog.get_trait(trait_id)
		return result if result is Dictionary else {}
	push_warning("TraitCatalog.get_trait: C# catalog not available")
	return {}

## Check if a trait exists
func has_trait(trait_id: String) -> bool:
	if _cs_catalog:
		return _cs_catalog.has_trait(trait_id)
	return false

## Get all trait IDs
func get_all_trait_ids() -> Array[String]:
	if _cs_catalog:
		var ids: Array[String] = []
		ids.assign(_cs_catalog.get_all_trait_ids())
		return ids
	return []

## Get all traits
func get_all_traits() -> Array[Dictionary]:
	if _cs_catalog:
		var traits: Array[Dictionary] = []
		traits.assign(_cs_catalog.get_all_traits())
		return traits
	return []

## Get traits by category
func get_traits_by_category(category: String) -> Array[Dictionary]:
	if _cs_catalog:
		var traits: Array[Dictionary] = []
		traits.assign(_cs_catalog.get_traits_by_category(category))
		return traits
	return []

## Get only innate traits
func get_innate_traits() -> Array[Dictionary]:
	if _cs_catalog:
		var traits: Array[Dictionary] = []
		traits.assign(_cs_catalog.get_innate_traits())
		return traits
	return []

## Get only acquirable boons
func get_acquirable_boons() -> Array[Dictionary]:
	if _cs_catalog:
		var traits: Array[Dictionary] = []
		traits.assign(_cs_catalog.get_acquirable_boons())
		return traits
	return []

## =============================================================================
## DISPLAY HELPERS (delegates to C#)
## =============================================================================

## Get localized trait name
func get_trait_name(trait_id: String) -> String:
	if _cs_catalog:
		return _cs_catalog.get_trait_name(trait_id)
	return trait_id

## Get localized trait description
func get_trait_description(trait_id: String) -> String:
	if _cs_catalog:
		return _cs_catalog.get_trait_description(trait_id)
	return ""

## Get unit modifiers for a trait (for SummonerModifierProvider)
## Returns modifiers that have target="unit" - these affect spawned units
func get_unit_modifiers_for_trait(trait_id: String) -> Array[Dictionary]:
	if _cs_catalog:
		var result: Array[Dictionary] = []
		result.assign(_cs_catalog.get_unit_modifiers_for_trait(trait_id))
		return result
	return []

## Get formatted modifier text for a trait
func get_trait_modifier_text(trait_id: String) -> String:
	if _cs_catalog:
		return _cs_catalog.get_trait_modifier_text(trait_id)
	return ""
