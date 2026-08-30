extends Control
class_name ShowcaseMessageModal

signal closed

@onready var panel: PanelContainer = %Panel
@onready var title_label: Label = %TitleLabel
@onready var message_label: RichTextLabel = %MessageLabel
@onready var continue_button: Button = %ContinueButton


func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	hide()
	continue_button.pressed.connect(_close)
	_apply_palette()


func present(title: String, message: String, action: String) -> void:
	title_label.text = title
	message_label.text = message
	continue_button.text = action
	show()
	continue_button.call_deferred("grab_focus")


func _close() -> void:
	hide()
	closed.emit()


func _unhandled_key_input(event: InputEvent) -> void:
	if not visible or not event.pressed or event.echo or not event.is_action("ui_cancel"):
		return
	get_viewport().set_input_as_handled()
	_close()


func _apply_palette() -> void:
	var panel_style: StyleBoxFlat = StyleBoxFlat.new()
	panel_style.bg_color = GameColorPalette.UI_SURFACE_RAISED
	panel_style.border_color = GameColorPalette.UI_BORDER
	panel_style.set_border_width_all(2)
	panel_style.set_corner_radius_all(12)
	panel.add_theme_stylebox_override("panel", panel_style)
	title_label.add_theme_color_override("font_color", GameColorPalette.TEXT_HIGHLIGHT)
	message_label.add_theme_color_override("default_color", GameColorPalette.TEXT_PRIMARY)
