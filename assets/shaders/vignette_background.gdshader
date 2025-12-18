shader_type canvas_item;

uniform vec4 center_color : source_color = vec4(0.5, 0.08, 0.08, 1.0);
uniform vec4 edge_color : source_color = vec4(0.18, 0.02, 0.02, 1.0);
uniform float vignette_strength : hint_range(0.0, 1.5) = 0.8;
uniform float vignette_softness : hint_range(0.1, 1.0) = 0.5;
uniform float noise_strength : hint_range(0.0, 0.15) = 0.04;
uniform float noise_scale : hint_range(100.0, 1000.0) = 400.0;

// Simple hash for noise
float hash(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

void fragment() {
	vec2 uv = UV;

	// Radial gradient - lighter center, darker edges
	vec2 center = uv - 0.5;
	float dist = length(center) * 2.0;
	float vignette = smoothstep(vignette_softness, vignette_strength + vignette_softness, dist);

	vec3 gradient_color = mix(center_color.rgb, edge_color.rgb, vignette);

	// Subtle noise/grain overlay
	float noise = hash(uv * noise_scale) * 2.0 - 1.0;
	gradient_color += noise * noise_strength;

	COLOR = vec4(gradient_color, 1.0);
}
