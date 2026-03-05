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
const CLAMP_EPSILON: float = 0.001
const MAX_ZOOM_SOLVER_ITERATIONS: int = 12
const MAX_CLAMP_ITERATIONS: int = 8

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
@export var center_if_too_big: bool = true

@export_group("Projection")
@export var default_fov: float = 38.0
@export var min_fov: float = 24.0  # Max zoom in
@export var max_fov: float = 62.0  # Max zoom out (limited to prevent showing outside map)
@export var perspective_near_clip: float = 0.5
@export var perspective_far_clip: float = 260.0

@export_group("Zoom Controls")
@export var zoom_speed: float = 2.0
@export var zoom_enabled: bool = true

@export_group("Zoom-Based Panning")
## If true, vertical panning is only enabled when zoomed in (fov < default_fov)
@export var vertical_pan_only_when_zoomed: bool = true

@export_group("Edge Panning")
## If true, camera pans when mouse is near screen edges
@export var edge_pan_enabled: bool = true
## Distance from edge (in pixels) where panning starts
@export var edge_pan_margin: float = 20.0
## Speed multiplier for edge panning
@export var edge_pan_speed: float = 1.0

@export_group("Debug")
## Temporary visualization: draw map bounds + current camera footprint on ground.
@export var debug_show_pan_bounds_overlay: bool = false
@export var debug_overlay_y_offset: float = 0.35
## World-space border thickness for debug rectangles.
@export var debug_overlay_line_thickness: float = 0.5
## Optional camera footprint rectangle (red). Off by default to avoid confusion.
@export var debug_show_camera_footprint_overlay: bool = false

# === State Variables ===

# Pan state for mouse/touch input
var is_panning: bool = false
var last_mouse_position: Vector2
var _max_fov_ceiling: float = -1.0
var _debug_overlay_mesh: MeshInstance3D
var _debug_overlay_lines: ImmediateMesh
var _debug_overlay_material: StandardMaterial3D

func _ready() -> void:
	# Set process mode to ALWAYS so camera panning works during dialogues
	process_mode = Node.PROCESS_MODE_ALWAYS

	# Perspective projection gives true depth/parallax across the battlefield.
	projection = PROJECTION_PERSPECTIVE
	near = perspective_near_clip
	far = perspective_far_clip
	# Wait one frame for transform initialization
	await get_tree().process_frame
	if _max_fov_ceiling < 0.0:
		_max_fov_ceiling = max_fov
	_refresh_zoom_limits()
	fov = clamp(default_fov, min_fov, max_fov)
	clamp_to_map()

	var vp: Viewport = get_viewport()
	if vp and not vp.size_changed.is_connected(_on_viewport_size_changed):
		vp.size_changed.connect(_on_viewport_size_changed)

	if _is_debug_overlay_enabled():
		_ensure_debug_overlay()
		_update_debug_overlay()

func _on_viewport_size_changed() -> void:
	_refresh_zoom_limits()
	fov = clamp(fov, min_fov, max_fov)
	clamp_to_map()

func _refresh_zoom_limits() -> void:
	var configured_max: float = _max_fov_ceiling if _max_fov_ceiling > 0.0 else max_fov
	var solved_max: float = _solve_max_fov(configured_max)
	max_fov = max(min_fov, min(configured_max, solved_max))

func set_map_bounds(bounds_xz: Rect2) -> void:
	if bounds_xz.size.x <= 0.0 or bounds_xz.size.y <= 0.0:
		return

	map_rect_xz = bounds_xz
	if _max_fov_ceiling < 0.0:
		_max_fov_ceiling = max_fov
	_refresh_zoom_limits()
	fov = clamp(fov, min_fov, max_fov)
	clamp_to_map()

	if _is_debug_overlay_enabled():
		_ensure_debug_overlay()
		_update_debug_overlay()

func _solve_max_fov(configured_max: float) -> float:
	var original_fov: float = fov
	var low: float = min_fov
	var high: float = max(configured_max, min_fov)

	for i: int in range(MAX_ZOOM_SOLVER_ITERATIONS):
		var mid: float = (low + high) * 0.5
		fov = mid
		var footprint: Rect2 = get_ground_footprint_xz()
		if _footprint_fits_map(footprint):
			low = mid
		else:
			high = mid

	fov = original_fov
	return low

func _footprint_fits_map(footprint: Rect2) -> bool:
	if footprint.size == Vector2.ZERO:
		return false

	return footprint.size.x <= map_rect_xz.size.x + CLAMP_EPSILON \
		and footprint.size.y <= map_rect_xz.size.y + CLAMP_EPSILON

