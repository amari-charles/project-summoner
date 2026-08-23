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
const MAX_CLAMP_ITERATIONS: int = 4
const MAX_DYNAMIC_ZOOM_OUT_SOLVER_ITERATIONS: int = 12
const ZOOM_OUT_STABILITY_EPSILON: float = 0.01

enum OversizeClampMode {
	CENTER,
	PIN_MIN_EDGE,
	PIN_MAX_EDGE
}

# === Exports ===

@export_group("Pan Settings")
@export var pan_speed: float = 20.0
@export var keyboard_pan_enabled: bool = true
@export var mouse_pan_enabled: bool = true
@export var touch_pan_enabled: bool = true

@export_group("Map Boundaries")
## Axis-aligned world bounds on the ground plane (XZ)
@export var map_rect_xz: Rect2 = Rect2(Vector2(-50, -25), Vector2(100, 50))
## Optional separate bounds used only for solving zoom-out limits.
## Keep this at legacy 100x80 to preserve previous zoom restrictions
## while still clamping pan/footprint to the live map_rect_xz.
@export var zoom_limit_rect_xz: Rect2 = Rect2(Vector2(-50, -40), Vector2(100, 80))
@export var use_zoom_limit_rect_override: bool = false
## Ground plane Y value (most 2.5D maps use y=0)
@export var ground_y: float = 0.0
## Oversize clamp policy for horizontal axis when camera view is wider than map width.
@export var horizontal_oversize_clamp_mode: OversizeClampMode = OversizeClampMode.CENTER
## Oversize clamp policy for vertical axis when camera view is deeper than map depth.
@export var vertical_oversize_clamp_mode: OversizeClampMode = OversizeClampMode.CENTER
## Margin from the minimum horizontal edge when using PIN_MIN_EDGE mode.
@export var horizontal_pin_min_edge_margin: float = 0.0
## Margin from the maximum horizontal edge when using PIN_MAX_EDGE mode.
@export var horizontal_pin_max_edge_margin: float = 0.0
## Margin from the minimum vertical edge when using PIN_MIN_EDGE mode.
@export var vertical_pin_min_edge_margin: float = 0.0
## Margin from the maximum vertical edge when using PIN_MAX_EDGE mode.
@export var vertical_pin_max_edge_margin: float = 0.0
## If true, horizontal clamping uses a single screen-height sample instead of the
## full frustum footprint. This better matches gameplay focus depth for perspective.
@export var horizontal_bounds_use_screen_sample: bool = false
## Normalized screen Y in [0,1] used when horizontal_bounds_use_screen_sample is enabled.
## 0.0 = top of screen, 0.5 = center, 1.0 = bottom.
@export_range(0.0, 1.0, 0.01) var horizontal_bounds_screen_y: float = 0.5
## Additional upward (positive Z / far side) clamp room, in world units.
@export var vertical_far_clamp_margin: float = 0.0
## Screen Y row used as the "visual center" anchor when oversized vertical clamp mode is CENTER.
## 0.0 = top, 0.5 = screen center, 1.0 = bottom.
@export_range(0.0, 1.0, 0.01) var vertical_center_reference_screen_y: float = 0.5
@export_group("Profile")
## Optional perspective profile resource (transform + lens + clamp defaults).
@export var perspective_camera_profile: BattleCameraProjectionProfile
## If true, applying the profile updates camera transform exactly.
@export var apply_profile_transform_from_profile: bool = true

@export_group("Projection")
@export var default_fov: float = 38.0
@export var min_fov: float = 24.0  # Max zoom in
@export var max_fov: float = 62.0  # Max zoom out (limited to prevent showing outside map)
@export var perspective_near_clip: float = 0.5
@export var perspective_far_clip: float = 260.0

@export_group("Zoom Controls")
@export var zoom_speed: float = 2.0
@export var zoom_enabled: bool = true
## If true, zooming in applies an upward (backward) camera pitch.
@export var zoom_pitch_enabled: bool = false
## Maximum upward pitch applied at min_fov, in degrees.
@export_range(0.0, 30.0, 0.1) var zoom_pitch_max_degrees: float = 0.0

@export_group("Zoom-Based Panning")
## If true, vertical panning is only enabled when zoomed in from default framing.
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
## If enabled (debug builds), logs zoom solver/cap and clamp offset details each zoom step.
@export var debug_log_zoom_solver: bool = false

# === State Variables ===

