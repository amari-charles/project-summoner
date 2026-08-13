extends GutTest

func test_canonical_academy_screens_exist_and_legacy_path_is_removed() -> void:
	assert_true(ResourceLoader.exists("res://scenes/meta/screens/academy_course_flow.tscn"))
	assert_true(ResourceLoader.exists("res://scenes/meta/screens/academy_activity_preparation.tscn"))
	assert_true(ResourceLoader.exists("res://scenes/meta/screens/academy_activity_results.tscn"))
	assert_false(FileAccess.file_exists("res://scenes/meta/screens/academy_course_path.tscn"))

func test_academy_content_has_no_text_only_nodes_or_legacy_loadout_fields() -> void:
	var file: FileAccess = FileAccess.open("res://data/academy/courses.json", FileAccess.READ)
	assert_not_null(file)
	var text: String = file.get_as_text()
	file.close()
	assert_false(text.contains("text_only"))
	assert_false(text.contains("loaner_player_deck"))
	assert_false(text.contains("fixed_class_deck"))
	assert_false(text.contains("additional_loaner_cards"))
	assert_true(text.contains('"loadout"'))

func test_preparation_owns_launch_and_does_not_detour_to_collection() -> void:
	var script_text: String = _read("res://scripts/meta/screens/academy_activity_preparation.gd")
	assert_true(script_text.contains("resolve_academy_activity_battle_config"))
	assert_true(script_text.contains("configure_academy_battle"))
	assert_false(script_text.contains("SCENE_COLLECTION_SCREEN"))

func test_results_owns_pending_reward_choice() -> void:
	var script_text: String = _read("res://scripts/meta/screens/academy_activity_results.gd")
	assert_true(script_text.contains("claim_academy_reward"))
	assert_true(script_text.contains('Loc.t("academy.flow.earned_now")'))
	assert_true(script_text.contains('Loc.t("academy.flow.course_progress")'))

func test_course_and_preparation_use_accessible_back_navigation() -> void:
	var course_script: String = _read("res://scripts/meta/screens/academy_course_flow.gd")
	var preparation_script: String = _read("res://scripts/meta/screens/academy_activity_preparation.gd")
	assert_true(course_script.contains("back_button.accessibility_name"))
	assert_true(course_script.contains("SCENE_ACADEMY_CLASS_HALL"))
	assert_true(preparation_script.contains("back_button.accessibility_name"))
	assert_true(preparation_script.contains("SCENE_ACADEMY_COURSE_FLOW"))

func _read(path: String) -> String:
	var file: FileAccess = FileAccess.open(path, FileAccess.READ)
	assert_not_null(file)
	var text: String = file.get_as_text()
	file.close()
	return text
