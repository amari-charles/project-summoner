class_name BiomeIDs

## Biome ID Constants - Type-Safe Biome References
##
## Use these constants instead of string literals when referencing biomes in code.
## This provides compile-time validation and autocomplete support.
##
## Usage:
##   var biome = BiomeIDs.SUMMER_PLAINS
##   BattleContext.new(BiomeIDs.SUMMER_PLAINS, ...)
##   var path = BiomeIDs.get_resource_path(BiomeIDs.SUMMER_PLAINS)
##
## When adding new biomes:
##   1. Create .tres file in resources/biomes/ with matching ID
##   2. Add constant here (const BIOME_NAME: StringName = &"biome_id")
##   3. Add to ALL_BIOMES array
##
## Note: StringName (&"text") is faster than String ("text") for dictionary lookups

# ============================================================================
# BIOME IDS
# ============================================================================

## Starting biome - grassy plains environment
const SUMMER_PLAINS: StringName = &"summer_plains"

# ============================================================================
# UTILITY
# ============================================================================

## All available biomes
const ALL_BIOMES: Array[StringName] = [SUMMER_PLAINS]

## Default biome used as fallback
const DEFAULT: StringName = SUMMER_PLAINS

## Get the resource path for a biome
## Returns the path to the biome's .tres configuration file
static func get_resource_path(biome_id: StringName) -> String:
	return "res://resources/biomes/%s.tres" % biome_id

## Check if a biome ID is valid
## Accepts String or StringName
static func is_valid(biome_id: String) -> bool:
	return StringName(biome_id) in ALL_BIOMES
