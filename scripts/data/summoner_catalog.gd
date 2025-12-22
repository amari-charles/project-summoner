extends Node
# SummonerCatalog is registered as autoload, no class_name needed

## Summoner Catalog - Central Database of All Summoner Definitions
##
## Single source of truth for all summoner data in the game.
## Provides methods to look up summoners by ID, element, etc.
##
## Usage:
##   var summoner_config = SummonerCatalog.get_summoner_config(SummonerIDs.FIRE)
##   var summoner_def = SummonerCatalog.get_summoner(SummonerIDs.FIRE)  # Legacy Dictionary access
##   var all_summoners = SummonerCatalog.list_all_summoners()
##   var starting_summoners = SummonerCatalog.get_starting_summoners()

## Summoner configurations
## Each summoner is a SummonerConfig instance
var _catalog: Dictionary = {}  ## Key: summoner_id (StringName), Value: SummonerConfig

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("SummonerCatalog: Initializing...")
	_init_catalog()
	print("SummonerCatalog: Loaded %d summoners" % _catalog.size())
	# Validate trait IDs after all autoloads are ready
	call_deferred("_validate_trait_ids")

## =============================================================================
## CATALOG INITIALIZATION
## =============================================================================

func _init_catalog() -> void:
	# Fire Summoner - Pyralis
	var summoner_fire: SummonerConfig = SummonerConfig.new()
	summoner_fire.summoner_id = SummonerIDs.FIRE
	summoner_fire.summoner_name = Loc.t("summoner.summoner_fire.name")
	summoner_fire.description = Loc.t("summoner.summoner_fire.description")
	summoner_fire.element_id = ElementRegistry.ElementId.FIRE
	summoner_fire.base_health = 1000.0
	summoner_fire.max_mana = 100.0
	summoner_fire.summoner_icon_path = ""
	summoner_fire.card_frame_style = "legendary"
	summoner_fire.unlock_condition = "starting_choice"
	summoner_fire.innate_trait_ids = ["trait_fire_affinity", "trait_burning_spirit"]
	_catalog[SummonerIDs.FIRE] = summoner_fire

	# Water Summoner - Aquira
	var summoner_water: SummonerConfig = SummonerConfig.new()
	summoner_water.summoner_id = SummonerIDs.WATER
	summoner_water.summoner_name = Loc.t("summoner.summoner_water.name")
	summoner_water.description = Loc.t("summoner.summoner_water.description")
	summoner_water.element_id = ElementRegistry.ElementId.WATER
	summoner_water.base_health = 1200.0
	summoner_water.max_mana = 100.0
	summoner_water.summoner_icon_path = ""
	summoner_water.card_frame_style = "legendary"
	summoner_water.unlock_condition = "starting_choice"
	summoner_water.innate_trait_ids = ["trait_water_affinity", "trait_tidal_resilience"]
	_catalog[SummonerIDs.WATER] = summoner_water

	# Wind Summoner - Zephyrion
	var summoner_wind: SummonerConfig = SummonerConfig.new()
	summoner_wind.summoner_id = SummonerIDs.WIND
	summoner_wind.summoner_name = Loc.t("summoner.summoner_wind.name")
	summoner_wind.description = Loc.t("summoner.summoner_wind.description")
	summoner_wind.element_id = ElementRegistry.ElementId.WIND
	summoner_wind.base_health = 900.0
	summoner_wind.max_mana = 100.0
	summoner_wind.summoner_icon_path = ""
	summoner_wind.card_frame_style = "legendary"
	summoner_wind.unlock_condition = "starting_choice"
	summoner_wind.innate_trait_ids = ["trait_wind_affinity", "trait_swift_casting"]
	_catalog[SummonerIDs.WIND] = summoner_wind

	# Earth Summoner - Terravorn
	var summoner_earth: SummonerConfig = SummonerConfig.new()
	summoner_earth.summoner_id = SummonerIDs.EARTH
	summoner_earth.summoner_name = Loc.t("summoner.summoner_earth.name")
	summoner_earth.description = Loc.t("summoner.summoner_earth.description")
	summoner_earth.element_id = ElementRegistry.ElementId.EARTH
	summoner_earth.base_health = 1500.0
	summoner_earth.max_mana = 100.0
	summoner_earth.summoner_icon_path = ""
	summoner_earth.card_frame_style = "legendary"
	summoner_earth.unlock_condition = "starting_choice"
	summoner_earth.innate_trait_ids = ["trait_earth_affinity", "trait_stone_fortitude"]
	_catalog[SummonerIDs.EARTH] = summoner_earth

	# =========================================================================
	# STARTER-ONLY SUMMONERS (Future / Optional for MVP)
	# These summoners are only available through the "Random" starting option.
	# They do NOT need to be implemented for the first MVP pass.
	# MVP can ship with only the 4 core summoners; Random just picks among those.
	# =========================================================================

	# Shadow Initiate (starter-only)
	var summoner_shadow: SummonerConfig = SummonerConfig.new()
	summoner_shadow.summoner_id = SummonerIDs.SHADOW_INITIATE
	summoner_shadow.summoner_name = Loc.t("summoner.summoner_shadow_initiate.name")
	summoner_shadow.description = Loc.t("summoner.summoner_shadow_initiate.description")
	summoner_shadow.element_id = ElementRegistry.ElementId.SHADOW
	summoner_shadow.base_health = 950.0
	summoner_shadow.max_mana = 100.0
	summoner_shadow.summoner_icon_path = ""
	summoner_shadow.card_frame_style = "rare"
	summoner_shadow.unlock_condition = "random_starter_only"
	_catalog[SummonerIDs.SHADOW_INITIATE] = summoner_shadow

	# =========================================================================
	# PURCHASABLE SUMMONERS (Premium Store)
	# These summoners can be unlocked via the Premium Store with gold.
	# Once unlocked, they can be selected for any new campaign.
	# =========================================================================

	# Lightning Adept - Fast glass cannon with high burst potential
	var summoner_lightning: SummonerConfig = SummonerConfig.new()
	summoner_lightning.summoner_id = SummonerIDs.LIGHTNING_ADEPT
	summoner_lightning.summoner_name = Loc.t("summoner.summoner_lightning_adept.name")
	summoner_lightning.description = Loc.t("summoner.summoner_lightning_adept.description")
	summoner_lightning.element_id = ElementRegistry.ElementId.LIGHTNING
	summoner_lightning.base_health = 800.0
	summoner_lightning.max_mana = 100.0
	summoner_lightning.summoner_icon_path = ""
	summoner_lightning.card_frame_style = "epic"
	summoner_lightning.unlock_condition = "premium_purchase"
	summoner_lightning.innate_trait_ids = ["trait_lightning_affinity"]
	_catalog[SummonerIDs.LIGHTNING_ADEPT] = summoner_lightning

	# Verdant Sage - Life element healer/support with high survivability
	var summoner_life: SummonerConfig = SummonerConfig.new()
	summoner_life.summoner_id = SummonerIDs.VERDANT_SAGE
	summoner_life.summoner_name = Loc.t("summoner.summoner_verdant_sage.name")
	summoner_life.description = Loc.t("summoner.summoner_verdant_sage.description")
	summoner_life.element_id = ElementRegistry.ElementId.LIFE
	summoner_life.base_health = 1100.0
	summoner_life.max_mana = 100.0
	summoner_life.summoner_icon_path = ""
	summoner_life.card_frame_style = "epic"
	summoner_life.unlock_condition = "premium_purchase"
	summoner_life.innate_trait_ids = ["trait_life_affinity"]
	_catalog[SummonerIDs.VERDANT_SAGE] = summoner_life

	# Void Walker - Death element with draining abilities
	var summoner_void: SummonerConfig = SummonerConfig.new()
	summoner_void.summoner_id = SummonerIDs.VOID_WALKER
	summoner_void.summoner_name = Loc.t("summoner.summoner_void_walker.name")
	summoner_void.description = Loc.t("summoner.summoner_void_walker.description")
	summoner_void.element_id = ElementRegistry.ElementId.DEATH
	summoner_void.base_health = 950.0
	summoner_void.max_mana = 100.0
	summoner_void.summoner_icon_path = ""
	summoner_void.card_frame_style = "epic"
	summoner_void.unlock_condition = "premium_purchase"
	summoner_void.innate_trait_ids = ["trait_death_affinity"]
	_catalog[SummonerIDs.VOID_WALKER] = summoner_void

	# =========================================================================
	# DEV/TEST SUMMONERS
	# These summoners are for testing features. Not available to players.
	# =========================================================================

	# Mana Test Summoner - High mana pool for testing tiered mana bar
	var summoner_mana_test: SummonerConfig = SummonerConfig.new()
	summoner_mana_test.summoner_id = SummonerIDs.MANA_TEST
	summoner_mana_test.summoner_name = Loc.t("summoner.summoner_mana_test.name")
	summoner_mana_test.description = Loc.t("summoner.summoner_mana_test.description")
	summoner_mana_test.element_id = ElementRegistry.ElementId.NEUTRAL
	summoner_mana_test.base_health = 1000.0
	summoner_mana_test.max_mana = 100.0
	summoner_mana_test.summoner_icon_path = ""
	summoner_mana_test.card_frame_style = "common"
	summoner_mana_test.unlock_condition = "dev_only"
	_catalog[SummonerIDs.MANA_TEST] = summoner_mana_test

