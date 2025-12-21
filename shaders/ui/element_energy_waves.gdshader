shader_type canvas_item;

// Element colors - passed from script based on summoner element
uniform vec4 color_primary : source_color = vec4(1.0, 0.478, 0.165, 1.0); // Fire orange #ff7a2a
uniform vec4 color_secondary : source_color = vec4(0.722, 0.188, 0.125, 1.0); // Ember #b83020
uniform float dark_blend : hint_range(0.1, 0.5) = 0.25; // How dark the background areas are

// Animation parameters
uniform float wave_speed : hint_range(0.1, 2.0) = 0.4;
uniform float wave_intensity : hint_range(0.05, 0.5) = 0.15;
uniform float wave_scale : hint_range(1.0, 10.0) = 3.0;

// Visual parameters
uniform float glow_intensity : hint_range(0.0, 1.0) = 0.3;
uniform float vignette_strength : hint_range(0.0, 1.0) = 0.4;

// Simplex noise function for organic texture
vec3 mod289(vec3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
vec2 mod289_2(vec2 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
vec3 permute(vec3 x) { return mod289(((x * 34.0) + 1.0) * x); }

float snoise(vec2 v) {
    const vec4 C = vec4(0.211324865405187, 0.366025403784439,
                        -0.577350269189626, 0.024390243902439);
    vec2 i = floor(v + dot(v, C.yy));
    vec2 x0 = v - i + dot(i, C.xx);
    vec2 i1 = (x0.x > x0.y) ? vec2(1.0, 0.0) : vec2(0.0, 1.0);
    vec4 x12 = x0.xyxy + C.xxzz;
    x12.xy -= i1;
    i = mod289_2(i);
    vec3 p = permute(permute(i.y + vec3(0.0, i1.y, 1.0)) + i.x + vec3(0.0, i1.x, 1.0));
    vec3 m = max(0.5 - vec3(dot(x0, x0), dot(x12.xy, x12.xy), dot(x12.zw, x12.zw)), 0.0);
    m = m * m;
    m = m * m;
    vec3 x = 2.0 * fract(p * C.www) - 1.0;
    vec3 h = abs(x) - 0.5;
    vec3 ox = floor(x + 0.5);
    vec3 a0 = x - ox;
    m *= 1.79284291400159 - 0.85373472095314 * (a0 * a0 + h * h);
    vec3 g;
    g.x = a0.x * x0.x + h.x * x0.y;
    g.yz = a0.yz * x12.xz + h.yz * x12.yw;
    return 130.0 * dot(m, g);
}

void fragment() {
    vec2 uv = UV;
    float time = TIME * wave_speed;

    // Create multiple layered waves for aurora effect
    float wave1 = sin(uv.x * wave_scale + time) * 0.5 + 0.5;
    float wave2 = sin(uv.x * wave_scale * 1.5 + time * 1.3 + 1.0) * 0.5 + 0.5;
    float wave3 = sin(uv.x * wave_scale * 0.7 + time * 0.8 + 2.0) * 0.5 + 0.5;

    // Add noise for organic feel
    float noise1 = snoise(vec2(uv.x * 2.0 + time * 0.5, uv.y * 0.5)) * 0.5 + 0.5;
    float noise2 = snoise(vec2(uv.x * 3.0 - time * 0.3, uv.y * 0.8 + time * 0.2)) * 0.5 + 0.5;

    // Combine waves with different vertical positions
    float combined_wave = wave1 * 0.4 + wave2 * 0.35 + wave3 * 0.25;
    combined_wave += (noise1 * 0.3 + noise2 * 0.2) * 0.5;

    // Create energy bands at different heights
    float band1_pos = 0.3 + combined_wave * wave_intensity;
    float band2_pos = 0.5 + combined_wave * wave_intensity * 0.8;
    float band3_pos = 0.7 + combined_wave * wave_intensity * 0.6;

    // Soft bands with glow
    float band1 = smoothstep(0.15, 0.0, abs(uv.y - band1_pos)) * 0.6;
    float band2 = smoothstep(0.12, 0.0, abs(uv.y - band2_pos)) * 0.8;
    float band3 = smoothstep(0.10, 0.0, abs(uv.y - band3_pos)) * 0.5;

    // Combine bands
    float energy = band1 + band2 + band3;
    energy = clamp(energy, 0.0, 1.0);

    // Add subtle noise texture to the energy
    float texture_noise = snoise(uv * 8.0 + vec2(time * 0.2, 0.0)) * 0.1 + 0.9;
    energy *= texture_noise;

    // Create color gradient - darkened element color at edges, bright element colors in energy bands
    vec3 base_color = color_secondary.rgb * dark_blend; // Darkened element color, not pure black
    vec3 energy_color = mix(color_secondary.rgb, color_primary.rgb, energy * 0.7 + noise1 * 0.3);

    // Mix base with energy
    vec3 final_color = mix(base_color, energy_color, energy * 0.8);

    // Add glow effect at wave peaks
    float glow = energy * glow_intensity;
    final_color += color_primary.rgb * glow * 0.5;

    // Vignette effect - darken edges
    vec2 vignette_uv = uv * 2.0 - 1.0;
    float vignette = 1.0 - dot(vignette_uv, vignette_uv) * vignette_strength;
    vignette = clamp(vignette, 0.0, 1.0);
    final_color *= vignette;

    // Keep it dark enough for UI readability
    final_color *= 0.7;

    COLOR = vec4(final_color, 1.0);
}
