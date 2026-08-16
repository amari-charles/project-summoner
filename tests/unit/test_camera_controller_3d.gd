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


func test_perspective_profile_applies_transform_zoom_and_clamp_settings() -> void:
	_camera.map_rect_xz = Rect2(Vector2(-10000, -10000), Vector2(20000, 20000))

	var perspective_profile: BattleCameraProjectionProfile = BattleCameraProjectionProfile.new()
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
	perspective_profile.zoom_pitch_enabled = true
	perspective_profile.zoom_pitch_max_degrees = 16.0
	perspective_profile.horizontal_bounds_use_screen_sample = true
	perspective_profile.horizontal_bounds_screen_y = 0.55
	perspective_profile.vertical_far_clamp_margin = 1.25

	_camera.perspective_camera_profile = perspective_profile
	_camera.apply_profile_transform_from_profile = true
	_camera.apply_perspective_profile(true)

	assert_eq(_camera.projection, Camera3D.PROJECTION_PERSPECTIVE, "Perspective profile should set perspective projection")
	assert_almost_eq(_camera.position.z, -54.61, 0.001, "Perspective profile should apply perspective framing")
	assert_almost_eq(_camera.default_fov, 72.0, 0.001, "Perspective profile should apply FOV defaults")
	assert_false(_camera.vertical_pan_only_when_zoomed, "Perspective profile should allow vertical pan at default zoom")
	assert_true(_camera.zoom_pitch_enabled, "Perspective profile should apply zoom pitch toggle")
	assert_almost_eq(_camera.zoom_pitch_max_degrees, 16.0, 0.001, "Perspective profile should apply max zoom pitch")
	assert_true(_camera.horizontal_bounds_use_screen_sample, "Perspective profile should enable sampled horizontal bounds")


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


func test_fixed_camera_zoom_can_ignore_map_fit_limit() -> void:
	_camera.map_rect_xz = Rect2(Vector2(-5, -5), Vector2(10, 10))
	_camera.min_fov = 32.0
	_camera.max_fov = 58.0
	_camera._max_fov_ceiling = 58.0
	_camera.zoom_respects_map_bounds = false
	_camera._refresh_zoom_limits()
	_camera.fov = 58.0

	_camera._apply_zoom(-2.0)
	assert_almost_eq(_camera.fov, 56.0, 0.001, "Fixed camera should zoom in")
	_camera._apply_zoom(2.0)
	assert_almost_eq(_camera.fov, 58.0, 0.001, "Fixed camera should zoom back out")


func test_zoom_in_applies_backward_pitch_when_enabled() -> void:
	_camera.zoom_pitch_enabled = true
	_camera.zoom_pitch_max_degrees = 8.0
	_camera._apply_zoom_limits(true)

	var start_forward: Vector3 = -_camera.global_basis.z
	_camera._apply_zoom(-9999.0)
	var zoomed_forward: Vector3 = -_camera.global_basis.z

	assert_gt(
		zoomed_forward.y,
		start_forward.y,
		"Zoom-in with zoom pitch enabled should tilt camera backward (less downward forward vector)"
	)


func test_zooming_back_to_default_fov_removes_zoom_pitch_offset() -> void:
	_camera.zoom_pitch_enabled = true
	_camera.zoom_pitch_max_degrees = 8.0
	_camera._apply_zoom_limits(true)
	var baseline_basis: Basis = _camera.global_basis

	_camera._apply_zoom(-9999.0)
	_camera._apply_zoom_limits(true)
	_assert_basis_almost_eq(
		_camera.global_basis,
		baseline_basis,
		0.0001,
		"Returning to default zoom should restore baseline camera angle"
	)


func test_zoom_pitch_disabled_keeps_camera_rotation_constant_during_zoom() -> void:
	_camera.zoom_pitch_enabled = false
	_camera.zoom_pitch_max_degrees = 8.0
	_camera._apply_zoom_limits(true)
	var baseline_basis: Basis = _camera.global_basis

	_camera._apply_zoom(-9999.0)
	_assert_basis_almost_eq(
		_camera.global_basis,
		baseline_basis,
		0.0001,
		"Zoom pitch disabled should keep camera rotation unchanged while zooming"
	)


