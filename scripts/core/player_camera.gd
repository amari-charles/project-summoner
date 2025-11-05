extends Camera2D
class_name PlayerCamera

## Player-controlled camera with drag panning and edge scrolling

@export var pan_speed: float = 400.0  # Speed for edge panning
@export var edge_pan_margin: float = 50.0  # Pixels from edge to trigger panning
@export var drag_enabled: bool = true

var is_dragging: bool = false
var drag_start_pos: Vector2
var camera_start_pos: Vector2

# Battlefield bounds (prevents camera from going off-field)
var min_x: float = 400.0
var max_x: float = 1520.0
var min_y: float = 540.0
var max_y: float = 540.0

func _ready() -> void:
	enabled = true

func _input(event: InputEvent) -> void:
	# Right-click drag to pan camera
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_RIGHT:
			if event.pressed:
				is_dragging = true
				drag_start_pos = event.position
				camera_start_pos = position
			else:
				is_dragging = false

	# Mouse motion while dragging
	if event is InputEventMouseMotion and is_dragging:
		var delta = drag_start_pos - event.position
		position = camera_start_pos + delta / zoom
		_clamp_position()

func _process(delta: float) -> void:
	# Edge panning - move camera when mouse near screen edges
	var mouse_pos = get_viewport().get_mouse_position()
	var viewport_size = get_viewport().get_visible_rect().size

	var pan_vec = Vector2.ZERO

	# Horizontal edge panning
	if mouse_pos.x < edge_pan_margin:
		pan_vec.x = -1
	elif mouse_pos.x > viewport_size.x - edge_pan_margin:
		pan_vec.x = 1

	# Apply edge panning
	if pan_vec.length() > 0:
		position += pan_vec * pan_speed * delta / zoom.x
		_clamp_position()

## Keep camera within battlefield bounds
func _clamp_position() -> void:
	position.x = clamp(position.x, min_x, max_x)
	position.y = clamp(position.y, min_y, max_y)
