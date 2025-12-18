shader_type canvas_item;

uniform vec4 base_color : source_color = vec4(0.5, 0.08, 0.08, 1.0);
uniform vec4 stripe_color : source_color = vec4(0.42, 0.06, 0.06, 1.0);
uniform vec4 edge_color : source_color = vec4(0.15, 0.02, 0.02, 1.0);
uniform float stripe_scale : hint_range(10.0, 100.0) = 40.0;
uniform float stripe_width : hint_range(0.1, 0.9) = 0.5;
uniform float vignette_strength : hint_range(0.0, 1.0) = 0.6;

void fragment() {
	vec2 uv = UV;

	// Diagonal stripes pattern
	float diagonal = (uv.x + uv.y) * stripe_scale;
	float stripe = step(stripe_width, fract(diagonal));

	// Mix base and stripe colors
	vec4 pattern_color = mix(base_color, stripe_color, stripe);

	// Vignette - darken edges
	vec2 center_uv = uv - 0.5;
	float dist = length(center_uv) * 1.4;
	float vignette = smoothstep(0.0, 1.0, dist) * vignette_strength;

	// Apply vignette
	COLOR = mix(pattern_color, edge_color, vignette);
}
