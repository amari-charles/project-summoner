extends Node3D
class_name RedirectIndicator

## Visual feedback for redirect system
## Shows selection circle and drag arrow during redirect operations

## Selection circle (shows affected area)
var selection_circle: MeshInstance3D = null

## Drag arrow (shows direction)
var drag_arrow: MeshInstance3D = null

## Colors
var circle_color: Color = Color(0.2, 0.8, 1.0, 0.5)  # Blue for attack
var arrow_color: Color = Color(1.0, 0.8, 0.2, 0.8)  # Yellow-orange for direction

## State
var is_active: bool = false
var drag_start: Vector3 = Vector3.ZERO
var drag_end: Vector3 = Vector3.ZERO

func _ready() -> void:
	# Create selection circle mesh
	selection_circle = MeshInstance3D.new()
	add_child(selection_circle)

	var circle_mesh: CylinderMesh = CylinderMesh.new()
	circle_mesh.top_radius = RedirectManager.REDIRECT_RADIUS
	circle_mesh.bottom_radius = RedirectManager.REDIRECT_RADIUS
	circle_mesh.height = 0.1  # Very thin cylinder (disc)
	selection_circle.mesh = circle_mesh

	# Create material for circle
	var circle_material: StandardMaterial3D = StandardMaterial3D.new()
	circle_material.albedo_color = circle_color
	circle_material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	circle_material.cull_mode = BaseMaterial3D.CULL_DISABLED  # Visible from both sides
	circle_material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	selection_circle.material_override = circle_material

	# Create drag arrow mesh
	drag_arrow = MeshInstance3D.new()
	add_child(drag_arrow)

	# Start hidden
	selection_circle.visible = false
	drag_arrow.visible = false

func _process(_delta: float) -> void:
	if is_active and drag_start != Vector3.ZERO:
		_update_arrow()

## Show selection circle at position
func show_selection_circle(target_position: Vector3, color: Color = Color.WHITE) -> void:
	# Raise circle slightly above ground to avoid z-fighting
	var raised_position: Vector3 = target_position + Vector3(0, 0.1, 0)

	if color != Color.WHITE:
		var mat: StandardMaterial3D = selection_circle.material_override as StandardMaterial3D
		if mat:
			mat.albedo_color = color

	print("RedirectIndicator: Showing circle at ", raised_position, " with color ", color)
	global_position = raised_position
	selection_circle.visible = true
	is_active = true
	print("RedirectIndicator: Circle visible=", selection_circle.visible, " global_pos=", selection_circle.global_position)

## Hide selection circle
func hide_selection_circle() -> void:
	selection_circle.visible = false
	drag_arrow.visible = false
	is_active = false

## Update drag arrow from start to end point
func update_drag(start: Vector3, end: Vector3) -> void:
	print("RedirectIndicator: Updating drag arrow from ", start, " to ", end)
	drag_start = start
	drag_end = end
	drag_arrow.visible = true

## Create arrow mesh dynamically
func _update_arrow() -> void:
	if drag_start == drag_end:
		drag_arrow.visible = false
		return

	var direction: Vector3 = drag_end - drag_start
	var distance: float = direction.length()

	if distance < 0.1:
		drag_arrow.visible = false
		return

	# Create cylinder for arrow shaft
	var arrow_mesh: CylinderMesh = CylinderMesh.new()
	arrow_mesh.top_radius = 0.1
	arrow_mesh.bottom_radius = 0.1
	arrow_mesh.height = distance

	drag_arrow.mesh = arrow_mesh

	# Create material
	var arrow_material: StandardMaterial3D = StandardMaterial3D.new()
	arrow_material.albedo_color = arrow_color
	arrow_material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	arrow_material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	drag_arrow.material_override = arrow_material

	# Position arrow at midpoint
	var midpoint: Vector3 = (drag_start + drag_end) / 2.0
	drag_arrow.global_position = midpoint

	# Rotate arrow to point from start to end
	drag_arrow.look_at(drag_end, Vector3.UP)
	drag_arrow.rotate_object_local(Vector3.RIGHT, PI / 2)  # Rotate to align with cylinder

	drag_arrow.visible = true
