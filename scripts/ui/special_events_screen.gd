extends Control
class_name SpecialEventsScreen

## Special Events Screen - Placeholder for future features
##
## Will eventually show:
## - Limited-time events (seasonal, holidays)
## - Daily/Weekly challenges with milestone rewards

@onready var close_button: Button = %CloseButton
@onready var title_label: Label = $MarginContainer/VBoxContainer/Header/Title
@onready var coming_soon_label: Label = $MarginContainer/VBoxContainer/ContentCenter/ComingSoonPanel/MarginContainer/VBoxContainer/ComingSoonLabel
@onready var description_label: Label = $MarginContainer/VBoxContainer/ContentCenter/ComingSoonPanel/MarginContainer/VBoxContainer/DescriptionLabel

func _ready() -> void:
	# Set localized text
	title_label.text = Loc.t("ui.events.title")
	coming_soon_label.text = Loc.t("ui.events.coming_soon")
	description_label.text = Loc.t("ui.events.description")

	close_button.pressed.connect(_on_close_pressed)

func _on_close_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.UI_CLICK)
	var return_scene: String = NavigationContext.pop_return()
	if return_scene.is_empty():
		return_scene = SceneManager.SCENE_CAMPAIGN_MAP
	SceneManager.transition_to(return_scene)
