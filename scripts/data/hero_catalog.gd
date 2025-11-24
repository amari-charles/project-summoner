extends Node
# HeroCatalog is registered as autoload, no class_name needed

## Hero Catalog - Central Database of All Hero Definitions
##
## Single source of truth for all hero data in the game.
## Provides methods to look up heroes by ID, element, etc.
##
## Usage:
##   var hero_def = HeroCatalog.get_hero("hero_fire")
##   var all_heroes = HeroCatalog.list_all_heroes()
##   var starting_heroes = HeroCatalog.get_starting_heroes()

## Hero data structure
## Each hero is defined as a Dictionary with all its properties
var _catalog: Dictionary = {}

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
	_catalog["hero_fire"] = {
		"hero_id": "hero_fire",
		"hero_name": "Pyralis",
		"description": "Master of flame and passion",
		"element": ElementTypes.FIRE,
		"base_health": 1000.0,
		"max_mana": 10.0,
		"mana_regen": 1.0,
		"traits": [],  # Empty in catalog; populated when hero is created
		"hero_icon_path": "",
		"card_frame_style": "legendary",
		"unlock_condition": "starting_choice"
	}

	# Water Hero - Aquira
	_catalog["hero_water"] = {
		"hero_id": "hero_water",
		"hero_name": "Aquira",
		"description": "Embodiment of adaptability and flow",
		"element": ElementTypes.WATER,
		"base_health": 1200.0,  # Higher health, lower regen
		"max_mana": 10.0,
		"mana_regen": 0.8,
		"traits": [],  # Empty in catalog; populated when hero is created
		"hero_icon_path": "",
		"card_frame_style": "legendary",
		"unlock_condition": "starting_choice"
	}

	# Wind Hero - Zephyrion
	_catalog["hero_wind"] = {
		"hero_id": "hero_wind",
		"hero_name": "Zephyrion",
		"description": "Spirit of freedom and motion",
		"element": ElementTypes.WIND,
		"base_health": 900.0,  # Lower health
		"max_mana": 12.0,      # Higher mana pool
		"mana_regen": 1.2,     # Faster regen
		"traits": [],  # Empty in catalog; populated when hero is created
		"hero_icon_path": "",
		"card_frame_style": "legendary",
		"unlock_condition": "starting_choice"
	}

	# Earth Hero - Terravorn
	_catalog["hero_earth"] = {
		"hero_id": "hero_earth",
		"hero_name": "Terravorn",
		"description": "Guardian of stability and endurance",
		"element": ElementTypes.EARTH,
		"base_health": 1500.0,  # Highest health
		"max_mana": 8.0,        # Lower mana
		"mana_regen": 0.7,      # Slower regen
		"traits": [],  # Empty in catalog; populated when hero is created
		"hero_icon_path": "",
		"card_frame_style": "legendary",
		"unlock_condition": "starting_choice"
	}

	# =========================================================================
	# STARTER-ONLY HEROES (Future / Optional for MVP)
	# These heroes are only available through the "Random" starting option.
	# They do NOT need to be implemented for the first MVP pass.
	# MVP can ship with only the 4 core heroes; Random just picks among those.
	# =========================================================================

	# Shadow Initiate (starter-only)
	_catalog["hero_shadow_initiate"] = {
		"hero_id": "hero_shadow_initiate",
		"hero_name": "Shadow Initiate",
		"description": "A mysterious figure cloaked in darkness",
		"element": ElementTypes.SHADOW,
		"base_health": 950.0,
		"max_mana": 11.0,
		"mana_regen": 1.1,
		"traits": [],  # Empty in catalog; populated when hero is created
		"hero_icon_path": "",
		"card_frame_style": "rare",
		"unlock_condition": "random_starter_only"
	}

	# TODO: Add 3-4 more starter-only heroes for other outer elements
	# - Lightning Adept
	# - Verdant Sage (Life)
	# - Void Walker (Death)

## =============================================================================
## LOOKUP METHODS
## =============================================================================

## Get a hero definition by ID
## Returns empty Dictionary if hero not found
func get_hero(hero_id: String) -> Dictionary:
	if hero_id.is_empty():
		push_warning("HeroCatalog.get_hero: Empty hero_id provided")
		return {}

	if not _catalog.has(hero_id):
		push_warning("HeroCatalog.get_hero: Hero not found: %s" % hero_id)
		return {}

	# Return a duplicate to prevent external modifications
	return _catalog[hero_id].duplicate(true)

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

## Get all heroes as an array of dictionaries
func list_all_heroes() -> Array[Dictionary]:
	var heroes: Array[Dictionary] = []
	for hero_data: Dictionary in _catalog.values():
		heroes.append(hero_data.duplicate(true))
	return heroes

## Get heroes that can be selected as starting heroes (4 core heroes)
func get_starting_heroes() -> Array[Dictionary]:
	var starting: Array[Dictionary] = []
	for hero_data: Dictionary in _catalog.values():
		if hero_data.get("unlock_condition", "") == "starting_choice":
			starting.append(hero_data.duplicate(true))
	return starting

## Get heroes available for "Random" option (core + starter-only)
func get_random_pool_heroes() -> Array[Dictionary]:
	var random_pool: Array[Dictionary] = []
	for hero_data: Dictionary in _catalog.values():
		var condition: String = hero_data.get("unlock_condition", "")
		if condition == "starting_choice" or condition == "random_starter_only":
			random_pool.append(hero_data.duplicate(true))
	return random_pool

## Get heroes by element
func get_heroes_by_element(element: ElementTypes.Element) -> Array[Dictionary]:
	var heroes: Array[Dictionary] = []
	for hero_data: Dictionary in _catalog.values():
		if hero_data.get("element", ElementTypes.NEUTRAL) == element:
			heroes.append(hero_data.duplicate(true))
	return heroes

## Get hero name (localized)
func get_hero_name(hero_id: String) -> String:
	var hero: Dictionary = get_hero(hero_id)
	if hero.is_empty():
		return ""
	return hero.get("hero_name", "")

## Get hero element
func get_hero_element(hero_id: String) -> ElementTypes.Element:
	var hero: Dictionary = get_hero(hero_id)
	if hero.is_empty():
		return ElementTypes.NEUTRAL
	return hero.get("element", ElementTypes.NEUTRAL)

## Print catalog summary for debugging
func print_catalog_summary() -> void:
	print("=== Hero Catalog Summary ===")
	print("Total Heroes: %d" % _catalog.size())
	print("\nStarting Heroes:")
	for hero: Dictionary in get_starting_heroes():
		print("  - %s (%s) | HP: %.0f | Mana: %.0f (%.1f/s)" % [
			hero.hero_name,
			ElementTypes.get_element_name(hero.element),
			hero.base_health,
			hero.max_mana,
			hero.mana_regen
		])
	print("\nRandom Pool Heroes:")
	for hero: Dictionary in get_random_pool_heroes():
		print("  - %s (%s) | HP: %.0f | Mana: %.0f (%.1f/s)" % [
			hero.hero_name,
			ElementTypes.get_element_name(hero.element),
			hero.base_health,
			hero.max_mana,
			hero.mana_regen
		])
	print("===========================")
