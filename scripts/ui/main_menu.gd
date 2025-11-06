extends Control
class_name MainMenu

## Main menu for Project Summoner
## Provides navigation to game modes and settings

@onready var placeholder_popup: AcceptDialog = $PlaceholderPopup

func _ready() -> void:
	print("Main Menu loaded")

func _input(event: InputEvent) -> void:
	# Debug: F11 to reset profile
	if event is InputEventKey and event.pressed and event.keycode == KEY_F11:
		print("MainMenu: F11 pressed - resetting profile...")
		var dev_console = get_node_or_null("/root/DevConsole")
		if dev_console:
			dev_console.execute_command("/save_wipe")
			# Reload the main menu to reflect fresh state
			get_tree().reload_current_scene()

## Launch the campaign screen (or onboarding if needed)
func _on_play_pressed() -> void:
	# Check if player has completed onboarding
	var profile_repo = get_node("/root/ProfileRepo")
	if profile_repo:
		var profile = profile_repo.get_active_profile()
		if not profile.is_empty():
			var onboarding_complete = profile.get("meta", {}).get("onboarding_complete", false)

			if not onboarding_complete:
				print("Opening onboarding - hero selection...")
				get_tree().change_scene_to_file("res://scenes/ui/hero_selection.tscn")
				return

	print("Opening campaign...")
	get_tree().change_scene_to_file("res://scenes/ui/campaign_screen.tscn")

## Open collection screen
func _on_collection_pressed() -> void:
	print("Opening collection screen...")
	get_tree().change_scene_to_file("res://scenes/ui/collection_screen.tscn")

## PLACEHOLDER - Settings screen not yet implemented
func _on_settings_pressed() -> void:
	print("Settings button pressed (PLACEHOLDER)")
	placeholder_popup.popup_centered()

## Quit the game
func _on_quit_pressed() -> void:
	print("Quitting game...")
	get_tree().quit()
