class_name CampaignIDs

## Campaign ID Constants - Type-Safe Campaign References
##
## Use these constants instead of string literals when referencing campaigns in code.
## This provides compile-time validation and autocomplete support.
##
## Usage:
##   Campaign.SetCurrentCampaign(CampaignIDs.SUMMONERS_PATH)
##   if current_campaign == CampaignIDs.SUMMONERS_PATH:
##       # Load campaign battles
##
## When adding new campaigns:
##   1. Create campaign data file in scripts/data/campaigns/
##   2. Add constant here matching the campaign's "campaign_id" field
##   3. Add to ALL_CAMPAIGNS array
##
## Note: StringName (&"text") is faster than String ("text") for dictionary lookups

# ============================================================================
# CAMPAIGN IDS
# ============================================================================

## Summoner's Path - The main campaign for all summoners
## One campaign, all summoners - different choices lead to different decks
const SUMMONERS_PATH: StringName = &"summoners_path"

## Test Arena - Debug campaign for testing core combat mechanics
const TEST_ARENA: StringName = &"test_arena"

# ============================================================================
# UTILITY
# ============================================================================

## All campaign IDs
const ALL_CAMPAIGNS: Array[StringName] = [
	SUMMONERS_PATH,
	TEST_ARENA,
]

## Default campaign for new players
const DEFAULT: StringName = SUMMONERS_PATH

## Check if a campaign ID is valid
## Accepts String or StringName
static func is_valid(campaign_id: String) -> bool:
	return StringName(campaign_id) in ALL_CAMPAIGNS
