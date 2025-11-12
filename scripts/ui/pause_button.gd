extends Button
class_name PauseButton

## Pause button for battle UI
## Always visible, allows pausing via button or ESC key

var game_controller: GameController3D

func _ready() -> void:
	# Always process input (not affected by pause state)
	process_mode = PROCESS_MODE_ALWAYS

	# Connect button press
	pressed.connect(_on_pressed)

	# Find game controller
	call_deferred("_find_game_controller")

func _find_game_controller() -> void:
	game_controller = get_tree().get_first_node_in_group("game_controller")

	if not game_controller:
		push_error("PauseButton: Could not find GameController3D")

func _unhandled_input(event: InputEvent) -> void:
	# ESC key handling (works even when paused because PROCESS_MODE_ALWAYS)
	if event is InputEventKey and event.pressed and event.keycode == KEY_ESCAPE:
		_toggle_pause()
		get_viewport().set_input_as_handled()

func _on_pressed() -> void:
	_toggle_pause()

func _toggle_pause() -> void:
	if not game_controller:
		return

	if game_controller.current_state == GameController3D.GameState.PLAYING:
		game_controller.pause_game()
	elif game_controller.current_state == GameController3D.GameState.PAUSED:
		game_controller.resume_game()
