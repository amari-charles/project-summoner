class_name HeroRegistry
extends RefCounted

## HeroRegistry - Single source of truth for hero ID mapping
##
## Maps between:
## - HeroId enum (int) - for code
## - StringName keys - for saves/JSON
##
## Ensures no hardcoded hero strings in gameplay code.

## Hero ID enum - use this in code
enum HeroId {
	PYRALIS,        # Fire
	AQUIRA,         # Water
	ZEPHYRION,      # Wind
	TERRAVORN,      # Earth
	SHADOW_INITIATE # Shadow (starter-only)
}

## Enum → StringName mapping (for serialization)
const ID_TO_KEY: Dictionary = {
	HeroId.PYRALIS:         &"hero_fire",
	HeroId.AQUIRA:          &"hero_water",
	HeroId.ZEPHYRION:       &"hero_wind",
	HeroId.TERRAVORN:       &"hero_earth",
	HeroId.SHADOW_INITIATE: &"hero_shadow_initiate"
}

## StringName → Enum mapping (for deserialization)
const KEY_TO_ID: Dictionary = {
	&"hero_fire":            HeroId.PYRALIS,
	&"hero_water":           HeroId.AQUIRA,
	&"hero_wind":            HeroId.ZEPHYRION,
	&"hero_earth":           HeroId.TERRAVORN,
	&"hero_shadow_initiate": HeroId.SHADOW_INITIATE
}

## Convert enum ID to StringName key (for saves/JSON)
static func get_key_from_id(id: int) -> StringName:
	assert(ID_TO_KEY.has(id), "Unknown hero ID: %s" % id)
	return ID_TO_KEY[id]

## Convert StringName key to enum ID (from saves/JSON)
static func get_id_from_key(key: StringName) -> int:
	assert(KEY_TO_ID.has(key), "Unknown hero key: %s" % key)
	return KEY_TO_ID[key]

## Check if an ID is valid
static func is_valid_id(id: int) -> bool:
	return ID_TO_KEY.has(id)

## Check if a key is valid
static func is_valid_key(key: StringName) -> bool:
	return KEY_TO_ID.has(key)
