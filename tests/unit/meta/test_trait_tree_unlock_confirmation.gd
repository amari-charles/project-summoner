extends GutTest

const SUMMONER_TRAIT_TREE_SCREEN_SCENE: PackedScene = preload("res://scenes/meta/screens/trait_tree_screen.tscn")

var _original_profile_id: String = ""


func before_all() -> void:
	_original_profile_id = SafeTypeUtils.string(ProfileRepo.GetActiveProfileDict().get("profile_id", ""), "")


func before_each() -> void:
	ProfileRepo.LoadProfile("test_trait_tree_c06")
	ProfileRepo.ResetProfile()
	SummonerSelectionApi.save_summoner_instance_dict({
		"summoner_id": "summoner_cole",
		"level": 2,
		"xp": 0,
		"acquired_trait_ids": [],
		"unspent_trait_points": 1
	})
	SummonerSelectionApi.set_active_summoner("summoner_cole", null)
	await get_tree().process_frame


func after_all() -> void:
	if not _original_profile_id.is_empty():
		ProfileRepo.LoadProfile(_original_profile_id)


func test_case_c06_available_node_opens_confirmation_modal() -> void:
	var screen: TraitTreeScreen = SUMMONER_TRAIT_TREE_SCREEN_SCENE.instantiate() as TraitTreeScreen
	assert_true(screen != null, "Expected summoner trait tree screen scene")
	add_child_autofree(screen)
	await get_tree().process_frame
	await get_tree().process_frame

	var available_trait_id: String = ""
	var available_node_data: Dictionary = {}
	for node_var: Variant in screen._progression_nodes:
		if not node_var is Dictionary:
			continue
		var node_data: Dictionary = node_var
		if SafeTypeUtils.bool_val(node_data.get("can_unlock", false), false):
			available_trait_id = str(node_data.get("id", ""))
			available_node_data = node_data
			break

	assert_false(available_trait_id.is_empty(), "Expected at least one unlockable trait at level 2 with one trait point")

	screen._on_trait_node_pressed(available_trait_id)
	await get_tree().process_frame

	assert_true(screen.unlock_confirm_dialog.visible, "Unlockable node click should open confirmation modal")
	assert_eq(screen.unlock_confirm_dialog.title, str(available_node_data.get("name", available_trait_id)))
	assert_true(screen.unlock_confirm_dialog.get_ok_button().visible, "Unlock action should be visible")
	assert_false(screen.unlock_confirm_dialog.get_ok_button().disabled, "Unlock action should be enabled for unlockable traits")
	assert_eq(screen._pending_unlock_trait_id, available_trait_id, "Pending unlock trait should match clicked node")
