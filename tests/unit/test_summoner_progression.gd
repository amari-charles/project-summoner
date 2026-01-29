extends GutTest

## Unit Tests for Summoner Progression Service
##
## Tests XP granting, level-up checks, and level-up with trait selection.
## Uses the actual autoloads (ProfileRepo, SummonerProgression) for integration-style testing.

var _original_profile_id: String = ""


func before_all() -> void:
	# Store original profile ID to restore after tests
	_original_profile_id = ProfileRepo.get_current_profile_id()


func before_each() -> void:
	# Load a test profile to avoid modifying real data
	ProfileRepo.load_profile("test_summoner_progression")
	ProfileRepo.reset_profile()
	# Wait for services to be ready after profile change
	await get_tree().process_frame


func after_all() -> void:
	# Restore original profile
	if not _original_profile_id.is_empty():
		ProfileRepo.load_profile(_original_profile_id)


## =============================================================================
## HELPERS
## =============================================================================

## Create a summoner instance with XP ready to level up
func _create_summoner_with_xp(summoner_id: String, xp: int) -> void:
	# Create a summoner instance directly in the profile
	var summoner_data: Dictionary = {
		"summoner_id": summoner_id,
		"level": 1,
		"xp": xp,
		"acquired_boon_ids": []
	}
	ProfileRepo.save_summoner_instance_dict(summoner_data)
	await get_tree().process_frame


## Get a valid acquirable boon ID for testing
func _get_test_boon_id() -> String:
	# Use one of the level-up boons defined in TraitCatalog
	return "boon_iron_will"


## =============================================================================
## CAN_LEVEL_UP TESTS
## =============================================================================

func test_can_level_up_returns_false_when_no_xp() -> void:
	await _create_summoner_with_xp("summoner_cole", 0)

	assert_false(SummonerProgression.can_level_up("summoner_cole"))


func test_can_level_up_returns_true_when_enough_xp() -> void:
	# Level 1 -> 2 requires 100 XP
	await _create_summoner_with_xp("summoner_cole", 100)

	assert_true(SummonerProgression.can_level_up("summoner_cole"))


func test_can_level_up_returns_false_when_just_under_threshold() -> void:
	# Level 1 -> 2 requires 100 XP
	await _create_summoner_with_xp("summoner_cole", 99)

	assert_false(SummonerProgression.can_level_up("summoner_cole"))


## =============================================================================
## LEVEL_UP_SUMMONER_WITH_TRAIT TESTS
## =============================================================================

func test_level_up_with_trait_succeeds_when_valid() -> void:
	# Set up summoner with enough XP
	await _create_summoner_with_xp("summoner_cole", 100)

	var success: bool = SummonerProgression.level_up_summoner_with_trait(
		"summoner_cole",
		_get_test_boon_id()
	)

	assert_true(success, "Level-up should succeed with valid trait")

	# Verify level increased
	var info: Dictionary = SummonerProgression.get_summoner_progression_info("summoner_cole")
	assert_eq(info.get("level"), 2, "Summoner should be level 2")


func test_level_up_with_trait_fails_when_not_enough_xp() -> void:
	# Set up summoner without enough XP
	await _create_summoner_with_xp("summoner_cole", 50)

	var success: bool = SummonerProgression.level_up_summoner_with_trait(
		"summoner_cole",
		_get_test_boon_id()
	)

	assert_false(success, "Level-up should fail without enough XP")
	# Expect a warning about not enough XP
	assert_engine_error("not have enough XP")


func test_level_up_with_trait_fails_for_unknown_trait() -> void:
	await _create_summoner_with_xp("summoner_cole", 100)

	var success: bool = SummonerProgression.level_up_summoner_with_trait(
		"summoner_cole",
		"nonexistent_trait_id"
	)

	assert_false(success, "Level-up should fail with unknown trait")
	# Expect an error about unknown trait
	assert_engine_error("Unknown trait")


func test_level_up_with_trait_fails_for_innate_trait() -> void:
	await _create_summoner_with_xp("summoner_cole", 100)

	# Cole's innate trait - cannot be acquired again
	var success: bool = SummonerProgression.level_up_summoner_with_trait(
		"summoner_cole",
		"trait_fire_affinity"
	)

	assert_false(success, "Level-up should fail with innate trait")
	# Expect an error about innate trait
	assert_engine_error("innate trait")


func test_level_up_with_trait_fails_for_already_acquired_trait() -> void:
	await _create_summoner_with_xp("summoner_cole", 250)  # Enough for 2 level-ups

	# First level-up should succeed
	var boon_id: String = _get_test_boon_id()
	var first_success: bool = SummonerProgression.level_up_summoner_with_trait(
		"summoner_cole",
		boon_id
	)
	assert_true(first_success, "First level-up should succeed")

	# Second level-up with same trait should fail
	var second_success: bool = SummonerProgression.level_up_summoner_with_trait(
		"summoner_cole",
		boon_id
	)
	assert_false(second_success, "Second level-up should fail - trait already acquired")
	# Expect a warning about already having trait
	assert_engine_error("already has trait")


func test_level_up_with_trait_adds_trait_to_summoner() -> void:
	await _create_summoner_with_xp("summoner_cole", 100)
	var boon_id: String = _get_test_boon_id()

	SummonerProgression.level_up_summoner_with_trait("summoner_cole", boon_id)

	# Verify the trait was added
	var summoner_data: Dictionary = ProfileRepo.get_summoner_instance("summoner_cole")
	var acquired_boons: Array = summoner_data.get("acquired_boon_ids", [])
	assert_true(boon_id in acquired_boons, "Trait should be in acquired boons")


func test_level_up_with_trait_updates_level_correctly() -> void:
	await _create_summoner_with_xp("summoner_cole", 100)

	var success: bool = SummonerProgression.level_up_summoner_with_trait("summoner_cole", _get_test_boon_id())
	assert_true(success, "Level-up should succeed")

	# Verify level was updated
	var info: Dictionary = SummonerProgression.get_summoner_progression_info("summoner_cole")
	assert_eq(info.get("level"), 2, "Level should be updated to 2")


## =============================================================================
## TRAIT POOL TESTS
## =============================================================================

func test_trait_pool_excludes_acquired_boons() -> void:
	await _create_summoner_with_xp("summoner_cole", 250)
	var boon_id: String = _get_test_boon_id()

	# Level up and acquire a boon
	SummonerProgression.level_up_summoner_with_trait("summoner_cole", boon_id)

	# Get trait pool - should not include the acquired boon
	var excluded: Array[String] = [boon_id]
	var pool: Array[Dictionary] = TraitCatalog.get_level_up_trait_pool(excluded, 3)

	for trait_data: Dictionary in pool:
		assert_ne(trait_data.get("id"), boon_id, "Trait pool should not include acquired boon")
