shader_type spatial;
render_mode unshaded, cull_disabled, blend_mix, depth_draw_never, depth_test_disabled;

uniform vec4 ripple_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform vec3 impact_dir = vec3(1.0, 0.2, 0.0);
uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float band_width : hint_range(0.01, 0.6) = 0.06;
uniform float alpha : hint_range(0.0, 1.0) = 0.7;
uniform float base_shell_alpha : hint_range(0.0, 1.0) = 0.0;

varying vec3 v_local_pos;

void vertex() {
	v_local_pos = VERTEX;
}

void fragment() {
	vec3 shell_dir = normalize(v_local_pos);
	vec3 hit_dir = normalize(impact_dir);
	float cosine = clamp(dot(shell_dir, hit_dir), -1.0, 1.0);
	float angular_distance = acos(cosine) / PI;
	float half_width = max(0.005, band_width * 0.5);
	float band = 1.0 - smoothstep(half_width, half_width + 0.03, abs(angular_distance - progress));
	float front_mask = smoothstep(-0.2, 0.3, cosine);
	float locality_mask = 1.0 - smoothstep(0.24, 0.42, angular_distance);

	float fade_in = smoothstep(0.0, 0.08, progress);
	float fade_out = 1.0 - smoothstep(0.55, 1.0, progress);
	float pulse_fade = fade_in * fade_out;
	float pulse_intensity = band * front_mask * locality_mask;
	float out_alpha = (base_shell_alpha + (pulse_intensity * alpha)) * pulse_fade;
	out_alpha = clamp(out_alpha, 0.0, 1.0);

	ALBEDO = ripple_color.rgb;
	EMISSION = ripple_color.rgb * (pulse_intensity * 1.2);
	ALPHA = out_alpha;
}
