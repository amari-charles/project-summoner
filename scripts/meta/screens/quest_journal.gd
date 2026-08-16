extends Control
class_name QuestJournal

const COLOR_MUTED: Color = GameColorPalette.TEXT_SECONDARY
const COLOR_HIGHLIGHT: Color = GameColorPalette.TEXT_HIGHLIGHT

@onready var back_button: Button = %BackButton
@onready var title_label: Label = %TitleLabel
@onready var term_label: Label = %TermLabel
@onready var capacity_label: Label = %CapacityLabel
@onready var active_list: VBoxContainer = %ActiveList
@onready var opportunities_list: VBoxContainer = %OpportunitiesList
@onready var completed_list: VBoxContainer = %CompletedList
@onready var detail_empty: Label = %DetailEmpty
@onready var detail_content: VBoxContainer = %DetailContent
@onready var detail_title: Label = %DetailTitle
@onready var detail_status: Label = %DetailStatus
@onready var detail_description: Label = %DetailDescription
@onready var detail_objective: Label = %DetailObjective
@onready var track_button: Button = %TrackButton

var _journal_state: Dictionary = {}
var _selected_quest_id: String = ""
var _entries_by_id: Dictionary = {}


func _ready() -> void:
	back_button.text = "←"
	back_button.tooltip_text = Loc.t("academy.hub.title")
	back_button.accessibility_name = Loc.t("academy.hub.title")
	title_label.text = Loc.t("academy.journal.title")
	back_button.pressed.connect(_go_back)
	track_button.pressed.connect(_track_selected)
	if Campaign.has_signal("CampaignProgressChanged"):
		Campaign.connect("CampaignProgressChanged", _refresh)
	_refresh()


func _refresh() -> void:
	_journal_state = CampaignApi.get_quest_journal_state()
	var year: int = SafeTypeUtils.int_val(_journal_state.get("current_year"), 1)
	var semester: int = SafeTypeUtils.int_val(_journal_state.get("current_semester"), 1)
	var total: int = SafeTypeUtils.int_val(_journal_state.get("capacity_total"), 0)
	var committed: int = SafeTypeUtils.int_val(_journal_state.get("capacity_committed"), 0)
	var completed: int = SafeTypeUtils.int_val(_journal_state.get("capacity_completed"), 0)
	term_label.text = Loc.t("academy.journal.term", {"year": year, "semester": semester})
	capacity_label.text = Loc.t(
		"academy.journal.capacity",
		{"committed": committed, "completed": completed, "total": total}
	)

	_entries_by_id.clear()
	_render_section(active_list, "active")
	_render_section(opportunities_list, "opportunities")
	_render_section(completed_list, "completed")
	_select_initial_entry()
	_render_detail()


func _render_section(container: VBoxContainer, section: String) -> void:
	_clear_children(container)
	for value: Variant in SafeTypeUtils.array(_journal_state.get(section)):
		var entry: Dictionary = SafeTypeUtils.dict(value)
		var quest_id: String = SafeTypeUtils.string(entry.get("id"))
		if quest_id.is_empty():
			continue
		_entries_by_id[quest_id] = entry
		var button: Button = Button.new()
		button.text = Loc.t(SafeTypeUtils.string(entry.get("title_key")))
		button.alignment = HORIZONTAL_ALIGNMENT_LEFT
		button.custom_minimum_size = Vector2(0.0, 46.0)
		button.toggle_mode = true
		button.button_pressed = quest_id == _selected_quest_id
		button.set_meta("quest_id", quest_id)
		button.pressed.connect(_select_entry.bind(quest_id))
		container.add_child(button)


func _select_initial_entry() -> void:
	if _entries_by_id.has(_selected_quest_id):
		return
	var tracked_id: String = SafeTypeUtils.string(_journal_state.get("tracked_quest_id"))
	if _entries_by_id.has(tracked_id):
		_selected_quest_id = tracked_id
		return
	for section: String in ["active", "opportunities", "completed"]:
		var entries: Array = SafeTypeUtils.array(_journal_state.get(section))
		if not entries.is_empty():
			_selected_quest_id = SafeTypeUtils.string(SafeTypeUtils.dict(entries[0]).get("id"))
			return
	_selected_quest_id = ""


func _select_entry(quest_id: String) -> void:
	_selected_quest_id = quest_id
	_refresh_selection_buttons(active_list)
	_refresh_selection_buttons(opportunities_list)
	_refresh_selection_buttons(completed_list)
	_render_detail()


func _refresh_selection_buttons(container: VBoxContainer) -> void:
	for child: Node in container.get_children():
		var button: Button = child as Button
		if button != null:
			button.button_pressed = SafeTypeUtils.string(button.get_meta("quest_id")) == _selected_quest_id


func _render_detail() -> void:
	var entry: Dictionary = SafeTypeUtils.dict(_entries_by_id.get(_selected_quest_id))
	var has_entry: bool = not entry.is_empty()
	detail_empty.visible = not has_entry
	detail_content.visible = has_entry
	if not has_entry:
		detail_empty.text = Loc.t("academy.journal.empty")
		return

	detail_title.text = Loc.t(SafeTypeUtils.string(entry.get("title_key")))
	detail_title.add_theme_color_override("font_color", COLOR_HIGHLIGHT)
	detail_status.text = _entry_status(entry)
	detail_status.add_theme_color_override("font_color", COLOR_MUTED)
	detail_description.text = Loc.t(SafeTypeUtils.string(entry.get("description_key")))
	var objective_key: String = SafeTypeUtils.string(entry.get("current_objective_key"))
	detail_objective.visible = not objective_key.is_empty()
	if not objective_key.is_empty():
		detail_objective.text = Loc.t(
			"academy.journal.current_objective",
			{"objective": Loc.t(objective_key)}
		)

	var is_active: bool = SafeTypeUtils.string(entry.get("state")) == "active"
	track_button.visible = is_active
	track_button.disabled = SafeTypeUtils.bool_val(entry.get("is_tracked"), false)
	track_button.text = (
		Loc.t("academy.journal.tracked")
		if track_button.disabled
		else Loc.t("academy.journal.track")
	)


func _entry_status(entry: Dictionary) -> String:
	var state: String = SafeTypeUtils.string(entry.get("state"))
	var cost: int = SafeTypeUtils.int_val(entry.get("curriculum_cost"), 0)
	if state == "opportunity":
		return "%s • %s" % [
			Loc.t("academy.journal.known_opportunity"),
			Loc.t("academy.journal.cost", {"cost": cost}),
		]
	if state == "completed":
		return Loc.t("academy.journal.completed_status")
	return "%s • %s" % [
		Loc.t("academy.journal.cost", {"cost": cost}),
		Loc.t(
			"academy.journal.progress",
			{
				"current": SafeTypeUtils.int_val(entry.get("progress_current"), 0),
				"total": SafeTypeUtils.int_val(entry.get("progress_total"), 0),
			},
		),
	]


func _track_selected() -> void:
	if not _selected_quest_id.is_empty():
		CampaignApi.track_quest(_selected_quest_id)


func _go_back() -> void:
	var return_scene: String = NavigationContext.pop_return() if NavigationContext.has_return() else ""
	if return_scene.is_empty():
		return_scene = SceneManager.SCENE_WALKABLE_ACADEMY_HUB
	SceneManager.transition_to(return_scene)


func _clear_children(parent: Node) -> void:
	for child: Node in parent.get_children():
		child.queue_free()
