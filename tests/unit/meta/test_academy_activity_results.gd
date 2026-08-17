extends GutTest


func test_encounter_results_is_context_agnostic_and_returns_to_campus() -> void:
	var script_text: String = _read("res://scripts/meta/screens/academy_activity_results.gd")
	assert_true(script_text.contains("class_name EncounterResults"))
	assert_true(script_text.contains("get_encounter_completion_summary"))
	assert_true(script_text.contains("consume_encounter_completion_summary"))
	assert_false(script_text.contains("CardWidgetScene"))
	assert_false(script_text.contains("_render_reward_reveals"))
	assert_true(script_text.contains("SCENE_WALKABLE_ACADEMY_HUB"))
	assert_false(script_text.contains("get_academy_course_flow_state"))
	assert_false(script_text.contains("SCENE_ACADEMY_COURSE_FLOW"))
	var packed_scene: PackedScene = load("res://scenes/meta/screens/academy_activity_results.tscn")
	var results: EncounterResults = packed_scene.instantiate() as EncounterResults
	assert_not_null(results.get_node_or_null("Center/Root/TitleLabel"))
	assert_not_null(results.get_node_or_null("Center/Root/ContinueButton"))
	assert_null(results.get_node_or_null("Center/Root/EarnedLabel"))
	assert_null(results.get_node_or_null("Center/Root/ProgressLabel"))
	results.free()


func test_generic_reward_modal_owns_visual_reward_presentation() -> void:
	var script_text: String = _read("res://scripts/meta/components/reward_grant_modal.gd")
	assert_true(script_text.contains("class_name RewardGrantModal"))
	assert_true(script_text.contains("CardWidgetScene"))
	assert_true(script_text.contains("func present(grants: Array"))
	var packed_scene: PackedScene = load("res://scenes/meta/components/reward_grant_modal.tscn")
	var modal: RewardGrantModal = packed_scene.instantiate() as RewardGrantModal
	assert_not_null(modal.get_node_or_null("Center/Panel/Margin/Content/Rewards"))
	assert_not_null(modal.get_node_or_null("Center/Panel/Margin/Content/ContinueButton"))
	modal.free()


func _read(path: String) -> String:
	var file: FileAccess = FileAccess.open(path, FileAccess.READ)
	assert_not_null(file)
	var contents: String = file.get_as_text()
	file.close()
	return contents
