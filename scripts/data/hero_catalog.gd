extends Node
# HeroCatalog is registered as autoload, no class_name needed

## Hero Catalog - Central Database of All Hero Definitions
##
## Single source of truth for all hero data in the game.
## Provides methods to look up heroes by ID, element, etc.
##
## Usage:
##   var hero_config = HeroCatalog.get_hero_config("hero_fire")
##   var hero_def = HeroCatalog.get_hero("hero_fire")  # Legacy Dictionary access
##   var all_heroes = HeroCatalog.list_all_heroes()
##   var starting_heroes = HeroCatalog.get_starting_heroes()

## Hero configurations
## Each hero is a HeroConfig instance
var _catalog: Dictionary = {}  ## Key: hero_id (String), Value: HeroConfig

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("HeroCatalog: Initializing...")
	_init_catalog()
	print("HeroCatalog: Loaded %d heroes" % _catalog.size())

## =============================================================================
## CATALOG INITIALIZATION
## =============================================================================

func _init_catalog() -> void:
	# Fire Hero - Pyralis
	var hero_fire: HeroConfig = HeroConfig.new()
	hero_fire.hero_id = "hero_fire"
	hero_fire.hero_name = Loc.t("hero.hero_fire.name")
	hero_fire.description = Loc.t("hero.hero_fire.description")
	hero_fire.element_id = ElementRegistry.ElementId.FIRE
	hero_fire.base_health = 1000.0
	hero_fire.max_mana = 10.0
	hero_fire.mana_regen = 1.0
	hero_fire.hero_icon_path = ""
	hero_fire.card_frame_style = "legendary"
	hero_fire.unlock_condition = "starting_choice"
	_catalog["hero_fire"] = hero_fire

	# Water Hero - Aquira
	var hero_water: HeroConfig = HeroConfig.new()
	hero_water.hero_id = "hero_water"
	hero_water.hero_name = Loc.t("hero.hero_water.name")
	hero_water.description = Loc.t("hero.hero_water.description")
	hero_water.element_id = ElementRegistry.ElementId.WATER
	hero_water.base_health = 1200.0  # Higher health, lower regen
	hero_water.max_mana = 10.0
	hero_water.mana_regen = 0.8
	hero_water.hero_icon_path = ""
	hero_water.card_frame_style = "legendary"
	hero_water.unlock_condition = "starting_choice"
	_catalog["hero_water"] = hero_water

	# Wind Hero - Zephyrion
	var hero_wind: HeroConfig = HeroConfig.new()
	hero_wind.hero_id = "hero_wind"
	hero_wind.hero_name = Loc.t("hero.hero_wind.name")
	hero_wind.description = Loc.t("hero.hero_wind.description")
	hero_wind.element_id = ElementRegistry.ElementId.WIND
	hero_wind.base_health = 900.0   # Lower health
	hero_wind.max_mana = 12.0       # Higher mana pool
	hero_wind.mana_regen = 1.2      # Faster regen
	hero_wind.hero_icon_path = ""
	hero_wind.card_frame_style = "legendary"
	hero_wind.unlock_condition = "starting_choice"
	_catalog["hero_wind"] = hero_wind

	# Earth Hero - Terravorn
	var hero_earth: HeroConfig = HeroConfig.new()
	hero_earth.hero_id = "hero_earth"
	hero_earth.hero_name = Loc.t("hero.hero_earth.name")
	hero_earth.description = Loc.t("hero.hero_earth.description")
	hero_earth.element_id = ElementRegistry.ElementId.EARTH
	hero_earth.base_health = 1500.0 # Highest health
	hero_earth.max_mana = 8.0       # Lower mana
	hero_earth.mana_regen = 0.7     # Slower regen
	hero_earth.hero_icon_path = ""
	hero_earth.card_frame_style = "legendary"
	hero_earth.unlock_condition = "starting_choice"
	_catalog["hero_earth"] = hero_earth

	# =========================================================================
	# STARTER-ONLY HEROES (Future / Optional for MVP)
	# These heroes are only available through the "Random" starting option.
	# They do NOT need to be implemented for the first MVP pass.
	# MVP can ship with only the 4 core heroes; Random just picks among those.
	# =========================================================================

	# Shadow Initiate (starter-only)
	var hero_shadow: HeroConfig = HeroConfig.new()
	hero_shadow.hero_id = "hero_shadow_initiate"
	hero_shadow.hero_name = Loc.t("hero.hero_shadow_initiate.name")
	hero_shadow.description = Loc.t("hero.hero_shadow_initiate.description")
	hero_shadow.element_id = ElementRegistry.ElementId.SHADOW
	hero_shadow.base_health = 950.0
	hero_shadow.max_mana = 11.0
	hero_shadow.mana_regen = 1.1
	hero_shadow.hero_icon_path = ""
	hero_shadow.card_frame_style = "rare"
	hero_shadow.unlock_condition = "random_starter_only"
	_catalog["hero_shadow_initiate"] = hero_shadow

	# TODO: Add 3-4 more starter-only heroes for other outer elements
	# - Lightning Adept
	# - Verdant Sage (Life)
	# - Void Walker (Death)

