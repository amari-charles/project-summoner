shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_opaque, blend_mix;

// Shadow customization parameters
uniform float shadow_size : hint_range(0.5, 5.0) = 1.0;
uniform float shadow_opacity : hint_range(0.0, 1.0) = 0.6;
uniform vec4 shadow_color : source_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform float edge_softness : hint_range(0.0, 1.0) = 0.7;

void fragment() {
	// Calculate distance from center (0.5, 0.5 in UV space)
	vec2 center = vec2(0.5, 0.5);
	float dist = distance(UV, center);

	// Create radial gradient (0 at center, 1 at edge)
	// Scale by shadow_size
	float gradient = dist / (0.5 / shadow_size);

	// Apply softness to edge falloff
	gradient = smoothstep(0.0, 1.0 - edge_softness, gradient);

	// Invert so center is opaque, edges transparent
	float alpha = (1.0 - gradient) * shadow_opacity;

	// Set final color
	ALBEDO = shadow_color.rgb;
	ALPHA = alpha;
}
