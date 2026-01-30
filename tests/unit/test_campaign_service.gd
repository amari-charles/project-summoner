extends GutTest

## Unit Tests for CampaignService
##
## Tests campaign progression: battle completion, unlocking, rewards.
## Uses MockProfileRepo and MockCampaignServiceCS for isolation.

const CampaignServiceScript: GDScript = preload("res://scripts/services/campaign_service.gd")
const MockCampaignServiceCSScript: GDScript = preload("res://tests/mocks/mock_campaign_service_cs.gd")
const _DeckConstants: GDScript = preload("res://scripts/data/deck_constants.gd")
const MockDeckServiceScript: GDScript = preload("res://tests/mocks/mock_deck_service.gd")

var campaign: Node
var mock_repo: MockProfileRepo
var mock_collection: MockCollectionService
var mock_cs_service: Node  # MockCampaignServiceCS

# Signal capture variables
var _signal_received: bool = false
var _received_battle_id: String = ""
var _unlocked_battles: Array = []
var _signal_count: int = 0


func before_each() -> void:
	# Reset signal capture state
	_signal_received = false
	_received_battle_id = ""
	_unlocked_battles = []
	_signal_count = 0

	# Create fresh mocks for each test
	mock_repo = MockProfileRepo.new()
	mock_collection = MockCollectionService.new()
	mock_cs_service = MockCampaignServiceCSScript.new()

	# Create campaign service and inject dependencies (including mock C# service)
	campaign = CampaignServiceScript.new()
	campaign.init_for_testing(mock_repo, mock_collection, mock_cs_service)


func after_each() -> void:
	if campaign:
		campaign.free()
	if mock_repo:
		mock_repo.free()
	if mock_collection:
		mock_collection.free()
	if mock_cs_service:
		mock_cs_service.free()


## =============================================================================
## BATTLE DEFINITION TESTS
## =============================================================================

func test_get_all_battles_returns_battles() -> void:
	var battles: Array = campaign.get_all_battles()

	assert_gt(battles.size(), 0, "Should have at least one battle defined")


func test_get_battle_returns_valid_battle() -> void:
	# Use a battle ID that exists in the hardcoded definitions
	var battle: Dictionary = campaign.get_battle(String(BattleIDs.FIRST_TRIAL))

	assert_false(battle.is_empty(), "Should return a battle dictionary")
	assert_eq(battle.get("id"), String(BattleIDs.FIRST_TRIAL))


func test_get_battle_returns_empty_for_invalid_id() -> void:
	var battle: Dictionary = campaign.get_battle("nonexistent_battle")

	assert_true(battle.is_empty(), "Should return empty dict for invalid ID")


## =============================================================================
## PROGRESS TRACKING TESTS
## =============================================================================

func test_is_battle_completed_false_initially() -> void:
	assert_false(campaign.is_battle_completed(String(BattleIDs.FIRST_TRIAL)))


func test_complete_battle_marks_as_completed() -> void:
	campaign.complete_battle(String(BattleIDs.FIRST_TRIAL))

	assert_true(campaign.is_battle_completed(String(BattleIDs.FIRST_TRIAL)))


func test_complete_battle_saves_to_profile() -> void:
	campaign.complete_battle(String(BattleIDs.FIRST_TRIAL))

	assert_eq(mock_repo.get_call_count("update_campaign_progress"), 1)
	var progress: Dictionary = mock_repo.get_campaign_progress()
	assert_true(String(BattleIDs.FIRST_TRIAL) in progress.get("completed_battles", []))


func test_complete_battle_ignores_already_completed() -> void:
	campaign.complete_battle(String(BattleIDs.FIRST_TRIAL))
	var initial_call_count: int = mock_repo.get_call_count("update_campaign_progress")

	campaign.complete_battle(String(BattleIDs.FIRST_TRIAL))

	# Should not save again
	assert_eq(mock_repo.get_call_count("update_campaign_progress"), initial_call_count)
	# Expect the push_warning about already completed
	assert_engine_error("already completed")


func _on_battle_completed(battle_id: String) -> void:
	_signal_received = true
	_received_battle_id = battle_id


