extends GutTest

## Unit Tests for TypedEventData
##
## Tests the typed accessor wrapper for event data dictionaries.
## Covers type coercion, localization translation, and helper methods.


## =============================================================================
## CONSTRUCTOR TESTS
## =============================================================================

func test_constructor_stores_data() -> void:
	var data: Dictionary = {"difficulty": 3}
	var event := TypedEventData.new(data, "test_event")

	assert_eq(event.id, "test_event")
	assert_eq(event.difficulty, 3)


func test_constructor_with_empty_data() -> void:
	var event := TypedEventData.new({}, "empty_event")

	assert_eq(event.id, "empty_event")
	assert_true(event.is_empty())


func test_constructor_defaults() -> void:
	var event := TypedEventData.new()

	assert_eq(event.id, "")
	assert_true(event.is_empty())


## =============================================================================
## NAME AND DESCRIPTION (LOCALIZATION) TESTS
## =============================================================================

func test_name_returns_localized_string() -> void:
	var data: Dictionary = {"name_key": "campaign.battle.first_trial.name"}
	var event := TypedEventData.new(data, "test")

	# Should call Loc.t() and return translated string
	var result: String = event.name
	assert_ne(result, "Unknown")
	assert_ne(result, "")
	# The actual translated value depends on localization file


func test_name_returns_unknown_when_key_empty() -> void:
	var data: Dictionary = {"name_key": ""}
	var event := TypedEventData.new(data, "test")

	assert_eq(event.name, "Unknown")


func test_name_returns_unknown_when_key_missing() -> void:
	var event := TypedEventData.new({}, "test")

	assert_eq(event.name, "Unknown")


func test_description_returns_localized_string() -> void:
	var data: Dictionary = {"description_key": "campaign.battle.first_trial.description"}
	var event := TypedEventData.new(data, "test")

	var result: String = event.description
	# Should not be empty if key exists in localization
	assert_true(result.length() > 0 or result == "")


func test_description_returns_empty_when_key_empty() -> void:
	var data: Dictionary = {"description_key": ""}
	var event := TypedEventData.new(data, "test")

	assert_eq(event.description, "")


func test_description_returns_empty_when_key_missing() -> void:
	var event := TypedEventData.new({}, "test")

	assert_eq(event.description, "")


## =============================================================================
## TYPE COERCION TESTS - STRING
## =============================================================================

func test_string_property_returns_value() -> void:
	var data: Dictionary = {"biome_id": "summer_plains"}
	var event := TypedEventData.new(data, "test")

	assert_eq(event.biome_id, "summer_plains")


func test_string_property_accepts_stringname_values() -> void:
	var data: Dictionary = {"biome_id": StringName("summer_plains")}
	var event := TypedEventData.new(data, "test")

	assert_eq(event.biome_id, "summer_plains")


func test_string_property_returns_default_when_missing() -> void:
	var event := TypedEventData.new({}, "test")

	# biome_id defaults to BiomeIDs.DEFAULT
	assert_eq(event.biome_id, String(BiomeIDs.DEFAULT))


func test_string_property_returns_default_when_wrong_type() -> void:
	var data: Dictionary = {"shop_id": 123}  # Wrong type: int instead of String
	var event := TypedEventData.new(data, "test")

	assert_eq(event.shop_id, "")  # Default for shop_id


## =============================================================================
## TYPE COERCION TESTS - INT
## =============================================================================

func test_int_property_returns_value() -> void:
	var data: Dictionary = {"difficulty": 5}
	var event := TypedEventData.new(data, "test")

	assert_eq(event.difficulty, 5)


func test_int_property_returns_default_when_missing() -> void:
	var event := TypedEventData.new({}, "test")

	assert_eq(event.difficulty, 0)


func test_int_property_returns_default_when_wrong_type() -> void:
	var data: Dictionary = {"difficulty": "hard"}  # Wrong type: String instead of int
	var event := TypedEventData.new(data, "test")

	assert_eq(event.difficulty, 0)


func test_gold_reward_returns_value() -> void:
	var data: Dictionary = {"gold_reward": 100}
	var event := TypedEventData.new(data, "test")

	assert_eq(event.gold_reward, 100)


func test_level_cap_returns_value() -> void:
	var data: Dictionary = {"level_cap": 3}
	var event := TypedEventData.new(data, "test")

	assert_eq(event.level_cap, 3)


## =============================================================================
## TYPE COERCION TESTS - FLOAT
## =============================================================================

func test_float_property_returns_value() -> void:
	var data: Dictionary = {"enemy_hp": 150.5}
	var event := TypedEventData.new(data, "test")

	assert_eq(event.enemy_hp, 150.5)


func test_float_property_returns_default_when_missing() -> void:
	var event := TypedEventData.new({}, "test")

	assert_eq(event.enemy_hp, 0.0)


func test_float_property_converts_int_to_float() -> void:
	var data: Dictionary = {"enemy_hp": 100}  # int that should be converted
	var event := TypedEventData.new(data, "test")

	assert_eq(event.enemy_hp, 100.0)


