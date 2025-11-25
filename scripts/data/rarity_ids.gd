class_name RarityIDs

## Rarity ID Constants - Type-Safe Rarity References
##
## Use these constants instead of string literals when referencing rarities in code.
## This provides compile-time validation and autocomplete support.
##
## Usage:
##   var rarity = RarityIDs.COMMON
##   Collection.grant_card("fireball", RarityIDs.RARE)
##   match card.get("rarity"):
##       RarityIDs.COMMON: return 5
##       RarityIDs.LEGENDARY: return 500
##
## Note: StringName (&"text") is faster than String ("text") for dictionary lookups

# ============================================================================
# RARITY TIERS (lowest to highest)
# ============================================================================

const COMMON: StringName = &"common"
const RARE: StringName = &"rare"
const EPIC: StringName = &"epic"
const LEGENDARY: StringName = &"legendary"

# ============================================================================
# UTILITY
# ============================================================================

## All rarities in order from lowest to highest
const ALL_RARITIES: Array[StringName] = [COMMON, RARE, EPIC, LEGENDARY]

## Get the tier index of a rarity (0 = common, 3 = legendary)
## Returns -1 if rarity is invalid
static func get_tier(rarity: StringName) -> int:
	return ALL_RARITIES.find(rarity)

## Check if a rarity string is valid
static func is_valid(rarity: StringName) -> bool:
	return rarity in ALL_RARITIES
