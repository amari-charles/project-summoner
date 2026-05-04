extends Control
class_name AcademyClassHall

@onready var title_label: Label = %TitleLabel
@onready var status_label: Label = %StatusLabel
@onready var exit_button: Button = %ExitButton
@onready var period_button: Button = %PeriodButton
@onready var view_status_label: Label = %ViewStatusLabel
@onready var board_title_label: Label = %BoardTitleLabel
@onready var advance_semester_button: Button = %AdvanceSemesterButton
@onready var open_classes_button: Button = %OpenClassesButton
@onready var my_classes_button: Button = %MyClassesButton
@onready var enrollment_summary_label: Label = %EnrollmentSummaryLabel
@onready var course_groups: VBoxContainer = %CourseGroups
@onready var detail_panel: PanelContainer = %DetailPanel
@onready var period_popup: PopupPanel = %PeriodPopup
@onready var period_picker_title: Label = %PeriodPickerTitle
@onready var period_options: GridContainer = %PeriodOptions
@onready var course_modal: PopupPanel = %CourseModal
@onready var course_modal_eyebrow_label: Label = %CourseModalEyebrowLabel
@onready var course_modal_title_label: Label = %CourseModalTitleLabel
@onready var course_modal_meta_label: Label = %CourseModalMetaLabel
@onready var course_modal_description_label: Label = %CourseModalDescriptionLabel
@onready var course_modal_rewards_label: Label = %CourseModalRewardsLabel
@onready var course_modal_activities_label: Label = %CourseModalActivitiesLabel
@onready var course_modal_close_button: Button = %CourseModalCloseButton
@onready var course_modal_action_button: Button = %CourseModalActionButton

const TOTAL_YEARS: int = 4
const TOTAL_SEMESTERS: int = 2
const COLOR_PANEL: Color = Color(0.13, 0.145, 0.17, 1.0)
const COLOR_PANEL_SELECTED: Color = Color(0.18, 0.215, 0.25, 1.0)
const COLOR_PANEL_LOCKED: Color = Color(0.105, 0.112, 0.125, 1.0)
const COLOR_ACCENT: Color = Color(0.82, 0.68, 0.36, 1.0)
const COLOR_TEXT_MUTED: Color = Color(0.72, 0.75, 0.78, 1.0)

var _current_year: int = 1
var _current_semester: int = 1
var _view_year: int = 1
var _view_semester: int = 1
var _courses: Array[Dictionary] = []
var _selected_course_id: String = ""
var _show_my_classes: bool = true
var _previewed_course: Dictionary = {}

func _ready() -> void:
	if SummonerSelectionApi.get_active_summoner_id().is_empty():
		call_deferred("_redirect_to_summoner_selection")
		return

	title_label.text = Loc.t("academy.class_hall.title")
	exit_button.text = Loc.t("academy.location.exit")
	advance_semester_button.text = Loc.t("academy.hub.advance_semester")
	period_picker_title.text = Loc.t("academy.hub.period_picker_title")
	open_classes_button.text = Loc.t("academy.class_hall.open_classes")
	my_classes_button.text = Loc.t("academy.class_hall.my_classes")
	course_modal_close_button.text = Loc.t("academy.course_modal.close")
	detail_panel.visible = false

	exit_button.pressed.connect(_on_exit_pressed)
	advance_semester_button.pressed.connect(_on_advance_semester_pressed)
	period_button.pressed.connect(_on_period_button_pressed)
	course_modal_close_button.pressed.connect(_hide_course_modal)
	course_modal_action_button.pressed.connect(_on_course_modal_action_pressed)
	open_classes_button.pressed.connect(func() -> void:
		_show_my_classes = false
		_selected_course_id = ""
		_refresh()
	)
	my_classes_button.pressed.connect(func() -> void:
		_show_my_classes = true
		_selected_course_id = ""
		_refresh()
	)

	if Campaign.has_signal("CampaignProgressChanged"):
		Campaign.connect("CampaignProgressChanged", _refresh)

	_refresh_from_current_progress()

