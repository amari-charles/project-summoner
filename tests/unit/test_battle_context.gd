extends GutTest

## Unit Tests for BattleContext
##
## Tests the battle state machine, configuration, and card tracking.
## Uses init_for_testing() to avoid scene tree dependencies.

var context: Node  # BattleContext instance


func before_each() -> void:
	# Load the BattleContext script and create instance
	var BattleContextScript: GDScript = load("res://scripts/application/battle_context.gd")
	context = BattleContextScript.new()


func after_each() -> void:
	if context:
		context.free()


## =============================================================================
## INITIAL STATE TESTS
## =============================================================================

func test_initial_state_is_none() -> void:
	assert_eq(context.battle_state, context.BattleState.NONE)


func test_initial_mode_is_practice() -> void:
	assert_eq(context.current_mode, context.BattleMode.PRACTICE)


func test_not_configured_initially() -> void:
	assert_false(context.is_configured())
	assert_false(context.was_configured)


func test_battle_config_empty_initially() -> void:
	assert_true(context.battle_config.is_empty())


## =============================================================================
## PRACTICE BATTLE CONFIGURATION TESTS
## =============================================================================

func test_configure_practice_sets_mode() -> void:
	context.configure_practice_battle()

	assert_eq(context.current_mode, context.BattleMode.PRACTICE)


func test_configure_practice_sets_state_to_configured() -> void:
	context.configure_practice_battle()

	assert_eq(context.battle_state, context.BattleState.CONFIGURED)


func test_configure_practice_marks_as_configured() -> void:
	context.configure_practice_battle()

	assert_true(context.was_configured)
	assert_true(context.is_configured())


func test_configure_practice_with_custom_config() -> void:
	var custom_config: Dictionary = {
		"enemy_side": {
			"team": 1,
			"source": "authored",
			"summoner": {
				"source": "authored",
				"hp": 500.0,
				"max_hp": 500.0
			},
			"deck": {
				"source": "authored",
				"cards": [{"catalog_id": "fire_wisp", "count": 3}]
			},
			"controller": {
				"kind": "trainer_ai",
				"ai_type": "aggressive"
			}
		}
	}

	context.configure_practice_battle(custom_config)

	var enemy_side: Dictionary = context.battle_config.get("enemy_side", {})
	assert_eq(enemy_side.get("summoner", {}).get("hp"), 500.0)
	assert_eq(enemy_side.get("controller", {}).get("ai_type"), "aggressive")


func test_configure_practice_uses_defaults_when_empty() -> void:
	context.configure_practice_battle({})

	assert_false(context.battle_config.is_empty())
	assert_true(context.battle_config.has("enemy_side"))
	assert_true(context.battle_config["enemy_side"].has("deck"))


func test_configure_academy_preserves_player_side_deck() -> void:
	var player_cards: Array = [
		{"catalog_id": "neutral_starter_unit", "count": 2},
		{"catalog_id": "magic_bolt", "count": 1}
	]
	var custom_config: Dictionary = {
		"player_side": {
			"team": 0,
			"source": "profile",
			"summoner": {"source": "profile"},
			"deck": {
				"source": "authored",
				"cards": player_cards
			},
			"controller": {"kind": "player"}
		},
		"enemy_side": {
			"team": 1,
			"source": "authored",
			"summoner": {
				"source": "authored",
				"hp": 20.0,
				"max_hp": 20.0
			},
			"deck": {
				"source": "authored",
				"cards": [{"catalog_id": "weak_enemy_unit", "count": 1}]
			},
			"controller": {"kind": "trainer_ai", "ai_type": "none"}
		}
	}

	context.configure_academy_battle("introduction_to_magic_101", "magic_101_spell_practice", custom_config)
	player_cards.clear()

	var player_side: Dictionary = context.battle_config.get("player_side", {})
	var player_deck: Dictionary = player_side.get("deck", {})
	var stored_cards: Array = player_deck.get("cards", [])
	assert_eq(player_deck.get("source"), "authored")
	assert_eq(stored_cards.size(), 2)
	assert_eq(stored_cards[0].get("catalog_id"), "neutral_starter_unit")
	assert_eq(stored_cards[1].get("catalog_id"), "magic_bolt")


## =============================================================================
## STATE MACHINE TESTS
## =============================================================================

func test_start_battle_transitions_to_in_progress() -> void:
	context.configure_practice_battle()

	context.start_battle()

	assert_eq(context.battle_state, context.BattleState.IN_PROGRESS)


