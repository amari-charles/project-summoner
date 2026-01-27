extends GutTest

## Unit Tests for Battle Reward Flow
##
## Tests XP granting to cards and summoners after battle victory.
## Uses mock services for isolation.

const BattleContextScript: GDScript = preload("res://scripts/core/battle_context.gd")
const MockPlayerCardServiceScript: GDScript = preload("res://tests/mocks/mock_player_card_service.gd")
const MockSummonerProgressionScript: GDScript = preload("res://tests/mocks/mock_summoner_progression.gd")

var battle_context: Node
var mock_card_service: Node  # MockPlayerCardService
var mock_summoner_prog: Node  # MockSummonerProgression


func before_each() -> void:
	# Create fresh mocks for each test
	mock_card_service = MockPlayerCardServiceScript.new()
	mock_summoner_prog = MockSummonerProgressionScript.new()

	# Create battle context and inject mock dependencies
	battle_context = BattleContextScript.new()
	battle_context.init_for_testing(null, null, mock_card_service, mock_summoner_prog)


func after_each() -> void:
	if battle_context:
		battle_context.free()
	if mock_card_service:
		mock_card_service.free()
	if mock_summoner_prog:
		mock_summoner_prog.free()


## =============================================================================
## CARDS PLAYED TRACKING TESTS
## =============================================================================

func test_register_card_played_adds_to_list() -> void:
	battle_context.register_card_played("card_001")
	battle_context.register_card_played("card_002")

	var cards_played: Array = battle_context.get_cards_played()

	assert_eq(cards_played.size(), 2)
	assert_true("card_001" in cards_played)
	assert_true("card_002" in cards_played)


func test_register_card_played_ignores_duplicates() -> void:
	battle_context.register_card_played("card_001")
	battle_context.register_card_played("card_001")
	battle_context.register_card_played("card_001")

	var cards_played: Array = battle_context.get_cards_played()

	assert_eq(cards_played.size(), 1, "Should only track unique card IDs")


func test_register_card_played_ignores_empty_string() -> void:
	battle_context.register_card_played("")

	var cards_played: Array = battle_context.get_cards_played()

	assert_eq(cards_played.size(), 0, "Should not track empty card IDs")


func test_cards_played_cleared_on_clear() -> void:
	battle_context.register_card_played("card_001")
	battle_context.register_card_played("card_002")
	assert_eq(battle_context.get_cards_played().size(), 2)

	battle_context.clear()

	assert_eq(battle_context.get_cards_played().size(), 0, "Cards played should be cleared")


func test_cards_played_cleared_on_abandon() -> void:
	# Set up battle state so abandon_battle can run
	battle_context.battle_state = battle_context.BattleState.IN_PROGRESS
	battle_context.register_card_played("card_001")
	battle_context.register_card_played("card_002")
	assert_eq(battle_context.get_cards_played().size(), 2)

	battle_context.abandon_battle()

	assert_eq(battle_context.get_cards_played().size(), 0, "Cards played should be cleared on abandon")


## =============================================================================
## CARD XP GRANTING TESTS
## =============================================================================

func test_grant_xp_to_played_cards_calls_card_service() -> void:
	# Set up battle config with XP reward
	battle_context.battle_config = {"card_xp_reward": 25}

	# Register some cards as played
	battle_context.register_card_played("card_001")
	battle_context.register_card_played("card_002")

	# Grant XP
	battle_context.grant_xp_to_played_cards()

	# Verify card service was called
	assert_eq(mock_card_service.get_call_count("grant_xp_to_cards"), 1)

	# Verify correct arguments
	var call_args: Array = mock_card_service.get_call_args("grant_xp_to_cards")
	assert_eq(call_args.size(), 1)
	var first_call: Array = call_args[0]
	assert_eq(first_call[0].size(), 2, "Should pass 2 card IDs")
	assert_eq(first_call[1], 25, "Should pass XP amount of 25")


func test_grant_xp_to_played_cards_no_xp_when_zero_configured() -> void:
	# Set up battle config without XP reward (or 0)
	battle_context.battle_config = {"card_xp_reward": 0}

	battle_context.register_card_played("card_001")
	battle_context.grant_xp_to_played_cards()

	# Card service should not be called
	assert_eq(mock_card_service.get_call_count("grant_xp_to_cards"), 0)


