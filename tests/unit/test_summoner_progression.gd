extends GutTest

## Unit Tests for Summoner Progression Service
##
## Tests XP granting and level-up checks.
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
		"xp": xp
	}
	ProfileRepo.save_summoner_instance_dict(summoner_data)
	await get_tree().process_frame


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
## LEVEL_UP_SUMMONER TESTS
## =============================================================================

func test_level_up_summoner_succeeds_when_valid() -> void:
	# Set up summoner with enough XP
	await _create_summoner_with_xp("summoner_cole", 100)

	var success: bool = SummonerProgression.level_up_summoner("summoner_cole")

	assert_true(success, "Level-up should succeed with enough XP")

	# Verify level increased
	var info: Dictionary = SummonerProgression.get_summoner_progression_info("summoner_cole")
	assert_eq(info.get("level"), 2, "Summoner should be level 2")


func test_level_up_summoner_fails_when_not_enough_xp() -> void:
	# Set up summoner without enough XP
	await _create_summoner_with_xp("summoner_cole", 50)

	var success: bool = SummonerProgression.level_up_summoner("summoner_cole")

	assert_false(success, "Level-up should fail without enough XP")


func test_level_up_summoner_updates_level_correctly() -> void:
	await _create_summoner_with_xp("summoner_cole", 100)

	var success: bool = SummonerProgression.level_up_summoner("summoner_cole")
	assert_true(success, "Level-up should succeed")

	# Verify level was updated
	var info: Dictionary = SummonerProgression.get_summoner_progression_info("summoner_cole")
	assert_eq(info.get("level"), 2, "Level should be updated to 2")
