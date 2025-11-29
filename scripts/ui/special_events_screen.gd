extends Control
class_name SpecialEventsScreen

## Special Events Screen - Placeholder for future features
##
## Will eventually show:
## - Limited-time events (seasonal, holidays)
## - Daily/Weekly challenges with milestone rewards

@onready var close_button: Button = %CloseButton

func _ready() -> void:
	close_button.pressed.connect(_on_close_pressed)

func _on_close_pressed() -> void:
	var return_scene: String = NavigationContext.pop_return()
	if return_scene.is_empty():
		return_scene = SceneManager.SCENE_CAMPAIGN_MAP
	SceneManager.transition_to(return_scene)