func test_zoom_pitch_keeps_reference_anchor_stable_during_zoom() -> void:
	_camera.map_rect_xz = Rect2(Vector2(-10000, -10000), Vector2(20000, 20000))
	_camera.vertical_center_reference_screen_y = 0.55
	_camera.zoom_pitch_enabled = true
	_camera.zoom_pitch_max_degrees = 16.0
	_camera._apply_zoom_limits(true)

	var anchor_before: Vector3 = _camera.get_ground_point_for_screen_uv(
		Vector2(0.5, _camera.vertical_center_reference_screen_y)
	)
	assert_true(anchor_before.is_finite(), "Reference anchor should be valid before zoom")

	_camera._apply_zoom(-12.0)
	var anchor_after: Vector3 = _camera.get_ground_point_for_screen_uv(
		Vector2(0.5, _camera.vertical_center_reference_screen_y)
	)
	assert_true(anchor_after.is_finite(), "Reference anchor should be valid after zoom")
	assert_almost_eq(
		anchor_after.x,
		anchor_before.x,
		0.05,
		"Zoom stabilization should keep horizontal reference anchor stable"
	)
	assert_almost_eq(
		anchor_after.z,
		anchor_before.z,
		0.05,
		"Zoom stabilization should keep depth reference anchor stable"
	)


func test_apply_zoom_limits_does_not_snap_position_after_pan_with_zoom_pitch() -> void:
	_camera.map_rect_xz = Rect2(Vector2(-10000, -10000), Vector2(20000, 20000))
	_camera.zoom_pitch_enabled = true
	_camera.zoom_pitch_max_degrees = 16.0
	_camera._apply_zoom_limits(true)

	_camera.position.x += 18.0
	_camera.position.z += 14.0
	var before_position: Vector3 = _camera.position

	_camera._apply_zoom_limits(false)
	assert_almost_eq(
		_camera.position.x,
		before_position.x,
		0.001,
		"Reapplying zoom limits should not snap camera X after panning"
	)
	assert_almost_eq(
		_camera.position.z,
		before_position.z,
		0.001,
		"Reapplying zoom limits should not snap camera Z after panning"
	)


func test_zoom_out_dynamic_cap_prevents_snap_translation() -> void:
	_camera.zoom_pitch_enabled = true
	_camera.zoom_pitch_max_degrees = 16.0
	_camera._apply_zoom_limits(true)

	var max_step_motion: float = 0.0
	for i: int in range(90):
		var previous_position: Vector3 = _camera.position
		_camera._apply_zoom(1.0)
		var step_motion: float = _camera.position.distance_to(previous_position)
		max_step_motion = max(max_step_motion, step_motion)

	assert_lt(
		max_step_motion,
		2.5,
		"Dynamic zoom-out cap should avoid abrupt one-step camera translation spikes"
	)


func test_zoom_out_cap_is_monotonic_and_sticky() -> void:
	_camera.zoom_pitch_enabled = true
	_camera.zoom_pitch_max_degrees = 16.0
	_camera._apply_zoom_limits(true)

	for i: int in range(120):
		_camera._apply_zoom(1.0)

	var capped_fov: float = _camera.fov
	var capped_position: Vector3 = _camera.position
	for i: int in range(8):
		_camera._apply_zoom(1.0)
		assert_almost_eq(
			_camera.fov,
			capped_fov,
			0.001,
			"After reaching dynamic cap, additional zoom-out input should not increase FOV"
		)
		assert_almost_eq(
			_camera.position.x,
			capped_position.x,
			0.01,
			"After reaching dynamic cap, additional zoom-out input should not shift X"
		)
		assert_almost_eq(
			_camera.position.z,
			capped_position.z,
			0.01,
			"After reaching dynamic cap, additional zoom-out input should not shift Z"
		)


func test_zoom_out_dynamic_cap_works_when_panned() -> void:
	_camera.zoom_pitch_enabled = true
	_camera.zoom_pitch_max_degrees = 16.0
	_camera._apply_zoom_limits(true)

	_camera.position.x += 24.0
	_camera.position.z += 16.0
	_camera.clamp_to_map()

	var max_step_motion: float = 0.0
	for i: int in range(120):
		var previous_position: Vector3 = _camera.position
		_camera._apply_zoom(1.0)
		var step_motion: float = _camera.position.distance_to(previous_position)
		max_step_motion = max(max_step_motion, step_motion)

	assert_lt(
		max_step_motion,
		2.5,
		"Dynamic zoom-out cap should still prevent large snap translation when camera is panned"
	)

	var capped_fov: float = _camera.fov
	var capped_position: Vector3 = _camera.position
	_camera._apply_zoom(4.0)
	assert_almost_eq(
		_camera.fov,
		capped_fov,
		0.001,
		"Panned camera should stop at its dynamic zoom-out cap"
	)
	assert_almost_eq(
		_camera.position.x,
		capped_position.x,
		0.01,
		"Panned camera should not shift X after hitting dynamic zoom-out cap"
	)
	assert_almost_eq(
		_camera.position.z,
		capped_position.z,
		0.01,
		"Panned camera should not shift Z after hitting dynamic zoom-out cap"
	)
	var residual_offset: Vector2 = _camera._get_required_clamp_offset_for_current_state()
	assert_true(
		_camera._is_clamp_offset_stable(residual_offset),
		"Panned dynamic zoom-out cap should settle at a stable no-translation clamp state"
	)


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


