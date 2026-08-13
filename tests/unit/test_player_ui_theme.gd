extends GutTest

const PLAYER_THEME_PATH: String = "res://resources/visual/base_theme.tres"


func test_player_theme_is_configured_globally() -> void:
	var project_theme_path: String = str(ProjectSettings.get_setting("gui/theme/custom", ""))
	assert_eq(project_theme_path, PLAYER_THEME_PATH)


func test_player_theme_uses_shared_warm_neutral_defaults() -> void:
	var theme: Theme = load(PLAYER_THEME_PATH) as Theme
	assert_not_null(theme)
	assert_true(theme.get_color("font_color", "Label").is_equal_approx(GameColorPalette.TEXT_PRIMARY))
	assert_true(theme.get_color("font_color", "Button").is_equal_approx(GameColorPalette.TEXT_PRIMARY))

	var panel_style: StyleBoxFlat = theme.get_stylebox("panel", "Panel") as StyleBoxFlat
	var button_style: StyleBoxFlat = theme.get_stylebox("normal", "Button") as StyleBoxFlat
	assert_not_null(panel_style)
	assert_not_null(button_style)
	assert_true(panel_style.bg_color.is_equal_approx(GameColorPalette.UI_SURFACE))
	assert_true(button_style.bg_color.is_equal_approx(GameColorPalette.BUTTON_NORMAL))
	assert_true(button_style.border_color.is_equal_approx(GameColorPalette.UI_BORDER))


func test_button_factory_uses_palette_for_all_interaction_states() -> void:
	assert_eq(ButtonStyleFactory.create_primary_normal().bg_color, GameColorPalette.BUTTON_PRIMARY_BG)
	assert_eq(ButtonStyleFactory.create_primary_hover().bg_color, GameColorPalette.BUTTON_PRIMARY_BG_HOVER)
	assert_eq(ButtonStyleFactory.create_primary_pressed().bg_color, GameColorPalette.BUTTON_PRIMARY_BG_PRESSED)
	assert_eq(ButtonStyleFactory.create_primary_disabled().bg_color, GameColorPalette.BUTTON_DISABLED)
	assert_eq(ButtonStyleFactory.create_secondary_normal().bg_color, GameColorPalette.BUTTON_SECONDARY_BG)
	assert_eq(ButtonStyleFactory.create_danger_normal().bg_color, GameColorPalette.BUTTON_DANGER_BG)
