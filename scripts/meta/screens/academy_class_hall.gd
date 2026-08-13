extends Control
class_name AcademyClassHall

@onready var title_label: Label = %TitleLabel
@onready var status_label: Label = %StatusLabel
@onready var exit_button: Button = %ExitButton
@onready var term_label: Label = %TermLabel
@onready var advance_semester_button: Button = %AdvanceSemesterButton
@onready var open_classes_button: Button = %OpenClassesButton
@onready var my_classes_button: Button = %MyClassesButton
@onready var enrollment_summary_label: Label = %EnrollmentSummaryLabel
@onready var course_groups: VBoxContainer = %CourseGroups
@onready var advance_semester_dialog: AcceptDialog = %AdvanceSemesterDialog

const COLOR_PANEL: Color = GameColorPalette.UI_SURFACE
const COLOR_PANEL_SELECTED: Color = GameColorPalette.BUTTON_PRIMARY_BG
const COLOR_PANEL_LOCKED: Color = GameColorPalette.UI_SURFACE_DISABLED
const COLOR_ACCENT: Color = GameColorPalette.TEXT_HIGHLIGHT
const COLOR_TEXT_MUTED: Color = GameColorPalette.TEXT_SECONDARY

var _current_year: int = 1
var _current_semester: int = 1
var _courses: Array[Dictionary] = []
var _show_my_classes: bool = true

func _ready() -> void:
	if SummonerSelectionApi.get_active_summoner_id().is_empty():
		call_deferred("_redirect_to_summoner_selection")
		return

	title_label.text = Loc.t("academy.class_hall.title")
	status_label.visible = false
	exit_button.text = "←"
	exit_button.tooltip_text = Loc.t("academy.hub.title")
	exit_button.accessibility_name = Loc.t("academy.hub.title")
	advance_semester_button.text = Loc.t("academy.hub.advance_semester")
	open_classes_button.text = Loc.t("academy.class_hall.open_classes")
	my_classes_button.text = Loc.t("academy.class_hall.my_classes")
	advance_semester_dialog.title = Loc.t("academy.class_hall.advance_blocked_title")
	advance_semester_dialog.ok_button_text = Loc.t("ui.common.ok")

	exit_button.pressed.connect(_on_exit_pressed)
	advance_semester_button.pressed.connect(_on_advance_semester_pressed)
	open_classes_button.pressed.connect(func() -> void:
		_show_my_classes = false
		_refresh()
	)
	my_classes_button.pressed.connect(func() -> void:
		_show_my_classes = true
		_refresh()
	)

	if Campaign.has_signal("CampaignProgressChanged"):
		Campaign.connect("CampaignProgressChanged", _refresh)

	_refresh_from_current_progress()

func _refresh_from_current_progress() -> void:
	var progress: Dictionary = CampaignApi.get_academy_progress()
	_current_year = SafeTypeUtils.int_val(progress.get("current_year"), 1)
	_current_semester = SafeTypeUtils.int_val(progress.get("current_semester"), 1)
	_refresh()

func _refresh() -> void:
	var progress: Dictionary = CampaignApi.get_academy_progress()
	_current_year = SafeTypeUtils.int_val(progress.get("current_year"), 1)
	_current_semester = SafeTypeUtils.int_val(progress.get("current_semester"), 1)
	var enrollments: int = SafeTypeUtils.int_val(progress.get("remaining_enrollments"), 0)

	term_label.text = Loc.t(
		"academy.class_hall.current_term",
		{"year": _current_year, "semester": _current_semester}
	)
	advance_semester_button.visible = true
	open_classes_button.button_pressed = not _show_my_classes
	my_classes_button.button_pressed = _show_my_classes
	enrollment_summary_label.text = Loc.t("academy.class_hall.approvals_left", {"count": enrollments})

	_load_view_courses()
	_render_course_groups()

func _load_view_courses() -> void:
	_courses.clear()
	for item: Variant in CampaignApi.get_academy_courses_for_semester(_current_year, _current_semester):
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
			COLOR_PANEL_LOCKED if is_locked else COLOR_PANEL,
			GameColorPalette.UI_BORDER
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

	var name: Label = Label.new()
	name.mouse_filter = Control.MOUSE_FILTER_IGNORE
	name.text = _course_name(course)
	name.add_theme_font_size_override("font_size", 18)
	name.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	root.add_child(name)

	var state: Label = Label.new()
	state.mouse_filter = Control.MOUSE_FILTER_IGNORE
	state.text = _card_status_text(course)
	state.add_theme_font_size_override("font_size", 13)
	state.add_theme_color_override("font_color", COLOR_TEXT_MUTED)
	state.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	root.add_child(state)

	var detail: Label = Label.new()
	detail.mouse_filter = Control.MOUSE_FILTER_IGNORE
	detail.text = _card_detail_text(course)
	detail.add_theme_font_size_override("font_size", 13)
	detail.add_theme_color_override("font_color", COLOR_TEXT_MUTED)
	detail.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	root.add_child(detail)

	var spacer: Control = Control.new()
	spacer.mouse_filter = Control.MOUSE_FILTER_IGNORE
	spacer.size_flags_vertical = Control.SIZE_EXPAND_FILL
	root.add_child(spacer)

	return panel

