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

## Onboarding - Account-wide tutorial completed once for all summoners
const ONBOARDING: StringName = &"onboarding"

## Academy Trials - The main campaign for new summoners (per-summoner progress)
const ACADEMY_TRIALS: StringName = &"academy_trials"

## Combat Arena - Debug campaign for testing core combat mechanics
const COMBAT_ARENA: StringName = &"combat_arena"

# ============================================================================
# SHARED CAMPAIGNS
# ============================================================================

## Campaigns with shared (account-wide) progress
const SHARED_CAMPAIGNS: Array[StringName] = [
	ONBOARDING,
]

# ============================================================================
# UTILITY
# ============================================================================

## All campaign IDs
const ALL_CAMPAIGNS: Array[StringName] = [
	ONBOARDING,
	ACADEMY_TRIALS,
	COMBAT_ARENA,
]

## Default campaign for new players
const DEFAULT: StringName = ONBOARDING

## Check if a campaign ID is valid
## Accepts String or StringName
static func is_valid(campaign_id: String) -> bool:
	return StringName(campaign_id) in ALL_CAMPAIGNS

## Check if a campaign uses shared (account-wide) progress
## Accepts String or StringName
static func is_shared_campaign(campaign_id: String) -> bool:
	return StringName(campaign_id) in SHARED_CAMPAIGNS
