extends Node
# TraitCatalog is registered as autoload "TraitCatalog", no class_name needed

## Trait Catalog - Defines all summoner traits and boons
##
## Traits are passive abilities that modify summoner stats or provide special effects.
## - Innate traits: Come with the summoner (defined in SummonerConfig)
## - Acquired boons: Earned through gameplay (stored in SummonerInstance)
##
## Usage:
##   var trait_data = TraitCatalog.get_trait("trait_fire_affinity")
##   var all_traits = TraitCatalog.get_all_traits()

## Trait data storage
var _traits: Dictionary = {}

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("TraitCatalog: Initializing...")
	_init_traits()
	print("TraitCatalog: Loaded %d traits" % _traits.size())

## =============================================================================
## TRAIT DEFINITIONS
## =============================================================================

func _init_traits() -> void:
	# ==========================================================================
	# INNATE TRAITS (come with specific summoners)
	# ==========================================================================

	# Fire Summoner Traits
	_register_trait({
		"id": "trait_fire_affinity",
		"name_key": "trait.fire_affinity.name",
		"description_key": "trait.fire_affinity.description",
		"category": "elemental",
		"is_innate": true,
		"modifiers": [
			# Summoner stat modifier
			{"stat": "fire_damage_bonus", "type": "percent", "value": 10.0},
			# Unit modifier - buffs all fire units
			{
				"target": "unit",
				"source": "trait_fire_affinity",
				"conditions": {"elemental_affinity": "fire"},
				"stat_mults": {"attack_damage": 1.10}
			}
		]
	})

	_register_trait({
		"id": "trait_burning_spirit",
		"name_key": "trait.burning_spirit.name",
		"description_key": "trait.burning_spirit.description",
		"category": "combat",
		"is_innate": true,
		"modifiers": [
			{"stat": "mana_regen", "type": "flat", "value": 0.3}
		]
	})

	# Water Summoner Traits
	_register_trait({
		"id": "trait_water_affinity",
		"name_key": "trait.water_affinity.name",
		"description_key": "trait.water_affinity.description",
		"category": "elemental",
		"is_innate": true,
		"modifiers": [
			# Summoner stat modifier
			{"stat": "water_damage_bonus", "type": "percent", "value": 10.0},
			# Unit modifier - buffs all water units
			{
				"target": "unit",
				"source": "trait_water_affinity",
				"conditions": {"elemental_affinity": "water"},
				"stat_mults": {"attack_damage": 1.10}
			}
		]
	})

	_register_trait({
		"id": "trait_tidal_resilience",
		"name_key": "trait.tidal_resilience.name",
		"description_key": "trait.tidal_resilience.description",
		"category": "defense",
		"is_innate": true,
		"modifiers": [
			{"stat": "max_health", "type": "percent", "value": 10.0}
		]
	})

	# Wind Summoner Traits
	_register_trait({
		"id": "trait_wind_affinity",
		"name_key": "trait.wind_affinity.name",
		"description_key": "trait.wind_affinity.description",
		"category": "elemental",
		"is_innate": true,
		"modifiers": [
			# Summoner stat modifier
			{"stat": "wind_damage_bonus", "type": "percent", "value": 10.0},
			# Unit modifier - buffs all wind units
			{
				"target": "unit",
				"source": "trait_wind_affinity",
				"conditions": {"elemental_affinity": "wind"},
				"stat_mults": {"attack_damage": 1.10}
			}
		]
	})

	_register_trait({
		"id": "trait_swift_casting",
		"name_key": "trait.swift_casting.name",
		"description_key": "trait.swift_casting.description",
		"category": "utility",
		"is_innate": true,
		"modifiers": [
			{"stat": "cast_speed", "type": "percent", "value": 10.0}
		]
	})

	# Earth Summoner Traits
	_register_trait({
		"id": "trait_earth_affinity",
		"name_key": "trait.earth_affinity.name",
		"description_key": "trait.earth_affinity.description",
		"category": "elemental",
		"is_innate": true,
		"modifiers": [
			# Summoner stat modifier
			{"stat": "earth_damage_bonus", "type": "percent", "value": 10.0},
			# Unit modifier - buffs all earth units
			{
				"target": "unit",
				"source": "trait_earth_affinity",
				"conditions": {"elemental_affinity": "earth"},
				"stat_mults": {"attack_damage": 1.10}
			}
		]
	})

	_register_trait({
		"id": "trait_stone_fortitude",
		"name_key": "trait.stone_fortitude.name",
		"description_key": "trait.stone_fortitude.description",
		"category": "defense",
		"is_innate": true,
		"modifiers": [
			{"stat": "damage_reduction", "type": "flat", "value": 5.0}
		]
	})

	# ==========================================================================
	# ACQUIRED BOONS (earned through gameplay)
	# ==========================================================================

	_register_trait({
		"id": "boon_veteran",
		"name_key": "trait.veteran.name",
		"description_key": "trait.veteran.description",
		"category": "milestone",
		"is_innate": false,
		"modifiers": [
			{"stat": "max_health", "type": "flat", "value": 100.0}
		]
	})

	_register_trait({
		"id": "boon_mana_well",
		"name_key": "trait.mana_well.name",
		"description_key": "trait.mana_well.description",
		"category": "milestone",
		"is_innate": false,
		"modifiers": [
			{"stat": "max_mana", "type": "flat", "value": 2.0}
		]
	})

	_register_trait({
		"id": "boon_battle_hardened",
		"name_key": "trait.battle_hardened.name",
		"description_key": "trait.battle_hardened.description",
		"category": "milestone",
		"is_innate": false,
		"modifiers": [
			{"stat": "damage_bonus", "type": "percent", "value": 5.0}
		]
	})

	_register_trait({
		"id": "boon_fortune_favors",
		"name_key": "trait.fortune_favors.name",
		"description_key": "trait.fortune_favors.description",
		"category": "special",
		"is_innate": false,
		"modifiers": [
			{"stat": "gold_bonus", "type": "percent", "value": 10.0}
		]
	})

	# Special trait granted for choosing "Random" summoner
	_register_trait({
		"id": "trait_fortune_favors_bold",
		"name_key": "trait.fortune_favors_bold.name",
		"description_key": "trait.fortune_favors_bold.description",
		"category": "special",
		"is_innate": false,  # Not innate to any summoner, but granted by system
		"modifiers": [
			{"stat": "max_health", "type": "flat", "value": 50.0}
		]
	})