func test_start_battle_fails_if_not_configured() -> void:
	# Try to start without configuring first - this emits a warning
	context.start_battle()

	# Should still be NONE since transition was invalid
	assert_eq(context.battle_state, context.BattleState.NONE)
	# Expect the push_warning from invalid state transition
	assert_engine_error("start_battle")


func test_end_battle_victory_transitions_from_in_progress() -> void:
	context.configure_practice_battle()
	context.start_battle()

	context.end_battle_victory()

	assert_eq(context.battle_state, context.BattleState.VICTORY)


func test_end_battle_defeat_transitions_from_in_progress() -> void:
	context.configure_practice_battle()
	context.start_battle()

	context.end_battle_defeat()

	assert_eq(context.battle_state, context.BattleState.DEFEAT)


func test_end_battle_victory_fails_if_not_in_progress() -> void:
	context.configure_practice_battle()
	# Don't call start_battle()

	context.end_battle_victory()

	# Should still be CONFIGURED
	assert_eq(context.battle_state, context.BattleState.CONFIGURED)
	# Expect the push_warning from invalid state transition
	assert_engine_error("end_battle_victory")


func test_end_battle_defeat_fails_if_not_in_progress() -> void:
	context.configure_practice_battle()
	# Don't call start_battle()

	context.end_battle_defeat()

	# Should still be CONFIGURED
	assert_eq(context.battle_state, context.BattleState.CONFIGURED)
	# Expect the push_warning from invalid state transition
	assert_engine_error("end_battle_defeat")


func test_abandon_battle_sets_abandoned_state() -> void:
	context.configure_practice_battle()
	context.start_battle()

	context.abandon_battle()

	assert_eq(context.battle_state, context.BattleState.ABANDONED)


func test_abandon_battle_does_nothing_when_none() -> void:
	# When there's no battle, abandon should do nothing
	context.abandon_battle()

	assert_eq(context.battle_state, context.BattleState.NONE)


## =============================================================================
## CLEAR/RESET TESTS
## =============================================================================

func test_clear_resets_all_state() -> void:
	context.configure_practice_battle()
	context.start_battle()
	context.set_player_summoner_stats({"damage_bonus": 10.0})

	context.clear()

	assert_eq(context.battle_state, context.BattleState.NONE)
	assert_false(context.was_configured)
	assert_true(context.battle_config.is_empty())
	assert_true(context.get_player_summoner_stats().is_empty())


func test_reset_is_alias_for_clear() -> void:
	context.configure_practice_battle()

	context.reset()

	assert_eq(context.battle_state, context.BattleState.NONE)
	assert_false(context.was_configured)


func test_bpa_c01_battle_attempt_id_round_trips_and_clears() -> void:
	context.set_battle_attempt_id("attempt-123")

	assert_eq(context.get_battle_attempt_id(), "attempt-123")

	context.clear()
	assert_eq(context.get_battle_attempt_id(), "")


## =============================================================================
## SUMMONER STATS TESTS
## =============================================================================

func test_set_player_summoner_stats_stores_stats() -> void:
	var stats: Dictionary = {
		"damage_bonus": 15.0,
		"damage_reduction": 5.0,
		"fire_damage_bonus": 20.0
	}

	context.set_player_summoner_stats(stats)

	var stored: Dictionary = context.get_player_summoner_stats()
	assert_eq(stored.get("damage_bonus"), 15.0)
	assert_eq(stored.get("damage_reduction"), 5.0)
	assert_eq(stored.get("fire_damage_bonus"), 20.0)


func test_get_player_summoner_stat_returns_specific_stat() -> void:
	context.set_player_summoner_stats({"damage_bonus": 25.0})

	var bonus: float = context.get_player_summoner_stat("damage_bonus")

	assert_eq(bonus, 25.0)


func test_get_player_summoner_stat_returns_default_for_missing() -> void:
	context.set_player_summoner_stats({})

	var bonus: float = context.get_player_summoner_stat("nonexistent", 99.0)

	assert_eq(bonus, 99.0)


func test_set_player_summoner_stats_duplicates_input() -> void:
	var original: Dictionary = {"damage_bonus": 10.0}
	context.set_player_summoner_stats(original)

	original["damage_bonus"] = 999.0  # Modify original

	# Stored value should be unchanged
	assert_eq(context.get_player_summoner_stat("damage_bonus"), 10.0)


## =============================================================================
## ORIGIN SCENE TESTS
## =============================================================================

