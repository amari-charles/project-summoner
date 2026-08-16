extends Control
class_name NpcDialogueBox

signal choice_selected(choice_id: String)
signal closed

@onready var panel: PanelContainer = %Panel
@onready var speaker_label: Label = %SpeakerLabel
@onready var line_label: RichTextLabel = %LineLabel
@onready var choices: HBoxContainer = %Choices

var _lines: Array[String] = []
var _line_index: int = 0
var _authored_choices: Array[Dictionary] = []


func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	hide()
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	gui_input.connect(_on_gui_input)
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
		return
	line_label.text = ""
	if _authored_choices.is_empty():
		dismiss()
		return
	for choice: Dictionary in _authored_choices:
		var button: Button = Button.new()
		button.text = SafeTypeUtils.string(choice.get("text"))
		button.custom_minimum_size = Vector2(150.0, 44.0)
		button.pressed.connect(_choose.bind(SafeTypeUtils.string(choice.get("id"))))
		choices.add_child(button)


func _on_gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.pressed:
		_advance()


func _unhandled_key_input(event: InputEvent) -> void:
	if visible and event.pressed and (event.is_action("ui_accept") or event.is_action("ui_select")):
		_advance()


func _advance() -> void:
	if not visible or _line_index >= _lines.size():
		return
	_line_index += 1
	_render_line()


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
