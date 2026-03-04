shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_always;

uniform vec4 aoe_color : source_color = vec4(1.0, 0.3, 0.0, 0.8);
uniform float edge_width : hint_range(0.01, 0.2, 0.01) = 0.08;

void fragment() {
	// Calculate distance from center (UV ranges from 0 to 1)
	vec2 center = vec2(0.5, 0.5);
	float dist = distance(UV, center);

	// Circle radius is 0.5 (half the quad)
	float radius = 0.5;

	// Discard fragments outside the circle
	if (dist > radius) {
		discard;
	}

	// Soft edge for the circle
	float edge_falloff = smoothstep(radius, radius - edge_width, dist);

	// Set color with edge glow
	ALBEDO = aoe_color.rgb;
	EMISSION = aoe_color.rgb * edge_falloff * 0.4;
}