# Pan state for mouse/touch input
var is_panning: bool = false
var last_mouse_position: Vector2
var _max_fov_ceiling: float = -1.0
var _is_camera_initialized: bool = false
var _zoom_pitch_base_transform: Transform3D = Transform3D.IDENTITY
var _zoom_pitch_current_radians: float = 0.0
var arena_floor_rect_xz: Rect2 = Rect2()
var _debug_overlay_mesh: MeshInstance3D
var _debug_overlay_lines: ImmediateMesh
var _debug_overlay_material: StandardMaterial3D

func _ready() -> void:
	# Set process mode to ALWAYS so camera panning works during dialogues
	process_mode = Node.PROCESS_MODE_ALWAYS
	edge_pan_enabled = edge_pan_enabled and SafeTypeUtils.bool_val(
		GameSettings.get_value(&"edge_pan_enabled"),
		true
	)
	pan_speed *= clampf(
		SafeTypeUtils.float_val(GameSettings.get_value(&"camera_speed"), 1.0),
		0.5,
		2.0
	)
	if SafeTypeUtils.bool_val(GameSettings.get_value(&"reduce_camera_motion"), false):
		zoom_pitch_enabled = false

	# Wait one frame for transform initialization
	await get_tree().process_frame
	if _max_fov_ceiling < 0.0:
		_max_fov_ceiling = max_fov
	_sync_zoom_pitch_base_from_current_transform()
	apply_perspective_profile(true)
	_is_camera_initialized = true

	var vp: Viewport = get_viewport()
	if vp and not vp.size_changed.is_connected(_on_viewport_size_changed):
		vp.size_changed.connect(_on_viewport_size_changed)

	if _is_debug_overlay_enabled():
		_ensure_debug_overlay()
		_update_debug_overlay()

func _on_viewport_size_changed() -> void:
	_refresh_zoom_limits()
	_apply_zoom_limits(false)
	clamp_to_map()

func _refresh_zoom_limits() -> void:
	var configured_max_fov: float = _max_fov_ceiling if _max_fov_ceiling > 0.0 else max_fov
	var solved_max_fov: float = _solve_max_fov(configured_max_fov)
	max_fov = max(min_fov, min(configured_max_fov, solved_max_fov))

func set_map_bounds(bounds_xz: Rect2) -> void:
	if bounds_xz.size.x <= 0.0 or bounds_xz.size.y <= 0.0:
		return

	map_rect_xz = bounds_xz
	if _max_fov_ceiling < 0.0:
		_max_fov_ceiling = max_fov
	if not _is_camera_initialized:
		return
	_ensure_camera_faces_map_center()
	_refresh_zoom_limits()
	_apply_zoom_limits(false)
	clamp_to_map()

	if _is_debug_overlay_enabled():
		_ensure_debug_overlay()
		_update_debug_overlay()

func set_arena_floor_bounds(bounds_xz: Rect2) -> void:
	if bounds_xz.size.x <= 0.0 or bounds_xz.size.y <= 0.0:
		arena_floor_rect_xz = Rect2()
	else:
		arena_floor_rect_xz = bounds_xz

	if _is_debug_overlay_enabled():
		_ensure_debug_overlay()
		_update_debug_overlay()

func apply_perspective_profile(reset_zoom: bool = true) -> void:
	projection = PROJECTION_PERSPECTIVE
	_apply_perspective_profile()
	near = perspective_near_clip
	far = perspective_far_clip

	_refresh_zoom_limits()
	_apply_zoom_limits(reset_zoom)
	_ensure_camera_faces_map_center()
	clamp_to_map()

func _apply_perspective_profile() -> void:
	var profile: BattleCameraProjectionProfile = perspective_camera_profile
	if not profile:
		return

	if apply_profile_transform_from_profile:
		transform = profile.camera_transform
		force_update_transform()

	keep_aspect = profile.keep_aspect as Camera3D.KeepAspect
	perspective_near_clip = profile.near_clip
	perspective_far_clip = profile.far_clip
	vertical_pan_only_when_zoomed = profile.vertical_pan_only_when_zoomed

	var min_zoom: float = max(profile.min_zoom, 0.01)
	var max_zoom: float = max(profile.max_zoom, min_zoom)
	var default_zoom: float = clamp(profile.default_zoom, min_zoom, max_zoom)
	var sample_y: float = clamp(profile.horizontal_bounds_screen_y, 0.0, 1.0)
	var far_margin: float = max(profile.vertical_far_clamp_margin, 0.0)
	var max_zoom_pitch_degrees: float = max(profile.zoom_pitch_max_degrees, 0.0)

	default_fov = default_zoom
	min_fov = min_zoom
	max_fov = max_zoom
	_max_fov_ceiling = max_fov

	zoom_pitch_enabled = profile.zoom_pitch_enabled
	zoom_pitch_max_degrees = max_zoom_pitch_degrees
	horizontal_bounds_use_screen_sample = profile.horizontal_bounds_use_screen_sample
	horizontal_bounds_screen_y = sample_y
	vertical_far_clamp_margin = far_margin
	_sync_zoom_pitch_base_from_current_transform()

