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

	# Load and configure shadow shader
	var shader = load("res://shaders/blob_shadow.gdshader")
	if shader:
		var material = ShaderMaterial.new()
		material.shader = shader

		# Set shader parameters
		material.set_shader_parameter("shadow_size", shadow_size)
		material.set_shader_parameter("shadow_opacity", shadow_opacity)
		material.set_shader_parameter("shadow_color", shadow_color)
		material.set_shader_parameter("edge_softness", edge_softness)

		set_surface_override_material(0, material)

	# Shadows should not cast shadows themselves
	cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF

	# Set layers appropriately (visible but not interactable)
	layers = 1

## Update shadow parameters at runtime
func set_shadow_size(size: float) -> void:
	shadow_size = size
	if mesh is QuadMesh:
		mesh.size = Vector2(size, size)
	var mat = get_surface_override_material(0)
	if mat is ShaderMaterial:
		mat.set_shader_parameter("shadow_size", size)

func set_shadow_opacity(opacity: float) -> void:
	shadow_opacity = opacity
	var mat = get_surface_override_material(0)
	if mat is ShaderMaterial:
		mat.set_shader_parameter("shadow_opacity", opacity)
