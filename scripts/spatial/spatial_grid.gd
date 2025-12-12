extends Node
# SpatialGrid is registered as an autoload, no class_name needed

## Spatial hash grid for efficient proximity queries
##
## Replaces O(n²) unit iteration with O(k) where k = nearby units.
## Used by Unit3D for targeting, separation, and overlap correction.
##
## Grid covers the battlefield (100x80 units) with 10x10 cells = 80 cells total.
## Units register on spawn, unregister on death, and update position when moving.
##
## Debug visualization: Toggle with F11 (only in debug builds)

## ============================================================================
## CONFIGURATION
## ============================================================================

## Cell size in world units (10x10 for 100x80 battlefield)
const CELL_SIZE: float = 10.0

## Battlefield bounds (from BattlefieldConstants)
const GRID_MIN_X: float = -50.0
const GRID_MAX_X: float = 50.0
const GRID_MIN_Z: float = -40.0
const GRID_MAX_Z: float = 40.0

## Grid dimensions
const GRID_WIDTH: int = 10   # (50 - -50) / 10 = 10 cells
const GRID_HEIGHT: int = 8   # (40 - -40) / 10 = 8 cells

## Minimum movement before re-registering unit (prevents cell thrashing)
const POSITION_UPDATE_THRESHOLD: float = 2.0
const POSITION_UPDATE_THRESHOLD_SQ: float = 4.0  # Pre-computed squared

## ============================================================================
## DATA STRUCTURES
## ============================================================================

## The grid: Array of Arrays (cells), each cell contains unit references
## Access: _grid[cell_index] where cell_index = z_index * GRID_WIDTH + x_index
var _grid: Array[Array] = []

## Reverse lookup: Unit instance_id -> {cell: int, pos: Vector3}
var _unit_registry: Dictionary = {}

## Debug statistics (reset each frame)
var _stats_queries_this_frame: int = 0
var _stats_cell_updates_this_frame: int = 0

## Debug visualization
var _debug_enabled: bool = false
var _debug_canvas: CanvasLayer = null
var _debug_panel: PanelContainer = null
var _debug_stats_label: Label = null
var _debug_mesh: ImmediateMesh = null
var _debug_mesh_instance: MeshInstance3D = null


## ============================================================================
## LIFECYCLE
## ============================================================================

func _ready() -> void:
	# Initialize empty grid
	_grid.clear()
	for i: int in range(GRID_WIDTH * GRID_HEIGHT):
		_grid.append([])

	print("[SpatialGrid] Initialized %dx%d grid (%d cells)" % [GRID_WIDTH, GRID_HEIGHT, _grid.size()])

	# Setup debug visualization (only in debug builds)
	if OS.is_debug_build():
		call_deferred("_setup_debug_ui")


func _process(_delta: float) -> void:
	# Update debug stats display
	if _debug_enabled and _debug_stats_label:
		_update_debug_stats()

	# Reset frame stats
	_stats_queries_this_frame = 0
	_stats_cell_updates_this_frame = 0


func _input(event: InputEvent) -> void:
	if not OS.is_debug_build():
		return

	if not event is InputEventKey:
		return

	var key_event: InputEventKey = event as InputEventKey
	if not key_event.pressed or key_event.echo:
		return

	if key_event.keycode == KEY_F11:
		_toggle_debug()


## ============================================================================
## REGISTRATION API
## ============================================================================

## Register a unit with the spatial grid
## Called from Unit3D._ready() after adding to groups
func register_unit(unit: Node3D) -> void:
	if not is_instance_valid(unit):
		return

	var instance_id: int = unit.get_instance_id()

	# Already registered?
	if _unit_registry.has(instance_id):
		return

	var pos: Vector3 = unit.global_position
	var cell_idx: int = _get_cell_index(pos)

	# Add to grid cell
	if cell_idx >= 0 and cell_idx < _grid.size():
		_grid[cell_idx].append(unit)

	# Add to registry
	_unit_registry[instance_id] = {
		"cell": cell_idx,
		"pos": pos
	}


