extends Node3D
class_name SpawnPreview

## Visual preview showing where units will spawn during card drag
## Uses ghost units to show exactly what will spawn

const VALID_COLOR: Color = Color(0.3, 0.7, 1.0, 0.5)  # Light blue, semi-transparent (valid spawn)
const INVALID_COLOR: Color = Color(1.0, 0.3, 0.3, 0.5)  # Red, semi-transparent (invalid spawn)
const CIRCLE_HEIGHT: float = 0.05

var ghost_units: Array[GhostUnit3D] = []
var circle_marker: MeshInstance3D = null  # Fallback if ghost creation fails
var _collision_radius: float = 0.5
var _is_valid: bool = true
var _spawn_count: int = 1
var _unit_scene: PackedScene = null


func _ready() -> void:
	# Don't create circle by default - wait for setup()
	pass


## Initialize preview with unit scene and spawn count
func setup(unit_scene: PackedScene, spawn_count: int = 1) -> void:
	_unit_scene = unit_scene
	_spawn_count = spawn_count

	if unit_scene:
		# Extract collision_radius for spacing/fallback
		var temp_unit: Node = unit_scene.instantiate()
		if temp_unit and "collision_radius" in temp_unit:
			_collision_radius = temp_unit.get("collision_radius")
		if temp_unit:
			temp_unit.queue_free()

	# Create ghost units for preview
	_create_ghost_units()


## Create ghost unit previews in formation
func _create_ghost_units() -> void:
	# Clear existing ghosts
	for ghost: GhostUnit3D in ghost_units:
		if is_instance_valid(ghost):
			ghost.queue_free()
	ghost_units.clear()

	if not _unit_scene:
		# Fallback to circle marker if no unit scene
		_create_circle_marker()
		return

	# Create ghost for each unit in formation
	var any_ghost_valid: bool = false
	for i: int in _spawn_count:
		var ghost: GhostUnit3D = GhostUnit3D.new()
		add_child(ghost)
		ghost.setup(_unit_scene)
		ghost.set_valid(_is_valid)
		ghost_units.append(ghost)
		if ghost.has_visual():
			any_ghost_valid = true

	# If no ghosts have visuals, fallback to circle marker
	if not any_ghost_valid:
		# Clean up empty ghosts
		for ghost: GhostUnit3D in ghost_units:
			ghost.queue_free()
		ghost_units.clear()
		_create_circle_marker()
		return

	# Position in formation
	_update_formation_positions()


## Update formation positions based on spawn count
func _update_formation_positions() -> void:
	for i: int in ghost_units.size():
		var offset: Vector3 = Card.generate_formation_offset(i, _spawn_count)
		ghost_units[i].position = offset


## Update the preview position
func update_position(pos: Vector3) -> void:
	global_position = pos


## Set whether the spawn position is valid (changes color)
func set_valid(is_valid: bool) -> void:
	if _is_valid == is_valid:
		return
	_is_valid = is_valid

	# Update all ghost units
	for ghost: GhostUnit3D in ghost_units:
		if is_instance_valid(ghost):
			ghost.set_valid(is_valid)

	# Update fallback circle if used
	_update_material_color()


## Update material color based on validity (for fallback circle)
func _update_material_color() -> void:
	if circle_marker and circle_marker.material_override:
		var mat: StandardMaterial3D = circle_marker.material_override as StandardMaterial3D
		if mat:
			mat.albedo_color = VALID_COLOR if _is_valid else INVALID_COLOR


## Create the circle marker mesh (fallback)
func _create_circle_marker() -> void:
	if circle_marker:
		return  # Already exists

	circle_marker = MeshInstance3D.new()
	_update_circle_size()
	add_child(circle_marker)


## Update circle mesh size based on collision_radius
func _update_circle_size() -> void:
	if not circle_marker:
		return

	var radius: float = _collision_radius

	# Create a flat cylinder for the circle
	var cylinder: CylinderMesh = CylinderMesh.new()
	cylinder.top_radius = radius
	cylinder.bottom_radius = radius
	cylinder.height = CIRCLE_HEIGHT
	circle_marker.mesh = cylinder

	# Create semi-transparent material
	var material: StandardMaterial3D = StandardMaterial3D.new()
	material.albedo_color = VALID_COLOR if _is_valid else INVALID_COLOR
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	material.cull_mode = BaseMaterial3D.CULL_DISABLED
	circle_marker.material_override = material

	# Position slightly above ground to avoid z-fighting
	circle_marker.position.y = CIRCLE_HEIGHT / 2 + BattlefieldConstants.GROUND_OVERLAY_OFFSET


## Clean up resources
func cleanup() -> void:
	for ghost: GhostUnit3D in ghost_units:
		if is_instance_valid(ghost):
			ghost.cleanup()
	ghost_units.clear()

	if circle_marker:
		circle_marker.queue_free()
		circle_marker = null

	queue_free()
