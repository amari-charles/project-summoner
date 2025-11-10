extends Camera3D
class_name CameraController3D

## Camera controller for pannable battlefield view with boundary constraints

@export_group("Pan Settings")
@export var pan_speed: float = 20.0
@export var keyboard_pan_enabled: bool = true
@export var mouse_pan_enabled: bool = true
@export var touch_pan_enabled: bool = true

@export_group("Boundaries")
@export var ground_size: Vector2 = Vector2(254, 143)  # Ground plane dimensions
@export var auto_calculate_bounds: bool = true

# Calculated bounds
var min_position: Vector3
var max_position: Vector3

# Pan state
var is_panning: bool = false
var last_mouse_position: Vector2

func _ready() -> void:
	if auto_calculate_bounds:
		_calculate_bounds()

func _calculate_bounds() -> void:
	# Calculate view dimensions at ground level (Y=0)
	var viewport_size = get_viewport().get_visible_rect().size
	var aspect_ratio = viewport_size.x / viewport_size.y

	# Orthographic view dimensions
	var view_height = size * 2.0
	var view_width = view_height * aspect_ratio

	# Camera is angled 35° down, calculate ground coverage
	# The view rectangle is perpendicular to camera direction
	var camera_angle_rad = deg_to_rad(35.0)

	# View half-extents at ground level
	var half_view_width_x = view_width / 2.0
	var half_view_depth_z = (view_height / 2.0) / sin(deg_to_rad(55.0))  # Complementary angle

	# Ground half-extents
	var half_ground_width = ground_size.x / 2.0
	var half_ground_depth = ground_size.y / 2.0

	# Camera position bounds (ensure view never goes outside ground)
	min_position = Vector3(
		-half_ground_width + half_view_width_x,
		position.y,  # Keep Y fixed
		-half_ground_depth + half_view_depth_z
	)

	max_position = Vector3(
		half_ground_width - half_view_width_x,
		position.y,  # Keep Y fixed
		half_ground_depth - half_view_depth_z
	)

	# Clamp initial position
	position = position.clamp(min_position, max_position)

func _input(event: InputEvent) -> void:
	if mouse_pan_enabled:
		_handle_mouse_pan(event)

	if touch_pan_enabled:
		_handle_touch_pan(event)

func _handle_mouse_pan(event: InputEvent) -> void:
	# Middle mouse button or right mouse button drag
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_MIDDLE or event.button_index == MOUSE_BUTTON_RIGHT:
			if event.pressed:
				is_panning = true
				last_mouse_position = event.position
			else:
				is_panning = false

	elif event is InputEventMouseMotion and is_panning:
		var delta = event.position - last_mouse_position
		last_mouse_position = event.position

		# Convert screen delta to world delta
		# Invert Y because screen Y is down but world Y is up
		var pan_delta = Vector3(-delta.x, 0, delta.y) * pan_speed * 0.01

		# Apply pan
		position += pan_delta
		position = position.clamp(min_position, max_position)

func _handle_touch_pan(event: InputEvent) -> void:
	# Single finger drag for mobile
	if event is InputEventScreenTouch:
		if event.pressed:
			is_panning = true
			last_mouse_position = event.position
		else:
			is_panning = false

	elif event is InputEventScreenDrag and is_panning:
		var delta = event.relative

		# Convert screen delta to world delta
		var pan_delta = Vector3(-delta.x, 0, delta.y) * pan_speed * 0.01

		# Apply pan
		position += pan_delta
		position = position.clamp(min_position, max_position)

func _process(delta: float) -> void:
	if keyboard_pan_enabled:
		_handle_keyboard_pan(delta)

func _handle_keyboard_pan(delta: float) -> void:
	var pan_input = Vector3.ZERO

	# WASD or Arrow keys
	if Input.is_action_pressed("ui_right") or Input.is_key_pressed(KEY_D):
		pan_input.x += 1.0
	if Input.is_action_pressed("ui_left") or Input.is_key_pressed(KEY_A):
		pan_input.x -= 1.0
	if Input.is_action_pressed("ui_down") or Input.is_key_pressed(KEY_S):
		pan_input.z += 1.0
	if Input.is_action_pressed("ui_up") or Input.is_key_pressed(KEY_W):
		pan_input.z -= 1.0

	if pan_input != Vector3.ZERO:
		pan_input = pan_input.normalized()
		position += pan_input * pan_speed * delta
		position = position.clamp(min_position, max_position)

## Manually set camera bounds (if not using auto-calculate)
func set_bounds(min_pos: Vector3, max_pos: Vector3) -> void:
	min_position = min_pos
	max_position = max_pos
	position = position.clamp(min_position, max_position)