## =============================================================================
## LOOKUP METHODS
## =============================================================================

## Get a hero configuration by ID (typed class)
func get_hero_config(hero_id: String) -> HeroConfig:
	if hero_id.is_empty():
		push_warning("HeroCatalog.get_hero_config: Empty hero_id provided")
		return null

	if not _catalog.has(hero_id):
		push_warning("HeroCatalog.get_hero_config: Hero not found: %s" % hero_id)
		return null

	return _catalog[hero_id]

## Get a hero definition by ID (legacy Dictionary access)
## Returns empty Dictionary if hero not found
func get_hero(hero_id: String) -> Dictionary:
	var config: HeroConfig = get_hero_config(hero_id)
	if config == null:
		return {}
	return config.to_dict()

## Check if a hero exists in the catalog
func has_hero(hero_id: String) -> bool:
	return _catalog.has(hero_id)

## Validate if a hero ID is valid (exists in catalog)
func is_valid_hero(hero_id: String) -> bool:
	return _catalog.has(hero_id)

## Get all hero IDs
func get_all_hero_ids() -> Array[String]:
	var ids: Array[String] = []
	ids.assign(_catalog.keys())
	return ids

## Get all hero configs
func get_all_hero_configs() -> Array[HeroConfig]:
	var configs: Array[HeroConfig] = []
	for config: HeroConfig in _catalog.values():
		configs.append(config)
	return configs

## Get all heroes as an array of dictionaries (legacy)
func list_all_heroes() -> Array[Dictionary]:
	var heroes: Array[Dictionary] = []
	for config: HeroConfig in _catalog.values():
		heroes.append(config.to_dict())
	return heroes

## Get heroes that can be selected as starting heroes (4 core heroes)
func get_starting_heroes() -> Array[Dictionary]:
	var starting: Array[Dictionary] = []
	for config: HeroConfig in _catalog.values():
		if config.unlock_condition == "starting_choice":
			starting.append(config.to_dict())
	return starting

## Get heroes available for "Random" option (core + starter-only)
func get_random_pool_heroes() -> Array[Dictionary]:
	var random_pool: Array[Dictionary] = []
	for config: HeroConfig in _catalog.values():
		if config.unlock_condition == "starting_choice" or config.unlock_condition == "random_starter_only":
			random_pool.append(config.to_dict())
	return random_pool

## Get heroes by element
func get_heroes_by_element(element: ElementTypes.Element) -> Array[Dictionary]:
	var heroes: Array[Dictionary] = []
	for config: HeroConfig in _catalog.values():
		if config.get_element() == element:
			heroes.append(config.to_dict())
	return heroes

## Get hero name (localized)
func get_hero_name(hero_id: String) -> String:
	var config: HeroConfig = get_hero_config(hero_id)
	if config == null:
		return ""
	return config.hero_name

## Get hero element
func get_hero_element(hero_id: String) -> ElementTypes.Element:
	var config: HeroConfig = get_hero_config(hero_id)
	if config == null:
		return ElementTypes.NEUTRAL
	return config.get_element()

## Print catalog summary for debugging
func print_catalog_summary() -> void:
	print("=== Hero Catalog Summary ===")
	print("Total Heroes: %d" % _catalog.size())
	print("\nStarting Heroes:")
	for config: HeroConfig in _catalog.values():
		if config.unlock_condition == "starting_choice":
			print("  - %s (%s) | HP: %.0f | Mana: %.0f (%.1f/s)" % [
				config.hero_name,
				ElementTypes.get_display_name(config.get_element()),
				config.base_health,
				config.max_mana,
				config.mana_regen
			])
	print("\nRandom Pool Heroes:")
	for config: HeroConfig in _catalog.values():
		if config.unlock_condition == "starting_choice" or config.unlock_condition == "random_starter_only":
			print("  - %s (%s) | HP: %.0f | Mana: %.0f (%.1f/s)" % [
				config.hero_name,
				ElementTypes.get_display_name(config.get_element()),
				config.base_health,
				config.max_mana,
				config.mana_regen
			])
	print("===========================")
