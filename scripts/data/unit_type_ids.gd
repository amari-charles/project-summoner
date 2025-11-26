class_name UnitTypeIDs

## Unit Type ID Constants - Type-Safe Unit Combat Type References
##
## Use these constants instead of string literals when referencing unit types in code.
## This provides compile-time validation and autocomplete support.
##
## Usage:
##   card.unit_type = UnitTypeIDs.MELEE
##   match unit_type:
##       UnitTypeIDs.MELEE: return "res://assets/icons/sword.png"
##       UnitTypeIDs.RANGED: return "res://assets/icons/bow.png"
##
## Note: StringName (&"text") is faster than String ("text") for dictionary lookups

# ============================================================================
# UNIT TYPES
# ============================================================================

## Close combat unit - attacks enemies at close range
const MELEE: StringName = &"melee"

## Ranged combat unit - attacks enemies from a distance
const RANGED: StringName = &"ranged"

## Static defensive structure - does not move
const STRUCTURE: StringName = &"structure"

# ============================================================================
# UTILITY
# ============================================================================

## All unit types
const ALL_TYPES: Array[StringName] = [MELEE, RANGED, STRUCTURE]

## Default unit type used as fallback
const DEFAULT: StringName = MELEE

## Check if a unit type string is valid
## Accepts String or StringName
static func is_valid(unit_type: String) -> bool:
	return StringName(unit_type) in ALL_TYPES

## Check if the unit type is mobile (can move)
static func is_mobile(unit_type: StringName) -> bool:
	return unit_type != STRUCTURE

## Check if the unit type attacks at range
static func is_ranged(unit_type: StringName) -> bool:
	return unit_type == RANGED
