shader_type spatial;
render_mode unshaded, cull_back;

uniform vec4 color_a : source_color = vec4(0.35, 0.60, 0.25, 1.0);
uniform vec4 color_b : source_color = vec4(0.30, 0.50, 0.20, 1.0);
uniform float top_face_threshold : hint_range(0.0, 1.0) = 0.8;
varying float parity_varying;
varying vec3 local_pos_varying;

void vertex() {
	// INSTANCE_CUSTOM is available in vertex stage for MultiMesh instances.
	parity_varying = step(0.5, INSTANCE_CUSTOM.r);
	// Preserve local-space position so fragment can derive geometric face normals.
	local_pos_varying = VERTEX;
}

void fragment() {
	// parity: 0.0 = checker color A on top, 1.0 = checker color B on top
	float parity = parity_varying;

	vec3 top_color = mix(color_a.rgb, color_b.rgb, parity);
	vec3 side_color = mix(color_b.rgb, color_a.rgb, parity);
	float top_alpha = mix(color_a.a, color_b.a, parity);
	float side_alpha = mix(color_b.a, color_a.a, parity);

	// Use geometric face normal from local-space derivatives (not interpolated vertex normals).
	// This prevents stair/triangle artifacts on side faces.
	vec3 local_face_normal = normalize(cross(dFdx(local_pos_varying), dFdy(local_pos_varying)));
	float top_mask = step(top_face_threshold, abs(local_face_normal.y));
	ALBEDO = mix(side_color, top_color, top_mask);
	ALPHA = mix(side_alpha, top_alpha, top_mask);
}
