class_name NodeTypeIDs

## Node Type ID Constants - Type-Safe Campaign Graph Node References
##
## Use these constants instead of string literals when referencing node types in code.
## This provides compile-time validation and autocomplete support.
##
## Usage:
##   node.type = NodeTypeIDs.BATTLE
##   match node_type:
##       NodeTypeIDs.BATTLE: _handle_battle()
##       NodeTypeIDs.CHOICE: _show_choice_modal()
##
## Note: StringName (&"text") is faster than String ("text") for dictionary lookups

# ============================================================================
# COMBAT NODE TYPES
# ============================================================================

## Standard combat battle
const BATTLE: StringName = &"battle"

## Elite battle with level caps and higher difficulty
const ELITE: StringName = &"elite"

## Major boss encounter
const BOSS: StringName = &"boss"

# ============================================================================
# EVENT NODE TYPES
# ============================================================================

## Path branching decision point
const CHOICE: StringName = &"choice"

## Traveling merchant shop
const CARAVAN: StringName = &"caravan"

## Rest/heal opportunity
const REST: StringName = &"rest"

## Story/narrative event (non-combat)
const STORY: StringName = &"story"

# ============================================================================
# UTILITY
# ============================================================================

## All node types
const ALL_TYPES: Array[StringName] = [BATTLE, ELITE, BOSS, CHOICE, CARAVAN, REST, STORY]

## Combat node types (require battle system)
const COMBAT_TYPES: Array[StringName] = [BATTLE, ELITE, BOSS]

## Event node types (non-combat)
const EVENT_TYPES: Array[StringName] = [CHOICE, CARAVAN, REST, STORY]

## Default node type used as fallback
const DEFAULT: StringName = BATTLE

## Check if a node type string is valid
## Accepts String or StringName
static func is_valid(node_type: String) -> bool:
	return StringName(node_type) in ALL_TYPES

## Check if a node type requires combat
static func is_combat(node_type: StringName) -> bool:
	return node_type in COMBAT_TYPES

## Check if a node type is an event (non-combat)
static func is_event(node_type: StringName) -> bool:
	return node_type in EVENT_TYPES

## Check if a node type represents a choice point
static func is_choice(node_type: StringName) -> bool:
	return node_type == CHOICE