func test_complete_battle_emits_signal() -> void:
	campaign.battle_completed.connect(_on_battle_completed)

	campaign.complete_battle(String(BattleIDs.FIRST_TRIAL))

	assert_true(_signal_received)
	assert_eq(_received_battle_id, String(BattleIDs.FIRST_TRIAL))


func test_loads_progress_from_profile() -> void:
	# Set up pre-existing progress
	mock_repo.set_campaign_progress({
		"completed_battles": [String(BattleIDs.FIRST_TRIAL), String(BattleIDs.SECOND_CHALLENGE)]
	})

	# Re-init to load progress
	campaign.init_for_testing(mock_repo, mock_collection)

	assert_true(campaign.is_battle_completed(String(BattleIDs.FIRST_TRIAL)))
	assert_true(campaign.is_battle_completed(String(BattleIDs.SECOND_CHALLENGE)))
	assert_false(campaign.is_battle_completed(String(BattleIDs.THIRD_TRIAL)))


## =============================================================================
## UNLOCK REQUIREMENTS TESTS
## =============================================================================

func test_first_battle_is_unlocked_initially() -> void:
	# FIRST_TRIAL has no requirements (start node)
	assert_true(campaign.is_battle_unlocked(String(BattleIDs.FIRST_TRIAL)))


func test_second_battle_locked_until_first_completed() -> void:
	# SECOND_CHALLENGE requires FIRST_TRIAL
	assert_false(campaign.is_battle_unlocked(String(BattleIDs.SECOND_CHALLENGE)))

	campaign.complete_battle(String(BattleIDs.FIRST_TRIAL))

	assert_true(campaign.is_battle_unlocked(String(BattleIDs.SECOND_CHALLENGE)))


func test_get_available_battles_excludes_locked() -> void:
	var available: Array = campaign.get_available_battles()

	# Only FIRST_TRIAL should be available initially
	assert_eq(available.size(), 1)
	assert_eq(available[0].get("id"), String(BattleIDs.FIRST_TRIAL))


func test_get_available_battles_excludes_completed() -> void:
	campaign.complete_battle(String(BattleIDs.FIRST_TRIAL))

	var available: Array = campaign.get_available_battles()

	# FIRST_TRIAL should not be in available list
	for battle: Dictionary in available:
		assert_ne(battle.get("id"), String(BattleIDs.FIRST_TRIAL))


func test_get_completed_battles_returns_completed_only() -> void:
	campaign.complete_battle(String(BattleIDs.FIRST_TRIAL))
	campaign.complete_battle(String(BattleIDs.SECOND_CHALLENGE))

	var completed: Array = campaign.get_completed_battles()

	assert_eq(completed.size(), 2)


## =============================================================================
## PENDING REWARD TESTS
## =============================================================================

func test_set_pending_reward_stores_in_profile() -> void:
	campaign.set_pending_reward(String(BattleIDs.FIRST_TRIAL), String(RewardTypeIDs.FIXED))

	var pending: Variant = campaign.get_pending_reward()
	assert_not_null(pending)
	assert_true(pending is Dictionary)

	var pending_dict: Dictionary = pending
	assert_eq(pending_dict.get("battle_id"), String(BattleIDs.FIRST_TRIAL))
	assert_eq(pending_dict.get("reward_type"), String(RewardTypeIDs.FIXED))


func test_get_pending_reward_returns_null_when_none() -> void:
	var pending: Variant = campaign.get_pending_reward()

	assert_null(pending)


func test_clear_pending_reward_removes_reward() -> void:
	campaign.set_pending_reward(String(BattleIDs.FIRST_TRIAL), String(RewardTypeIDs.FIXED))
	campaign.clear_pending_reward()

	var pending: Variant = campaign.get_pending_reward()
	assert_null(pending)


func test_update_pending_choice_updates_index() -> void:
	campaign.set_pending_reward(String(BattleIDs.FIRST_TRIAL), String(RewardTypeIDs.FLEXIBLE), -1)

	campaign.update_pending_choice(2)

	var pending: Variant = campaign.get_pending_reward()
	assert_true(pending is Dictionary)
	var pending_dict: Dictionary = pending
	assert_eq(pending_dict.get("choice_index"), 2)


## =============================================================================
## REWARD GRANTING TESTS
## =============================================================================

