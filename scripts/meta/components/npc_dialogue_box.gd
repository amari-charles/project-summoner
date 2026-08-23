extends DialogueBoxBase
class_name NpcDialogueBox

signal choice_selected(choice_id: String)
signal closed

@onready var panel: PanelContainer = %Panel
@onready var speaker_label: Label = %SpeakerLabel
@onready var line_label: RichTextLabel = %LineLabel
@onready var choices: Container = %Choices
@onready var advance_indicator: Label = %AdvanceIndicator

func _ready() -> void:
	_initialize_dialogue_box(panel, choices)
	_apply_palette()


func present(speaker: String, lines: Array[String], response_choices: Array[Dictionary] = []) -> void:
	speaker_label.text = speaker
	_present_dialogue(lines, response_choices)


func dismiss() -> void:
	_hide_dialogue()
	closed.emit()


func _display_dialogue_line(line: String) -> void:
	line_label.text = line


func _set_dialogue_advance_visible(is_visible: bool) -> void:
	advance_indicator.visible = is_visible


func _render_dialogue_choices(response_choices: Array[Dictionary]) -> void:
	for choice: Dictionary in response_choices:
		var button: Button = Button.new()
		button.text = "› %s" % SafeTypeUtils.string(choice.get("text"))
		button.alignment = HORIZONTAL_ALIGNMENT_LEFT
		button.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		button.custom_minimum_size = Vector2(0.0, 48.0)
		button.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		button.pressed.connect(_choose.bind(SafeTypeUtils.string(choice.get("id"))))
		choices.add_child(button)


func _skip_to_choices_or_dismiss() -> void:
	_skip_to_dialogue_choices_or_finish()


func _on_dialogue_exhausted() -> void:
	dismiss()


func _on_dialogue_skipped() -> void:
	dismiss()


func _on_dialogue_cancelled() -> void:
	dismiss()


func _choose(choice_id: String) -> void:
	dismiss()
	choice_selected.emit(choice_id)


func _apply_palette() -> void:
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = GameColorPalette.UI_SURFACE_RAISED
	style.border_color = GameColorPalette.UI_BORDER
	style.set_border_width_all(2)
	style.set_corner_radius_all(10)
	panel.add_theme_stylebox_override("panel", style)
	speaker_label.add_theme_color_override("font_color", GameColorPalette.TEXT_HIGHLIGHT)
	line_label.add_theme_color_override("default_color", GameColorPalette.TEXT_PRIMARY)