func _apply_zoom_limits(reset_zoom: bool) -> void:
	if not reset_zoom:
		_sync_zoom_pitch_base_from_current_transform()
	if reset_zoom:
		fov = clamp(default_fov, min_fov, max_fov)
	else:
		fov = clamp(fov, min_fov, max_fov)
	_apply_zoom_pitch_from_current_fov()

func _sync_zoom_pitch_base_from_current_transform() -> void:
	_clear_zoom_pitch_from_transform()
	_zoom_pitch_base_transform = transform

func _clear_zoom_pitch_from_transform() -> void:
	if abs(_zoom_pitch_current_radians) <= CLAMP_EPSILON:
		return
	transform = transform.rotated_local(Vector3.RIGHT, -_zoom_pitch_current_radians)
	_zoom_pitch_current_radians = 0.0
	force_update_transform()

func _get_zoom_pitch_ratio() -> float:
	if not zoom_pitch_enabled:
		return 0.0
	if fov >= default_fov:
		return 0.0
	var zoom_span: float = default_fov - min_fov
	if zoom_span <= CLAMP_EPSILON:
		return 0.0
	return clamp((default_fov - fov) / zoom_span, 0.0, 1.0)

func _apply_zoom_pitch_from_current_fov() -> void:
	_clear_zoom_pitch_from_transform()
	var zoom_ratio: float = _get_zoom_pitch_ratio()
	var max_pitch_degrees: float = max(zoom_pitch_max_degrees, 0.0)
	var target_pitch_radians: float = deg_to_rad(max_pitch_degrees * zoom_ratio)
	if abs(target_pitch_radians) <= CLAMP_EPSILON:
		transform = _zoom_pitch_base_transform
		_zoom_pitch_current_radians = 0.0
		force_update_transform()
		return
	transform = _zoom_pitch_base_transform.rotated_local(Vector3.RIGHT, target_pitch_radians)
	_zoom_pitch_current_radians = target_pitch_radians
	force_update_transform()

func _get_zoom_anchor_screen_uv() -> Vector2:
	return Vector2(0.5, clamp(vertical_center_reference_screen_y, 0.0, 1.0))

func _stabilize_zoom_anchor(anchor_before: Vector3) -> void:
	if not anchor_before.is_finite():
		return

	var anchor_after: Vector3 = get_ground_point_for_screen_uv(_get_zoom_anchor_screen_uv())
	if not anchor_after.is_finite():
		return

	var delta_x: float = anchor_before.x - anchor_after.x
	var delta_z: float = anchor_before.z - anchor_after.z
	if abs(delta_x) < CLAMP_EPSILON and abs(delta_z) < CLAMP_EPSILON:
		return

	position.x += delta_x
	position.z += delta_z
	force_update_transform()

func _capture_zoom_state() -> Dictionary:
	return {
		"fov": fov,
		"transform": transform,
		"zoom_pitch_base_transform": _zoom_pitch_base_transform,
		"zoom_pitch_current_radians": _zoom_pitch_current_radians
	}

func _restore_zoom_state(state: Dictionary) -> void:
	var saved_fov_variant: Variant = state.get("fov", fov)
	var saved_fov: float = saved_fov_variant if saved_fov_variant is float or saved_fov_variant is int else fov
	var saved_transform_variant: Variant = state.get("transform", transform)
	var saved_transform: Transform3D = saved_transform_variant if saved_transform_variant is Transform3D else transform
	var saved_pitch_base_variant: Variant = state.get("zoom_pitch_base_transform", _zoom_pitch_base_transform)
	var saved_pitch_base: Transform3D = saved_pitch_base_variant if saved_pitch_base_variant is Transform3D else _zoom_pitch_base_transform
	var saved_pitch_radians_variant: Variant = state.get("zoom_pitch_current_radians", _zoom_pitch_current_radians)
	var saved_pitch_radians: float = saved_pitch_radians_variant if saved_pitch_radians_variant is float or saved_pitch_radians_variant is int else _zoom_pitch_current_radians

	fov = saved_fov
	transform = saved_transform
	_zoom_pitch_base_transform = saved_pitch_base
	_zoom_pitch_current_radians = saved_pitch_radians
	force_update_transform()

