class_name ElementRegistry
extends RefCounted

## ElementRegistry - Single source of truth for element ID mapping
##
## Maps between:
## - ElementId enum (int) - for code
## - StringName keys - for saves/JSON
## - Element objects - for runtime
##
## Pattern ensures:
## - No hardcoded element strings in gameplay code
## - Type-safe enum usage with IDE support
## - Stable string IDs for serialization
## - Single point of conversion with asserts

## Element ID enum - use this in code
enum ElementId {
	NEUTRAL,
	FIRE,
	WATER,
	WIND,
	EARTH,
	LIGHTNING,
	SHADOW,
	POISON,
	LIFE,
	DEATH,
	OCCULTIST,
	HOLY,
	ICE,
	METAL,
	SPIRIT
}

## Enum → StringName mapping (for serialization)
const ID_TO_KEY: Dictionary = {
	ElementId.NEUTRAL:   &"neutral",
	ElementId.FIRE:      &"fire",
	ElementId.WATER:     &"water",
	ElementId.WIND:      &"wind",
	ElementId.EARTH:     &"earth",
	ElementId.LIGHTNING: &"lightning",
	ElementId.SHADOW:    &"shadow",
	ElementId.POISON:    &"poison",
	ElementId.LIFE:      &"life",
	ElementId.DEATH:     &"death",
	ElementId.OCCULTIST: &"occultist",
	ElementId.HOLY:      &"holy",
	ElementId.ICE:       &"ice",
	ElementId.METAL:     &"metal",
	ElementId.SPIRIT:    &"spirit"
}

## StringName → Enum mapping (for deserialization)
const KEY_TO_ID: Dictionary = {
	&"neutral":   ElementId.NEUTRAL,
	&"fire":      ElementId.FIRE,
	&"water":     ElementId.WATER,
	&"wind":      ElementId.WIND,
	&"earth":     ElementId.EARTH,
	&"lightning": ElementId.LIGHTNING,
	&"shadow":    ElementId.SHADOW,
	&"poison":    ElementId.POISON,
	&"life":      ElementId.LIFE,
	&"death":     ElementId.DEATH,
	&"occultist": ElementId.OCCULTIST,
	&"holy":      ElementId.HOLY,
	&"ice":       ElementId.ICE,
	&"metal":     ElementId.METAL,
	&"spirit":    ElementId.SPIRIT
}

## Convert enum ID to StringName key (for saves/JSON)
static func get_key_from_id(id: int) -> StringName:
	assert(ID_TO_KEY.has(id), "Unknown element ID: %s" % id)
	return ID_TO_KEY[id]

## Convert StringName key to enum ID (from saves/JSON)
static func get_id_from_key(key: StringName) -> int:
	assert(KEY_TO_ID.has(key), "Unknown element key: %s" % key)
	return KEY_TO_ID[key]

## Convert enum ID to runtime Element object
static func get_element_from_id(id: int) -> ElementTypes.Element:
	var key: StringName = get_key_from_id(id)
	var element: ElementTypes.Element = ElementTypes.from_string(String(key))
	assert(element != null, "No Element object found for key: %s" % key)
	return element

## Convert StringName key to runtime Element object
static func get_element_from_key(key: StringName) -> ElementTypes.Element:
	var element: ElementTypes.Element = ElementTypes.from_string(String(key))
	assert(element != null, "No Element object found for key: %s" % key)
	return element

## Check if an ID is valid
static func is_valid_id(id: int) -> bool:
	return ID_TO_KEY.has(id)

## Check if a key is valid
static func is_valid_key(key: StringName) -> bool:
	return KEY_TO_ID.has(key)
