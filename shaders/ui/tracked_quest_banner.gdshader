shader_type canvas_item;

uniform vec4 banner_color : source_color = vec4(0.1059, 0.0941, 0.0784, 0.78);

void fragment() {
	float right_fade = 1.0 - smoothstep(0.58, 1.0, UV.x);
	COLOR = vec4(banner_color.rgb, banner_color.a * right_fade);
}
