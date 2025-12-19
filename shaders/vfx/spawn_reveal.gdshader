shader_type canvas_item;

// Ghost spawn reveal shader
// Reveals the sprite from bottom to top with a glowing edge
// Uses viewport-relative coordinates so multi-part sprites animate together

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float edge_width : hint_range(0.0, 0.3) = 0.1;
uniform vec4 glow_color : source_color = vec4(0.4, 0.7, 1.0, 1.0);
uniform float glow_intensity : hint_range(0.0, 3.0) = 1.5;

void fragment() {
	vec4 tex_color = texture(TEXTURE, UV);

	// Use SCREEN_UV for viewport-relative position
	// This ensures all sprite parts use the same Y reference
	// SCREEN_UV.y = 0 at top of viewport, 1 at bottom
	float reveal_pos = 1.0 - SCREEN_UV.y;

	// Calculate reveal threshold (where the edge currently is)
	// Scale progress so it goes from -edge_width to 1+edge_width
	float threshold = progress * (1.0 + edge_width * 2.0) - edge_width;

	// Distance from the reveal edge
	float edge_dist = reveal_pos - threshold;

	// Fully visible below the threshold (already revealed)
	// Fully invisible above threshold + edge_width (not yet revealed)
	float visibility = smoothstep(edge_width, 0.0, edge_dist);

	// Glow at the edge (brightest right at the threshold)
	float glow = smoothstep(edge_width, 0.0, abs(edge_dist)) * glow_intensity;
	glow *= smoothstep(0.0, 0.3, progress); // Fade in glow as reveal starts
	glow *= smoothstep(1.0, 0.7, progress); // Fade out glow as reveal ends

	// Apply glow to color (additive blend)
	vec3 final_color = tex_color.rgb + glow_color.rgb * glow * tex_color.a;

	// Apply visibility (alpha modulation)
	float final_alpha = tex_color.a * visibility;

	COLOR = vec4(final_color, final_alpha);
}
