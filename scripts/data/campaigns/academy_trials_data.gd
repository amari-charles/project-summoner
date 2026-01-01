class_name AcademyTrialsData
extends RefCounted

## Academy Trials Campaign Data
##
## The main campaign for new summoners (per-summoner progress).
## Uses CardIDs constants for compile-time validation.

static func get_campaign() -> Dictionary:
	return {
		"campaign_id": CampaignIDs.ACADEMY_TRIALS,
		"name_key": "campaign.academy_trials.name",
		"description_key": "campaign.academy_trials.description",
		"icon": "",
		"sort_order": 1,
		"is_shared": false,
		"unlock_requirements": [CampaignIDs.ONBOARDING],
		"battles": _get_battles(),
	}


static func _get_battles() -> Array[Dictionary]:
	# Academy Trials battles will be added as the campaign develops
	return []
