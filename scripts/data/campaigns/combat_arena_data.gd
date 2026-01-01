class_name CombatArenaData
extends RefCounted

## Combat Arena Campaign Data
##
## Debug campaign for testing core combat mechanics.
## Uses CardIDs constants for compile-time validation.

static func get_campaign() -> Dictionary:
	return {
		"campaign_id": CampaignIDs.COMBAT_ARENA,
		"name_key": "campaign.combat_arena.name",
		"description_key": "campaign.combat_arena.description",
		"icon": "",
		"sort_order": 99,
		"is_shared": false,
		"unlock_requirements": [],
		"battles": _get_battles(),
	}


static func _get_battles() -> Array[Dictionary]:
	return [
		# Arena: Earth Sprite
		{
			"id": BattleIDs.ARENA_EARTH_SPRITE,
			"biome_id": BiomeIDs.SUMMER_PLAINS,
			"name_key": "campaign.battle.arena_earth_sprite.name",
			"description_key": "campaign.battle.arena_earth_sprite.description",
			"difficulty": 1,
			"event_type": EventTypeIDs.BATTLE,
			"requires_deck": false,
			"repeatable": true,
			"reward_type": RewardTypeIDs.NONE,
			"reward_cards": [],
			"gold_reward": 0,
			"card_xp_reward": 0,
			"summoner_xp_reward": 0,
			"dev_player_deck": [
				{"catalog_id": CardIDs.EARTH_SPRITE, "count": 4},
				{"catalog_id": CardIDs.PUFF, "count": 2},
			],
			"enemy_deck": [
				{"catalog_id": CardIDs.FIRE_ELEMENTAL, "count": 3},
				{"catalog_id": CardIDs.PUFF, "count": 2},
			],
			"enemy_hp": 100.0,
			"unlock_requirements": [],
			"ai_type": "heuristic",
		},
		# Arena: Puff
		{
			"id": BattleIDs.ARENA_PUFF,
			"biome_id": BiomeIDs.SUMMER_PLAINS,
			"name_key": "campaign.battle.arena_puff.name",
			"description_key": "campaign.battle.arena_puff.description",
			"difficulty": 1,
			"event_type": EventTypeIDs.BATTLE,
			"requires_deck": false,
			"repeatable": true,
			"reward_type": RewardTypeIDs.NONE,
			"reward_cards": [],
			"gold_reward": 0,
			"card_xp_reward": 0,
			"summoner_xp_reward": 0,
			"dev_player_deck": [
				{"catalog_id": CardIDs.PUFF, "count": 4},
				{"catalog_id": CardIDs.EARTH_SPRITE, "count": 2},
			],
			"enemy_deck": [
				{"catalog_id": CardIDs.FIRE_ELEMENTAL, "count": 3},
				{"catalog_id": CardIDs.EARTH_SPRITE, "count": 2},
			],
			"enemy_hp": 100.0,
			"unlock_requirements": [],
			"ai_type": "heuristic",
		},
		# Arena: Fire Elemental
		{
			"id": BattleIDs.ARENA_FIRE_ELEMENTAL,
			"biome_id": BiomeIDs.SUMMER_PLAINS,
			"name_key": "campaign.battle.arena_fire_elemental.name",
			"description_key": "campaign.battle.arena_fire_elemental.description",
			"difficulty": 1,
			"event_type": EventTypeIDs.BATTLE,
			"requires_deck": false,
			"repeatable": true,
			"reward_type": RewardTypeIDs.NONE,
			"reward_cards": [],
			"gold_reward": 0,
			"card_xp_reward": 0,
			"summoner_xp_reward": 0,
			"dev_player_deck": [
				{"catalog_id": CardIDs.FIRE_ELEMENTAL, "count": 2},
				{"catalog_id": CardIDs.FIRE_TITAN, "count": 2},
				{"catalog_id": CardIDs.FIRE_ELEMENTAL_SWARM, "count": 2},
			],
			"enemy_deck": [
				{"catalog_id": CardIDs.EARTH_SPRITE, "count": 3},
				{"catalog_id": CardIDs.PUFF, "count": 2},
			],
			"enemy_hp": 100.0,
			"unlock_requirements": [],
			"ai_type": "heuristic",
		},
		# Arena: Melee Basics
		{
			"id": BattleIDs.ARENA_MELEE_BASICS,
			"biome_id": BiomeIDs.SUMMER_PLAINS,
			"name_key": "campaign.battle.arena_melee_basics.name",
			"description_key": "campaign.battle.arena_melee_basics.description",
			"difficulty": 1,
			"event_type": EventTypeIDs.BATTLE,
			"requires_deck": false,
			"repeatable": true,
			"reward_type": RewardTypeIDs.NONE,
			"reward_cards": [],
			"gold_reward": 0,
			"card_xp_reward": 0,
			"summoner_xp_reward": 0,
			"dev_player_deck": [
				{"catalog_id": CardIDs.EARTH_SPRITE, "count": 4},
				{"catalog_id": CardIDs.FIRE_ELEMENTAL, "count": 2},
			],
			"enemy_deck": [
				{"catalog_id": CardIDs.EARTH_SPRITE, "count": 3},
				{"catalog_id": CardIDs.FIRE_ELEMENTAL, "count": 2},
			],
			"enemy_hp": 100.0,
			"unlock_requirements": [],
			"ai_type": "heuristic",
		},
		# Arena: Ranged Intro
		{
			"id": BattleIDs.ARENA_RANGED_INTRO,
			"biome_id": BiomeIDs.SUMMER_PLAINS,
			"name_key": "campaign.battle.arena_ranged_intro.name",
			"description_key": "campaign.battle.arena_ranged_intro.description",
			"difficulty": 2,
			"event_type": EventTypeIDs.BATTLE,
			"requires_deck": false,
			"repeatable": true,
			"reward_type": RewardTypeIDs.NONE,
			"reward_cards": [],
			"gold_reward": 0,
			"card_xp_reward": 0,
			"summoner_xp_reward": 0,
			"dev_player_deck": [
				{"catalog_id": CardIDs.FIRE_ELEMENTAL, "count": 3},
				{"catalog_id": CardIDs.PUFF, "count": 3},
			],
			"enemy_deck": [
				{"catalog_id": CardIDs.FIRE_ELEMENTAL, "count": 3},
				{"catalog_id": CardIDs.PUFF, "count": 2},
			],
			"enemy_hp": 150.0,
			"unlock_requirements": [BattleIDs.ARENA_MELEE_BASICS],
			"ai_type": "heuristic",
		},
		# Arena: Mixed Tactics
		{
			"id": BattleIDs.ARENA_MIXED_TACTICS,
			"biome_id": BiomeIDs.SUMMER_PLAINS,
			"name_key": "campaign.battle.arena_mixed_tactics.name",
			"description_key": "campaign.battle.arena_mixed_tactics.description",
			"difficulty": 3,
			"event_type": EventTypeIDs.BATTLE,
			"requires_deck": false,
			"repeatable": true,
			"reward_type": RewardTypeIDs.NONE,
			"reward_cards": [],
			"gold_reward": 0,
			"card_xp_reward": 0,
			"summoner_xp_reward": 0,
			"dev_player_deck": [
				{"catalog_id": CardIDs.FIRE_ELEMENTAL, "count": 2},
				{"catalog_id": CardIDs.PUFF, "count": 2},
				{"catalog_id": CardIDs.EARTH_SPRITE, "count": 2},
			],
			"enemy_deck": [
				{"catalog_id": CardIDs.FIRE_ELEMENTAL, "count": 2},
				{"catalog_id": CardIDs.PUFF, "count": 2},
				{"catalog_id": CardIDs.EARTH_SPRITE, "count": 2},
			],
			"enemy_hp": 200.0,
			"unlock_requirements": [BattleIDs.ARENA_RANGED_INTRO],
			"ai_type": "heuristic",
		},
		# Arena: Full Combat
		{
			"id": BattleIDs.ARENA_FULL_COMBAT,
			"biome_id": BiomeIDs.SUMMER_PLAINS,
			"name_key": "campaign.battle.arena_full_combat.name",
			"description_key": "campaign.battle.arena_full_combat.description",
			"difficulty": 4,
			"event_type": EventTypeIDs.BATTLE,
			"requires_deck": false,
			"repeatable": true,
			"reward_type": RewardTypeIDs.NONE,
			"reward_cards": [],
			"gold_reward": 0,
			"card_xp_reward": 0,
			"summoner_xp_reward": 0,
			"dev_player_deck": [
				{"catalog_id": CardIDs.FIRE_ELEMENTAL, "count": 2},
				{"catalog_id": CardIDs.PUFF, "count": 2},
				{"catalog_id": CardIDs.EARTH_SPRITE, "count": 1},
				{"catalog_id": CardIDs.FIRE_TITAN, "count": 1},
			],
			"enemy_deck": [
				{"catalog_id": CardIDs.FIRE_ELEMENTAL, "count": 2},
				{"catalog_id": CardIDs.PUFF, "count": 2},
				{"catalog_id": CardIDs.EARTH_SPRITE, "count": 1},
				{"catalog_id": CardIDs.FIRE_TITAN, "count": 1},
			],
			"enemy_hp": 250.0,
			"unlock_requirements": [BattleIDs.ARENA_MIXED_TACTICS],
			"ai_type": "heuristic",
		},
	]
