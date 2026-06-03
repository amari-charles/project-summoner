extends Node3D
class_name SpellPreview

## Visual preview for spell targeting during card drag.
## Draws the same area shape the simulation will use.

const VALID_COLOR: Color = Color(0.25, 0.82, 1.0, 0.42)
const VALID_EDGE_COLOR: Color = Color(0.55, 0.95, 1.0, 0.9)
const INVALID_COLOR: Color = Color(1.0, 0.25, 0.25, 0.42)
const INVALID_EDGE_COLOR: Color = Color(1.0, 0.45, 0.45, 0.9)
const GROUND_Y: float = 0.08
const EDGE_HEIGHT: float = 0.06
const DEFAULT_RADIUS: float = 5.0
const DEFAULT_LINE_WIDTH: float = 2.5
const CROSSHAIR_LENGTH: float = 1.5
const CROSSHAIR_WIDTH: float = 0.08
const CENTER_DOT_RADIUS: float = 0.18
const BOUNDARY_WIDTH: float = 0.16

var _spell_radius: float = DEFAULT_RADIUS
var _shape: String = "circle"
var _line_width: float = DEFAULT_LINE_WIDTH
var _element: String = "neutral"
var _is_valid: bool = true
var _fill_nodes: Array[MeshInstance3D] = []
var _edge_nodes: Array[MeshInstance3D] = []
var _all_materials: Array[StandardMaterial3D] = []


func setup(spell_radius: float = DEFAULT_RADIUS, shape: String = "circle", line_width: float = DEFAULT_LINE_WIDTH, element: String = "neutral") -> void:
	_spell_radius = maxf(0.0, spell_radius)
	_shape = shape
	_line_width = maxf(0.2, line_width)
	_element = element
	_rebuild()


func update_position(pos: Vector3) -> void:
	if _shape != "line":
		global_position = pos


func update_points(source: Vector3, target: Vector3) -> void:
	if _shape == "line":
		_update_line(source, target)
	elif _shape == "single_target":
		global_position = target


func set_valid(is_valid: bool) -> void:
	if _is_valid == is_valid:
		return
	_is_valid = is_valid
	_apply_colors()


func cleanup() -> void:
	queue_free()


func _rebuild() -> void:
	for child: Node in get_children():
		child.queue_free()
	_fill_nodes.clear()
	_edge_nodes.clear()
	_all_materials.clear()

	match _shape:
		"square":
			_create_square(_spell_radius)
		"line":
			_create_line(maxf(_spell_radius, 0.1), _line_width)
		"single_target":
			_create_single_target()
		"cone":
			_create_circle(_spell_radius)
		_:
			_create_circle(_spell_radius)

	_apply_colors()


func _create_circle(radius: float) -> void:
	if radius <= 0.0:
		_create_single_target()
		return

	var fill: MeshInstance3D = _mesh_instance("CircleFill")
	var fill_mesh: CylinderMesh = CylinderMesh.new()
	fill_mesh.top_radius = 1.0
	fill_mesh.bottom_radius = 1.0
	fill_mesh.height = 0.025
	fill.mesh = fill_mesh
	fill.scale = Vector3(radius, 1.0, radius)
	fill.position.y = GROUND_Y
	fill.material_override = _new_material(true)
	add_child(fill)
	_fill_nodes.append(fill)

	_create_crosshair()


func _create_square(radius: float) -> void:
	if radius <= 0.0:
		_create_single_target()
		return

	var fill: MeshInstance3D = _mesh_instance("SquareFill")
	var fill_mesh: BoxMesh = BoxMesh.new()
	fill_mesh.size = Vector3(radius * 2.0, 0.025, radius * 2.0)
	fill.mesh = fill_mesh
	fill.position.y = GROUND_Y
	fill.material_override = _new_material(true)
	add_child(fill)
	_fill_nodes.append(fill)

	_add_edge_box("NorthEdge", Vector3(radius * 2.0, BOUNDARY_WIDTH, BOUNDARY_WIDTH), Vector3(0.0, GROUND_Y + EDGE_HEIGHT, -radius))
	_add_edge_box("SouthEdge", Vector3(radius * 2.0, BOUNDARY_WIDTH, BOUNDARY_WIDTH), Vector3(0.0, GROUND_Y + EDGE_HEIGHT, radius))
	_add_edge_box("WestEdge", Vector3(BOUNDARY_WIDTH, BOUNDARY_WIDTH, radius * 2.0), Vector3(-radius, GROUND_Y + EDGE_HEIGHT, 0.0))
	_add_edge_box("EastEdge", Vector3(BOUNDARY_WIDTH, BOUNDARY_WIDTH, radius * 2.0), Vector3(radius, GROUND_Y + EDGE_HEIGHT, 0.0))
	_create_crosshair()


