extends GutTest


func test_settings_surface_exposes_five_scalable_categories() -> void:
	var scene: PackedScene = load("res://scenes/shared/settings_panel.tscn")
	var panel: SettingsPanel = scene.instantiate() as SettingsPanel
	add_child_autofree(panel)
	await get_tree().process_frame

	assert_eq(panel.category_buttons.get_child_count(), 5)
	assert_eq(panel.category_title.text, Loc.t("ui.settings.category_audio"))
	assert_eq(panel.settings_list.get_child_count(), 4)

	panel._show_category(&"display")
	await get_tree().process_frame
	assert_eq(panel.category_title.text, Loc.t("ui.settings.category_display"))
	assert_eq(panel.settings_list.get_child_count(), 4)

	panel._show_category(&"controls")
	await get_tree().process_frame
	assert_eq(panel.settings_list.get_child_count(), 5)


func test_full_screen_and_battle_settings_reuse_the_shared_surface() -> void:
	var full_scene: PackedScene = load("res://scenes/meta/screens/settings_screen.tscn")
	var full_screen: SettingsScreen = full_scene.instantiate() as SettingsScreen
	add_child_autofree(full_screen)
	var full_panel: SettingsPanel = full_screen.get_node(
		"Margin/Layout/ContentCenter/SettingsPanel"
	) as SettingsPanel
	assert_not_null(full_panel)

	var battle_scene: PackedScene = load("res://scenes/battle/ui/pause_settings_panel.tscn")
	var battle_settings: PauseSettingsPanel = battle_scene.instantiate() as PauseSettingsPanel
	add_child_autofree(battle_settings)
	var battle_panel: SettingsPanel = battle_settings.get_node(
		"Center/Layout/SettingsPanel"
	) as SettingsPanel
	assert_not_null(battle_panel)
