extends Control
class_name AcademyHub

@onready var title_label: Label = %TitleLabel
@onready var status_label: Label = %StatusLabel
@onready var collection_button: Button = %CollectionButton
@onready var shop_button: Button = %ShopButton
@onready var online_button: Button = %OnlineButton
@onready var settings_button: Button = %SettingsButton
@onready var year_tabs: HBoxContainer = %YearTabs
@onready var semester_tabs: HBoxContainer = %SemesterTabs
@onready var view_status_label: Label = %ViewStatusLabel
@onready var board_title_label: Label = %BoardTitleLabel
@onready var advance_semester_button: Button = %AdvanceSemesterButton
@onready var enrollment_slots: HBoxContainer = %EnrollmentSlots
@onready var course_groups: VBoxContainer = %CourseGroups
@onready var detail_title_label: Label = %DetailTitleLabel
@onready var detail_meta_label: Label = %DetailMetaLabel
@onready var detail_description_label: Label = %DetailDescriptionLabel
@onready var detail_rewards_label: Label = %DetailRewardsLabel
@onready var detail_activities_label: Label = %DetailActivitiesLabel
@onready var detail_action_button: Button = %DetailActionButton

const TOTAL_YEARS: int = 4
const TOTAL_SEMESTERS: int = 2
const ENROLLMENT_SLOTS: int = 3

var _current_year: int = 1
var _current_semester: int = 1
var _view_year: int = 1
var _view_semester: int = 1
var _courses: Array[Dictionary] = []
var _selected_course_id: String = ""

func _ready() -> void:
	if SummonerSelectionApi.get_active_summoner_id().is_empty():
		call_deferred("_redirect_to_summoner_selection")
		return

	title_label.text = Loc.t("academy.hub.title")
	collection_button.text = Loc.t("ui.nav.collection")
	shop_button.text = Loc.t("academy.hub.campus_shop")
	online_button.text = Loc.t("ui.nav.online")
	settings_button.text = Loc.t("ui.nav.settings")
	board_title_label.text = Loc.t("academy.hub.semester_board")
	advance_semester_button.text = Loc.t("academy.hub.advance_semester")

	collection_button.pressed.connect(_on_collection_pressed)
	shop_button.pressed.connect(_on_shop_pressed)
	online_button.pressed.connect(_on_online_pressed)
	settings_button.pressed.connect(_on_settings_pressed)
	advance_semester_button.pressed.connect(_on_advance_semester_pressed)

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
	view_status_label.text = _view_status_text()
	advance_semester_button.visible = _is_viewing_current_semester()

	_render_tabs()
	_render_enrollment_slots(progress)
	_load_view_courses()
	_render_course_groups()
	_update_detail_panel()

func _render_tabs() -> void:
	_clear_children(year_tabs)
	_clear_children(semester_tabs)

	for year: int in range(1, TOTAL_YEARS + 1):
		var button: Button = Button.new()
		button.text = Loc.t("academy.hub.year_tab", {"year": year})
		button.toggle_mode = true
		button.button_pressed = year == _view_year
		button.pressed.connect(func() -> void:
			_view_year = year
			_selected_course_id = ""
			_refresh()
		)
		year_tabs.add_child(button)

	for semester: int in range(1, TOTAL_SEMESTERS + 1):
		var button: Button = Button.new()
		button.text = Loc.t("academy.hub.semester_tab", {"semester": semester})
		button.toggle_mode = true
		button.button_pressed = semester == _view_semester
		button.pressed.connect(func() -> void:
			_view_semester = semester
			_selected_course_id = ""
			_refresh()
		)
		semester_tabs.add_child(button)

func _render_enrollment_slots(progress: Dictionary) -> void:
	_clear_children(enrollment_slots)

	var enrolled: Array = SafeTypeUtils.array(progress.get("enrolled_courses"))
	var completed: Array = SafeTypeUtils.array(progress.get("completed_courses"))
	var taken_in_view: Array[String] = []

	for item: Variant in enrolled:
		var id: String = SafeTypeUtils.string(item)
		if _course_belongs_to_view(id):
			taken_in_view.append(id)

	for item: Variant in completed:
		var id: String = SafeTypeUtils.string(item)
		if _course_belongs_to_view(id):
			taken_in_view.append(id)

	for index: int in range(ENROLLMENT_SLOTS):
		var slot: PanelContainer = PanelContainer.new()
		slot.custom_minimum_size = Vector2(170, 54)
		slot.size_flags_horizontal = Control.SIZE_EXPAND_FILL

		var label: Label = Label.new()
		label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
		label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		label.text = _slot_label(index, taken_in_view)
		slot.add_child(label)
		enrollment_slots.add_child(slot)

func _slot_label(index: int, taken_in_view: Array[String]) -> String:
	if index < taken_in_view.size():
		return _display_course_name(taken_in_view[index])
	return Loc.t("academy.hub.empty_slot", {"slot": index + 1})

