extends VFXInstance
class_name CleanseSpellVFX

@export var default_radius: float = 7.0
@export var default_duration: float = 0.6

var _radius: float = 7.0
var _duration: float = 0.6
var _ring: MeshInstance3D = null
var _column: MeshInstance3D = null
var _tween: Tween = null

func _ready() -> void:
	_ring = $GroundRing if has_node("GroundRing") else null
	_column = $WaterColumn if has_node("WaterColumn") else null

	if _ring:
		isolate_mesh_resources(_ring, false, true)
	if _column:
		isolate_mesh_resources(_column, false, true)

	lifetime = 0.0
	super._ready()

func receive_data(data: Dictionary) -> void:
	_radius = default_radius
	_duration = default_duration

	if data.has("radius"):
		var radius_value: Variant = data["radius"]
		if radius_value is float:
			_radius = maxf(1.0, float(radius_value))
		elif radius_value is int:
			_radius = maxf(1.0, float(radius_value))

	if data.has("duration"):
		var duration_value: Variant = data["duration"]
		if duration_value is float:
			_duration = maxf(0.25, float(duration_value))
		elif duration_value is int:
			_duration = maxf(0.25, float(duration_value))

func _on_play() -> void:
	if _ring == null or _column == null:
		stop()
		return

	_ring.visible = true
	_column.visible = true

	var ring_scale: float = maxf(0.8, _radius)
	_ring.scale = Vector3(ring_scale * 0.6, 1.0, ring_scale * 0.6)
	_column.scale = Vector3(0.85, 1.0, 0.85)

	if _tween:
		_tween.kill()
	_tween = create_tween()
	_tween.tween_property(_ring, "scale", Vector3(ring_scale, 1.0, ring_scale), _duration * 0.6)
	_tween.parallel().tween_property(_column, "scale", Vector3(1.1, 1.0, 1.1), _duration * 0.45)
	_tween.tween_interval(_duration * 0.25)
	_tween.tween_callback(stop)

func _on_reset() -> void:
	if _tween:
		_tween.kill()
		_tween = null
	if _ring:
		_ring.visible = false
		_ring.scale = Vector3.ONE
	if _column:
		_column.visible = false
		_column.scale = Vector3.ONE
