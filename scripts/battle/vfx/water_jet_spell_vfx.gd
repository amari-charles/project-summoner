extends VFXInstance
class_name WaterJetSpellVFX

@export var beam_duration: float = 0.35

var _beam: MeshInstance3D = null
var _impact: MeshInstance3D = null
var _tween: Tween = null
var _source_position: Vector3 = Vector3.ZERO
var _target_position: Vector3 = Vector3(2.2, 0.0, 0.0)
var _beam_length: float = 2.2
var _beam_direction: float = 1.0
var _beam_length_scale: float = 1.0

func _ready() -> void:
	_beam = $JetBeam if has_node("JetBeam") else null
	_impact = $ImpactSphere if has_node("ImpactSphere") else null

	if _beam:
		isolate_mesh_resources(_beam, false, true)
	if _impact:
		isolate_mesh_resources(_impact, false, true)

	lifetime = 0.0
	super._ready()

func receive_data(data: Dictionary) -> void:
	_source_position = global_position
	_target_position = global_position + Vector3(2.2, 0.0, 0.0)

	if data.has("source_position"):
		var source_value: Variant = data["source_position"]
		if source_value is Vector3:
			_source_position = source_value

	if data.has("target_position"):
		var target_value: Variant = data["target_position"]
		if target_value is Vector3:
			_target_position = target_value

	var delta_x: float = _target_position.x - _source_position.x
	_beam_length = maxf(0.05, absf(delta_x))
	_beam_direction = -1.0 if delta_x < 0.0 else 1.0
	_beam_length_scale = _beam_length / 2.2

func _on_play() -> void:
	if _beam == null or _impact == null:
		stop()
		return

	_beam.visible = true
	_impact.visible = true
	rotation = Vector3.ZERO
	_set_travel_progress(0.02)
	_impact.scale = Vector3(0.35, 0.35, 0.35)

	if _tween:
		_tween.kill()
	_tween = create_tween()
	_tween.tween_method(_set_travel_progress, 0.02, 1.0, beam_duration * 0.55)
	_tween.parallel().tween_property(_impact, "scale", Vector3(1.2, 1.2, 1.2), beam_duration * 0.55)
	_tween.tween_interval(beam_duration * 0.25)
	_tween.tween_callback(stop)

func _set_travel_progress(progress: float) -> void:
	if _beam == null or _impact == null:
		return

	var t: float = clampf(progress, 0.0, 1.0)
	var current_length: float = _beam_length * t
	var current_scale_y: float = maxf(0.01, _beam_length_scale * t)
	_beam.position = Vector3(current_length * 0.5 * _beam_direction, 0.45, 0.0)
	_beam.scale = Vector3(1.0, current_scale_y, 1.0)
	_impact.position = Vector3(current_length * _beam_direction, 0.45, 0.0)

func _on_reset() -> void:
	if _tween:
		_tween.kill()
		_tween = null
	if _beam:
		_beam.visible = false
		_beam.position = Vector3(1.0, 0.45, 0.0)
		_beam.scale = Vector3.ONE
	if _impact:
		_impact.visible = false
		_impact.position = Vector3(2.2, 0.45, 0.0)
		_impact.scale = Vector3.ONE
