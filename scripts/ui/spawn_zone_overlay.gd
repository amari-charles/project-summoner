extends Node3D
class_name SpawnZoneOverlay

## Visual overlay showing invalid spawn zone on the battlefield
## Displayed while dragging summon cards to indicate where units cannot be placed

const INVALID_COLOR: Color = Color(0.8, 0.2, 0.2, 0.25)  # Red, semi-transparent
const OVERLAY_Y: float = 0.02  # Slightly above ground to avoid z-fighting

var invalid_zone: MeshInstance3D

func _ready() -> void:
	_create_invalid_zone()

func _create_invalid_zone() -> void:
	invalid_zone = MeshInstance3D.new()

	var plane := PlaneMesh.new()
	plane.size = Vector2(
		BattlefieldConstants.BATTLEFIELD_HALF_WIDTH,
		BattlefieldConstants.BATTLEFIELD_HALF_DEPTH * 2
	)
	invalid_zone.mesh = plane

	var material := StandardMaterial3D.new()
	material.albedo_color = INVALID_COLOR
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	material.cull_mode = BaseMaterial3D.CULL_DISABLED
	invalid_zone.material_override = material

	# Position at center of enemy's half (X > 0)
	invalid_zone.position = Vector3(
		BattlefieldConstants.BATTLEFIELD_HALF_WIDTH / 2,
		OVERLAY_Y,
		0
	)
	add_child(invalid_zone)

func cleanup() -> void:
	queue_free()
