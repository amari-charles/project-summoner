shader_type spatial;
render_mode unshaded, blend_mul, cull_disabled, depth_draw_never;

uniform sampler2D sprite_texture : source_color;
uniform float shadow_opacity : hint_range(0.0, 1.0) = 0.5;
// Skew factors simulate a directional light from the upper-left.
// skew_x pushes the head-end rightward; skew_y pushes it toward the camera.
// Set via ShadowHelper constants; defaults here are fallbacks.
uniform float skew_x : hint_range(-3.0, 3.0) = 0.8;
uniform float skew_y : hint_range(-3.0, 3.0) = 0.6;

void vertex() {
	// Pin feet, displace head to simulate directional light from upper-left.
	// t=0 at feet (grounded), t=1 at head (max displacement).
	float t = 1.0 - UV.y;
	VERTEX.x += skew_x * t;
	VERTEX.y += skew_y * t;
}

void fragment() {
	float alpha = texture(sprite_texture, UV).a;
	float brightness = 1.0 - (alpha * shadow_opacity);
	ALBEDO = vec3(brightness);
	ALPHA = 1.0;
}
