extends VFXInstance
class_name SpellAreaVFX

@export var default_radius: float = 6.0
@export var default_duration: float = 0.75

const GROUND_Y: float = 0.08
const EDGE_Y: float = 0.16
const BOUNDARY_WIDTH: float = 0.18

var _radius: float = 6.0
var _duration: float = 0.75
var _shape: String = "circle"
var _element: String = "neutral"
var _card_id: String = ""
var _mode: String = "field"
var _fill: MeshInstance3D = null
var _boundary_nodes: Array[MeshInstance3D] = []
var _vortex_nodes: Array[MeshInstance3D] = []
var _tween: Tween = null


func _ready() -> void:
	lifetime = 0.0
	super._ready()


func receive_data(data: Dictionary) -> void:
	_radius = default_radius
	_duration = default_duration
	_shape = "circle"
	_element = "neutral"
	_card_id = ""
	_mode = "field"

	if data.has("radius"):
		_radius = _float_from_variant(data["radius"], default_radius)
	if data.has("duration"):
		_duration = _float_from_variant(data["duration"], default_duration)
	if data.has("shape"):
		_shape = str(data["shape"])
	if data.has("element"):
		_element = str(data["element"])
	if data.has("card_id"):
		_card_id = str(data["card_id"])
	if data.has("mode"):
		_mode = str(data["mode"])

	_radius = maxf(0.35, _radius)
	_duration = maxf(0.25, _duration)


func _on_play() -> void:
	_clear_nodes()
	var palette: Dictionary = _palette()
	var fill_color: Color = palette["fill"]
	var edge_color: Color = palette["edge"]

	if _shape == "square":
		_create_square(fill_color, edge_color)
	else:
		_create_circle(fill_color, edge_color)
	if _card_id == "tornado":
		_create_tornado_column(fill_color, edge_color)

	if _tween:
		_tween.kill()
	_tween = create_tween()

	if _mode == "burst" or _mode == "pulse" or _duration <= 1.1:
		_play_burst(fill_color, edge_color)
	else:
		_play_field(edge_color)


func _create_circle(fill_color: Color, _edge_color: Color) -> void:
	_fill = _mesh_instance("AreaFill")
	var fill_mesh: CylinderMesh = CylinderMesh.new()
	fill_mesh.top_radius = 1.0
	fill_mesh.bottom_radius = 1.0
	fill_mesh.height = 0.035
	_fill.mesh = fill_mesh
	_fill.scale = Vector3(_radius, 1.0, _radius)
	_fill.position.y = GROUND_Y
	_fill.material_override = _material(fill_color, 0.25)
	add_child(_fill)


func _create_square(fill_color: Color, edge_color: Color) -> void:
	_fill = _mesh_instance("AreaFill")
	var fill_mesh: BoxMesh = BoxMesh.new()
	fill_mesh.size = Vector3(_radius * 2.0, 0.035, _radius * 2.0)
	_fill.mesh = fill_mesh
	_fill.position.y = GROUND_Y
	_fill.material_override = _material(fill_color, 0.23)
	add_child(_fill)

	_add_boundary_box("NorthBoundary", Vector3(_radius * 2.0, BOUNDARY_WIDTH, BOUNDARY_WIDTH), Vector3(0.0, EDGE_Y, -_radius), edge_color)
	_add_boundary_box("SouthBoundary", Vector3(_radius * 2.0, BOUNDARY_WIDTH, BOUNDARY_WIDTH), Vector3(0.0, EDGE_Y, _radius), edge_color)
	_add_boundary_box("WestBoundary", Vector3(BOUNDARY_WIDTH, BOUNDARY_WIDTH, _radius * 2.0), Vector3(-_radius, EDGE_Y, 0.0), edge_color)
	_add_boundary_box("EastBoundary", Vector3(BOUNDARY_WIDTH, BOUNDARY_WIDTH, _radius * 2.0), Vector3(_radius, EDGE_Y, 0.0), edge_color)


func _play_burst(_fill_color: Color, _edge_color: Color) -> void:
	if _fill:
		_fill.scale = _fill.scale * 0.25
	for boundary: MeshInstance3D in _boundary_nodes:
		boundary.scale = Vector3.ONE * 0.25

	_tween.set_parallel(true)
	if _fill:
		var fill_target: Vector3 = Vector3(_radius, 1.0, _radius) if _shape == "circle" else Vector3.ONE
		_tween.tween_property(_fill, "scale", fill_target, 0.18)
	for boundary: MeshInstance3D in _boundary_nodes:
		_tween.tween_property(boundary, "scale", Vector3.ONE, 0.18)
	for vortex: MeshInstance3D in _vortex_nodes:
		_tween.tween_property(vortex, "scale", vortex.scale, 0.18)
	_tween.set_parallel(false)
	_tween.tween_interval(_duration)
	_tween.tween_callback(stop)


