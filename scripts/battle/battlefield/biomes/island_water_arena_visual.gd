extends Node3D
class_name IslandWaterArenaVisual

## Placeholder Tiny Swords arena treatment. This scene is visual-only; the
## battlefield Background plane remains authoritative for gameplay bounds.
const GRASS_CENTER: Texture2D = preload("res://assets/placeholders/tiny_swords/terrain/placeholder_grass_color3.png")
const GRASS_TOP_LEFT: Texture2D = preload("res://assets/placeholders/tiny_swords/terrain/placeholder_grass_top_left.png")
const GRASS_TOP: Texture2D = preload("res://assets/placeholders/tiny_swords/terrain/placeholder_grass_top.png")
const GRASS_TOP_RIGHT: Texture2D = preload("res://assets/placeholders/tiny_swords/terrain/placeholder_grass_top_right.png")
const GRASS_LEFT: Texture2D = preload("res://assets/placeholders/tiny_swords/terrain/placeholder_grass_left.png")
const GRASS_RIGHT: Texture2D = preload("res://assets/placeholders/tiny_swords/terrain/placeholder_grass_right.png")
const GRASS_BOTTOM_LEFT: Texture2D = preload("res://assets/placeholders/tiny_swords/terrain/placeholder_grass_bottom_left.png")
const GRASS_BOTTOM: Texture2D = preload("res://assets/placeholders/tiny_swords/terrain/placeholder_grass_bottom.png")
const GRASS_BOTTOM_RIGHT: Texture2D = preload("res://assets/placeholders/tiny_swords/terrain/placeholder_grass_bottom_right.png")
const CLIFF_LEFT: Texture2D = preload("res://assets/placeholders/tiny_swords/terrain/placeholder_cliff_middle_left.png")
const CLIFF_MIDDLE: Texture2D = preload("res://assets/placeholders/tiny_swords/terrain/placeholder_cliff_middle.png")
const CLIFF_RIGHT: Texture2D = preload("res://assets/placeholders/tiny_swords/terrain/placeholder_cliff_middle_right.png")
const WATER_BACKGROUND: Texture2D = preload("res://assets/placeholders/tiny_swords/terrain/placeholder_water_background.png")
const WATER_FOAM: Texture2D = preload("res://assets/placeholders/tiny_swords/terrain/placeholder_water_foam.png")
const WATER_FOAM_SHADER: Shader = preload("res://shaders/meta/placeholder_water_foam.gdshader")

## Match the proven campus-island scale so the extracted 64px tiles join cleanly.
const TILE_SIZE: float = 2.25
const WATER_MARGIN_TILES: int = 20
const WATER_FOAM_FRAME_COUNT: int = 16
const WATER_FOAM_TILE_SPAN: float = 3.0
const WATER_FOAM_FPS: float = 8.0
const GRASS_TINT: Color = Color(0.78, 0.8, 0.76, 1.0)


func configure(requested_ground_size: Vector2) -> void:
	for child: Node in get_children():
		child.free()

	var column_count: int = maxi(floori(requested_ground_size.x / TILE_SIZE), 3)
	var row_count: int = maxi(floori(requested_ground_size.y / TILE_SIZE), 3)
	var rendered_size: Vector2 = Vector2(column_count, row_count) * TILE_SIZE
	_build_island(rendered_size, column_count, row_count)
	_build_water(rendered_size, column_count, row_count)


