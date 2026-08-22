extends BackNavigableScreen
class_name QuestJournal

const SECTION_ACTIVE: String = "active"
const SECTION_OPEN: String = "opportunities"
const SECTION_COMPLETED: String = "completed"

@onready var background: ColorRect = %Background
@onready var back_button: Button = %BackButton
@onready var title_label: Label = %TitleLabel
@onready var term_label: Label = %TermLabel
@onready var capacity_label: Label = %CapacityLabel
@onready var category_panel: PanelContainer = %CategoryPanel
@onready var list_panel: PanelContainer = %ListPanel
@onready var detail_panel: PanelContainer = %DetailPanel
@onready var active_button: Button = %ActiveButton
@onready var open_button: Button = %OpenButton
@onready var completed_button: Button = %CompletedButton
@onready var quest_list: VBoxContainer = %QuestList
@onready var list_empty: Label = %ListEmpty
@onready var quest_detail: QuestDetailPanel = %QuestDetailPanel
@onready var track_button: Button = %TrackButton

var _journal_state: Dictionary = {}
var _section: String = SECTION_ACTIVE
var _selected_quest_id: String = ""
var _section_entries: Array = []
var _has_initialized_selection: bool = false


func _ready() -> void:
	_apply_palette()
	back_button.text = "←"
	back_button.tooltip_text = Loc.t("academy.hub.title")
	back_button.accessibility_name = Loc.t("academy.hub.title")
	title_label.text = Loc.t("academy.journal.title")
	back_button.pressed.connect(_go_back)
	active_button.pressed.connect(_select_section.bind(SECTION_ACTIVE))
	open_button.pressed.connect(_select_section.bind(SECTION_OPEN))
	completed_button.pressed.connect(_select_section.bind(SECTION_COMPLETED))
	track_button.pressed.connect(_track_selected)
	if Campaign.has_signal("CampaignProgressChanged"):
		Campaign.connect("CampaignProgressChanged", _refresh)
	_refresh()


func _refresh() -> void:
	_journal_state = CampaignApi.get_generic_quest_journal_state()
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
	_refresh_category_buttons()
	if not _has_initialized_selection:
		_select_tracked_section_if_needed()
		_has_initialized_selection = true
	_render_quest_list()


func _select_section(section: String) -> void:
	_section = section
	_selected_quest_id = ""
	_refresh_category_buttons()
	_render_quest_list()


func _refresh_category_buttons() -> void:
	active_button.text = "%s (%d)" % [
		Loc.t("academy.journal.active"),
		SafeTypeUtils.array(_journal_state.get(SECTION_ACTIVE)).size(),
	]
	open_button.text = "%s (%d)" % [
		Loc.t("academy.journal.open"),
		SafeTypeUtils.array(_journal_state.get(SECTION_OPEN)).size(),
	]
	completed_button.text = "%s (%d)" % [
		Loc.t("academy.journal.completed"),
		SafeTypeUtils.array(_journal_state.get(SECTION_COMPLETED)).size(),
	]
	active_button.button_pressed = _section == SECTION_ACTIVE
	open_button.button_pressed = _section == SECTION_OPEN
	completed_button.button_pressed = _section == SECTION_COMPLETED


func _select_tracked_section_if_needed() -> void:
	var tracked_id: String = SafeTypeUtils.string(_journal_state.get("tracked_quest_id"))
	if tracked_id.is_empty():
		return
	for section: String in [SECTION_ACTIVE, SECTION_OPEN, SECTION_COMPLETED]:
		for value: Variant in SafeTypeUtils.array(_journal_state.get(section)):
			var entry: Dictionary = SafeTypeUtils.dict(value)
			if SafeTypeUtils.string(entry.get("id")) == tracked_id:
				_section = section
				_selected_quest_id = tracked_id
				_refresh_category_buttons()
				return


