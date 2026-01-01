class_name OnboardingData
extends RefCounted

## Onboarding Campaign Data
##
## Account-wide tutorial completed once for all summoners.
## Uses CardIDs constants for compile-time validation.

static func get_campaign() -> Dictionary:
	return {
		"campaign_id": CampaignIDs.ONBOARDING,
		"name_key": "campaign.onboarding.name",
		"description_key": "campaign.onboarding.description",
		"icon": "",
		"sort_order": 0,
		"is_shared": true,
		"unlock_requirements": [],
		"battles": _get_battles(),
	}


static func _get_battles() -> Array[Dictionary]:
	return [
		# Event: Summoner affinity selection
		{
			"id": BattleIDs.EVENT_AFFINITY,
			"biome_id": "",
			"name_key": "campaign.event.affinity.name",
			"description_key": "campaign.event.affinity.description",
			"difficulty": 0,
			"event_type": EventTypeIDs.AFFINITY,
			"requires_deck": false,
			"repeatable": false,
			"reward_type": RewardTypeIDs.FIXED,
			"reward_cards": [],
			"enemy_deck": [],
			"unlock_requirements": [],
		},
		# Event: First summon card selection
		{
			"id": BattleIDs.EVENT_FIRST_SUMMON,
			"biome_id": "",
			"name_key": "campaign.event.first_summon.name",
			"description_key": "campaign.event.first_summon.description",
			"difficulty": 0,
			"event_type": EventTypeIDs.FIRST_SUMMON,
			"requires_deck": false,
			"repeatable": false,
			"reward_type": RewardTypeIDs.FIXED,
			"reward_cards": [],
			"enemy_deck": [],
			"unlock_requirements": [BattleIDs.EVENT_AFFINITY],
		},
		# Tutorial Battle: First Trial
		{
			"id": BattleIDs.FIRST_TRIAL,
			"biome_id": BiomeIDs.SUMMER_PLAINS,
			"name_key": "campaign.battle.first_trial.name",
			"description_key": "campaign.battle.first_trial.description",
			"difficulty": 1,
			"event_type": EventTypeIDs.BATTLE,
			"requires_deck": true,
			"repeatable": false,
			"is_tutorial": true,
			"reward_type": RewardTypeIDs.FIXED,
			"reward_cards": [
				{"catalog_id": CardIDs.CHARGE, "rarity": RarityIDs.COMMON, "count": 1},
			],
			"gold_reward": 30,
			"card_xp_reward": 15,
			"summoner_xp_reward": 50,
			"enemy_deck": [
				{"catalog_id": CardIDs.EARTH_SPRITE, "count": 1},
			],
			"enemy_hp": 30.0,
			"unlock_requirements": [BattleIDs.EVENT_FIRST_SUMMON],
			"event_sequence": "res://resources/sequences/first_trial_tutorial.tres",
			"ai_type": "scripted",
			"ai_script": [],
		},
		# Tutorial Battle: Charge Tutorial
		{
			"id": BattleIDs.CHARGE_TUTORIAL,
			"biome_id": BiomeIDs.SUMMER_PLAINS,
			"name_key": "campaign.battle.charge_tutorial.name",
			"description_key": "campaign.battle.charge_tutorial.description",
			"difficulty": 1,
			"event_type": EventTypeIDs.BATTLE,
			"requires_deck": true,
			"repeatable": false,
			"is_tutorial": true,
			"reward_type": RewardTypeIDs.FIXED,
			"reward_cards": [
				{"catalog_id": CardIDs.FIRE_ELEMENTAL, "rarity": RarityIDs.COMMON, "count": 1},
				{"catalog_id": CardIDs.PUFF, "rarity": RarityIDs.COMMON, "count": 1},
			],
			"gold_reward": 40,
			"card_xp_reward": 15,
			"summoner_xp_reward": 75,
			"enemy_deck": [
				{"catalog_id": CardIDs.PUFF, "count": 2},
				{"catalog_id": CardIDs.EARTH_SPRITE, "count": 2},
			],
			"enemy_hp": 50.0,
			"unlock_requirements": [BattleIDs.FIRST_TRIAL],
			"event_sequence": "res://resources/sequences/charge_tutorial.tres",
			"ai_type": "scripted",
			"ai_script": [],
		},
		# Event: Caravan Tutorial
		{
			"id": BattleIDs.EVENT_CARAVAN_TUTORIAL,
			"biome_id": "",
			"name_key": "campaign.event.caravan_tutorial.name",
			"description_key": "campaign.event.caravan_tutorial.description",
			"difficulty": 1,
			"event_type": EventTypeIDs.CARAVAN,
			"requires_deck": false,
			"repeatable": false,
			"reward_type": RewardTypeIDs.NONE,
			"reward_cards": [],
			"gold_reward": 0,
			"unlock_requirements": [BattleIDs.CHARGE_TUTORIAL],
			"shop_id": "caravan_tutorial",
			"event_sequence": "res://resources/sequences/caravan_tutorial.tres",
		},
	]
