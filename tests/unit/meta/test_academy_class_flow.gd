extends GutTest


func test_generic_encounter_screens_exist_and_old_course_flow_is_not_reachable() -> void:
	assert_true(ResourceLoader.exists(SceneManager.SCENE_ENCOUNTER_PREPARATION))
	assert_true(ResourceLoader.exists(SceneManager.SCENE_ENCOUNTER_RESULTS))
	var hub_script: String = _read("res://scripts/meta/screens/walkable_academy_hub.gd")
	assert_false(hub_script.contains("SCENE_ACADEMY_CLASS_HALL"))
	assert_false(hub_script.contains("DESTINATION_CLASS_HALL"))


func test_preparation_uses_generic_encounter_contracts() -> void:
	var script_text: String = _read("res://scripts/meta/screens/academy_activity_preparation.gd")
	var scene: PackedScene = load(SceneManager.SCENE_ENCOUNTER_PREPARATION)
	var preparation: EncounterPreparation = scene.instantiate() as EncounterPreparation
	assert_not_null(preparation)
	assert_true(script_text.contains("get_encounter_preparation_state"))
	assert_true(script_text.contains("resolve_encounter_battle_config"))
	assert_true(script_text.contains("configure_encounter_battle"))
	assert_true(script_text.contains("update_encounter_loadout"))
	assert_true(script_text.contains("fill_encounter_loadout_from_deck"))
	assert_true(script_text.contains("save_encounter_loadout_to_deck"))
	assert_false(script_text.contains("get_academy_activity"))
	assert_false(script_text.contains("configure_academy_battle"))
	assert_not_null(preparation.find_child("ModalPanel", true, false))
	assert_not_null(preparation.find_child("DeckEditorPanel", true, false))
	assert_not_null(preparation.find_child("StartButton", true, false))
	preparation.free()


func test_preparation_and_collection_share_deck_editor_interactions() -> void:
	var preparation_scene: String = _read("res://scenes/meta/screens/academy_activity_preparation.tscn")
	var collection_scene: String = _read("res://scenes/meta/screens/collection_screen.tscn")
	assert_true(preparation_scene.contains("deck_editor_panel.tscn"))
	assert_true(collection_scene.contains("deck_editor_panel.tscn"))
	var editor_scene: PackedScene = load("res://scenes/meta/components/deck_editor_panel.tscn")
	var editor: DeckEditorPanel = editor_scene.instantiate() as DeckEditorPanel
	add_child_autofree(editor)
	editor.set_available_columns(7)
	editor.set_active_deck("Active Deck", [], DeckConstants.MAX_DECK_SIZE, true)
	assert_eq(editor.available_cards.columns, 7)
	assert_eq(editor.active_deck_count.text, "0/12")


func _read(path: String) -> String:
	var file: FileAccess = FileAccess.open(path, FileAccess.READ)
	assert_not_null(file)
	var contents: String = file.get_as_text()
	file.close()
	return contents
