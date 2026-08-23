extends GutTest


func test_legacy_summoner_card_component_is_retired() -> void:
	assert_false(FileAccess.file_exists("res://scenes/meta/components/summoner_card.tscn"))
	assert_false(FileAccess.file_exists("res://scripts/meta/components/summoner_card.gd"))
	var switch_source: String = FileAccess.get_file_as_string(
		"res://scripts/meta/screens/summoner_switch_screen.gd"
	)
	assert_false(switch_source.contains("SummonerCard"))
	assert_true(switch_source.contains("SummonerRosterItem"))


func test_summoner_profile_uses_a_fixed_overlay_and_switch_keeps_a_readable_background() -> void:
	var profile_source: String = FileAccess.get_file_as_string(
		"res://scripts/meta/screens/summoner_screen.gd"
	)
	var switch_source: String = FileAccess.get_file_as_string(
		"res://scripts/meta/screens/summoner_switch_screen.gd"
	)
	assert_true(profile_source.contains("style.bg_color = GameColorPalette.UI_BACKGROUND"))
	assert_true(profile_source.contains("func open_profile"))
	assert_true(switch_source.contains("background.color = GameColorPalette.UI_BACKGROUND"))

	var profile_scene: String = FileAccess.get_file_as_string(
		"res://scenes/meta/screens/summoner_screen.tscn"
	)
	var switch_scene: String = FileAccess.get_file_as_string(
		"res://scenes/meta/screens/summoner_switch_screen.tscn"
	)
	assert_false(profile_scene.contains("element_energy_waves"))
	assert_true(profile_scene.contains("custom_minimum_size = Vector2(1200, 720)"))
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


func test_summoner_profile_labels_traits_and_opens_an_owned_trait() -> void:
	var packed_scene: PackedScene = load("res://scenes/meta/screens/summoner_screen.tscn")
	var screen: SummonerScreen = packed_scene.instantiate() as SummonerScreen
	add_child_autofree(screen)
	await get_tree().process_frame

	var traits_header: Label = screen.find_child("TraitsHeader", true, false) as Label
	var stats_header: Label = screen.find_child("StatsHeader", true, false) as Label
	var traits_container: HFlowContainer = (
		screen.find_child("TraitsContainer", true, false) as HFlowContainer
	)
	var trait_overlay: TraitDevelopmentOverlay = (
		screen.find_child("TraitDevelopmentOverlay", true, false) as TraitDevelopmentOverlay
	)
	assert_eq(stats_header.text, "Stats")
	assert_gte(stats_header.custom_minimum_size.y, 28.0)
	assert_eq(stats_header.get_theme_color("font_color"), GameColorPalette.TEXT_PRIMARY)
	assert_eq(traits_header.text, "Traits")
	assert_gte(traits_header.custom_minimum_size.y, 28.0)
	assert_eq(traits_header.get_theme_color("font_color"), GameColorPalette.TEXT_PRIMARY)

	var trait_button: Button = null
	for child: Node in traits_container.get_children():
		if child is Button:
			trait_button = child as Button
			break
	assert_not_null(trait_button)
	trait_button.pressed.emit()
	await get_tree().process_frame
	assert_true(trait_overlay.visible)
