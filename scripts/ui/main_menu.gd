extends Control
class_name MainMenu

## Main menu for Project Summoner
## Provides navigation to game modes and settings

@onready var placeholder_popup: AcceptDialog = $PlaceholderPopup

func _ready() -> void:
	print("Main Menu loaded")

func _input(event: InputEvent) -> void:
	# Debug: F11 to reset profile
	if event is InputEventKey:
		var key_event: InputEventKey = event as InputEventKey
		if key_event.pressed and not key_event.is_echo() and key_event.keycode == KEY_F11:
			print("MainMenu: F11 pressed - resetting profile...")
			var dev_console: Node = get_node_or_null("/root/DevConsole")
			if dev_console:
				dev_console.call("execute_command", "/save_wipe")
				# Reload the main menu to reflect fresh state
				get_tree().reload_current_scene()

## Open game mode selection screen
func _on_play_pressed() -> void:
	print("Opening game mode selection...")
	SceneManager.change_scene(SceneManager.SCENE_GAME_MODE_MENU)

## PLACEHOLDER - Achievements not yet implemented
func _on_achievements_pressed() -> void:
	print("Achievements button pressed (PLACEHOLDER)")
	placeholder_popup.popup_centered()

## PLACEHOLDER - Settings screen not yet implemented
func _on_settings_pressed() -> void:
	print("Settings button pressed (PLACEHOLDER)")
	placeholder_popup.popup_centered()

## DEBUG: Reset profile button
func _on_debug_reset_pressed() -> void:
	print("MainMenu: Debug reset button pressed - resetting profile...")
	var dev_console: Node = get_node_or_null("/root/DevConsole")
	if dev_console:
		dev_console.call("execute_command", "/save_wipe")
		# Reload the main menu to reflect fresh state
		get_tree().reload_current_scene()
	else:
		push_warning("DevConsole not found - cannot reset profile")

## Quit the game
func _on_quit_pressed() -> void:
	print("Quitting game...")
	get_tree().quit()
