extends GutTest

## Unit Tests for CampaignService
##
## Tests campaign progression: battle completion, unlocking, rewards.
## Uses MockProfileRepo for isolation from disk I/O.

var campaign: CampaignService
var mock_repo: MockProfileRepo
var mock_economy: MockEconomyService
var mock_collection: MockCollectionService

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

	# Create campaign service and inject dependencies
	campaign = CampaignService.new()
	campaign.init_for_testing(mock_repo, mock_economy, mock_collection)


func after_each() -> void:
	if campaign:
		campaign.free()
	if mock_repo:
		mock_repo.free()
	if mock_economy:
		mock_economy.free()
	if mock_collection:
		mock_collection.free()


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
	campaign.complete_battle(String(BattleIDs.EVENT_AFFINITY))

	assert_true(campaign.is_battle_completed(String(BattleIDs.EVENT_AFFINITY)))


func test_complete_battle_saves_to_profile() -> void:
	# Default campaign is onboarding (shared), so this saves to shared progress
	campaign.complete_battle(String(BattleIDs.EVENT_AFFINITY))

	assert_eq(mock_repo.get_call_count("update_shared_campaign_progress"), 1)
	var progress: Dictionary = mock_repo.get_shared_campaign_progress()
	assert_true(String(BattleIDs.EVENT_AFFINITY) in progress.get("completed_battles", []))


func test_complete_battle_ignores_already_completed() -> void:
	campaign.complete_battle(String(BattleIDs.EVENT_AFFINITY))
	var initial_call_count: int = mock_repo.get_call_count("update_campaign_progress")

	campaign.complete_battle(String(BattleIDs.EVENT_AFFINITY))

	# Should not save again
	assert_eq(mock_repo.get_call_count("update_campaign_progress"), initial_call_count)
	# Expect the push_warning about already completed
	assert_engine_error("already completed")


func _on_battle_completed(battle_id: String) -> void:
	_signal_received = true
	_received_battle_id = battle_id


func test_complete_battle_emits_signal() -> void:
	campaign.battle_completed.connect(_on_battle_completed)

	campaign.complete_battle(String(BattleIDs.EVENT_AFFINITY))

	assert_true(_signal_received)
	assert_eq(_received_battle_id, String(BattleIDs.EVENT_AFFINITY))


func test_loads_progress_from_profile() -> void:
	# Set up pre-existing shared progress (onboarding is the default campaign)
	mock_repo.set_shared_campaign_progress({
		"completed_battles": [String(BattleIDs.EVENT_AFFINITY), String(BattleIDs.EVENT_FIRST_SUMMON)]
	})

	# Re-init to load progress
	campaign.init_for_testing(mock_repo, mock_economy, mock_collection)

	assert_true(campaign.is_battle_completed(String(BattleIDs.EVENT_AFFINITY)))
	assert_true(campaign.is_battle_completed(String(BattleIDs.EVENT_FIRST_SUMMON)))
	assert_false(campaign.is_battle_completed(String(BattleIDs.FIRST_TRIAL)))


## =============================================================================
## UNLOCK REQUIREMENTS TESTS
## =============================================================================

func test_first_battle_is_unlocked_initially() -> void:
	# EVENT_AFFINITY has no requirements
	assert_true(campaign.is_battle_unlocked(String(BattleIDs.EVENT_AFFINITY)))


func test_second_battle_locked_until_first_completed() -> void:
	# EVENT_FIRST_SUMMON requires EVENT_AFFINITY
	assert_false(campaign.is_battle_unlocked(String(BattleIDs.EVENT_FIRST_SUMMON)))

	campaign.complete_battle(String(BattleIDs.EVENT_AFFINITY))

	assert_true(campaign.is_battle_unlocked(String(BattleIDs.EVENT_FIRST_SUMMON)))


func test_get_available_battles_excludes_locked() -> void:
	var available: Array = campaign.get_available_battles()

	# Only EVENT_AFFINITY should be available initially
	assert_eq(available.size(), 1)
	assert_eq(available[0].get("id"), String(BattleIDs.EVENT_AFFINITY))


