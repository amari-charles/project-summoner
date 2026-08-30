extends Node3D
class_name AcademyCityGraybox

const COLOR_PATH: Color = Color(0.22, 0.22, 0.24, 1.0)
const COLOR_PLAZA: Color = Color(0.48, 0.48, 0.51, 1.0)
const COLOR_USABLE: Color = Color(0.82, 0.12, 0.12, 1.0)
const COLOR_BACKGROUND: Color = Color(0.94, 0.94, 0.96, 1.0)
const LABEL_OUTLINE: Color = Color(0.02, 0.02, 0.025, 0.95)

const USABLE_BUILDINGS: Array[Dictionary] = [
	{"name": "Campus Shop", "position": Vector3(-21, 0, 37), "size": Vector3(12, 4, 7)},
	{"name": "Competitive Arena", "position": Vector3(58, 0, 35), "size": Vector3(18, 5, 14)},
	{"name": "Dining Hall", "position": Vector3(35, 0, 8), "size": Vector3(14, 4, 9)},
	{"name": "Library / Archive", "position": Vector3(-34, 0, -38), "size": Vector3(15, 5, 10)},
	{"name": "Main Academy Hall", "position": Vector3(0, 0, -49), "size": Vector3(22, 6, 10)},
	{"name": "Restricted Old Hall", "position": Vector3(-65, 0, -48), "size": Vector3(11, 4, 8)},
]

const BACKGROUND_BUILDINGS: Array[Dictionary] = [
	{"name": "West Residences", "position": Vector3(-69, 0, 26), "size": Vector3(10, 4, 18)},
	{"name": "Workshop / Storage", "position": Vector3(-51, 0, 39), "size": Vector3(14, 3, 8)},
	{"name": "Service Buildings", "position": Vector3(-69, 0, 1), "size": Vector3(9, 3, 12)},
	{"name": "East Residences", "position": Vector3(69, 0, -4), "size": Vector3(10, 4, 20)},
	{"name": "North Residences", "position": Vector3(61, 0, -48), "size": Vector3(13, 4, 9)},
	{"name": "Academic Offices", "position": Vector3(24, 0, -49), "size": Vector3(8, 4, 9)},
]

const OUTDOOR_AREAS: Array[Dictionary] = [
	{"name": "Arrival Court", "position": Vector3(0, 0, 50), "size": Vector2(30, 14)},
	{"name": "Central Commons", "position": Vector3(0, 0, 8), "size": Vector2(34, 22)},
	{"name": "Shared Practice Ground", "position": Vector3(-44, 0, 20), "size": Vector2(30, 20)},
	{"name": "Fire Teacher Area", "position": Vector3(-43, 0, 4), "size": Vector2(20, 9)},
	{"name": "Earth Teacher Area", "position": Vector3(-52, 0, -12), "size": Vector2(24, 14)},
	{"name": "Water Teacher Area", "position": Vector3(43, 0, -17), "size": Vector2(26, 16)},
	{"name": "Wind Teacher Area", "position": Vector3(42, 0, -41), "size": Vector2(24, 14)},
]

const PATH_SEGMENTS: Array[Dictionary] = [
	# Grand Walk: arrival through the commons to the Main Hall.
	{"start": Vector3(0, 0, 57), "end": Vector3(0, 0, -41), "width": 7.0},
	# Arena Road branches before the social heart.
	{"start": Vector3(0, 0, 36), "end": Vector3(43, 0, 36), "width": 7.0},
	# West half of the academic loop.
	{"start": Vector3(-15, 0, 10), "end": Vector3(-31, 0, 17), "width": 5.0},
	{"start": Vector3(-31, 0, 17), "end": Vector3(-40, 0, -11), "width": 5.0},
	{"start": Vector3(-40, 0, -11), "end": Vector3(-34, 0, -28), "width": 5.0},
	{"start": Vector3(-34, 0, -28), "end": Vector3(-14, 0, -40), "width": 5.0},
	# East half of the academic loop and garden walk.
	{"start": Vector3(14, 0, -40), "end": Vector3(35, 0, -39), "width": 5.0},
	{"start": Vector3(35, 0, -39), "end": Vector3(36, 0, -17), "width": 5.0},
	{"start": Vector3(36, 0, -17), "end": Vector3(28, 0, 8), "width": 5.0},
	{"start": Vector3(28, 0, 8), "end": Vector3(15, 0, 10), "width": 5.0},
	# Workshop/service lane and restricted path.
	{"start": Vector3(-18, 0, 36), "end": Vector3(-42, 0, 36), "width": 4.0},
	{"start": Vector3(-42, 0, 36), "end": Vector3(-44, 0, 29), "width": 4.0},
	{"start": Vector3(-44, 0, -38), "end": Vector3(-56, 0, -46), "width": 3.5},
]


