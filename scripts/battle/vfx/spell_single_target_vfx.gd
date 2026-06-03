extends VFXInstance
class_name SpellSingleTargetVFX

@export var default_duration: float = 0.55

const MARK_Y: float = 1.1

var _duration: float = 0.55
var _element: String = "neutral"
var _source_position: Vector3 = Vector3.ZERO
var _target_position: Vector3 = Vector3.ZERO
var _marker: MeshInstance3D = null
var _beam: MeshInstance3D = null
var _tween: Tween = null


func _ready() -> void:
	lifetime = 0.0
	super._ready()


func receive_data(data: Dictionary) -> void:
	_duration = default_duration
	_element = "neutral"
	_source_position = Vector3.ZERO
	_target_position = global_position

	if data.has("duration"):
		_duration = maxf(0.25, _float_from_variant(data["duration"], default_duration))
	if data.has("element"):
		_element = str(data["element"])
	if data.has("source_position") and data["source_position"] is Vector3:
		_source_position = data["source_position"]
	if data.has("target_position") and data["target_position"] is Vector3:
		_target_position = data["target_position"]


func _on_play() -> void:
	_clear_nodes()
	global_position = _target_position

	var palette: Dictionary = _palette()
	var edge_color: Color = palette["edge"]
	var fill_color: Color = palette["fill"]

	_marker = MeshInstance3D.new()
	_marker.name = "TargetMarker"
	_marker.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	var sphere: SphereMesh = SphereMesh.new()
	sphere.radius = 0.35
	sphere.height = 0.7
	_marker.mesh = sphere
	_marker.position.y = MARK_Y
	_marker.material_override = _material(fill_color, 0.7)
	add_child(_marker)

	_create_beam(edge_color)

	if _tween:
		_tween.kill()
	_tween = create_tween()
	_tween.set_parallel(true)
	_tween.tween_property(_marker, "scale", Vector3.ONE * 1.25, _duration * 0.45)
	_tween.set_parallel(false)
	_tween.tween_interval(_duration * 0.45)
	_tween.tween_callback(stop)


func _create_beam(edge_color: Color) -> void:
	var delta: Vector3 = _target_position - _source_position
	delta.y = 0.0
	if _source_position == Vector3.ZERO or delta.length() <= 0.05:
		return

	_beam = MeshInstance3D.new()
	_beam.name = "SourceBeam"
	_beam.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	var mesh: BoxMesh = BoxMesh.new()
	mesh.size = Vector3(0.16, 0.16, delta.length())
	_beam.mesh = mesh
	_beam.material_override = _material(edge_color, 0.7)
	add_child(_beam)
	_beam.global_position = _source_position + delta * 0.5 + Vector3(0.0, 0.55, 0.0)
	_beam.rotation.y = atan2(delta.x, delta.z)


func _palette() -> Dictionary:
	match _element:
		"fire":
			return {"fill": Color(1.0, 0.3, 0.05, 0.55), "edge": Color(1.0, 0.62, 0.16, 0.95)}
		"water":
			return {"fill": Color(0.25, 0.72, 1.0, 0.5), "edge": Color(0.58, 0.93, 1.0, 0.95)}
		"earth":
			return {"fill": Color(0.52, 0.78, 0.34, 0.55), "edge": Color(0.8, 0.96, 0.48, 0.95)}
		"wind":
			return {"fill": Color(0.82, 0.98, 1.0, 0.45), "edge": Color(0.96, 1.0, 1.0, 0.95)}
		_:
			return {"fill": Color(0.62, 0.74, 1.0, 0.5), "edge": Color(0.78, 0.88, 1.0, 0.95)}


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
	_marker = null
	_beam = null


func _on_reset() -> void:
	_clear_nodes()
