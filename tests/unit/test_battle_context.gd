extends GutTest

## Unit Tests for BattleContext
##
## Tests the battle state machine, configuration, and card tracking.
## Uses init_for_testing() to avoid scene tree dependencies.

var context: Node  # BattleContext instance


func before_each() -> void:
	# Load the BattleContext script and create instance
	var BattleContextScript: GDScript = load("res://scripts/core/battle_context.gd")
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
		"enemy_deck": [{"catalog_id": "fire_wisp", "count": 3}],
		"enemy_hp": 500.0,
		"ai_type": "aggressive"
	}

	context.configure_practice_battle(custom_config)

	assert_eq(context.battle_config.get("enemy_hp"), 500.0)
	assert_eq(context.battle_config.get("ai_type"), "aggressive")


func test_configure_practice_uses_defaults_when_empty() -> void:
	context.configure_practice_battle({})

	assert_false(context.battle_config.is_empty())
	assert_true(context.battle_config.has("enemy_deck"))


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


func test_abandon_battle_clears_deck_cards() -> void:
	context.configure_practice_battle()
	context.start_battle()
	context.store_deck_card_ids(_make_mock_deck(["card_1", "card_2"]))

	context.abandon_battle()

	assert_eq(context.get_deck_card_ids().size(), 0)


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
	context.store_deck_card_ids(_make_mock_deck(["card_1"]))
	context.set_player_summoner_stats({"damage_bonus": 10.0})

	context.clear()

	assert_eq(context.battle_state, context.BattleState.NONE)
	assert_false(context.was_configured)
	assert_true(context.battle_config.is_empty())
	assert_eq(context.get_deck_card_ids().size(), 0)
	assert_true(context.get_player_summoner_stats().is_empty())


func test_reset_is_alias_for_clear() -> void:
	context.configure_practice_battle()

	context.reset()

	assert_eq(context.battle_state, context.BattleState.NONE)
	assert_false(context.was_configured)


## =============================================================================
## DECK CARD TRACKING TESTS
## =============================================================================

func test_store_deck_card_ids_stores_cards() -> void:
	context.store_deck_card_ids(_make_mock_deck(["card_123"]))

	var cards: Array = context.get_deck_card_ids()
	assert_eq(cards.size(), 1)
	assert_true("card_123" in cards)


func test_store_deck_card_ids_stores_multiple_cards() -> void:
	context.store_deck_card_ids(_make_mock_deck(["card_1", "card_2", "card_3"]))

	var cards: Array = context.get_deck_card_ids()
	assert_eq(cards.size(), 3)


func test_store_deck_card_ids_ignores_empty_ids() -> void:
	var deck: Array = _make_mock_deck(["card_1", "", "card_2"])
	context.store_deck_card_ids(deck)

	var cards: Array = context.get_deck_card_ids()
	assert_eq(cards.size(), 2)


func test_store_deck_card_ids_clears_previous() -> void:
	context.store_deck_card_ids(_make_mock_deck(["old_card"]))
	context.store_deck_card_ids(_make_mock_deck(["new_card"]))

	var cards: Array = context.get_deck_card_ids()
	assert_eq(cards.size(), 1)
	assert_true("new_card" in cards)
	assert_false("old_card" in cards)


func test_get_deck_card_ids_returns_copy() -> void:
	context.store_deck_card_ids(_make_mock_deck(["card_1"]))

	var cards: Array = context.get_deck_card_ids()
	cards.append("card_2")  # Modify returned array

	# Original should be unchanged
	assert_eq(context.get_deck_card_ids().size(), 1)


## Helper to create mock card objects with instance_id
func _make_mock_deck(instance_ids: Array) -> Array:
	var deck: Array = []
	for id: String in instance_ids:
		var mock_card: MockCard = MockCard.new()
		mock_card.instance_id = id
		deck.append(mock_card)
	return deck


## Simple mock card class for testing
class MockCard extends RefCounted:
	var instance_id: String = ""


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