func test_grant_battle_reward_grants_gold() -> void:
	# FIRST_TRIAL has gold_reward: 30 (campaign-scoped gold)
	var result: Dictionary = campaign.grant_battle_reward(String(BattleIDs.FIRST_TRIAL))

	# Verify gold is included in result (C# grants via EconomyService.Instance directly)
	assert_eq(result.get("gold", 0), 30)


func test_grant_battle_reward_grants_cards() -> void:
	# FIRST_TRIAL has reward_cards with charge card
	campaign.grant_battle_reward(String(BattleIDs.FIRST_TRIAL))

	assert_eq(mock_collection.get_call_count("GrantCard"), 1)
	var args: Array = mock_collection.get_call_args("GrantCard")
	assert_eq(args[0][0], "charge")  # catalog_id


func test_claim_pending_reward_completes_battle() -> void:
	campaign.set_pending_reward(String(BattleIDs.FIRST_TRIAL), String(RewardTypeIDs.FIXED))

	campaign.claim_pending_reward()

	assert_true(campaign.is_battle_completed(String(BattleIDs.FIRST_TRIAL)))


func test_claim_pending_reward_clears_pending() -> void:
	campaign.set_pending_reward(String(BattleIDs.FIRST_TRIAL), String(RewardTypeIDs.FIXED))

	campaign.claim_pending_reward()

	assert_null(campaign.get_pending_reward())


func test_claim_pending_reward_returns_empty_when_none() -> void:
	var result: Dictionary = campaign.claim_pending_reward()

	assert_true(result.is_empty())
	# Expect the push_warning about no pending reward
	assert_engine_error("No pending reward")


func test_flexible_reward_uses_chosen_index() -> void:
	# Set up a battle with reward_options (flexible choice)
	var test_battle: Dictionary = {
		"id": "test_flexible",
		"type": NodeTypeIDs.BATTLE,
		"reward_type": RewardTypeIDs.FLEXIBLE,
		"reward_options": ["card_a", "card_b", "card_c"],
		"gold_reward": 0
	}
	# Add test battle to mock
	mock_cs_service._battles["test_flexible"] = test_battle

	# Set pending reward with choice index 1 (card_b)
	campaign.set_pending_reward("test_flexible", String(RewardTypeIDs.FLEXIBLE), 1)
	campaign.update_pending_choice(1)

	# Claim the reward
	campaign.claim_pending_reward()

	# Verify the chosen card (index 1 = card_b) was granted
	assert_eq(mock_collection.get_call_count("GrantCard"), 1)
	var args: Array = mock_collection.get_call_args("GrantCard")
	assert_eq(args[0][0], "card_b", "Should grant the card at chosen index (1)")


func test_fixed_reward_grants_all_cards() -> void:
	# Set up a battle with multiple fixed reward cards
	var test_battle: Dictionary = {
		"id": "test_fixed_multi",
		"type": NodeTypeIDs.BATTLE,
		"reward_type": RewardTypeIDs.FIXED,
		"reward_cards": [
			{"catalog_id": "card_1", "rarity": "common", "count": 1},
			{"catalog_id": "card_2", "rarity": "rare", "count": 1}
		],
		"gold_reward": 50
	}
	# Add test battle to mock
	mock_cs_service._battles["test_fixed_multi"] = test_battle

	# Grant the reward
	campaign.grant_battle_reward("test_fixed_multi")

	# Verify ALL cards were granted (not just the one at chosen index)
	assert_eq(mock_collection.get_call_count("GrantCard"), 2, "Should grant all reward cards")

	var args: Array = mock_collection.get_call_args("GrantCard")
	assert_eq(args[0][0], "card_1", "First card should be card_1")
	assert_eq(args[1][0], "card_2", "Second card should be card_2")


