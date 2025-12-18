shader_type canvas_item;

uniform vec4 center_color : source_color = vec4(0.55, 0.09, 0.09, 1.0);
uniform vec4 edge_color : source_color = vec4(0.2, 0.02, 0.02, 1.0);
uniform float vignette_intensity : hint_range(0.0, 2.0) = 1.0;
uniform float vignette_softness : hint_range(0.0, 1.0) = 0.4;

void fragment() {
	vec2 uv = UV - 0.5;
	// Account for aspect ratio
	float aspect = 1.0 / SCREEN_PIXEL_SIZE.x * SCREEN_PIXEL_SIZE.y;
	uv.x *= aspect;

	float dist = length(uv) * 2.0;
	float vignette = smoothstep(vignette_softness, vignette_intensity, dist);

	COLOR = mix(center_color, edge_color, vignette);
}
