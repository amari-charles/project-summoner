extends GutTest


func test_generic_encounter_screens_exist_and_old_academic_flow_is_not_reachable() -> void:
	assert_true(ResourceLoader.exists(SceneManager.SCENE_ENCOUNTER_PREPARATION))
	assert_true(ResourceLoader.exists(SceneManager.SCENE_POST_BATTLE_RESULTS))
	var hub_script: String = _read("res://scripts/meta/screens/walkable_academy_hub.gd")
	assert_false(hub_script.contains("SCENE_ACADEMY_CLASS_HALL"))
	assert_false(hub_script.contains("DESTINATION_CLASS_HALL"))


func test_preparation_uses_generic_encounter_contracts() -> void:
	var script_text: String = _read("res://scripts/meta/screens/encounter_preparation.gd")
	var collection_script: String = _read("res://scripts/meta/screens/collection_screen.gd")
	var scene: PackedScene = load(SceneManager.SCENE_ENCOUNTER_PREPARATION)
	var preparation: EncounterPreparation = scene.instantiate() as EncounterPreparation
	assert_not_null(preparation)
	assert_true(script_text.contains("EncounterApi.get_preparation_state"))
	assert_true(script_text.contains("EncounterApi.resolve_battle_config"))
	assert_true(script_text.contains("configure_encounter_battle"))
	assert_true(collection_script.contains("EncounterApi.update_loadout"))
	assert_true(collection_script.contains("EncounterApi.fill_loadout_from_deck"))
	assert_true(script_text.contains("EncounterApi.save_loadout_to_deck"))
	assert_false(script_text.contains("get_academy_activity"))
	assert_false(script_text.contains("configure_academy_battle"))
	assert_not_null(preparation.find_child("ModalPanel", true, false))
	assert_not_null(preparation.find_child("CollectionOverlay", true, false))
	assert_not_null(preparation.find_child("StartButton", true, false))
	preparation.free()


func test_preparation_reuses_collection_overlay_for_deck_editing() -> void:
	var preparation_scene: String = _read("res://scenes/meta/screens/encounter_preparation.tscn")
	var collection_scene: String = _read("res://scenes/meta/screens/collection_screen.tscn")
	var preparation_script: String = _read("res://scripts/meta/screens/encounter_preparation.gd")
	var collection_script: String = _read("res://scripts/meta/screens/collection_screen.gd")
	assert_true(preparation_scene.contains("collection_screen.tscn"))
	assert_false(preparation_scene.contains("deck_editor_panel.tscn"))
	assert_true(collection_scene.contains("deck_editor_panel.tscn"))
	assert_true(preparation_script.contains("open_encounter_loadout"))
	assert_true(preparation_script.contains("open_collection"))
	assert_false(preparation_script.contains("_editing_deck"))
	assert_false(collection_script.contains("_encounter_loadout_mode"))
	assert_true(collection_scene.contains("LoadoutErrorDialog"))


func test_activity_deck_sources_hide_saved_deck_management_actions() -> void:
	var item_scene: PackedScene = load("res://scenes/meta/components/deck_list_item.tscn")
	var item: DeckListItem = item_scene.instantiate() as DeckListItem
	add_child_autofree(item)
	await get_tree().process_frame
	item.setup({
		"id": "test_deck",
		"name": "Test Deck",
		"management_enabled": false,
	})
	assert_false(item.star_button.visible)
	assert_false(item.rename_button.visible)
	assert_false(item.delete_button.visible)


func _read(path: String) -> String:
	var file: FileAccess = FileAccess.open(path, FileAccess.READ)
	assert_not_null(file)
	var contents: String = file.get_as_text()
	file.close()
	return contents
