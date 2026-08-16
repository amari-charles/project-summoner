extends Node3D
class_name SpawnZoneOverlay

## Visual overlay showing invalid spawn zone on the battlefield
## Displayed while dragging summon cards to indicate where units cannot be placed

const INVALID_COLOR: Color = Color(0.8, 0.2, 0.2, 0.25)  # Red, semi-transparent
const RANGE_COLOR: Color = Color(0.2, 0.75, 1.0, 0.2)

var invalid_zone: MeshInstance3D

func _ready() -> void:
	_create_invalid_zone()

func show_team_half() -> void:
	if is_instance_valid(invalid_zone):
		invalid_zone.queue_free()
	_create_invalid_zone()

func show_card_range(center: Vector3, radius: float) -> void:
	if is_instance_valid(invalid_zone):
		invalid_zone.queue_free()

	invalid_zone = MeshInstance3D.new()
	var disc: CylinderMesh = CylinderMesh.new()
	disc.top_radius = maxf(radius, 0.0)
	disc.bottom_radius = maxf(radius, 0.0)
	disc.height = 0.02
	disc.radial_segments = 64
	invalid_zone.mesh = disc

	var material: StandardMaterial3D = StandardMaterial3D.new()
	material.albedo_color = RANGE_COLOR
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	material.cull_mode = BaseMaterial3D.CULL_DISABLED
	invalid_zone.material_override = material
	invalid_zone.position = Vector3(center.x, BattlefieldConstants.GROUND_OVERLAY_OFFSET, center.z)
	add_child(invalid_zone)

func _create_invalid_zone() -> void:
	invalid_zone = MeshInstance3D.new()

	var plane: PlaneMesh = PlaneMesh.new()
	plane.size = Vector2(
		BattlefieldConstants.BATTLEFIELD_HALF_WIDTH,
		BattlefieldConstants.BATTLEFIELD_HALF_DEPTH * 2
	)
	invalid_zone.mesh = plane

	var material: StandardMaterial3D = StandardMaterial3D.new()
	material.albedo_color = INVALID_COLOR
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	material.cull_mode = BaseMaterial3D.CULL_DISABLED
	invalid_zone.material_override = material

	# Position at center of enemy's half (X > 0)
	invalid_zone.position = Vector3(
		BattlefieldConstants.BATTLEFIELD_HALF_WIDTH / 2,
		BattlefieldConstants.GROUND_OVERLAY_OFFSET,
		0
	)
	add_child(invalid_zone)

func cleanup() -> void:
	queue_free()
