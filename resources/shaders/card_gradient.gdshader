shader_type canvas_item;

// Gradient colors
uniform vec4 color_center : source_color = vec4(0.2, 0.2, 0.2, 1.0);
uniform vec4 color_edge : source_color = vec4(0.4, 0.4, 0.4, 1.0);

// Rounded corner settings
uniform float corner_radius : hint_range(0.0, 50.0) = 8.0;
uniform vec2 rect_size = vec2(120.0, 160.0);

void fragment() {
	// Calculate diagonal gradient (top-left to bottom-right)
	// UV.x + UV.y ranges from 0.0 (top-left) to 2.0 (bottom-right)
	float gradient_value = (UV.x + UV.y) * 0.5;

	// Create smooth diagonal gradient
	vec4 gradient_color = mix(color_center, color_edge, smoothstep(0.0, 1.0, gradient_value));

	// Calculate rounded corner mask
	vec2 pixel_pos = UV * rect_size;
	vec2 half_size = rect_size * 0.5;

	// Distance from each corner
	float alpha = 1.0;

	// Top-left corner
	if (pixel_pos.x < corner_radius && pixel_pos.y < corner_radius) {
		vec2 corner_center = vec2(corner_radius, corner_radius);
		float corner_dist = distance(pixel_pos, corner_center);
		if (corner_dist > corner_radius) {
			alpha = 0.0;
		} else {
			alpha = smoothstep(corner_radius, corner_radius - 1.0, corner_dist);
		}
	}

	// Top-right corner
	if (pixel_pos.x > rect_size.x - corner_radius && pixel_pos.y < corner_radius) {
		vec2 corner_center = vec2(rect_size.x - corner_radius, corner_radius);
		float corner_dist = distance(pixel_pos, corner_center);
		if (corner_dist > corner_radius) {
			alpha = 0.0;
		} else {
			alpha = smoothstep(corner_radius, corner_radius - 1.0, corner_dist);
		}
	}

	// Bottom-left corner
	if (pixel_pos.x < corner_radius && pixel_pos.y > rect_size.y - corner_radius) {
		vec2 corner_center = vec2(corner_radius, rect_size.y - corner_radius);
		float corner_dist = distance(pixel_pos, corner_center);
		if (corner_dist > corner_radius) {
			alpha = 0.0;
		} else {
			alpha = smoothstep(corner_radius, corner_radius - 1.0, corner_dist);
		}
	}

	// Bottom-right corner
	if (pixel_pos.x > rect_size.x - corner_radius && pixel_pos.y > rect_size.y - corner_radius) {
		vec2 corner_center = vec2(rect_size.x - corner_radius, rect_size.y - corner_radius);
		float corner_dist = distance(pixel_pos, corner_center);
		if (corner_dist > corner_radius) {
			alpha = 0.0;
		} else {
			alpha = smoothstep(corner_radius, corner_radius - 1.0, corner_dist);
		}
	}

	// Apply gradient with rounded corner alpha
	COLOR = vec4(gradient_color.rgb, gradient_color.a * alpha);
}
