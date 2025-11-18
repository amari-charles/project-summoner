extends Control
class_name GameModeMenu

## Game Mode Selection Menu
## Allows player to choose between Campaign, Shop, Collection, and Events

@onready var placeholder_popup: AcceptDialog = $PlaceholderPopup

func _ready() -> void:
	print("Game Mode Menu loaded")

## Navigate to Campaign screen
func _on_campaign_pressed() -> void:
	print("Opening campaign screen...")
	get_tree().change_scene_to_file("res://scenes/ui/campaign_screen.tscn")

## PLACEHOLDER - Shop not yet implemented
func _on_shop_pressed() -> void:
	print("Shop button pressed (PLACEHOLDER)")
	placeholder_popup.popup_centered()

## Navigate to Collection screen
func _on_collection_pressed() -> void:
	print("Opening collection screen...")
	get_tree().change_scene_to_file("res://scenes/ui/collection_screen.tscn")

## PLACEHOLDER - Events not yet implemented
func _on_events_pressed() -> void:
	print("Events button pressed (PLACEHOLDER)")
	placeholder_popup.popup_centered()

## Return to Main Menu
func _on_back_pressed() -> void:
	print("Returning to main menu...")
	get_tree().change_scene_to_file("res://scenes/ui/main_menu.tscn")
