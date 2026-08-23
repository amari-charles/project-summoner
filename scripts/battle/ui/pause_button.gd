extends Button
class_name PauseButton

## Pause button for battle UI
## Always visible, allows pausing via button or ESC key

var game_controller: Node = null
var battle_menu: PauseMenu = null

func _ready() -> void:
	# Always process input (not affected by pause state)
	process_mode = PROCESS_MODE_ALWAYS

	# Connect button press
	pressed.connect(_on_pressed)

	# Find game controller
	call_deferred("_find_game_controller")

func _exit_tree() -> void:
	# Clean up signal connections to prevent memory leaks
	if game_controller and game_controller.is_connected("GameEnded", _on_game_ended):
		game_controller.disconnect("GameEnded", _on_game_ended)

func _find_game_controller() -> void:
	game_controller = get_tree().get_first_node_in_group(GroupIDs.GAME_CONTROLLER)
	battle_menu = get_node_or_null("../PauseMenu") as PauseMenu

	if not game_controller:
		push_error("PauseButton: Could not find game controller")
		return

	# Hide button when game ends
	game_controller.connect("GameEnded", _on_game_ended)
	text = Loc.t("ui.pause_menu.menu") if BattleContext.is_multiplayer_battle() else Loc.t("ui.pause_menu.pause")

func _on_game_ended(_winner: UnitConstants.Team) -> void:
	visible = false

func _unhandled_input(event: InputEvent) -> void:
	# ESC key handling (works even when paused because PROCESS_MODE_ALWAYS)
	if event is InputEventKey:
		var key_event: InputEventKey = event
		if key_event.pressed and key_event.keycode == KEY_ESCAPE:
			_toggle_pause()
			get_viewport().set_input_as_handled()

func _on_pressed() -> void:
	_toggle_pause()

func _toggle_pause() -> void:
	if not game_controller:
		return
	if battle_menu != null and battle_menu.close_settings_if_open():
		return
	if BattleContext.is_multiplayer_battle():
		if battle_menu != null:
			battle_menu.toggle_menu()
		return

	var current_state: int = SafeTypeUtils.int_val(game_controller.get("CurrentState"), int(UnitConstants.GameState.PLAYING))

	# Only allow pausing during active gameplay (not during setup or game over)
	if current_state == int(UnitConstants.GameState.PLAYING):
		if game_controller.has_method("PauseGame"):
			game_controller.call("PauseGame")
	elif current_state == int(UnitConstants.GameState.PAUSED):
		if game_controller.has_method("ResumeGame"):
			game_controller.call("ResumeGame")