func test_get_available_battles_excludes_completed() -> void:
	campaign.complete_battle(String(BattleIDs.EVENT_AFFINITY))

	var available: Array = campaign.get_available_battles()

	# EVENT_AFFINITY should not be in available list
	for battle: Dictionary in available:
		assert_ne(battle.get("id"), String(BattleIDs.EVENT_AFFINITY))


func test_get_completed_battles_returns_completed_only() -> void:
	campaign.complete_battle(String(BattleIDs.EVENT_AFFINITY))
	campaign.complete_battle(String(BattleIDs.EVENT_FIRST_SUMMON))

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
	campaign.set_pending_reward(String(BattleIDs.FIRST_TRIAL), String(RewardTypeIDs.CHOICE), -1)

	campaign.update_pending_choice(2)

	var pending: Variant = campaign.get_pending_reward()
	assert_true(pending is Dictionary)
	var pending_dict: Dictionary = pending
	assert_eq(pending_dict.get("choice_index"), 2)


## =============================================================================
## REWARD GRANTING TESTS
## =============================================================================

func test_grant_battle_reward_adds_gold() -> void:
	# FIRST_TRIAL has gold_reward: 30 (now uses campaign-scoped gold)
	campaign.grant_battle_reward(String(BattleIDs.FIRST_TRIAL))

	assert_eq(mock_economy.get_call_count("add_campaign_gold"), 1)
	var args: Array = mock_economy.get_call_args("add_campaign_gold")
	assert_eq(args[0][0], 30)


func test_grant_battle_reward_grants_cards() -> void:
	# FIRST_TRIAL has reward_cards with charge card
	campaign.grant_battle_reward(String(BattleIDs.FIRST_TRIAL))

	assert_eq(mock_collection.get_call_count("grant_card"), 1)
	var args: Array = mock_collection.get_call_args("grant_card")
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
	assert_true(campaign.is_battle_tutorial(String(BattleIDs.CHARGE_TUTORIAL)))


func test_is_battle_tutorial_returns_false_for_event() -> void:
	assert_false(campaign.is_battle_tutorial(String(BattleIDs.EVENT_AFFINITY)))


func test_is_tutorial_complete_false_initially() -> void:
	assert_false(campaign.is_tutorial_complete())


func test_is_tutorial_complete_true_when_all_done() -> void:
	# Complete all tutorial battles
	campaign.complete_battle(String(BattleIDs.FIRST_TRIAL))
	campaign.complete_battle(String(BattleIDs.CHARGE_TUTORIAL))

	assert_true(campaign.is_tutorial_complete())


func test_get_tutorial_battles_returns_tutorials() -> void:
	var tutorials: Array = campaign.get_tutorial_battles()

	assert_true(String(BattleIDs.FIRST_TRIAL) in tutorials)
	assert_true(String(BattleIDs.CHARGE_TUTORIAL) in tutorials)
	assert_false(String(BattleIDs.EVENT_AFFINITY) in tutorials)


## =============================================================================
## SIGNAL TESTS
## =============================================================================

func _on_battle_unlocked(battle_id: String) -> void:
	_unlocked_battles.append(battle_id)


func test_battle_unlocked_emitted_when_requirements_met() -> void:
	campaign.battle_unlocked.connect(_on_battle_unlocked)

	# Complete EVENT_AFFINITY which should unlock EVENT_FIRST_SUMMON
	campaign.complete_battle(String(BattleIDs.EVENT_AFFINITY))

	assert_true(String(BattleIDs.EVENT_FIRST_SUMMON) in _unlocked_battles)


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
	# First complete a battle (onboarding uses shared progress)
	campaign.complete_battle(String(BattleIDs.EVENT_AFFINITY))
	assert_true(campaign.is_battle_completed(String(BattleIDs.EVENT_AFFINITY)))

	# Simulate external change - reset shared progress
	mock_repo.set_shared_campaign_progress({"completed_battles": []})
	mock_repo.data_changed.emit()

	# Should have reloaded and now show no battles completed
	assert_false(campaign.is_battle_completed(String(BattleIDs.EVENT_AFFINITY)))


