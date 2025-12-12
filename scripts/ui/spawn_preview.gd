extends Node3D
class_name SpawnPreview

## Visual preview showing where a unit will spawn during card drag
## Shows a circle marker at the spawn position (ghost unit can be added later for 3D models)

const VALID_COLOR: Color = Color(0.3, 0.7, 1.0, 0.5)  # Light blue, semi-transparent (valid spawn)
const INVALID_COLOR: Color = Color(1.0, 0.3, 0.3, 0.5)  # Red, semi-transparent (invalid spawn)
const CIRCLE_HEIGHT: float = 0.05

var circle_marker: MeshInstance3D = null
var _collision_radius: float = 0.5
var _is_valid: bool = true

func _ready() -> void:
	_create_circle_marker()

## Initialize preview with a unit scene (extracts collision_radius for circle scaling)
func setup(unit_scene: PackedScene) -> void:
	if unit_scene:
		# Get collision_radius from scene to scale circle appropriately
		var temp_unit: Node = unit_scene.instantiate()
		if temp_unit and "collision_radius" in temp_unit:
			_collision_radius = temp_unit.get("collision_radius")
		if temp_unit:
			temp_unit.queue_free()

		# Update circle size based on unit
		_update_circle_size()

## Update the preview position
func update_position(pos: Vector3) -> void:
	global_position = pos

## Set whether the spawn position is valid (changes color)
func set_valid(is_valid: bool) -> void:
	if _is_valid == is_valid:
		return
	_is_valid = is_valid
	_update_material_color()

## Update material color based on validity
func _update_material_color() -> void:
	if circle_marker and circle_marker.material_override:
		var mat: StandardMaterial3D = circle_marker.material_override as StandardMaterial3D
		if mat:
			mat.albedo_color = VALID_COLOR if _is_valid else INVALID_COLOR

## Create the circle marker mesh
func _create_circle_marker() -> void:
	circle_marker = MeshInstance3D.new()
	_update_circle_size()
	add_child(circle_marker)

## Update circle mesh size based on collision_radius
func _update_circle_size() -> void:
	if not circle_marker:
		return

	# Circle shows the collision radius (actual footprint of unit)
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
	if circle_marker:
		circle_marker.queue_free()
		circle_marker = null
	queue_free()
