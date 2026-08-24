shader_type canvas_item;

uniform vec2 uv_offset = vec2(0.0, 0.0);
uniform vec2 uv_scale = vec2(1.0, 1.0);

void fragment() {
	vec2 sample_uv = uv_offset + UV * uv_scale;
	COLOR = texture(TEXTURE, sample_uv);
}
