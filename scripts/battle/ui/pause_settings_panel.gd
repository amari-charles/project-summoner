extends Control
class_name PauseSettingsPanel

## Settings panel overlay for pause menu
## Shows volume controls without leaving the battle

@onready var title_label: Label = %TitleLabel
@onready var music_label: Label = %MusicLabel
@onready var music_slider: HSlider = %MusicSlider
@onready var music_value_label: Label = %MusicValueLabel
@onready var sfx_label: Label = %SFXLabel
@onready var sfx_slider: HSlider = %SFXSlider
@onready var sfx_value_label: Label = %SFXValueLabel
@onready var close_button: Button = %CloseButton


func _ready() -> void:
	process_mode = PROCESS_MODE_WHEN_PAUSED
	visible = false

	# Prevent clicks from passing through overlay
	var overlay: Control = get_node_or_null("BackgroundOverlay")
	if overlay:
		overlay.mouse_filter = Control.MOUSE_FILTER_STOP

	_setup_localized_text()
	_load_current_settings()
	_connect_signals()


func _setup_localized_text() -> void:
	title_label.text = Loc.t("ui.settings.title")
	music_label.text = Loc.t("ui.settings.music_volume")
	sfx_label.text = Loc.t("ui.settings.sfx_volume")
	close_button.text = Loc.t("ui.common.close")


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


func show_panel() -> void:
	_load_current_settings()
	visible = true


func hide_panel() -> void:
	visible = false


func _on_music_volume_changed(value: float) -> void:
	AudioManager.set_volume(AudioManager.BUS_MUSIC, value)
	_update_value_labels()


func _on_sfx_volume_changed(value: float) -> void:
	AudioManager.set_volume(AudioManager.BUS_SFX, value)
	_update_value_labels()


func _on_close_pressed() -> void:
	hide_panel()
