extends Camera3D
class_name CameraController3D

## Camera controller for pannable battlefield view with boundary constraints
##
## This script allows the player to pan the camera across the battlefield while
## ensuring they can't see beyond the edges of the ground plane.

# === Constants ===

# Mouse/touch movement is measured in screen pixels, but we need to move the camera
# in "world units" (the 3D coordinate system). This scale factor converts between them.
# Lower values = slower panning, higher values = faster panning
const MOUSE_TO_WORLD_SCALE: float = 0.01
const TOUCH_TO_WORLD_SCALE: float = 0.01

# === Exports ===

@export_group("Pan Settings")
@export var pan_speed: float = 20.0
@export var keyboard_pan_enabled: bool = true
@export var mouse_pan_enabled: bool = true
@export var touch_pan_enabled: bool = true

@export_group("Map Boundaries")
## Axis-aligned world bounds on the ground plane (XZ)
@export var map_rect_xz: Rect2 = Rect2(Vector2(-50, -40), Vector2(100, 80))
## Ground plane Y value (most 2.5D maps use y=0)
@export var ground_y: float = 0.0
## If view is larger than map, center camera (true) or just clamp edges (false)
@export var center_if_too_big: bool = false

@export_group("Zoom Controls")
## Default orthographic size (camera starts at this zoom level)
@export var default_ortho_size: float = 40.0
@export var min_ortho_size: float = 20.0  # Max zoom in
@export var max_ortho_size: float = 50.0  # Max zoom out (limited to prevent showing outside map)
@export var zoom_speed: float = 2.0
@export var zoom_enabled: bool = true

@export_group("Zoom-Based Panning")
## If true, vertical panning is only enabled when zoomed in (size < default_ortho_size)
@export var vertical_pan_only_when_zoomed: bool = true

@export_group("Edge Panning")
## If true, camera pans when mouse is near screen edges
@export var edge_pan_enabled: bool = true
## Distance from edge (in pixels) where panning starts
@export var edge_pan_margin: float = 20.0
## Speed multiplier for edge panning
@export var edge_pan_speed: float = 1.0

# === State Variables ===

# Pan state for mouse/touch input
var is_panning: bool = false
var last_mouse_position: Vector2

func _ready() -> void:
	# Ensure orthographic projection for true 2.5D
	projection = PROJECTION_ORTHOGONAL
	# Wait one frame for transform initialization, then clamp
	await get_tree().process_frame
	clamp_to_map()

	# Calculate max zoom dynamically to prevent view from exceeding map
	var viewport_size: Vector2i = get_viewport().get_visible_rect().size
	var aspect_ratio: float = float(viewport_size.x) / float(viewport_size.y)

	# Map dimensions from map_rect_xz
	var map_width: float = map_rect_xz.size.x   # 100
	var map_height: float = map_rect_xz.size.y  # 80

	# Calculate max size where view doesn't exceed map
	# For orthographic: view_width = size * aspect_ratio, view_height = size * 2
	var max_size_for_width: float = map_width / aspect_ratio
	var max_size_for_height: float = map_height / 2.0

	# Use the smaller (more restrictive) value with 99% buffer (allows full view, prevents edge cases)
	max_ortho_size = min(max_size_for_width, max_size_for_height) * 0.99