func _load_view_courses() -> void:
	_courses.clear()
	for item: Variant in CampaignApi.get_academy_courses_for_semester(_view_year, _view_semester):
		var course: Dictionary = SafeTypeUtils.dict(item)
		if not course.is_empty():
			_courses.append(course)

	if _selected_course_id.is_empty() and not _courses.is_empty():
		_selected_course_id = SafeTypeUtils.string(_courses[0].get("id"))

func _render_course_groups() -> void:
	_clear_children(course_groups)

	var required: Array[Dictionary] = []
	var chosen: Array[Dictionary] = []
	var available: Array[Dictionary] = []
	var locked: Array[Dictionary] = []

	for course: Dictionary in _courses:
		if SafeTypeUtils.bool_val(course.get("is_enrolled")) or SafeTypeUtils.bool_val(course.get("is_completed")):
			chosen.append(course)
		elif SafeTypeUtils.bool_val(course.get("is_required")):
			required.append(course)
		elif SafeTypeUtils.bool_val(course.get("is_available")):
			available.append(course)
		else:
			locked.append(course)

	_add_course_group("academy.hub.group_required", required)
	_add_course_group("academy.hub.group_chosen", chosen)
	_add_course_group("academy.hub.group_available", available)
	if not locked.is_empty():
		_add_course_group("academy.hub.group_locked", locked)

func _add_course_group(title_key: String, courses: Array[Dictionary]) -> void:
	if courses.is_empty():
		return

	var group: VBoxContainer = VBoxContainer.new()
	group.add_theme_constant_override("separation", 8)

	var title: Label = Label.new()
	title.add_theme_font_size_override("font_size", 18)
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
	var button: Button = Button.new()
	button.custom_minimum_size = Vector2(210, 112)
	button.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	button.text = _course_card_text(course)
	button.alignment = HORIZONTAL_ALIGNMENT_LEFT
	button.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART

	var course_id: String = SafeTypeUtils.string(course.get("id"))
	button.toggle_mode = true
	button.button_pressed = course_id == _selected_course_id
	button.disabled = false
	button.pressed.connect(func() -> void:
		_selected_course_id = course_id
		_update_detail_panel()
		_render_course_groups()
	)
	return button

func _course_card_text(course: Dictionary) -> String:
	var name: String = _course_name(course)
	var track: String = SafeTypeUtils.string(course.get("track"))
	var cost: int = SafeTypeUtils.int_val(course.get("enrollment_cost"), 1)
	var state: String = _course_state_label(course)
	var rewards: String = _compact_rewards(SafeTypeUtils.array(course.get("reward_previews")))
	return "%s\n%s | %s\n%s\n%s" % [
		name,
		track,
		Loc.t("academy.hub.cost_short", {"cost": cost}),
		rewards,
		state,
	]

func _update_detail_panel() -> void:
	var course: Dictionary = _selected_course()
	if course.is_empty():
		detail_title_label.text = Loc.t("academy.hub.no_course_selected")
		detail_meta_label.text = ""
		detail_description_label.text = ""
		detail_rewards_label.text = ""
		detail_activities_label.text = ""
		detail_action_button.visible = false
		return

	detail_action_button.visible = true
	detail_title_label.text = _course_name(course)
	detail_meta_label.text = _course_meta_text(course)
	detail_description_label.text = _course_description(course)
	detail_rewards_label.text = _reward_preview_text(SafeTypeUtils.array(course.get("reward_previews")))
	detail_activities_label.text = _activities_text(SafeTypeUtils.array(course.get("activities")))
	_configure_detail_action(course)

func _configure_detail_action(course: Dictionary) -> void:
	for connection: Dictionary in detail_action_button.pressed.get_connections():
		detail_action_button.pressed.disconnect(connection["callable"])

	var course_id: String = SafeTypeUtils.string(course.get("id"))
	var is_available: bool = SafeTypeUtils.bool_val(course.get("is_available"), false)
	var is_enrolled: bool = SafeTypeUtils.bool_val(course.get("is_enrolled"), false)
	var is_completed: bool = SafeTypeUtils.bool_val(course.get("is_completed"), false)

	if is_completed:
		detail_action_button.text = Loc.t("academy.hub.completed")
		detail_action_button.disabled = true
	elif is_enrolled:
		detail_action_button.text = Loc.t("academy.hub.continue_course")
		detail_action_button.disabled = false
		detail_action_button.pressed.connect(func() -> void:
			_continue_course(course)
		)
	elif is_available:
		detail_action_button.text = Loc.t("academy.hub.enroll")
		detail_action_button.disabled = false
		detail_action_button.pressed.connect(func() -> void:
			CampaignApi.enroll_academy_course(course_id)
			_selected_course_id = course_id
			_refresh()
		)
	else:
		detail_action_button.text = _locked_action_text(course)
		detail_action_button.disabled = true

