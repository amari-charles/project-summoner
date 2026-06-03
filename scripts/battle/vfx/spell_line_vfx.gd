extends VFXInstance
class_name SpellLineVFX

@export var default_length: float = 10.0
@export var default_width: float = 2.5
@export var default_duration: float = 0.45

const BEAM_Y: float = 0.28
const EDGE_Y: float = 0.34

var _length: float = 10.0
var _width: float = 2.5
var _duration: float = 0.45
var _element: String = "wind"
var _source_position: Vector3 = Vector3.ZERO
var _target_position: Vector3 = Vector3.RIGHT * 10.0
var _beam: MeshInstance3D = null
var _left_edge: MeshInstance3D = null
var _right_edge: MeshInstance3D = null
var _tween: Tween = null


func _ready() -> void:
	lifetime = 0.0
	super._ready()


func receive_data(data: Dictionary) -> void:
	_length = default_length
	_width = default_width
	_duration = default_duration
	_element = "wind"

	if data.has("radius"):
		_length = _float_from_variant(data["radius"], default_length)
	if data.has("line_width"):
		_width = _float_from_variant(data["line_width"], default_width)
	if data.has("duration"):
		_duration = maxf(0.25, _float_from_variant(data["duration"], default_duration))
	if data.has("element"):
		_element = str(data["element"])
	if data.has("source_position") and data["source_position"] is Vector3:
		_source_position = data["source_position"]
	if data.has("target_position") and data["target_position"] is Vector3:
		_target_position = data["target_position"]

	var delta: Vector3 = _target_position - _source_position
	delta.y = 0.0
	if delta.length() > 0.05:
		_length = maxf(_length, delta.length())


func _on_play() -> void:
	_clear_nodes()
	global_position = _source_position

	var delta: Vector3 = _target_position - _source_position
	delta.y = 0.0
	if delta.length_squared() <= 0.0001:
		delta = Vector3.RIGHT * _length
	rotation = Vector3.ZERO
	rotation.y = atan2(delta.x, delta.z)

	var palette: Dictionary = _palette()
	var fill_color: Color = palette["fill"]
	var edge_color: Color = palette["edge"]

	_beam = _box("LineBeam", Vector3(_width, 0.05, _length), fill_color, 0.45)
	_beam.position = Vector3(0.0, BEAM_Y, _length * 0.5)
	_beam.scale = Vector3(0.15, 1.0, 0.08)
	add_child(_beam)

	_left_edge = _box("LineLeftEdge", Vector3(0.16, 0.08, _length), edge_color, 0.9)
	_left_edge.position = Vector3(-_width * 0.5, EDGE_Y, _length * 0.5)
	_left_edge.scale = Vector3(1.0, 1.0, 0.08)
	add_child(_left_edge)

	_right_edge = _box("LineRightEdge", Vector3(0.16, 0.08, _length), edge_color, 0.9)
	_right_edge.position = Vector3(_width * 0.5, EDGE_Y, _length * 0.5)
	_right_edge.scale = Vector3(1.0, 1.0, 0.08)
	add_child(_right_edge)

	if _tween:
		_tween.kill()
	_tween = create_tween()
	_tween.set_parallel(true)
	_tween.tween_property(_beam, "scale", Vector3.ONE, _duration * 0.45)
	_tween.tween_property(_left_edge, "scale", Vector3.ONE, _duration * 0.45)
	_tween.tween_property(_right_edge, "scale", Vector3.ONE, _duration * 0.45)
	_tween.set_parallel(false)
	_tween.tween_interval(_duration * 0.55)
	_tween.tween_callback(stop)


func _box(node_name: String, size: Vector3, color: Color, emission_energy: float) -> MeshInstance3D:
	var node: MeshInstance3D = MeshInstance3D.new()
	node.name = node_name
	node.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	var mesh: BoxMesh = BoxMesh.new()
	mesh.size = size
	node.mesh = mesh
	node.material_override = _material(color, emission_energy)
	return node


func _palette() -> Dictionary:
	if _element == "fire":
		return {"fill": Color(1.0, 0.35, 0.08, 0.34), "edge": Color(1.0, 0.68, 0.18, 0.9)}
	if _element == "water":
		return {"fill": Color(0.22, 0.66, 1.0, 0.34), "edge": Color(0.55, 0.92, 1.0, 0.9)}
	if _element == "earth":
		return {"fill": Color(0.48, 0.74, 0.36, 0.34), "edge": Color(0.82, 0.94, 0.48, 0.9)}
	return {"fill": Color(0.8, 0.98, 1.0, 0.30), "edge": Color(0.96, 1.0, 1.0, 0.9)}


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
	_beam = null
	_left_edge = null
	_right_edge = null


func _on_reset() -> void:
	_clear_nodes()
