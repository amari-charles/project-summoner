extends GutTest

## Regression tests for CameraController3D boundary handling.
##
## Focus:
## - Scroll zoom must never expose outside map bounds
## - Large drag deltas must remain clamped
## - Edge-pan must not interfere with active drag panning

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
	assert_almost_eq(constrained.y, 0.0, 0.0001, "Constrained Z should remain unchanged")


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
