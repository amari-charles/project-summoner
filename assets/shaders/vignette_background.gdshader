shader_type canvas_item;

uniform vec4 base_color : source_color = vec4(0.45, 0.07, 0.07, 1.0);
uniform vec4 vignette_color : source_color = vec4(0.15, 0.02, 0.02, 1.0);
uniform float pattern_scale : hint_range(3.0, 30.0) = 12.0;
uniform float edge_width : hint_range(0.02, 0.15) = 0.06;
uniform float highlight_amount : hint_range(0.0, 0.4) = 0.15;
uniform float shadow_amount : hint_range(0.0, 0.4) = 0.12;
uniform float vignette_strength : hint_range(0.0, 1.0) = 0.4;

void fragment() {
	vec2 uv = UV;

	// Scale UV for diamond grid
	vec2 scaled = uv * pattern_scale;

	// Diamond coordinates: rotate 45 degrees
	vec2 diamond_uv = vec2(scaled.x + scaled.y, scaled.x - scaled.y);

	// Get position within current diamond cell (0 to 1)
	vec2 cell = fract(diamond_uv);

	// Sharp edge detection for bevel
	// Top-left edges (highlight): near 0 on either axis
	float highlight_edge = step(cell.x, edge_width) + step(cell.y, edge_width);
	highlight_edge = clamp(highlight_edge, 0.0, 1.0);

	// Bottom-right edges (shadow): near 1 on either axis
	float shadow_edge = step(1.0 - edge_width, cell.x) + step(1.0 - edge_width, cell.y);
	shadow_edge = clamp(shadow_edge, 0.0, 1.0);

	// Apply sharp highlight and shadow
	vec3 pattern_color = base_color.rgb;
	pattern_color += vec3(highlight_amount) * highlight_edge;
	pattern_color -= vec3(shadow_amount) * shadow_edge;

	// Vignette - darken edges of screen
	vec2 center_uv = uv - 0.5;
	float dist = length(center_uv) * 1.4;
	float vignette = smoothstep(0.0, 1.0, dist) * vignette_strength;

	// Final color
	vec3 final_color = mix(pattern_color, vignette_color.rgb, vignette);
	COLOR = vec4(final_color, 1.0);
}
