extends Node3D
class_name SpawnPreview

## Visual preview showing where a unit will spawn during card drag
## Shows a circle marker at the spawn position (ghost unit can be added later for 3D models)

const CIRCLE_COLOR: Color = Color(0.3, 0.7, 1.0, 0.5)  # Light blue, semi-transparent
const CIRCLE_RADIUS: float = 0.8
const CIRCLE_HEIGHT: float = 0.05

var circle_marker: MeshInstance3D = null
var _unit_size: float = 1.0

func _ready() -> void:
	_create_circle_marker()

## Initialize preview with a unit scene (extracts unit_size for circle scaling)
func setup(unit_scene: PackedScene) -> void:
	if unit_scene:
		# Get unit_size from scene to scale circle appropriately
		var temp_unit: Node = unit_scene.instantiate()
		if temp_unit and "unit_size" in temp_unit:
			_unit_size = temp_unit.get("unit_size")
		if temp_unit:
			temp_unit.queue_free()

		# Update circle size based on unit
		_update_circle_size()

## Update the preview position
func update_position(pos: Vector3) -> void:
	global_position = pos

## Create the circle marker mesh
func _create_circle_marker() -> void:
	circle_marker = MeshInstance3D.new()
	_update_circle_size()
	add_child(circle_marker)

## Update circle mesh size based on unit_size
func _update_circle_size() -> void:
	if not circle_marker:
		return

	var radius: float = CIRCLE_RADIUS * _unit_size

	# Create a flat cylinder for the circle
	var cylinder: CylinderMesh = CylinderMesh.new()
	cylinder.top_radius = radius
	cylinder.bottom_radius = radius
	cylinder.height = CIRCLE_HEIGHT
	circle_marker.mesh = cylinder

	# Create semi-transparent material
	var material: StandardMaterial3D = StandardMaterial3D.new()
	material.albedo_color = CIRCLE_COLOR
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	material.cull_mode = BaseMaterial3D.CULL_DISABLED
	circle_marker.material_override = material

	# Position slightly above ground to avoid z-fighting
	circle_marker.position.y = CIRCLE_HEIGHT / 2 + 0.01

## Clean up resources
func cleanup() -> void:
	if circle_marker:
		circle_marker.queue_free()
		circle_marker = null
	queue_free()
