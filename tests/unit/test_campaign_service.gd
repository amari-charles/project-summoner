extends GutTest

## Unit Tests for CampaignService
##
## Tests campaign progression: battle completion, unlocking, rewards.
## Uses MockProfileRepo and MockCampaignServiceCS for isolation.

const CampaignServiceScript: GDScript = preload("res://scripts/services/campaign_service.gd")
const MockCampaignServiceCSScript: GDScript = preload("res://tests/mocks/mock_campaign_service_cs.gd")

var campaign: Node
var mock_repo: MockProfileRepo
var mock_economy: MockEconomyService
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
	mock_economy = MockEconomyService.new()
	mock_collection = MockCollectionService.new()
	mock_cs_service = MockCampaignServiceCSScript.new()

	# Create campaign service and inject dependencies (including mock C# service)
	campaign = CampaignServiceScript.new()
	campaign.init_for_testing(mock_repo, mock_economy, mock_collection, mock_cs_service)


func after_each() -> void:
	if campaign:
		campaign.free()
	if mock_repo:
		mock_repo.free()
	if mock_economy:
		mock_economy.free()
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
	campaign.init_for_testing(mock_repo, mock_economy, mock_collection)

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

func test_grant_battle_reward_adds_gold() -> void:
	# FIRST_TRIAL has gold_reward: 30 (uses campaign-scoped gold)
	campaign.grant_battle_reward(String(BattleIDs.FIRST_TRIAL))

	assert_eq(mock_economy.get_call_count("add_campaign_gold"), 1)
	var args: Array = mock_economy.get_call_args("add_campaign_gold")
	assert_eq(args[0][0], 30)


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
		"completed_battles": [String(BattleIDs.ARENA_FIRE_ELEMENTAL)]
	})
	campaign.init_for_testing(mock_repo, mock_economy, mock_collection)
	campaign.set_current_campaign(String(CampaignIDs.TEST_ARENA))

	# Should have per-summoner battles
	assert_true(campaign.is_battle_completed(String(BattleIDs.ARENA_FIRE_ELEMENTAL)))
