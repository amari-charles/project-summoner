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
