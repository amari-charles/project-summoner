extends Control
class_name MainMenu

## Main menu for Project Summoner
## Provides navigation to game modes and settings

@onready var placeholder_popup: AcceptDialog = $PlaceholderPopup

func _ready() -> void:
	print("Main Menu loaded")

## Launch the campaign screen
func _on_play_pressed() -> void:
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
