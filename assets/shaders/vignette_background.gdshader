shader_type canvas_item;

uniform vec4 center_color : source_color = vec4(0.55, 0.09, 0.09, 1.0);
uniform vec4 edge_color : source_color = vec4(0.2, 0.02, 0.02, 1.0);
uniform float vignette_intensity : hint_range(0.0, 2.0) = 1.2;
uniform float vignette_softness : hint_range(0.0, 1.0) = 0.3;

void fragment() {
	// UV from center (-0.5 to 0.5)
	vec2 uv = UV - 0.5;

	// Elliptical distance - reaches edges at the same time horizontally and vertically
	float dist = length(uv) * 2.0;

	float vignette = smoothstep(vignette_softness, vignette_intensity, dist);

	COLOR = mix(center_color, edge_color, vignette);
}
