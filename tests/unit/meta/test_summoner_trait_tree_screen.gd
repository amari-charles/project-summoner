extends GutTest

const SUMMONER_TRAIT_TREE_SCREEN_SCENE: PackedScene = preload("res://scenes/meta/screens/trait_tree_screen.tscn")

var _original_profile_id: String = ""


func before_all() -> void:
	_original_profile_id = SafeTypeUtils.string(ProfileRepo.GetActiveProfileDict().get("profile_id", ""), "")


func before_each() -> void:
	ProfileRepo.LoadProfile("test_trait_tree_c04")
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


func test_case_c04_summoner_traits_use_tree_surface() -> void:
	var screen: TraitTreeScreen = SUMMONER_TRAIT_TREE_SCREEN_SCENE.instantiate() as TraitTreeScreen
	assert_true(screen != null, "Expected summoner trait tree screen scene")
	add_child(screen)
	await get_tree().process_frame
	await get_tree().process_frame

	assert_true(screen._progression_nodes.size() > 0, "Summoner trait screen should load progression node data")

	var node_buttons: int = 0
	for child: Node in screen.tree_canvas.get_children():
		if child is Button:
			node_buttons += 1
	assert_true(node_buttons > 0, "Summoner traits should render as positioned tree nodes, not list-only fallback")