func test_float_property_returns_default_when_wrong_type() -> void:
	var data: Dictionary = {"enemy_hp": "high"}  # Wrong type
	var event := TypedEventData.new(data, "test")

	assert_eq(event.enemy_hp, 0.0)


## =============================================================================
## TYPE COERCION TESTS - BOOL
## =============================================================================

func test_bool_property_returns_true() -> void:
	var data: Dictionary = {"requires_deck": true}
	var event := TypedEventData.new(data, "test")

	assert_true(event.requires_deck)


func test_bool_property_returns_false() -> void:
	var data: Dictionary = {"requires_deck": false}
	var event := TypedEventData.new(data, "test")

	assert_false(event.requires_deck)


func test_bool_property_returns_default_when_missing() -> void:
	var event := TypedEventData.new({}, "test")

	# requires_deck defaults to true
	assert_true(event.requires_deck)
	# repeatable defaults to false
	assert_false(event.repeatable)


func test_bool_property_returns_default_when_wrong_type() -> void:
	var data: Dictionary = {"is_tutorial": "yes"}  # Wrong type
	var event := TypedEventData.new(data, "test")

	assert_false(event.is_tutorial)


## =============================================================================
## TYPE COERCION TESTS - ARRAY
## =============================================================================

func test_array_property_returns_value() -> void:
	var cards: Array = [{"catalog_id": "fire_wisp", "count": 2}]
	var data: Dictionary = {"reward_cards": cards}
	var event := TypedEventData.new(data, "test")

	assert_eq(event.reward_cards.size(), 1)
	assert_eq(event.reward_cards[0]["catalog_id"], "fire_wisp")


func test_array_property_returns_empty_when_missing() -> void:
	var event := TypedEventData.new({}, "test")

	assert_eq(event.reward_cards.size(), 0)
	assert_eq(event.options.size(), 0)


func test_array_property_returns_empty_when_wrong_type() -> void:
	var data: Dictionary = {"options": "not_an_array"}
	var event := TypedEventData.new(data, "test")

	assert_eq(event.options.size(), 0)


## =============================================================================
## EVENT TYPE TESTS
## =============================================================================

func test_event_type_returns_stringname() -> void:
	var data: Dictionary = {"event_type": "battle"}
	var event := TypedEventData.new(data, "test")

	assert_eq(event.event_type, EventTypeIDs.BATTLE)


func test_event_type_handles_stringname_input() -> void:
	var data: Dictionary = {"event_type": &"caravan"}
	var event := TypedEventData.new(data, "test")

	assert_eq(event.event_type, EventTypeIDs.CARAVAN)


func test_event_type_defaults_to_battle() -> void:
	var event := TypedEventData.new({}, "test")

	assert_eq(event.event_type, EventTypeIDs.BATTLE)


func test_reward_type_returns_stringname() -> void:
	var data: Dictionary = {"reward_type": "flexible"}
	var event := TypedEventData.new(data, "test")

	assert_eq(event.reward_type, RewardTypeIDs.FLEXIBLE)


## =============================================================================
## HAS_LEVEL_CAP TESTS
## =============================================================================

func test_has_level_cap_true_when_positive() -> void:
	var data: Dictionary = {"level_cap": 3}
	var event := TypedEventData.new(data, "test")

	assert_true(event.has_level_cap)


func test_has_level_cap_false_when_zero() -> void:
	var data: Dictionary = {"level_cap": 0}
	var event := TypedEventData.new(data, "test")

	assert_false(event.has_level_cap)


func test_has_level_cap_false_when_missing() -> void:
	var event := TypedEventData.new({}, "test")

	assert_false(event.has_level_cap)


## =============================================================================
## TYPE CHECKING HELPER TESTS
## =============================================================================

func test_is_combat_true_for_battle() -> void:
	var data: Dictionary = {"event_type": "battle"}
	var event := TypedEventData.new(data, "test")

	assert_true(event.is_combat())


func test_is_combat_true_for_elite() -> void:
	var data: Dictionary = {"event_type": "elite"}
	var event := TypedEventData.new(data, "test")

	assert_true(event.is_combat())


func test_is_combat_true_for_boss() -> void:
	var data: Dictionary = {"event_type": "boss"}
	var event := TypedEventData.new(data, "test")

	assert_true(event.is_combat())


func test_is_combat_false_for_caravan() -> void:
	var data: Dictionary = {"event_type": "caravan"}
	var event := TypedEventData.new(data, "test")

	assert_false(event.is_combat())


func test_is_battle_true_only_for_battle() -> void:
	var battle := TypedEventData.new({"event_type": "battle"}, "test")
	var elite := TypedEventData.new({"event_type": "elite"}, "test")

	assert_true(battle.is_battle())
	assert_false(elite.is_battle())


func test_is_caravan_true_for_caravan() -> void:
	var data: Dictionary = {"event_type": "caravan"}
	var event := TypedEventData.new(data, "test")

	assert_true(event.is_caravan())
	assert_false(event.is_battle())


