extends GutTest

## Unit Tests for Summoner Progression Service
##
## Tests XP granting and automatic level application.
## Uses the actual autoloads (ProfileRepo, SummonerProgression) for integration-style testing.

var _original_profile_id: String = ""


func before_all() -> void:
	# Store original profile ID to restore after tests
	_original_profile_id = ProfileRepo.GetActiveProfileDict().get("profile_id", "")


func before_each() -> void:
	# Load a test profile to avoid modifying real data
	ProfileRepo.LoadProfile("test_summoner_progression")
	ProfileRepo.ResetProfile()
	# Wait for services to be ready after profile change
	await get_tree().process_frame


func after_all() -> void:
	# Restore original profile
	if not _original_profile_id.is_empty():
		ProfileRepo.LoadProfile(_original_profile_id)


## =============================================================================
## HELPERS
## =============================================================================

## Create a summoner instance with a chosen starting XP value.
func _create_summoner_with_xp(summoner_id: String, xp: int) -> void:
	# Create a summoner instance directly in the profile
	var summoner_data: Dictionary = {
		"summoner_id": summoner_id,
		"level": 1,
		"xp": xp
	}
	ProfileRepo.SaveSummonerInstanceDict(summoner_data)
	await get_tree().process_frame


## =============================================================================
## AUTOMATIC LEVEL TESTS
## =============================================================================

func test_grant_xp_automatically_levels_summoner() -> void:
	await _create_summoner_with_xp("summoner_cole", 0)

	SummonerProgression.GrantSummonerXp("summoner_cole", 100)

	# Verify level increased
	var info: Dictionary = SummonerProgression.GetSummonerProgressionInfo("summoner_cole")
	assert_eq(info.get("level"), 2, "Summoner should be level 2")


func test_grant_xp_below_threshold_preserves_level() -> void:
	await _create_summoner_with_xp("summoner_cole", 0)

	SummonerProgression.GrantSummonerXp("summoner_cole", 50)

	var info: Dictionary = SummonerProgression.GetSummonerProgressionInfo("summoner_cole")
	assert_eq(info.get("level"), 1)
	assert_eq(info.get("xp"), 50)


func test_grant_xp_banks_upgrade_point() -> void:
	await _create_summoner_with_xp("summoner_cole", 0)

	SummonerProgression.GrantSummonerXp("summoner_cole", 100)

	var info: Dictionary = SummonerProgression.GetSummonerProgressionInfo("summoner_cole")
	assert_eq(info.get("level"), 2, "Level should be updated to 2")
	assert_eq(info.get("unspent_trait_points"), 1)
