class_name EventTypeIDs

## Event Type ID Constants - Type-Safe Campaign Event References
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

## Summoner element affinity selection event (onboarding)
const AFFINITY: StringName = &"affinity"

## First card summon tutorial event (onboarding)
const FIRST_SUMMON: StringName = &"first_summon"

## Traveling merchant shop event
const CARAVAN: StringName = &"caravan"

## Generic onboarding event type
const ONBOARDING: StringName = &"onboarding"

# ============================================================================
# UTILITY
# ============================================================================

## All event types
const ALL_TYPES: Array[StringName] = [BATTLE, AFFINITY, FIRST_SUMMON, CARAVAN, ONBOARDING]

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
	return event_type == BATTLE
