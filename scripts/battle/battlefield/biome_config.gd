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
	var background_node: Node = battlefield.get_node_or_null("Background")
	if not background_node or not background_node is MeshInstance3D:
		push_warning("BiomeConfig: Background node not found")
		return
	var background: MeshInstance3D = background_node

	# Update mesh size
	if background.mesh is PlaneMesh:
		var plane_mesh: PlaneMesh = background.mesh
		plane_mesh.size = ground_size

	# Create and apply material
	var material: StandardMaterial3D = StandardMaterial3D.new()
	material.albedo_texture = ground_texture
	material.texture_filter = BaseMaterial3D.TEXTURE_FILTER_NEAREST
	material.uv1_scale = ground_uv_scale
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.cull_mode = BaseMaterial3D.CULL_BACK

	background.set_surface_override_material(0, material)

## Apply lighting settings
func _apply_lighting(battlefield: Node3D) -> void:
	# Update directional light
	var light_node: Node = battlefield.get_node_or_null("DirectionalLight3D")
	if light_node and light_node is DirectionalLight3D:
		var directional_light: DirectionalLight3D = light_node
		directional_light.rotation_degrees = directional_light_rotation_degrees
		directional_light.light_color = directional_light_color
		directional_light.light_energy = directional_light_energy

## Apply environment settings
func _apply_environment(battlefield: Node3D) -> void:
	var world_env_node: Node = battlefield.get_node_or_null("WorldEnvironment")
	if not world_env_node or not world_env_node is WorldEnvironment:
		push_warning("BiomeConfig: WorldEnvironment not found")
		return
	var world_env: WorldEnvironment = world_env_node
	if not world_env.environment:
		push_warning("BiomeConfig: WorldEnvironment has no environment")
		return

	var env: Environment = world_env.environment
	env.background_color = background_color
	env.ambient_light_color = ambient_light_color
	env.ambient_light_energy = ambient_light_energy

	# Apply fog if enabled
	env.fog_enabled = fog_enabled
	if fog_enabled:
		env.fog_light_color = fog_color