func _ready() -> void:
	_build_paths()
	_build_outdoor_areas()
	_build_buildings(USABLE_BUILDINGS, true)
	_build_buildings(BACKGROUND_BUILDINGS, false)


func _build_paths() -> void:
	for index: int in PATH_SEGMENTS.size():
		var segment: Dictionary = PATH_SEGMENTS[index]
		_add_path(
			"Path%02d" % index,
			SafeTypeUtils.vector3(segment.get("start")),
			SafeTypeUtils.vector3(segment.get("end")),
			SafeTypeUtils.float_val(segment.get("width"), 5.0)
		)


func _build_outdoor_areas() -> void:
	for area: Dictionary in OUTDOOR_AREAS:
		var position: Vector3 = SafeTypeUtils.vector3(area.get("position"))
		var size: Vector2 = area.get("size", Vector2(12, 8)) as Vector2
		var pad: MeshInstance3D = MeshInstance3D.new()
		pad.name = SafeTypeUtils.string(area.get("name")).replace(" ", "")
		var mesh: BoxMesh = BoxMesh.new()
		mesh.size = Vector3(size.x, 0.08, size.y)
		mesh.material = _material(COLOR_PLAZA)
		pad.mesh = mesh
		pad.position = position + Vector3.UP * 0.05
		add_child(pad)
		_add_label(pad, SafeTypeUtils.string(area.get("name")), Vector3(0, 0.2, 0))


func _build_buildings(definitions: Array[Dictionary], usable: bool) -> void:
	for definition: Dictionary in definitions:
		var building_name: String = SafeTypeUtils.string(definition.get("name"))
		var size: Vector3 = SafeTypeUtils.vector3(definition.get("size"), Vector3(12, 6, 10))
		var root: StaticBody3D = StaticBody3D.new()
		root.name = building_name.replace(" ", "").replace("/", "")
		root.position = SafeTypeUtils.vector3(definition.get("position"))
		root.collision_layer = 4
		root.collision_mask = 0
		root.set_meta("usable", usable)
		add_child(root)

		var visual: MeshInstance3D = MeshInstance3D.new()
		visual.name = "Block"
		var mesh: BoxMesh = BoxMesh.new()
		mesh.size = size
		mesh.material = _material(COLOR_USABLE if usable else COLOR_BACKGROUND)
		visual.mesh = mesh
		visual.position.y = size.y * 0.5
		root.add_child(visual)

		var collision: CollisionShape3D = CollisionShape3D.new()
		collision.name = "CollisionShape3D"
		var shape: BoxShape3D = BoxShape3D.new()
		shape.size = size
		collision.shape = shape
		collision.position.y = size.y * 0.5
		root.add_child(collision)

		_add_label(root, building_name, Vector3(0, size.y + 0.8, 0))


func _add_path(path_name: String, start: Vector3, end: Vector3, width: float) -> void:
	var delta: Vector3 = end - start
	var length: float = Vector2(delta.x, delta.z).length()
	if length <= 0.01:
		return
	var path: MeshInstance3D = MeshInstance3D.new()
	path.name = path_name
	var mesh: BoxMesh = BoxMesh.new()
	mesh.size = Vector3(width, 0.06, length)
	mesh.material = _material(COLOR_PATH)
	path.mesh = mesh
	path.position = (start + end) * 0.5 + Vector3.UP * 0.035
	path.rotation.y = atan2(delta.x, delta.z)
	add_child(path)


func _add_label(parent: Node3D, text: String, offset: Vector3) -> void:
	var label: Label3D = Label3D.new()
	label.name = "Label"
	label.text = text
	label.position = offset
	label.pixel_size = 0.01
	label.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	label.outline_size = 8
	label.outline_modulate = LABEL_OUTLINE
	label.font_size = 26
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	parent.add_child(label)


func _material(color: Color) -> StandardMaterial3D:
	var material: StandardMaterial3D = StandardMaterial3D.new()
	material.albedo_color = color
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	return material