func _refresh_from_current_progress() -> void:
	var progress: Dictionary = CampaignApi.get_academy_progress()
	_current_year = SafeTypeUtils.int_val(progress.get("current_year"), 1)
	_current_semester = SafeTypeUtils.int_val(progress.get("current_semester"), 1)
	_view_year = _current_year
	_view_semester = _current_semester
	_refresh()

func _refresh() -> void:
	var progress: Dictionary = CampaignApi.get_academy_progress()
	_current_year = SafeTypeUtils.int_val(progress.get("current_year"), 1)
	_current_semester = SafeTypeUtils.int_val(progress.get("current_semester"), 1)
	var enrollments: int = SafeTypeUtils.int_val(progress.get("remaining_enrollments"), 0)

	status_label.text = Loc.t(
		"academy.hub.status",
		{"year": _current_year, "semester": _current_semester, "enrollments": enrollments}
	)
	period_button.text = _period_button_text()
	view_status_label.text = _view_status_text()
	advance_semester_button.visible = _is_viewing_current_semester()
	board_title_label.text = Loc.t("academy.class_hall.my_classes") if _show_my_classes else Loc.t("academy.class_hall.open_classes")
	open_classes_button.button_pressed = not _show_my_classes
	my_classes_button.button_pressed = _show_my_classes
	enrollment_summary_label.text = Loc.t("academy.class_hall.approvals", {"count": enrollments})

	_load_view_courses()
	_render_course_groups()

func _render_period_picker() -> void:
	_clear_children(period_options)

	for year: int in range(1, TOTAL_YEARS + 1):
		for semester: int in range(1, TOTAL_SEMESTERS + 1):
			var button: Button = Button.new()
			button.custom_minimum_size = Vector2(220, 64)
			button.text = "%s\n%s" % [
				Loc.t("academy.hub.period_button", {"year": year, "semester": semester}),
				_period_relation_label(year, semester),
			]
			button.toggle_mode = true
			button.button_pressed = year == _view_year and semester == _view_semester
			button.alignment = HORIZONTAL_ALIGNMENT_LEFT
			button.pressed.connect(func() -> void:
				_view_year = year
				_view_semester = semester
				_selected_course_id = ""
				period_popup.hide()
				_refresh()
			)
			period_options.add_child(button)

func _load_view_courses() -> void:
	_courses.clear()
	for item: Variant in CampaignApi.get_academy_courses_for_semester(_view_year, _view_semester):
		var course: Dictionary = SafeTypeUtils.dict(item)
		if not course.is_empty():
			_courses.append(course)

func _render_course_groups() -> void:
	_clear_children(course_groups)

	var required: Array[Dictionary] = []
	var chosen: Array[Dictionary] = []
	var available: Array[Dictionary] = []
	var locked: Array[Dictionary] = []

	for course: Dictionary in _filtered_courses_for_active_tab():
		if SafeTypeUtils.bool_val(course.get("is_enrolled")) or SafeTypeUtils.bool_val(course.get("is_completed")):
			chosen.append(course)
		elif SafeTypeUtils.bool_val(course.get("is_required")):
			required.append(course)
		elif SafeTypeUtils.bool_val(course.get("is_available")):
			available.append(course)
		else:
			locked.append(course)

	if _show_my_classes:
		_add_course_group("academy.class_hall.my_classes", chosen)
	else:
		_add_course_group("academy.hub.group_required", required)
		_add_course_group("academy.hub.group_available", available)
		if not locked.is_empty():
			_add_course_group("academy.hub.group_locked", locked)

	if course_groups.get_child_count() == 0:
		var empty: Label = Label.new()
		empty.text = Loc.t("academy.class_hall.no_my_classes") if _show_my_classes else Loc.t("academy.class_hall.no_open_classes")
		empty.add_theme_font_size_override("font_size", 18)
		empty.add_theme_color_override("font_color", COLOR_TEXT_MUTED)
		course_groups.add_child(empty)

