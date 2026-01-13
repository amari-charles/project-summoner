extends Button
class_name DebugPauseButton

## Debug pause button - freezes gameplay but allows unit placement
## Uses Engine.time_scale instead of tree.paused

var _is_paused: bool = false

func _ready() -> void:
	process_mode = PROCESS_MODE_ALWAYS
	pressed.connect(_on_pressed)
	_update_text()


func _exit_tree() -> void:
	Engine.time_scale = 1.0


func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventKey:
		var key_event: InputEventKey = event
		if key_event.pressed and key_event.keycode == KEY_ESCAPE:
			_toggle_pause()
			get_viewport().set_input_as_handled()


func _on_pressed() -> void:
	_toggle_pause()


func _toggle_pause() -> void:
	_is_paused = not _is_paused
	Engine.time_scale = 0.0 if _is_paused else 1.0
	_update_text()


func _update_text() -> void:
	text = "▶ Resume" if _is_paused else "⏸ Pause"
