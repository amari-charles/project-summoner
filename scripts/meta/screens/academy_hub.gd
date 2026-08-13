extends Control
class_name AcademyHub

const SummonerIconWidgetScene: PackedScene = preload("res://scenes/meta/components/summoner_icon_widget.tscn")

@onready var campus_locations: Control = %CampusLocations
@onready var settings_button: Button = %SettingsButton

const SUMMONER_ICON_SIZE: float = 86.0
const SUMMONER_ICON_MARGIN: float = 18.0
const LOCATION_BUTTON_SIZE: Vector2 = Vector2(210, 54)
const COLOR_LOCATION: Color = Color(0.898, 0.882, 0.851, 0.94)
const COLOR_LOCATION_HOVER: Color = Color(0.937, 0.922, 0.894, 0.98)
const COLOR_LOCATION_PRESSED: Color = Color(0.788, 0.741, 0.651, 0.98)
const COLOR_LOCATION_DISABLED: Color = Color(0.757, 0.741, 0.706, 0.88)
const COLOR_BORDER: Color = Color(0.604, 0.455, 0.176, 1.0)
const COLOR_TEXT: Color = Color(0.145, 0.137, 0.122, 1.0)
const COLOR_TEXT_DISABLED: Color = Color(0.549, 0.529, 0.498, 1.0)

var summoner_icon: SummonerIconWidget = null

func _ready() -> void:
	if SummonerSelectionApi.get_active_summoner_id().is_empty():
		call_deferred("_redirect_to_summoner_selection")
		return

	settings_button.text = Loc.t("ui.nav.settings")
	settings_button.pressed.connect(_on_settings_pressed)
	_setup_summoner_icon()
	_refresh()

func _refresh() -> void:
	_clear_children(campus_locations)
	_add_location(
		"academy.campus.class_hall.name",
		"academy.campus.class_hall.description",
		false,
		Vector2(0.50, 0.33),
		func() -> void:
			SceneManager.transition_to(SceneManager.SCENE_ACADEMY_CLASS_HALL)
	)
	_add_location(
		"academy.campus.shop.name",
		"academy.campus.shop.description",
		false,
		Vector2(0.78, 0.47),
		func() -> void:
			NavigationContext.push_return(SceneManager.SCENE_CAMPAIGN_MAP)
			SceneManager.transition_to(SceneManager.SCENE_SHOP_SCREEN)
	)
	_add_location(
		"academy.campus.mission_hall.name",
		"academy.campus.mission_hall.description",
		false,
		Vector2(0.25, 0.74),
		func() -> void:
			NavigationContext.push_return(SceneManager.SCENE_CAMPAIGN_MAP)
			SceneManager.transition_to(SceneManager.SCENE_SPECIAL_EVENTS)
	)
	_add_location(
		"academy.campus.dorms.name",
		"academy.campus.dorms.description",
		false,
		Vector2(0.22, 0.46),
		func() -> void:
			NavigationContext.push_return(SceneManager.SCENE_CAMPAIGN_MAP)
			SceneManager.transition_to(SceneManager.SCENE_COLLECTION_SCREEN)
	)
	_add_location(
		"academy.campus.online.name",
		"academy.campus.online.description",
		false,
		Vector2(0.76, 0.74),
		func() -> void:
			NavigationContext.push_return(SceneManager.SCENE_CAMPAIGN_MAP)
			SceneManager.transition_to(SceneManager.SCENE_ONLINE)
	)

func _add_location(
	name_key: String,
	description_key: String,
	disabled: bool,
	normalized_position: Vector2,
	action: Callable
) -> void:
	var button: Button = Button.new()
	button.text = Loc.t(name_key)
	button.tooltip_text = Loc.t(description_key)
	button.disabled = disabled
	button.custom_minimum_size = LOCATION_BUTTON_SIZE
	button.size = LOCATION_BUTTON_SIZE
	button.anchor_left = normalized_position.x
	button.anchor_right = normalized_position.x
	button.anchor_top = normalized_position.y
	button.anchor_bottom = normalized_position.y
	button.offset_left = -LOCATION_BUTTON_SIZE.x * 0.5
	button.offset_right = LOCATION_BUTTON_SIZE.x * 0.5
	button.offset_top = -LOCATION_BUTTON_SIZE.y * 0.5
	button.offset_bottom = LOCATION_BUTTON_SIZE.y * 0.5
	button.add_theme_font_size_override("font_size", 20)
	button.add_theme_color_override("font_color", COLOR_TEXT)
	button.add_theme_color_override("font_hover_color", COLOR_TEXT)
	button.add_theme_color_override("font_pressed_color", COLOR_TEXT)
	button.add_theme_color_override("font_disabled_color", COLOR_TEXT_DISABLED)
	button.add_theme_stylebox_override("normal", _button_style(COLOR_LOCATION, COLOR_BORDER, 2))
	button.add_theme_stylebox_override("hover", _button_style(COLOR_LOCATION_HOVER, COLOR_BORDER, 3))
	button.add_theme_stylebox_override("pressed", _button_style(COLOR_LOCATION_PRESSED, COLOR_BORDER, 3))
	button.add_theme_stylebox_override("disabled", _button_style(COLOR_LOCATION_DISABLED, GameColorPalette.UI_BORDER, 1))
	button.pressed.connect(action)
	campus_locations.add_child(button)

func _button_style(bg: Color, border: Color, border_width: int) -> StyleBoxFlat:
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = bg
	style.border_color = border
	style.set_border_width_all(border_width)
	style.set_corner_radius_all(7)
	style.shadow_color = GameColorPalette.BUTTON_SHADOW
	style.shadow_size = 8
	style.shadow_offset = Vector2(0, 3)
	return style

func _clear_children(node: Node) -> void:
	for child: Node in node.get_children():
		child.queue_free()

func _on_settings_pressed() -> void:
	NavigationContext.push_return(SceneManager.SCENE_CAMPAIGN_MAP)
	SceneManager.transition_to(SceneManager.SCENE_SETTINGS)

func _setup_summoner_icon() -> void:
	summoner_icon = SummonerIconWidgetScene.instantiate()
	add_child(summoner_icon)
	summoner_icon.anchor_left = 0.0
	summoner_icon.anchor_right = 0.0
	summoner_icon.anchor_top = 0.0
	summoner_icon.anchor_bottom = 0.0
	summoner_icon.offset_left = SUMMONER_ICON_MARGIN
	summoner_icon.offset_right = SUMMONER_ICON_MARGIN + SUMMONER_ICON_SIZE
	summoner_icon.offset_top = SUMMONER_ICON_MARGIN
	summoner_icon.offset_bottom = SUMMONER_ICON_MARGIN + SUMMONER_ICON_SIZE
	summoner_icon.z_index = 10
	summoner_icon.icon_clicked.connect(_on_summoner_icon_clicked)

func _on_summoner_icon_clicked() -> void:
	NavigationContext.push_return(SceneManager.SCENE_CAMPAIGN_MAP)
	SceneManager.transition_to(SceneManager.SCENE_SUMMONER_SCREEN)

func _redirect_to_summoner_selection() -> void:
	SceneManager.transition_to(SceneManager.SCENE_SUMMONER_SELECTION)
