class_name RewardTypeIDs

## Reward Type ID Constants - Type-Safe Battle Reward References
##
## Use these constants instead of string literals when referencing reward types in code.
## This provides compile-time validation and autocomplete support.
##
## Three reward types:
## - FIXED: Grant specific predetermined cards (tutorials, scripted rewards)
## - FLEXIBLE: Dynamic generation with player selection (main gameplay)
## - NONE: No card reward (shop events, etc.)
##
## Usage:
##   battle.reward_type = RewardTypeIDs.FIXED
##   match reward_type:
##       RewardTypeIDs.FIXED: _grant_fixed_reward()
##       RewardTypeIDs.FLEXIBLE: _show_flexible_ui()
##
## Note: StringName (&"text") is faster than String ("text") for dictionary lookups

# ============================================================================
# REWARD TYPES
# ============================================================================

## Fixed reward - player receives specific predetermined cards
## Used for tutorials and scripted rewards where exact cards are required
const FIXED: StringName = &"fixed"

## No reward - battle has no card reward (e.g., shop events)
const NONE: StringName = &"none"

## Flexible reward - player picks from options
##
## Config options (use ONE of these):
## - reward_pool: int - RewardPoolId enum value (see C# RewardPoolId.cs)
## - reward_filters: Dictionary - Inline filters {element, rarity, card_type}
## - reward_options: Array[CardIDs] - Explicit card choices
##
## Additional options:
## - draw_count: int - Number of cards to draw from pool (default: 3)
## - exclude_owned: bool - Exclude cards player already owns
## - unique_options: bool - Ensure no duplicate cards in options
## - player_selects: bool - If false, auto-grant first option
const FLEXIBLE: StringName = &"flexible"

# ============================================================================
# UTILITY
# ============================================================================

## All reward types
const ALL_TYPES: Array[StringName] = [FIXED, NONE, FLEXIBLE]

## Default reward type used as fallback
const DEFAULT: StringName = FIXED

## Check if a reward type string is valid
## Accepts String or StringName
static func is_valid(reward_type: String) -> bool:
	return StringName(reward_type) in ALL_TYPES

## Check if the reward type requires player selection
## FLEXIBLE requires selection by default (unless player_selects: false in config)
static func requires_selection(reward_type: StringName) -> bool:
	return reward_type == FLEXIBLE

## Check if the reward type provides a card
static func has_card_reward(reward_type: StringName) -> bool:
	return reward_type != NONE

## Check if the reward type uses flexible reward generation
static func is_flexible(reward_type: StringName) -> bool:
	return reward_type == FLEXIBLE