func _build_island(rendered_size: Vector2, column_count: int, row_count: int) -> void:
	var island: Node3D = Node3D.new()
	island.name = "Island"
	add_child(island)

	var inner_columns: int = column_count - 2
	var inner_rows: int = row_count - 2
	var inner_size: Vector2 = Vector2(inner_columns, inner_rows) * TILE_SIZE
	_add_plane(island, "Center", inner_size, Vector3.ZERO, GRASS_CENTER, Vector2(inner_columns, inner_rows), false)
	_add_plane(
		island,
		"TopEdge",
		Vector2(inner_size.x, TILE_SIZE),
		Vector3(0.0, 0.0, -rendered_size.y * 0.5 + TILE_SIZE * 0.5),
		GRASS_TOP,
		Vector2(inner_columns, 1.0)
	)
	_add_plane(
		island,
		"BottomEdge",
		Vector2(inner_size.x, TILE_SIZE),
		Vector3(0.0, 0.0, rendered_size.y * 0.5 - TILE_SIZE * 0.5),
		GRASS_BOTTOM,
		Vector2(inner_columns, 1.0)
	)
	_add_plane(
		island,
		"LeftEdge",
		Vector2(TILE_SIZE, inner_size.y),
		Vector3(-rendered_size.x * 0.5 + TILE_SIZE * 0.5, 0.0, 0.0),
		GRASS_LEFT,
		Vector2(1.0, inner_rows)
	)
	_add_plane(
		island,
		"RightEdge",
		Vector2(TILE_SIZE, inner_size.y),
		Vector3(rendered_size.x * 0.5 - TILE_SIZE * 0.5, 0.0, 0.0),
		GRASS_RIGHT,
		Vector2(1.0, inner_rows)
	)
	_add_corner(island, "TopLeftCorner", Vector3(-rendered_size.x * 0.5 + TILE_SIZE * 0.5, 0.0, -rendered_size.y * 0.5 + TILE_SIZE * 0.5), GRASS_TOP_LEFT)
	_add_corner(island, "TopRightCorner", Vector3(rendered_size.x * 0.5 - TILE_SIZE * 0.5, 0.0, -rendered_size.y * 0.5 + TILE_SIZE * 0.5), GRASS_TOP_RIGHT)
	_add_corner(island, "BottomLeftCorner", Vector3(-rendered_size.x * 0.5 + TILE_SIZE * 0.5, 0.0, rendered_size.y * 0.5 - TILE_SIZE * 0.5), GRASS_BOTTOM_LEFT)
	_add_corner(island, "BottomRightCorner", Vector3(rendered_size.x * 0.5 - TILE_SIZE * 0.5, 0.0, rendered_size.y * 0.5 - TILE_SIZE * 0.5), GRASS_BOTTOM_RIGHT)
	_build_front_cliff(island, rendered_size, inner_size.x)


func _add_corner(parent: Node3D, piece_name: String, piece_position: Vector3, texture: Texture2D) -> void:
	_add_plane(parent, piece_name, Vector2.ONE * TILE_SIZE, piece_position, texture, Vector2.ONE)


func _add_plane(
	parent: Node3D,
	piece_name: String,
	piece_size: Vector2,
	piece_position: Vector3,
	texture: Texture2D,
	uv_scale: Vector2,
	use_alpha: bool = true
) -> void:
	var piece: MeshInstance3D = MeshInstance3D.new()
	piece.name = piece_name
	var mesh: PlaneMesh = PlaneMesh.new()
	mesh.size = piece_size
	piece.mesh = mesh
	piece.position = piece_position
	piece.material_override = _make_material(texture, uv_scale, use_alpha)
	parent.add_child(piece)


func _build_front_cliff(parent: Node3D, rendered_size: Vector2, inner_width: float) -> void:
	var y: float = -TILE_SIZE * 0.5
	# The battle camera faces the opposite Z direction from the campus camera,
	# so its visible front edge is the island's negative-Z (top-texture) edge.
	var front_z: float = -rendered_size.y * 0.5
	_add_cliff_piece(parent, "FrontCliffLeft", Vector2.ONE * TILE_SIZE, Vector3(-inner_width * 0.5 - TILE_SIZE * 0.5, y, front_z), CLIFF_LEFT, Vector2.ONE)
	_add_cliff_piece(parent, "FrontCliffCenter", Vector2(inner_width, TILE_SIZE), Vector3(0.0, y, front_z), CLIFF_MIDDLE, Vector2(inner_width / TILE_SIZE, 1.0))
	_add_cliff_piece(parent, "FrontCliffRight", Vector2.ONE * TILE_SIZE, Vector3(inner_width * 0.5 + TILE_SIZE * 0.5, y, front_z), CLIFF_RIGHT, Vector2.ONE)


