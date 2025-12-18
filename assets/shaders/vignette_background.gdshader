shader_type canvas_item;

uniform vec4 base_color : source_color = vec4(0.45, 0.07, 0.07, 1.0);
uniform vec4 vignette_color : source_color = vec4(0.15, 0.02, 0.02, 1.0);
uniform float pattern_scale : hint_range(5.0, 50.0) = 20.0;
uniform float bevel_strength : hint_range(0.0, 0.3) = 0.12;
uniform float vignette_strength : hint_range(0.0, 1.0) = 0.5;

void fragment() {
	vec2 uv = UV;

	// Diamond pattern - two diagonal lines crossing
	vec2 pattern_uv = uv * pattern_scale;
	float d1 = fract(pattern_uv.x + pattern_uv.y);
	float d2 = fract(pattern_uv.x - pattern_uv.y);

	// Create diamond shape
	float diamond = min(d1, 1.0 - d1) + min(d2, 1.0 - d2);

	// Emboss effect - highlight top-left edges, shadow bottom-right
	float offset = 0.02;
	vec2 pattern_uv_offset = (uv + vec2(offset, offset)) * pattern_scale;
	float d1_off = fract(pattern_uv_offset.x + pattern_uv_offset.y);
	float d2_off = fract(pattern_uv_offset.x - pattern_uv_offset.y);
	float diamond_offset = min(d1_off, 1.0 - d1_off) + min(d2_off, 1.0 - d2_off);

	// Bevel: difference creates highlight/shadow
	float bevel = (diamond - diamond_offset) * bevel_strength;

	// Apply bevel to base color
	vec3 pattern_color = base_color.rgb + vec3(bevel);

	// Vignette - darken edges
	vec2 center_uv = uv - 0.5;
	float dist = length(center_uv) * 1.4;
	float vignette = smoothstep(0.0, 1.0, dist) * vignette_strength;

	// Final color
	vec3 final_color = mix(pattern_color, vignette_color.rgb, vignette);
	COLOR = vec4(final_color, 1.0);
}
