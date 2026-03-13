extends VFXInstance
class_name RainFieldSpellVFX

@export var default_radius: float = 8.0
@export var default_duration: float = 3.0

var _radius: float = 8.0
var _duration: float = 3.0
var _ring: MeshInstance3D = null
var _cloud: MeshInstance3D = null
var _rain_a: MeshInstance3D = null
var _rain_b: MeshInstance3D = null
var _rain_c: MeshInstance3D = null
var _tween: Tween = null

func _ready() -> void:
	_ring = $GroundRing if has_node("GroundRing") else null
	_cloud = $RainCloud if has_node("RainCloud") else null
	_rain_a = $RainA if has_node("RainA") else null
	_rain_b = $RainB if has_node("RainB") else null
	_rain_c = $RainC if has_node("RainC") else null

	if _ring:
		isolate_mesh_resources(_ring, false, true)
	if _cloud:
		isolate_mesh_resources(_cloud, false, true)
	if _rain_a:
		isolate_mesh_resources(_rain_a, false, true)
	if _rain_b:
		isolate_mesh_resources(_rain_b, false, true)
	if _rain_c:
		isolate_mesh_resources(_rain_c, false, true)

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
			_duration = maxf(1.0, float(duration_value))
		elif duration_value is int:
			_duration = maxf(1.0, float(duration_value))

func _on_play() -> void:
	if _ring == null or _cloud == null:
		stop()
		return

	var zone_scale: float = maxf(0.8, _radius * 0.25)
	_ring.visible = true
	_ring.scale = Vector3(zone_scale, 1.0, zone_scale)

	_cloud.visible = true
	_cloud.scale = Vector3(zone_scale * 0.65, 1.0, zone_scale * 0.65)

	if _rain_a:
		_rain_a.visible = true
		_rain_a.scale = Vector3(1.0, 0.6, 1.0)
	if _rain_b:
		_rain_b.visible = true
		_rain_b.scale = Vector3(1.0, 0.6, 1.0)
	if _rain_c:
		_rain_c.visible = true
		_rain_c.scale = Vector3(1.0, 0.6, 1.0)

	if _tween:
		_tween.kill()
	_tween = create_tween()
	_tween.set_loops(6)
	_tween.tween_property(_cloud, "scale", Vector3(zone_scale * 0.72, 1.0, zone_scale * 0.72), 0.2)
	_tween.tween_property(_cloud, "scale", Vector3(zone_scale * 0.62, 1.0, zone_scale * 0.62), 0.2)
	_tween.finished.connect(func() -> void:
		if is_instance_valid(self):
			stop()
	)

	var cleanup: SceneTreeTimer = get_tree().create_timer(_duration)
	cleanup.timeout.connect(func() -> void:
		if is_instance_valid(self) and is_playing:
			stop()
	)

func _on_reset() -> void:
	if _tween:
		_tween.kill()
		_tween = null

	if _ring:
		_ring.visible = false
		_ring.scale = Vector3.ONE
	if _cloud:
		_cloud.visible = false
		_cloud.scale = Vector3.ONE
	if _rain_a:
		_rain_a.visible = false
		_rain_a.scale = Vector3.ONE
	if _rain_b:
		_rain_b.visible = false
		_rain_b.scale = Vector3.ONE
	if _rain_c:
		_rain_c.visible = false
		_rain_c.scale = Vector3.ONE
