extends Control
class_name QuestObjectiveIndicator

const ACCENT: Color = Color(1.0, 0.78, 0.16, 1.0)
const BORDER_WIDTH: float = 6.0
const PADDING: float = 8.0

var target: Control = null
var action_text: String = ""
var _pulse_time: float = 0.0
var _hint_label: Label = null


func _ready() -> void:
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	process_mode = Node.PROCESS_MODE_ALWAYS
	set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	_hint_label = Label.new()
	_hint_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_hint_label.add_theme_font_size_override("font_size", 20)
	_hint_label.add_theme_color_override("font_color", Color(0.12, 0.09, 0.03, 1.0))
	_hint_label.add_theme_stylebox_override("normal", _hint_style())
	add_child(_hint_label)


func _process(delta: float) -> void:
	_pulse_time += delta
	if not is_instance_valid(target) or not target.is_visible_in_tree():
		visible = false
		return
	visible = true
	_refresh_hint_position()
	queue_redraw()


func set_action_text(value: String) -> void:
	action_text = value
	if _hint_label != null:
		_hint_label.text = action_text


func _refresh_hint_position() -> void:
	if _hint_label == null:
		return
	_hint_label.text = action_text
	_hint_label.reset_size()
	var rect: Rect2 = target.get_global_rect().grow(PADDING)
	var hint_size: Vector2 = _hint_label.size
	var hint_y: float = rect.position.y - hint_size.y - 39.0
	if hint_y < 8.0:
		hint_y = rect.end.y + 15.0
	_hint_label.position = Vector2(
		clampf(rect.get_center().x - hint_size.x * 0.5, 8.0, size.x - hint_size.x - 8.0),
		hint_y
	)


func _draw() -> void:
	if not is_instance_valid(target):
		return
	var rect: Rect2 = target.get_global_rect().grow(PADDING)
	var pulse: float = (sin(_pulse_time * 5.0) + 1.0) * 0.5
	var color: Color = ACCENT
	color.a = lerpf(0.58, 1.0, pulse)
	draw_style_box(_outline_style(color), rect)

	var arrow_tip: Vector2 = Vector2(rect.get_center().x, rect.position.y - 3.0)
	var arrow_top: Vector2 = arrow_tip - Vector2(0.0, 22.0 + pulse * 7.0)
	draw_colored_polygon(
		PackedVector2Array([
			arrow_tip,
			arrow_top + Vector2(-13.0, 0.0),
			arrow_top + Vector2(13.0, 0.0),
		]),
		color
	)


func _outline_style(color: Color) -> StyleBoxFlat:
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = Color.TRANSPARENT
	style.border_color = color
	style.set_border_width_all(int(BORDER_WIDTH))
	style.set_corner_radius_all(10)
	return style


func _hint_style() -> StyleBoxFlat:
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = ACCENT
	style.border_color = Color(0.15, 0.11, 0.03, 1.0)
	style.set_border_width_all(2)
	style.set_corner_radius_all(6)
	style.content_margin_left = 12.0
	style.content_margin_right = 12.0
	style.content_margin_top = 6.0
	style.content_margin_bottom = 6.0
	return style