## Unregister a unit from the spatial grid
## Called from Unit3D._die() or queue_free()
func unregister_unit(unit: Node3D) -> void:
	if not is_instance_valid(unit):
		return

	var instance_id: int = unit.get_instance_id()
	if not _unit_registry.has(instance_id):
		return

	var data: Dictionary = _unit_registry[instance_id]
	var cell_idx: int = data.cell

	# Remove from grid cell
	if cell_idx >= 0 and cell_idx < _grid.size():
		_grid[cell_idx].erase(unit)

	# Remove from registry
	_unit_registry.erase(instance_id)


## Update a unit's position in the grid if it moved significantly
## Returns true if cell changed, false otherwise
func update_unit_position(unit: Node3D) -> bool:
	if not is_instance_valid(unit):
		return false

	var instance_id: int = unit.get_instance_id()
	if not _unit_registry.has(instance_id):
		# Unit not registered, register it now
		register_unit(unit)
		return true

	var data: Dictionary = _unit_registry[instance_id]
	var old_pos: Vector3 = data.pos
	var new_pos: Vector3 = unit.global_position

	# Check if moved enough to warrant update (squared distance on XZ plane)
	var delta: Vector3 = new_pos - old_pos
	var dist_sq: float = delta.x * delta.x + delta.z * delta.z
	if dist_sq < POSITION_UPDATE_THRESHOLD_SQ:
		return false

	# Calculate new cell
	var old_cell: int = data.cell
	var new_cell: int = _get_cell_index(new_pos)

	# Update registry position
	data.pos = new_pos

	# If cell changed, move unit between cells
	if old_cell != new_cell:
		if old_cell >= 0 and old_cell < _grid.size():
			_grid[old_cell].erase(unit)
		if new_cell >= 0 and new_cell < _grid.size():
			_grid[new_cell].append(unit)
		data.cell = new_cell
		_stats_cell_updates_this_frame += 1
		return true

	return false


## Clear all units from the grid (for battle end/scene transitions)
func clear() -> void:
	for cell: Array in _grid:
		cell.clear()
	_unit_registry.clear()


## ============================================================================
## QUERY API
## ============================================================================

## Get all units within a radius of a position
## @param position: Center of the query circle
## @param radius: Query radius in world units
## @param team_filter: Team to filter by (-1 = all teams, 0 = player, 1 = enemy)
## @param exclude_unit: Optional unit to exclude from results (typically self)
## @return Array of Unit3D references
func get_units_in_radius(
	position: Vector3,
	radius: float,
	team_filter: int = -1,
	exclude_unit: Node3D = null
) -> Array[Node3D]:
	_stats_queries_this_frame += 1
	var result: Array[Node3D] = []
	var radius_sq: float = radius * radius

	# Get all cells that could contain units within radius
	var cells: Array[int] = _get_cells_in_radius(position, radius)

	for cell_idx: int in cells:
		if cell_idx < 0 or cell_idx >= _grid.size():
			continue

		var cell: Array = _grid[cell_idx]
		for unit_variant: Variant in cell:
			if not unit_variant is Node3D:
				continue
			var unit: Node3D = unit_variant

			# Skip excluded unit
			if unit == exclude_unit:
				continue

			# Skip invalid units
			if not is_instance_valid(unit):
				continue

			# Check if unit is alive (Unit3D has is_alive property)
			if "is_alive" in unit and not unit.get("is_alive"):
				continue

			# Team filter (Unit3D has team property: 0 = player, 1 = enemy)
			if team_filter >= 0 and "team" in unit:
				var unit_team: int = unit.get("team")
				if unit_team != team_filter:
					continue

			# Distance check (2D on XZ plane)
			var delta: Vector3 = unit.global_position - position
			var dist_sq: float = delta.x * delta.x + delta.z * delta.z
			if dist_sq <= radius_sq:
				result.append(unit)

	return result


## ============================================================================
## COORDINATE HELPERS
## ============================================================================

## Convert world X to cell X index
func _world_to_cell_x(world_x: float) -> int:
	return int((world_x - GRID_MIN_X) / CELL_SIZE)


## Convert world Z to cell Z index
func _world_to_cell_z(world_z: float) -> int:
	return int((world_z - GRID_MIN_Z) / CELL_SIZE)


## Get cell index from world position
func _get_cell_index(position: Vector3) -> int:
	var cell_x: int = _world_to_cell_x(position.x)
	var cell_z: int = _world_to_cell_z(position.z)

	# Clamp to grid bounds
	cell_x = clampi(cell_x, 0, GRID_WIDTH - 1)
	cell_z = clampi(cell_z, 0, GRID_HEIGHT - 1)

	return cell_z * GRID_WIDTH + cell_x