func clamp_to_map() -> void:
	## Clamps camera to keep ground footprint (projection) within map bounds
	##
	## Uses corner ray-casting to calculate what the camera sees on the ground,
	## then moves camera to keep that footprint inside map_rect_xz bounds.
	## For orthographic cameras, moving camera in XZ translates footprint 1:1.

	# Get viewport size (handles SubViewport correctly)
	var vp := get_viewport()
	var view_size: Vector2i = vp.get_visible_rect().size
	var w: float = float(view_size.x)
	var h: float = float(view_size.y)

	# Define 4 screen corners
	var screen_corners := [
		Vector2(0.0, 0.0),       # Top-left
		Vector2(w, 0.0),         # Top-right
		Vector2(w, h),           # Bottom-right
		Vector2(0.0, h)          # Bottom-left
	]

	# Project each corner to ground plane (y = ground_y)
	var world_points: Array[Vector3] = []
	for corner in screen_corners:
		var origin: Vector3 = project_ray_origin(corner)
		var dir: Vector3 = project_ray_normal(corner)

		# Skip if ray is parallel to ground (shouldn't happen with tilted camera)
		if abs(dir.y) < 0.0001:
			continue

		# Calculate intersection with ground plane: t = (ground_y - origin.y) / dir.y
		var t: float = (ground_y - origin.y) / dir.y

		# Only consider intersections in front of camera
		if t >= 0.0:
			var point: Vector3 = origin + dir * t
			world_points.append(point)

	# Need at least 2 valid intersections to proceed
	if world_points.size() < 2:
		return

	# Calculate footprint extents on ground (XZ plane)
	var view_min_x: float = world_points[0].x
	var view_max_x: float = view_min_x
	var view_min_z: float = world_points[0].z
	var view_max_z: float = view_min_z

	for p in world_points:
		view_min_x = min(view_min_x, p.x)
		view_max_x = max(view_max_x, p.x)
		view_min_z = min(view_min_z, p.z)
		view_max_z = max(view_max_z, p.z)

	# Map bounds
	var map_min_x: float = map_rect_xz.position.x
	var map_min_z: float = map_rect_xz.position.y
	var map_max_x: float = map_min_x + map_rect_xz.size.x
	var map_max_z: float = map_min_z + map_rect_xz.size.y

	# Calculate view and map dimensions
	var view_width: float = view_max_x - view_min_x
	var view_height: float = view_max_z - view_min_z
	var map_width: float = map_max_x - map_min_x
	var map_height: float = map_max_z - map_min_z

	# If view is larger than map, center camera and return
	if center_if_too_big and (view_width >= map_width or view_height >= map_height):
		position.x = (map_min_x + map_max_x) * 0.5
		position.z = (map_min_z + map_max_z) * 0.5
		return

	# Calculate translation needed to bring footprint inside bounds
	var dx: float = 0.0
	var dz: float = 0.0

	# X-axis clamping
	if view_min_x < map_min_x:
		dx = map_min_x - view_min_x  # Need to move right
	elif view_max_x > map_max_x:
		dx = map_max_x - view_max_x  # Need to move left (negative)

	# Z-axis clamping
	if view_min_z < map_min_z:
		dz = map_min_z - view_min_z  # Need to move forward
	elif view_max_z > map_max_z:
		dz = map_max_z - view_max_z  # Need to move back (negative)

	# STABILITY: Only apply correction if offset is significant (prevents micro-jitter)
	if abs(dx) < 0.1 and abs(dz) < 0.1:
		return

	# Move camera to bring footprint inside bounds
	# For orthographic cameras, moving camera XZ translates footprint 1:1
	position.x += dx
	position.z += dz

func _input(event: InputEvent) -> void:
	## Handle mouse and touch input for panning and zooming
	if zoom_enabled:
		_handle_zoom(event)

	if mouse_pan_enabled:
		_handle_mouse_pan(event)

	if touch_pan_enabled:
		_handle_touch_pan(event)

func _handle_zoom(event: InputEvent) -> void:
	## Handle mouse scroll wheel zoom and trackpad gestures
	if event is InputEventMouseButton:
		if event.pressed:
			if event.button_index == MOUSE_BUTTON_WHEEL_UP:
				# Zoom in (decrease ortho size)
				_apply_zoom(-zoom_speed)
			elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
				# Zoom out (increase ortho size)
				_apply_zoom(zoom_speed)

	# Support macOS/Linux trackpad pinch-to-zoom gesture
	elif event is InputEventMagnifyGesture:
		# factor > 1.0 means pinch out (zoom out), < 1.0 means pinch in (zoom in)
		# We invert this to make pinch-in zoom in (decrease size)
		var zoom_delta = (1.0 - event.factor) * zoom_speed * 10.0
		_apply_zoom(zoom_delta)

	# Support macOS/Linux trackpad two-finger scroll for zoom
	elif event is InputEventPanGesture:
		# delta.y > 0 means scroll down, < 0 means scroll up
		# Scroll up = zoom in (decrease size), scroll down = zoom out (increase size)
		var zoom_delta = event.delta.y * zoom_speed * 0.2
		_apply_zoom(zoom_delta)

func _apply_zoom(delta: float) -> void:
	## Apply zoom change and re-clamp camera
	size = clamp(size + delta, min_ortho_size, max_ortho_size)
	# Clamp after zoom to adjust for new view size
	clamp_to_map()

func _handle_mouse_pan(event: InputEvent) -> void:
	## Pan the camera by dragging with middle or right mouse button

	# Check if middle or right mouse button was pressed/released
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_MIDDLE or event.button_index == MOUSE_BUTTON_RIGHT:
			if event.pressed:
				is_panning = true
				last_mouse_position = event.position
			else:
				is_panning = false

	# Check if mouse moved while panning
	elif event is InputEventMouseMotion and is_panning:
		var delta = event.position - last_mouse_position
		last_mouse_position = event.position
		_apply_pan_delta(delta)

