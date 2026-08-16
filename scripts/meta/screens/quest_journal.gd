extends Control
class_name QuestJournal

const COLOR_PANEL: Color = GameColorPalette.UI_SURFACE
const COLOR_BORDER: Color = GameColorPalette.UI_BORDER
const COLOR_MUTED: Color = GameColorPalette.TEXT_SECONDARY
const COLOR_HIGHLIGHT: Color = GameColorPalette.TEXT_HIGHLIGHT

@onready var back_button: Button = %BackButton
@onready var title_label: Label = %TitleLabel
@onready var term_label: Label = %TermLabel
@onready var capacity_label: Label = %CapacityLabel
@onready var active_button: Button = %ActiveButton
@onready var opportunities_button: Button = %OpportunitiesButton
@onready var completed_button: Button = %CompletedButton
@onready var entry_list: VBoxContainer = %EntryList
@onready var empty_label: Label = %EmptyLabel

var _journal_state: Dictionary = {}
var _section: String = "active"


func _ready() -> void:
	back_button.text = "←"
	back_button.tooltip_text = Loc.t("academy.hub.title")
	back_button.accessibility_name = Loc.t("academy.hub.title")
	title_label.text = Loc.t("academy.journal.title")
	back_button.pressed.connect(_go_back)
	active_button.pressed.connect(_select_section.bind("active"))
	opportunities_button.pressed.connect(_select_section.bind("opportunities"))
	completed_button.pressed.connect(_select_section.bind("completed"))
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
	_refresh_tabs()
	_render_entries()


func _select_section(section: String) -> void:
	_section = section
	_refresh_tabs()
	_render_entries()


func _refresh_tabs() -> void:
	var active_count: int = SafeTypeUtils.array(_journal_state.get("active")).size()
	var opportunity_count: int = SafeTypeUtils.array(_journal_state.get("opportunities")).size()
	var completed_count: int = SafeTypeUtils.array(_journal_state.get("completed")).size()
	active_button.text = "%s (%d)" % [Loc.t("academy.journal.active"), active_count]
	opportunities_button.text = "%s (%d)" % [
		Loc.t("academy.journal.opportunities"), opportunity_count
	]
	completed_button.text = "%s (%d)" % [Loc.t("academy.journal.completed"), completed_count]
	active_button.button_pressed = _section == "active"
	opportunities_button.button_pressed = _section == "opportunities"
	completed_button.button_pressed = _section == "completed"


func _render_entries() -> void:
	_clear_children(entry_list)
	var entries: Array = SafeTypeUtils.array(_journal_state.get(_section))
	empty_label.visible = entries.is_empty()
	empty_label.text = Loc.t("academy.journal.empty_%s" % _section)
	for value: Variant in entries:
		var entry: Dictionary = SafeTypeUtils.dict(value)
		if not entry.is_empty():
			entry_list.add_child(_build_entry(entry))


func _build_entry(entry: Dictionary) -> Control:
	var panel: PanelContainer = PanelContainer.new()
	panel.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = COLOR_PANEL
	style.border_color = COLOR_BORDER
	style.set_border_width_all(1)
	style.set_corner_radius_all(8)
	panel.add_theme_stylebox_override("panel", style)

	var margin: MarginContainer = MarginContainer.new()
	margin.add_theme_constant_override("margin_left", 18)
	margin.add_theme_constant_override("margin_top", 14)
	margin.add_theme_constant_override("margin_right", 18)
	margin.add_theme_constant_override("margin_bottom", 14)
	panel.add_child(margin)

	var content: VBoxContainer = VBoxContainer.new()
	content.add_theme_constant_override("separation", 7)
	margin.add_child(content)

	var title: Label = Label.new()
	title.text = Loc.t(SafeTypeUtils.string(entry.get("title_key")))
	title.add_theme_font_size_override("font_size", 22)
	title.add_theme_color_override("font_color", COLOR_HIGHLIGHT)
	content.add_child(title)

	var details: Label = Label.new()
	details.text = _entry_details(entry)
	details.add_theme_color_override("font_color", COLOR_MUTED)
	content.add_child(details)

	var description: Label = Label.new()
	description.text = Loc.t(SafeTypeUtils.string(entry.get("description_key")))
	description.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	content.add_child(description)

	var objective_key: String = SafeTypeUtils.string(entry.get("current_objective_key"))
	if not objective_key.is_empty():
		var objective: Label = Label.new()
		objective.text = Loc.t(
			"academy.journal.current_objective",
			{"objective": Loc.t(objective_key)}
		)
		objective.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		content.add_child(objective)

	return panel


func _entry_details(entry: Dictionary) -> String:
	var state: String = SafeTypeUtils.string(entry.get("state"))
	var cost: int = SafeTypeUtils.int_val(entry.get("curriculum_cost"), 0)
	if state == "opportunity":
		return "%s • %s" % [
			Loc.t("academy.journal.known_opportunity"),
			Loc.t("academy.journal.cost", {"cost": cost})
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
			}
		)
	]


func _go_back() -> void:
	var return_scene: String = NavigationContext.pop_return() if NavigationContext.has_return() else ""
	if return_scene.is_empty():
		return_scene = SceneManager.SCENE_WALKABLE_ACADEMY_HUB
	SceneManager.transition_to(return_scene)


func _clear_children(parent: Node) -> void:
	for child: Node in parent.get_children():
		child.queue_free()