func _get_required_clamp_offset_for_current_state() -> Vector2:
	var footprint: Rect2 = get_ground_footprint_xz()
	if footprint.size == Vector2.ZERO:
		return Vector2(INF, INF)

	var view_bounds: Rect2 = _get_clamp_view_bounds_xz(footprint)
	var view_min_x: float = view_bounds.position.x
	var view_max_x: float = view_min_x + view_bounds.size.x
	var view_min_z: float = view_bounds.position.y
	var view_max_z: float = view_min_z + view_bounds.size.y

	var effective_map: Rect2 = _get_effective_map_bounds()
	var map_min_x: float = effective_map.position.x
	var map_min_z: float = effective_map.position.y
	var map_max_x: float = map_min_x + effective_map.size.x
	var map_max_z: float = map_min_z + effective_map.size.y

	var dx: float = _resolve_oversize_axis_offset(
		view_min_x,
		view_max_x,
		map_min_x,
		map_max_x,
		horizontal_oversize_clamp_mode,
		horizontal_pin_min_edge_margin,
		horizontal_pin_max_edge_margin
	)
	var dz: float = _resolve_oversize_axis_offset(
		view_min_z,
		view_max_z,
		map_min_z,
		map_max_z,
		vertical_oversize_clamp_mode,
		vertical_pin_min_edge_margin,
		vertical_pin_max_edge_margin,
		_get_vertical_center_anchor_z(view_min_z, view_max_z)
	)
	return Vector2(dx, dz)

func _is_clamp_offset_stable(offset: Vector2, epsilon: float = ZOOM_OUT_STABILITY_EPSILON) -> bool:
	return offset.is_finite() and abs(offset.x) <= epsilon and abs(offset.y) <= epsilon

func _is_zoom_out_candidate_stable(candidate_fov: float, anchor_before: Vector3) -> bool:
	var saved_state: Dictionary = _capture_zoom_state()
	fov = clamp(candidate_fov, min_fov, max_fov)
	_apply_zoom_pitch_from_current_fov()
	_stabilize_zoom_anchor(anchor_before)
	var footprint: Rect2 = get_ground_footprint_xz()
	var footprint_fits: bool = footprint.size != Vector2.ZERO and _footprint_fits_map(footprint)
	var required_offset: Vector2 = _get_required_clamp_offset_for_current_state()
	var is_stable: bool = footprint_fits and _is_clamp_offset_stable(required_offset)
	_restore_zoom_state(saved_state)
	return is_stable

func _solve_stable_zoom_out_fov(requested_fov: float, anchor_before: Vector3) -> float:
	var current_fov: float = fov
	var clamped_requested_fov: float = clamp(requested_fov, current_fov, max_fov)
	if clamped_requested_fov <= current_fov + CLAMP_EPSILON:
		return current_fov

	if _is_zoom_out_candidate_stable(clamped_requested_fov, anchor_before):
		return clamped_requested_fov

	var low: float = current_fov
	var high: float = clamped_requested_fov
	for i: int in range(MAX_DYNAMIC_ZOOM_OUT_SOLVER_ITERATIONS):
		var mid: float = (low + high) * 0.5
		if _is_zoom_out_candidate_stable(mid, anchor_before):
			low = mid
		else:
			high = mid
	return low

func _log_zoom_solver_step(
	delta: float,
	current_fov: float,
	requested_fov: float,
	solved_fov: float,
	pre_clamp_offset: Vector2,
	post_clamp_offset: Vector2
) -> void:
	if not OS.is_debug_build() or not debug_log_zoom_solver:
		return

	var capped: bool = solved_fov + CLAMP_EPSILON < requested_fov
	print(
		"[CameraZoom] delta=%.3f current_fov=%.3f requested_fov=%.3f solved_fov=%.3f capped=%s pre_clamp_offset=(%.3f, %.3f) post_clamp_offset=(%.3f, %.3f) pos=(%.3f, %.3f, %.3f)"
		% [
			delta,
			current_fov,
			requested_fov,
			solved_fov,
			str(capped),
			pre_clamp_offset.x,
			pre_clamp_offset.y,
			post_clamp_offset.x,
			post_clamp_offset.y,
			global_position.x,
			global_position.y,
			global_position.z
		]
	)

func _is_zoomed_in_for_vertical_pan() -> bool:
	return fov < default_fov

func _ensure_camera_faces_map_center() -> void:
	_sync_zoom_pitch_base_from_current_transform()
	var forward_xz: Vector2 = Vector2(-global_basis.z.x, -global_basis.z.z)
	if forward_xz.length_squared() <= CLAMP_EPSILON:
		_apply_zoom_pitch_from_current_fov()
		return

	var map_center_xz: Vector2 = map_rect_xz.position + map_rect_xz.size * 0.5
	var camera_xz: Vector2 = Vector2(global_position.x, global_position.z)
	var to_center: Vector2 = map_center_xz - camera_xz
	if to_center.length_squared() <= CLAMP_EPSILON:
		_apply_zoom_pitch_from_current_fov()
		return

	if forward_xz.normalized().dot(to_center.normalized()) < 0.0:
		rotate_y(PI)
		force_update_transform()
		_zoom_pitch_base_transform = transform
	_apply_zoom_pitch_from_current_fov()

