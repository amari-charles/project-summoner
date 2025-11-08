extends MeshInstance3D
class_name SelectionRingComponent

## Visual selection ring for units
## Shows when unit is selected or hovered
## Uses QuadMesh with radial gradient texture (similar to ShadowComponent)

@export var ring_radius: float = 1.5
@export var ring_color: Color = Color(0.2, 0.8, 1.0, 0.8)  ## Bright cyan

var ring_texture: ImageTexture = null

## Initialize the selection ring with specified radius
## Must be called after adding to scene tree
func initialize(radius: float) -> void:
	ring_radius = radius

	# Create quad mesh
	var quad = QuadMesh.new()
	quad.size = Vector2(ring_radius, ring_radius)
	mesh = quad

	# Orient flat on ground (rotate -90° around X axis)
	rotation_degrees = Vector3(-90, 0, 0)

	# Position just above ground (slightly higher than shadow to be visible on top)
	position.y = 0.02

	# Create ring texture (radial gradient)
	ring_texture = _create_ring_texture()

	# Create material with bright color and transparency
	var material = StandardMaterial3D.new()
	material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	material.blend_mode = BaseMaterial3D.BLEND_MODE_MIX
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.cull_mode = BaseMaterial3D.CULL_DISABLED
	material.albedo_texture = ring_texture
	material.albedo_color = ring_color

	set_surface_override_material(0, material)

	# Rendering settings
	cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	gi_mode = GeometryInstance3D.GI_MODE_DISABLED
	layers = 1

	# Start hidden
	visible = false

## Create a ring-shaped texture (bright center fading to transparent edges)
func _create_ring_texture() -> ImageTexture:
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

			# Create bright ring with soft edges
			# Inverted from shadow - bright in center, fading to edges
			var alpha = 1.0 - smoothstep(0.0, 1.0, normalized_dist)

			# Set pixel (white with varying alpha - color comes from albedo_color)
			image.set_pixel(x, y, Color(1, 1, 1, alpha))

	return ImageTexture.create_from_image(image)

## Show the selection ring
func show_ring() -> void:
	visible = true

## Hide the selection ring
func hide_ring() -> void:
	visible = false

## Set ring color
func set_ring_color(color: Color) -> void:
	ring_color = color
	var mat = get_surface_override_material(0)
	if mat is StandardMaterial3D:
		mat.albedo_color = color

## Update ring radius at runtime
func set_ring_radius(radius: float) -> void:
	ring_radius = radius
	if mesh is QuadMesh:
		mesh.size = Vector2(ring_radius, ring_radius)
