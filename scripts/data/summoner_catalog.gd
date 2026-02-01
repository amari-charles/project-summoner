extends Node
# SummonerCatalog is registered as autoload, no class_name needed

## Summoner Catalog - Thin wrapper delegating to C# SummonerCatalogCS
##
## All summoner data is defined in C# (scripts/csharp/Data/Summoners/SummonerCatalog.cs)
## This GDScript wrapper provides backwards-compatible API and creates SummonerConfig instances.
##
## Usage:
##   var summoner_config = SummonerCatalog.get_summoner_config(SummonerIDs.COLE)
##   var summoner_def = SummonerCatalog.get_summoner(SummonerIDs.COLE)  # Legacy Dictionary access
##   var all_summoners = SummonerCatalog.list_all_summoners()
##   var starting_summoners = SummonerCatalog.get_starting_summoners()

## Reference to the C# bridge (set in _ready)
var _cs_catalog: Node = null

## Cache of SummonerConfig instances (populated on demand)
var _config_cache: Dictionary = {}  ## Key: summoner_id (String), Value: SummonerConfig

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	# Wait for C# autoload to be available
	call_deferred("_connect_to_cs_catalog")

func _connect_to_cs_catalog() -> void:
	_cs_catalog = SummonerCatalogCS
	print("SummonerCatalog: Connected to C# catalog with %d summoners" % _cs_catalog.GetSummonerCount())
	# Validate trait IDs after connection
	call_deferred("_validate_trait_ids")

## =============================================================================
## LOOKUP METHODS
## =============================================================================

## Get a summoner configuration by ID (typed class)
func get_summoner_config(summoner_id: String) -> SummonerConfig:
	if summoner_id.is_empty():
		push_warning("SummonerCatalog.get_summoner_config: Empty summoner_id provided")
		return null

	# Check cache first
	if _config_cache.has(summoner_id):
		return _config_cache[summoner_id]

	# Get from C# and create SummonerConfig
	if not _cs_catalog:
		push_warning("SummonerCatalog.get_summoner_config: C# catalog not available")
		return null

	var summoner_dict: Dictionary = _cs_catalog.GetSummoner(summoner_id)
	if summoner_dict.is_empty():
		push_warning("SummonerCatalog.get_summoner_config: Summoner not found: %s" % summoner_id)
		return null

	var config: SummonerConfig = _create_config_from_dict(summoner_dict)
	_config_cache[summoner_id] = config
	return config

## Get a summoner definition by ID (legacy Dictionary access)
## Returns empty Dictionary if summoner not found
func get_summoner(summoner_id: String) -> Dictionary:
	var config: SummonerConfig = get_summoner_config(summoner_id)
	if config == null:
		return {}
	return config.to_dict()

## Check if a summoner exists in the catalog
func has_summoner(summoner_id: String) -> bool:
	if _cs_catalog:
		return _cs_catalog.HasSummoner(summoner_id)
	return false

## Validate if a summoner ID is valid (exists in catalog)
func is_valid_summoner(summoner_id: String) -> bool:
	return has_summoner(summoner_id)

## Get all summoner IDs
func get_all_summoner_ids() -> Array[String]:
	if _cs_catalog:
		var ids: Array[String] = []
		ids.assign(_cs_catalog.GetAllSummonerIds())
		return ids
	return []

## Get all summoner configs
func get_all_summoner_configs() -> Array[SummonerConfig]:
	var configs: Array[SummonerConfig] = []
	for summoner_id: String in get_all_summoner_ids():
		var config: SummonerConfig = get_summoner_config(summoner_id)
		if config:
			configs.append(config)
	return configs

## Get all summoners as an array of dictionaries (legacy)
func list_all_summoners() -> Array[Dictionary]:
	if _cs_catalog:
		var summoners: Array[Dictionary] = []
		summoners.assign(_cs_catalog.ListAllSummoners())
		return summoners
	return []

## Get summoners that can be selected as starting summoners (4 core summoners)
func get_starting_summoners() -> Array[Dictionary]:
	if _cs_catalog:
		var summoners: Array[Dictionary] = []
		summoners.assign(_cs_catalog.GetStartingSummoners())
		return summoners
	return []

## Get summoners available for "Random" option (core + starter-only)
func get_random_pool_summoners() -> Array[Dictionary]:
	if _cs_catalog:
		var summoners: Array[Dictionary] = []
		summoners.assign(_cs_catalog.GetRandomPoolSummoners())
		return summoners
	return []

