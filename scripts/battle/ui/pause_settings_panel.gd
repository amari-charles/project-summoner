extends Control
class_name PauseSettingsPanel

signal closed

@onready var close_button: Button = %CloseButton


func _ready() -> void:
	process_mode = PROCESS_MODE_ALWAYS
	visible = false
	var overlay: Control = get_node_or_null("BackgroundOverlay")
	if overlay != null:
		overlay.mouse_filter = Control.MOUSE_FILTER_STOP
	close_button.text = Loc.t("ui.common.close")
	close_button.pressed.connect(hide_panel)


func show_panel() -> void:
	visible = true


func hide_panel() -> void:
	if not visible:
		return
	visible = false
	closed.emit()
