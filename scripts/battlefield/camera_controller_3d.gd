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

@export_group("Boundaries")
## If true, automatically calculate camera bounds based on ground size
## If false, you must call set_bounds() manually
@export var auto_calculate_bounds: bool = true

@export_group("Zoom Controls")
## Default orthographic size (camera starts at this zoom level)
@export var default_ortho_size: float = 40.0
@export var min_ortho_size: float = 20.0  # Max zoom in
@export var max_ortho_size: float = 60.0  # Max zoom out
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

# The min/max positions the camera is allowed to move to (calculated based on ground size)
var min_position: Vector3
var max_position: Vector3

# Pan state for mouse/touch input
var is_panning: bool = false
var last_mouse_position: Vector2

func _ready() -> void:
	if auto_calculate_bounds:
		# Wait one frame to ensure the camera's transform is fully initialized by the scene
		# Without this, the camera might not be in its correct starting position yet
		await get_tree().process_frame
		_calculate_bounds()

func _calculate_bounds() -> void:
	## Calculates the min/max camera positions that keep the ground fully visible
	##
	## This is complex because the camera is tilted at an angle. We need to figure out
	## where the edges of the camera's view intersect with the ground plane.

	# Get ground size from parent's Background mesh
	var ground_mesh: MeshInstance3D = get_parent().get_node_or_null("Background")
	if not ground_mesh or not ground_mesh.mesh:
		push_error("CameraController: Could not find Background mesh to calculate bounds")
		return

	var ground_size: Vector2 = ground_mesh.mesh.size

	# The "viewport" is the game window. We need its dimensions to calculate the aspect ratio
	# Aspect ratio = width/height (e.g., 1920/1080 = 1.78 for widescreen)
	var viewport_size = get_viewport().get_visible_rect().size
	var aspect_ratio = viewport_size.x / viewport_size.y

	# Orthographic cameras don't have perspective distortion (objects don't get smaller with distance)
	# The "size" property defines half the view height in world units
	# So view_height = size * 2 (full height from top to bottom of view)
	var view_height = size * 2.0

	# For KEEP_HEIGHT mode, width is calculated by multiplying size by aspect ratio
	# This ensures the view scales correctly for different screen sizes
	var view_width = size * aspect_ratio

	# Half-widths are useful for calculating offsets from the center
	var half_view_width = view_width / 2.0

	# === Z-axis bounds calculation ===
	# This is where it gets tricky: because the camera is tilted, we need to project
	# the top and bottom edges of the view down to the ground plane (Y=0) to see
	# where they intersect

	# "Basis vectors" define the camera's orientation in 3D space
	# - forward: the direction the camera is looking
	# - up: the direction that's "up" for the camera (not world up, but camera up)
	var forward = -transform.basis.z  # Camera's viewing direction
	var up = transform.basis.y  # Camera's up direction

	# Check if camera is too horizontal (would cause division by zero)
	if abs(forward.y) < 0.001:
		push_error("CameraController: Camera is too horizontal for ground-based bounds calculation")
		# Set safe fallback bounds
		min_position = Vector3(-50, position.y, -50)
		max_position = Vector3(50, position.y, 50)
		return

	# Top edge of the camera's view in world space
	var top_edge_world = position + up * size

	# We need to find where a ray from top_edge_world in the forward direction hits Y=0
	# This is called "ray-ground intersection" - we're casting a ray and finding where it hits
	# The distance along the ray to reach Y=0 is: current_y / downward_component_of_direction
	var ray_distance_top = top_edge_world.y / (-forward.y)

	# Now we know the distance, we can find the Z coordinate where the ray hits ground
	var top_ground_z = top_edge_world.z + ray_distance_top * forward.z

	# Do the same for the bottom edge of the view
	var bottom_edge_world = position - up * size
	var ray_distance_bottom = bottom_edge_world.y / (-forward.y)
	var bottom_ground_z = bottom_edge_world.z + ray_distance_bottom * forward.z

	# Special case: if the bottom edge is below ground level (Y < 0),
	# we need to find where the Y=0 plane intersects the viewport edge instead
	if bottom_edge_world.y < 0:
		# Linear interpolation: find the ratio where Y=0 falls between bottom and top
		var y_ratio = (0 - bottom_edge_world.y) / (top_edge_world.y - bottom_edge_world.y)
		# Use that ratio to interpolate the Z coordinate
		bottom_ground_z = bottom_edge_world.z + y_ratio * (top_edge_world.z - bottom_edge_world.z)

	# Calculate how far the ground intersection points are from the camera's Z position
	# These offsets tell us how the view edges map to ground positions
	var top_z_offset = top_ground_z - position.z
	var bottom_z_offset = bottom_ground_z - position.z

	# Ground extents: half the size in each direction from center (0,0)
	var half_ground_width = ground_size.x / 2.0
	var half_ground_depth = ground_size.y / 2.0

	# === Calculate bounds ===
	# We want the camera positioned so that the view edges align with the ground edges

	# X-axis bounds (left-right):
	# Camera can move from the left edge to the right edge, minus the view width
	# Example: if ground is 200 wide and view is 80 wide, camera can move from -60 to +60
	min_position = Vector3(
		-half_ground_width + half_view_width,  # Leftmost position
		position.y,  # Keep Y fixed (don't move up/down)
		0  # Z calculated below
	)

	max_position = Vector3(
		half_ground_width - half_view_width,  # Rightmost position
		position.y,  # Keep Y fixed
		0  # Z calculated below
	)

	# Z-axis bounds (forward-back depth):
	# When camera is at max Z, the top view edge should show the far ground edge
	max_position.z = half_ground_depth - top_z_offset

	# When camera is at min Z, the bottom view edge should show the near ground edge
	min_position.z = -half_ground_depth - bottom_z_offset

	# Validate that we got sane bounds
	if min_position.x >= max_position.x or min_position.z >= max_position.z:
		push_error("CameraController: Calculated invalid bounds (min >= max)")
		# Use fallback bounds
		min_position = Vector3(-50, position.y, -50)
		max_position = Vector3(50, position.y, 50)
		return

	# Debug logging to help diagnose bounds issues
	print("CameraController: Bounds calculated")
	print("  Ground size: ", ground_size)
	print("  Camera ortho size: ", size)
	print("  View dimensions: ", view_width, " x ", view_height)
	print("  Bounds X: [", min_position.x, " to ", max_position.x, "]")
	print("  Bounds Z: [", min_position.z, " to ", max_position.z, "]")
	print("  Camera position: ", position)

	# "Clamp" means restrict a value to a range. Here we ensure the camera starts within bounds
	# If the camera was positioned outside the bounds, this moves it to the nearest valid position
	position = position.clamp(min_position, max_position)

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
	## Apply zoom change and recalculate bounds if needed
	var old_size = size
	size = clamp(size + delta, min_ortho_size, max_ortho_size)

	# If zoom actually changed, recalculate bounds
	if size != old_size and auto_calculate_bounds:
		_calculate_bounds()

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

	# Clamp keeps the camera position within the allowed bounds
	# Without this, you could pan beyond the edge of the battlefield
	position = position.clamp(min_position, max_position)

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
		position = position.clamp(min_position, max_position)

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
		position = position.clamp(min_position, max_position)

## Manually set camera bounds (alternative to auto-calculation)
##
## Use this if you want to set custom bounds rather than auto-calculating from ground size
func set_bounds(min_pos: Vector3, max_pos: Vector3) -> void:
	min_position = min_pos
	max_position = max_pos
	position = position.clamp(min_position, max_position)