func test_vertical_oversize_mode_pin_min_edge_pins_near_edge() -> void:
	_camera.map_rect_xz = Rect2(Vector2(-50, -10), Vector2(100, 20))
	_camera.vertical_oversize_clamp_mode = CameraController3D.OversizeClampMode.PIN_MIN_EDGE
	_camera.vertical_pin_min_edge_margin = 2.0
	_camera.horizontal_oversize_clamp_mode = CameraController3D.OversizeClampMode.CENTER
	_camera.position.z += 1000.0
	_camera.clamp_to_map()

	var view_bounds: Rect2 = _get_effective_view_bounds_xz()
	var map_min_z: float = _camera.map_rect_xz.position.y
	assert_almost_eq(
		view_bounds.position.y,
		map_min_z + 2.0,
		0.1,
		"PIN_MIN_EDGE should place the view minimum Z at map minimum plus configured margin"
	)


func test_vertical_oversize_mode_pin_max_edge_pins_far_edge() -> void:
	_camera.map_rect_xz = Rect2(Vector2(-50, -10), Vector2(100, 20))
	_camera.vertical_oversize_clamp_mode = CameraController3D.OversizeClampMode.PIN_MAX_EDGE
	_camera.vertical_pin_max_edge_margin = 1.5
	_camera.horizontal_oversize_clamp_mode = CameraController3D.OversizeClampMode.CENTER
	_camera.position.z -= 1000.0
	_camera.clamp_to_map()

	var view_bounds: Rect2 = _get_effective_view_bounds_xz()
	var map_max_z: float = _camera.map_rect_xz.position.y + _camera.map_rect_xz.size.y
	assert_almost_eq(
		view_bounds.position.y + view_bounds.size.y,
		map_max_z - 1.5,
		0.1,
		"PIN_MAX_EDGE should place the view maximum Z at map maximum minus configured margin"
	)


func test_vertical_oversize_mode_center_keeps_screen_reference_centered() -> void:
	_camera.map_rect_xz = Rect2(Vector2(-50, -10), Vector2(100, 20))
	_camera.vertical_oversize_clamp_mode = CameraController3D.OversizeClampMode.CENTER
	_camera.horizontal_oversize_clamp_mode = CameraController3D.OversizeClampMode.CENTER
	_camera.position.z += 600.0
	_camera.clamp_to_map()

	var anchor_point: Vector3 = _camera.get_ground_point_for_screen_uv(
		Vector2(0.5, _camera.vertical_center_reference_screen_y)
	)
	var map_center_z: float = _camera.map_rect_xz.position.y + (_camera.map_rect_xz.size.y * 0.5)
	assert_almost_eq(
		anchor_point.z,
		map_center_z,
		0.1,
		"CENTER oversize mode should align the configured screen reference row with map depth center"
	)


func test_vertical_oversize_center_can_follow_screen_reference_anchor() -> void:
	_camera.map_rect_xz = Rect2(Vector2(-50, -10), Vector2(100, 20))
	_camera.vertical_oversize_clamp_mode = CameraController3D.OversizeClampMode.CENTER
	_camera.vertical_center_reference_screen_y = 0.65
	_camera.position.z += 600.0
	_camera.clamp_to_map()

	var anchor_point: Vector3 = _camera.get_ground_point_for_screen_uv(
		Vector2(0.5, _camera.vertical_center_reference_screen_y)
	)
	var map_center_z: float = _camera.map_rect_xz.position.y + (_camera.map_rect_xz.size.y * 0.5)
	assert_almost_eq(
		anchor_point.z,
		map_center_z,
		0.1,
		"Vertical CENTER oversize mode should align the chosen screen reference row with map center"
	)


