extends Control
class_name PauseMenu

## Pause menu overlay for battles
## Shows when game is paused, allows resume or quit to menu

var game_controller: GameController3D

@onready var resume_button: Button = %ResumeButton
@onready var quit_button: Button = %QuitButton

func _ready() -> void:
	# CRITICAL: Process input even when game is paused
	process_mode = PROCESS_MODE_WHEN_PAUSED

	# Start hidden (also set in scene, but enforce here)
	visible = false

	# Connect button signals first
	resume_button.pressed.connect(_on_resume_pressed)
	quit_button.pressed.connect(_on_quit_pressed)

	# Find game controller (deferred to ensure it's ready)
	call_deferred("_find_game_controller")

func _find_game_controller() -> void:
	game_controller = get_tree().get_first_node_in_group("game_controller")

	if not game_controller:
		push_error("PauseMenu: Could not find GameController3D in scene")
		return

	# Listen for pause state changes
	game_controller.state_changed.connect(_on_game_state_changed)

	# Sync initial state
	_on_game_state_changed(game_controller.current_state)

func _input(event: InputEvent) -> void:
	# ESC key toggles pause
	if event is InputEventKey and event.pressed and event.keycode == KEY_ESCAPE:
		if not game_controller:
			return

		if game_controller.current_state == GameController3D.GameState.PLAYING:
			game_controller.pause_game()
		elif game_controller.current_state == GameController3D.GameState.PAUSED:
			game_controller.resume_game()

		# Consume input so it doesn't propagate
		get_viewport().set_input_as_handled()

## Show/hide based on game state
func _on_game_state_changed(new_state: GameController3D.GameState) -> void:
	visible = (new_state == GameController3D.GameState.PAUSED)

## Resume button - unpause game
func _on_resume_pressed() -> void:
	if not game_controller:
		push_error("PauseMenu: Cannot resume - no game controller")
		return
	game_controller.resume_game()

## Quit button - return to campaign screen
func _on_quit_pressed() -> void:
	# CRITICAL: Unpause before changing scenes
	get_tree().paused = false
	get_tree().change_scene_to_file("res://scenes/ui/campaign_screen.tscn")
