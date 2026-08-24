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
const DEFAULT_BASE_CAST_SPEED: float = 1.0
const DEFAULT_PORTRAIT_UV_OFFSET: Vector2 = Vector2(0.2, 0.05)
const DEFAULT_PORTRAIT_UV_SCALE: Vector2 = Vector2(0.6, 0.45)

## Identity
@export var summoner_id: String = ""
@export var summoner_name: String = ""
@export var description: String = ""
@export_enum("NEUTRAL", "FIRE", "WATER", "WIND", "EARTH", "LIGHTNING", "SHADOW", "POISON", "LIFE", "DEATH", "OCCULTIST", "HOLY", "ICE", "METAL", "SPIRIT")
var element_id: int = ElementRegistry.ElementId.NEUTRAL

## Base Stats (before traits)
@export var base_health: float = DEFAULT_BASE_HEALTH
@export var max_mana: float = DEFAULT_MAX_MANA
@export var base_cast_speed: float = DEFAULT_BASE_CAST_SPEED  ## Multiplier for summon time (higher = faster)

## Visual
@export var summoner_icon_path: String = ""
@export var card_frame_style: String = "legendary"
@export var portrait_uv_offset: Vector2 = DEFAULT_PORTRAIT_UV_OFFSET
@export var portrait_uv_scale: Vector2 = DEFAULT_PORTRAIT_UV_SCALE

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

## Create from the canonical C# SummonerCatalogBridge dictionary format:
##   name_key / description_key  (localization keys, translated via Loc.t())
##   element_id                  (int matching ElementRegistry.ElementId)
static func from_dict(data: Dictionary) -> SummonerConfig:
	var config: SummonerConfig = SummonerConfig.new()

	# Identity
	config.summoner_id = data.get("summoner_id", "")
	var name_key: String = SafeTypeUtils.string(data.get("name_key", ""), "")
	config.summoner_name = Loc.t(name_key) if not name_key.is_empty() else ""
	var description_key: String = SafeTypeUtils.string(data.get("description_key", ""), "")
	config.description = Loc.t(description_key) if not description_key.is_empty() else ""
	config.element_id = SafeTypeUtils.int_val(
		data.get("element_id"),
		ElementRegistry.ElementId.NEUTRAL
	)

	# Base Stats
	config.base_health = data.get("base_health", DEFAULT_BASE_HEALTH)
	config.max_mana = data.get("max_mana", DEFAULT_MAX_MANA)
	config.base_cast_speed = data.get("base_cast_speed", DEFAULT_BASE_CAST_SPEED)

	# Visual
	config.summoner_icon_path = data.get("summoner_icon_path", "")
	config.card_frame_style = data.get("card_frame_style", "legendary")
	config.portrait_uv_offset = SafeTypeUtils.vector2(data.get("portrait_uv_offset", DEFAULT_PORTRAIT_UV_OFFSET), DEFAULT_PORTRAIT_UV_OFFSET)
	config.portrait_uv_scale = SafeTypeUtils.vector2(data.get("portrait_uv_scale", DEFAULT_PORTRAIT_UV_SCALE), DEFAULT_PORTRAIT_UV_SCALE)

	# Unlock
	config.unlock_condition = data.get("unlock_condition", "starting_choice")

	# Traits - from TraitCatalog
	var traits_var: Variant = data.get("innate_trait_ids", [])
	if traits_var is Array:
		var traits_array: Array = traits_var
		for trait_id_var: Variant in traits_array:
			var trait_id: String = SafeTypeUtils.string(trait_id_var, "")
			if not trait_id.is_empty():
				config.innate_trait_ids.append(trait_id)

	# Starter card
	config.starter_card_id = data.get("starter_card_id", CardIDs.FIRE_WISP)

	return config
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