## =============================================================================
## SHARED CAMPAIGN TESTS
## =============================================================================

func test_onboarding_campaign_is_shared() -> void:
	# Set current campaign to onboarding
	campaign.set_current_campaign(String(CampaignIDs.ONBOARDING))

	# Complete a battle - should save to shared progress
	campaign.complete_battle(String(BattleIDs.EVENT_AFFINITY))

	# Verify it was saved to shared progress, not per-summoner
	assert_eq(mock_repo.get_call_count("update_shared_campaign_progress"), 1)
	var shared_progress: Dictionary = mock_repo.get_shared_campaign_progress()
	assert_true(String(BattleIDs.EVENT_AFFINITY) in shared_progress.get("completed_battles", []))


func test_academy_trials_is_not_shared() -> void:
	# First complete onboarding to unlock academy trials
	mock_repo.set_shared_campaign_progress({
		"completed_battles": [
			String(BattleIDs.EVENT_AFFINITY),
			String(BattleIDs.EVENT_FIRST_SUMMON),
			String(BattleIDs.FIRST_TRIAL),
			String(BattleIDs.CHARGE_TUTORIAL),
			String(BattleIDs.EVENT_CARAVAN_TUTORIAL)
		]
	})
	campaign.init_for_testing(mock_repo, mock_economy, mock_collection)

	# Set current campaign to academy trials
	campaign.set_current_campaign(String(CampaignIDs.ACADEMY_TRIALS))

	# Verify academy trials has no battles (they're in onboarding now)
	var battles: Array = campaign.get_all_battles()
	assert_eq(battles.size(), 0, "Academy trials should have no battles yet")


func test_loads_shared_progress_for_onboarding_campaign() -> void:
	# Set up shared progress
	mock_repo.set_shared_campaign_progress({
		"completed_battles": [String(BattleIDs.EVENT_AFFINITY)]
	})

	# Set current campaign to onboarding and reinit
	mock_repo.update_profile_meta({"selected_campaign": String(CampaignIDs.ONBOARDING)})
	campaign.init_for_testing(mock_repo, mock_economy, mock_collection)

	# Should load from shared progress
	assert_true(campaign.is_battle_completed(String(BattleIDs.EVENT_AFFINITY)))


func test_loads_per_summoner_progress_for_non_shared_campaign() -> void:
	# Complete onboarding first
	mock_repo.set_shared_campaign_progress({
		"completed_battles": [
			String(BattleIDs.EVENT_AFFINITY),
			String(BattleIDs.EVENT_FIRST_SUMMON),
			String(BattleIDs.FIRST_TRIAL),
			String(BattleIDs.CHARGE_TUTORIAL),
			String(BattleIDs.EVENT_CARAVAN_TUTORIAL)
		]
	})

	# Set up per-summoner progress for combat arena (a non-shared campaign with battles)
	mock_repo.set_campaign_progress({
		"completed_battles": [String(BattleIDs.ARENA_FIRE_ELEMENTAL)]
	})

	# Set current campaign to combat arena (non-shared, has battles)
	mock_repo.update_profile_meta({"selected_campaign": String(CampaignIDs.COMBAT_ARENA)})
	campaign.init_for_testing(mock_repo, mock_economy, mock_collection)

	# Switch to combat arena
	campaign.set_current_campaign(String(CampaignIDs.COMBAT_ARENA))

	# Should NOT have onboarding battles in completed list (they're in shared)
	assert_false(campaign.is_battle_completed(String(BattleIDs.EVENT_AFFINITY)))
	# Should have per-summoner battles
	assert_true(campaign.is_battle_completed(String(BattleIDs.ARENA_FIRE_ELEMENTAL)))


## =============================================================================
## CAMPAIGN UNLOCK REQUIREMENT TESTS
## =============================================================================

