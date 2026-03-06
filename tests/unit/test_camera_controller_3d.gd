extends GutTest

## Regression tests for CameraController3D boundary handling.
##
## Focus:
## - Scroll zoom must never expose outside map bounds
## - Large drag deltas must remain clamped
## - Edge-pan must not interfere with active drag panning
## - Perspective camera may show foreground (near-camera) space below map min Z

var _camera: CameraController3D


func before_each() -> void:
	_camera = CameraController3D.new()
	_camera.transform = Transform3D(
		Vector3(1, 0, 0),
		Vector3(0, 0.819152, 0.573576),
		Vector3(0, 0.573576, -0.819152),
		Vector3(0, 30, -42.85)
	)
	_camera.map_rect_xz = Rect2(Vector2(-50, -40), Vector2(100, 80))
	_camera.keyboard_pan_enabled = false
	_camera.mouse_pan_enabled = false
	_camera.touch_pan_enabled = false
	_camera.edge_pan_enabled = false
	_camera.current = true
	add_child(_camera)
	await get_tree().process_frame
	await get_tree().process_frame


func after_each() -> void:
	if is_instance_valid(_camera) and _camera.is_inside_tree():
		_camera.queue_free()
	await get_tree().process_frame


func test_zoom_out_clamps_ground_footprint_inside_map() -> void:
	_camera._apply_zoom(9999.0)  # Simulate aggressive scroll-wheel zoom out
	_assert_footprint_inside_map("Zoom out must keep footprint inside map")


func test_camera_uses_perspective_projection() -> void:
	assert_eq(
		_camera.projection,
		Camera3D.PROJECTION_PERSPECTIVE,
		"Battle camera should default to perspective projection"
	)


func test_projection_mode_toggle_switches_projection_and_zoom_domain() -> void:
	_camera.set_projection_mode(BattleCameraProjectionProfile.ProjectionMode.ORTHOGRAPHIC)
	assert_eq(
		_camera.projection,
		Camera3D.PROJECTION_ORTHOGONAL,
		"Projection toggle should switch to orthographic mode"
	)

	var start_size: float = _camera.size
	_camera._apply_zoom(-4.0)
	assert_lt(_camera.size, start_size, "Orthographic zoom in should decrease camera size")

	_camera._apply_zoom(-9999.0)
	assert_almost_eq(_camera.size, _camera.min_ortho_size, 0.001, "Ortho zoom in should clamp to min_ortho_size")

	_camera._apply_zoom(9999.0)
	assert_almost_eq(_camera.size, _camera.max_ortho_size, 0.001, "Ortho zoom out should clamp to max_ortho_size")

	_camera.set_projection_mode(BattleCameraProjectionProfile.ProjectionMode.PERSPECTIVE)
	assert_eq(
		_camera.projection,
		Camera3D.PROJECTION_PERSPECTIVE,
		"Projection toggle should switch back to perspective mode"
	)