func get_ground_footprint_xz() -> Rect2:
	## Calculate the current ground-plane footprint of the camera view in XZ-space.
	## Rect2.position = (min_x, min_z), Rect2.size = (width_x, depth_z).
	##
	## Exposed for regression tests and zoom-limit solving.

	# Get viewport size (handles SubViewport correctly)
	var vp: Viewport = get_viewport()
	var view_size: Vector2i = vp.get_visible_rect().size
	var w: float = float(view_size.x)
	var h: float = float(view_size.y)

	# Define 4 screen corners
	var screen_corners: Array[Vector2] = [
		Vector2(0.0, 0.0),       # Top-left
		Vector2(w, 0.0),         # Top-right
		Vector2(w, h),           # Bottom-right
		Vector2(0.0, h)          # Bottom-left
	]

	# Project each corner to ground plane (y = ground_y)
	var world_points: Array[Vector3] = []
	for corner: Vector2 in screen_corners:
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

	# We need all 4 corners to intersect ground; otherwise part of the frustum
	# points above the horizon and map clamping math becomes invalid.
	if world_points.size() != 4:
		return Rect2()

	# Calculate footprint extents on ground (XZ plane)
	var view_min_x: float = world_points[0].x
	var view_max_x: float = view_min_x
	var view_min_z: float = world_points[0].z
	var view_max_z: float = view_min_z

	for p: Vector3 in world_points:
		view_min_x = min(view_min_x, p.x)
		view_max_x = max(view_max_x, p.x)
		view_min_z = min(view_min_z, p.z)
		view_max_z = max(view_max_z, p.z)

	return Rect2(
		Vector2(view_min_x, view_min_z),
		Vector2(view_max_x - view_min_x, view_max_z - view_min_z)
	)

func clamp_to_map() -> void:
	## Clamps camera to keep ground footprint (projection) within map bounds
	##
	## Uses corner ray-casting to calculate what the camera sees on the ground,
	## then moves camera to keep that footprint inside map_rect_xz bounds.
	for i: int in range(MAX_CLAMP_ITERATIONS):
		var footprint: Rect2 = get_ground_footprint_xz()
		if footprint.size == Vector2.ZERO:
			return

		var view_min_x: float = footprint.position.x
		var view_max_x: float = view_min_x + footprint.size.x
		var view_min_z: float = footprint.position.y
		var view_max_z: float = view_min_z + footprint.size.y
		var view_center_x: float = view_min_x + footprint.size.x * 0.5
		var view_center_z: float = view_min_z + footprint.size.y * 0.5

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

		# Calculate translation needed to bring footprint inside bounds
		var dx: float = 0.0
		var dz: float = 0.0
		var map_center_x: float = (map_min_x + map_max_x) * 0.5
		var map_center_z: float = (map_min_z + map_max_z) * 0.5

		# If the view footprint is larger than the map on an axis, pin center on that axis.
		if view_width >= map_width:
			if center_if_too_big:
				dx = map_center_x - view_center_x
			else:
				# Fallback mode: keep one side pinned when oversized.
				dx = map_min_x - view_min_x
		else:
			# X-axis clamping
			if view_min_x < map_min_x:
				dx = map_min_x - view_min_x  # Need to move right
			elif view_max_x > map_max_x:
				dx = map_max_x - view_max_x  # Need to move left (negative)

		if view_height >= map_height:
			if center_if_too_big:
				dz = map_center_z - view_center_z
			else:
				# Fallback mode: keep one side pinned when oversized.
				dz = map_min_z - view_min_z
		else:
			# Z-axis clamping
			if view_min_z < map_min_z:
				dz = map_min_z - view_min_z  # Need to move forward
			elif view_max_z > map_max_z:
				dz = map_max_z - view_max_z  # Need to move back (negative)

		# STABILITY: Only apply correction if offset is significant (prevents micro-jitter)
		if abs(dx) < CLAMP_EPSILON and abs(dz) < CLAMP_EPSILON:
			return

		# Perspective projection introduces slight coupling between position and
		# footprint scale; iterate to converge fully inside bounds.
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
		var mouse_event: InputEventMouseButton = event
		if mouse_event.pressed:
			if mouse_event.button_index == MOUSE_BUTTON_WHEEL_UP:
				# Zoom in (decrease FOV)
				_apply_zoom(-zoom_speed)
			elif mouse_event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
				# Zoom out (increase FOV)
				_apply_zoom(zoom_speed)

	# Support macOS/Linux trackpad pinch-to-zoom gesture
	elif event is InputEventMagnifyGesture:
		var magnify_event: InputEventMagnifyGesture = event
		# factor > 1.0 means pinch out (zoom out), < 1.0 means pinch in (zoom in)
		# We invert this to make pinch-in zoom in (decrease FOV)
		var zoom_delta: float = (1.0 - magnify_event.factor) * zoom_speed * 10.0
		_apply_zoom(zoom_delta)

	# Support macOS/Linux trackpad two-finger scroll for zoom
	elif event is InputEventPanGesture:
		var pan_gesture: InputEventPanGesture = event
		# delta.y > 0 means scroll down, < 0 means scroll up
		# Scroll up = zoom in (decrease FOV), scroll down = zoom out (increase FOV)
		var zoom_delta: float = pan_gesture.delta.y * zoom_speed * 0.2
		_apply_zoom(zoom_delta)

