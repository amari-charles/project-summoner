extends Control
class_name DialogueBoxBase

## Shared dialogue-flow and input contract. Context adapters provide localized
## line/choice rendering and completion behavior, while this base owns the state
## machine that must remain identical across every dialogue surface.

var _lines: Array = []
var _line_index: int = 0
var _authored_choices: Array[Dictionary] = []

var _dialogue_panel: PanelContainer
var _dialogue_choices: Container


func _initialize_dialogue_box(
	panel_control: PanelContainer,
	choices_container: Container
) -> void:
	_dialogue_panel = panel_control
	_dialogue_choices = choices_container
	process_mode = Node.PROCESS_MODE_ALWAYS
	hide()
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	gui_input.connect(_on_dialogue_gui_input)
	_dialogue_panel.gui_input.connect(_on_dialogue_gui_input)


func _present_dialogue(lines: Array, response_choices: Array[Dictionary]) -> void:
	_lines = lines.duplicate()
	_line_index = 0
	_authored_choices = response_choices.duplicate(true)
	# Dialogue owns the interaction keys while it is visible. Clear focus left by
	# world HUD controls so Space cannot activate one behind the conversation.
	_release_dialogue_focus()
	show()
	mouse_filter = Control.MOUSE_FILTER_STOP
	_render_dialogue_state()


func _hide_dialogue() -> void:
	_lines.clear()
	_authored_choices.clear()
	_clear_dialogue_choices()
	hide()
	mouse_filter = Control.MOUSE_FILTER_IGNORE


func _render_dialogue_state() -> void:
	_clear_dialogue_choices()
	if _line_index < _lines.size():
		_display_dialogue_line(SafeTypeUtils.string(_lines[_line_index]))
		_set_dialogue_advance_visible(true)
		return

	_display_dialogue_line("")
	_set_dialogue_advance_visible(false)
	if _authored_choices.is_empty():
		_on_dialogue_exhausted()
		return

	_release_dialogue_focus()
	_render_dialogue_choices(_authored_choices)


func _input(event: InputEvent) -> void:
	if not visible or not event.is_pressed() or event.is_echo():
		return
	if event.is_action("ui_cancel"):
		_on_dialogue_cancelled()
		get_viewport().set_input_as_handled()
		return
	if not _is_dialogue_advance_event(event):
		return

	if _line_index < _lines.size():
		_advance_dialogue()
		get_viewport().set_input_as_handled()
		return

	# Responses begin unfocused. Consume an unfocused advance action so world
	# interaction cannot receive the same input and restart the conversation.
	# A deliberately focused response remains selectable through Button input.
	if not _authored_choices.is_empty() and not _dialogue_choice_has_focus():
		get_viewport().set_input_as_handled()


func _on_dialogue_gui_input(event: InputEvent) -> void:
	if (
		event is InputEventMouseButton
		and event.pressed
		and event.button_index == MOUSE_BUTTON_LEFT
	):
		_advance_dialogue()


func _is_dialogue_advance_event(event: InputEvent) -> bool:
	if event is InputEventKey:
		var key_event: InputEventKey = event as InputEventKey
		if key_event.keycode == KEY_SPACE or key_event.physical_keycode == KEY_SPACE:
			return true
	return (
		event.is_action("interact")
		or event.is_action("ui_accept")
		or event.is_action("ui_select")
	)


func _advance_dialogue() -> void:
	if not visible or _line_index >= _lines.size():
		return
	_line_index += 1
	_render_dialogue_state()


func _skip_to_dialogue_choices_or_finish() -> void:
	if not _authored_choices.is_empty():
		_line_index = _lines.size()
		_render_dialogue_state()
	else:
		_on_dialogue_skipped()


func _clear_dialogue_choices() -> void:
	if _dialogue_choices == null:
		return
	for child: Node in _dialogue_choices.get_children():
		child.queue_free()


func _release_dialogue_focus() -> void:
	var focus_owner: Control = get_viewport().gui_get_focus_owner()
	if focus_owner != null:
		focus_owner.release_focus()


func _dialogue_choice_has_focus() -> bool:
	var focus_owner: Control = get_viewport().gui_get_focus_owner()
	return focus_owner is Button and focus_owner.get_parent() == _dialogue_choices


# Adapter hooks.
func _display_dialogue_line(_line: String) -> void:
	push_error("DialogueBoxBase: Adapter must implement _display_dialogue_line")


func _set_dialogue_advance_visible(_is_visible: bool) -> void:
	pass


func _render_dialogue_choices(_choices: Array[Dictionary]) -> void:
	push_error("DialogueBoxBase: Adapter must implement _render_dialogue_choices")


func _on_dialogue_exhausted() -> void:
	push_error("DialogueBoxBase: Adapter must implement _on_dialogue_exhausted")


func _on_dialogue_skipped() -> void:
	_on_dialogue_exhausted()


func _on_dialogue_cancelled() -> void:
	push_error("DialogueBoxBase: Adapter must implement _on_dialogue_cancelled")
