class_name SummonerRegistry
extends RefCounted

## SummonerRegistry - Single source of truth for summoner ID mapping
##
## Maps between:
## - SummonerId enum (int) - for code
## - StringName keys - for saves/JSON
##
## Ensures no hardcoded summoner strings in gameplay code.

## Summoner ID enum - use this in code
enum SummonerId {
	COLE,    # Fire Fateforger
	CELINE,  # Water Fateforger
	MEI,     # Wind Fateforger
	TEO      # Earth Fateforger
}

## Enum → StringName mapping (for serialization)
const ID_TO_KEY: Dictionary = {
	SummonerId.COLE:   SummonerIDs.COLE,
	SummonerId.CELINE: SummonerIDs.CELINE,
	SummonerId.MEI:    SummonerIDs.MEI,
	SummonerId.TEO:    SummonerIDs.TEO
}

## StringName → Enum mapping (for deserialization)
const KEY_TO_ID: Dictionary = {
	SummonerIDs.COLE:   SummonerId.COLE,
	SummonerIDs.CELINE: SummonerId.CELINE,
	SummonerIDs.MEI:    SummonerId.MEI,
	SummonerIDs.TEO:    SummonerId.TEO
}

## Convert enum ID to StringName key (for saves/JSON)
static func get_key_from_id(id: int) -> StringName:
	assert(ID_TO_KEY.has(id), "Unknown summoner ID: %s" % id)
	return ID_TO_KEY[id]

## Convert StringName key to enum ID (from saves/JSON)
static func get_id_from_key(key: StringName) -> int:
	assert(KEY_TO_ID.has(key), "Unknown summoner key: %s" % key)
	return KEY_TO_ID[key]

## Check if an ID is valid
static func is_valid_id(id: int) -> bool:
	return ID_TO_KEY.has(id)

## Check if a key is valid
static func is_valid_key(key: StringName) -> bool:
	return KEY_TO_ID.has(key)