func _solve_max_fov(configured_max: float) -> float:
	var original_fov: float = fov
	var original_transform: Transform3D = transform
	var original_zoom_pitch_base: Transform3D = _zoom_pitch_base_transform
	var original_zoom_pitch_radians: float = _zoom_pitch_current_radians
	var low: float = min_fov
	var high: float = max(configured_max, min_fov)

	for i: int in range(MAX_ZOOM_SOLVER_ITERATIONS):
		var mid: float = (low + high) * 0.5
		fov = mid
		_apply_zoom_pitch_from_current_fov()
		var footprint: Rect2 = get_ground_footprint_xz()
		if _footprint_fits_map(footprint):
			low = mid
		else:
			high = mid

	fov = original_fov
	transform = original_transform
	_zoom_pitch_base_transform = original_zoom_pitch_base
	_zoom_pitch_current_radians = original_zoom_pitch_radians
	force_update_transform()
	return low

func _footprint_fits_map(footprint: Rect2) -> bool:
	if footprint.size == Vector2.ZERO:
		return false

	var effective_map: Rect2 = _get_effective_zoom_bounds()
	var effective_view_width_x: float = _get_effective_view_width_x(footprint)
	return effective_view_width_x <= effective_map.size.x + CLAMP_EPSILON \
		and footprint.size.y <= effective_map.size.y + CLAMP_EPSILON

func _get_effective_zoom_bounds() -> Rect2:
	var base_rect: Rect2 = zoom_limit_rect_xz if use_zoom_limit_rect_override else map_rect_xz
	var far_margin: float = max(_get_active_vertical_far_clamp_margin(), 0.0)
	return Rect2(
		base_rect.position,
		Vector2(base_rect.size.x, base_rect.size.y + far_margin)
	)

func _get_effective_map_bounds() -> Rect2:
	var far_margin: float = max(_get_active_vertical_far_clamp_margin(), 0.0)
	return Rect2(
		map_rect_xz.position,
		Vector2(map_rect_xz.size.x, map_rect_xz.size.y + far_margin)
	)

func _get_oversize_mode_name(mode: OversizeClampMode) -> String:
	match mode:
		OversizeClampMode.PIN_MIN_EDGE:
			return "pin_min"
		OversizeClampMode.PIN_MAX_EDGE:
			return "pin_max"
		_:
			return "center"

func _get_clamp_view_bounds_xz(footprint: Rect2) -> Rect2:
	var view_min_x: float = footprint.position.x
	var view_max_x: float = view_min_x + footprint.size.x
	if _is_horizontal_sample_bounds_enabled():
		var sample_x_bounds: Vector2 = _get_horizontal_sample_bounds_x()
		if sample_x_bounds != Vector2.ZERO:
			view_min_x = sample_x_bounds.x
			view_max_x = sample_x_bounds.y

	return Rect2(
		Vector2(view_min_x, footprint.position.y),
		Vector2(view_max_x - view_min_x, footprint.size.y)
	)

func _resolve_oversize_axis_offset(
	view_min: float,
	view_max: float,
	map_min: float,
	map_max: float,
	mode: OversizeClampMode,
	pin_min_margin: float,
	pin_max_margin: float,
	center_anchor: float = NAN
) -> float:
	var view_size: float = view_max - view_min
	var map_size: float = map_max - map_min

	if view_size < map_size:
		if view_min < map_min:
			return map_min - view_min
		if view_max > map_max:
			return map_max - view_max
		return 0.0

	var safe_min_margin: float = max(pin_min_margin, 0.0)
	var safe_max_margin: float = max(pin_max_margin, 0.0)
	match mode:
		OversizeClampMode.PIN_MIN_EDGE:
			var target_min: float = clamp(map_min + safe_min_margin, map_min, map_max)
			return target_min - view_min
		OversizeClampMode.PIN_MAX_EDGE:
			var target_max: float = clamp(map_max - safe_max_margin, map_min, map_max)
			return target_max - view_max
		_:
			var view_center: float = center_anchor if not is_nan(center_anchor) else (view_min + view_max) * 0.5
			var map_center: float = (map_min + map_max) * 0.5
			return map_center - view_center