func test_projection_profiles_apply_mode_specific_transform_and_zoom() -> void:
	_camera.map_rect_xz = Rect2(Vector2(-10000, -10000), Vector2(20000, 20000))

	var perspective_profile: BattleCameraProjectionProfile = BattleCameraProjectionProfile.new()
	perspective_profile.projection_mode = BattleCameraProjectionProfile.ProjectionMode.PERSPECTIVE
	perspective_profile.camera_transform = Transform3D(
		Vector3(1, 0, 0),
		Vector3(0, 0.819152, 0.573576),
		Vector3(0, 0.573576, -0.819152),
		Vector3(0, 46.18, -54.61)
	)
	perspective_profile.keep_aspect = Camera3D.KEEP_WIDTH
	perspective_profile.default_zoom = 72.0
	perspective_profile.min_zoom = 24.0
	perspective_profile.max_zoom = 82.0
	perspective_profile.vertical_pan_only_when_zoomed = false
	perspective_profile.horizontal_bounds_use_screen_sample = true
	perspective_profile.horizontal_bounds_screen_y = 0.55
	perspective_profile.vertical_far_clamp_margin = 1.25

	var ortho_profile: BattleCameraProjectionProfile = BattleCameraProjectionProfile.new()
	ortho_profile.projection_mode = BattleCameraProjectionProfile.ProjectionMode.ORTHOGRAPHIC
	ortho_profile.camera_transform = Transform3D(
		Vector3(1, 0, 0),
		Vector3(0, 0.819152, 0.573576),
		Vector3(0, 0.573576, -0.819152),
		Vector3(0, 30, -42.85)
	)
	ortho_profile.keep_aspect = Camera3D.KEEP_HEIGHT
	ortho_profile.default_zoom = 40.0
	ortho_profile.min_zoom = 20.0
	ortho_profile.max_zoom = 50.0
	ortho_profile.vertical_pan_only_when_zoomed = true
	ortho_profile.horizontal_bounds_use_screen_sample = false
	ortho_profile.horizontal_bounds_screen_y = 0.5
	ortho_profile.vertical_far_clamp_margin = 0.0

	_camera.perspective_camera_profile = perspective_profile
	_camera.orthographic_camera_profile = ortho_profile
	_camera.apply_profile_transform_on_mode_switch = true

	_camera.set_projection_mode(BattleCameraProjectionProfile.ProjectionMode.ORTHOGRAPHIC)
	assert_eq(_camera.projection, Camera3D.PROJECTION_ORTHOGONAL, "Ortho profile should set orthographic projection")
	assert_almost_eq(_camera.position.z, -42.85, 0.001, "Ortho profile should restore old camera distance")
	assert_almost_eq(_camera.default_ortho_size, 40.0, 0.001, "Ortho profile should apply orthographic zoom defaults")
	assert_true(_camera.vertical_pan_only_when_zoomed, "Ortho profile should keep vertical pan gated by zoom")
	assert_false(_camera.ortho_horizontal_bounds_use_screen_sample, "Ortho profile should keep strict horizontal bounds")

	_camera.set_projection_mode(BattleCameraProjectionProfile.ProjectionMode.PERSPECTIVE)
	assert_eq(_camera.projection, Camera3D.PROJECTION_PERSPECTIVE, "Perspective profile should set perspective projection")
	assert_almost_eq(_camera.position.z, -54.61, 0.001, "Perspective profile should apply perspective framing")
	assert_almost_eq(_camera.default_fov, 72.0, 0.001, "Perspective profile should apply FOV defaults")
	assert_false(_camera.vertical_pan_only_when_zoomed, "Perspective profile should allow vertical pan at default zoom")
	assert_true(_camera.horizontal_bounds_use_screen_sample, "Perspective profile should enable sampled horizontal bounds")

func test_projection_modes_use_independent_clamp_profiles() -> void:
	_camera.horizontal_bounds_use_screen_sample = true
	_camera.ortho_horizontal_bounds_use_screen_sample = false
	_camera.vertical_far_clamp_margin = 1.25
	_camera.ortho_vertical_far_clamp_margin = 0.0

	_camera.set_projection_mode(BattleCameraProjectionProfile.ProjectionMode.PERSPECTIVE)
	assert_true(
		_camera._is_horizontal_sample_bounds_enabled(),
		"Perspective mode should use perspective horizontal sample profile"
	)
	var perspective_bounds: Rect2 = _camera._get_effective_map_bounds()

	_camera.set_projection_mode(BattleCameraProjectionProfile.ProjectionMode.ORTHOGRAPHIC)
	assert_false(
		_camera._is_horizontal_sample_bounds_enabled(),
		"Orthographic mode should use orthographic horizontal sample profile"
	)
	var ortho_bounds: Rect2 = _camera._get_effective_map_bounds()

	assert_gt(
		perspective_bounds.size.y,
		ortho_bounds.size.y,
		"Perspective far clamp margin should be independent from orthographic margin"
	)


func test_set_map_bounds_reorients_camera_when_facing_away_from_map() -> void:
	_camera.rotate_y(PI)
	_camera.force_update_transform()
	_camera.set_map_bounds(_camera.map_rect_xz)

	var forward_xz: Vector2 = Vector2(-_camera.global_basis.z.x, -_camera.global_basis.z.z).normalized()
	var map_center: Vector2 = _camera.map_rect_xz.position + (_camera.map_rect_xz.size * 0.5)
	var to_center: Vector2 = (map_center - Vector2(_camera.global_position.x, _camera.global_position.z)).normalized()
	assert_gt(
		forward_xz.dot(to_center),
		0.0,
		"Camera should be oriented toward map center after bounds are configured"
	)