func _apply_zoom(delta: float) -> void:
	## Apply zoom change and re-clamp camera
	fov = clamp(fov + delta, min_fov, max_fov)
	# Clamp after zoom to adjust for new view size
	clamp_to_map()

func _handle_mouse_pan(event: InputEvent) -> void:
	## Pan the camera by dragging with middle or right mouse button

	# Check if middle or right mouse button was pressed/released
	if event is InputEventMouseButton:
		var mouse_button: InputEventMouseButton = event
		if mouse_button.button_index == MOUSE_BUTTON_MIDDLE or mouse_button.button_index == MOUSE_BUTTON_RIGHT:
			if mouse_button.pressed:
				is_panning = true
				last_mouse_position = mouse_button.position
			else:
				is_panning = false

	# Check if mouse moved while panning
	elif event is InputEventMouseMotion and is_panning:
		var mouse_motion: InputEventMouseMotion = event
		var button_mask: int = mouse_motion.button_mask
		var pan_mask: int = MOUSE_BUTTON_MASK_MIDDLE | MOUSE_BUTTON_MASK_RIGHT
		if (button_mask & pan_mask) == 0:
			# If release happened outside viewport, reset pan state on next motion.
			is_panning = false
			return
		last_mouse_position = mouse_motion.position
		_apply_pan_delta(mouse_motion.relative)

func _handle_touch_pan(event: InputEvent) -> void:
	## Pan the camera by dragging with one finger on mobile

	if event is InputEventScreenTouch:
		var touch_event: InputEventScreenTouch = event
		if touch_event.pressed:
			is_panning = true
			last_mouse_position = touch_event.position
		else:
			is_panning = false

	elif event is InputEventScreenDrag and is_panning:
		var drag_event: InputEventScreenDrag = event
		# For touch, we use 'relative' which gives us the movement delta directly
		_apply_pan_delta(drag_event.relative)

func _apply_pan_delta(delta: Vector2) -> void:
	## Apply a pan movement delta (in screen pixels) to the camera position
	##
	## delta: The movement in screen space (pixels)
	##
	## The camera moves in the ground plane (X and Z axes), not up/down (Y axis)
	## We invert X so dragging left moves the view left (intuitive drag behavior)

	# Horizontal panning (X-axis) is always allowed
	var desired_dx: float = -delta.x * pan_speed * MOUSE_TO_WORLD_SCALE

	# Vertical panning (Z-axis) only allowed when zoomed in
	var desired_dz: float = 0.0
	if not vertical_pan_only_when_zoomed or fov < default_fov:
		desired_dz = delta.y * pan_speed * TOUCH_TO_WORLD_SCALE

	# Constrain movement before applying, so drag panning "stops at edge"
	# rather than overshooting and snapping back every frame.
	var constrained_pan: Vector2 = _constrain_pan_motion_to_map(desired_dx, desired_dz)
	if constrained_pan == Vector2.ZERO:
		return
	position.x += constrained_pan.x
	position.z += constrained_pan.y
	clamp_to_map()