## =============================================================================
## LOOKUP METHODS
## =============================================================================

## Get a summoner configuration by ID (typed class)
func get_summoner_config(summoner_id: String) -> SummonerConfig:
	if summoner_id.is_empty():
		push_warning("SummonerCatalog.get_summoner_config: Empty summoner_id provided")
		return null

	# Convert to StringName for catalog lookup (catalog keys are StringName)
	var key: StringName = StringName(summoner_id)
	if not _catalog.has(key):
		push_warning("SummonerCatalog.get_summoner_config: Summoner not found: %s" % summoner_id)
		return null

	return _catalog[key]

## Get a summoner definition by ID (legacy Dictionary access)
## Returns empty Dictionary if summoner not found
func get_summoner(summoner_id: String) -> Dictionary:
	var config: SummonerConfig = get_summoner_config(summoner_id)
	if config == null:
		return {}
	return config.to_dict()

## Check if a summoner exists in the catalog
func has_summoner(summoner_id: String) -> bool:
	return _catalog.has(StringName(summoner_id))

## Validate if a summoner ID is valid (exists in catalog)
func is_valid_summoner(summoner_id: String) -> bool:
	return _catalog.has(StringName(summoner_id))

## Get all summoner IDs
func get_all_summoner_ids() -> Array[String]:
	var ids: Array[String] = []
	ids.assign(_catalog.keys())
	return ids