func test_zoom_adjusts_fov_and_clamps_limits() -> void:
	var start_fov: float = _camera.fov
	_camera._apply_zoom(-4.0)
	assert_lt(_camera.fov, start_fov, "Zoom in should decrease FOV")

	var zoomed_in_fov: float = _camera.fov
	_camera._apply_zoom(4.0)
	assert_gt(_camera.fov, zoomed_in_fov, "Zoom out should increase FOV")

	_camera._apply_zoom(-9999.0)
	assert_almost_eq(_camera.fov, _camera.min_fov, 0.001, "Zoom in should clamp to min_fov")

	_camera._apply_zoom(9999.0)
	assert_almost_eq(_camera.fov, _camera.max_fov, 0.001, "Zoom out should clamp to max_fov")


func test_large_drag_delta_clamps_ground_footprint_inside_map() -> void:
	_camera.vertical_pan_only_when_zoomed = false
	_camera._apply_pan_delta(Vector2(-100000, 100000))
	_assert_footprint_inside_map("Large drag delta must stay clamped")

	_camera._apply_pan_delta(Vector2(100000, -100000))
	_assert_footprint_inside_map("Reverse large drag delta must stay clamped")


func test_random_zoom_and_pan_sequence_keeps_footprint_inside_map() -> void:
	_camera.vertical_pan_only_when_zoomed = false
	var rng: RandomNumberGenerator = RandomNumberGenerator.new()
	rng.seed = 20260305

	for i: int in range(250):
		if rng.randf() < 0.45:
			var zoom_delta: float = rng.randf_range(-8.0, 8.0)
			_camera._apply_zoom(zoom_delta)
		else:
			var pan_delta: Vector2 = Vector2(
				rng.randf_range(-5000.0, 5000.0),
				rng.randf_range(-5000.0, 5000.0)
			)
			_camera._apply_pan_delta(pan_delta)

		_assert_footprint_inside_map("Random sequence step %d" % i)


func test_constrain_pan_motion_limits_single_step_to_available_room() -> void:
	_camera.vertical_pan_only_when_zoomed = false
	var footprint: Rect2 = _camera.get_ground_footprint_xz()
	var map_rect: Rect2 = _camera.map_rect_xz

	var available_right_dx: float = (map_rect.position.x + map_rect.size.x) - (footprint.position.x + footprint.size.x)
	var constrained: Vector2 = _camera._constrain_pan_motion_to_map(100000.0, 0.0)

	assert_almost_eq(
		constrained.x,
		available_right_dx,
		0.01,
		"Constrained X pan should be capped to remaining room to the right boundary"
	)

	var start_position: Vector3 = _camera.position
	_camera.position.x += constrained.x
	_camera.position.z += constrained.y
	_camera.clamp_to_map()
	_assert_footprint_inside_map("Constrained pan result should keep footprint inside map limits")
	_camera.position = start_position


func test_perspective_clamp_is_stable_at_edges() -> void:
	_camera.vertical_pan_only_when_zoomed = false
	# Force a large move so clamp pushes to map boundary in perspective mode.
	_camera.position.x += 10000.0
	_camera.position.z += 10000.0
	_camera.clamp_to_map()
	var anchored_position: Vector3 = _camera.position
	_assert_footprint_inside_map("Initial clamp after large move must remain inside map")

	for i: int in range(5):
		_camera.clamp_to_map()
		assert_almost_eq(
			_camera.position.x,
			anchored_position.x,
			0.001,
			"Repeated clamp should not drift on X in perspective mode (iteration %d)" % i
		)
		assert_almost_eq(
			_camera.position.z,
			anchored_position.z,
			0.001,
			"Repeated clamp should not drift on Z in perspective mode (iteration %d)" % i
		)
		_assert_footprint_inside_map("Repeated clamp must keep footprint inside map (iteration %d)" % i)


func test_horizontal_sample_mode_allows_more_side_pan_room() -> void:
	_camera.vertical_pan_only_when_zoomed = false
	_camera.keep_aspect = Camera3D.KEEP_WIDTH
	_camera.transform = Transform3D(
		Vector3(1, 0, 0),
		Vector3(0, 0.587785, 0.809017),
		Vector3(0, 0.809017, -0.587785),
		Vector3(0, 30, -25.17)
	)
	_camera.default_fov = 56.0
	_camera.fov = 56.0
	_camera.force_update_transform()
	_camera.clamp_to_map()

	_camera.horizontal_bounds_use_screen_sample = false
	var strict_x: float = _camera._constrain_pan_motion_to_map(100000.0, 0.0).x

	_camera.horizontal_bounds_use_screen_sample = true
	_camera.horizontal_bounds_screen_y = 0.55
	var sampled_x: float = _camera._constrain_pan_motion_to_map(100000.0, 0.0).x

	assert_gt(
		sampled_x,
		strict_x,
		"Horizontal sample bounds should allow more left/right travel than strict full-frustum bounds"
	)

