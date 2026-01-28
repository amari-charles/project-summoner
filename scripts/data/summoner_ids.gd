class_name SummonerIDs

## Summoner ID Constants - Type-Safe Summoner References
##
## Use these constants instead of string literals when referencing summoners in code.
## This provides compile-time validation and autocomplete support.
##
## Usage:
##   SummonerSelection.set_active_summoner(SummonerIDs.COLE)
##   if current_summoner == SummonerIDs.SELENE:
##       # Water-specific logic
##
## When adding new summoners:
##   1. Add summoner definition to SummonerCatalog._init_catalog()
##   2. Add constant here matching the summoner's "summoner_id" field
##   3. Add to appropriate category array (STARTING, etc.)
##
## Note: StringName (&"text") is faster than String ("text") for dictionary lookups

# ============================================================================
# STARTING SUMMONERS (Core 4 Fateforgers - Always available at start)
# ============================================================================

## Cole - Fire Fateforger. Arrogant, competitive, cocky.
const COLE: StringName = &"summoner_cole"

## Selene - Water Fateforger. Gentle, caring, emotionally intelligent.
const SELENE: StringName = &"summoner_selene"

## Mei - Wind Fateforger. Elusive, self-interested, loner.
const MEI: StringName = &"summoner_mei"

## Teo - Earth Fateforger. Gym rat, straightforward, reliable.
const TEO: StringName = &"summoner_teo"

# ============================================================================
# DEV/TEST SUMMONERS
# ============================================================================

## Mana Test Summoner - High mana pool for testing
const MANA_TEST: StringName = &"summoner_mana_test"

# ============================================================================
# UTILITY ARRAYS
# ============================================================================

## Core starting summoners (always available in selection)
const ALL_STARTING: Array[StringName] = [
	COLE,
	SELENE,
	MEI,
	TEO,
]

## Dev/test summoners (not available to players)
const ALL_DEV: Array[StringName] = [
	MANA_TEST,
]

## All summoner IDs
static func all_ids() -> Array[StringName]:
	var result: Array[StringName] = []
	result.append_array(ALL_STARTING)
	result.append_array(ALL_DEV)
	return result

## Check if a summoner ID is valid
## Accepts String or StringName
static func is_valid(summoner_id: String) -> bool:
	return StringName(summoner_id) in all_ids()

## Check if a summoner ID is a starting summoner
## Accepts String or StringName
static func is_starting(summoner_id: String) -> bool:
	return StringName(summoner_id) in ALL_STARTING

## Check if a summoner ID is a dev/test summoner
## Accepts String or StringName
static func is_dev(summoner_id: String) -> bool:
	return StringName(summoner_id) in ALL_DEV

## Default summoner ID (used for fallbacks)
const DEFAULT: StringName = COLE