func _play_field(edge_color: Color) -> void:
	var cleanup: SceneTreeTimer = get_tree().create_timer(_duration)
	cleanup.timeout.connect(func() -> void:
		if is_instance_valid(self) and is_playing:
			stop()
	)

	for boundary: MeshInstance3D in _boundary_nodes:
		if boundary.material_override is StandardMaterial3D:
			var material: StandardMaterial3D = boundary.material_override
			material.albedo_color = edge_color

	if _vortex_nodes.size() > 0:
		_tween.set_parallel(true)
		for vortex: MeshInstance3D in _vortex_nodes:
			_tween.tween_property(vortex, "rotation_degrees:y", vortex.rotation_degrees.y + 1440.0, _duration)
		_tween.set_parallel(false)


func _add_boundary_box(node_name: String, size: Vector3, pos: Vector3, edge_color: Color) -> void:
	var boundary: MeshInstance3D = _mesh_instance(node_name)
	var mesh: BoxMesh = BoxMesh.new()
	mesh.size = size
	boundary.mesh = mesh
	boundary.position = pos
	boundary.material_override = _material(edge_color, 0.85)
	add_child(boundary)
	_boundary_nodes.append(boundary)


func _create_tornado_column(fill_color: Color, edge_color: Color) -> void:
	var column: MeshInstance3D = _mesh_instance("TornadoColumn")
	var column_mesh: CylinderMesh = CylinderMesh.new()
	column_mesh.bottom_radius = maxf(0.75, _radius * 0.18)
	column_mesh.top_radius = maxf(1.8, _radius * 0.52)
	column_mesh.height = 6.6
	column.mesh = column_mesh
	column.position.y = 3.3
	column.material_override = _material(Color(fill_color.r, fill_color.g, fill_color.b, 0.18), 0.18)
	add_child(column)
	_vortex_nodes.append(column)

	var i: int = 0
	while i < 3:
		var ribbon: MeshInstance3D = _mesh_instance("TornadoRibbon%d" % i)
		var mesh: BoxMesh = BoxMesh.new()
		mesh.size = Vector3(0.16, 5.8, maxf(1.2, _radius * 0.55))
		ribbon.mesh = mesh
		ribbon.position.y = 3.0
		ribbon.rotation_degrees.y = float(i) * 120.0
		ribbon.material_override = _material(Color(edge_color.r, edge_color.g, edge_color.b, 0.32), 0.55)
		add_child(ribbon)
		_vortex_nodes.append(ribbon)
		i += 1


func _palette() -> Dictionary:
	match _element:
		"fire":
			return {
				"fill": Color(1.0, 0.28, 0.05, 0.34),
				"edge": Color(1.0, 0.55, 0.12, 0.9),
			}
		"water":
			return {
				"fill": Color(0.18, 0.62, 1.0, 0.30),
				"edge": Color(0.48, 0.9, 1.0, 0.9),
			}
		"earth":
			return {
				"fill": Color(0.42, 0.72, 0.36, 0.32),
				"edge": Color(0.72, 0.9, 0.46, 0.9),
			}
		"wind":
			return {
				"fill": Color(0.78, 0.96, 0.95, 0.25),
				"edge": Color(0.92, 1.0, 0.98, 0.86),
			}
		_:
			return {
				"fill": Color(0.62, 0.74, 1.0, 0.28),
				"edge": Color(0.75, 0.86, 1.0, 0.85),
			}


func _material(color: Color, emission_energy: float) -> StandardMaterial3D:
	var material: StandardMaterial3D = StandardMaterial3D.new()
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	material.cull_mode = BaseMaterial3D.CULL_DISABLED
	material.no_depth_test = true
	material.albedo_color = color
	material.emission_enabled = true
	material.emission = Color(color.r, color.g, color.b, 1.0)
	material.emission_energy_multiplier = emission_energy
	return material


func _mesh_instance(node_name: String) -> MeshInstance3D:
	var mesh_instance: MeshInstance3D = MeshInstance3D.new()
	mesh_instance.name = node_name
	mesh_instance.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	return mesh_instance


func _float_from_variant(value: Variant, fallback: float) -> float:
	if value is float:
		return float(value)
	if value is int:
		return float(value)
	return fallback


func _clear_nodes() -> void:
	if _tween:
		_tween.kill()
		_tween = null
	for child: Node in get_children():
		child.queue_free()
	_fill = null
	_boundary_nodes.clear()
	_vortex_nodes.clear()


func _on_reset() -> void:
	_clear_nodes()
