extends Control
class_name AcademyHub

@onready var title_label: Label = %TitleLabel
@onready var status_label: Label = %StatusLabel
@onready var campus_locations: GridContainer = %CampusLocations
@onready var settings_button: Button = %SettingsButton

const COLOR_CAMPUS_BG: Color = Color(0.075, 0.08, 0.095, 1.0)
const COLOR_LOCATION: Color = Color(0.16, 0.13, 0.1, 1.0)
const COLOR_LOCATION_DISABLED: Color = Color(0.10, 0.105, 0.115, 1.0)
const COLOR_BORDER: Color = Color(0.55, 0.42, 0.22, 1.0)
const COLOR_TEXT_MUTED: Color = Color(0.74, 0.70, 0.62, 1.0)

func _ready() -> void:
	if SummonerSelectionApi.get_active_summoner_id().is_empty():
		call_deferred("_redirect_to_summoner_selection")
		return

	title_label.text = Loc.t("academy.hub.title")
	settings_button.text = Loc.t("ui.nav.settings")
	settings_button.pressed.connect(_on_settings_pressed)
	_refresh()

func _refresh() -> void:
	var progress: Dictionary = CampaignApi.get_academy_progress()
	var year: int = SafeTypeUtils.int_val(progress.get("current_year"), 1)
	var semester: int = SafeTypeUtils.int_val(progress.get("current_semester"), 1)
	var enrollments: int = SafeTypeUtils.int_val(progress.get("remaining_enrollments"), 0)
	status_label.text = Loc.t(
		"academy.hub.status",
		{"year": year, "semester": semester, "enrollments": enrollments}
	)

	_clear_children(campus_locations)
	_add_location(
		"academy.campus.class_hall.name",
		"academy.campus.class_hall.description",
		false,
		func() -> void:
			SceneManager.transition_to(SceneManager.SCENE_ACADEMY_CLASS_HALL)
	)
	_add_location(
		"academy.campus.shop.name",
		"academy.campus.shop.description",
		false,
		func() -> void:
			NavigationContext.push_return(SceneManager.SCENE_CAMPAIGN_MAP)
			SceneManager.transition_to(SceneManager.SCENE_SHOP_SCREEN)
	)
	_add_location(
		"academy.campus.mission_hall.name",
		"academy.campus.mission_hall.description",
		true,
		func() -> void:
			pass
	)
	_add_location(
		"academy.campus.transcript.name",
		"academy.campus.transcript.description",
		true,
		func() -> void:
			pass
	)
	_add_location(
		"academy.campus.online.name",
		"academy.campus.online.description",
		false,
		func() -> void:
			NavigationContext.push_return(SceneManager.SCENE_CAMPAIGN_MAP)
			SceneManager.transition_to(SceneManager.SCENE_ONLINE)
	)
	_add_location(
		"ui.nav.collection",
		"academy.campus.collection.description",
		false,
		func() -> void:
			NavigationContext.push_return(SceneManager.SCENE_CAMPAIGN_MAP)
			SceneManager.transition_to(SceneManager.SCENE_COLLECTION_SCREEN)
	)

func _add_location(name_key: String, description_key: String, disabled: bool, action: Callable) -> void:
	var panel: PanelContainer = PanelContainer.new()
	panel.custom_minimum_size = Vector2(360, 170)
	panel.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	panel.add_theme_stylebox_override(
		"panel",
		_panel_style(COLOR_LOCATION_DISABLED if disabled else COLOR_LOCATION)
	)
	campus_locations.add_child(panel)

	var margin: MarginContainer = MarginContainer.new()
	margin.add_theme_constant_override("margin_left", 18)
	margin.add_theme_constant_override("margin_top", 16)
	margin.add_theme_constant_override("margin_right", 18)
	margin.add_theme_constant_override("margin_bottom", 16)
	panel.add_child(margin)

	var root: VBoxContainer = VBoxContainer.new()
	root.add_theme_constant_override("separation", 8)
	margin.add_child(root)

	var title: Label = Label.new()
	title.text = Loc.t(name_key)
	title.add_theme_font_size_override("font_size", 26)
	title.add_theme_color_override("font_color", COLOR_BORDER)
	root.add_child(title)

	var description: Label = Label.new()
	description.text = Loc.t(description_key)
	description.add_theme_font_size_override("font_size", 16)
	description.add_theme_color_override("font_color", COLOR_TEXT_MUTED)
	description.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	root.add_child(description)

	var spacer: Control = Control.new()
	spacer.size_flags_vertical = Control.SIZE_EXPAND_FILL
	root.add_child(spacer)

	var button: Button = Button.new()
	button.text = Loc.t("academy.campus.coming_soon") if disabled else Loc.t("academy.campus.enter")
	button.disabled = disabled
	button.pressed.connect(action)
	root.add_child(button)

func _panel_style(bg: Color) -> StyleBoxFlat:
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = bg
	style.border_color = COLOR_BORDER
	style.set_border_width_all(2)
	style.set_corner_radius_all(8)
	return style

func _clear_children(node: Node) -> void:
	for child: Node in node.get_children():
		child.queue_free()

func _on_settings_pressed() -> void:
	NavigationContext.push_return(SceneManager.SCENE_CAMPAIGN_MAP)
	SceneManager.transition_to(SceneManager.SCENE_SETTINGS)

func _redirect_to_summoner_selection() -> void:
	SceneManager.transition_to(SceneManager.SCENE_SUMMONER_SELECTION)