## Get all cell indices that overlap with a circle query
func _get_cells_in_radius(position: Vector3, radius: float) -> Array[int]:
	var cells: Array[int] = []

	# Calculate bounding box of query circle
	var min_x: float = position.x - radius
	var max_x: float = position.x + radius
	var min_z: float = position.z - radius
	var max_z: float = position.z + radius

	# Convert to cell indices
	var start_x: int = _world_to_cell_x(min_x)
	var end_x: int = _world_to_cell_x(max_x)
	var start_z: int = _world_to_cell_z(min_z)
	var end_z: int = _world_to_cell_z(max_z)

	# Clamp to grid bounds
	start_x = clampi(start_x, 0, GRID_WIDTH - 1)
	end_x = clampi(end_x, 0, GRID_WIDTH - 1)
	start_z = clampi(start_z, 0, GRID_HEIGHT - 1)
	end_z = clampi(end_z, 0, GRID_HEIGHT - 1)

	# Collect all cells in range
	for z: int in range(start_z, end_z + 1):
		for x: int in range(start_x, end_x + 1):
			cells.append(z * GRID_WIDTH + x)

	return cells


## ============================================================================
## DEBUG VISUALIZATION
## ============================================================================

func _setup_debug_ui() -> void:
	# Create CanvasLayer for 2D UI overlay
	_debug_canvas = CanvasLayer.new()
	_debug_canvas.layer = 99  # Below FPS tool (100)
	add_child(_debug_canvas)

	# Create panel
	_debug_panel = PanelContainer.new()
	_debug_panel.position = Vector2(10, 200)  # Below FPS tool
	_debug_panel.visible = false
	_debug_canvas.add_child(_debug_panel)

	# Style the panel
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = Color(0.1, 0.1, 0.15, 0.85)
	style.corner_radius_top_left = 8
	style.corner_radius_top_right = 8
	style.corner_radius_bottom_left = 8
	style.corner_radius_bottom_right = 8
	style.content_margin_left = 12
	style.content_margin_right = 12
	style.content_margin_top = 8
	style.content_margin_bottom = 8
	_debug_panel.add_theme_stylebox_override("panel", style)

	# Create content
	var vbox: VBoxContainer = VBoxContainer.new()
	vbox.add_theme_constant_override("separation", 4)
	_debug_panel.add_child(vbox)

	# Title
	var title: Label = Label.new()
	title.text = "Spatial Grid"
	title.add_theme_font_size_override("font_size", 14)
	title.add_theme_color_override("font_color", Color(0.9, 0.9, 0.9))
	vbox.add_child(title)

	# Stats label
	_debug_stats_label = Label.new()
	_debug_stats_label.text = "Units: 0\nQueries: 0\nCell updates: 0"
	_debug_stats_label.add_theme_font_size_override("font_size", 12)
	_debug_stats_label.add_theme_color_override("font_color", Color(0.7, 0.9, 0.7))
	vbox.add_child(_debug_stats_label)

	# Instructions
	var instructions: Label = Label.new()
	instructions.text = "F11 to toggle"
	instructions.add_theme_font_size_override("font_size", 10)
	instructions.add_theme_color_override("font_color", Color(0.5, 0.5, 0.5))
	vbox.add_child(instructions)

	# Create 3D mesh for grid visualization
	_debug_mesh = ImmediateMesh.new()
	_debug_mesh_instance = MeshInstance3D.new()
	_debug_mesh_instance.mesh = _debug_mesh
	_debug_mesh_instance.visible = false

	# Create unshaded material for grid lines
	var material: StandardMaterial3D = StandardMaterial3D.new()
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.vertex_color_use_as_albedo = true
	material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	_debug_mesh_instance.material_override = material


func _toggle_debug() -> void:
	_debug_enabled = not _debug_enabled

	if _debug_panel:
		_debug_panel.visible = _debug_enabled

	if _debug_mesh_instance:
		_debug_mesh_instance.visible = _debug_enabled

		# Add/remove 3D mesh from scene tree
		if _debug_enabled:
			var viewport: Viewport = get_viewport()
			if viewport:
				var root: Node = viewport.get_tree().current_scene
				if root and not _debug_mesh_instance.is_inside_tree():
					root.add_child(_debug_mesh_instance)
		else:
			if _debug_mesh_instance.is_inside_tree():
				_debug_mesh_instance.get_parent().remove_child(_debug_mesh_instance)

	print("[SpatialGrid] Debug visualization %s" % ("enabled" if _debug_enabled else "disabled"))


