extends Node3D
class_name SpawnZoneOverlay

## Visual overlay showing valid/invalid spawn zones on the battlefield
## Displayed while dragging summon cards to indicate where units can be placed

const VALID_COLOR: Color = Color(0.2, 0.6, 0.3, 0.25)  # Green, semi-transparent
const INVALID_COLOR: Color = Color(0.8, 0.2, 0.2, 0.25)  # Red, semi-transparent
const OVERLAY_Y: float = 0.02  # Slightly above ground to avoid z-fighting

var valid_zone: MeshInstance3D
var invalid_zone: MeshInstance3D

func _ready() -> void:
	_create_zones()

func _create_zones() -> void:
	# Each zone is 50 units wide (half battlefield) x 80 units tall
	# Battlefield is -50 to +50 on X, -40 to +40 on Z
	var half_width: float = 50.0
	var full_height: float = 80.0

	# Valid zone (player's half, X < 0)
	valid_zone = _create_zone_mesh(VALID_COLOR)
	valid_zone.position = Vector3(-half_width / 2, OVERLAY_Y, 0)  # Center at X = -25
	add_child(valid_zone)

	# Invalid zone (enemy's half, X > 0)
	invalid_zone = _create_zone_mesh(INVALID_COLOR)
	invalid_zone.position = Vector3(half_width / 2, OVERLAY_Y, 0)  # Center at X = +25
	add_child(invalid_zone)

func _create_zone_mesh(color: Color) -> MeshInstance3D:
	var mesh_instance := MeshInstance3D.new()
	var plane := PlaneMesh.new()
	plane.size = Vector2(50.0, 80.0)  # Half battlefield width, full height
	mesh_instance.mesh = plane

	var material := StandardMaterial3D.new()
	material.albedo_color = color
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	material.cull_mode = BaseMaterial3D.CULL_DISABLED
	mesh_instance.material_override = material

	return mesh_instance

func cleanup() -> void:
	queue_free()