func test_grant_xp_to_played_cards_no_xp_when_not_configured() -> void:
	# Set up battle config without card_xp_reward key
	battle_context.battle_config = {}

	battle_context.register_card_played("card_001")
	battle_context.grant_xp_to_played_cards()

	# Card service should not be called
	assert_eq(mock_card_service.get_call_count("grant_xp_to_cards"), 0)


func test_grant_xp_to_played_cards_no_xp_when_no_cards_played() -> void:
	# Set up battle config with XP reward but no cards played
	battle_context.battle_config = {"card_xp_reward": 25}

	# Don't register any cards
	battle_context.grant_xp_to_played_cards()

	# Card service should not be called
	assert_eq(mock_card_service.get_call_count("grant_xp_to_cards"), 0)


## =============================================================================
## SUMMONER XP GRANTING TESTS
## =============================================================================

func test_grant_xp_to_active_summoner_calls_progression_service() -> void:
	# Set up battle config with summoner XP reward
	battle_context.battle_config = {"summoner_xp_reward": 50}

	# Grant XP
	battle_context.grant_xp_to_active_summoner()

	# Verify progression service was called
	assert_eq(mock_summoner_prog.get_call_count("grant_active_summoner_xp"), 1)

	# Verify correct arguments
	var call_args: Array = mock_summoner_prog.get_call_args("grant_active_summoner_xp")
	assert_eq(call_args.size(), 1)
	assert_eq(call_args[0][0], 50, "Should pass XP amount of 50")


func test_grant_xp_to_active_summoner_no_xp_when_zero_configured() -> void:
	# Set up battle config with 0 summoner XP
	battle_context.battle_config = {"summoner_xp_reward": 0}

	battle_context.grant_xp_to_active_summoner()

	# Progression service should not be called
	assert_eq(mock_summoner_prog.get_call_count("grant_active_summoner_xp"), 0)


func test_grant_xp_to_active_summoner_no_xp_when_not_configured() -> void:
	# Set up battle config without summoner_xp_reward key
	battle_context.battle_config = {}

	battle_context.grant_xp_to_active_summoner()

	# Progression service should not be called
	assert_eq(mock_summoner_prog.get_call_count("grant_active_summoner_xp"), 0)


func test_grant_xp_accumulates_in_progression_service() -> void:
	# Set up battle config with XP
	battle_context.battle_config = {"summoner_xp_reward": 30}

	# Grant XP twice (simulating two battles)
	battle_context.grant_xp_to_active_summoner()
	battle_context.grant_xp_to_active_summoner()

	# Verify total XP accumulated
	assert_eq(mock_summoner_prog.get_summoner_xp(), 60, "XP should accumulate: 30 + 30 = 60")


## =============================================================================
## COMBINED XP FLOW TESTS
## =============================================================================

func test_both_card_and_summoner_xp_granted_with_full_config() -> void:
	# Set up battle config with both XP rewards
	battle_context.battle_config = {
		"card_xp_reward": 20,
		"summoner_xp_reward": 40
	}

	# Register cards played
	battle_context.register_card_played("card_001")
	battle_context.register_card_played("card_002")
	battle_context.register_card_played("card_003")

	# Grant both XP types
	battle_context.grant_xp_to_played_cards()
	battle_context.grant_xp_to_active_summoner()

	# Verify card service called
	assert_eq(mock_card_service.get_call_count("grant_xp_to_cards"), 1)
	var card_args: Array = mock_card_service.get_call_args("grant_xp_to_cards")[0]
	assert_eq(card_args[0].size(), 3, "Should pass 3 card IDs")
	assert_eq(card_args[1], 20, "Card XP should be 20")

	# Verify summoner progression called
	assert_eq(mock_summoner_prog.get_call_count("grant_active_summoner_xp"), 1)
	var summoner_args: Array = mock_summoner_prog.get_call_args("grant_active_summoner_xp")[0]
	assert_eq(summoner_args[0], 40, "Summoner XP should be 40")
