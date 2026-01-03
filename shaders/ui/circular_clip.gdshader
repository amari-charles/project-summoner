shader_type canvas_item;

// Controls which region of the image to show
uniform vec2 uv_offset = vec2(0.0, 0.0);
uniform vec2 uv_scale = vec2(1.0, 1.0);

// Border settings
uniform float border_width : hint_range(0.0, 0.1) = 0.03;
uniform vec4 border_color : source_color = vec4(0.0, 0.0, 0.0, 1.0);

void fragment() {
	vec2 center = vec2(0.5, 0.5);
	float dist = distance(UV, center);

	// Outside circle - discard
	if (dist > 0.5) {
		discard;
	}

	// Border ring
	if (dist > 0.5 - border_width) {
		COLOR = border_color;
	} else {
		// Sample from adjusted UV region (for zooming/cropping)
		vec2 sample_uv = uv_offset + UV * uv_scale;
		COLOR = texture(TEXTURE, sample_uv);
	}
}
