class_name WinConditionIDs

## Win Condition ID Constants - Type-Safe Battle Objective References
##
## Use these constants instead of string literals when configuring battle objectives.
## This provides compile-time validation and autocomplete support.
##
## Usage:
##   battle_config["win_condition"] = WinConditionIDs.DESTROY_BASE
##   match win_condition:
##       WinConditionIDs.SURVIVE_TIME: _start_survival_timer()
##       WinConditionIDs.TIMED_DESTROY: _start_countdown()
##
## Note: StringName (&"text") is faster than String ("text") for dictionary lookups

# ============================================================================
# WIN CONDITION TYPES
# ============================================================================

## Default: Destroy enemy base to win (no time limit)
const DESTROY_BASE: StringName = &"destroy_base"

## Survive for specified duration without losing your base
## Requires: time_limit (seconds)
const SURVIVE_TIME: StringName = &"survive_time"

## Destroy enemy base within time limit, lose on timeout
## Requires: time_limit (seconds)
const TIMED_DESTROY: StringName = &"timed_destroy"

## Kill specified number of enemy units
## Requires: kill_target (int)
const KILL_COUNT: StringName = &"kill_count"

# ============================================================================
# UTILITY
# ============================================================================

## All win condition types
const ALL_TYPES: Array[StringName] = [DESTROY_BASE, SURVIVE_TIME, TIMED_DESTROY, KILL_COUNT]

## Default win condition used when not specified
const DEFAULT: StringName = DESTROY_BASE

## Check if a win condition type is valid
static func is_valid(condition_type: String) -> bool:
	return StringName(condition_type) in ALL_TYPES

## Check if a win condition has a time limit
static func has_time_limit(condition_type: StringName) -> bool:
	return condition_type in [SURVIVE_TIME, TIMED_DESTROY]

## Check if timeout results in loss (vs win for survive modes)
static func timeout_is_loss(condition_type: StringName) -> bool:
	return condition_type == TIMED_DESTROY

## Check if timeout results in win (survive modes)
static func timeout_is_win(condition_type: StringName) -> bool:
	return condition_type == SURVIVE_TIME
