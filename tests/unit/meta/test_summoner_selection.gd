extends GutTest


func test_starting_roster_keeps_five_panel_layout_with_neutral_placeholders() -> void:
	var packed_scene: PackedScene = load("res://scenes/meta/screens/summoner_selection.tscn")
	var selection: SummonerSelectionScreen = packed_scene.instantiate() as SummonerSelectionScreen
	assert_not_null(selection)
	var choices: Node = selection.get_node("CenterContainer/VBoxContainer/SummonerContainer")
	assert_eq(choices.get_child_count(), 5)
	for choice: Node in choices.get_children():
		assert_not_null(choice.find_child("CharacterPlaceholder", true, false))
		assert_null(choice.find_child("SummonerDescription", true, false))
	assert_null(selection.find_child("HPLabel", true, false))
	assert_null(selection.find_child("ManaLabel", true, false))
	assert_null(selection.find_child("DetailPanel", true, false))
	assert_null(selection.find_child("ConfirmButton", true, false))
	selection.free()


func test_starting_selection_omits_traits_stats_and_summoner_cards() -> void:
	var script_text: String = _read("res://scripts/meta/screens/summoner_selection.gd")
	assert_false(script_text.contains("TraitCatalogApi.get_trait"))
	assert_false(script_text.contains("summoner.selection_identity_trait"))
	assert_false(script_text.contains("description_key"))
	assert_false(script_text.contains("selection_subtitle"))
	assert_false(script_text.contains("SummonerCard"))
	assert_false(script_text.contains("base_health"))
	assert_false(script_text.contains("max_mana"))
	assert_true(script_text.contains("SCENE_SUMMONER_REVEAL"))
	assert_true(script_text.contains("NAV_KEY_REVEAL_RESULT"))
	assert_true(script_text.contains("\"summoner_id\": final_summoner_id"))
	assert_true(script_text.contains("\"was_random\": chosen_random"))
	assert_true(FileAccess.file_exists("res://scenes/meta/modals/summoner_reveal.tscn"))
	assert_true(FileAccess.file_exists("res://scripts/meta/modals/summoner_reveal.gd"))


func test_post_selection_reveal_is_character_focused_for_direct_and_random_choices() -> void:
	var packed_scene: PackedScene = load("res://scenes/meta/modals/summoner_reveal.tscn")
	var reveal: Control = packed_scene.instantiate() as Control
	assert_not_null(reveal)
	assert_not_null(reveal.find_child("CharacterPlaceholder", true, false))
	assert_not_null(reveal.find_child("ElementLabel", true, false))
	assert_not_null(reveal.find_child("ContinueButton", true, false))
	assert_null(reveal.find_child("SummonerCard", true, false))
	assert_null(reveal.find_child("HPLabel", true, false))
	assert_null(reveal.find_child("ManaLabel", true, false))
	reveal.free()

	var reveal_source: String = _read("res://scripts/meta/modals/summoner_reveal.gd")
	assert_true(reveal_source.contains("ui.summoner_reveal.chosen_title"))
	assert_true(reveal_source.contains("ui.summoner_reveal.random_title"))
	assert_true(reveal_source.contains("Missing required selection result"))
	assert_false(reveal_source.contains("get_active_summoner_id"))
	assert_false(reveal_source.contains("SummonerCard"))


func _read(path: String) -> String:
	var file: FileAccess = FileAccess.open(path, FileAccess.READ)
	assert_not_null(file)
	var contents: String = file.get_as_text()
	file.close()
	return contents
