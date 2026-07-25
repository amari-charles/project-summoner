class_name BattleIDs

## Battle/Node ID Constants - Type-Safe Campaign Node References
##
## Use these constants instead of string literals when referencing nodes in code.
## This provides compile-time validation and autocomplete support.
##
## Usage:
##   CampaignApi.start_battle(BattleIDs.FIRST_TRIAL)
##   if current_node == BattleIDs.PATH_FORK:
##       # Show path choice
##
## When adding new nodes:
##   1. Add node definition to campaign data file in scripts/infrastructure/data/campaigns/
##   2. Add constant here matching the node's "id" field
##   3. Add to appropriate category array (ACT1_NODES, TEST_ARENA_NODES, etc.)
##
## Note: StringName (&"text") is faster than String ("text") for dictionary lookups

# ============================================================================
# ACT 1: THE INITIATE'S PATH
# ============================================================================

## Battle: First Trial - Learn basics with 1 card
const FIRST_TRIAL: StringName = &"first_trial"

## Battle: Second Challenge - Test 2-card combos
const SECOND_CHALLENGE: StringName = &"second_challenge"

## Event: Caravan - First shop visit
const CARAVAN_01: StringName = &"caravan_01"

## Battle: Third Trial - Medium difficulty
const THIRD_TRIAL: StringName = &"third_trial"

## Choice: Elite vs Standard Path Fork
const PATH_FORK: StringName = &"path_fork"

## Battle: Elite Path Battle 1 - Higher difficulty with level cap
const ELITE_BATTLE_01: StringName = &"elite_battle_01"

## Battle: Standard Path Battle 1 - Normal difficulty
const STANDARD_BATTLE_01: StringName = &"standard_battle_01"

## Boss: Act 1 Boss - First major boss
const ACT1_BOSS: StringName = &"act1_boss"

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
const ALL_ACT1_NODES: Array[StringName] = [
	FIRST_TRIAL,
	SECOND_CHALLENGE,
	CARAVAN_01,
	THIRD_TRIAL,
	PATH_FORK,
	ELITE_BATTLE_01,
	STANDARD_BATTLE_01,
	ACT1_BOSS,
]

## All tutorial battle IDs (battles with is_tutorial: true)
const ALL_TUTORIALS: Array[StringName] = [
	FIRST_TRIAL,
	SECOND_CHALLENGE,
]

## All test arena battle IDs
const ALL_TEST_ARENA_NODES: Array[StringName] = [
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

## All battle IDs (campaign + test arena)
static func all_ids() -> Array[StringName]:
	var result: Array[StringName] = []
	result.append_array(ALL_ACT1_NODES)
	result.append_array(ALL_TEST_ARENA_NODES)
	return result

## Check if a battle ID is valid
## Accepts String or StringName
static func is_valid(battle_id: String) -> bool:
	return StringName(battle_id) in all_ids()

## Check if a battle ID is a tutorial battle
## Accepts String or StringName
static func is_tutorial(battle_id: String) -> bool:
	return StringName(battle_id) in ALL_TUTORIALS

## Check if a battle ID is a test arena battle
## Accepts String or StringName
static func is_test_arena(battle_id: String) -> bool:
	return StringName(battle_id) in ALL_TEST_ARENA_NODES
