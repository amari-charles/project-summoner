extends Resource
class_name BiomeConfig

## Visual theme configuration for battlefield environments
## Defines textures, colors, lighting for a specific biome (summer, winter, desert, etc.)
const CHECKER_PILLARS_NODE_NAME: StringName = &"CheckerPillars"
const CHECKER_PILLARS_SIDES_NODE_NAME: StringName = &"Sides"
const CHECKER_PILLARS_TOPS_NODE_NAME: StringName = &"Tops"
const TOP_SURFACE_Y_OFFSET: float = 0.005

@export_group("Identification")
@export var biome_id: String = "unknown"
@export var biome_name: String = "Unknown Biome"

@export_group("Ground")
@export var ground_texture: Texture2D
@export var ground_size: Vector2 = Vector2(100, 80)
@export var ground_uv_scale: Vector3 = Vector3(17, 14, 1)
## Render the arena as per-checker tile pillars instead of a single flat mesh.
@export var use_checker_tile_pillars: bool = true
## Tile pillar height as a fraction of checker tile width.
@export var checker_tile_pillar_height_ratio: float = 1.0 / 3.0
## Global brightness multiplier for checker tile pillar colors (1.0 = unchanged).
@export var checker_tile_brightness: float = 0.94

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

	# Keep the logical arena mesh flat for bounds/camera calculations.
	if background.mesh is PlaneMesh:
		var plane_mesh: PlaneMesh = background.mesh
		plane_mesh.size = ground_size

	if use_checker_tile_pillars and _build_checker_tile_pillars(battlefield):
		background.visible = false
		background.set_surface_override_material(0, null)
		return

	_clear_checker_tile_pillars(battlefield)
	background.visible = true
	_apply_flat_ground_material(background)

func _apply_flat_ground_material(background: MeshInstance3D) -> void:
	# Fallback visual if pillar generation is unavailable.
	var material: StandardMaterial3D = StandardMaterial3D.new()
	material.albedo_texture = ground_texture
	material.texture_filter = BaseMaterial3D.TEXTURE_FILTER_NEAREST
	material.uv1_scale = ground_uv_scale
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.cull_mode = BaseMaterial3D.CULL_BACK
	background.set_surface_override_material(0, material)