func _get_effective_view_width_x(footprint: Rect2) -> float:
	var effective_width: float = footprint.size.x
	if _is_horizontal_sample_bounds_enabled():
		var sample_x_bounds: Vector2 = _get_horizontal_sample_bounds_x()
		if sample_x_bounds != Vector2.ZERO:
			effective_width = sample_x_bounds.y - sample_x_bounds.x
	return effective_width

func _is_horizontal_sample_bounds_enabled() -> bool:
	return horizontal_bounds_use_screen_sample

func _get_active_horizontal_bounds_screen_y() -> float:
	return horizontal_bounds_screen_y

func _get_active_vertical_far_clamp_margin() -> float:
	return vertical_far_clamp_margin

func _get_vertical_center_anchor_z(view_min_z: float, view_max_z: float) -> float:
	var anchor_point: Vector3 = get_ground_point_for_screen_uv(Vector2(0.5, vertical_center_reference_screen_y))
	if anchor_point.is_finite():
		return anchor_point.z
	return (view_min_z + view_max_z) * 0.5

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
	if w <= 0.0 or h <= 0.0:
		return Rect2()
	var screen_size: Vector2 = Vector2(w, h)

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
		var origin: Vector3 = global_position
		var dir: Vector3 = _get_perspective_ray_direction(corner, screen_size)

		# Skip if ray is parallel to ground (would create unstable intersection).
		if abs(dir.y) < 0.0001:
			continue

		var t: float = (ground_y - origin.y) / dir.y
		if t < 0.0:
			continue

		var point: Vector3 = origin + dir * t
		var depth: float = _get_forward_depth(point)
		if depth < near - CLAMP_EPSILON or depth > far + CLAMP_EPSILON:
			continue
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

func _get_perspective_ray_direction(screen_pos: Vector2, screen_size: Vector2) -> Vector3:
	var aspect: float = screen_size.x / screen_size.y
	var half_fov_tan: float = tan(deg_to_rad(fov) * 0.5)

	var tan_x: float
	var tan_y: float
	if keep_aspect == KEEP_WIDTH:
		tan_x = half_fov_tan
		tan_y = tan_x / aspect
	else:
		tan_y = half_fov_tan
		tan_x = tan_y * aspect

	# Convert screen pixels to normalized device coordinates in range [-1, 1].
	var ndc_x: float = (screen_pos.x / screen_size.x) * 2.0 - 1.0
	var ndc_y: float = 1.0 - (screen_pos.y / screen_size.y) * 2.0
	var local_dir: Vector3 = Vector3(ndc_x * tan_x, ndc_y * tan_y, -1.0).normalized()
	return (global_basis * local_dir).normalized()

func _get_forward_depth(world_pos: Vector3) -> float:
	var forward: Vector3 = -global_basis.z
	return forward.dot(world_pos - global_position)

func _get_horizontal_sample_bounds_x() -> Vector2:
	var vp: Viewport = get_viewport()
	var view_size: Vector2i = vp.get_visible_rect().size
	var w: float = float(view_size.x)
	var h: float = float(view_size.y)
	if w <= 0.0 or h <= 0.0:
		return Vector2.ZERO

	var sample_y: float = clamp(_get_active_horizontal_bounds_screen_y(), 0.0, 1.0) * h
	var left_screen: Vector2 = Vector2(0.0, sample_y)
	var right_screen: Vector2 = Vector2(w, sample_y)

	var screen_size: Vector2 = Vector2(w, h)
	var left_origin: Vector3 = global_position
	var right_origin: Vector3 = global_position
	var left_dir: Vector3 = _get_perspective_ray_direction(left_screen, screen_size)
	var right_dir: Vector3 = _get_perspective_ray_direction(right_screen, screen_size)

	if abs(left_dir.y) < 0.0001 or abs(right_dir.y) < 0.0001:
		return Vector2.ZERO

	var left_t: float = (ground_y - left_origin.y) / left_dir.y
	var right_t: float = (ground_y - right_origin.y) / right_dir.y
	if left_t < 0.0 or right_t < 0.0:
		return Vector2.ZERO

	var left_point: Vector3 = left_origin + left_dir * left_t
	var right_point: Vector3 = right_origin + right_dir * right_t

	var left_depth: float = _get_forward_depth(left_point)
	var right_depth: float = _get_forward_depth(right_point)
	if left_depth < near - CLAMP_EPSILON or left_depth > far + CLAMP_EPSILON:
		return Vector2.ZERO
	if right_depth < near - CLAMP_EPSILON or right_depth > far + CLAMP_EPSILON:
		return Vector2.ZERO

	return Vector2(minf(left_point.x, right_point.x), maxf(left_point.x, right_point.x))

