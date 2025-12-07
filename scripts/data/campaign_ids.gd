class_name CampaignIDs

## Campaign ID Constants - Type-Safe Campaign References
##
## Use these constants instead of string literals when referencing campaigns in code.
## This provides compile-time validation and autocomplete support.
##
## Usage:
##   Campaign.set_current_campaign(CampaignIDs.ACADEMY_TRIALS)
##   if current_campaign == CampaignIDs.ACADEMY_TRIALS:
##       # Load academy battles
##
## When adding new campaigns:
##   1. Create campaign JSON file in data/campaigns/
##   2. Add constant here matching the campaign's "campaign_id" field
##   3. Add to ALL_CAMPAIGNS array
##
## Note: StringName (&"text") is faster than String ("text") for dictionary lookups

# ============================================================================
# CAMPAIGN IDS
# ============================================================================

## Academy Trials - The introductory campaign for new summoners
const ACADEMY_TRIALS: StringName = &"academy_trials"

## Combat Arena - Debug campaign for testing core combat mechanics
const COMBAT_ARENA: StringName = &"combat_arena"

# ============================================================================
# UTILITY
# ============================================================================

## All campaign IDs
const ALL_CAMPAIGNS: Array[StringName] = [
	ACADEMY_TRIALS,
	COMBAT_ARENA,
]

## Default campaign for new players
const DEFAULT: StringName = ACADEMY_TRIALS

## Check if a campaign ID is valid
## Accepts String or StringName
static func is_valid(campaign_id: String) -> bool:
	return StringName(campaign_id) in ALL_CAMPAIGNS