func _filtered_courses_for_active_tab() -> Array[Dictionary]:
	var filtered: Array[Dictionary] = []
	for course: Dictionary in _courses:
		var is_enrolled: bool = SafeTypeUtils.bool_val(course.get("is_enrolled"))
		var is_completed: bool = SafeTypeUtils.bool_val(course.get("is_completed"))
		if _show_my_classes and (is_enrolled or is_completed):
			filtered.append(course)
		elif not _show_my_classes and not is_enrolled and not is_completed:
			filtered.append(course)
	return filtered

func _add_course_group(title_key: String, courses: Array[Dictionary]) -> void:
	if courses.is_empty():
		return

	var group: VBoxContainer = VBoxContainer.new()
	group.add_theme_constant_override("separation", 8)

	var title: Label = Label.new()
	title.add_theme_font_size_override("font_size", 18)
	title.add_theme_color_override("font_color", COLOR_ACCENT)
	title.text = Loc.t(title_key)
	group.add_child(title)

	var grid: GridContainer = GridContainer.new()
	grid.columns = 3
	grid.add_theme_constant_override("h_separation", 10)
	grid.add_theme_constant_override("v_separation", 10)
	group.add_child(grid)

	for course: Dictionary in courses:
		grid.add_child(_build_course_card(course))

	course_groups.add_child(group)

func _build_course_card(course: Dictionary) -> Control:
	var course_id: String = SafeTypeUtils.string(course.get("id"))
	var is_selected: bool = course_id == _selected_course_id
	var is_locked: bool = not SafeTypeUtils.bool_val(course.get("is_available")) \
		and not SafeTypeUtils.bool_val(course.get("is_enrolled")) \
		and not SafeTypeUtils.bool_val(course.get("is_completed"))

	var panel: PanelContainer = PanelContainer.new()
	panel.custom_minimum_size = Vector2(230, 138)
	panel.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	panel.mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND
	panel.gui_input.connect(func(event: InputEvent) -> void:
		if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
			_activate_course(course)
	)
	panel.add_theme_stylebox_override(
		"panel",
		_panel_style(
			COLOR_PANEL_LOCKED if is_locked else (COLOR_PANEL_SELECTED if is_selected else COLOR_PANEL),
			COLOR_ACCENT if is_selected else Color(0.24, 0.26, 0.29, 1.0)
		)
	)

	var margin: MarginContainer = MarginContainer.new()
	margin.mouse_filter = Control.MOUSE_FILTER_IGNORE
	margin.add_theme_constant_override("margin_left", 12)
	margin.add_theme_constant_override("margin_top", 10)
	margin.add_theme_constant_override("margin_right", 12)
	margin.add_theme_constant_override("margin_bottom", 10)
	panel.add_child(margin)

	var root: VBoxContainer = VBoxContainer.new()
	root.mouse_filter = Control.MOUSE_FILTER_IGNORE
	root.add_theme_constant_override("separation", 7)
	margin.add_child(root)

	var top: HBoxContainer = HBoxContainer.new()
	top.mouse_filter = Control.MOUSE_FILTER_IGNORE
	root.add_child(top)

	var track: Label = Label.new()
	track.mouse_filter = Control.MOUSE_FILTER_IGNORE
	track.text = SafeTypeUtils.string(course.get("track"))
	track.add_theme_font_size_override("font_size", 12)
	track.add_theme_color_override("font_color", COLOR_ACCENT)
	track.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	top.add_child(track)

	var cost: Label = Label.new()
	cost.mouse_filter = Control.MOUSE_FILTER_IGNORE
	cost.text = Loc.t("academy.hub.cost_short", {"cost": SafeTypeUtils.int_val(course.get("enrollment_cost"), 1)})
	cost.add_theme_font_size_override("font_size", 12)
	cost.add_theme_color_override("font_color", COLOR_TEXT_MUTED)
	top.add_child(cost)

	var name: Label = Label.new()
	name.mouse_filter = Control.MOUSE_FILTER_IGNORE
	name.text = _course_name(course)
	name.add_theme_font_size_override("font_size", 17)
	name.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	root.add_child(name)

	var rewards: Label = Label.new()
	rewards.mouse_filter = Control.MOUSE_FILTER_IGNORE
	rewards.text = _compact_rewards(SafeTypeUtils.array(course.get("reward_previews")))
	rewards.add_theme_font_size_override("font_size", 13)
	rewards.add_theme_color_override("font_color", COLOR_TEXT_MUTED)
	rewards.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	root.add_child(rewards)

	var spacer: Control = Control.new()
	spacer.mouse_filter = Control.MOUSE_FILTER_IGNORE
	spacer.size_flags_vertical = Control.SIZE_EXPAND_FILL
	root.add_child(spacer)

	var state: Label = Label.new()
	state.mouse_filter = Control.MOUSE_FILTER_IGNORE
	state.text = _course_state_label(course)
	state.add_theme_font_size_override("font_size", 13)
	state.add_theme_color_override("font_color", COLOR_ACCENT if is_selected else COLOR_TEXT_MUTED)
	root.add_child(state)

	return panel

