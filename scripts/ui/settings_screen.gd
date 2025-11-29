extends Control
class_name SettingsScreen

## Settings Screen - Placeholder for future features
##
## Will eventually include:
## - Audio settings (music/SFX volume)
## - Display settings
## - Account/profile management
## - Return to title screen option

@onready var close_button: Button = %CloseButton

func _ready() -> void:
	close_button.pressed.connect(_on_close_pressed)

func _on_close_pressed() -> void:
	var return_scene: String = NavigationContext.pop_return()
	if return_scene.is_empty():
		return_scene = SceneManager.SCENE_CAMPAIGN_MAP
	SceneManager.transition_to(return_scene)