## Get all summoner configs
func get_all_summoner_configs() -> Array[SummonerConfig]:
	var configs: Array[SummonerConfig] = []
	for config: SummonerConfig in _catalog.values():
		configs.append(config)
	return configs

## Get all summoners as an array of dictionaries (legacy)
func list_all_summoners() -> Array[Dictionary]:
	var summoners: Array[Dictionary] = []
	for config: SummonerConfig in _catalog.values():
		summoners.append(config.to_dict())
	return summoners

## Get summoners that can be selected as starting summoners (4 core summoners)
func get_starting_summoners() -> Array[Dictionary]:
	var starting: Array[Dictionary] = []
	for config: SummonerConfig in _catalog.values():
		if config.unlock_condition == "starting_choice":
			starting.append(config.to_dict())
	return starting

## Get summoners available for "Random" option (core + starter-only)
func get_random_pool_summoners() -> Array[Dictionary]:
	var random_pool: Array[Dictionary] = []
	for config: SummonerConfig in _catalog.values():
		if config.unlock_condition == "starting_choice" or config.unlock_condition == "random_starter_only":
			random_pool.append(config.to_dict())
	return random_pool

## Get summoners available for purchase in the Premium Store
func get_purchasable_summoners() -> Array[SummonerConfig]:
	var purchasable: Array[SummonerConfig] = []
	for config: SummonerConfig in _catalog.values():
		if config.unlock_condition == "premium_purchase":
			purchasable.append(config)
	return purchasable

## Get summoners by element
func get_summoners_by_element(element: ElementTypes.Element) -> Array[Dictionary]:
	var summoners: Array[Dictionary] = []
	for config: SummonerConfig in _catalog.values():
		if config.get_element() == element:
			summoners.append(config.to_dict())
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
	print("Total Summoners: %d" % _catalog.size())
	print("\nStarting Summoners:")
	for config: SummonerConfig in _catalog.values():
		if config.unlock_condition == "starting_choice":
			print("  - %s (%s) | HP: %.0f | Mana: %.0f" % [
				config.summoner_name,
				ElementTypes.get_display_name(config.get_element()),
				config.base_health,
				config.max_mana
			])
	print("\nRandom Pool Summoners:")
	for config: SummonerConfig in _catalog.values():
		if config.unlock_condition == "starting_choice" or config.unlock_condition == "random_starter_only":
			print("  - %s (%s) | HP: %.0f | Mana: %.0f" % [
				config.summoner_name,
				ElementTypes.get_display_name(config.get_element()),
				config.base_health,
				config.max_mana
			])
	print("===========================")

## =============================================================================
## VALIDATION
## =============================================================================

## Validate all trait IDs in summoner configs against TraitCatalog
## Called after autoloads are initialized to ensure TraitCatalog is available
func _validate_trait_ids() -> void:
	var trait_catalog: Node = get_node_or_null("/root/TraitCatalog")
	if not trait_catalog:
		push_warning("SummonerCatalog: Cannot validate trait IDs - TraitCatalog not found")
		return

	if not trait_catalog.has_method("has_trait"):
		push_warning("SummonerCatalog: Cannot validate trait IDs - TraitCatalog.has_trait() not available")
		return

	var invalid_count: int = 0
	for summoner_id: String in _catalog.keys():
		var config: SummonerConfig = _catalog[summoner_id]
		for trait_id: String in config.innate_trait_ids:
			if not trait_catalog.call("has_trait", trait_id):
				push_error("SummonerCatalog: Summoner '%s' has unknown trait ID: '%s'" % [summoner_id, trait_id])
				invalid_count += 1

	if invalid_count > 0:
		push_error("SummonerCatalog: Found %d invalid trait ID(s) - these traits will not be applied!" % invalid_count)
	else:
		print("SummonerCatalog: All trait IDs validated successfully")