func _build_checker_tile_pillars(battlefield: Node3D) -> bool:
	if not ground_texture:
		return false

	var texture_image: Image = ground_texture.get_image()
	if texture_image == null:
		return false
	if texture_image.get_width() <= 0 or texture_image.get_height() <= 0:
		return false

	_clear_checker_tile_pillars(battlefield)

	var tile_count_x: int = maxi(1, int(round(ground_uv_scale.x * texture_image.get_width())))
	var tile_count_z: int = maxi(1, int(round(ground_uv_scale.y * texture_image.get_height())))
	var tile_size_x: float = max(ground_size.x / float(tile_count_x), 0.01)
	var tile_size_z: float = max(ground_size.y / float(tile_count_z), 0.01)
	var pillar_height: float = max(min(tile_size_x, tile_size_z) * max(checker_tile_pillar_height_ratio, 0.01), 0.01)
	var tile_count_total: int = tile_count_x * tile_count_z

	var side_mesh: BoxMesh = BoxMesh.new()
	side_mesh.size = Vector3(tile_size_x, pillar_height, tile_size_z)
	var top_mesh: PlaneMesh = PlaneMesh.new()
	top_mesh.size = Vector2(tile_size_x, tile_size_z)

	var sides_multimesh: MultiMesh = MultiMesh.new()
	sides_multimesh.transform_format = MultiMesh.TRANSFORM_3D
	sides_multimesh.use_colors = true
	sides_multimesh.mesh = side_mesh
	sides_multimesh.instance_count = tile_count_total

	var tops_multimesh: MultiMesh = MultiMesh.new()
	tops_multimesh.transform_format = MultiMesh.TRANSFORM_3D
	tops_multimesh.use_colors = true
	tops_multimesh.mesh = top_mesh
	tops_multimesh.instance_count = tile_count_total

	var pillar_colors: Array[Color] = _resolve_checker_palette(texture_image)
	var color_a: Color = pillar_colors[0]
	var color_b: Color = pillar_colors[1]

	var min_x: float = -ground_size.x * 0.5 + tile_size_x * 0.5
	var min_z: float = -ground_size.y * 0.5 + tile_size_z * 0.5
	var y: float = -pillar_height * 0.5
	var instance_idx: int = 0
	for z_idx: int in range(tile_count_z):
		for x_idx: int in range(tile_count_x):
			var center: Vector3 = Vector3(
				min_x + float(x_idx) * tile_size_x,
				0.0,
				min_z + float(z_idx) * tile_size_z
			)
			var top_color: Color = color_a if ((x_idx + z_idx) % 2 == 0) else color_b
			var side_color: Color = color_b if ((x_idx + z_idx) % 2 == 0) else color_a

			var side_transform: Transform3D = Transform3D.IDENTITY
			side_transform.origin = Vector3(center.x, y, center.z)
			sides_multimesh.set_instance_transform(instance_idx, side_transform)
			sides_multimesh.set_instance_color(instance_idx, side_color)

			var top_transform: Transform3D = Transform3D.IDENTITY
			top_transform.origin = Vector3(center.x, TOP_SURFACE_Y_OFFSET, center.z)
			tops_multimesh.set_instance_transform(instance_idx, top_transform)
			tops_multimesh.set_instance_color(instance_idx, top_color)
			instance_idx += 1

	var side_material: StandardMaterial3D = StandardMaterial3D.new()
	side_material.vertex_color_use_as_albedo = true
	side_material.vertex_color_is_srgb = true
	side_material.albedo_color = Color(checker_tile_brightness, checker_tile_brightness, checker_tile_brightness, 1.0)
	side_material.texture_filter = BaseMaterial3D.TEXTURE_FILTER_NEAREST
	side_material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	side_material.cull_mode = BaseMaterial3D.CULL_BACK

	var top_material: StandardMaterial3D = StandardMaterial3D.new()
	top_material.vertex_color_use_as_albedo = true
	top_material.vertex_color_is_srgb = true
	top_material.albedo_color = Color(checker_tile_brightness, checker_tile_brightness, checker_tile_brightness, 1.0)
	top_material.texture_filter = BaseMaterial3D.TEXTURE_FILTER_NEAREST
	top_material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	top_material.cull_mode = BaseMaterial3D.CULL_BACK

	var sides: MultiMeshInstance3D = MultiMeshInstance3D.new()
	sides.name = String(CHECKER_PILLARS_SIDES_NODE_NAME)
	sides.multimesh = sides_multimesh
	sides.material_override = side_material

	var tops: MultiMeshInstance3D = MultiMeshInstance3D.new()
	tops.name = String(CHECKER_PILLARS_TOPS_NODE_NAME)
	tops.multimesh = tops_multimesh
	tops.material_override = top_material

	var pillars_root: Node3D = Node3D.new()
	pillars_root.name = String(CHECKER_PILLARS_NODE_NAME)
	pillars_root.add_child(sides)
	pillars_root.add_child(tops)

	var ground_layer_node: Node = battlefield.get_node_or_null("GroundLayer")
	var parent: Node = ground_layer_node if ground_layer_node else battlefield
	parent.add_child(pillars_root)
	return true

func _clear_checker_tile_pillars(battlefield: Node3D) -> void:
	var existing: Node = battlefield.find_child(String(CHECKER_PILLARS_NODE_NAME), true, false)
	if existing and is_instance_valid(existing):
		existing.free()

func _resolve_checker_palette(texture_image: Image) -> Array[Color]:
	var first: Color = texture_image.get_pixel(0, 0)
	var second: Color = first
	for y: int in range(texture_image.get_height()):
		for x: int in range(texture_image.get_width()):
			var current: Color = texture_image.get_pixel(x, y)
			if not _colors_almost_equal(current, first):
				second = current
				return [first, second]
	return [first, first.darkened(0.1)]

func _colors_almost_equal(a: Color, b: Color) -> bool:
	return (
		is_equal_approx(a.r, b.r) and
		is_equal_approx(a.g, b.g) and
		is_equal_approx(a.b, b.b) and
		is_equal_approx(a.a, b.a)
	)

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