func _add_cliff_piece(
	parent: Node3D,
	piece_name: String,
	piece_size: Vector2,
	piece_position: Vector3,
	texture: Texture2D,
	uv_scale: Vector2
) -> void:
	var piece: MeshInstance3D = MeshInstance3D.new()
	piece.name = piece_name
	var mesh: QuadMesh = QuadMesh.new()
	mesh.size = piece_size
	piece.mesh = mesh
	piece.position = piece_position
	piece.material_override = _make_material(texture, uv_scale, true)
	parent.add_child(piece)


func _build_water(rendered_size: Vector2, column_count: int, row_count: int) -> void:
	var water: Node3D = Node3D.new()
	water.name = "Water"
	add_child(water)

	var water_level: float = -TILE_SIZE - 0.05
	var water_margin: float = TILE_SIZE * WATER_MARGIN_TILES
	var water_size: Vector2 = rendered_size + Vector2.ONE * water_margin * 2.0
	var surface: MeshInstance3D = MeshInstance3D.new()
	surface.name = "Surface"
	var surface_mesh: PlaneMesh = PlaneMesh.new()
	surface_mesh.size = water_size
	surface.mesh = surface_mesh
	surface.position.y = water_level
	var material: StandardMaterial3D = _make_material(
		WATER_BACKGROUND,
		Vector2(water_size.x / TILE_SIZE, water_size.y / TILE_SIZE),
		false
	)
	material.albedo_color = Color.WHITE
	surface.material_override = material
	water.add_child(surface)

	var foam: Node3D = Node3D.new()
	foam.name = "Foam"
	water.add_child(foam)
	var foam_index: int = 0
	var left: float = -rendered_size.x * 0.5 + TILE_SIZE * 0.5
	var right: float = rendered_size.x * 0.5 - TILE_SIZE * 0.5
	var top: float = -rendered_size.y * 0.5 + TILE_SIZE * 0.5
	var bottom: float = rendered_size.y * 0.5 - TILE_SIZE * 0.5
	for column: int in column_count:
		var x: float = left + column * TILE_SIZE
		_add_foam_piece(foam, "Top%d" % column, Vector3(x, water_level + 0.02, top), foam_index)
		foam_index += 1
		_add_foam_piece(foam, "Bottom%d" % column, Vector3(x, water_level + 0.02, bottom), foam_index)
		foam_index += 1
	for row: int in range(1, row_count - 1):
		var z: float = top + row * TILE_SIZE
		_add_foam_piece(foam, "Left%d" % row, Vector3(left, water_level + 0.02, z), foam_index)
		foam_index += 1
		_add_foam_piece(foam, "Right%d" % row, Vector3(right, water_level + 0.02, z), foam_index)
		foam_index += 1


func _add_foam_piece(parent: Node3D, piece_name: String, piece_position: Vector3, animation_offset: int) -> void:
	var piece: MeshInstance3D = MeshInstance3D.new()
	piece.name = piece_name
	var mesh: PlaneMesh = PlaneMesh.new()
	mesh.size = Vector2.ONE * TILE_SIZE * WATER_FOAM_TILE_SPAN
	piece.mesh = mesh
	piece.position = piece_position
	var material: ShaderMaterial = ShaderMaterial.new()
	material.shader = WATER_FOAM_SHADER
	material.set_shader_parameter("foam_texture", WATER_FOAM)
	material.set_shader_parameter("animation_fps", WATER_FOAM_FPS)
	material.set_shader_parameter("animation_offset", float(animation_offset % WATER_FOAM_FRAME_COUNT))
	piece.material_override = material
	parent.add_child(piece)


func _make_material(texture: Texture2D, uv_scale: Vector2, use_alpha: bool) -> StandardMaterial3D:
	var material: StandardMaterial3D = StandardMaterial3D.new()
	material.albedo_color = GRASS_TINT
	material.albedo_texture = texture
	material.texture_filter = BaseMaterial3D.TEXTURE_FILTER_NEAREST
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.cull_mode = BaseMaterial3D.CULL_DISABLED
	material.uv1_scale = Vector3(uv_scale.x, uv_scale.y, 1.0)
	if use_alpha:
		material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA_SCISSOR
		material.alpha_scissor_threshold = 0.5
	return material
