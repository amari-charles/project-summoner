extends GutTest


func test_legacy_summoner_card_component_is_retired() -> void:
	assert_false(FileAccess.file_exists("res://scenes/meta/components/summoner_card.tscn"))
	assert_false(FileAccess.file_exists("res://scripts/meta/components/summoner_card.gd"))
	var switch_source: String = FileAccess.get_file_as_string(
		"res://scripts/meta/screens/summoner_switch_screen.gd"
	)
	assert_false(switch_source.contains("SummonerCard"))
	assert_true(switch_source.contains("SummonerRosterItem"))


func test_summoner_screens_use_shared_readable_background() -> void:
	var profile_source: String = FileAccess.get_file_as_string(
		"res://scripts/meta/screens/summoner_screen.gd"
	)
	var switch_source: String = FileAccess.get_file_as_string(
		"res://scripts/meta/screens/summoner_switch_screen.gd"
	)
	assert_true(profile_source.contains("background.color = GameColorPalette.UI_BACKGROUND"))
	assert_true(switch_source.contains("background.color = GameColorPalette.UI_BACKGROUND"))

	var profile_scene: String = FileAccess.get_file_as_string(
		"res://scenes/meta/screens/summoner_screen.tscn"
	)
	var switch_scene: String = FileAccess.get_file_as_string(
		"res://scenes/meta/screens/summoner_switch_screen.tscn"
	)
	assert_false(profile_scene.contains("element_energy_waves"))
	assert_false(switch_scene.contains("element_energy_waves"))
	assert_false(profile_scene.contains("EquipmentHeader"))


func test_summoner_switch_uses_a_scrollable_roster_instead_of_a_carousel() -> void:
	var packed_scene: PackedScene = load("res://scenes/meta/screens/summoner_switch_screen.tscn")
	var screen: Control = packed_scene.instantiate() as Control
	assert_not_null(screen)
	assert_not_null(screen.find_child("RosterScroll", true, false))
	assert_not_null(screen.find_child("SummonerList", true, false))
	assert_not_null(screen.find_child("ConfirmButton", true, false))
	assert_null(screen.find_child("CardArea", true, false))
	assert_null(screen.find_child("LeftArrow", true, false))
	assert_null(screen.find_child("RightArrow", true, false))
	screen.free()