func test_claim_pending_reward_uses_stored_choice_index() -> void:
	# Set up a battle with reward_options
	var test_battle: Dictionary = {
		"id": "test_choice_persist",
		"type": NodeTypeIDs.BATTLE,
		"reward_type": RewardTypeIDs.FLEXIBLE,
		"reward_options": ["option_a", "option_b"],
		"gold_reward": 0
	}
	mock_cs_service._battles["test_choice_persist"] = test_battle

	# Set pending reward, then update choice
	campaign.set_pending_reward("test_choice_persist", String(RewardTypeIDs.FLEXIBLE), -1)
	campaign.update_pending_choice(1)

	# Verify choice is stored
	var pending: Variant = campaign.get_pending_reward()
	assert_true(pending is Dictionary)
	var pending_dict: Dictionary = pending
	assert_eq(pending_dict.get("choice_index"), 1, "Choice index should be persisted")

	# Claim - should use the stored choice index
	campaign.claim_pending_reward()

	assert_eq(mock_collection.get_call_count("GrantCard"), 1)
	var args: Array = mock_collection.get_call_args("GrantCard")
	assert_eq(args[0][0], "option_b", "Should grant option at stored index (1)")


## =============================================================================
## TUTORIAL HELPER TESTS
## =============================================================================

func test_is_battle_tutorial_returns_true_for_tutorial() -> void:
	assert_true(campaign.is_battle_tutorial(String(BattleIDs.FIRST_TRIAL)))
	assert_true(campaign.is_battle_tutorial(String(BattleIDs.SECOND_CHALLENGE)))


func test_is_battle_tutorial_returns_false_for_non_tutorial() -> void:
	# CARAVAN_01 is a caravan event, not a tutorial
	assert_false(campaign.is_battle_tutorial(String(BattleIDs.CARAVAN_01)))


func test_is_tutorial_complete_false_initially() -> void:
	assert_false(campaign.is_tutorial_complete())


func test_is_tutorial_complete_true_when_all_done() -> void:
	# Complete all tutorial battles
	campaign.complete_battle(String(BattleIDs.FIRST_TRIAL))
	campaign.complete_battle(String(BattleIDs.SECOND_CHALLENGE))

	assert_true(campaign.is_tutorial_complete())


func test_get_tutorial_battles_returns_tutorials() -> void:
	var tutorials: Array = campaign.get_tutorial_battles()

	assert_true(String(BattleIDs.FIRST_TRIAL) in tutorials)
	assert_true(String(BattleIDs.SECOND_CHALLENGE) in tutorials)
	assert_false(String(BattleIDs.CARAVAN_01) in tutorials)


## =============================================================================
## SIGNAL TESTS
## =============================================================================

func _on_battle_unlocked(battle_id: String) -> void:
	_unlocked_battles.append(battle_id)


func test_battle_unlocked_emitted_when_requirements_met() -> void:
	campaign.battle_unlocked.connect(_on_battle_unlocked)

	# Complete FIRST_TRIAL which should unlock SECOND_CHALLENGE
	campaign.complete_battle(String(BattleIDs.FIRST_TRIAL))

	assert_true(String(BattleIDs.SECOND_CHALLENGE) in _unlocked_battles)


func _on_campaign_progress_changed() -> void:
	_signal_count += 1


func test_campaign_progress_changed_emitted_on_save() -> void:
	campaign.campaign_progress_changed.connect(_on_campaign_progress_changed)

	campaign.save_progress()

	assert_eq(_signal_count, 1)


## =============================================================================
## DATA CHANGE REACTIVITY TESTS
## =============================================================================

func test_reloads_progress_when_profile_data_changes() -> void:
	# First complete a battle
	campaign.complete_battle(String(BattleIDs.FIRST_TRIAL))
	assert_true(campaign.is_battle_completed(String(BattleIDs.FIRST_TRIAL)))

	# Simulate external change - reset progress
	mock_repo.set_campaign_progress({"completed_battles": []})
	mock_repo.data_changed.emit()

	# Should have reloaded and now show no battles completed
	assert_false(campaign.is_battle_completed(String(BattleIDs.FIRST_TRIAL)))


## =============================================================================
## CAMPAIGN SELECTION TESTS
## =============================================================================

func test_summoners_path_is_default_campaign() -> void:
	# Summoner's Path should be the default campaign
	assert_true(campaign.is_campaign_unlocked(String(CampaignIDs.SUMMONERS_PATH)))


func test_test_arena_has_battles() -> void:
	# Set current campaign to test arena
	campaign.set_current_campaign(String(CampaignIDs.TEST_ARENA))

	# Test arena should have test battles
	var battles: Array = campaign.get_all_battles()
	assert_gt(battles.size(), 0, "Test Arena should have test battles")


