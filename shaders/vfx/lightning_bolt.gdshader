shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_opaque;

// Realistic lightning shader using FBM noise + UV distortion
// Based on techniques from godotshaders.com

uniform vec3 effect_color: source_color = vec3(0.6, 0.8, 1.0);
uniform int octave_count: hint_range(1, 20) = 10;
uniform float amp_start = 0.5;
uniform float amp_coeff = 0.5;
uniform float freq_coeff = 2.0;
uniform float speed = 2.0;
uniform float thickness: hint_range(0.1, 5.0) = 1.5;
uniform float alpha: hint_range(0.0, 1.0) = 1.0;
uniform float flicker_intensity: hint_range(0.0, 1.0) = 0.3;

// Hash function for randomness
float hash12(vec2 x) {
	return fract(cos(mod(dot(x, vec2(13.9898, 8.141)), 3.14)) * 43758.5453);
}

// 2D hash for gradient noise
vec2 hash22(vec2 uv) {
	uv = vec2(dot(uv, vec2(127.1, 311.7)),
	          dot(uv, vec2(269.5, 183.3)));
	return 2.0 * fract(sin(uv) * 43758.5453123) - 1.0;
}

// Perlin noise with gradient interpolation
float noise(vec2 uv) {
	vec2 iuv = floor(uv);
	vec2 fuv = fract(uv);
	vec2 blur = smoothstep(0.0, 1.0, fuv);
	return mix(mix(dot(hash22(iuv + vec2(0.0, 0.0)), fuv - vec2(0.0, 0.0)),
	               dot(hash22(iuv + vec2(1.0, 0.0)), fuv - vec2(1.0, 0.0)), blur.x),
	           mix(dot(hash22(iuv + vec2(0.0, 1.0)), fuv - vec2(0.0, 1.0)),
	               dot(hash22(iuv + vec2(1.0, 1.0)), fuv - vec2(1.0, 1.0)), blur.x), blur.y) + 0.5;
}

// Fractal Brownian Motion - creates complex, self-similar patterns
float fbm(vec2 uv, int octaves) {
	float value = 0.0;
	float amplitude = amp_start;
	for (int i = 0; i < octaves; i++) {
		value += amplitude * noise(uv);
		uv *= freq_coeff;
		amplitude *= amp_coeff;
	}
	return value;
}

void fragment() {
	// Remap UV from 0-1 to -1 to 1
	vec2 uv = 2.0 * UV - 1.0;

	// Distort UV coordinates with FBM noise
	// This creates the jagged, branching lightning pattern
	uv += 2.0 * fbm(uv + TIME * speed, octave_count) - 1.0;

	// Distance from center line (lightning core)
	float dist = abs(uv.x) / thickness;

	// Flickering effect using time-based hash
	float flicker = mix(1.0, hash12(vec2(TIME)), flicker_intensity);

	// Inverse distance creates natural glow falloff
	// Bright at center, fading outward
	float intensity = (0.05 * flicker) / dist;

	// Apply color
	vec3 color = effect_color * intensity;

	// Alpha with power falloff for softer edges
	float a = pow(intensity, 2.0) * alpha;

	// Discard nearly transparent pixels
	if (a < 0.01) {
		discard;
	}

	ALBEDO = color;
	ALPHA = min(a, 1.0);
	EMISSION = color * 2.0;
}
