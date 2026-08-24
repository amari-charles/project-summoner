extends GutTest


func test_exposes_authored_battle_fields() -> void:
	var battle := TypedBattleData.new({
		"name_key": "debug.battle.arena_puff.name",
		"description_key": "debug.battle.arena_puff.description",
		"event_type": "battle",
		"difficulty": 2,
		"requires_deck": false,
		"biome_id": "summer_plains",
		"repeatable": true,
		"summoner_xp_reward": 10,
		"card_xp_reward": 5,
	}, "arena_puff")

	assert_eq(battle.id, "arena_puff")
	assert_eq(battle.event_type, EventTypeIDs.BATTLE)
	assert_eq(battle.difficulty, 2)
	assert_false(battle.requires_deck)
	assert_true(battle.repeatable)
	assert_eq(battle.summoner_xp_reward, 10)
	assert_eq(battle.card_xp_reward, 5)
	assert_true(battle.is_combat())
	assert_true(battle.is_battle())


func test_missing_fields_use_safe_defaults() -> void:
	var battle := TypedBattleData.new({}, "missing")

	assert_true(battle.is_empty())
	assert_eq(battle.difficulty, 0)
	assert_true(battle.requires_deck)
	assert_eq(battle.first_clear_reward_offers, [])
	assert_eq(battle.enemy_hp, 0.0)
