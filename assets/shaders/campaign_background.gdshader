shader_type canvas_item;

// Campaign map background - parchment with dark fantasy color grading

uniform sampler2D parchment_tex;
uniform vec4 tint_color : source_color = vec4(0.6, 0.5, 0.4, 1.0);
uniform float brightness : hint_range(0.0, 1.0) = 0.4;

void fragment() {
	vec4 tex_color = texture(parchment_tex, UV);

	// If texture is empty/black, show a debug magenta color
	if (tex_color.r < 0.01 && tex_color.g < 0.01 && tex_color.b < 0.01) {
		COLOR = vec4(1.0, 0.0, 1.0, 1.0); // Magenta = texture not loaded
	} else {
		vec3 result = tex_color.rgb * tint_color.rgb * brightness;
		COLOR = vec4(result, 1.0);
	}
}
