extends Node3D
class_name ObjectivePathTrail

const WORLD_MIN: Vector2 = Vector2(-80.0, -60.0)
const WORLD_MAX: Vector2 = Vector2(80.0, 60.0)
const CELL_SIZE: float = 2.5
const TRAIL_SPACING: float = 2.6
const OBSTACLE_PADDING: float = 1.8
const MAX_WISPS: int = 64
const REPATH_INTERVAL: float = 0.35
const REPATH_DISTANCE: float = 1.5
const WISP_COLOR: Color = Color(0.32, 0.86, 1.0, 0.92)

var _player: Node3D = null
var _target_position: Vector3 = Vector3.ZERO
var _has_target: bool = false
var _grid: AStarGrid2D = null
var _wisps: Array[MeshInstance3D] = []
var _last_path_origin: Vector3 = Vector3(INF, INF, INF)
var _repath_time: float = 0.0
var _animation_time: float = 0.0


func _ready() -> void:
	process_mode = Node.PROCESS_MODE_PAUSABLE


func configure(player_node: Node3D, obstacle_definitions: Array[Dictionary]) -> void:
	_player = player_node
	_build_grid(obstacle_definitions)
	_rebuild_path()


func set_target(target_position: Variant) -> void:
	if not target_position is Vector3:
		_has_target = false
		_set_visible_wisp_count(0)
		return
	var next_target: Vector3 = target_position as Vector3
	var changed: bool = not _has_target or not next_target.is_equal_approx(_target_position)
	_target_position = next_target
	_has_target = true
	if changed:
		_rebuild_path()


func _process(delta: float) -> void:
	_animation_time += delta
	_repath_time += delta
	if (
		_has_target
		and is_instance_valid(_player)
		and _repath_time >= REPATH_INTERVAL
		and _player.global_position.distance_to(_last_path_origin) >= REPATH_DISTANCE
	):
		_rebuild_path()
	_animate_wisps()


func build_route(start: Vector3, destination: Vector3) -> PackedVector3Array:
	var route: PackedVector3Array = PackedVector3Array()
	if _grid == null:
		return route
	var start_id: Vector2i = _nearest_walkable_id(_world_to_grid(start))
	var end_id: Vector2i = _nearest_walkable_id(_world_to_grid(destination))
	var id_path: Array[Vector2i] = _grid.get_id_path(start_id, end_id)
	if id_path.is_empty():
		return route
	route.append(Vector3(start.x, 0.16, start.z))
	for id: Vector2i in id_path:
		var point: Vector3 = _grid_to_world(id)
		if route[-1].distance_to(point) >= CELL_SIZE * 0.75:
			route.append(point)
	route.append(Vector3(destination.x, 0.16, destination.z))
	return route


func _build_grid(obstacle_definitions: Array[Dictionary]) -> void:
	_grid = AStarGrid2D.new()
	var grid_size: Vector2i = Vector2i(
		ceili((WORLD_MAX.x - WORLD_MIN.x) / CELL_SIZE) + 1,
		ceili((WORLD_MAX.y - WORLD_MIN.y) / CELL_SIZE) + 1
	)
	_grid.region = Rect2i(Vector2i.ZERO, grid_size)
	_grid.cell_size = Vector2(CELL_SIZE, CELL_SIZE)
	_grid.diagonal_mode = AStarGrid2D.DIAGONAL_MODE_ONLY_IF_NO_OBSTACLES
	_grid.update()
	for definition: Dictionary in obstacle_definitions:
		var center: Vector3 = SafeTypeUtils.vector3(definition.get("position"))
		var size: Vector3 = SafeTypeUtils.vector3(
			definition.get("size"), Vector3(10.0, 4.0, 8.0)
		)
		var half_extent: Vector2 = Vector2(size.x, size.z) * 0.5 + Vector2.ONE * OBSTACLE_PADDING
		for x: int in grid_size.x:
			for y: int in grid_size.y:
				var world: Vector3 = _grid_to_world(Vector2i(x, y))
				if (
					absf(world.x - center.x) <= half_extent.x
					and absf(world.z - center.z) <= half_extent.y
				):
					_grid.set_point_solid(Vector2i(x, y), true)


