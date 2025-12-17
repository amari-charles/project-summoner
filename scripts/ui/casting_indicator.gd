extends Control
class_name CastingIndicator

## Circular cooldown indicator for casting progress
## Shows a filling circle with card icon/name during summon time

const INDICATOR_SIZE: float = 80.0
const RING_WIDTH: float = 6.0
const BG_COLOR: Color = Color(0.1, 0.1, 0.15, 0.9)
const RING_BG_COLOR: Color = Color(0.2, 0.2, 0.25, 0.8)
const FILL_COLOR: Color = Color(0.3, 0.6, 1.0, 1.0)

var progress: float = 0.0
var card_name: String = ""
var is_active: bool = false

func _ready() -> void:
	custom_minimum_size = Vector2(INDICATOR_SIZE, INDICATOR_SIZE + 20)  # Extra for label
	size = custom_minimum_size
	visible = false

func _draw() -> void:
	if not is_active:
		return

	var center: Vector2 = Vector2(INDICATOR_SIZE / 2.0, INDICATOR_SIZE / 2.0)
	var radius: float = (INDICATOR_SIZE - RING_WIDTH) / 2.0 - 4.0

	# Background circle
	draw_circle(center, radius + RING_WIDTH / 2.0, BG_COLOR)

	# Ring background
	_draw_arc_ring(center, radius, 0.0, TAU, RING_BG_COLOR, RING_WIDTH)

	# Progress fill (clockwise from top)
	if progress > 0.0:
		var end_angle: float = -PI / 2.0 + TAU * progress
		_draw_arc_ring(center, radius, -PI / 2.0, end_angle, FILL_COLOR, RING_WIDTH)

	# Inner circle (darker)
	draw_circle(center, radius - RING_WIDTH / 2.0 - 2.0, Color(0.05, 0.05, 0.1, 0.95))

	# Card name below
	if not card_name.is_empty():
		var font: Font = ThemeDB.fallback_font
		var font_size: int = 12
		var text_size: Vector2 = font.get_string_size(card_name, HORIZONTAL_ALIGNMENT_CENTER, -1, font_size)
		var text_pos: Vector2 = Vector2(
			(INDICATOR_SIZE - text_size.x) / 2.0,
			INDICATOR_SIZE + 14
		)
		draw_string(font, text_pos, card_name, HORIZONTAL_ALIGNMENT_LEFT, -1, font_size, Color.WHITE)

## Draw an arc ring (thick arc)
func _draw_arc_ring(center: Vector2, radius: float, start_angle: float, end_angle: float, color: Color, width: float) -> void:
	var points: int = 32
	var angle_range: float = end_angle - start_angle

	if absf(angle_range) < 0.01:
		return

	var point_count: int = maxi(int(absf(angle_range) / TAU * points), 2)
	var arc_points: PackedVector2Array = PackedVector2Array()

	for i: int in range(point_count + 1):
		var angle: float = start_angle + angle_range * (float(i) / float(point_count))
		arc_points.append(center + Vector2(cos(angle), sin(angle)) * radius)

	if arc_points.size() >= 2:
		draw_polyline(arc_points, color, width, true)

## Start showing the casting indicator
func start_casting(card: Card, duration: float) -> void:
	card_name = card.card_name
	progress = 0.0
	is_active = true
	visible = true
	queue_redraw()

## Update casting progress (0 to 1)
func update_progress(remaining: float, total: float) -> void:
	if total > 0.0:
		progress = 1.0 - (remaining / total)
	else:
		progress = 1.0
	queue_redraw()

## Stop and hide the indicator
func stop_casting() -> void:
	is_active = false
	visible = false
	progress = 0.0
	card_name = ""
	queue_redraw()