func _activate_course(course: Dictionary) -> void:
	_show_course_modal(course)

func _show_course_modal(course: Dictionary) -> void:
	_previewed_course = course.duplicate(true)
	var course_id: String = SafeTypeUtils.string(course.get("id"))
	var is_available: bool = SafeTypeUtils.bool_val(course.get("is_available"), false)
	var is_enrolled: bool = SafeTypeUtils.bool_val(course.get("is_enrolled"), false)
	var is_completed: bool = SafeTypeUtils.bool_val(course.get("is_completed"), false)

	_selected_course_id = course_id
	course_modal_eyebrow_label.text = Loc.t("academy.course_modal.syllabus")
	course_modal_title_label.text = _course_name(course)
	course_modal_meta_label.text = Loc.t(
		"academy.hub.detail_meta",
		{
			"track": SafeTypeUtils.string(course.get("track")),
			"cost": SafeTypeUtils.int_val(course.get("enrollment_cost"), 1),
			"state": _course_state_label(course),
		}
	)
	course_modal_description_label.text = _course_description(course)
	course_modal_rewards_label.text = _reward_preview_text(SafeTypeUtils.array(course.get("reward_previews")))
	course_modal_activities_label.text = _activities_preview_text(SafeTypeUtils.array(course.get("activities")))

	course_modal_action_button.visible = is_available or is_enrolled or is_completed
	course_modal_action_button.disabled = false
	if is_enrolled:
		course_modal_action_button.text = Loc.t("academy.hub.continue_course")
	elif is_completed:
		course_modal_action_button.text = Loc.t("academy.course_modal.review")
	elif is_available:
		course_modal_action_button.text = Loc.t("academy.hub.enroll")

	course_modal.popup_centered(Vector2i(620, 440))
	_refresh()

func _hide_course_modal() -> void:
	_previewed_course = {}
	course_modal.hide()

func _on_course_modal_action_pressed() -> void:
	var course: Dictionary = _previewed_course.duplicate(true)
	if course.is_empty():
		return

	var course_id: String = SafeTypeUtils.string(course.get("id"))
	if SafeTypeUtils.bool_val(course.get("is_enrolled")) or SafeTypeUtils.bool_val(course.get("is_completed")):
		BattleContext.select_academy_course(course_id)
		SceneManager.transition_to(SceneManager.SCENE_ACADEMY_COURSE_PATH)
		return

	if SafeTypeUtils.bool_val(course.get("is_available")) and CampaignApi.enroll_academy_course(course_id):
		_show_my_classes = true
		BattleContext.select_academy_course(course_id)
		SceneManager.transition_to(SceneManager.SCENE_ACADEMY_COURSE_PATH)

func _compact_rewards(rewards: Array) -> String:
	var labels: Array[String] = []
	for item: Variant in rewards:
		var reward: Dictionary = SafeTypeUtils.dict(item)
		var label_key: String = SafeTypeUtils.string(reward.get("label_key"))
		if not label_key.is_empty():
			labels.append(Loc.t(label_key))
	return ", ".join(labels)