func _handle_touch_pan(event: InputEvent) -> void:
	## Pan the camera by dragging with one finger on mobile

	if event is InputEventScreenTouch:
		if event.pressed:
			is_panning = true
			last_mouse_position = event.position
		else:
			is_panning = false

	elif event is InputEventScreenDrag and is_panning:
		# For touch, we use 'relative' which gives us the movement delta directly
		_apply_pan_delta(event.relative)

func _apply_pan_delta(delta: Vector2) -> void:
	## Apply a pan movement delta (in screen pixels) to the camera position
	##
	## delta: The movement in screen space (pixels)
	##
	## The camera moves in the ground plane (X and Z axes), not up/down (Y axis)
	## We invert X so dragging left moves the view left (intuitive drag behavior)

	# Horizontal panning (X-axis) is always allowed
	position.x += -delta.x * pan_speed * MOUSE_TO_WORLD_SCALE

	# Vertical panning (Z-axis) only allowed when zoomed in
	if not vertical_pan_only_when_zoomed or size < default_ortho_size:
		position.z += delta.y * pan_speed * TOUCH_TO_WORLD_SCALE

	# Clamp after panning to keep view inside map
	clamp_to_map()

func _process(delta: float) -> void:
	## Called every frame. Handle keyboard and edge panning here.
	## delta: Time elapsed since last frame (usually ~0.016 for 60 FPS)
	if keyboard_pan_enabled:
		_handle_keyboard_pan(delta)

	if edge_pan_enabled:
		_handle_edge_pan(delta)

func _handle_keyboard_pan(delta: float) -> void:
	## Pan the camera using WASD or arrow keys

	var pan_input = Vector2.ZERO

	# Collect horizontal input (always allowed)
	if Input.is_action_pressed("ui_right") or Input.is_key_pressed(KEY_D):
		pan_input.x += 1.0
	if Input.is_action_pressed("ui_left") or Input.is_key_pressed(KEY_A):
		pan_input.x -= 1.0

	# Collect vertical input (only if zoomed in or restriction disabled)
	if not vertical_pan_only_when_zoomed or size < default_ortho_size:
		if Input.is_action_pressed("ui_down") or Input.is_key_pressed(KEY_S):
			pan_input.y -= 1.0  # Down = move toward negative Z (closer to camera)
		if Input.is_action_pressed("ui_up") or Input.is_key_pressed(KEY_W):
			pan_input.y += 1.0  # Up = move toward positive Z (away from camera)

	if pan_input != Vector2.ZERO:
		# Normalize ensures diagonal movement isn't faster than straight movement
		# Without this, pressing W+D would move at 1.414x speed
		pan_input = pan_input.normalized()

		# Keyboard panning uses delta (time-based) for smooth, framerate-independent movement
		# Mouse/touch use direct pixel deltas instead
		position.x += pan_input.x * pan_speed * delta
		position.z += pan_input.y * pan_speed * delta
		# Clamp after panning
		clamp_to_map()

func _handle_edge_pan(delta: float) -> void:
	## Pan the camera when mouse is near screen edges (RTS-style)

	var viewport_size = get_viewport().get_visible_rect().size
	var mouse_pos = get_viewport().get_mouse_position()

	var pan_input = Vector2.ZERO

	# Check horizontal edges (always allowed)
	if mouse_pos.x <= edge_pan_margin:
		# Near left edge, pan left
		pan_input.x = -1.0
	elif mouse_pos.x >= viewport_size.x - edge_pan_margin:
		# Near right edge, pan right
		pan_input.x = 1.0

	# Check vertical edges (only if zoomed in or restriction disabled)
	if not vertical_pan_only_when_zoomed or size < default_ortho_size:
		if mouse_pos.y <= edge_pan_margin:
			# Near top edge, pan up (away from camera)
			pan_input.y = 1.0
		elif mouse_pos.y >= viewport_size.y - edge_pan_margin:
			# Near bottom edge, pan down (toward camera)
			pan_input.y = -1.0

	if pan_input != Vector2.ZERO:
		# Apply edge panning (no need to normalize, edges are mutually exclusive)
		position.x += pan_input.x * pan_speed * edge_pan_speed * delta
		position.z += pan_input.y * pan_speed * edge_pan_speed * delta
		# Clamp after panning
		clamp_to_map()
