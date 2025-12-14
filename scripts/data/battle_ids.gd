class_name BattleIDs

## Battle ID Constants - Type-Safe Battle/Event References
##
## Use these constants instead of string literals when referencing battles in code.
## This provides compile-time validation and autocomplete support.
##
## Usage:
##   Campaign.start_battle(BattleIDs.FIRST_TRIAL)
##   if current_battle == BattleIDs.EVENT_AFFINITY:
##       # Show summoner selection
##
## When adding new battles:
##   1. Add battle definition to CampaignService._init_battles()
##   2. Add constant here matching the battle's "id" field
##   3. Add to appropriate category array (EVENTS, TUTORIALS, CAMPAIGN_BATTLES)
##
## Note: StringName (&"text") is faster than String ("text") for dictionary lookups

# ============================================================================
# ONBOARDING EVENTS (Non-combat story/selection events)
# ============================================================================

## Onboarding Event 1: Summoner affinity selection
const EVENT_AFFINITY: StringName = &"event_affinity"

## Onboarding Event 2: First summon card selection
const EVENT_FIRST_SUMMON: StringName = &"event_first_summon"

## Caravan Event: Mr. Merriweather's Trading Post
const EVENT_CARAVAN_TUTORIAL: StringName = &"event_caravan_tutorial"

# ============================================================================
# TUTORIAL BATTLES (Combat with guidance)
# ============================================================================

## Tutorial Battle 0: The First Trial - Basic combat introduction
const FIRST_TRIAL: StringName = &"first_trial"

## Tutorial: Charge Card Introduction
const CHARGE_TUTORIAL: StringName = &"charge_tutorial"

# ============================================================================
# COMBAT ARENA (Debug/test battles with fixed decks)
# ============================================================================

## Arena Battle: Fire Elemental Test - Fire elemental variants
const ARENA_FIRE_ELEMENTAL: StringName = &"arena_fire_elemental"

## Arena Battle 1: Melee Basics - Slime vs Slime
const ARENA_MELEE_BASICS: StringName = &"arena_melee_basics"

## Arena Battle 2: Ranged Intro - Fire units with ranged attackers
const ARENA_RANGED_INTRO: StringName = &"arena_ranged_intro"

## Arena Battle 3: Mixed Tactics - Combined melee and ranged
const ARENA_MIXED_TACTICS: StringName = &"arena_mixed_tactics"

## Arena Battle 4: Full Combat - All unit types
const ARENA_FULL_COMBAT: StringName = &"arena_full_combat"

# ============================================================================
# UTILITY ARRAYS
# ============================================================================

## All event (non-combat) IDs
const ALL_EVENTS: Array[StringName] = [
	EVENT_AFFINITY,
	EVENT_FIRST_SUMMON,
	EVENT_CARAVAN_TUTORIAL,
]

## All tutorial battle IDs
const ALL_TUTORIALS: Array[StringName] = [
	FIRST_TRIAL,
	CHARGE_TUTORIAL,
]

## All campaign battle IDs
const ALL_CAMPAIGN_BATTLES: Array[StringName] = [
	ARENA_FIRE_ELEMENTAL,
	ARENA_MELEE_BASICS,
	ARENA_RANGED_INTRO,
	ARENA_MIXED_TACTICS,
	ARENA_FULL_COMBAT,
]

## All battle IDs (events + tutorials + campaign)
static func all_ids() -> Array[StringName]:
	var result: Array[StringName] = []
	result.append_array(ALL_EVENTS)
	result.append_array(ALL_TUTORIALS)
	result.append_array(ALL_CAMPAIGN_BATTLES)
	return result

## Check if a battle ID is valid
## Accepts String or StringName
static func is_valid(battle_id: String) -> bool:
	return StringName(battle_id) in all_ids()

## Check if a battle ID is an event (non-combat)
## Accepts String or StringName
static func is_event(battle_id: String) -> bool:
	return StringName(battle_id) in ALL_EVENTS

## Check if a battle ID is a tutorial battle
## Accepts String or StringName
static func is_tutorial(battle_id: String) -> bool:
	return StringName(battle_id) in ALL_TUTORIALS