func _create_line(length: float, width: float) -> void:
	var fill: MeshInstance3D = _mesh_instance("LineFill")
	var fill_mesh: BoxMesh = BoxMesh.new()
	fill_mesh.size = Vector3(width, 0.025, length)
	fill.mesh = fill_mesh
	fill.position = Vector3(0.0, GROUND_Y, length * 0.5)
	fill.material_override = _new_material(true)
	add_child(fill)
	_fill_nodes.append(fill)

	var edge_left: MeshInstance3D = _edge_box("LineLeft", Vector3(BOUNDARY_WIDTH, BOUNDARY_WIDTH, length))
	edge_left.position = Vector3(-width * 0.5, GROUND_Y + EDGE_HEIGHT, length * 0.5)
	add_child(edge_left)
	_edge_nodes.append(edge_left)

	var edge_right: MeshInstance3D = _edge_box("LineRight", Vector3(BOUNDARY_WIDTH, BOUNDARY_WIDTH, length))
	edge_right.position = Vector3(width * 0.5, GROUND_Y + EDGE_HEIGHT, length * 0.5)
	add_child(edge_right)
	_edge_nodes.append(edge_right)

	var edge_end: MeshInstance3D = _edge_box("LineEnd", Vector3(width, BOUNDARY_WIDTH, BOUNDARY_WIDTH))
	edge_end.position = Vector3(0.0, GROUND_Y + EDGE_HEIGHT, length)
	add_child(edge_end)
	_edge_nodes.append(edge_end)


func _create_single_target() -> void:
	_create_crosshair()

	var dot: MeshInstance3D = _mesh_instance("TargetDot")
	var sphere: SphereMesh = SphereMesh.new()
	sphere.radius = CENTER_DOT_RADIUS
	sphere.height = CENTER_DOT_RADIUS * 2.0
	dot.mesh = sphere
	dot.position.y = GROUND_Y + 0.15
	dot.material_override = _new_material(false)
	add_child(dot)
	_edge_nodes.append(dot)


func _create_crosshair() -> void:
	_add_edge_box("CrosshairX", Vector3(CROSSHAIR_LENGTH, CROSSHAIR_WIDTH, CROSSHAIR_WIDTH), Vector3(0.0, GROUND_Y + EDGE_HEIGHT + 0.03, 0.0))
	_add_edge_box("CrosshairZ", Vector3(CROSSHAIR_WIDTH, CROSSHAIR_WIDTH, CROSSHAIR_LENGTH), Vector3(0.0, GROUND_Y + EDGE_HEIGHT + 0.03, 0.0))


func _update_line(source: Vector3, target: Vector3) -> void:
	var start: Vector3 = source
	if start == Vector3.ZERO:
		start = target
	var delta: Vector3 = target - start
	delta.y = 0.0
	var length: float = maxf(_spell_radius, delta.length())
	if delta.length_squared() <= 0.0001:
		delta = Vector3.RIGHT * length

	global_position = start
	rotation = Vector3.ZERO
	rotation.y = atan2(delta.x, delta.z)

	for fill: MeshInstance3D in _fill_nodes:
		if fill.mesh is BoxMesh:
			var fill_mesh: BoxMesh = fill.mesh
			fill_mesh.size = Vector3(_line_width, 0.025, length)
			fill.position = Vector3(0.0, GROUND_Y, length * 0.5)

	for edge: MeshInstance3D in _edge_nodes:
		if not edge.mesh is BoxMesh:
			continue
		var edge_mesh: BoxMesh = edge.mesh
		match edge.name:
			"LineLeft":
				edge_mesh.size = Vector3(BOUNDARY_WIDTH, BOUNDARY_WIDTH, length)
				edge.position = Vector3(-_line_width * 0.5, GROUND_Y + EDGE_HEIGHT, length * 0.5)
			"LineRight":
				edge_mesh.size = Vector3(BOUNDARY_WIDTH, BOUNDARY_WIDTH, length)
				edge.position = Vector3(_line_width * 0.5, GROUND_Y + EDGE_HEIGHT, length * 0.5)
			"LineEnd":
				edge_mesh.size = Vector3(_line_width, BOUNDARY_WIDTH, BOUNDARY_WIDTH)
				edge.position = Vector3(0.0, GROUND_Y + EDGE_HEIGHT, length)


func _add_edge_box(node_name: String, size: Vector3, pos: Vector3) -> void:
	var edge: MeshInstance3D = _edge_box(node_name, size)
	edge.position = pos
	add_child(edge)
	_edge_nodes.append(edge)


func _edge_box(node_name: String, size: Vector3) -> MeshInstance3D:
	var edge: MeshInstance3D = _mesh_instance(node_name)
	var mesh: BoxMesh = BoxMesh.new()
	mesh.size = size
	edge.mesh = mesh
	edge.material_override = _new_material(false)
	return edge


func _mesh_instance(node_name: String) -> MeshInstance3D:
	var mesh_instance: MeshInstance3D = MeshInstance3D.new()
	mesh_instance.name = node_name
	mesh_instance.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	return mesh_instance


func _new_material(is_fill: bool) -> StandardMaterial3D:
	var material: StandardMaterial3D = StandardMaterial3D.new()
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	material.cull_mode = BaseMaterial3D.CULL_DISABLED
	material.no_depth_test = true
	_all_materials.append(material)
	return material


func _apply_colors() -> void:
	var fill_color: Color = VALID_COLOR if _is_valid else INVALID_COLOR
	var edge_color: Color = VALID_EDGE_COLOR if _is_valid else INVALID_EDGE_COLOR

	for fill: MeshInstance3D in _fill_nodes:
		if fill.material_override is StandardMaterial3D:
			var fill_material: StandardMaterial3D = fill.material_override
			fill_material.albedo_color = fill_color
			fill_material.emission_enabled = true
			fill_material.emission = fill_color
			fill_material.emission_energy_multiplier = 0.25

	for edge: MeshInstance3D in _edge_nodes:
		if edge.material_override is StandardMaterial3D:
			var edge_material: StandardMaterial3D = edge.material_override
			edge_material.albedo_color = edge_color
			edge_material.emission_enabled = true
			edge_material.emission = edge_color
			edge_material.emission_energy_multiplier = 0.8
