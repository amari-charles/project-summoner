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
	_apply_high_contrast_palette()


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


func _apply_high_contrast_palette() -> void:
	var panel_style: StyleBoxFlat = StyleBoxFlat.new()
	panel_style.bg_color = Color("17140f")
	panel_style.border_color = Color("ffd45a")
	panel_style.set_border_width_all(4)
	panel_style.set_corner_radius_all(14)
	panel_style.shadow_color = Color(0, 0, 0, 0.75)
	panel_style.shadow_size = 24
	panel.add_theme_stylebox_override("panel", panel_style)
	title_label.add_theme_color_override("font_color", Color("fff1b8"))
	message_label.add_theme_color_override("default_color", Color("fffaf0"))
	message_label.add_theme_color_override("font_color", Color("fffaf0"))
	message_label.add_theme_color_override("font_outline_color", Color(0, 0, 0, 1))
	message_label.add_theme_constant_override("outline_size", 2)
	var normal: StyleBoxFlat = StyleBoxFlat.new()
	normal.bg_color = Color("f4c84a")
	normal.border_color = Color("fff1a8")
	normal.set_border_width_all(3)
	normal.set_corner_radius_all(8)
	var hover: StyleBoxFlat = normal.duplicate()
	hover.bg_color = Color("ffe073")
	var pressed: StyleBoxFlat = normal.duplicate()
	pressed.bg_color = Color("dba82c")
	continue_button.add_theme_stylebox_override("normal", normal)
	continue_button.add_theme_stylebox_override("hover", hover)
	continue_button.add_theme_stylebox_override("focus", hover)
	continue_button.add_theme_stylebox_override("pressed", pressed)
	for color_name: String in ["font_color", "font_hover_color", "font_focus_color", "font_pressed_color"]:
		continue_button.add_theme_color_override(color_name, Color("211805"))