func test_test_arena_progress_is_per_summoner() -> void:
	# Set current campaign to test arena
	campaign.set_current_campaign(String(CampaignIDs.TEST_ARENA))

	# Set up per-summoner progress for test arena
	mock_repo.set_campaign_progress({
		"completed_battles": [String(BattleIDs.ARENA_FIRE_WISP)]
	})
	campaign.init_for_testing(mock_repo, mock_collection)
	campaign.set_current_campaign(String(CampaignIDs.TEST_ARENA))

	# Should have per-summoner battles
	assert_true(campaign.is_battle_completed(String(BattleIDs.ARENA_FIRE_WISP)))


## =============================================================================
## STARTER DECK AUTO-ADD TESTS
## =============================================================================

func test_grant_card_auto_adds_to_starter_deck_when_under_threshold() -> void:
	# Create mock deck service with a Starter Deck
	var mock_deck: Node = MockDeckServiceScript.new()
	mock_deck.add_mock_deck({
		"id": "starter-deck-123",
		"name": _DeckConstants.STARTER_DECK_NAME,
		"card_instance_ids": ["card1", "card2"]  # 2 cards, under threshold of 15
	})

	# Reinitialize campaign with mock deck service
	campaign.init_for_testing(mock_repo, mock_collection, mock_cs_service, mock_deck)

	# Grant a card (simulates battle reward)
	campaign.grant_battle_reward(String(BattleIDs.FIRST_TRIAL))

	# Verify card was auto-added to starter deck
	assert_eq(mock_deck.get_call_count("add_card_to_deck"), 1, "Should auto-add card to starter deck")
	var call_args: Array = mock_deck.get_call_args("add_card_to_deck")
	assert_eq(call_args[0][0], "starter-deck-123", "Should add to correct deck")

	mock_deck.free()


func test_grant_card_does_not_auto_add_when_at_threshold() -> void:
	# Create mock deck service with a Starter Deck at threshold
	var mock_deck: Node = MockDeckServiceScript.new()
	var full_card_ids: Array = []
	for i: int in range(_DeckConstants.STARTER_DECK_AUTO_ADD_THRESHOLD):
		full_card_ids.append("card%d" % i)

	mock_deck.add_mock_deck({
		"id": "starter-deck-123",
		"name": _DeckConstants.STARTER_DECK_NAME,
		"card_instance_ids": full_card_ids  # At threshold
	})

	# Reinitialize campaign with mock deck service
	campaign.init_for_testing(mock_repo, mock_collection, mock_cs_service, mock_deck)

	# Grant a card
	campaign.grant_battle_reward(String(BattleIDs.FIRST_TRIAL))

	# Verify card was NOT auto-added (deck is at capacity)
	assert_eq(mock_deck.get_call_count("add_card_to_deck"), 0, "Should not auto-add when at threshold")

	mock_deck.free()


func test_grant_card_does_not_auto_add_when_no_starter_deck() -> void:
	# Create mock deck service with a different deck (not named "Starter Deck")
	var mock_deck: Node = MockDeckServiceScript.new()
	mock_deck.add_mock_deck({
		"id": "other-deck-123",
		"name": "My Custom Deck",
		"card_instance_ids": ["card1"]
	})

	# Reinitialize campaign with mock deck service
	campaign.init_for_testing(mock_repo, mock_collection, mock_cs_service, mock_deck)

	# Grant a card
	campaign.grant_battle_reward(String(BattleIDs.FIRST_TRIAL))

	# Verify card was NOT auto-added (no Starter Deck exists)
	assert_eq(mock_deck.get_call_count("add_card_to_deck"), 0, "Should not auto-add when no Starter Deck")

	mock_deck.free()


func test_grant_card_skips_auto_add_when_deck_service_null() -> void:
	# Reinitialize campaign with null deck service (default for tests)
	campaign.init_for_testing(mock_repo, mock_collection, mock_cs_service, null)

	# Grant a card - should not crash
	campaign.grant_battle_reward(String(BattleIDs.FIRST_TRIAL))

	# Verify card was still granted to collection
	assert_eq(mock_collection.get_call_count("GrantCard"), 1, "Card should still be granted to collection")