func test_camera_clamp_diagnostics_reports_mode_and_target_offsets() -> void:
	_camera.map_rect_xz = Rect2(Vector2(-50, -10), Vector2(100, 20))
	_camera.vertical_oversize_clamp_mode = CameraController3D.OversizeClampMode.PIN_MIN_EDGE
	_camera.vertical_pin_min_edge_margin = 1.0
	_camera.clamp_to_map()

	var diagnostics: Dictionary = _camera.get_clamp_diagnostics()
	assert_eq(diagnostics.get("vertical_mode"), "pin_min", "Diagnostics should expose active vertical oversize mode")
	assert_true(diagnostics.has("target_dz"), "Diagnostics should expose target_dz correction")
	assert_true(diagnostics.has("view_bounds_xz"), "Diagnostics should expose effective view bounds")
	assert_true(diagnostics.has("map_bounds_xz"), "Diagnostics should expose effective map bounds")


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


func test_apply_perspective_profile_with_realistic_bounds_keeps_valid_startup_fov() -> void:
	_camera.map_rect_xz = Rect2(Vector2(-50, -40), Vector2(100, 80))

	var perspective_profile: BattleCameraProjectionProfile = BattleCameraProjectionProfile.new()
	perspective_profile.camera_transform = Transform3D(
		Vector3(1, 0, 0),
		Vector3(0, 0.819152, 0.573576),
		Vector3(0, 0.573576, -0.819152),
		Vector3(0, 34.71, -55.6859)
	)
	perspective_profile.keep_aspect = Camera3D.KEEP_WIDTH
	perspective_profile.default_zoom = 72.0
	perspective_profile.min_zoom = 24.0
	perspective_profile.max_zoom = 82.0
	perspective_profile.vertical_pan_only_when_zoomed = false
	perspective_profile.horizontal_bounds_use_screen_sample = true
	perspective_profile.horizontal_bounds_screen_y = 0.55
	perspective_profile.vertical_far_clamp_margin = 0.0

	_camera.perspective_camera_profile = perspective_profile
	_camera.apply_profile_transform_from_profile = true
	_camera.apply_perspective_profile(true)
	var first_fov: float = _camera.fov
	var first_z: float = _camera.position.z

	assert_eq(_camera.projection, Camera3D.PROJECTION_PERSPECTIVE, "Camera should remain perspective-only")
	assert_true(first_fov <= perspective_profile.default_zoom, "Startup FOV should respect profile default upper bound")
	assert_true(first_fov >= _camera.min_fov, "Startup FOV should not collapse below min_fov")
	assert_true(first_z <= perspective_profile.camera_transform.origin.z, "Clamp may pull camera back but should not move farther into map")

	_camera.apply_perspective_profile(true)
	assert_almost_eq(_camera.fov, first_fov, 0.001, "Repeated startup application should keep stable solved FOV")
	assert_almost_eq(_camera.position.z, first_z, 0.001, "Repeated startup application should keep stable clamped position")


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


func _assert_basis_almost_eq(actual: Basis, expected: Basis, epsilon: float, message: String) -> void:
	_assert_vector3_almost_eq(actual.x, expected.x, epsilon, "%s (basis.x)" % message)
	_assert_vector3_almost_eq(actual.y, expected.y, epsilon, "%s (basis.y)" % message)
	_assert_vector3_almost_eq(actual.z, expected.z, epsilon, "%s (basis.z)" % message)


func _assert_vector3_almost_eq(actual: Vector3, expected: Vector3, epsilon: float, message: String) -> void:
	assert_almost_eq(actual.x, expected.x, epsilon, "%s x mismatch" % message)
	assert_almost_eq(actual.y, expected.y, epsilon, "%s y mismatch" % message)
	assert_almost_eq(actual.z, expected.z, epsilon, "%s z mismatch" % message)


func _get_effective_view_bounds_xz() -> Rect2:
	var footprint: Rect2 = _camera.get_ground_footprint_xz()
	if footprint.size == Vector2.ZERO:
		return Rect2()

	var view_min_x: float = footprint.position.x
	var view_max_x: float = view_min_x + footprint.size.x
	if _camera._is_horizontal_sample_bounds_enabled():
		var sample_x_bounds: Vector2 = _camera._get_horizontal_sample_bounds_x()
		if sample_x_bounds != Vector2.ZERO:
			view_min_x = sample_x_bounds.x
			view_max_x = sample_x_bounds.y

	return Rect2(
		Vector2(view_min_x, footprint.position.y),
		Vector2(view_max_x - view_min_x, footprint.size.y)
	)
