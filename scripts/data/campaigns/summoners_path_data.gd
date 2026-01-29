class_name SummonersPathData
extends RefCounted

## Summoner's Path Campaign Data (Graph Format)
##
## The main campaign for all summoners - one campaign, all summoners.
## Uses a graph structure with nodes (battles/events) and edges (connections).
## Player starts with 1 card and builds their deck through card choice rewards.

static func get_campaign() -> Dictionary:
	return {
		"campaign_id": CampaignIDs.SUMMONERS_PATH,
		"name_key": "campaign.summoners_path.name",
		"description_key": "campaign.summoners_path.description",
		"icon": "",
		"sort_order": 0,
		"start_node": BattleIDs.FIRST_TRIAL,
		"unlock_requirements": [],
		"nodes": _get_nodes(),
		"edges": _get_edges(),
	}


static func _get_nodes() -> Array[Dictionary]:
	return [
		# =======================================================================
		# ACT 1: THE INITIATE'S PATH
		# Player starts with 1 card, builds deck to 4+ cards
		# =======================================================================

		# Battle 1: First Trial - Learn basics with 1 card
		{
			"id": BattleIDs.FIRST_TRIAL,
			"type": NodeTypeIDs.BATTLE,
			"position": Vector2(100, 300),
			"data": {
				"biome_id": BiomeIDs.SUMMER_PLAINS,
				"name_key": "campaign.battle.first_trial.name",
				"description_key": "campaign.battle.first_trial.description",
				"difficulty": 1,
				"is_tutorial": true,
				"requires_deck": true,
				"reward_type": RewardTypeIDs.FLEXIBLE,
				"reward_options": [CardIDs.CHARGE, CardIDs.FIRE_WISP, CardIDs.PUFF],
				"player_selects": true,
				"gold_reward": 30,
				"enemy_deck": [
					{"catalog_id": CardIDs.FIRE_WISP, "count": 1},
				],
				"enemy_hp": 30.0,
			},
		},

		# Battle 2: Second Challenge - Test 2-card combos
		{
			"id": BattleIDs.SECOND_CHALLENGE,
			"type": NodeTypeIDs.BATTLE,
			"position": Vector2(250, 300),
			"data": {
				"biome_id": BiomeIDs.SUMMER_PLAINS,
				"name_key": "campaign.battle.second_challenge.name",
				"description_key": "campaign.battle.second_challenge.description",
				"difficulty": 2,
				"is_tutorial": true,
				"requires_deck": true,
				"reward_type": RewardTypeIDs.FLEXIBLE,
				"reward_options": [CardIDs.EARTH_SPRITE, CardIDs.CLOUD_SWARM, CardIDs.PUFF],
				"player_selects": true,
				"gold_reward": 40,
				"enemy_deck": [
					{"catalog_id": CardIDs.PUFF, "count": 2},
					{"catalog_id": CardIDs.EARTH_SPRITE, "count": 1},
				],
				"enemy_hp": 45.0,
			},
		},

		# Event: Caravan - First shop, introduces economy
		{
			"id": BattleIDs.CARAVAN_01,
			"type": NodeTypeIDs.CARAVAN,
			"position": Vector2(400, 300),
			"data": {
				"name_key": "campaign.event.caravan_01.name",
				"description_key": "campaign.event.caravan_01.description",
				"shop_id": "caravan_tutorial",
			},
		},

		# Battle 3: Third Trial - Medium difficulty
		{
			"id": BattleIDs.THIRD_TRIAL,
			"type": NodeTypeIDs.BATTLE,
			"position": Vector2(550, 300),
			"data": {
				"biome_id": BiomeIDs.SUMMER_PLAINS,
				"name_key": "campaign.battle.third_trial.name",
				"description_key": "campaign.battle.third_trial.description",
				"difficulty": 3,
				"requires_deck": true,
				"reward_type": RewardTypeIDs.FLEXIBLE,
				"reward_options": [CardIDs.FIRE_WISP, CardIDs.CHARGE, CardIDs.CLOUD_SWARM],
				"player_selects": true,
				"gold_reward": 50,
				"enemy_deck": [
					{"catalog_id": CardIDs.PUFF, "count": 2},
					{"catalog_id": CardIDs.EARTH_SPRITE, "count": 2},
				],
				"enemy_hp": 60.0,
			},
		},

		# Choice: Elite vs Standard Path
		{
			"id": BattleIDs.PATH_FORK,
			"type": NodeTypeIDs.CHOICE,
			"position": Vector2(700, 300),
			"data": {
				"name_key": "campaign.choice.path_fork.name",
				"description_key": "campaign.choice.path_fork.description",
				"options": [
					{
						"id": "elite",
						"label_key": "campaign.path.elite.label",
						"description_key": "campaign.path.elite.description",
					},
					{
						"id": "standard",
						"label_key": "campaign.path.standard.label",
						"description_key": "campaign.path.standard.description",
					},
				],
			},
		},

		# =======================================================================
		# ELITE PATH - Higher difficulty, better rewards, level caps
		# =======================================================================

		{
			"id": BattleIDs.ELITE_BATTLE_01,
			"type": NodeTypeIDs.ELITE,
			"position": Vector2(850, 200),
			"data": {
				"biome_id": BiomeIDs.SUMMER_PLAINS,
				"name_key": "campaign.battle.elite_01.name",
				"description_key": "campaign.battle.elite_01.description",
				"difficulty": 5,
				"requires_deck": true,
				"level_cap": 3,
				"reward_type": RewardTypeIDs.FLEXIBLE,
				"reward_options": [CardIDs.FIRE_WISP, CardIDs.CHARGE, CardIDs.MANA_BOLT],
				"player_selects": true,
				"gold_reward": 80,
				"enemy_deck": [
					{"catalog_id": CardIDs.FIRE_WISP, "count": 2},
					{"catalog_id": CardIDs.PUFF, "count": 2},
				],
				"enemy_hp": 80.0,
			},
		},

		# =======================================================================
		# STANDARD PATH - Easier, grindable, common rewards
		# =======================================================================

		{
			"id": BattleIDs.STANDARD_BATTLE_01,
			"type": NodeTypeIDs.BATTLE,
			"position": Vector2(850, 400),
			"data": {
				"biome_id": BiomeIDs.SUMMER_PLAINS,
				"name_key": "campaign.battle.standard_01.name",
				"description_key": "campaign.battle.standard_01.description",
				"difficulty": 3,
				"requires_deck": true,
				"reward_type": RewardTypeIDs.FLEXIBLE,
				"reward_options": [CardIDs.PUFF, CardIDs.EARTH_SPRITE, CardIDs.CLOUD_SWARM],
				"player_selects": true,
				"gold_reward": 50,
				"enemy_deck": [
					{"catalog_id": CardIDs.EARTH_SPRITE, "count": 3},
				],
				"enemy_hp": 55.0,
			},
		},

		# =======================================================================
		# PATHS MERGE - Act 1 Boss
		# =======================================================================

		{
			"id": BattleIDs.ACT1_BOSS,
			"type": NodeTypeIDs.BOSS,
			"position": Vector2(1000, 300),
			"data": {
				"biome_id": BiomeIDs.SUMMER_PLAINS,
				"name_key": "campaign.battle.act1_boss.name",
				"description_key": "campaign.battle.act1_boss.description",
				"difficulty": 6,
				"requires_deck": true,
				"reward_type": RewardTypeIDs.FIXED,
				"reward_cards": [
					{"catalog_id": CardIDs.MANA_BOLT, "rarity": RarityIDs.RARE, "count": 1},
				],
				"gold_reward": 100,
				"enemy_deck": [
					{"catalog_id": CardIDs.FIRE_WISP, "count": 2},
					{"catalog_id": CardIDs.EARTH_SPRITE, "count": 2},
					{"catalog_id": CardIDs.PUFF, "count": 2},
				],
				"enemy_hp": 100.0,
			},
		},
	]


static func _get_edges() -> Array[Dictionary]:
	return [
		# Linear progression through early game
		{"from": BattleIDs.FIRST_TRIAL, "to": BattleIDs.SECOND_CHALLENGE},
		{"from": BattleIDs.SECOND_CHALLENGE, "to": BattleIDs.CARAVAN_01},
		{"from": BattleIDs.CARAVAN_01, "to": BattleIDs.THIRD_TRIAL},
		{"from": BattleIDs.THIRD_TRIAL, "to": BattleIDs.PATH_FORK},

		# Branching paths based on player choice
		{"from": BattleIDs.PATH_FORK, "to": BattleIDs.ELITE_BATTLE_01, "condition": {"choice": "elite"}},
		{"from": BattleIDs.PATH_FORK, "to": BattleIDs.STANDARD_BATTLE_01, "condition": {"choice": "standard"}},

		# Both paths lead to Act 1 Boss
		{"from": BattleIDs.ELITE_BATTLE_01, "to": BattleIDs.ACT1_BOSS},
		{"from": BattleIDs.STANDARD_BATTLE_01, "to": BattleIDs.ACT1_BOSS},
	]
