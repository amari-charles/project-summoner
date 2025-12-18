extends Node3D
class_name SpellPreview

## Visual preview for spell targeting during card drag
## Shows targeting reticle and radius indicator

const VALID_COLOR: Color = Color(0.3, 0.7, 1.0, 0.6)  # Cyan, 60% alpha
const INVALID_COLOR: Color = Color(1.0, 0.3, 0.3, 0.6)  # Red, 60% alpha
const DEFAULT_RADIUS: float = 5.0  # Default spell radius when card has none specified
const CROSSHAIR_LENGTH: float = 1.5
const LINE_WIDTH: float = 0.08
const RING_WIDTH: float = 0.15
const CROSSHAIR_Y_OFFSET: float = 0.05  # Extra height above ground overlay for crosshair
const DOT_Y_OFFSET: float = 0.1  # Extra height for center dot (above crosshair)
const DOT_RADIUS: float = 0.15

var radius_ring: MeshInstance3D = null
var crosshair_h: MeshInstance3D = null
var crosshair_v: MeshInstance3D = null
var center_dot: MeshInstance3D = null
var _spell_radius: float = 5.0
var _is_valid: bool = true


## Initialize with spell data
func setup(spell_radius: float = 5.0) -> void:
	_spell_radius = spell_radius
	_create_radius_ring()
	_create_crosshair()
	_create_center_dot()


## Create radius indicator ring using a torus
func _create_radius_ring() -> void:
	if _spell_radius <= 0:
		return

	radius_ring = MeshInstance3D.new()

	# Use a torus for a ring effect
	var torus: TorusMesh = TorusMesh.new()
	torus.inner_radius = _spell_radius - RING_WIDTH
	torus.outer_radius = _spell_radius
	radius_ring.mesh = torus

	# Rotate to lie flat on ground
	radius_ring.rotation_degrees.x = 90

	var material: StandardMaterial3D = _create_material()
	radius_ring.material_override = material

	radius_ring.position.y = BattlefieldConstants.GROUND_OVERLAY_OFFSET
	add_child(radius_ring)


## Create crosshair at center
func _create_crosshair() -> void:
	# Horizontal line (X-axis)
	crosshair_h = MeshInstance3D.new()
	var box_h: BoxMesh = BoxMesh.new()
	box_h.size = Vector3(CROSSHAIR_LENGTH, LINE_WIDTH, LINE_WIDTH)
	crosshair_h.mesh = box_h
	crosshair_h.material_override = _create_material()
	crosshair_h.position.y = BattlefieldConstants.GROUND_OVERLAY_OFFSET + CROSSHAIR_Y_OFFSET
	add_child(crosshair_h)

	# Vertical line (Z-axis)
	crosshair_v = MeshInstance3D.new()
	var box_v: BoxMesh = BoxMesh.new()
	box_v.size = Vector3(LINE_WIDTH, LINE_WIDTH, CROSSHAIR_LENGTH)
	crosshair_v.mesh = box_v
	crosshair_v.material_override = _create_material()
	crosshair_v.position.y = BattlefieldConstants.GROUND_OVERLAY_OFFSET + CROSSHAIR_Y_OFFSET
	add_child(crosshair_v)


## Create center dot
func _create_center_dot() -> void:
	center_dot = MeshInstance3D.new()
	var sphere: SphereMesh = SphereMesh.new()
	sphere.radius = DOT_RADIUS
	sphere.height = DOT_RADIUS * 2
	center_dot.mesh = sphere
	center_dot.material_override = _create_material()
	center_dot.position.y = BattlefieldConstants.GROUND_OVERLAY_OFFSET + DOT_Y_OFFSET
	add_child(center_dot)


## Create standard material for preview elements
func _create_material() -> StandardMaterial3D:
	var material: StandardMaterial3D = StandardMaterial3D.new()
	material.albedo_color = VALID_COLOR if _is_valid else INVALID_COLOR
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	material.cull_mode = BaseMaterial3D.CULL_DISABLED
	return material


## Update position
func update_position(pos: Vector3) -> void:
	global_position = pos


## Set validity (changes color)
func set_valid(is_valid: bool) -> void:
	if _is_valid == is_valid:
		return
	_is_valid = is_valid

	var color: Color = VALID_COLOR if is_valid else INVALID_COLOR

	if radius_ring and radius_ring.material_override:
		var mat: StandardMaterial3D = radius_ring.material_override
		mat.albedo_color = color

	if crosshair_h and crosshair_h.material_override:
		var mat: StandardMaterial3D = crosshair_h.material_override
		mat.albedo_color = color

	if crosshair_v and crosshair_v.material_override:
		var mat: StandardMaterial3D = crosshair_v.material_override
		mat.albedo_color = color

	if center_dot and center_dot.material_override:
		var mat: StandardMaterial3D = center_dot.material_override
		mat.albedo_color = color


## Cleanup
func cleanup() -> void:
	queue_free()
