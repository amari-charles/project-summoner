extends GutTest

const SUMMONER_TRAIT_TREE_SCREEN_SCENE: PackedScene = preload("res://scenes/meta/screens/trait_tree_screen.tscn")

var _original_profile_id: String = ""


func before_all() -> void:
	_original_profile_id = SafeTypeUtils.string(ProfileRepo.GetActiveProfileDict().get("profile_id", ""), "")


func before_each() -> void:
	ProfileRepo.LoadProfile("test_trait_tree_c05")
	ProfileRepo.ResetProfile()
	SummonerSelectionApi.save_summoner_instance_dict({
		"summoner_id": "summoner_cole",
		"level": 1,
		"xp": 0,
		"acquired_trait_ids": [],
		"unspent_trait_points": 0
	})
	SummonerSelectionApi.set_active_summoner("summoner_cole", null)
	await get_tree().process_frame


func after_all() -> void:
	if not _original_profile_id.is_empty():
		ProfileRepo.LoadProfile(_original_profile_id)


func test_case_c05_locked_node_popup_shows_details_and_disabled_unlock() -> void:
	var screen: TraitTreeScreen = SUMMONER_TRAIT_TREE_SCREEN_SCENE.instantiate() as TraitTreeScreen
	assert_true(screen != null, "Expected summoner trait tree screen scene")
	add_child_autofree(screen)
	await get_tree().process_frame
	await get_tree().process_frame

	var locked_trait_id: String = ""
	var locked_node_data: Dictionary = {}
	for node_var: Variant in screen._progression_nodes:
		if not node_var is Dictionary:
			continue
		var node_data: Dictionary = node_var
		if str(node_data.get("state", "")) == "locked":
			locked_trait_id = str(node_data.get("id", ""))
			locked_node_data = node_data
			break

	assert_false(locked_trait_id.is_empty(), "Expected at least one locked trait node for level 1 summoner")

	screen._on_trait_node_pressed(locked_trait_id)
	await get_tree().process_frame

	assert_true(screen.unlock_confirm_dialog.visible, "Clicking a locked node should open detail modal")
	assert_eq(screen.unlock_confirm_dialog.title, str(locked_node_data.get("name", locked_trait_id)))
	assert_true(screen.unlock_confirm_dialog.dialog_text.length() > 0, "Locked node modal should show description/reason text")
	assert_true(screen.unlock_confirm_dialog.get_ok_button().visible, "Unlock action should be visible for progression nodes")
	assert_true(screen.unlock_confirm_dialog.get_ok_button().disabled, "Unlock action should be disabled when locked")
