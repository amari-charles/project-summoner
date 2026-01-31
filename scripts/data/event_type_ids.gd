class_name EventTypeIDs

## Event Type ID Constants - Type-Safe Campaign Event References
##
## Mirrors C# EventType enum in scripts/csharp/Data/Events/EventType.cs
## Keep these in sync when adding new event types.
##
## Use these constants instead of string literals when referencing event types in code.
## This provides compile-time validation and autocomplete support.
##
## Usage:
##   event.event_type = EventTypeIDs.BATTLE
##   match event_type:
##       EventTypeIDs.AFFINITY: _handle_affinity_selection()
##       EventTypeIDs.BATTLE: _start_battle()
##
## Note: StringName (&"text") is faster than String ("text") for dictionary lookups

# ============================================================================
# EVENT TYPES
# ============================================================================

## Standard combat battle event
const BATTLE: StringName = &"battle"

## Elite battle with level caps and higher difficulty
const ELITE: StringName = &"elite"

## Major boss encounter
const BOSS: StringName = &"boss"

## Summoner element affinity selection event (onboarding)
const AFFINITY: StringName = &"affinity"

## First card summon tutorial event (onboarding)
const FIRST_SUMMON: StringName = &"first_summon"

## Traveling merchant shop event
const CARAVAN: StringName = &"caravan"

## Path choice/branching event
const CHOICE: StringName = &"choice"

## Generic onboarding event type
const ONBOARDING: StringName = &"onboarding"

## Rest/heal opportunity
const REST: StringName = &"rest"

## Story/narrative event (non-combat)
const STORY: StringName = &"story"

# ============================================================================
# UTILITY
# ============================================================================

## All event types
const ALL_TYPES: Array[StringName] = [BATTLE, ELITE, BOSS, AFFINITY, FIRST_SUMMON, CARAVAN, CHOICE, ONBOARDING, REST, STORY]

## Default event type used as fallback
const DEFAULT: StringName = BATTLE

## Check if an event type string is valid
## Accepts String or StringName
static func is_valid(event_type: String) -> bool:
	return StringName(event_type) in ALL_TYPES

## Check if an event type is an onboarding event
static func is_onboarding(event_type: StringName) -> bool:
	return event_type in [AFFINITY, FIRST_SUMMON, ONBOARDING]

## Check if an event type requires combat
static func is_combat(event_type: StringName) -> bool:
	return event_type in [BATTLE, ELITE, BOSS]
