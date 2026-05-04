extends Control
class_name AcademyHub

@onready var title_label: Label = %TitleLabel
@onready var status_label: Label = %StatusLabel
@onready var collection_button: Button = %CollectionButton
@onready var shop_button: Button = %ShopButton
@onready var online_button: Button = %OnlineButton
@onready var settings_button: Button = %SettingsButton
@onready var catalog_title_label: Label = %CatalogTitleLabel
@onready var advance_semester_button: Button = %AdvanceSemesterButton
@onready var course_list: VBoxContainer = %CourseList
@onready var transcript_title_label: Label = %TranscriptTitleLabel
@onready var transcript_list: VBoxContainer = %TranscriptList

const COURSE_PANEL_MIN_HEIGHT: float = 112.0

func _ready() -> void:
	if SummonerSelectionApi.get_active_summoner_id().is_empty():
		call_deferred("_redirect_to_summoner_selection")
		return

	title_label.text = Loc.t("academy.hub.title")
	collection_button.text = Loc.t("ui.nav.collection")
	shop_button.text = Loc.t("academy.hub.campus_shop")
	online_button.text = Loc.t("ui.nav.online")
	settings_button.text = Loc.t("ui.nav.settings")
	catalog_title_label.text = Loc.t("academy.hub.course_catalog")
	transcript_title_label.text = Loc.t("academy.hub.transcript")
	advance_semester_button.text = Loc.t("academy.hub.advance_semester")

	collection_button.pressed.connect(_on_collection_pressed)
	shop_button.pressed.connect(_on_shop_pressed)
	online_button.pressed.connect(_on_online_pressed)
	settings_button.pressed.connect(_on_settings_pressed)
	advance_semester_button.pressed.connect(_on_advance_semester_pressed)

	if Campaign.has_signal("CampaignProgressChanged"):
		Campaign.connect("CampaignProgressChanged", _refresh)

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

	_render_courses(CampaignApi.get_available_academy_courses())
	_render_transcript(SafeTypeUtils.array(progress.get("transcript")))

func _render_courses(courses: Array) -> void:
	_clear_children(course_list)

	for item: Variant in courses:
		var course: Dictionary = SafeTypeUtils.dict(item)
		if course.is_empty():
			continue
		course_list.add_child(_build_course_row(course))

func _build_course_row(course: Dictionary) -> Control:
	var panel: PanelContainer = PanelContainer.new()
	panel.custom_minimum_size = Vector2(0, COURSE_PANEL_MIN_HEIGHT)

	var margin: MarginContainer = MarginContainer.new()
	margin.add_theme_constant_override("margin_left", 14)
	margin.add_theme_constant_override("margin_top", 10)
	margin.add_theme_constant_override("margin_right", 14)
	margin.add_theme_constant_override("margin_bottom", 10)
	panel.add_child(margin)

	var row: HBoxContainer = HBoxContainer.new()
	row.add_theme_constant_override("separation", 12)
	margin.add_child(row)

	var text_stack: VBoxContainer = VBoxContainer.new()
	text_stack.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	text_stack.add_theme_constant_override("separation", 4)
	row.add_child(text_stack)

	var name_key: String = SafeTypeUtils.string(course.get("name_key"))
	var desc_key: String = SafeTypeUtils.string(course.get("description_key"))
	var name_label: Label = Label.new()
	name_label.add_theme_font_size_override("font_size", 20)
	name_label.text = Loc.t(name_key) if not name_key.is_empty() else SafeTypeUtils.string(course.get("id"))
	text_stack.add_child(name_label)

	var meta_label: Label = Label.new()
	meta_label.add_theme_font_size_override("font_size", 14)
	meta_label.text = _course_meta_text(course)
	text_stack.add_child(meta_label)

	var desc_label: Label = Label.new()
	desc_label.add_theme_font_size_override("font_size", 14)
	desc_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	desc_label.text = Loc.t(desc_key) if not desc_key.is_empty() else ""
	text_stack.add_child(desc_label)

	var rewards_label: Label = Label.new()
	rewards_label.add_theme_font_size_override("font_size", 13)
	rewards_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	rewards_label.text = _reward_preview_text(SafeTypeUtils.array(course.get("reward_previews")))
	text_stack.add_child(rewards_label)

	var action_button: Button = Button.new()
	action_button.custom_minimum_size = Vector2(132, 42)
	row.add_child(action_button)

	var course_id: String = SafeTypeUtils.string(course.get("id"))
	var is_available: bool = SafeTypeUtils.bool_val(course.get("is_available"), false)
	var is_enrolled: bool = SafeTypeUtils.bool_val(course.get("is_enrolled"), false)
	var is_completed: bool = SafeTypeUtils.bool_val(course.get("is_completed"), false)

	if is_completed:
		action_button.text = Loc.t("academy.hub.completed")
		action_button.disabled = true
	elif is_enrolled:
		action_button.text = Loc.t("academy.hub.complete")
		action_button.pressed.connect(func() -> void:
			CampaignApi.complete_academy_course(course_id)
			_refresh()
		)
	elif is_available:
		action_button.text = Loc.t("academy.hub.enroll")
		action_button.pressed.connect(func() -> void:
			CampaignApi.enroll_academy_course(course_id)
			_refresh()
		)
	else:
		action_button.text = Loc.t("academy.hub.locked")
		action_button.disabled = true
		action_button.tooltip_text = SafeTypeUtils.string(course.get("unavailable_reason"))

	return panel

func _course_meta_text(course: Dictionary) -> String:
	var track: String = SafeTypeUtils.string(course.get("track"))
	var cost: int = SafeTypeUtils.int_val(course.get("enrollment_cost"), 1)
	return Loc.t("academy.hub.course_meta", {"track": track, "cost": cost})

func _reward_preview_text(rewards: Array) -> String:
	if rewards.is_empty():
		return ""

	var labels: Array[String] = []
	for item: Variant in rewards:
		var reward: Dictionary = SafeTypeUtils.dict(item)
		var label_key: String = SafeTypeUtils.string(reward.get("label_key"))
		if not label_key.is_empty():
			labels.append(Loc.t(label_key))

	return Loc.t("academy.hub.rewards", {"rewards": ", ".join(labels)})

func _render_transcript(entries: Array) -> void:
	_clear_children(transcript_list)

	if entries.is_empty():
		var empty_label: Label = Label.new()
		empty_label.text = Loc.t("academy.hub.empty_transcript")
		empty_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		transcript_list.add_child(empty_label)
		return

	for item: Variant in entries:
		var entry: Dictionary = SafeTypeUtils.dict(item)
		var course_id: String = SafeTypeUtils.string(entry.get("course_id"))
		var grade: String = SafeTypeUtils.string(entry.get("grade"), "pass")
		var honors: bool = SafeTypeUtils.bool_val(entry.get("honors"), false)
		var line: Label = Label.new()
		line.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		line.text = Loc.t(
			"academy.hub.transcript_entry",
			{"course": course_id, "grade": grade, "honors": Loc.t("academy.hub.honors") if honors else ""}
		)
		transcript_list.add_child(line)

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
	_refresh()

func _redirect_to_summoner_selection() -> void:
	SceneManager.transition_to(SceneManager.SCENE_SUMMONER_SELECTION)
