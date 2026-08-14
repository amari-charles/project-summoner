extends GutTest

var _host: Node3D


func before_each() -> void:
	_host = Node3D.new()
	add_child(_host)
	await get_tree().process_frame

func after_each() -> void:
	if is_instance_valid(_host) and _host.is_inside_tree():
		_host.queue_free()
	await get_tree().process_frame


func _make_battlefield_root() -> Node3D:
	var battlefield: Node3D = Node3D.new()
	var background: MeshInstance3D = MeshInstance3D.new()
	background.name = "Background"
	background.mesh = PlaneMesh.new()
	var ground_layer: Node3D = Node3D.new()
	ground_layer.name = "GroundLayer"
	battlefield.add_child(background)
	battlefield.add_child(ground_layer)
	return battlefield

func _make_checker_texture() -> ImageTexture:
	var image: Image = Image.create(2, 2, false, Image.FORMAT_RGBA8)
	image.fill(Color(0.6667, 0.6667, 0.6667, 1.0))
	image.set_pixel(1, 0, Color(0.8235, 0.8235, 0.8235, 1.0))
	image.set_pixel(0, 1, Color(0.8235, 0.8235, 0.8235, 1.0))
	image.set_pixel(1, 1, Color(0.6667, 0.6667, 0.6667, 1.0))
	return ImageTexture.create_from_image(image)


func test_apply_ground_builds_checker_pillars_with_opposite_side_colors() -> void:
	var battlefield: Node3D = _make_battlefield_root()
	_host.add_child(battlefield)

	var biome: BiomeConfig = BiomeConfig.new()
	biome.ground_texture = _make_checker_texture()
	biome.ground_size = Vector2(10.0, 10.0)
	biome.ground_uv_scale = Vector3(2.0, 2.0, 1.0)
	biome.use_checker_tile_pillars = true

	biome._apply_ground(battlefield)

	var background: MeshInstance3D = battlefield.get_node("Background") as MeshInstance3D
	assert_false(background.visible, "Background should be hidden when checker pillars are active")

	var pillars_root: Node3D = battlefield.get_node("GroundLayer/CheckerPillars") as Node3D
	assert_not_null(pillars_root, "Checker pillar root should be created under GroundLayer")
	var sides: MultiMeshInstance3D = pillars_root.get_node("Sides") as MultiMeshInstance3D
	var tops: MultiMeshInstance3D = pillars_root.get_node("Tops") as MultiMeshInstance3D
	assert_not_null(sides, "Sides multimesh should exist")
	assert_not_null(tops, "Tops multimesh should exist")
	assert_eq(sides.multimesh.instance_count, 16, "Expected 4x4 checker tiles for this texture/uv scale")
	assert_eq(tops.multimesh.instance_count, 16, "Expected 4x4 checker tiles for this texture/uv scale")
	assert_true(sides.multimesh.use_colors, "Sides multimesh should enable per-instance colors")
	assert_true(tops.multimesh.use_colors, "Tops multimesh should enable per-instance colors")
	var side_material: StandardMaterial3D = sides.material_override as StandardMaterial3D
	var top_material: StandardMaterial3D = tops.material_override as StandardMaterial3D
	assert_not_null(side_material, "Sides should use StandardMaterial3D")
	assert_not_null(top_material, "Tops should use StandardMaterial3D")
	assert_true(side_material.vertex_color_use_as_albedo, "Side material should render vertex colors")
	assert_true(top_material.vertex_color_use_as_albedo, "Top material should render vertex colors")

