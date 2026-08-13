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
	var scene_text: String = _read("res://scenes/meta/screens/academy_activity_preparation.tscn")
	assert_true(script_text.contains("resolve_academy_activity_battle_config"))
	assert_true(script_text.contains("configure_academy_battle"))
	assert_false(script_text.contains("SCENE_COLLECTION_SCREEN"))
	assert_true(scene_text.contains('name="ModalPanel"'))
	assert_true(scene_text.contains('name="LoadoutScroll"'))
	assert_true(script_text.contains('mode != "Fixed"'))
	var scene: PackedScene = load("res://scenes/meta/screens/academy_activity_preparation.tscn")
	var preparation: Control = scene.instantiate() as Control
	assert_not_null(preparation)
	assert_not_null(preparation.find_child("ModalPanel", true, false))
	assert_not_null(preparation.find_child("EditDeckButton", true, false))
	assert_not_null(preparation.find_child("StartButton", true, false))
	preparation.free()

func test_preparation_edits_deck_inline_and_centers_start_action() -> void:
	var scene: PackedScene = load("res://scenes/meta/screens/academy_activity_preparation.tscn")
	var preparation: Control = scene.instantiate() as Control
	assert_not_null(preparation)
	var content: Control = preparation.find_child("Content", true, false)
	var edit_panel: Control = preparation.find_child("EditPanel", true, false)
	var footer: Control = preparation.find_child("Footer", true, false)
	assert_eq(edit_panel.get_parent(), content)
	assert_not_null(footer.get_node_or_null("LeftSpacer"))
	assert_not_null(footer.get_node_or_null("StartButton"))
	assert_not_null(footer.get_node_or_null("RightSpacer"))
	var script_text: String = _read("res://scripts/meta/screens/academy_activity_preparation.gd")
	assert_true(script_text.contains("info_panel.visible = not _editing_deck"))
	assert_true(script_text.contains("DecksApi.add_card_to_deck"))
	assert_true(script_text.contains("DecksApi.remove_card_from_deck"))
	preparation.free()

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

func test_course_flow_uses_icon_graph_and_guards_locked_activity_selection() -> void:
	var scene_text: String = _read("res://scenes/meta/screens/academy_course_flow.tscn")
	var course_script: String = _read("res://scripts/meta/screens/academy_course_flow.gd")
	var graph_script: String = _read("res://scripts/meta/components/academy_activity_graph.gd")
	assert_true(scene_text.contains("academy_activity_graph.gd"))
	assert_true(course_script.contains('activity.get("is_locked")'))
	assert_true(graph_script.contains('activity.get("prerequisites")'))
	assert_true(graph_script.contains("tooltip_text = activity_name"))
	assert_false(graph_script.contains("button.text = activity_name"))

func test_activity_graph_renders_linear_icon_nodes_and_locked_nodes_do_not_select() -> void:
	var graph: AcademyActivityGraph = AcademyActivityGraph.new()
	add_child_autofree(graph)
	graph.size = Vector2(1920.0, 1080.0)
	watch_signals(graph)
	graph.set_activities([
		{
			"id": "first",
			"label_key": "academy.activity.practice",
			"role": "Practice",
			"lifecycle_state": "Active",
			"prerequisites": [],
			"repeatable": true,
		},
		{
			"id": "second",
			"label_key": "academy.activity.assessment",
			"role": "Assessment",
			"lifecycle_state": "Locked",
			"prerequisites": ["first"],
			"repeatable": false,
		},
	])

	var first: Button = graph.get_node("Activity_first")
	var second: Button = graph.get_node("Activity_second")
	assert_eq(first.text, "")
	assert_eq(second.text, "")
	assert_eq(first.tooltip_text, Loc.t("academy.activity.practice"))
	assert_gt(second.position.x, first.position.x)

	first.pressed.emit()
	assert_signal_emitted_with_parameters(graph, "activity_selected", ["first"])
	second.pressed.emit()
	assert_signal_emit_count(graph, "activity_selected", 1)

func test_standard_deck_maximum_is_twelve_across_gdscript_ui() -> void:
	assert_eq(DeckConstants.MAX_DECK_SIZE, 12)
	assert_eq(DeckConstants.STARTER_DECK_AUTO_ADD_THRESHOLD, 12)
	var collection_script: String = _read("res://scripts/meta/screens/collection_screen.gd")
	assert_true(collection_script.contains("DeckConstants.MAX_DECK_SIZE"))

func _read(path: String) -> String:
	var file: FileAccess = FileAccess.open(path, FileAccess.READ)
	assert_not_null(file)
	var text: String = file.get_as_text()
	file.close()
	return text
