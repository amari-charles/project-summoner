extends VFXInstance
class_name LightningStrikeVFX
## Lightning Strike VFX - Instant bolt from attacker to target
## Used for ranged units with VFX-based attacks (storm cloud, etc.)

## Configurable parameters
@export var hold_duration: float = 0.4  ## Time at full brightness
@export var fade_duration: float = 0.4  ## Fade out time
@export var bolt_segments: int = 8  ## Segments for the bolt mesh (more = smoother curves)

## Runtime state (received via receive_data)
var start_position: Vector3 = Vector3.ZERO
var end_position: Vector3 = Vector3.ZERO
var spell_damage: float = 0.0
var spell_team: int = 0
var target_unit: Node3D = null

## Internal references
var bolt_mesh: MeshInstance3D = null
var bolt_material: ShaderMaterial = null
var animation_tween: Tween = null

func _ready() -> void:
	# Find bolt mesh child
	bolt_mesh = $BoltMesh if has_node("BoltMesh") else null

	if bolt_mesh:
		# Isolate material for pooling safety (we modify alpha uniform)
		isolate_mesh_resources(bolt_mesh, true, true)
		bolt_material = bolt_mesh.get_surface_override_material(0) as ShaderMaterial
		if not bolt_material:
			bolt_material = bolt_mesh.material_override as ShaderMaterial

	# Disable auto-expire - we control timing via tween
	lifetime = 0.0

	super._ready()

## Receive custom data from VFXManager
func receive_data(data: Dictionary) -> void:
	if data.has("start_position") and data.start_position is Vector3:
		start_position = data.start_position

	if data.has("end_position") and data.end_position is Vector3:
		end_position = data.end_position

	if data.has("damage"):
		if data.damage is float:
			spell_damage = data.damage
		elif data.damage is int:
			spell_damage = float(data.damage)

	if data.has("team") and data.team is int:
		spell_team = data.team

	if data.has("target") and data.target is Node3D:
		target_unit = data.target

## Start the lightning effect
func _on_play() -> void:
	if not bolt_mesh or not bolt_material:
		push_error("LightningStrikeVFX: Missing mesh or material!")
		stop()
		return

	# Position and orient the bolt mesh between start and end
	_setup_bolt_geometry()

	# Set shader to full visibility
	bolt_material.set_shader_parameter("alpha", 1.0)
	bolt_mesh.visible = true

	# Apply damage immediately (lightning is instant)
	_apply_damage()

	# Animate: brief hold, then fade out
	if animation_tween:
		animation_tween.kill()

	animation_tween = create_tween()
	animation_tween.tween_interval(hold_duration)
	animation_tween.tween_method(_set_alpha, 1.0, 0.0, fade_duration)
	animation_tween.tween_callback(stop)

## Set up bolt mesh geometry between start and end positions
func _setup_bolt_geometry() -> void:
	var direction: Vector3 = end_position - start_position
	var length: float = direction.length()

	if length < 0.001:
		push_warning("LightningStrikeVFX: Start and end positions are the same!")
		return

	# Position at midpoint between start and end
	global_position = (start_position + end_position) / 2.0

	# Orient to point from start to end
	# The mesh is a vertical quad, so we need to rotate it to align with the bolt direction
	var up: Vector3 = Vector3.UP
	var dir_normalized: Vector3 = direction.normalized()

	# If direction is nearly vertical, use a different up vector
	if abs(dir_normalized.dot(up)) > 0.99:
		up = Vector3.RIGHT

	# Look at end position, with the up vector perpendicular
	look_at(end_position, up)

	# Rotate so the quad faces the camera (billboard-like but along the bolt axis)
	# The quad's local Y axis should align with the bolt direction
	rotate_object_local(Vector3.RIGHT, PI / 2.0)

	# Scale the mesh to match bolt length
	# QuadMesh default is 2x2, so we scale Y to length and X for width
	bolt_mesh.scale = Vector3(1.0, length / 2.0, 1.0)

## Apply single-target damage
func _apply_damage() -> void:
	if not target_unit or not is_instance_valid(target_unit):
		return

	if spell_damage <= 0:
		return

	# Apply damage via DamageSystem if available, otherwise direct
	if target_unit.has_method("take_damage"):
		target_unit.take_damage(spell_damage)

## Update shader alpha (called by tween)
func _set_alpha(value: float) -> void:
	if bolt_material:
		bolt_material.set_shader_parameter("alpha", value)

## Reset state for pooling
func _on_reset() -> void:
	# Kill any active tween
	if animation_tween:
		animation_tween.kill()
		animation_tween = null

	# Hide and reset mesh
	if bolt_mesh:
		bolt_mesh.visible = false
		bolt_mesh.scale = Vector3.ONE

	# Reset shader alpha
	if bolt_material:
		bolt_material.set_shader_parameter("alpha", 1.0)

	# Clear runtime state
	start_position = Vector3.ZERO
	end_position = Vector3.ZERO
	spell_damage = 0.0
	spell_team = 0
	target_unit = null