func _continue_course(course: Dictionary) -> void:
	var course_id: String = SafeTypeUtils.string(course.get("id"))
	var next_activity: Dictionary = SafeTypeUtils.dict(course.get("next_activity"))
	var activity_type: String = SafeTypeUtils.string(next_activity.get("type"))
	if activity_type == "PracticeBattle" or activity_type == "AssessmentBattle":
		_launch_academy_battle(course_id, next_activity)
	else:
		CampaignApi.complete_next_academy_activity(course_id)
	_refresh()

func _course_meta_text(course: Dictionary) -> String:
	return Loc.t(
		"academy.hub.detail_meta",
		{
			"track": SafeTypeUtils.string(course.get("track")),
			"cost": SafeTypeUtils.int_val(course.get("enrollment_cost"), 1),
			"state": _course_state_label(course),
		}
	)

func _reward_preview_text(rewards: Array) -> String:
	if rewards.is_empty():
		return Loc.t("academy.hub.no_rewards")

	return Loc.t("academy.hub.rewards", {"rewards": _compact_rewards(rewards)})

func _compact_rewards(rewards: Array) -> String:
	var labels: Array[String] = []
	for item: Variant in rewards:
		var reward: Dictionary = SafeTypeUtils.dict(item)
		var label_key: String = SafeTypeUtils.string(reward.get("label_key"))
		if not label_key.is_empty():
			labels.append(Loc.t(label_key))
	return ", ".join(labels)

func _activities_text(activities: Array) -> String:
	if activities.is_empty():
		return ""

	var labels: Array[String] = []
	for item: Variant in activities:
		var activity: Dictionary = SafeTypeUtils.dict(item)
		var label_key: String = SafeTypeUtils.string(activity.get("label_key"))
		labels.append(Loc.t(label_key) if not label_key.is_empty() else SafeTypeUtils.string(activity.get("type")))
	return Loc.t("academy.hub.activities", {"activities": " -> ".join(labels)})

func _selected_course() -> Dictionary:
	for course: Dictionary in _courses:
		if SafeTypeUtils.string(course.get("id")) == _selected_course_id:
			return course
	return {}

func _course_belongs_to_view(course_id: String) -> bool:
	for course: Dictionary in _courses:
		if SafeTypeUtils.string(course.get("id")) == course_id:
			return true
	var course_data: Array = CampaignApi.get_academy_courses_for_semester(_view_year, _view_semester)
	for item: Variant in course_data:
		var course: Dictionary = SafeTypeUtils.dict(item)
		if SafeTypeUtils.string(course.get("id")) == course_id:
			return true
	return false

func _course_name(course: Dictionary) -> String:
	var name_key: String = SafeTypeUtils.string(course.get("name_key"))
	return Loc.t(name_key) if not name_key.is_empty() else SafeTypeUtils.string(course.get("id"))

func _course_description(course: Dictionary) -> String:
	var desc_key: String = SafeTypeUtils.string(course.get("description_key"))
	return Loc.t(desc_key) if not desc_key.is_empty() else ""

func _display_course_name(course_id: String) -> String:
	for course: Dictionary in _courses:
		if SafeTypeUtils.string(course.get("id")) == course_id:
			return _course_name(course)
	return course_id

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

func _locked_action_text(course: Dictionary) -> String:
	var reason: String = SafeTypeUtils.string(course.get("unavailable_reason"))
	if reason == "past_semester":
		return Loc.t("academy.hub.view_only")
	if reason == "future_semester":
		return Loc.t("academy.hub.future")
	return Loc.t("academy.hub.locked")

func _view_status_text() -> String:
	var viewed_index: int = ((_view_year - 1) * 2) + _view_semester
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

func _on_collection_pressed() -> void:
	NavigationContext.push_return(SceneManager.SCENE_CAMPAIGN_MAP)
	SceneManager.transition_to(SceneManager.SCENE_COLLECTION_SCREEN)

func _on_shop_pressed() -> void:
	NavigationContext.push_return(SceneManager.SCENE_CAMPAIGN_MAP)
	SceneManager.transition_to(SceneManager.SCENE_SHOP_SCREEN)

func _on_online_pressed() -> void:
	NavigationContext.push_return(SceneManager.SCENE_CAMPAIGN_MAP)
	SceneManager.transition_to(SceneManager.SCENE_ONLINE)

func _on_settings_pressed() -> void:
	NavigationContext.push_return(SceneManager.SCENE_CAMPAIGN_MAP)
	SceneManager.transition_to(SceneManager.SCENE_SETTINGS)

func _on_advance_semester_pressed() -> void:
	CampaignApi.advance_academy_semester()
	_refresh_from_current_progress()

func _launch_academy_battle(course_id: String, activity: Dictionary) -> void:
	var activity_id: String = SafeTypeUtils.string(activity.get("id"), course_id)
	BattleContext.configure_academy_battle(course_id, activity_id)
	SceneManager.transition_to(SceneManager.SCENE_BATTLE_3D)

func _redirect_to_summoner_selection() -> void:
	SceneManager.transition_to(SceneManager.SCENE_SUMMONER_SELECTION)