func _constrain_pan_motion_to_map(desired_dx: float, desired_dz: float) -> Vector2:
	var footprint: Rect2 = get_ground_footprint_xz()
	if footprint.size == Vector2.ZERO:
		# If projection is invalid, avoid blind movement that can cause snap-back.
		return Vector2.ZERO

	var view_min_x: float = footprint.position.x
	var view_max_x: float = view_min_x + footprint.size.x
	var view_min_z: float = footprint.position.y
	var view_max_z: float = view_min_z + footprint.size.y
	var map_min_x: float = map_rect_xz.position.x
	var map_max_x: float = map_min_x + map_rect_xz.size.x
	var map_min_z: float = map_rect_xz.position.y
	var map_max_z: float = map_min_z + map_rect_xz.size.y

	var min_dx: float
	var max_dx: float
	var min_dz: float
	var max_dz: float

	var view_width: float = footprint.size.x
	var view_height: float = footprint.size.y
	var map_width: float = map_rect_xz.size.x
	var map_height: float = map_rect_xz.size.y

	if view_width >= map_width:
		# When view is wider than map, stay centered on X.
		min_dx = 0.0
		max_dx = 0.0
	else:
		min_dx = map_min_x - view_min_x
		max_dx = map_max_x - view_max_x

	if view_height >= map_height:
		# When view is deeper than map, stay centered on Z.
		min_dz = 0.0
		max_dz = 0.0
	else:
		min_dz = map_min_z - view_min_z
		max_dz = map_max_z - view_max_z

	var clamped_dx: float = clamp(desired_dx, min_dx, max_dx)
	var clamped_dz: float = clamp(desired_dz, min_dz, max_dz)

	# Snap sub-epsilon drift to zero to avoid visible edge jitter.
	if abs(clamped_dx) < CLAMP_EPSILON:
		clamped_dx = 0.0
	if abs(clamped_dz) < CLAMP_EPSILON:
		clamped_dz = 0.0

	return Vector2(clamped_dx, clamped_dz)

func _process(delta: float) -> void:
	## Called every frame. Handle keyboard and edge panning here.
	## delta: Time elapsed since last frame (usually ~0.016 for 60 FPS)
	if keyboard_pan_enabled:
		_handle_keyboard_pan(delta)

	if edge_pan_enabled:
		_handle_edge_pan(delta)

	if _is_debug_overlay_enabled():
		_ensure_debug_overlay()
		_update_debug_overlay()
	elif _debug_overlay_mesh:
		_debug_overlay_mesh.visible = false

func _is_debug_overlay_enabled() -> bool:
	return OS.is_debug_build() and debug_show_pan_bounds_overlay

func _handle_keyboard_pan(delta: float) -> void:
	## Pan the camera using WASD or arrow keys

	var pan_input: Vector2 = Vector2.ZERO

	# Collect horizontal input (always allowed)
	if Input.is_action_pressed("ui_right") or Input.is_key_pressed(KEY_D):
		pan_input.x += 1.0
	if Input.is_action_pressed("ui_left") or Input.is_key_pressed(KEY_A):
		pan_input.x -= 1.0

	# Collect vertical input (only if zoomed in or restriction disabled)
	if not vertical_pan_only_when_zoomed or fov < default_fov:
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
		var desired_dx: float = pan_input.x * pan_speed * delta
		var desired_dz: float = pan_input.y * pan_speed * delta
		var constrained_pan: Vector2 = _constrain_pan_motion_to_map(desired_dx, desired_dz)
		if constrained_pan == Vector2.ZERO:
			return
		position.x += constrained_pan.x
		position.z += constrained_pan.y
		clamp_to_map()

func _handle_edge_pan(delta: float) -> void:
	## Pan the camera when mouse is near screen edges (RTS-style)
	if is_panning:
		# Don't mix RTS edge-pan with active drag panning.
		return

	var viewport_size: Vector2 = get_viewport().get_visible_rect().size
	var mouse_pos: Vector2 = get_viewport().get_mouse_position()

	var pan_input: Vector2 = Vector2.ZERO

	# Check horizontal edges (always allowed)
	if mouse_pos.x <= edge_pan_margin:
		# Near left edge, pan left
		pan_input.x = -1.0
	elif mouse_pos.x >= viewport_size.x - edge_pan_margin:
		# Near right edge, pan right
		pan_input.x = 1.0

	# Check vertical edges (only if zoomed in or restriction disabled)
	if not vertical_pan_only_when_zoomed or fov < default_fov:
		if mouse_pos.y <= edge_pan_margin:
			# Near top edge, pan up (away from camera)
			pan_input.y = 1.0
		elif mouse_pos.y >= viewport_size.y - edge_pan_margin:
			# Near bottom edge, pan down (toward camera)
			pan_input.y = -1.0

		if pan_input != Vector2.ZERO:
			# Apply edge panning (no need to normalize, edges are mutually exclusive)
			var desired_dx: float = pan_input.x * pan_speed * edge_pan_speed * delta
			var desired_dz: float = pan_input.y * pan_speed * edge_pan_speed * delta
			var constrained_pan: Vector2 = _constrain_pan_motion_to_map(desired_dx, desired_dz)
			if constrained_pan == Vector2.ZERO:
				return
			position.x += constrained_pan.x
			position.z += constrained_pan.y
			clamp_to_map()

