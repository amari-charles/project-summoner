extends Resource
class_name SummonerConfig

## SummonerConfig - Typed configuration for a summoner
##
## Replaces dictionary-based summoner data with typed class.
## Includes base stats and trait system for extensibility.
## Can be created from JSON/Dictionary data (data-driven).

## Default stat values (used for @export defaults and from_dict() fallbacks)
const DEFAULT_BASE_HEALTH: float = 1000.0
const DEFAULT_MAX_MANA: float = 100.0

## Identity
@export var summoner_id: String = ""
@export var summoner_name: String = ""
@export var description: String = ""
@export_enum("NEUTRAL", "FIRE", "WATER", "WIND", "EARTH", "LIGHTNING", "SHADOW", "POISON", "LIFE", "DEATH", "OCCULTIST", "HOLY", "ICE", "METAL", "SPIRIT")
var element_id: int = ElementRegistry.ElementId.NEUTRAL

## Base Stats (before traits)
@export var base_health: float = DEFAULT_BASE_HEALTH
@export var max_mana: float = DEFAULT_MAX_MANA

## Visual
@export var summoner_icon_path: String = ""
@export var card_frame_style: String = "legendary"

## Unlock
@export var unlock_condition: String = "starting_choice"  ## "starting_choice", "random_starter_only", "unlock_after_battle"

## Trait System - trait IDs from TraitCatalog
@export var innate_trait_ids: Array[String] = []

## Starter card - catalog ID granted when this summoner is first selected
@export var starter_card_id: String = CardIDs.FIRE_WISP

## Get the Element object for this summoner (runtime)
func get_element() -> ElementTypes.Element:
	return ElementRegistry.get_element_from_id(element_id)

## Get the element key for serialization
func get_element_key() -> StringName:
	return ElementRegistry.get_key_from_id(element_id)

## Create from dictionary (for data-driven config from JSON)
static func from_dict(data: Dictionary) -> SummonerConfig:
	var config: SummonerConfig = SummonerConfig.new()

	# Identity
	config.summoner_id = data.get("summoner_id", "")
	config.summoner_name = data.get("summoner_name", "")
	config.description = data.get("description", "")

	# Element - handle Element objects, StringName keys, and legacy strings
	var element_var: Variant = data.get("element", &"neutral")
	if element_var is ElementTypes.Element:
		# Runtime Element object (legacy)
		var elem: ElementTypes.Element = element_var
		var key: StringName = StringName(elem.id)
		config.element_id = ElementRegistry.get_id_from_key(key)
	elif element_var is StringName or element_var is String:
		# StringName key (preferred) or String (legacy)
		var key: StringName = StringName(element_var)
		if ElementRegistry.is_valid_key(key):
			config.element_id = ElementRegistry.get_id_from_key(key)
		else:
			push_warning("SummonerConfig.from_dict: Unknown element key '%s', using NEUTRAL" % key)
			config.element_id = ElementRegistry.ElementId.NEUTRAL
	else:
		config.element_id = ElementRegistry.ElementId.NEUTRAL

	# Base Stats
	config.base_health = data.get("base_health", DEFAULT_BASE_HEALTH)
	config.max_mana = data.get("max_mana", DEFAULT_MAX_MANA)

	# Visual
	config.summoner_icon_path = data.get("summoner_icon_path", "")
	config.card_frame_style = data.get("card_frame_style", "legendary")

	# Unlock
	config.unlock_condition = data.get("unlock_condition", "starting_choice")

	# Traits - from TraitCatalog
	var traits_var: Variant = data.get("innate_trait_ids", [])
	if traits_var is Array:
		var traits_array: Array = traits_var
		for trait_id_var: Variant in traits_array:
			if trait_id_var is String:
				config.innate_trait_ids.append(trait_id_var)

	# Starter card
	config.starter_card_id = data.get("starter_card_id", CardIDs.FIRE_WISP)

	return config

## Convert to dictionary (for saving/debugging)
func to_dict() -> Dictionary:
	return {
		"summoner_id": summoner_id,
		"summoner_name": summoner_name,
		"description": description,
		"element": get_element_key(),
		"base_health": base_health,
		"max_mana": max_mana,
		"summoner_icon_path": summoner_icon_path,
		"card_frame_style": card_frame_style,
		"unlock_condition": unlock_condition,
		"innate_trait_ids": innate_trait_ids,
		"starter_card_id": starter_card_id
	}

## Validation
func is_valid() -> bool:
	if summoner_id.is_empty():
		push_error("SummonerConfig: summoner_id is empty")
		return false
	if summoner_name.is_empty():
		push_error("SummonerConfig: summoner_name is empty for %s" % summoner_id)
		return false
	if base_health <= 0:
		push_error("SummonerConfig: base_health must be positive for %s" % summoner_id)
		return false
	return true
