extends GutTest


func test_encounter_results_is_context_agnostic_and_returns_to_campus() -> void:
	var script_text: String = _read("res://scripts/meta/screens/academy_activity_results.gd")
	assert_true(script_text.contains("class_name EncounterResults"))
	assert_true(script_text.contains("get_encounter_completion_summary"))
	assert_true(script_text.contains("consume_encounter_completion_summary"))
	assert_true(script_text.contains("SCENE_WALKABLE_ACADEMY_HUB"))
	assert_false(script_text.contains("get_academy_course_flow_state"))
	assert_false(script_text.contains("SCENE_ACADEMY_COURSE_FLOW"))


func _read(path: String) -> String:
	var file: FileAccess = FileAccess.open(path, FileAccess.READ)
	assert_not_null(file)
	var contents: String = file.get_as_text()
	file.close()
	return contents