func _activate_course(course: Dictionary) -> void:
	var course_id: String = SafeTypeUtils.string(course.get("id"))
	if course_id.is_empty():
		return
	BattleContext.select_academy_course(course_id)
	SceneManager.transition_to(SceneManager.SCENE_ACADEMY_COURSE_FLOW)

func _compact_rewards(rewards: Array) -> String:
	var labels: Array[String] = []
	for item: Variant in rewards:
		var reward: Dictionary = SafeTypeUtils.dict(item)
		if SafeTypeUtils.string(reward.get("status")) == "claimed":
			continue
		var label_key: String = SafeTypeUtils.string(reward.get("label_key"))
		if label_key.is_empty():
			label_key = SafeTypeUtils.string(reward.get("category_key"))
		if not label_key.is_empty():
			labels.append(Loc.t(label_key))
	return ", ".join(labels)

func _card_status_text(course: Dictionary) -> String:
	if _show_my_classes and SafeTypeUtils.bool_val(course.get("is_enrolled")):
		var next_activity: Dictionary = SafeTypeUtils.dict(course.get("next_activity"))
		var activity_label_key: String = SafeTypeUtils.string(next_activity.get("label_key"))
		if not activity_label_key.is_empty():
			var activities: Array = SafeTypeUtils.array(course.get("activities"))
			var activity_index: int = SafeTypeUtils.int_val(course.get("activity_index"), 0)
			return Loc.t(
				"academy.hub.next_activity",
				{
					"activity": Loc.t(activity_label_key),
					"index": activity_index + 1,
					"total": maxi(activities.size(), 1),
				}
			)

	return _course_state_label(course)

func _card_detail_text(course: Dictionary) -> String:
	var parts: Array[String] = []
	if not _show_my_classes:
		parts.append(Loc.t("academy.hub.cost_short", {"cost": SafeTypeUtils.int_val(course.get("enrollment_cost"), 1)}))

	var rewards: String = _compact_rewards(SafeTypeUtils.array(course.get("reward_previews")))
	if not rewards.is_empty():
		parts.append(rewards)

	if parts.is_empty():
		parts.append(_track_label(course))

	return " | ".join(parts)

func _course_name(course: Dictionary) -> String:
	var name_key: String = SafeTypeUtils.string(course.get("name_key"))
	return Loc.t(name_key) if not name_key.is_empty() else SafeTypeUtils.string(course.get("id"))

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

func _on_exit_pressed() -> void:
	SceneManager.transition_to(SceneManager.SCENE_CAMPAIGN_MAP)

func _on_advance_semester_pressed() -> void:
	var block_reason: String = _advance_semester_block_reason()
	if not block_reason.is_empty():
		_show_advance_semester_dialog(block_reason)
		return

	if not CampaignApi.advance_academy_semester():
		_show_advance_semester_dialog(Loc.t("academy.class_hall.advance_blocked_next_term"))
		return

	_refresh_from_current_progress()

func _advance_semester_block_reason() -> String:
	for course: Dictionary in _courses:
		if SafeTypeUtils.bool_val(course.get("is_required")) and not SafeTypeUtils.bool_val(course.get("is_completed")):
			return Loc.t("academy.class_hall.advance_blocked_required")

	var progress: Dictionary = CampaignApi.get_academy_progress()
	var enrollments: int = SafeTypeUtils.int_val(progress.get("remaining_enrollments"), 0)
	if enrollments <= 0:
		return ""

	for course: Dictionary in _courses:
		if SafeTypeUtils.bool_val(course.get("is_available")) \
			and not SafeTypeUtils.bool_val(course.get("is_enrolled")) \
			and not SafeTypeUtils.bool_val(course.get("is_completed")):
			return Loc.t("academy.class_hall.advance_blocked_picks", {"count": enrollments})

	return ""

func _show_advance_semester_dialog(message: String) -> void:
	advance_semester_dialog.dialog_text = message
	advance_semester_dialog.popup_centered()

func _redirect_to_summoner_selection() -> void:
	SceneManager.transition_to(SceneManager.SCENE_SUMMONER_SELECTION)
