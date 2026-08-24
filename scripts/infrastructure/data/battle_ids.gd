class_name BattleIDs

## Stable IDs for direct authored debug battles.
##
## Use these constants instead of string literals at GDScript boundaries.
##
## Usage:
##   ProgressionAuthority.StartBattleAttempt(BattleIDs.ARENA_EARTH_SPRITE)
##
## When adding new nodes:
##   1. Add node definition to authored battle catalog in scripts/csharp/Infrastructure/Data/Events/
##   2. Add constant here matching the node's "id" field
##   3. Add it to ALL_DEBUG_BATTLES.
##
## Note: StringName (&"text") is faster than String ("text") for dictionary lookups

# ============================================================================
# TEST ARENA (Debug/test battles with fixed decks)
# ============================================================================

## Test Arena: Earth Sprite Test
const ARENA_EARTH_SPRITE: StringName = &"arena_earth_sprite"

## Test Arena: Puff Test
const ARENA_PUFF: StringName = &"arena_puff"

## Test Arena: Fire Elemental Test
const ARENA_FIRE_WISP: StringName = &"arena_fire_wisp"

## Test Arena: Cloud Swarm Test
const ARENA_CLOUD_SWARM: StringName = &"arena_cloud_swarm"

## Test Arena: Debug Arena - Testing sandbox with infinite mana/HP
const DEBUG_ARENA: StringName = &"debug_arena"

## Test Arena: Mana Bolt Spell Test
const ARENA_MANA_BOLT: StringName = &"arena_mana_bolt"

## Test Arena: Wind/Earth New Card Set (+ Fire Wisp reference)
const ARENA_WIND_EARTH_NEW_CARDS: StringName = &"arena_wind_earth_new_cards"

## Test Arena: All active Fire/Water/Earth/Wind units
const ARENA_ALL_UNITS: StringName = &"arena_all_units"

## Test Arena: All active Fire/Water/Earth/Wind cards
const ARENA_ALL_CARDS: StringName = &"arena_all_cards"

## Test Arena: All active Fire/Water/Earth/Wind spells with a small real-art unit set
const ARENA_ALL_SPELLS: StringName = &"arena_all_spells"

## Test Arena: Debug battle using only summon cards with production sprite scenes
const ARENA_SPRITE_UNITS: StringName = &"arena_sprite_units"

# ============================================================================
# UTILITY ARRAYS
# ============================================================================

## All Act 1 node IDs (Summoner's Path)
## All direct debug battle IDs.
const ALL_DEBUG_BATTLES: Array[StringName] = [
	ARENA_EARTH_SPRITE,
	ARENA_PUFF,
	ARENA_FIRE_WISP,
	ARENA_CLOUD_SWARM,
	DEBUG_ARENA,
	ARENA_MANA_BOLT,
	ARENA_WIND_EARTH_NEW_CARDS,
	ARENA_ALL_UNITS,
	ARENA_ALL_CARDS,
	ARENA_ALL_SPELLS,
	ARENA_SPRITE_UNITS,
]

## All battle IDs (debug arena)
static func all_ids() -> Array[StringName]:
	return ALL_DEBUG_BATTLES.duplicate()

## Check if a battle ID is valid
## Accepts String or StringName
static func is_valid(battle_id: String) -> bool:
	return StringName(battle_id) in all_ids()

## Check if a battle ID is a test arena battle
## Accepts String or StringName
static func is_test_arena(battle_id: String) -> bool:
	return StringName(battle_id) in ALL_DEBUG_BATTLES
