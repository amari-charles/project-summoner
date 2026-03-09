extends GutTest

var _host: Node3D


class _FakeCamera extends CameraController3D:
	var simulated_footprint: Rect2 = Rect2(Vector2(-60.0, -60.0), Vector2(120.0, 120.0))
	var captured_map_bounds: Rect2 = Rect2()
	var captured_arena_bounds: Rect2 = Rect2()

	func get_ground_footprint_xz() -> Rect2:
		return simulated_footprint

	func set_map_bounds(bounds_xz: Rect2) -> void:
		captured_map_bounds = bounds_xz

	func set_arena_floor_bounds(bounds_xz: Rect2) -> void:
		captured_arena_bounds = bounds_xz


func _make_background_plane(width: float, depth: float) -> MeshInstance3D:
	var background: MeshInstance3D = MeshInstance3D.new()
	var plane: PlaneMesh = PlaneMesh.new()
	plane.size = Vector2(width, depth)
	background.mesh = plane
	return background

func before_each() -> void:
	_host = Node3D.new()
	add_child(_host)
	await get_tree().process_frame

func after_each() -> void:
	if is_instance_valid(_host) and _host.is_inside_tree():
		_host.queue_free()
	await get_tree().process_frame


func test_configure_camera_bounds_merges_startup_footprint_only_on_enabled_axes() -> void:
	var battlefield: BaseBattlefield3D = BaseBattlefield3D.new()
	var camera: _FakeCamera = _FakeCamera.new()

	var perspective_profile: BattleCameraProjectionProfile = BattleCameraProjectionProfile.new()
	perspective_profile.projection_mode = BattleCameraProjectionProfile.ProjectionMode.PERSPECTIVE
	perspective_profile.camera_transform = Transform3D.IDENTITY.translated(Vector3(0.0, 30.0, -42.85))
	perspective_profile.keep_aspect = Camera3D.KEEP_HEIGHT
	perspective_profile.default_zoom = 72.0
	perspective_profile.min_zoom = 24.0
	perspective_profile.max_zoom = 82.0
	camera.perspective_camera_profile = perspective_profile
	camera.projection_mode = BattleCameraProjectionProfile.ProjectionMode.PERSPECTIVE
	_host.add_child(camera)

	battlefield.camera = camera
	battlefield.background = _make_background_plane(100.0, 50.0)
	_host.add_child(battlefield.background)
	battlefield.include_startup_camera_footprint_in_bounds_x = false
	battlefield.include_startup_camera_footprint_in_bounds_z = true

	battlefield._configure_camera_bounds()

	assert_almost_eq(camera.captured_arena_bounds.position.x, -50.0, 0.001, "Arena min X should come from plane width")
	assert_almost_eq(camera.captured_arena_bounds.position.y, -25.0, 0.001, "Arena min Z should come from plane depth")
	assert_almost_eq(camera.captured_arena_bounds.size.x, 100.0, 0.001, "Arena width should match plane width")
	assert_almost_eq(camera.captured_arena_bounds.size.y, 50.0, 0.001, "Arena depth should match plane depth")

	assert_almost_eq(camera.captured_map_bounds.position.x, -50.0, 0.001, "X bounds should remain tight to arena when startup merge X is disabled")
	assert_almost_eq(camera.captured_map_bounds.size.x, 100.0, 0.001, "Merged map width should remain arena width")
	assert_almost_eq(camera.captured_map_bounds.position.y, -60.0, 0.001, "Z min should expand to include startup footprint when enabled")
	assert_almost_eq(camera.captured_map_bounds.size.y, 120.0, 0.001, "Z size should expand to include startup footprint when enabled")
	battlefield.free()


func test_startup_footprint_probe_restores_camera_state_after_sampling() -> void:
	var battlefield: BaseBattlefield3D = BaseBattlefield3D.new()
	var camera: _FakeCamera = _FakeCamera.new()
	var profile: BattleCameraProjectionProfile = BattleCameraProjectionProfile.new()
	profile.projection_mode = BattleCameraProjectionProfile.ProjectionMode.PERSPECTIVE
	profile.camera_transform = Transform3D.IDENTITY.translated(Vector3(1.0, 35.0, -50.0))
	profile.keep_aspect = Camera3D.KEEP_WIDTH
	profile.near_clip = 0.7
	profile.far_clip = 275.0
	profile.default_zoom = 64.0
	profile.min_zoom = 20.0
	profile.max_zoom = 80.0
	camera.perspective_camera_profile = profile
	camera.projection_mode = BattleCameraProjectionProfile.ProjectionMode.PERSPECTIVE
	camera.transform = Transform3D.IDENTITY.translated(Vector3(-9.0, 20.0, -15.0))
	camera.projection = Camera3D.PROJECTION_ORTHOGONAL
	camera.keep_aspect = Camera3D.KEEP_HEIGHT
	camera.fov = 42.0
	camera.size = 17.0
	camera.near = 0.2
	camera.far = 800.0
	_host.add_child(camera)

	battlefield.camera = camera

	var saved_transform: Transform3D = camera.transform
	var saved_projection: int = camera.projection
	var saved_keep_aspect: int = camera.keep_aspect
	var saved_fov: float = camera.fov
	var saved_size: float = camera.size
	var saved_near: float = camera.near
	var saved_far: float = camera.far

	var footprint: Rect2 = battlefield._get_startup_camera_footprint_bounds()
	assert_eq(footprint, camera.simulated_footprint, "Startup probe should return sampled camera footprint")

	assert_eq(camera.transform, saved_transform, "Startup probe should restore camera transform")
	assert_eq(camera.projection, saved_projection, "Startup probe should restore camera projection")
	assert_eq(camera.keep_aspect, saved_keep_aspect, "Startup probe should restore keep_aspect")
	assert_almost_eq(camera.fov, saved_fov, 0.001, "Startup probe should restore FOV")
	assert_almost_eq(camera.size, saved_size, 0.001, "Startup probe should restore ortho size")
	assert_almost_eq(camera.near, saved_near, 0.001, "Startup probe should restore near clip")
	assert_almost_eq(camera.far, saved_far, 0.001, "Startup probe should restore far clip")
	battlefield.free()
