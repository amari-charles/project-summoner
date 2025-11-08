extends Decal
class_name ShadowComponent

## Blob shadow for 2.5D units using Decal projection
## Projects a circular shadow texture onto the ground plane

@export var shadow_size: float = 1.0
@export var shadow_opacity: float = 0.6
@export var ground_offset: float = 0.05  ## Distance above ground to prevent z-fighting

var shadow_texture: ImageTexture = null

func _ready() -> void:
	_setup_shadow()

func _setup_shadow() -> void:
	# Create circular gradient texture for shadow
	_create_shadow_texture()

	# Configure decal
	texture_albedo = shadow_texture
	albedo_mix = shadow_opacity

	# Set decal size (projects downward in a box)
	# X/Z control shadow radius, Y controls projection depth
	size = Vector3(shadow_size, 1.0, shadow_size)

	# Decals project downward by default (Y-axis), which is what we want
	# No rotation needed!

	# Position at ground level
	position.y = ground_offset

	# Disable other texture channels (only use albedo for shadow)
	modulate = Color(0, 0, 0, 1)  # Black shadow

	# Render settings
	cull_mask = 1  # Project onto layer 1 (ground)

func _create_shadow_texture() -> void:
	# Create a circular gradient image for the shadow
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

			# Create soft falloff (0 = opaque, 1 = transparent)
			var alpha = 1.0 - clamp(normalized_dist, 0.0, 1.0)

			# Apply additional edge softness
			alpha = pow(alpha, 2.0)  # Quadratic falloff for softer edges

			# Set pixel (black with varying alpha)
			image.set_pixel(x, y, Color(0, 0, 0, alpha))

	# Convert to texture
	shadow_texture = ImageTexture.create_from_image(image)

## Update shadow size at runtime
func set_shadow_size(new_size: float) -> void:
	shadow_size = new_size
	size = Vector3(shadow_size, 1.0, shadow_size)

## Update shadow opacity at runtime
func set_shadow_opacity(opacity: float) -> void:
	shadow_opacity = opacity
	albedo_mix = opacity
