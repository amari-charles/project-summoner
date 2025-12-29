shader_type canvas_item;

// Campaign map background - animated starfield
// Dark background with twinkling stars

uniform vec4 bg_color : source_color = vec4(0.05, 0.05, 0.12, 1.0);
uniform vec4 star_color : source_color = vec4(0.8, 0.85, 1.0, 1.0);
uniform float star_density : hint_range(50.0, 500.0) = 200.0;
uniform float twinkle_speed : hint_range(0.5, 5.0) = 1.5;

// Pseudo-random function
float hash(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

void fragment() {
	vec2 uv = UV * star_density;
	vec2 grid_id = floor(uv);
	vec2 grid_uv = fract(uv) - 0.5;

	// Random offset within each grid cell
	vec2 star_pos = vec2(hash(grid_id), hash(grid_id + 1.0)) - 0.5;
	float dist = length(grid_uv - star_pos * 0.8);

	// Star brightness with twinkle
	float rand_phase = hash(grid_id + 2.0) * 6.28;
	float twinkle = sin(TIME * twinkle_speed + rand_phase) * 0.5 + 0.5;
	float star_size = hash(grid_id + 3.0) * 0.015 + 0.005;
	float star = smoothstep(star_size, 0.0, dist) * twinkle;

	// Vary star brightness
	float brightness = hash(grid_id + 4.0) * 0.7 + 0.3;
	star *= brightness;

	vec3 color = bg_color.rgb + star_color.rgb * star;
	COLOR = vec4(color, 1.0);
}
