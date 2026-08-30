extends Control
class_name QuestObjectiveIndicator

const ACCENT: Color = Color(1.0, 0.78, 0.16, 1.0)
const BORDER_WIDTH: float = 6.0
const PADDING: float = 8.0

var target: Control = null
var _pulse_time: float = 0.0


func _ready() -> void:
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	process_mode = Node.PROCESS_MODE_ALWAYS
	set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)


func _process(delta: float) -> void:
	_pulse_time += delta
	if not is_instance_valid(target) or not target.is_visible_in_tree():
		visible = false
		return
	visible = true
	queue_redraw()


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
