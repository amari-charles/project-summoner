extends VFXInstance
class_name SummonCircleVFX

## Summoning circle visual effect
## Shows an animated magical circle at the spawn location during casting
## Receives data: position, duration, team (for color)

@onready var circle_mesh: MeshInstance3D = $CircleMesh

## Casting duration (for progress animation)
var cast_duration: float = 1.0
var cast_progress: float = 0.0

## Team colors
const PLAYER_COLOR: Color = Color(0.3, 0.6, 1.0, 0.8)  # Blue
const ENEMY_COLOR: Color = Color(1.0, 0.3, 0.3, 0.8)   # Red

func _ready() -> void:
	super._ready()
	# Isolate materials for safe modification
	if circle_mesh:
		isolate_mesh_resources(circle_mesh, false, true)

func receive_data(data: Dictionary) -> void:
	# Extract casting duration
	if data.has("duration"):
		cast_duration = data.duration
		lifetime = cast_duration + 0.5  # Slight buffer for fade out

	# Set color based on team
	if data.has("team") and circle_mesh:
		var material: ShaderMaterial = circle_mesh.material_override as ShaderMaterial
		if material:
			var team: int = data.team
			var color: Color = PLAYER_COLOR if team == Unit3D.Team.PLAYER else ENEMY_COLOR
			var glow_color: Color = color.lightened(0.3)
			material.set_shader_parameter("circle_color", color)
			material.set_shader_parameter("glow_color", glow_color)

func _on_play() -> void:
	cast_progress = 0.0
	# Reset progress shader param
	if circle_mesh:
		var material: ShaderMaterial = circle_mesh.material_override as ShaderMaterial
		if material:
			material.set_shader_parameter("progress", 0.0)

func _physics_process(delta: float) -> void:
	super._physics_process(delta)

	if not is_playing:
		return

	# Update progress
	if cast_duration > 0.0:
		cast_progress = minf(time_alive / cast_duration, 1.0)

		if circle_mesh:
			var material: ShaderMaterial = circle_mesh.material_override as ShaderMaterial
			if material:
				material.set_shader_parameter("progress", cast_progress)

		# Scale up slightly as progress increases
		var scale_factor: float = 1.0 + cast_progress * 0.1
		circle_mesh.scale = Vector3(scale_factor, 1.0, scale_factor)

func _on_reset() -> void:
	cast_progress = 0.0
	if circle_mesh:
		circle_mesh.scale = Vector3.ONE
		var material: ShaderMaterial = circle_mesh.material_override as ShaderMaterial
		if material:
			material.set_shader_parameter("progress", 0.0)
