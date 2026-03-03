extends Button
class_name DebugMenuButton

## Opens pause menu in debug arena

var game_controller: Node = null

func _ready() -> void:
	process_mode = PROCESS_MODE_ALWAYS
	pressed.connect(_on_pressed)
	call_deferred("_find_game_controller")


func _find_game_controller() -> void:
	game_controller = get_tree().get_first_node_in_group(GroupIDs.GAME_CONTROLLER)


func _on_pressed() -> void:
	_open_menu()


func _open_menu() -> void:
	if game_controller:
		game_controller.PauseGame()
