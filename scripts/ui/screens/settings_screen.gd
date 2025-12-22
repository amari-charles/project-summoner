extends Control
class_name SettingsScreen

## Settings Screen - Audio volume controls and future settings
##
## Features:
## - Music volume slider
## - SFX volume slider
## - Settings persist via ProfileRepo

@onready var close_button: Button = %CloseButton
@onready var title_label: Label = %Title
@onready var audio_header: Label = %AudioHeader
@onready var music_label: Label = %MusicLabel
@onready var music_slider: HSlider = %MusicSlider
@onready var music_value_label: Label = %MusicValueLabel
@onready var sfx_label: Label = %SFXLabel
@onready var sfx_slider: HSlider = %SFXSlider
@onready var sfx_value_label: Label = %SFXValueLabel
@onready var coming_soon_label: Label = %ComingSoonLabel


func _ready() -> void:
	_setup_localized_text()
	_load_current_settings()
	_connect_signals()


func _setup_localized_text() -> void:
	title_label.text = Loc.t("ui.settings.title")
	audio_header.text = Loc.t("ui.settings.audio_header")
	music_label.text = Loc.t("ui.settings.music_volume")
	sfx_label.text = Loc.t("ui.settings.sfx_volume")
	coming_soon_label.text = Loc.t("ui.settings.coming_soon")


func _load_current_settings() -> void:
	music_slider.value = AudioManager.get_volume(AudioManager.BUS_MUSIC)
	sfx_slider.value = AudioManager.get_volume(AudioManager.BUS_SFX)
	_update_value_labels()


func _connect_signals() -> void:
	close_button.pressed.connect(_on_close_pressed)
	music_slider.value_changed.connect(_on_music_volume_changed)
	sfx_slider.value_changed.connect(_on_sfx_volume_changed)


func _update_value_labels() -> void:
	music_value_label.text = AudioManager.format_volume_percent(music_slider.value)
	sfx_value_label.text = AudioManager.format_volume_percent(sfx_slider.value)


func _on_music_volume_changed(value: float) -> void:
	AudioManager.set_volume(AudioManager.BUS_MUSIC, value)
	_update_value_labels()


func _on_sfx_volume_changed(value: float) -> void:
	AudioManager.set_volume(AudioManager.BUS_SFX, value)
	_update_value_labels()


func _on_close_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	var return_scene: String = NavigationContext.pop_return()
	if return_scene.is_empty():
		return_scene = SceneManager.SCENE_CAMPAIGN_MAP
	SceneManager.transition_to(return_scene)
