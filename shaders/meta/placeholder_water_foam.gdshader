shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_opaque;

uniform sampler2D foam_texture : source_color, filter_nearest;
uniform float animation_fps = 8.0;
uniform float animation_offset = 0.0;

const float FRAME_COUNT = 16.0;


void fragment() {
	float frame = mod(floor(TIME * animation_fps + animation_offset), FRAME_COUNT);
	vec2 frame_uv = vec2((UV.x + frame) / FRAME_COUNT, UV.y);
	vec4 foam = texture(foam_texture, frame_uv);
	ALBEDO = foam.rgb;
	ALPHA = foam.a;
	ALPHA_SCISSOR_THRESHOLD = 0.5;
}