func _rebuild_path() -> void:
	_repath_time = 0.0
	if not _has_target or not is_instance_valid(_player) or _grid == null:
		_set_visible_wisp_count(0)
		return
	_last_path_origin = _player.global_position
	var route: PackedVector3Array = build_route(_last_path_origin, _target_position)
	var samples: PackedVector3Array = _sample_route(route)
	_set_visible_wisp_count(samples.size())
	for index: int in samples.size():
		_wisps[index].global_position = samples[index]


func _sample_route(route: PackedVector3Array) -> PackedVector3Array:
	var samples: PackedVector3Array = PackedVector3Array()
	if route.size() < 2:
		return samples
	var distance_until_sample: float = TRAIL_SPACING
	for segment_index: int in range(route.size() - 1):
		var start: Vector3 = route[segment_index]
		var finish: Vector3 = route[segment_index + 1]
		var segment: Vector3 = finish - start
		var length: float = segment.length()
		if length <= 0.001:
			continue
		var direction: Vector3 = segment / length
		while distance_until_sample <= length and samples.size() < MAX_WISPS:
			samples.append(start + direction * distance_until_sample)
			distance_until_sample += TRAIL_SPACING
		distance_until_sample -= length
	return samples


func _set_visible_wisp_count(count: int) -> void:
	while _wisps.size() < count:
		var wisp: MeshInstance3D = _create_wisp()
		_wisps.append(wisp)
		add_child(wisp)
	for index: int in _wisps.size():
		_wisps[index].visible = index < count


func _create_wisp() -> MeshInstance3D:
	var wisp: MeshInstance3D = MeshInstance3D.new()
	var mesh: SphereMesh = SphereMesh.new()
	mesh.radius = 0.16
	mesh.height = 0.32
	mesh.radial_segments = 12
	mesh.rings = 6
	mesh.material = _wisp_material()
	wisp.mesh = mesh
	wisp.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	return wisp


func _wisp_material() -> StandardMaterial3D:
	var material: StandardMaterial3D = StandardMaterial3D.new()
	material.albedo_color = WISP_COLOR
	material.emission_enabled = true
	material.emission = WISP_COLOR
	material.emission_energy_multiplier = 3.2
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	return material


func _animate_wisps() -> void:
	for index: int in _wisps.size():
		var wisp: MeshInstance3D = _wisps[index]
		if not wisp.visible:
			continue
		var phase: float = _animation_time * 3.2 - index * 0.48
		var pulse: float = (sin(phase) + 1.0) * 0.5
		wisp.position.y = 0.16 + pulse * 0.22
		wisp.scale = Vector3.ONE * lerpf(0.72, 1.25, pulse)
		wisp.transparency = lerpf(0.42, 0.02, pulse)


func _world_to_grid(world: Vector3) -> Vector2i:
	return Vector2i(
		clampi(roundi((world.x - WORLD_MIN.x) / CELL_SIZE), 0, _grid.region.size.x - 1),
		clampi(roundi((world.z - WORLD_MIN.y) / CELL_SIZE), 0, _grid.region.size.y - 1)
	)


func _grid_to_world(id: Vector2i) -> Vector3:
	return Vector3(
		WORLD_MIN.x + id.x * CELL_SIZE,
		0.16,
		WORLD_MIN.y + id.y * CELL_SIZE
	)


func _nearest_walkable_id(origin: Vector2i) -> Vector2i:
	if not _grid.is_point_solid(origin):
		return origin
	for radius: int in range(1, 10):
		for x: int in range(origin.x - radius, origin.x + radius + 1):
			for y: int in range(origin.y - radius, origin.y + radius + 1):
				var candidate: Vector2i = Vector2i(x, y)
				if _grid.is_in_boundsv(candidate) and not _grid.is_point_solid(candidate):
					return candidate
	return origin
