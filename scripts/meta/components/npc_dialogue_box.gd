extends Control
class_name NpcDialogueBox

signal choice_selected(choice_id: String)
signal closed

@onready var panel: PanelContainer = %Panel
@onready var speaker_label: Label = %SpeakerLabel
@onready var line_label: RichTextLabel = %LineLabel
@onready var choices: Container = %Choices
@onready var advance_indicator: Label = %AdvanceIndicator

var _lines: Array[String] = []
var _line_index: int = 0
var _authored_choices: Array[Dictionary] = []


func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	hide()
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	gui_input.connect(_on_gui_input)
	panel.gui_input.connect(_on_gui_input)
	_apply_palette()


func present(speaker: String, lines: Array[String], response_choices: Array[Dictionary] = []) -> void:
	speaker_label.text = speaker
	_lines = lines
	_line_index = 0
	_authored_choices = response_choices
	show()
	mouse_filter = Control.MOUSE_FILTER_STOP
	_render_line()


func dismiss() -> void:
	_lines.clear()
	_authored_choices.clear()
	_clear_choices()
	hide()
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	closed.emit()


func _render_line() -> void:
	_clear_choices()
	if _line_index < _lines.size():
		line_label.text = _lines[_line_index]
		advance_indicator.visible = true
		return
	line_label.text = ""
	advance_indicator.visible = false
	if _authored_choices.is_empty():
		dismiss()
		return
	_release_dialogue_focus()
	for choice: Dictionary in _authored_choices:
		var button: Button = Button.new()
		button.text = "› %s" % SafeTypeUtils.string(choice.get("text"))
		button.alignment = HORIZONTAL_ALIGNMENT_LEFT
		button.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		button.custom_minimum_size = Vector2(0.0, 48.0)
		button.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		button.pressed.connect(_choose.bind(SafeTypeUtils.string(choice.get("id"))))
		choices.add_child(button)


func _on_gui_input(event: InputEvent) -> void:
	if (
		event is InputEventMouseButton
		and event.pressed
		and event.button_index == MOUSE_BUTTON_LEFT
	):
		_advance()


func _unhandled_key_input(event: InputEvent) -> void:
	if not visible or not event.pressed or event.echo:
		return
	if event.is_action("ui_cancel"):
		_skip_to_choices_or_dismiss()


func _input(event: InputEvent) -> void:
	if (
		visible
		and event.is_pressed()
		and not event.is_echo()
		and _line_index < _lines.size()
		and (
			event.is_action("interact")
			or event.is_action("ui_accept")
			or event.is_action("ui_select")
		)
	):
		_advance()
		get_viewport().set_input_as_handled()


func _advance() -> void:
	if not visible or _line_index >= _lines.size():
		return
	_line_index += 1
	_render_line()


func _skip_to_choices_or_dismiss() -> void:
	if not _authored_choices.is_empty():
		_line_index = _lines.size()
		_render_line()
	else:
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


func _clear_choices() -> void:
	for child: Node in choices.get_children():
		child.queue_free()


func _release_dialogue_focus() -> void:
	var focus_owner: Control = get_viewport().gui_get_focus_owner()
	if focus_owner != null:
		focus_owner.release_focus()
