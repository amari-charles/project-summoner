extends BackNavigableScreen
class_name SettingsScreen

@onready var close_button: Button = %CloseButton
@onready var title_label: Label = %Title


func _ready() -> void:
	title_label.text = Loc.t("ui.settings.title")
	close_button.pressed.connect(_on_close_pressed)


func _on_close_pressed() -> void:
	var return_scene: String = NavigationContext.pop_return()
	if return_scene.is_empty():
		return_scene = SceneManager.SCENE_CAMPAIGN_MAP
	SceneManager.transition_to(return_scene)


func _request_back_navigation() -> void:
	_on_close_pressed()