func get_ground_point_for_screen_uv(screen_uv: Vector2) -> Vector3:
	var vp: Viewport = get_viewport()
	var view_size: Vector2i = vp.get_visible_rect().size
	var w: float = float(view_size.x)
	var h: float = float(view_size.y)
	if w <= 0.0 or h <= 0.0:
		return Vector3.INF

	var clamped_uv: Vector2 = Vector2(
		clamp(screen_uv.x, 0.0, 1.0),
		clamp(screen_uv.y, 0.0, 1.0)
	)
	var screen_pos: Vector2 = Vector2(clamped_uv.x * w, clamped_uv.y * h)
	var screen_size: Vector2 = Vector2(w, h)

	var origin: Vector3 = global_position
	var dir: Vector3 = _get_perspective_ray_direction(screen_pos, screen_size)

	if abs(dir.y) < 0.0001:
		return Vector3.INF

	var t: float = (ground_y - origin.y) / dir.y
	if t < 0.0:
		return Vector3.INF

	var point: Vector3 = origin + dir * t
	var depth: float = _get_forward_depth(point)
	if depth < near - CLAMP_EPSILON or depth > far + CLAMP_EPSILON:
		return Vector3.INF

	return point

func clamp_to_map() -> void:
	## Clamps camera to keep ground footprint (projection) within map bounds
	##
	## Uses corner ray-casting to calculate what the camera sees on the ground,
	## then moves camera to keep that footprint inside map_rect_xz bounds.
	for i: int in range(MAX_CLAMP_ITERATIONS):
		var footprint: Rect2 = get_ground_footprint_xz()
		if footprint.size == Vector2.ZERO:
			return

		var view_bounds: Rect2 = _get_clamp_view_bounds_xz(footprint)
		var view_min_x: float = view_bounds.position.x
		var view_max_x: float = view_min_x + view_bounds.size.x
		var view_min_z: float = view_bounds.position.y
		var view_max_z: float = view_min_z + view_bounds.size.y

		# Map bounds
		var effective_map: Rect2 = _get_effective_map_bounds()
		var map_min_x: float = effective_map.position.x
		var map_min_z: float = effective_map.position.y
		var map_max_x: float = map_min_x + effective_map.size.x
		var map_max_z: float = map_min_z + effective_map.size.y

		# Calculate translation needed to bring footprint inside bounds
		var dx: float = _resolve_oversize_axis_offset(
			view_min_x,
			view_max_x,
			map_min_x,
			map_max_x,
			horizontal_oversize_clamp_mode,
			horizontal_pin_min_edge_margin,
			horizontal_pin_max_edge_margin
		)
		var dz: float = _resolve_oversize_axis_offset(
			view_min_z,
			view_max_z,
			map_min_z,
			map_max_z,
			vertical_oversize_clamp_mode,
			vertical_pin_min_edge_margin,
			vertical_pin_max_edge_margin,
			_get_vertical_center_anchor_z(view_min_z, view_max_z)
		)

		# STABILITY: Only apply correction if offset is significant (prevents micro-jitter)
		if abs(dx) < CLAMP_EPSILON and abs(dz) < CLAMP_EPSILON:
			return

		position.x += dx
		position.z += dz
		force_update_transform()

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
	var anchor_before: Vector3 = get_ground_point_for_screen_uv(_get_zoom_anchor_screen_uv())
	var current_fov: float = fov
	var requested_fov: float = clamp(fov + delta, min_fov, max_fov)
	if delta > 0.0 and requested_fov > current_fov + CLAMP_EPSILON:
		requested_fov = _solve_stable_zoom_out_fov(requested_fov, anchor_before)
	fov = requested_fov
	_apply_zoom_pitch_from_current_fov()
	_stabilize_zoom_anchor(anchor_before)
	var pre_clamp_offset: Vector2 = _get_required_clamp_offset_for_current_state()
	# Clamp after zoom to adjust for new view size
	clamp_to_map()
	var post_clamp_offset: Vector2 = _get_required_clamp_offset_for_current_state()
	_log_zoom_solver_step(
		delta,
		current_fov,
		clamp(current_fov + delta, min_fov, max_fov),
		fov,
		pre_clamp_offset,
		post_clamp_offset
	)

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
	if not vertical_pan_only_when_zoomed or _is_zoomed_in_for_vertical_pan():
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

	var view_bounds: Rect2 = _get_clamp_view_bounds_xz(footprint)
	var view_min_x: float = view_bounds.position.x
	var view_max_x: float = view_min_x + view_bounds.size.x
	var view_min_z: float = view_bounds.position.y
	var view_max_z: float = view_min_z + view_bounds.size.y
	var effective_map: Rect2 = _get_effective_map_bounds()
	var map_min_x: float = effective_map.position.x
	var map_max_x: float = map_min_x + effective_map.size.x
	var map_min_z: float = effective_map.position.y
	var map_max_z: float = map_min_z + effective_map.size.y

	var min_dx: float
	var max_dx: float
	var min_dz: float
	var max_dz: float

	var view_width: float = view_bounds.size.x
	var view_height: float = view_bounds.size.y
	var map_width: float = effective_map.size.x
	var map_height: float = effective_map.size.y

	if view_width >= map_width:
		var oversize_dx: float = _resolve_oversize_axis_offset(
			view_min_x,
			view_max_x,
			map_min_x,
			map_max_x,
			horizontal_oversize_clamp_mode,
			horizontal_pin_min_edge_margin,
			horizontal_pin_max_edge_margin
		)
		min_dx = oversize_dx
		max_dx = oversize_dx
	else:
		min_dx = map_min_x - view_min_x
		max_dx = map_max_x - view_max_x

	if view_height >= map_height:
		var oversize_dz: float = _resolve_oversize_axis_offset(
			view_min_z,
			view_max_z,
			map_min_z,
			map_max_z,
			vertical_oversize_clamp_mode,
			vertical_pin_min_edge_margin,
			vertical_pin_max_edge_margin,
			_get_vertical_center_anchor_z(view_min_z, view_max_z)
		)
		min_dz = oversize_dz
		max_dz = oversize_dz
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

