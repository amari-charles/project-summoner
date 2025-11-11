extends Resource
class_name BiomeConfig

## Visual theme configuration for battlefield environments
## Defines textures, colors, lighting for a specific biome (summer, winter, desert, etc.)

@export_group("Identification")
@export var biome_id: String = "unknown"
@export var biome_name: String = "Unknown Biome"

@export_group("Ground")
@export var ground_texture: Texture2D
@export var ground_size: Vector2 = Vector2(100, 80)
@export var ground_uv_scale: Vector3 = Vector3(17, 14, 1)

@export_group("Lighting")
@export var ambient_light_color: Color = Color.WHITE
@export var ambient_light_energy: float = 0.5
@export var directional_light_rotation_degrees: Vector3 = Vector3(-30, 45, 0)
@export var directional_light_color: Color = Color.WHITE
@export var directional_light_energy: float = 1.0

@export_group("Environment")
@export var background_color: Color = Color(0.1, 0.1, 0.1, 1)
@export var fog_enabled: bool = false
@export var fog_color: Color = Color(0.8, 0.8, 0.9)

## Apply this biome to a battlefield
func apply_to_battlefield(battlefield: Node3D) -> void:
	_apply_ground(battlefield)
	_apply_lighting(battlefield)
	_apply_environment(battlefield)

	print("BiomeConfig: Applied biome '%s' to battlefield" % biome_name)

## Apply ground texture and material
func _apply_ground(battlefield: Node3D) -> void:
	var background = battlefield.get_node_or_null("Background") as MeshInstance3D
	if not background:
		push_warning("BiomeConfig: Background node not found")
		return

	# Update mesh size
	if background.mesh is PlaneMesh:
		(background.mesh as PlaneMesh).size = ground_size

	# Create and apply material
	var material = StandardMaterial3D.new()
	material.albedo_texture = ground_texture
	material.texture_filter = BaseMaterial3D.TEXTURE_FILTER_NEAREST
	material.uv1_scale = ground_uv_scale
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.cull_mode = BaseMaterial3D.CULL_BACK

	background.set_surface_override_material(0, material)

## Apply lighting settings
func _apply_lighting(battlefield: Node3D) -> void:
	# Update directional light
	var directional_light = battlefield.get_node_or_null("DirectionalLight3D") as DirectionalLight3D
	if directional_light:
		directional_light.rotation_degrees = directional_light_rotation_degrees
		directional_light.light_color = directional_light_color
		directional_light.light_energy = directional_light_energy

## Apply environment settings
func _apply_environment(battlefield: Node3D) -> void:
	var world_env = battlefield.get_node_or_null("WorldEnvironment") as WorldEnvironment
	if not world_env or not world_env.environment:
		push_warning("BiomeConfig: WorldEnvironment not found")
		return

	var env = world_env.environment
	env.background_color = background_color
	env.ambient_light_color = ambient_light_color
	env.ambient_light_energy = ambient_light_energy

	# Apply fog if enabled
	env.fog_enabled = fog_enabled
	if fog_enabled:
		env.fog_light_color = fog_color
