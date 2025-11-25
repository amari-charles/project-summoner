class_name ModifierRegistry
extends RefCounted

## ModifierRegistry - Single source of truth for modifier ID mapping
##
## Maps between:
## - ModifierId enum (int) - for code
## - StringName keys - for saves/JSON
##
## Ensures no hardcoded modifier strings in gameplay code.

## Modifier ID enum - use this in code
enum ModifierId {
	FORTUNE_FAVORS_BOLD,
	# Add more modifiers here as they're created
}

## Enum → StringName mapping (for serialization)
const ID_TO_KEY: Dictionary = {
	ModifierId.FORTUNE_FAVORS_BOLD: &"fortune_favors_bold",
}

## StringName → Enum mapping (for deserialization)
const KEY_TO_ID: Dictionary = {
	&"fortune_favors_bold": ModifierId.FORTUNE_FAVORS_BOLD,
}

## Convert enum ID to StringName key (for saves/JSON)
static func get_key_from_id(id: int) -> StringName:
	assert(ID_TO_KEY.has(id), "Unknown modifier ID: %s" % id)
	return ID_TO_KEY[id]

## Convert StringName key to enum ID (from saves/JSON)
static func get_id_from_key(key: StringName) -> int:
	assert(KEY_TO_ID.has(key), "Unknown modifier key: %s" % key)
	return KEY_TO_ID[key]

## Check if an ID is valid
static func is_valid_id(id: int) -> bool:
	return ID_TO_KEY.has(id)

## Check if a key is valid
static func is_valid_key(key: StringName) -> bool:
	return KEY_TO_ID.has(key)