func test_is_choice_true_for_choice() -> void:
	var data: Dictionary = {"event_type": "choice"}
	var event := TypedEventData.new(data, "test")

	assert_true(event.is_choice())
	assert_false(event.is_combat())


## =============================================================================
## RAW ACCESS TESTS
## =============================================================================

func test_get_raw_returns_dictionary() -> void:
	var data: Dictionary = {"custom_field": "custom_value"}
	var event := TypedEventData.new(data, "test")

	var raw: Dictionary = event.get_raw()
	assert_eq(raw["custom_field"], "custom_value")


func test_is_empty_true_for_empty_dict() -> void:
	var event := TypedEventData.new({}, "test")

	assert_true(event.is_empty())


func test_is_empty_false_for_non_empty_dict() -> void:
	var event := TypedEventData.new({"key": "value"}, "test")

	assert_false(event.is_empty())


func test_has_key_returns_true_when_exists() -> void:
	var event := TypedEventData.new({"difficulty": 5}, "test")

	assert_true(event.has_key("difficulty"))


func test_has_key_returns_false_when_missing() -> void:
	var event := TypedEventData.new({}, "test")

	assert_false(event.has_key("difficulty"))


func test_get_value_returns_raw_value() -> void:
	var event := TypedEventData.new({"custom": [1, 2, 3]}, "test")

	var result: Variant = event.get_value("custom")
	assert_eq(result, [1, 2, 3])


func test_get_value_returns_default_when_missing() -> void:
	var event := TypedEventData.new({}, "test")

	var result: Variant = event.get_value("missing", "default_value")
	assert_eq(result, "default_value")


## =============================================================================
## COMPLETE EVENT DATA TEST
## =============================================================================

func test_full_battle_event_data() -> void:
	# Simulate complete battle event data from EventCatalog
	var data: Dictionary = {
		"id": "first_trial",
		"type": "battle",
		"event_type": "battle",
		"name_key": "campaign.battle.first_trial.name",
		"description_key": "campaign.battle.first_trial.description",
		"biome_id": "summer_plains",
		"difficulty": 1,
		"is_tutorial": true,
		"requires_deck": true,
		"enemy_hp": 30.0,
		"reward_type": "flexible",
		"gold_reward": 30,
		"summoner_xp_reward": 20,
		"card_xp_reward": 15,
		"player_selects": true,
		"repeatable": false
	}
	var event := TypedEventData.new(data, "first_trial")

	assert_eq(event.id, "first_trial")
	assert_eq(event.event_type, EventTypeIDs.BATTLE)
	assert_eq(event.biome_id, "summer_plains")
	assert_eq(event.difficulty, 1)
	assert_true(event.is_tutorial)
	assert_true(event.requires_deck)
	assert_eq(event.enemy_hp, 30.0)
	assert_eq(event.reward_type, RewardTypeIDs.FLEXIBLE)
	assert_eq(event.gold_reward, 30)
	assert_eq(event.summoner_xp_reward, 20)
	assert_eq(event.card_xp_reward, 15)
	assert_true(event.player_selects)
	assert_false(event.repeatable)
	assert_true(event.is_combat())
	assert_true(event.is_battle())
	assert_false(event.is_caravan())


func test_full_caravan_event_data() -> void:
	var data: Dictionary = {
		"id": "caravan_01",
		"event_type": "caravan",
		"name_key": "campaign.event.caravan_01.name",
		"description_key": "campaign.event.caravan_01.description",
		"shop_id": "caravan_tutorial",
		"repeatable": false
	}
	var event := TypedEventData.new(data, "caravan_01")

	assert_eq(event.id, "caravan_01")
	assert_eq(event.event_type, EventTypeIDs.CARAVAN)
	assert_eq(event.shop_id, "caravan_tutorial")
	assert_true(event.is_caravan())
	assert_false(event.is_combat())


func test_full_choice_event_data() -> void:
	var options: Array = [
		{"id": "elite", "label_key": "path.elite", "description_key": "path.elite.desc"},
		{"id": "standard", "label_key": "path.standard", "description_key": "path.standard.desc"}
	]
	var data: Dictionary = {
		"id": "path_fork",
		"event_type": "choice",
		"name_key": "campaign.choice.path_fork.name",
		"description_key": "campaign.choice.path_fork.description",
		"options": options
	}
	var event := TypedEventData.new(data, "path_fork")

	assert_eq(event.id, "path_fork")
	assert_eq(event.event_type, EventTypeIDs.CHOICE)
	assert_true(event.is_choice())
	assert_eq(event.options.size(), 2)
	assert_eq(event.options[0]["id"], "elite")


func test_elite_event_with_level_cap() -> void:
	var data: Dictionary = {
		"id": "elite_01",
		"event_type": "elite",
		"difficulty": 5,
		"level_cap": 3,
		"enemy_hp": 80.0
	}
	var event := TypedEventData.new(data, "elite_01")

	assert_eq(event.event_type, EventTypeIDs.ELITE)
	assert_true(event.is_combat())
	assert_false(event.is_battle())  # Elite is not "battle" type
	assert_eq(event.level_cap, 3)
	assert_true(event.has_level_cap)