func _update_debug_stats() -> void:
	# Calculate grid statistics
	var total_units: int = _unit_registry.size()
	var max_cell_population: int = 0
	var non_empty_cells: int = 0

	for cell: Array in _grid:
		var size: int = cell.size()
		if size > 0:
			non_empty_cells += 1
		if size > max_cell_population:
			max_cell_population = size

	_debug_stats_label.text = "Units: %d\nCells used: %d/%d\nMax/cell: %d\nQueries/frame: %d\nCell updates: %d" % [
		total_units,
		non_empty_cells,
		_grid.size(),
		max_cell_population,
		_stats_queries_this_frame,
		_stats_cell_updates_this_frame
	]

	# Update 3D grid visualization
	_draw_debug_grid()


func _draw_debug_grid() -> void:
	if not _debug_mesh or not _debug_mesh_instance.visible:
		return

	_debug_mesh.clear_surfaces()
	_debug_mesh.surface_begin(Mesh.PRIMITIVE_LINES)

	var grid_y: float = 0.05  # Slightly above ground

	# Draw grid lines
	for x: int in range(GRID_WIDTH + 1):
		var world_x: float = GRID_MIN_X + x * CELL_SIZE
		_debug_mesh.surface_set_color(Color(0.3, 0.5, 0.3, 0.4))
		_debug_mesh.surface_add_vertex(Vector3(world_x, grid_y, GRID_MIN_Z))
		_debug_mesh.surface_add_vertex(Vector3(world_x, grid_y, GRID_MAX_Z))

	for z: int in range(GRID_HEIGHT + 1):
		var world_z: float = GRID_MIN_Z + z * CELL_SIZE
		_debug_mesh.surface_set_color(Color(0.3, 0.5, 0.3, 0.4))
		_debug_mesh.surface_add_vertex(Vector3(GRID_MIN_X, grid_y, world_z))
		_debug_mesh.surface_add_vertex(Vector3(GRID_MAX_X, grid_y, world_z))

	# Draw cell population indicators (brighter = more units)
	for z: int in range(GRID_HEIGHT):
		for x: int in range(GRID_WIDTH):
			var cell_idx: int = z * GRID_WIDTH + x
			var population: int = _grid[cell_idx].size()

			if population > 0:
				var intensity: float = min(population / 5.0, 1.0)  # Cap at 5 units
				var color: Color = Color(0.2, 0.8, 0.2, 0.3 + intensity * 0.4)

				# Draw cell center marker
				var center_x: float = GRID_MIN_X + (x + 0.5) * CELL_SIZE
				var center_z: float = GRID_MIN_Z + (z + 0.5) * CELL_SIZE
				var marker_size: float = 0.5 + intensity * 1.5

				_debug_mesh.surface_set_color(color)
				_debug_mesh.surface_add_vertex(Vector3(center_x - marker_size, grid_y, center_z))
				_debug_mesh.surface_add_vertex(Vector3(center_x + marker_size, grid_y, center_z))
				_debug_mesh.surface_add_vertex(Vector3(center_x, grid_y, center_z - marker_size))
				_debug_mesh.surface_add_vertex(Vector3(center_x, grid_y, center_z + marker_size))

	_debug_mesh.surface_end()


## ============================================================================
## STATISTICS (for profiling)
## ============================================================================

## Get debug statistics dictionary
func get_stats() -> Dictionary:
	var total_units: int = 0
	var max_cell_population: int = 0
	var non_empty_cells: int = 0

	for cell: Array in _grid:
		var size: int = cell.size()
		total_units += size
		if size > 0:
			non_empty_cells += 1
		if size > max_cell_population:
			max_cell_population = size

	return {
		"total_units": total_units,
		"registered_units": _unit_registry.size(),
		"non_empty_cells": non_empty_cells,
		"max_cell_population": max_cell_population,
		"queries_this_frame": _stats_queries_this_frame,
		"cell_updates_this_frame": _stats_cell_updates_this_frame
	}