func _ensure_debug_overlay() -> void:
	if _debug_overlay_mesh:
		_debug_overlay_mesh.visible = true
		return

	_debug_overlay_lines = ImmediateMesh.new()

	_debug_overlay_material = StandardMaterial3D.new()
	_debug_overlay_material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	_debug_overlay_material.vertex_color_use_as_albedo = true
	_debug_overlay_material.cull_mode = BaseMaterial3D.CULL_DISABLED
	_debug_overlay_material.no_depth_test = true
	_debug_overlay_material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA

	_debug_overlay_mesh = MeshInstance3D.new()
	_debug_overlay_mesh.name = "PanBoundsDebugOverlay"
	# Vertices are authored in world coordinates, so keep the debug mesh in
	# top-level mode with identity transform.
	_debug_overlay_mesh.top_level = true
	_debug_overlay_mesh.global_transform = Transform3D.IDENTITY
	_debug_overlay_mesh.mesh = _debug_overlay_lines
	_debug_overlay_mesh.material_override = _debug_overlay_material
	_debug_overlay_mesh.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	add_child(_debug_overlay_mesh)

func _update_debug_overlay() -> void:
	if not _debug_overlay_lines:
		return
	if _debug_overlay_mesh:
		_debug_overlay_mesh.global_transform = Transform3D.IDENTITY

	_debug_overlay_lines.clear_surfaces()
	_debug_overlay_lines.surface_begin(Mesh.PRIMITIVE_TRIANGLES)

	# Green = allowed map area.
	_draw_debug_rect_border(
		_debug_overlay_lines,
		map_rect_xz,
		Color(0.1, 1.0, 0.2, 0.9),
		ground_y + debug_overlay_y_offset,
		debug_overlay_line_thickness
	)

	# Red = current camera footprint on the ground (optional).
	if debug_show_camera_footprint_overlay:
		var footprint: Rect2 = get_ground_footprint_xz()
		if footprint.size != Vector2.ZERO:
			_draw_debug_rect_border(
				_debug_overlay_lines,
				footprint,
				Color(1.0, 0.2, 0.2, 0.9),
				ground_y + debug_overlay_y_offset + 0.02,
				debug_overlay_line_thickness
			)

	_debug_overlay_lines.surface_end()

func _draw_debug_rect_border(
	mesh: ImmediateMesh,
	rect: Rect2,
	color: Color,
	y: float,
	thickness: float
) -> void:
	if thickness <= 0.0:
		return

	var min_x: float = rect.position.x
	var min_z: float = rect.position.y
	var max_x: float = rect.position.x + rect.size.x
	var max_z: float = rect.position.y + rect.size.y

	var half_t: float = thickness * 0.5

	# Top edge strip
	_add_debug_quad(
		mesh,
		Vector3(min_x, y, min_z - half_t),
		Vector3(max_x, y, min_z - half_t),
		Vector3(max_x, y, min_z + half_t),
		Vector3(min_x, y, min_z + half_t),
		color
	)
	# Bottom edge strip
	_add_debug_quad(
		mesh,
		Vector3(min_x, y, max_z - half_t),
		Vector3(max_x, y, max_z - half_t),
		Vector3(max_x, y, max_z + half_t),
		Vector3(min_x, y, max_z + half_t),
		color
	)
	# Left edge strip
	_add_debug_quad(
		mesh,
		Vector3(min_x - half_t, y, min_z),
		Vector3(min_x + half_t, y, min_z),
		Vector3(min_x + half_t, y, max_z),
		Vector3(min_x - half_t, y, max_z),
		color
	)
	# Right edge strip
	_add_debug_quad(
		mesh,
		Vector3(max_x - half_t, y, min_z),
		Vector3(max_x + half_t, y, min_z),
		Vector3(max_x + half_t, y, max_z),
		Vector3(max_x - half_t, y, max_z),
		color
	)

func _add_debug_quad(
	mesh: ImmediateMesh,
	p0: Vector3,
	p1: Vector3,
	p2: Vector3,
	p3: Vector3,
	color: Color
) -> void:
	mesh.surface_set_color(color)
	mesh.surface_add_vertex(p0)
	mesh.surface_add_vertex(p1)
	mesh.surface_add_vertex(p2)

	mesh.surface_add_vertex(p0)
	mesh.surface_add_vertex(p2)
	mesh.surface_add_vertex(p3)
