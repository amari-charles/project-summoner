extends Sprite2D
class_name BattlefieldGround

## Visual ground for the battlefield
## Can be easily swapped with different textures or replaced with 3D mesh later

@export var ground_texture: Texture2D = null
@export var fallback_color: Color = Color(0.3, 0.5, 0.2, 1.0)  # Default grass green
@export var ground_width: float = 1920.0
@export var ground_height: float = 1080.0
@export var texture_scale: float = 1.0  # Scale for tiling

func _ready() -> void:
	_setup_ground()

func _setup_ground() -> void:
	# Center the ground at origin
	centered = true

	# Apply texture if provided
	if ground_texture:
		texture = ground_texture
		# Scale to cover the desired area while tiling
		var tex_size = ground_texture.get_size()
		region_enabled = true
		region_rect = Rect2(0, 0, ground_width / texture_scale, ground_height / texture_scale)
		scale = Vector2(texture_scale, texture_scale)
	else:
		# Fallback: create a simple colored rectangle
		# Note: Without texture, Sprite2D won't show anything, so we just set modulate
		# The actual fallback visual would need a ColorRect, but for simplicity we'll
		# expect a texture to be assigned
		modulate = fallback_color

## Helper for swapping textures at runtime
func set_ground_texture(new_texture: Texture2D) -> void:
	ground_texture = new_texture
	_setup_ground()
