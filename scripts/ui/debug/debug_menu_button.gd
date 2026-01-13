extends Button
class_name DebugMenuButton

## Opens pause menu in debug arena
## Handles ESC key for menu access

var game_controller: GameController3D = null

func _ready() -> void:
	process_mode = PROCESS_MODE_ALWAYS
	pressed.connect(_on_pressed)
	call_deferred("_find_game_controller")


func _find_game_controller() -> void:
	game_controller = get_tree().get_first_node_in_group(GroupIDs.GAME_CONTROLLER)


func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventKey:
		var key_event: InputEventKey = event
		if key_event.pressed and key_event.keycode == KEY_ESCAPE:
			_open_menu()
			get_viewport().set_input_as_handled()


func _on_pressed() -> void:
	_open_menu()


func _open_menu() -> void:
	if game_controller:
		game_controller.pause_game()
