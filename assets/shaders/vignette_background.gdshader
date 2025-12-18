shader_type canvas_item;

uniform vec4 base_color : source_color = vec4(0.45, 0.07, 0.07, 1.0);
uniform vec4 highlight_color : source_color = vec4(0.55, 0.12, 0.12, 1.0);
uniform vec4 vignette_color : source_color = vec4(0.15, 0.02, 0.02, 1.0);
uniform float pattern_scale : hint_range(3.0, 30.0) = 10.0;
uniform float pillow_strength : hint_range(0.0, 1.0) = 0.7;
uniform float vignette_strength : hint_range(0.0, 1.0) = 0.4;

void fragment() {
	vec2 uv = UV;

	// Scale UV for diamond grid
	vec2 scaled = uv * pattern_scale;

	// Diamond coordinates: rotate 45 degrees
	vec2 diamond_uv = vec2(scaled.x + scaled.y, scaled.x - scaled.y);

	// Get position within current diamond cell (0 to 1)
	vec2 cell = fract(diamond_uv);

	// Distance from center of cell (0 at center, 0.5 at edges)
	vec2 from_center = abs(cell - 0.5);

	// Pillow effect: bright center, dark edges
	// Use smooth falloff for that puffy/rounded look
	float pillow = 1.0 - smoothstep(0.0, 0.5, max(from_center.x, from_center.y));

	// Mix base and highlight based on pillow
	vec3 pattern_color = mix(base_color.rgb, highlight_color.rgb, pillow * pillow_strength);

	// Vignette - darken edges of screen
	vec2 center_uv = uv - 0.5;
	float dist = length(center_uv) * 1.4;
	float vignette = smoothstep(0.0, 1.0, dist) * vignette_strength;

	// Final color
	vec3 final_color = mix(pattern_color, vignette_color.rgb, vignette);
	COLOR = vec4(final_color, 1.0);
}