func _register_trait(trait_data: Dictionary) -> void:
	var trait_id: String = trait_data.get("id", "")
	if trait_id.is_empty():
		push_error("TraitCatalog: Cannot register trait without id")
		return
	_traits[trait_id] = trait_data

## =============================================================================
## QUERIES
## =============================================================================

## Get a trait by ID
func get_trait(trait_id: String) -> Dictionary:
	return _traits.get(trait_id, {})

## Check if a trait exists
func has_trait(trait_id: String) -> bool:
	return _traits.has(trait_id)

## Get all trait IDs
func get_all_trait_ids() -> Array[String]:
	var ids: Array[String] = []
	for key: String in _traits.keys():
		ids.append(key)
	return ids

## Get all traits
func get_all_traits() -> Array[Dictionary]:
	var traits: Array[Dictionary] = []
	for trait_data: Dictionary in _traits.values():
		traits.append(trait_data)
	return traits

## Get traits by category
func get_traits_by_category(category: String) -> Array[Dictionary]:
	var traits: Array[Dictionary] = []
	for trait_data: Dictionary in _traits.values():
		if trait_data.get("category", "") == category:
			traits.append(trait_data)
	return traits

## Get only innate traits
func get_innate_traits() -> Array[Dictionary]:
	var traits: Array[Dictionary] = []
	for trait_data: Dictionary in _traits.values():
		if trait_data.get("is_innate", false):
			traits.append(trait_data)
	return traits

## Get only acquirable boons
func get_acquirable_boons() -> Array[Dictionary]:
	var traits: Array[Dictionary] = []
	for trait_data: Dictionary in _traits.values():
		if not trait_data.get("is_innate", true):
			traits.append(trait_data)
	return traits

## =============================================================================
## DISPLAY HELPERS
## =============================================================================

## Get localized trait name
func get_trait_name(trait_id: String) -> String:
	var trait_data: Dictionary = get_trait(trait_id)
	if trait_data.is_empty():
		return trait_id
	var name_key: String = trait_data.get("name_key", "")
	if name_key.is_empty():
		return trait_id
	return Loc.t(name_key)

## Get localized trait description
func get_trait_description(trait_id: String) -> String:
	var trait_data: Dictionary = get_trait(trait_id)
	if trait_data.is_empty():
		return ""
	var desc_key: String = trait_data.get("description_key", "")
	if desc_key.is_empty():
		return ""
	return Loc.t(desc_key)

## Get unit modifiers for a trait (for SummonerModifierProvider)
## Returns modifiers that have target="unit" - these affect spawned units
func get_unit_modifiers_for_trait(trait_id: String) -> Array[Dictionary]:
	var trait_data: Dictionary = get_trait(trait_id)
	var result: Array[Dictionary] = []
	var modifiers: Variant = trait_data.get("modifiers", [])
	if not modifiers is Array:
		return result
	for mod: Variant in modifiers:
		if mod is Dictionary:
			var mod_dict: Dictionary = mod
			if mod_dict.get("target") == "unit":
				result.append(mod_dict)
	return result

## Get formatted modifier text for a trait
func get_trait_modifier_text(trait_id: String) -> String:
	var trait_data: Dictionary = get_trait(trait_id)
	if trait_data.is_empty():
		return ""

	var modifiers: Array = trait_data.get("modifiers", [])
	var texts: Array[String] = []

	for mod: Variant in modifiers:
		if not mod is Dictionary:
			continue
		var mod_dict: Dictionary = mod
		var stat: String = mod_dict.get("stat", "")
		var mod_type: String = mod_dict.get("type", "flat")
		var value: float = mod_dict.get("value", 0.0)

		var sign: String = "+" if value >= 0 else ""
		var suffix: String = "%" if mod_type == "percent" else ""
		var stat_name: String = stat.replace("_", " ").capitalize()

		texts.append("%s%s%s %s" % [sign, str(value), suffix, stat_name])

	return ", ".join(texts)