func _render_quest_list() -> void:
	_clear_children(quest_list)
	_section_entries = SafeTypeUtils.array(_journal_state.get(_section))
	list_empty.visible = _section_entries.is_empty()
	list_empty.text = Loc.t("academy.journal.empty_%s" % _section)
	if _section_entries.is_empty():
		_selected_quest_id = ""
		_render_detail({})
		return

	if not _section_contains(_selected_quest_id):
		_selected_quest_id = SafeTypeUtils.string(
			SafeTypeUtils.dict(_section_entries[0]).get("id")
		)

	for value: Variant in _section_entries:
		var entry: Dictionary = SafeTypeUtils.dict(value)
		var quest_id: String = SafeTypeUtils.string(entry.get("id"))
		var button: Button = Button.new()
		button.text = Loc.t(SafeTypeUtils.string(entry.get("title_key")))
		button.alignment = HORIZONTAL_ALIGNMENT_LEFT
		button.custom_minimum_size = Vector2(0.0, 68.0)
		button.toggle_mode = true
		button.button_pressed = quest_id == _selected_quest_id
		button.set_meta("quest_id", quest_id)
		button.pressed.connect(_select_entry.bind(quest_id))
		quest_list.add_child(button)

	_render_detail(_selected_entry())


func _select_entry(quest_id: String) -> void:
	_selected_quest_id = quest_id
	for child: Node in quest_list.get_children():
		var button: Button = child as Button
		if button != null:
			button.button_pressed = SafeTypeUtils.string(button.get_meta("quest_id")) == quest_id
	_render_detail(_selected_entry())


func _render_detail(entry: Dictionary) -> void:
	quest_detail.present(
		entry,
		_entry_status(entry) if not entry.is_empty() else "",
		Loc.t("academy.journal.empty")
	)
	var is_active: bool = SafeTypeUtils.string(entry.get("state")) == SECTION_ACTIVE
	track_button.visible = is_active
	track_button.disabled = SafeTypeUtils.bool_val(entry.get("is_tracked"), false)
	track_button.text = (
		Loc.t("academy.journal.tracked")
		if track_button.disabled
		else Loc.t("academy.journal.track")
	)
func _entry_status(entry: Dictionary) -> String:
	var state: String = SafeTypeUtils.string(entry.get("state"))
	var cost: int = _curriculum_cost(entry)
	if state == SECTION_OPEN:
		return Loc.t("academy.journal.cost", {"cost": cost})
	if state == SECTION_COMPLETED:
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


func _curriculum_cost(entry: Dictionary) -> int:
	for value: Variant in SafeTypeUtils.array(entry.get("acceptance_previews")):
		var preview: Dictionary = SafeTypeUtils.dict(value)
		if SafeTypeUtils.string(preview.get("kind")) == "commit_curriculum_capacity":
			return SafeTypeUtils.int_val(preview.get("amount"), 0)
	return SafeTypeUtils.int_val(entry.get("curriculum_cost"), 0)


func _track_selected() -> void:
	if not _selected_quest_id.is_empty():
		CampaignApi.track_quest(_selected_quest_id)


func _section_contains(quest_id: String) -> bool:
	for value: Variant in _section_entries:
		if SafeTypeUtils.string(SafeTypeUtils.dict(value).get("id")) == quest_id:
			return true
	return false


func _selected_entry() -> Dictionary:
	for value: Variant in _section_entries:
		var entry: Dictionary = SafeTypeUtils.dict(value)
		if SafeTypeUtils.string(entry.get("id")) == _selected_quest_id:
			return entry
	return {}


func _apply_palette() -> void:
	background.color = GameColorPalette.UI_BACKGROUND
	_apply_panel_style(category_panel, GameColorPalette.UI_SURFACE_ALT)
	_apply_panel_style(list_panel, GameColorPalette.UI_SURFACE)
	_apply_panel_style(detail_panel, GameColorPalette.UI_SURFACE_RAISED)
	title_label.add_theme_color_override("font_color", GameColorPalette.TEXT_PRIMARY)
	term_label.add_theme_color_override("font_color", GameColorPalette.TEXT_SECONDARY)
	capacity_label.add_theme_color_override("font_color", GameColorPalette.TEXT_PRIMARY)
	list_empty.add_theme_color_override("font_color", GameColorPalette.TEXT_SECONDARY)


func _apply_panel_style(panel: PanelContainer, color: Color) -> void:
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = color
	style.border_color = GameColorPalette.UI_BORDER
	style.set_border_width_all(1)
	style.set_corner_radius_all(8)
	panel.add_theme_stylebox_override("panel", style)


func _go_back() -> void:
	var return_scene: String = NavigationContext.pop_return() if NavigationContext.has_return() else ""
	if return_scene.is_empty():
		return_scene = SceneManager.SCENE_WALKABLE_ACADEMY_HUB
	SceneManager.transition_to(return_scene)


func _clear_children(parent: Node) -> void:
	for child: Node in parent.get_children():
		child.queue_free()


func _request_back_navigation() -> void:
	_go_back()
