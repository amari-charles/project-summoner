class_name RewardTypeIDs

## Reward Type ID Constants - Type-Safe Battle Reward References
##
## Use these constants instead of string literals when referencing reward types in code.
## This provides compile-time validation and autocomplete support.
##
## Usage:
##   battle.reward_type = RewardTypeIDs.FIXED
##   match reward_type:
##       RewardTypeIDs.FIXED: _grant_fixed_reward()
##       RewardTypeIDs.CHOICE: _show_choice_ui()
##
## Note: StringName (&"text") is faster than String ("text") for dictionary lookups

# ============================================================================
# REWARD TYPES
# ============================================================================

## Fixed reward - player receives a specific predetermined card
const FIXED: StringName = &"fixed"

## Random reward - player receives a randomly selected card
const RANDOM: StringName = &"random"

## Choice reward - player picks one card from multiple options
const CHOICE: StringName = &"choice"

## No reward - battle has no card reward (e.g., shop events)
const NONE: StringName = &"none"

# ============================================================================
# UTILITY
# ============================================================================

## All reward types
const ALL_TYPES: Array[StringName] = [FIXED, RANDOM, CHOICE, NONE]

## Default reward type used as fallback
const DEFAULT: StringName = FIXED

## Check if a reward type string is valid
## Accepts String or StringName
static func is_valid(reward_type: String) -> bool:
	return StringName(reward_type) in ALL_TYPES

## Check if the reward type requires player selection
static func requires_selection(reward_type: StringName) -> bool:
	return reward_type == CHOICE

## Check if the reward type provides a card
static func has_card_reward(reward_type: StringName) -> bool:
	return reward_type != NONE
