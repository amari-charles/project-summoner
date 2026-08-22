extends GutTest

var _original_profile_id: String = ""


func before_all() -> void:
	_original_profile_id = SafeTypeUtils.string(ProfileRepo.GetActiveProfileDict().get("profile_id", ""), "")


func before_each() -> void:
	ProfileRepo.LoadProfile("test_trait_tree_c01")
	ProfileRepo.ResetProfile()
	await get_tree().process_frame


func after_all() -> void:
	if not _original_profile_id.is_empty():
		ProfileRepo.LoadProfile(_original_profile_id)


func test_case_c01_xp_auto_levels_and_banks_points_without_forced_trait_choice() -> void:
	var card_instance_id: String = CardServiceApi.grant_card("fire_wisp", "common")
	assert_false(card_instance_id.is_empty(), "Expected granted card instance id")

	assert_true(ProfileRepo.UpdateCardFromDict(card_instance_id, {
		"level": 1,
		"xp": 0,
		"unspent_trait_points": 0
	}))
	await get_tree().process_frame

	var before_info: Dictionary = CardServiceApi.get_card_progression_info_dict(card_instance_id)
	var before_points: int = SafeTypeUtils.int_val(before_info.get("unspent_trait_points", 0), 0)
	var before_traits: Array = CardServiceApi.get_applied_traits(card_instance_id)

	CardServiceApi.grant_xp(card_instance_id, 30)
	await get_tree().process_frame

	var after_info: Dictionary = CardServiceApi.get_card_progression_info_dict(card_instance_id)
	var after_points: int = SafeTypeUtils.int_val(after_info.get("unspent_trait_points", 0), 0)
	var after_traits: Array = CardServiceApi.get_applied_traits(card_instance_id)

	assert_eq(after_points, before_points + 1, "Level-up should grant one trait point")
	assert_eq(SafeTypeUtils.int_val(after_info.get("level", 0), 0), 2, "XP should apply the level automatically")
	assert_eq(SafeTypeUtils.int_val(after_info.get("xp", -1), -1), 0, "Exact threshold XP should be consumed")
	assert_eq(after_traits, before_traits, "Level-up should not auto-spend or auto-apply a trait")
