extends Control
class_name OnlineScreen

## Online Screen - Placeholder for online play features
##
## Will eventually show:
## - Casual matchmaking
## - Ranked competitive play
## - Leaderboards and seasons

@onready var close_button: Button = %CloseButton
@onready var title_label: Label = $MarginContainer/VBoxContainer/Header/Title
@onready var coming_soon_label: Label = $MarginContainer/VBoxContainer/ContentCenter/ComingSoonPanel/MarginContainer/VBoxContainer/ComingSoonLabel
@onready var description_label: Label = $MarginContainer/VBoxContainer/ContentCenter/ComingSoonPanel/MarginContainer/VBoxContainer/DescriptionLabel

func _ready() -> void:
	# Set localized text
	title_label.text = Loc.t("ui.online.title")
	coming_soon_label.text = Loc.t("ui.online.coming_soon")
	description_label.text = Loc.t("ui.online.description")

	close_button.pressed.connect(_on_close_pressed)

func _on_close_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	var return_scene: String = NavigationContext.pop_return()
	if return_scene.is_empty():
		return_scene = SceneManager.SCENE_CAMPAIGN_MAP
	SceneManager.transition_to(return_scene)