func get_clamp_diagnostics() -> Dictionary:
	var footprint: Rect2 = get_ground_footprint_xz()
	var view_bounds: Rect2 = Rect2()
	if footprint.size != Vector2.ZERO:
		view_bounds = _get_clamp_view_bounds_xz(footprint)
	var map_bounds: Rect2 = _get_effective_map_bounds()

	var view_min_x: float = view_bounds.position.x
	var view_max_x: float = view_min_x + view_bounds.size.x
	var view_min_z: float = view_bounds.position.y
	var view_max_z: float = view_min_z + view_bounds.size.y
	var map_min_x: float = map_bounds.position.x
	var map_max_x: float = map_min_x + map_bounds.size.x
	var map_min_z: float = map_bounds.position.y
	var map_max_z: float = map_min_z + map_bounds.size.y

	var needs_x_oversize: bool = view_bounds.size.x >= map_bounds.size.x
	var needs_z_oversize: bool = view_bounds.size.y >= map_bounds.size.y
	var target_dx: float = 0.0
	var target_dz: float = 0.0
	if footprint.size != Vector2.ZERO:
		target_dx = _resolve_oversize_axis_offset(
			view_min_x,
			view_max_x,
			map_min_x,
			map_max_x,
			horizontal_oversize_clamp_mode,
			horizontal_pin_min_edge_margin,
			horizontal_pin_max_edge_margin
		)
		target_dz = _resolve_oversize_axis_offset(
			view_min_z,
			view_max_z,
			map_min_z,
			map_max_z,
			vertical_oversize_clamp_mode,
			vertical_pin_min_edge_margin,
			vertical_pin_max_edge_margin,
			_get_vertical_center_anchor_z(view_min_z, view_max_z)
		)

	return {
		"projection_mode": "Perspective",
		"camera_position": global_position,
		"map_bounds_xz": map_bounds,
		"footprint_xz": footprint,
		"view_bounds_xz": view_bounds,
		"horizontal_mode": _get_oversize_mode_name(horizontal_oversize_clamp_mode),
		"vertical_mode": _get_oversize_mode_name(vertical_oversize_clamp_mode),
		"vertical_center_reference_screen_y": vertical_center_reference_screen_y,
		"oversize_x": needs_x_oversize,
		"oversize_z": needs_z_oversize,
		"target_dx": target_dx,
		"target_dz": target_dz,
		"vertical_center_anchor_z": _get_vertical_center_anchor_z(view_min_z, view_max_z)
	}

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
	if not vertical_pan_only_when_zoomed or _is_zoomed_in_for_vertical_pan():
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
	if not vertical_pan_only_when_zoomed or _is_zoomed_in_for_vertical_pan():
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

	# Blue = live camera clamp bounds.
	_draw_debug_rect_border(
		_debug_overlay_lines,
		map_rect_xz,
		Color(0.2, 0.65, 1.0, 0.9),
		ground_y + debug_overlay_y_offset,
		debug_overlay_line_thickness
	)

	# Gold = actual arena floor mesh bounds.
	if arena_floor_rect_xz.size != Vector2.ZERO:
		_draw_debug_rect_border(
			_debug_overlay_lines,
			arena_floor_rect_xz,
			Color(1.0, 0.78, 0.2, 0.9),
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