func test_get_origin_scene_returns_set_value() -> void:
	context.origin_scene = "res://scenes/custom.tscn"

	assert_eq(context.get_origin_scene(), "res://scenes/custom.tscn")


func test_get_origin_scene_returns_default_when_empty() -> void:
	context.origin_scene = ""

	# Should return campaign map as default
	assert_false(context.get_origin_scene().is_empty())


func test_campaign_battle_origin_uses_legacy_map_for_test_arena() -> void:
	var previous_campaign_id: String = CampaignApi.get_current_campaign_id()
	CampaignApi.set_current_campaign(String(CampaignIDs.TEST_ARENA))

	assert_eq(context._get_campaign_battle_origin_scene(), SceneManager.SCENE_LEGACY_CAMPAIGN_MAP)

	if not previous_campaign_id.is_empty():
		CampaignApi.set_current_campaign(previous_campaign_id)


func test_campaign_battle_origin_uses_academy_hub_for_main_campaign() -> void:
	var previous_campaign_id: String = CampaignApi.get_current_campaign_id()
	CampaignApi.set_current_campaign(String(CampaignIDs.SUMMONERS_PATH))

	assert_eq(context._get_campaign_battle_origin_scene(), SceneManager.SCENE_CAMPAIGN_MAP)

	if not previous_campaign_id.is_empty():
		CampaignApi.set_current_campaign(previous_campaign_id)


## =============================================================================
## MODE CONFIGURATION TESTS
## =============================================================================

func test_configure_arena_sets_mode() -> void:
	# Arena mode emits a "not implemented" warning
	context.configure_arena_battle(1)

	assert_eq(context.current_mode, context.BattleMode.ARENA)
	assert_eq(context.battle_state, context.BattleState.CONFIGURED)
	# Expect the push_warning about not implemented
	assert_engine_error("Arena mode not yet implemented")


func test_configure_endless_sets_mode() -> void:
	# Endless mode emits a "not implemented" warning
	context.configure_endless_wave(1)

	assert_eq(context.current_mode, context.BattleMode.ENDLESS)
	assert_eq(context.battle_state, context.BattleState.CONFIGURED)
	# Expect the push_warning about not implemented
	assert_engine_error("Endless mode not yet implemented")


## =============================================================================
## MULTIPLAYER AUTHORITY TESTS
## =============================================================================

func test_is_multiplayer_battle_false_by_default() -> void:
	context.configure_practice_battle()
	assert_false(context.is_multiplayer_battle())


func test_is_multiplayer_battle_true_when_configured_multiplayer() -> void:
	context.configure_multiplayer_battle(
		"merlin",
		"merriweather",
		_make_multiplayer_deck("fire_wisp"),
		_make_multiplayer_deck("pebbloom"),
		true,
		12345
	)

	assert_true(context.is_multiplayer_battle())


func test_has_authority_true_for_single_player() -> void:
	context.configure_practice_battle()
	assert_true(context.has_authority())


func test_has_authority_uses_is_host_for_multiplayer() -> void:
	context.configure_multiplayer_battle(
		"merlin",
		"merriweather",
		_make_multiplayer_deck("fire_wisp"),
		_make_multiplayer_deck("pebbloom"),
		false,
		12345
	)

	assert_false(context.has_authority())


func test_set_authority_provider_overrides_flags() -> void:
	var provider: MockAuthorityProvider = MockAuthorityProvider.new(false, true, 42)
	context.set_authority_provider(provider)

	assert_true(provider.initialized)
	assert_false(context.has_authority())
	assert_true(context.is_multiplayer_battle())
	assert_eq(context.get_local_peer_id(), 42)

	context.clear()
	assert_true(provider.cleaned_up)


func _make_multiplayer_deck(catalog_id: String) -> Array:
	return [{"catalog_id": catalog_id, "count": 1}]


class MockAuthorityProvider extends RefCounted:
	var _has_authority: bool = false
	var _is_multiplayer: bool = true
	var _peer_id: int = 0
	var initialized: bool = false
	var cleaned_up: bool = false

	func _init(has_authority_flag: bool, is_multiplayer_flag: bool, peer_id: int) -> void:
		_has_authority = has_authority_flag
		_is_multiplayer = is_multiplayer_flag
		_peer_id = peer_id

	func initialize() -> void:
		initialized = true

	func cleanup() -> void:
		cleaned_up = true

	func has_authority() -> bool:
		return _has_authority

	func is_multiplayer() -> bool:
		return _is_multiplayer

	func get_local_peer_id() -> int:
		return _peer_id