func test_horizontal_sample_mode_expands_solved_max_fov() -> void:
	_camera.keep_aspect = Camera3D.KEEP_WIDTH
	_camera.map_rect_xz = Rect2(Vector2(-50, -1000), Vector2(100, 2000))
	_camera.transform = Transform3D(
		Vector3(1, 0, 0),
		Vector3(0, 0.819152, 0.573576),
		Vector3(0, 0.573576, -0.819152),
		Vector3(0, 30, -42.85)
	)
	_camera.min_fov = 24.0
	_camera.max_fov = 110.0
	_camera._max_fov_ceiling = 110.0
	_camera.fov = 56.0
	_camera.force_update_transform()
	_camera.clamp_to_map()

	_camera.horizontal_bounds_use_screen_sample = false
	_camera._refresh_zoom_limits()
	var strict_max_fov: float = _camera.max_fov

	_camera.max_fov = 110.0
	_camera._max_fov_ceiling = 110.0
	_camera.horizontal_bounds_use_screen_sample = true
	_camera.horizontal_bounds_screen_y = 0.92
	_camera._refresh_zoom_limits()
	var sampled_max_fov: float = _camera.max_fov

	assert_gt(
		sampled_max_fov,
		strict_max_fov,
		"Sample-based horizontal fitting should permit farther perspective zoom-out"
	)

func test_vertical_far_margin_allows_more_upward_pan_room() -> void:
	_camera.vertical_pan_only_when_zoomed = false
	_camera.vertical_far_clamp_margin = 0.0
	_camera.clamp_to_map()
	var strict_upward_room: float = _camera._constrain_pan_motion_to_map(0.0, 100000.0).y

	_camera.vertical_far_clamp_margin = 1.25
	_camera.clamp_to_map()
	var margin_upward_room: float = _camera._constrain_pan_motion_to_map(0.0, 100000.0).y

	assert_gt(
		margin_upward_room,
		strict_upward_room,
		"Vertical far-side margin should provide more upward pan room"
	)


func test_edge_pan_is_ignored_while_drag_panning() -> void:
	# Oversized margin guarantees edge-pan input regardless cursor location.
	_camera.edge_pan_margin = 100000.0
	var start_position: Vector3 = _camera.position

	_camera.is_panning = false
	_camera._handle_edge_pan(1.0)
	assert_ne(_camera.position, start_position, "Edge-pan should move camera when not dragging")

	var moved_position: Vector3 = _camera.position
	_camera.is_panning = true
	_camera._handle_edge_pan(1.0)
	assert_eq(_camera.position, moved_position, "Edge-pan must not apply during drag panning")


func _assert_footprint_inside_map(message_prefix: String) -> void:
	var footprint: Rect2 = _camera.get_ground_footprint_xz()
	var map_rect: Rect2 = _camera.map_rect_xz
	var epsilon: float = 0.05

	assert_ne(footprint.size, Vector2.ZERO, "%s: footprint should be valid" % message_prefix)
	assert_true(
		footprint.position.x >= map_rect.position.x - epsilon,
		"%s: footprint min X escaped map (%.3f < %.3f)" % [
			message_prefix,
			footprint.position.x,
			map_rect.position.x
		]
	)
	if not _camera.is_perspective_mode():
		assert_true(
			footprint.position.y >= map_rect.position.y - epsilon,
			"%s: footprint min Z escaped map (%.3f < %.3f)" % [
				message_prefix,
				footprint.position.y,
				map_rect.position.y
			]
		)
	assert_true(
		(footprint.position.x + footprint.size.x) <= (map_rect.position.x + map_rect.size.x + epsilon),
		"%s: footprint max X escaped map (%.3f > %.3f)" % [
			message_prefix,
			footprint.position.x + footprint.size.x,
			map_rect.position.x + map_rect.size.x
		]
	)
	assert_true(
		(footprint.position.y + footprint.size.y) <= (map_rect.position.y + map_rect.size.y + epsilon),
		"%s: footprint max Z escaped map (%.3f > %.3f)" % [
			message_prefix,
			footprint.position.y + footprint.size.y,
			map_rect.position.y + map_rect.size.y
		]
	)
