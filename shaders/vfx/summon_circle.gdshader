shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_always;

// Summoning circle shader - animated magical circle effect
// Features: rotating runes, pulsing glow, progress indicator

uniform vec4 circle_color : source_color = vec4(0.3, 0.6, 1.0, 0.8);
uniform vec4 glow_color : source_color = vec4(0.5, 0.8, 1.0, 1.0);
uniform float ring_width : hint_range(0.02, 0.15, 0.01) = 0.06;
uniform float inner_ring_width : hint_range(0.01, 0.1, 0.01) = 0.03;
uniform float glow_intensity : hint_range(0.0, 2.0, 0.1) = 1.0;
uniform float rotation_speed : hint_range(0.0, 5.0, 0.1) = 1.0;
uniform float pulse_speed : hint_range(0.0, 10.0, 0.5) = 3.0;
uniform float time_offset : hint_range(0.0, 10.0, 0.1) = 0.0;

// Progress (0 to 1) - fills the circle as casting completes
uniform float progress : hint_range(0.0, 1.0, 0.01) = 0.0;

// Rune segment count for the outer ring pattern
const int RUNE_SEGMENTS = 8;

void fragment() {
	vec2 center = vec2(0.5, 0.5);
	vec2 uv_centered = UV - center;
	float dist = length(uv_centered);
	float angle = atan(uv_centered.y, uv_centered.x);

	// Animated time
	float t = TIME + time_offset;

	// Outer ring (radius 0.45-0.5)
	float outer_radius = 0.48;
	float outer_ring = smoothstep(outer_radius, outer_radius - ring_width * 0.5, dist)
	                 * smoothstep(outer_radius - ring_width, outer_radius - ring_width * 0.5, dist);

	// Inner ring (radius 0.25-0.3)
	float inner_radius = 0.28;
	float inner_ring = smoothstep(inner_radius, inner_radius - inner_ring_width * 0.5, dist)
	                 * smoothstep(inner_radius - inner_ring_width, inner_radius - inner_ring_width * 0.5, dist);

	// Rotating rune segments on outer ring
	float rotated_angle = angle + t * rotation_speed;
	float segment_angle = mod(rotated_angle, 6.28318 / float(RUNE_SEGMENTS));
	float segment_center = 6.28318 / float(RUNE_SEGMENTS) / 2.0;
	float rune_pattern = smoothstep(0.0, segment_center * 0.3, segment_angle)
	                   * smoothstep(segment_center * 2.0, segment_center * 1.7, segment_angle);

	// Counter-rotating inner pattern
	float inner_rotated_angle = angle - t * rotation_speed * 1.5;
	float inner_segment_angle = mod(inner_rotated_angle, 6.28318 / 6.0);
	float inner_segment_center = 6.28318 / 6.0 / 2.0;
	float inner_pattern = smoothstep(0.0, inner_segment_center * 0.4, inner_segment_angle)
	                    * smoothstep(inner_segment_center * 2.0, inner_segment_center * 1.6, inner_segment_angle);

	// Progress fill (fills from center outward)
	float progress_radius = progress * 0.5;
	float progress_fill = smoothstep(progress_radius, progress_radius - 0.05, dist) * 0.3;

	// Pulsing glow
	float pulse = 0.7 + 0.3 * sin(t * pulse_speed);

	// Center glow (always visible, pulses)
	float center_glow = smoothstep(0.2, 0.0, dist) * pulse * 0.5;

	// Combine all elements
	float rings = outer_ring * (0.5 + 0.5 * rune_pattern) + inner_ring * (0.5 + 0.5 * inner_pattern);
	float total = rings + center_glow + progress_fill;

	// Discard if nothing to show
	if (total < 0.01) {
		discard;
	}

	// Mix colors based on intensity
	vec3 final_color = mix(circle_color.rgb, glow_color.rgb, total * pulse);

	ALBEDO = final_color;
	ALPHA = total * circle_color.a;
	EMISSION = final_color * glow_intensity * pulse;
}
