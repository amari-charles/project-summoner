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
const COURSE_MODAL_TARGET_SIZE: Vector2i = Vector2i(720, 520)
const COURSE_MODAL_MIN_SIZE: Vector2i = Vector2i(480, 320)

var _current_year: int = 1
var _current_semester: int = 1
var _view_year: int = 1
var _view_semester: int = 1
var _courses: Array[Dictionary] = []
var _selected_course_id: String = ""
var _show_my_classes: bool = true
var _selected_course: Dictionary = {}

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

	exit_button.pressed.connect(_on_exit_pressed)
	advance_semester_button.pressed.connect(_on_advance_semester_pressed)
	period_button.pressed.connect(_on_period_button_pressed)
	course_modal_close_button.pressed.connect(_hide_course_modal)
	course_modal_action_button.pressed.connect(_on_course_modal_action_pressed)
	open_classes_button.pressed.connect(func() -> void:
		_show_my_classes = false
		_selected_course_id = ""
		_selected_course = {}
		_refresh()
	)
	my_classes_button.pressed.connect(func() -> void:
		_show_my_classes = true
		_selected_course_id = ""
		_selected_course = {}
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
	_sync_selected_course()
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
				_selected_course = {}
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

	if _show_my_classes:
		_render_schedule_groups()
	else:
		_render_open_course_groups()

	if course_groups.get_child_count() == 0:
		var empty: Label = Label.new()
		empty.text = Loc.t("academy.class_hall.no_my_classes") if _show_my_classes else Loc.t("academy.class_hall.no_open_classes")
		empty.add_theme_font_size_override("font_size", 18)
		empty.add_theme_color_override("font_color", COLOR_TEXT_MUTED)
		course_groups.add_child(empty)

func _render_schedule_groups() -> void:
	var active: Array[Dictionary] = []
	var completed: Array[Dictionary] = []

	for course: Dictionary in _filtered_courses_for_active_tab():
		if SafeTypeUtils.bool_val(course.get("is_completed")):
			completed.append(course)
		else:
			active.append(course)

	_add_course_group("academy.class_hall.current_schedule", _sort_courses_for_display(active))
	_add_course_group("academy.class_hall.completed_courses", _sort_courses_for_display(completed))

func _render_open_course_groups() -> void:
	var groups: Dictionary = {}

	for course: Dictionary in _filtered_courses_for_active_tab():
		var group_id: String = SafeTypeUtils.string(course.get("group_id"), "available")
		if not groups.has(group_id):
			groups[group_id] = {
				"title_key": SafeTypeUtils.string(course.get("group_title_key"), "academy.class_hall.choice_group"),
				"sort_order": SafeTypeUtils.int_val(course.get("group_sort_order"), 999),
				"courses": [],
			}
		groups[group_id]["courses"].append(course)

	var group_defs: Array[Dictionary] = []
	for group: Dictionary in groups.values():
		group_defs.append(group)

	group_defs.sort_custom(func(a: Dictionary, b: Dictionary) -> bool:
		var sort_a: int = SafeTypeUtils.int_val(a.get("sort_order"), 999)
		var sort_b: int = SafeTypeUtils.int_val(b.get("sort_order"), 999)
		if sort_a != sort_b:
			return sort_a < sort_b
		return SafeTypeUtils.string(a.get("title_key")) < SafeTypeUtils.string(b.get("title_key"))
	)

	for group: Dictionary in group_defs:
		_add_course_group(
			SafeTypeUtils.string(group.get("title_key"), "academy.class_hall.choice_group"),
			_sort_courses_for_display(SafeTypeUtils.array(group.get("courses")))
		)

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

func _sync_selected_course() -> void:
	_selected_course = {}
	if _selected_course_id.is_empty():
		_selected_course_id = ""
		return

	var filtered: Array[Dictionary] = _filtered_courses_for_active_tab()
	for course: Dictionary in filtered:
		if SafeTypeUtils.string(course.get("id")) == _selected_course_id:
			_selected_course = course
			return

	_selected_course_id = ""

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
	track.text = _track_label(course)
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
	_selected_course = course.duplicate(true)
	_selected_course_id = SafeTypeUtils.string(course.get("id"))
	_render_course_groups()
	_show_course_modal(_selected_course)

func _show_course_modal(course: Dictionary) -> void:
	var is_available: bool = SafeTypeUtils.bool_val(course.get("is_available"), false)
	var is_enrolled: bool = SafeTypeUtils.bool_val(course.get("is_enrolled"), false)
	var is_completed: bool = SafeTypeUtils.bool_val(course.get("is_completed"), false)

	course_modal_eyebrow_label.text = Loc.t("academy.course_modal.syllabus")
	course_modal_title_label.text = _course_name(course)
	course_modal_meta_label.text = _compact_course_meta(course)
	course_modal_description_label.text = _course_description(course)
	course_modal_rewards_label.text = _reward_preview_text(SafeTypeUtils.array(course.get("reward_previews")))
	course_modal_activities_label.text = _activities_preview_text(SafeTypeUtils.array(course.get("activities")))

	course_modal_action_button.visible = true
	course_modal_action_button.disabled = false
	if is_enrolled:
		course_modal_action_button.text = Loc.t("academy.hub.continue_course")
	elif is_completed:
		course_modal_action_button.text = Loc.t("academy.course_modal.review")
	elif is_available:
		course_modal_action_button.text = Loc.t("academy.hub.enroll")
	else:
		course_modal_action_button.text = _course_state_label(course)
		course_modal_action_button.disabled = true

	var modal_size: Vector2i = _course_modal_size()
	course_modal.min_size = Vector2i(
		mini(COURSE_MODAL_MIN_SIZE.x, modal_size.x),
		mini(COURSE_MODAL_MIN_SIZE.y, modal_size.y)
	)
	course_modal.max_size = modal_size
	course_modal.size = modal_size
	course_modal.popup_centered_clamped(modal_size, 0.72)

func _hide_course_modal() -> void:
	course_modal.hide()

func _on_course_modal_action_pressed() -> void:
	_activate_selected_course()

func _course_modal_size() -> Vector2i:
	var viewport_size: Vector2 = get_viewport_rect().size
	var max_width: int = maxi(360, int(viewport_size.x) - 96)
	var max_height: int = maxi(280, int(viewport_size.y * 0.72))
	return Vector2i(
		mini(COURSE_MODAL_TARGET_SIZE.x, max_width),
		mini(COURSE_MODAL_TARGET_SIZE.y, max_height)
	)

func _activate_selected_course() -> void:
	if _selected_course.is_empty():
		return

	var course_id: String = SafeTypeUtils.string(_selected_course.get("id"))
	if SafeTypeUtils.bool_val(_selected_course.get("is_enrolled")) or SafeTypeUtils.bool_val(_selected_course.get("is_completed")):
		BattleContext.select_academy_course(course_id)
		SceneManager.transition_to(SceneManager.SCENE_ACADEMY_COURSE_PATH)
		return

	if SafeTypeUtils.bool_val(_selected_course.get("is_available")) and CampaignApi.enroll_academy_course(course_id):
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

func _compact_course_meta(course: Dictionary) -> String:
	var parts: Array[String] = [
		_track_label(course),
		Loc.t("academy.hub.cost_short", {"cost": SafeTypeUtils.int_val(course.get("enrollment_cost"), 1)}),
		_course_state_label(course),
	]
	var reason: String = _locked_reason_text(course)
	if not reason.is_empty():
		parts.append(reason)
	return "  |  ".join(parts)

func _locked_reason_text(course: Dictionary) -> String:
	if SafeTypeUtils.bool_val(course.get("is_available")) \
		or SafeTypeUtils.bool_val(course.get("is_enrolled")) \
		or SafeTypeUtils.bool_val(course.get("is_completed")):
		return ""

	var reason: String = SafeTypeUtils.string(course.get("unavailable_reason"))
	if reason.is_empty():
		return ""
	return _course_state_label(course)

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
	if reason == "choice_group_taken":
		return Loc.t("academy.hub.state_choice_taken")
	return Loc.t("academy.hub.state_locked")

func _sort_courses_for_display(courses: Array) -> Array[Dictionary]:
	var sorted: Array[Dictionary] = []
	for item: Variant in courses:
		var course: Dictionary = SafeTypeUtils.dict(item)
		if not course.is_empty():
			sorted.append(course)

	sorted.sort_custom(func(a: Dictionary, b: Dictionary) -> bool:
		var state_a: int = _course_state_sort_value(a)
		var state_b: int = _course_state_sort_value(b)
		if state_a != state_b:
			return state_a < state_b

		var group_a: int = SafeTypeUtils.int_val(a.get("group_sort_order"), 999)
		var group_b: int = SafeTypeUtils.int_val(b.get("group_sort_order"), 999)
		if group_a != group_b:
			return group_a < group_b

		return _course_name(a) < _course_name(b)
	)
	return sorted

func _course_state_sort_value(course: Dictionary) -> int:
	if SafeTypeUtils.bool_val(course.get("is_enrolled")):
		return 0
	if SafeTypeUtils.bool_val(course.get("is_available")):
		return 1
	if SafeTypeUtils.bool_val(course.get("is_completed")):
		return 2
	return 3

func _track_label(course: Dictionary) -> String:
	var title_key: String = SafeTypeUtils.string(course.get("track_title_key"))
	if not title_key.is_empty():
		var title: String = Loc.t(title_key)
		if title != title_key:
			return title

	var track_id: String = SafeTypeUtils.string(course.get("track"))
	var key: String = "academy.track.%s" % track_id.to_snake_case()
	var translated: String = Loc.t(key)
	return translated if translated != key else track_id

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
