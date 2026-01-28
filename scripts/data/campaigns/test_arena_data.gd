class_name TestArenaData
extends RefCounted

## Test Arena Campaign Data (Graph Format)
##
## Debug campaign for testing core combat mechanics.
## All battles are standalone and can be accessed in any order.
## Uses CardIDs constants for compile-time validation.

static func get_campaign() -> Dictionary:
	return {
		"campaign_id": CampaignIDs.TEST_ARENA,
		"name_key": "campaign.test_arena.name",
		"description_key": "campaign.test_arena.description",
		"icon": "",
		"sort_order": 99,
		"start_node": BattleIDs.ARENA_EARTH_SPRITE,
		"unlock_requirements": [],
		"nodes": _get_nodes(),
		"edges": [],  # No edges - all nodes are independently accessible
	}


static func _get_nodes() -> Array[Dictionary]:
	return [
		# Arena: Earth Sprite
		{
			"id": BattleIDs.ARENA_EARTH_SPRITE,
			"type": NodeTypeIDs.BATTLE,
			"position": Vector2(100, 100),
			"data": {
				"biome_id": BiomeIDs.SUMMER_PLAINS,
				"name_key": "campaign.battle.arena_earth_sprite.name",
				"description_key": "campaign.battle.arena_earth_sprite.description",
				"difficulty": 1,
				"requires_deck": false,
				"repeatable": true,
				"reward_type": RewardTypeIDs.NONE,
				"dev_player_deck": [
					{"catalog_id": CardIDs.EARTH_SPRITE, "count": 4},
					{"catalog_id": CardIDs.PUFF, "count": 2},
				],
				"enemy_deck": [
					{"catalog_id": CardIDs.FIRE_WISP, "count": 3},
					{"catalog_id": CardIDs.PUFF, "count": 2},
				],
				"enemy_hp": 100.0,
				"ai_type": "heuristic",
			},
		},

		# Arena: Puff
		{
			"id": BattleIDs.ARENA_PUFF,
			"type": NodeTypeIDs.BATTLE,
			"position": Vector2(250, 100),
			"data": {
				"biome_id": BiomeIDs.SUMMER_PLAINS,
				"name_key": "campaign.battle.arena_puff.name",
				"description_key": "campaign.battle.arena_puff.description",
				"difficulty": 1,
				"requires_deck": false,
				"repeatable": true,
				"reward_type": RewardTypeIDs.NONE,
				"dev_player_deck": [
					{"catalog_id": CardIDs.PUFF, "count": 4},
					{"catalog_id": CardIDs.EARTH_SPRITE, "count": 2},
				],
				"enemy_deck": [
					{"catalog_id": CardIDs.FIRE_WISP, "count": 3},
					{"catalog_id": CardIDs.EARTH_SPRITE, "count": 2},
				],
				"enemy_hp": 100.0,
				"ai_type": "heuristic",
			},
		},

		# Arena: Fire Elemental
		{
			"id": BattleIDs.ARENA_FIRE_WISP,
			"type": NodeTypeIDs.BATTLE,
			"position": Vector2(400, 100),
			"data": {
				"biome_id": BiomeIDs.SUMMER_PLAINS,
				"name_key": "campaign.battle.arena_fire_wisp.name",
				"description_key": "campaign.battle.arena_fire_wisp.description",
				"difficulty": 1,
				"requires_deck": false,
				"repeatable": true,
				"reward_type": RewardTypeIDs.NONE,
				"dev_player_deck": [
					{"catalog_id": CardIDs.FIRE_WISP, "count": 2},
					{"catalog_id": CardIDs.FIRE_TITAN, "count": 2},
					{"catalog_id": CardIDs.FIRE_WISP_SWARM, "count": 2},
				],
				"enemy_deck": [
					{"catalog_id": CardIDs.EARTH_SPRITE, "count": 3},
					{"catalog_id": CardIDs.PUFF, "count": 2},
				],
				"enemy_hp": 100.0,
				"ai_type": "heuristic",
			},
		},

		# Arena: Cloud Swarm
		{
			"id": BattleIDs.ARENA_CLOUD_SWARM,
			"type": NodeTypeIDs.BATTLE,
			"position": Vector2(100, 250),
			"data": {
				"biome_id": BiomeIDs.SUMMER_PLAINS,
				"name_key": "campaign.battle.arena_cloud_swarm.name",
				"description_key": "campaign.battle.arena_cloud_swarm.description",
				"difficulty": 1,
				"requires_deck": false,
				"repeatable": true,
				"reward_type": RewardTypeIDs.NONE,
				"dev_player_deck": [
					{"catalog_id": CardIDs.CLOUD_SWARM, "count": 4},
					{"catalog_id": CardIDs.PUFF, "count": 2},
				],
				"enemy_deck": [
					{"catalog_id": CardIDs.FIRE_WISP, "count": 3},
					{"catalog_id": CardIDs.EARTH_SPRITE, "count": 2},
				],
				"enemy_hp": 100.0,
				"ai_type": "heuristic",
			},
		},

		# Arena: Mana Bolt Spell Test
		{
			"id": BattleIDs.ARENA_MANA_BOLT,
			"type": NodeTypeIDs.BATTLE,
			"position": Vector2(250, 250),
			"data": {
				"biome_id": BiomeIDs.SUMMER_PLAINS,
				"name_key": "campaign.battle.arena_mana_bolt.name",
				"description_key": "campaign.battle.arena_mana_bolt.description",
				"difficulty": 1,
				"requires_deck": false,
				"repeatable": true,
				"reward_type": RewardTypeIDs.NONE,
				"dev_player_deck": [
					{"catalog_id": CardIDs.MANA_BOLT, "count": 5},
					{"catalog_id": CardIDs.FIRE_WISP, "count": 3},
				],
				"enemy_deck": [
					{"catalog_id": CardIDs.EARTH_SPRITE, "count": 3},
					{"catalog_id": CardIDs.FIRE_WISP, "count": 2},
				],
				"enemy_hp": 100.0,
				"ai_type": "heuristic",
			},
		},

		# Debug Arena - Testing sandbox
		{
			"id": BattleIDs.DEBUG_ARENA,
			"type": NodeTypeIDs.BATTLE,
			"position": Vector2(400, 250),
			"data": {
				"biome_id": BiomeIDs.SUMMER_PLAINS,
				"name_key": "debug.arena.name",
				"description_key": "debug.arena.description",
				"difficulty": 0,
				"requires_deck": false,
				"repeatable": true,
				"reward_type": RewardTypeIDs.NONE,
				"dev_player_deck": [
					{"catalog_id": CardIDs.FIRE_WISP, "count": 5},
					{"catalog_id": CardIDs.PUFF, "count": 5},
					{"catalog_id": CardIDs.EARTH_SPRITE, "count": 5},
					{"catalog_id": CardIDs.FIRE_TITAN, "count": 5},
					{"catalog_id": CardIDs.FIREBALL, "count": 5},
					{"catalog_id": CardIDs.ROCK, "count": 5},
				],
				"enemy_deck": [],  # No AI spawning
				"enemy_hp": 999999.0,
				"ai_type": "passive",
				"scene_path": "res://scenes/battlefield/dev/debug_arena.tscn",
			},
		},
	]