## Get summoners available for purchase in the Premium Store
func get_purchasable_summoners() -> Array[SummonerConfig]:
	if _cs_catalog:
		var purchasable: Array[SummonerConfig] = []
		var dicts: Array = []
		dicts.assign(_cs_catalog.GetPurchasableSummoners())
		for dict: Dictionary in dicts:
			var summoner_id: String = dict.get("summoner_id", "")
			var config: SummonerConfig = get_summoner_config(summoner_id)
			if config:
				purchasable.append(config)
		return purchasable
	return []

## Get summoners by element
func get_summoners_by_element(element: ElementTypes.Element) -> Array[Dictionary]:
	# Filter from all summoners since C# bridge doesn't have this exact method
	var summoners: Array[Dictionary] = []
	for dict: Dictionary in list_all_summoners():
		var summoner_id: String = dict.get("summoner_id", "")
		var config: SummonerConfig = get_summoner_config(summoner_id)
		if config and config.get_element() == element:
			summoners.append(dict)
	return summoners

## Get summoner name (localized)
func get_summoner_name(summoner_id: String) -> String:
	var config: SummonerConfig = get_summoner_config(summoner_id)
	if config == null:
		return ""
	return config.summoner_name

## Get summoner element
func get_summoner_element(summoner_id: String) -> ElementTypes.Element:
	var config: SummonerConfig = get_summoner_config(summoner_id)
	if config == null:
		return ElementTypes.NEUTRAL
	return config.get_element()

## Print catalog summary for debugging
func print_catalog_summary() -> void:
	print("=== Summoner Catalog Summary ===")
	print("Total Summoners: %d" % get_all_summoner_ids().size())
	print("\nStarting Summoners:")
	for dict: Dictionary in get_starting_summoners():
		var summoner_id: String = dict.get("summoner_id", "")
		var config: SummonerConfig = get_summoner_config(summoner_id)
		if config:
			print("  - %s (%s) | HP: %.0f | Mana: %.0f" % [
				config.summoner_name,
				ElementTypes.get_display_name(config.get_element()),
				config.base_health,
				config.max_mana
			])
	print("===========================")

## =============================================================================
## INTERNAL HELPERS
## =============================================================================

## Create a SummonerConfig from C# dictionary data
func _create_config_from_dict(dict: Dictionary) -> SummonerConfig:
	var config: SummonerConfig = SummonerConfig.new()
	config.summoner_id = dict.get("summoner_id", "")
	config.summoner_name = Loc.t(dict.get("name_key", ""))
	config.description = Loc.t(dict.get("description_key", ""))
	config.element_id = dict.get("element_id", ElementRegistry.ElementId.NEUTRAL)
	config.base_health = dict.get("base_health", 1000.0)
	config.max_mana = dict.get("max_mana", 100.0)
	config.summoner_icon_path = dict.get("summoner_icon_path", "")
	config.card_frame_style = dict.get("card_frame_style", "legendary")
	config.unlock_condition = dict.get("unlock_condition", "starting_choice")

	# Load trait IDs
	var traits_var: Variant = dict.get("innate_trait_ids", [])
	if traits_var is Array:
		for trait_id: Variant in traits_var:
			if trait_id is String:
				config.innate_trait_ids.append(trait_id)

	# Starter card
	config.starter_card_id = dict.get("starter_card_id", CardIDs.FIRE_WISP)

	return config

## =============================================================================
## VALIDATION
## =============================================================================

## Validate all trait IDs in summoner configs against TraitCatalog
## Called after autoloads are initialized to ensure TraitCatalog is available
func _validate_trait_ids() -> void:
	var invalid_count: int = 0
	for summoner_id: String in get_all_summoner_ids():
		var config: SummonerConfig = get_summoner_config(summoner_id)
		if not config:
			continue
		for trait_id: String in config.innate_trait_ids:
			if not TraitCatalog.has_trait(trait_id):
				push_error("SummonerCatalog: Summoner '%s' has unknown trait ID: '%s'" % [summoner_id, trait_id])
				invalid_count += 1

	if invalid_count > 0:
		push_error("SummonerCatalog: Found %d invalid trait ID(s) - these traits will not be applied!" % invalid_count)
	else:
		print("SummonerCatalog: All trait IDs validated successfully")
