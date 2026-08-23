extends GutTest


func test_generic_encounter_screens_exist_and_old_course_flow_is_not_reachable() -> void:
	assert_true(ResourceLoader.exists(SceneManager.SCENE_ENCOUNTER_PREPARATION))
	assert_true(ResourceLoader.exists(SceneManager.SCENE_ENCOUNTER_RESULTS))
	var hub_script: String = _read("res://scripts/meta/screens/walkable_academy_hub.gd")
	assert_false(hub_script.contains("SCENE_ACADEMY_CLASS_HALL"))
	assert_false(hub_script.contains("DESTINATION_CLASS_HALL"))


func test_preparation_uses_generic_encounter_contracts() -> void:
	var script_text: String = _read("res://scripts/meta/screens/academy_activity_preparation.gd")
	var collection_script: String = _read("res://scripts/meta/screens/collection_screen.gd")
	var scene: PackedScene = load(SceneManager.SCENE_ENCOUNTER_PREPARATION)
	var preparation: EncounterPreparation = scene.instantiate() as EncounterPreparation
	assert_not_null(preparation)
	assert_true(script_text.contains("get_encounter_preparation_state"))
	assert_true(script_text.contains("resolve_encounter_battle_config"))
	assert_true(script_text.contains("configure_encounter_battle"))
	assert_true(collection_script.contains("update_encounter_loadout"))
	assert_true(collection_script.contains("fill_encounter_loadout_from_deck"))
	assert_true(script_text.contains("save_encounter_loadout_to_deck"))
	assert_false(script_text.contains("get_academy_activity"))
	assert_false(script_text.contains("configure_academy_battle"))
	assert_not_null(preparation.find_child("ModalPanel", true, false))
	assert_not_null(preparation.find_child("CollectionOverlay", true, false))
	assert_not_null(preparation.find_child("StartButton", true, false))
	preparation.free()


func test_preparation_reuses_collection_overlay_for_deck_editing() -> void:
	var preparation_scene: String = _read("res://scenes/meta/screens/academy_activity_preparation.tscn")
	var collection_scene: String = _read("res://scenes/meta/screens/collection_screen.tscn")
	var preparation_script: String = _read("res://scripts/meta/screens/academy_activity_preparation.gd")
	assert_true(preparation_scene.contains("collection_screen.tscn"))
	assert_false(preparation_scene.contains("deck_editor_panel.tscn"))
	assert_true(collection_scene.contains("deck_editor_panel.tscn"))
	assert_true(preparation_script.contains("open_encounter_loadout"))
	assert_true(preparation_script.contains("open_collection"))


func _read(path: String) -> String:
	var file: FileAccess = FileAccess.open(path, FileAccess.READ)
	assert_not_null(file)
	var contents: String = file.get_as_text()
	file.close()
	return contents