func test_academy_trials_locked_without_onboarding() -> void:
	# No shared progress - onboarding not complete
	mock_repo.set_shared_campaign_progress({"completed_battles": []})
	campaign.init_for_testing(mock_repo, mock_economy, mock_collection)

	assert_false(campaign.is_campaign_unlocked(String(CampaignIDs.ACADEMY_TRIALS)))


func test_academy_trials_unlocked_after_onboarding() -> void:
	# Complete all onboarding battles
	mock_repo.set_shared_campaign_progress({
		"completed_battles": [
			String(BattleIDs.EVENT_AFFINITY),
			String(BattleIDs.EVENT_FIRST_SUMMON),
			String(BattleIDs.FIRST_TRIAL),
			String(BattleIDs.CHARGE_TUTORIAL),
			String(BattleIDs.EVENT_CARAVAN_TUTORIAL)
		]
	})
	campaign.init_for_testing(mock_repo, mock_economy, mock_collection)

	assert_true(campaign.is_campaign_unlocked(String(CampaignIDs.ACADEMY_TRIALS)))


func test_onboarding_always_unlocked() -> void:
	# No progress at all
	mock_repo.set_shared_campaign_progress({"completed_battles": []})
	campaign.init_for_testing(mock_repo, mock_economy, mock_collection)

	assert_true(campaign.is_campaign_unlocked(String(CampaignIDs.ONBOARDING)))


## =============================================================================
## IS_ONBOARDING_COMPLETE TESTS
## =============================================================================

func test_is_onboarding_complete_false_initially() -> void:
	mock_repo.set_shared_campaign_progress({"completed_battles": []})
	campaign.init_for_testing(mock_repo, mock_economy, mock_collection)

	assert_false(campaign.is_onboarding_complete())


func test_is_onboarding_complete_false_partial() -> void:
	# Some onboarding battles done but not all
	mock_repo.set_shared_campaign_progress({
		"completed_battles": [
			String(BattleIDs.EVENT_AFFINITY),
			String(BattleIDs.EVENT_FIRST_SUMMON)
		]
	})
	campaign.init_for_testing(mock_repo, mock_economy, mock_collection)

	assert_false(campaign.is_onboarding_complete())


func test_is_onboarding_complete_true_when_all_done() -> void:
	# All onboarding battles complete
	mock_repo.set_shared_campaign_progress({
		"completed_battles": [
			String(BattleIDs.EVENT_AFFINITY),
			String(BattleIDs.EVENT_FIRST_SUMMON),
			String(BattleIDs.FIRST_TRIAL),
			String(BattleIDs.CHARGE_TUTORIAL),
			String(BattleIDs.EVENT_CARAVAN_TUTORIAL)
		]
	})
	campaign.init_for_testing(mock_repo, mock_economy, mock_collection)

	assert_true(campaign.is_onboarding_complete())


## =============================================================================
## PENDING REWARD WITH SHARED CAMPAIGNS
## =============================================================================

func test_pending_reward_uses_shared_storage_for_onboarding() -> void:
	# Set campaign to onboarding
	campaign.set_current_campaign(String(CampaignIDs.ONBOARDING))

	campaign.set_pending_reward(String(BattleIDs.FIRST_TRIAL), String(RewardTypeIDs.FIXED))

	# Should have called shared progress
	assert_eq(mock_repo.get_call_count("update_shared_campaign_progress"), 1)


func test_clear_pending_reward_uses_shared_storage_for_onboarding() -> void:
	# Set campaign to onboarding
	campaign.set_current_campaign(String(CampaignIDs.ONBOARDING))

	# Set and then clear a pending reward
	mock_repo.set_shared_campaign_progress({
		"completed_battles": [],
		"pending_reward": {"battle_id": String(BattleIDs.FIRST_TRIAL)}
	})
	campaign.clear_pending_reward()

	# Should have called shared progress to clear
	assert_gt(mock_repo.get_call_count("update_shared_campaign_progress"), 0)
