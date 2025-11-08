extends MeshInstance3D
class_name ShadowComponent

## Simple blob shadow for 2.5D units
## Uses a QuadMesh with transparent gradient texture
## Works with any ground material (doesn't require StandardMaterial3D like Decals)

@export var shadow_radius: float = 1.0
@export var shadow_opacity: float = 0.6

var shadow_texture: ImageTexture = null

## Initialize the shadow with specified radius and opacity
## Must be called after adding to scene tree
func initialize(radius: float, opacity: float) -> void:
	shadow_radius = radius
	shadow_opacity = opacity

	# Create quad mesh
	var quad = QuadMesh.new()
	quad.size = Vector2(shadow_radius, shadow_radius)
	mesh = quad

	# Orient flat on ground (rotate -90° around X axis)
	rotation_degrees = Vector3(-90, 0, 0)

	# Position just above ground (PlaneMesh has no volume)
	position.y = 0.01

	# Create radial gradient texture
	shadow_texture = _create_radial_gradient_texture()

	# Create material with proper transparent gradient shadow
	var material = StandardMaterial3D.new()
	material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	material.blend_mode = BaseMaterial3D.BLEND_MODE_MIX
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.cull_mode = BaseMaterial3D.CULL_DISABLED
	material.albedo_texture = shadow_texture
	material.albedo_color = Color(0, 0, 0, shadow_opacity)

	set_surface_override_material(0, material)

	# Rendering settings
	cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	gi_mode = GeometryInstance3D.GI_MODE_DISABLED
	visible = true
	layers = 1

## Create a radial gradient texture for the shadow
func _create_radial_gradient_texture() -> ImageTexture:
	var size_px = 128
	var image = Image.create(size_px, size_px, false, Image.FORMAT_RGBA8)

	var center = Vector2(size_px / 2.0, size_px / 2.0)
	var max_radius = size_px / 2.0

	for y in range(size_px):
		for x in range(size_px):
			var pos = Vector2(x, y)
			var dist = pos.distance_to(center)

			# Normalize distance (0 at center, 1 at edge)
			var normalized_dist = dist / max_radius

			# Create soft falloff with smoothstep
			# Make it more aggressive than quadratic for softer edges
			var alpha = 1.0 - smoothstep(0.0, 1.0, normalized_dist)

			# Set pixel (white with varying alpha - color comes from albedo_color)
			image.set_pixel(x, y, Color(1, 1, 1, alpha))

	return ImageTexture.create_from_image(image)

## Update shadow radius at runtime
func set_shadow_radius(radius: float) -> void:
	shadow_radius = radius
	if mesh is QuadMesh:
		mesh.size = Vector2(shadow_radius, shadow_radius)

## Update shadow opacity at runtime
func set_shadow_opacity(opacity: float) -> void:
	shadow_opacity = opacity
	var mat = get_surface_override_material(0)
	if mat is StandardMaterial3D:
		var color = mat.albedo_color
		color.a = opacity
		mat.albedo_color = color