func test_checker_tile_count_is_clamped_for_large_textures_or_uv_scales() -> void:
	var battlefield: Node3D = _make_battlefield_root()
	_host.add_child(battlefield)

	var biome: BiomeConfig = BiomeConfig.new()
	biome.ground_texture = _make_checker_texture()
	biome.ground_size = Vector2(10.0, 10.0)
	biome.ground_uv_scale = Vector3(1000.0, 1000.0, 1.0)
	biome.use_checker_tile_pillars = true

	var built: bool = biome._build_checker_tile_pillars(battlefield)
	assert_true(built, "Checker pillar generation should succeed with valid texture input")

	var sides: MultiMeshInstance3D = battlefield.get_node("GroundLayer/CheckerPillars/Sides") as MultiMeshInstance3D
	assert_not_null(sides, "Sides multimesh should exist after generation")
	assert_eq(sides.multimesh.instance_count, 16384, "Tile count should clamp to 128x128")


func test_custom_arena_visual_replaces_rendered_ground_but_preserves_logical_size() -> void:
	var battlefield: Node3D = _make_battlefield_root()
	_host.add_child(battlefield)

	var biome: BiomeConfig = BiomeConfig.new()
	biome.ground_size = Vector2(100.0, 50.0)
	biome.arena_visual_scene = load(
		"res://scenes/battle/battlefield/biomes/island_water_arena_visual.tscn"
	) as PackedScene

	biome._apply_ground(battlefield)

	var background: MeshInstance3D = battlefield.get_node("Background") as MeshInstance3D
	assert_false(background.visible, "Logical background should be hidden behind a custom visual")
	assert_eq((background.mesh as PlaneMesh).size, Vector2(100.0, 50.0))
	var visuals: Node3D = battlefield.get_node("GroundLayer/BiomeVisuals") as Node3D
	assert_not_null(visuals, "Custom visual should be mounted under GroundLayer")
	assert_not_null(visuals.get_node_or_null("Island/Center"), "Island ground should be built")
	var front_cliff: MeshInstance3D = visuals.get_node(
		"Island/FrontCliffCenter"
	) as MeshInstance3D
	assert_not_null(front_cliff, "Front cliff should be built")
	assert_almost_eq(visuals.rotation.y, PI, 0.001, "Visual should face the battle camera")
	assert_gt(front_cliff.position.z, 0.0, "Cliff should retain the proven local arrangement")
	assert_lt(front_cliff.global_position.z, 0.0, "Rotated cliff should face the battle camera")
	assert_null(visuals.get_node_or_null("Island/LeftCliff"), "Side walls should not be tiled")
	assert_null(visuals.get_node_or_null("Island/RightCliff"), "Side walls should not be tiled")
	assert_not_null(visuals.get_node_or_null("Water/Surface"), "Water surface should be built")
	var water_surface: MeshInstance3D = visuals.get_node("Water/Surface") as MeshInstance3D
	var foam: Node3D = visuals.get_node("Water/Foam") as Node3D
	assert_eq(foam.get_child_count(), 128, "Foam should cover the full 44x22 island perimeter")
	var top_foam: MeshInstance3D = foam.get_node("Top0") as MeshInstance3D
	var bottom_foam: MeshInstance3D = foam.get_node("Bottom0") as MeshInstance3D
	var side_foam: MeshInstance3D = foam.get_node("Left1") as MeshInstance3D
	var foam_y: float = water_surface.position.y + IslandWaterArenaVisual.WATER_FOAM_HEIGHT_OFFSET
	assert_almost_eq(top_foam.position.y, foam_y, 0.001, "Top foam should sit on the water")
	assert_almost_eq(bottom_foam.position.y, foam_y, 0.001, "Bottom foam should sit on the water")
	assert_almost_eq(side_foam.position.y, foam_y, 0.001, "Side foam should sit on the water")


func test_all_registered_biomes_have_matching_resources() -> void:
	for biome_id: StringName in BiomeIDs.ALL_BIOMES:
		var resource_path: String = BiomeIDs.get_resource_path(biome_id)
		assert_true(ResourceLoader.exists(resource_path), "%s should exist" % resource_path)
		var biome: BiomeConfig = load(resource_path) as BiomeConfig
		assert_not_null(biome, "%s should load as BiomeConfig" % resource_path)
		assert_eq(StringName(biome.biome_id), biome_id, "%s should declare its registered ID" % resource_path)
