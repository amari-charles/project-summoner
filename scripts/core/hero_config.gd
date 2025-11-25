extends Resource
class_name HeroConfig

## HeroConfig - Typed configuration for a hero
##
## Replaces dictionary-based hero data with typed class.
## Includes base stats and modifier system for extensibility.
## Can be created from JSON/Dictionary data (data-driven).

## Default stat values (used for @export defaults and from_dict() fallbacks)
const DEFAULT_BASE_HEALTH: float = 1000.0
const DEFAULT_MAX_MANA: float = 10.0
const DEFAULT_MANA_REGEN: float = 1.0

## Identity
@export var hero_id: String = ""
@export var hero_name: String = ""
@export var description: String = ""
@export_enum("NEUTRAL", "FIRE", "WATER", "WIND", "EARTH", "LIGHTNING", "SHADOW", "POISON", "LIFE", "DEATH", "OCCULTIST", "HOLY", "ICE", "METAL", "SPIRIT")
var element_id: int = ElementRegistry.ElementId.NEUTRAL

## Base Stats (before modifiers)
@export var base_health: float = DEFAULT_BASE_HEALTH
@export var max_mana: float = DEFAULT_MAX_MANA
@export var mana_regen: float = DEFAULT_MANA_REGEN

## Visual
@export var hero_icon_path: String = ""
@export var card_frame_style: String = "legendary"

## Unlock
@export var unlock_condition: String = "starting_choice"  ## "starting_choice", "random_starter_only", "unlock_after_battle"

## Modifier System
@export var innate_modifier_ids: Array[int] = []   ## Modifiers this hero starts with (enum IDs)

## Level-up choices (Post-MVP)
var level_choice_modifier_ids_by_level: Dictionary = {}   ## Key: int (level), Value: PackedStringArray (modifier IDs)

## Get the Element object for this hero (runtime)
func get_element() -> ElementTypes.Element:
	return ElementRegistry.get_element_from_id(element_id)

## Get the element key for serialization
func get_element_key() -> StringName:
	return ElementRegistry.get_key_from_id(element_id)

## Create from dictionary (for data-driven config from JSON)
static func from_dict(data: Dictionary) -> HeroConfig:
	var config: HeroConfig = HeroConfig.new()

	# Identity
	config.hero_id = data.get("hero_id", "")
	config.hero_name = data.get("hero_name", "")
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
			push_warning("HeroConfig.from_dict: Unknown element key '%s', using NEUTRAL" % key)
			config.element_id = ElementRegistry.ElementId.NEUTRAL
	else:
		config.element_id = ElementRegistry.ElementId.NEUTRAL

	# Base Stats
	config.base_health = data.get("base_health", DEFAULT_BASE_HEALTH)
	config.max_mana = data.get("max_mana", DEFAULT_MAX_MANA)
	config.mana_regen = data.get("mana_regen", DEFAULT_MANA_REGEN)

	# Visual
	config.hero_icon_path = data.get("hero_icon_path", "")
	config.card_frame_style = data.get("card_frame_style", "legendary")

	# Unlock
	config.unlock_condition = data.get("unlock_condition", "starting_choice")

	# Modifiers - handle both StringName keys and int enum IDs
	var innate_var: Variant = data.get("innate_modifier_ids", [])
	if innate_var is Array:
		var innate_array: Array = innate_var
		for mod_id_var: Variant in innate_array:
			if mod_id_var is int:
				# Already an enum ID
				config.innate_modifier_ids.append(mod_id_var)
			elif mod_id_var is StringName or mod_id_var is String:
				# StringName key - convert to enum
				var key: StringName = StringName(mod_id_var)
				if ModifierRegistry.is_valid_key(key):
					var id: int = ModifierRegistry.get_id_from_key(key)
					config.innate_modifier_ids.append(id)
				else:
					push_warning("HeroConfig.from_dict: Unknown modifier key '%s'" % key)

	# Level choices (Post-MVP)
	var level_choices_var: Variant = data.get("level_choice_modifier_ids_by_level", {})
	if level_choices_var is Dictionary:
		config.level_choice_modifier_ids_by_level = level_choices_var

	return config

## Convert to dictionary (for saving/debugging)
func to_dict() -> Dictionary:
	# Convert modifier enum IDs to StringName keys for serialization
	var modifier_keys: Array = []
	for mod_id: int in innate_modifier_ids:
		modifier_keys.append(ModifierRegistry.get_key_from_id(mod_id))

	return {
		"hero_id": hero_id,
		"hero_name": hero_name,
		"description": description,
		"element": get_element_key(),  ## Store as StringName key for serialization
		"base_health": base_health,
		"max_mana": max_mana,
		"mana_regen": mana_regen,
		"hero_icon_path": hero_icon_path,
		"card_frame_style": card_frame_style,
		"unlock_condition": unlock_condition,
		"innate_modifier_ids": modifier_keys,  ## Store as StringName keys
		"level_choice_modifier_ids_by_level": level_choice_modifier_ids_by_level
	}

## Validation
func is_valid() -> bool:
	if hero_id.is_empty():
		push_error("HeroConfig: hero_id is empty")
		return false
	if hero_name.is_empty():
		push_error("HeroConfig: hero_name is empty for %s" % hero_id)
		return false
	if base_health <= 0:
		push_error("HeroConfig: base_health must be positive for %s" % hero_id)
		return false
	return true
