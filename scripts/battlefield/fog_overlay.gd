extends ColorRect
class_name FogOverlay

## Creates atmospheric depth with a vertical gradient
## Darker at top (enemy/horizon), lighter at bottom (player side)

@export var top_color: Color = Color(0, 0, 0, 0.18)  # Slightly dark at horizon
@export var bottom_color: Color = Color(0, 0, 0, 0.0)  # Transparent at player side
@export var overlay_width: float = 1920.0
@export var overlay_height: float = 1080.0

var gradient_texture: GradientTexture2D

func _ready() -> void:
	_setup_gradient()
	mouse_filter = Control.MOUSE_FILTER_IGNORE  # Don't block mouse input

func _setup_gradient() -> void:
	# Set size
	size = Vector2(overlay_width, overlay_height)

	# Center it
	position = Vector2(-overlay_width / 2, -overlay_height / 2)

	# Create gradient
	var gradient = Gradient.new()
	gradient.set_color(0, top_color)
	gradient.set_color(1, bottom_color)

	# Create gradient texture (vertical)
	gradient_texture = GradientTexture2D.new()
	gradient_texture.gradient = gradient
	gradient_texture.fill_from = Vector2(0, 0)
	gradient_texture.fill_to = Vector2(0, 1)  # Top to bottom
	gradient_texture.width = int(overlay_width)
	gradient_texture.height = int(overlay_height)

	# Create shader material to apply gradient
	var shader_code = """
shader_type canvas_item;

uniform sampler2D gradient_tex;

void fragment() {
	vec4 grad_color = texture(gradient_tex, UV);
	COLOR = grad_color;
}
"""

	var shader = Shader.new()
	shader.code = shader_code

	var mat = ShaderMaterial.new()
	mat.shader = shader
	mat.set_shader_parameter("gradient_tex", gradient_texture)

	material = mat

## Update colors at runtime
func set_gradient_colors(top: Color, bottom: Color) -> void:
	top_color = top
	bottom_color = bottom
	_setup_gradient()