func _reward_preview_text(rewards: Array) -> String:
	var compact: String = _compact_rewards(rewards)
	if compact.is_empty():
		return Loc.t("academy.hub.no_rewards")
	return Loc.t("academy.hub.rewards", {"rewards": compact})

func _activities_preview_text(activities: Array) -> String:
	var labels: Array[String] = []
	for item: Variant in activities:
		var activity: Dictionary = SafeTypeUtils.dict(item)
		var label_key: String = SafeTypeUtils.string(activity.get("label_key"))
		if not label_key.is_empty():
			labels.append(Loc.t(label_key))
	return Loc.t("academy.hub.activities", {"activities": " -> ".join(labels)})

func _course_name(course: Dictionary) -> String:
	var name_key: String = SafeTypeUtils.string(course.get("name_key"))
	return Loc.t(name_key) if not name_key.is_empty() else SafeTypeUtils.string(course.get("id"))

func _course_description(course: Dictionary) -> String:
	var description_key: String = SafeTypeUtils.string(course.get("description_key"))
	return Loc.t(description_key) if not description_key.is_empty() else ""

func _course_state_label(course: Dictionary) -> String:
	if SafeTypeUtils.bool_val(course.get("is_completed")):
		return Loc.t("academy.hub.state_completed")
	if SafeTypeUtils.bool_val(course.get("is_enrolled")):
		return Loc.t("academy.hub.state_enrolled")
	if SafeTypeUtils.bool_val(course.get("is_available")):
		return Loc.t("academy.hub.state_available")

	var reason: String = SafeTypeUtils.string(course.get("unavailable_reason"))
	if reason == "past_semester":
		return Loc.t("academy.hub.state_past")
	if reason == "future_semester":
		return Loc.t("academy.hub.state_future")
	if reason == "missing_prerequisite":
		return Loc.t("academy.hub.state_prereq")
	if reason == "not_enough_enrollments":
		return Loc.t("academy.hub.state_no_enrollments")
	return Loc.t("academy.hub.state_locked")

func _view_status_text() -> String:
	var viewed_index: int = ((_view_year - 1) * 2) + _view_semester
	var current_index: int = ((_current_year - 1) * 2) + _current_semester
	if viewed_index == current_index:
		return Loc.t("academy.hub.viewing_current")
	if viewed_index < current_index:
		return Loc.t("academy.hub.viewing_past")
	return Loc.t("academy.hub.viewing_future")

func _period_button_text() -> String:
	return Loc.t("academy.hub.period_button", {"year": _view_year, "semester": _view_semester})

func _period_relation_label(year: int, semester: int) -> String:
	var viewed_index: int = ((year - 1) * 2) + semester
	var current_index: int = ((_current_year - 1) * 2) + _current_semester
	if viewed_index == current_index:
		return Loc.t("academy.hub.viewing_current")
	if viewed_index < current_index:
		return Loc.t("academy.hub.viewing_past")
	return Loc.t("academy.hub.viewing_future")

func _is_viewing_current_semester() -> bool:
	return _view_year == _current_year and _view_semester == _current_semester

func _clear_children(node: Node) -> void:
	for child: Node in node.get_children():
		child.queue_free()

func _panel_style(bg: Color, border: Color) -> StyleBoxFlat:
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = bg
	style.border_color = border
	style.set_border_width_all(1)
	style.set_corner_radius_all(6)
	style.content_margin_left = 0
	style.content_margin_top = 0
	style.content_margin_right = 0
	style.content_margin_bottom = 0
	return style

func _on_period_button_pressed() -> void:
	_render_period_picker()
	period_popup.popup_centered(Vector2i(520, 360))

func _on_exit_pressed() -> void:
	SceneManager.transition_to(SceneManager.SCENE_CAMPAIGN_MAP)

func _on_advance_semester_pressed() -> void:
	CampaignApi.advance_academy_semester()
	_refresh_from_current_progress()

func _redirect_to_summoner_selection() -> void:
	SceneManager.transition_to(SceneManager.SCENE_SUMMONER_SELECTION)
