extends GutTest

const SUMMONER_TRAIT_TREE_SCREEN_SCENE: PackedScene = preload("res://scenes/meta/screens/trait_tree_screen.tscn")

var _original_profile_id: String = ""


func before_all() -> void:
	_original_profile_id = SafeTypeUtils.string(ProfileRepo.GetActiveProfileDict().get("profile_id", ""), "")


func before_each() -> void:
	ProfileRepo.LoadProfile("test_trait_tree_c12")
	ProfileRepo.ResetProfile()
	SummonerSelectionApi.save_summoner_instance_dict({
		"summoner_id": "summoner_cole",
		"level": 4,
		"xp": 0,
		"acquired_trait_ids": [],
		"unspent_trait_points": 0
	})
	SummonerSelectionApi.set_active_summoner("summoner_cole", null)
	await get_tree().process_frame


func after_all() -> void:
	if not _original_profile_id.is_empty():
		ProfileRepo.LoadProfile(_original_profile_id)


func test_case_c12_tree_canvas_layout_bottom_up_without_overlap() -> void:
	var screen: TraitTreeScreen = SUMMONER_TRAIT_TREE_SCREEN_SCENE.instantiate() as TraitTreeScreen
	assert_true(screen != null, "Expected summoner trait tree screen scene")
	add_child(screen)
	await get_tree().process_frame
	await get_tree().process_frame

	var node_by_id: Dictionary = screen._node_by_id
	assert_true(node_by_id.size() > 0, "Tree layout should create positioned progression nodes")

	var node_rects: Array[Rect2] = []
	for node_var: Variant in node_by_id.values():
		if not node_var is Button:
			continue
		var node: Button = node_var
		assert_eq(node.custom_minimum_size.x, node.custom_minimum_size.y, "Progression nodes should be circular (square bounds)")

		var node_rect: Rect2 = Rect2(node.position, node.size)
		for other_rect: Rect2 in node_rects:
			assert_false(node_rect.intersects(other_rect), "Trait nodes should not overlap in the tree layout")
		node_rects.append(node_rect)

	for node_var: Variant in screen._progression_nodes:
		if not node_var is Dictionary:
			continue
		var node_data: Dictionary = node_var
		var child_id: String = str(node_data.get("id", ""))
		if child_id.is_empty() or not node_by_id.has(child_id):
			continue
		var child_node: Button = node_by_id[child_id]
		var prerequisites: Array = SafeTypeUtils.array(node_data.get("prerequisites", []))
		for prereq_var: Variant in prerequisites:
			var prereq_id: String = str(prereq_var)
			if prereq_id.is_empty() or not node_by_id.has(prereq_id):
				continue
			var prereq_node: Button = node_by_id[prereq_id]
			assert_true(prereq_node.position.y > child_node.position.y, "Tree should render prerequisite nodes below dependent nodes (bottom-up)")
