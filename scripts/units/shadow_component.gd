extends MeshInstance3D
class_name ShadowComponent

## Simple blob shadow for 2.5D units
## Renders a circular shadow on the ground using a shader

@export var shadow_size: float = 1.0
@export var shadow_opacity: float = 0.6
@export var shadow_color: Color = Color(0.0, 0.0, 0.0, 1.0)
@export var edge_softness: float = 0.7

func _ready() -> void:
	_setup_shadow()

func _setup_shadow() -> void:
	# Create quad mesh facing up (will be rotated to face down towards ground)
	var quad = QuadMesh.new()
	quad.size = Vector2(shadow_size, shadow_size)
	mesh = quad

	# Rotate to lay flat on ground (facing up)
	rotation_degrees = Vector3(-90, 0, 0)

	# Position slightly above ground to prevent z-fighting
	position.y = 0.01

	# Try StandardMaterial3D first for debugging
	var material = StandardMaterial3D.new()
	material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.cull_mode = BaseMaterial3D.CULL_DISABLED
	material.albedo_color = Color(0, 0, 0, shadow_opacity)
	material.depth_draw_mode = BaseMaterial3D.DEPTH_DRAW_DISABLED

	set_surface_override_material(0, material)

	# Shadows should not cast shadows themselves
	cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF

	# Set rendering flags
	gi_mode = GeometryInstance3D.GI_MODE_DISABLED

	# Set layers appropriately (visible but not interactable)
	layers = 1

## Update shadow parameters at runtime
func set_shadow_size(size: float) -> void:
	shadow_size = size
	if mesh is QuadMesh:
		mesh.size = Vector2(size, size)

func set_shadow_opacity(opacity: float) -> void:
	shadow_opacity = opacity
	var mat = get_surface_override_material(0)
	if mat is StandardMaterial3D:
		var color = mat.albedo_color
		color.a = opacity
		mat.albedo_color = color
	elif mat is ShaderMaterial:
		mat.set_shader_parameter("shadow_opacity", opacity)
