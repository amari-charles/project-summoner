shader_type spatial;
render_mode unshaded, cull_back;

// ============================================================================
// PAINTERLY GRASS BASE SHADER (SPATIAL)
// ============================================================================
// Creates a hand-painted grass field effect using multiple noise layers.
// Designed for 2D/2.5D mobile-style games (Mini Warriors, Monster Chef style)
// Applied to 3D plane mesh for battlefield ground
// ============================================================================

// --- COLOR PALETTE ---
// Base grass colors - adjust these to change the overall look
uniform vec4 dark_grass : source_color = vec4(0.369, 0.541, 0.196, 1.0);    // #5e8a32
uniform vec4 mid_grass : source_color = vec4(0.459, 0.706, 0.255, 1.0);     // #75b441
uniform vec4 light_grass : source_color = vec4(0.616, 0.863, 0.361, 1.0);   // #9ddc5c
uniform vec4 highlight_grass : source_color = vec4(0.725, 0.902, 0.459, 1.0); // #b9e675

// Global tint for biome variations (fire = red, corrupted = purple, etc.)
uniform vec4 tint_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);

// --- NOISE LAYER CONTROLS ---
// Primary layer: Large, sweeping color variations (main patches)
uniform float primary_scale : hint_range(0.1, 10.0) = 2.0;
uniform float primary_intensity : hint_range(0.0, 1.0) = 0.7;

// Secondary layer: Medium-sized splotches for depth
uniform float secondary_scale : hint_range(0.1, 20.0) = 5.0;
uniform float secondary_intensity : hint_range(0.0, 1.0) = 0.4;

// Tertiary layer: Subtle brush stroke feel
uniform float tertiary_scale : hint_range(0.1, 30.0) = 12.0;
uniform float tertiary_intensity : hint_range(0.0, 1.0) = 0.2;

// --- DETAIL SPOTS (Green/Tan patches) ---
// Creates sparse colored spots across the grass
uniform float detail_scale : hint_range(1.0, 50.0) = 20.0;
uniform float detail_threshold : hint_range(0.0, 1.0) = 0.7;  // Higher = fewer spots
uniform float detail_intensity : hint_range(0.0, 1.0) = 0.3;
uniform vec4 detail_color_1 : source_color = vec4(0.2, 0.5, 0.2, 1.0);  // Dark green
uniform vec4 detail_color_2 : source_color = vec4(0.6, 0.5, 0.3, 1.0);  // Tan/brown

// --- VISUAL TWEAKS ---
// Overall contrast: higher = more variation between light/dark areas
uniform float contrast : hint_range(0.0, 2.0) = 1.0;

// Color spread: how much of the color ramp to use (0.5-1.0 recommended)
uniform float color_spread : hint_range(0.1, 1.0) = 0.8;

// Animation speed (optional): set to 0 for static grass
uniform float animation_speed : hint_range(0.0, 1.0) = 0.0;

// --- NOISE FUNCTIONS ---
// Simple 2D noise function (value noise approximation)
float hash(vec2 p) {
    p = fract(p * vec2(234.34, 435.345));
    p += dot(p, p + 34.23);
    return fract(p.x * p.y);
}

// Smooth 2D value noise
float noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);

    // Smooth interpolation (smoothstep for painterly feel)
    vec2 u = f * f * (3.0 - 2.0 * f);

    float a = hash(i + vec2(0.0, 0.0));
    float b = hash(i + vec2(1.0, 0.0));
    float c = hash(i + vec2(0.0, 1.0));
    float d = hash(i + vec2(1.0, 1.0));

    return mix(mix(a, b, u.x), mix(c, d, u.x), u.y);
}

// Fractal noise (combines multiple octaves for organic look)
float fbm(vec2 p, int octaves) {
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;

    for (int i = 0; i < octaves; i++) {
        value += amplitude * noise(p * frequency);
        frequency *= 2.0;
        amplitude *= 0.5;
    }

    return value;
}

// --- COLOR MAPPING ---
// Maps a noise value [0-1] through the grass color palette
vec3 map_to_grass_colors(float noise_value) {
    // Remap noise to use only part of the range for better control
    float t = noise_value * color_spread + (1.0 - color_spread) * 0.5;

    // Apply contrast
    t = (t - 0.5) * contrast + 0.5;
    t = clamp(t, 0.0, 1.0);

    // Four-color gradient mapping
    vec3 color;
    if (t < 0.33) {
        // Dark to mid grass
        color = mix(dark_grass.rgb, mid_grass.rgb, t / 0.33);
    } else if (t < 0.66) {
        // Mid to light grass
        color = mix(mid_grass.rgb, light_grass.rgb, (t - 0.33) / 0.33);
    } else {
        // Light to highlight grass
        color = mix(light_grass.rgb, highlight_grass.rgb, (t - 0.66) / 0.34);
    }

    return color;
}

void fragment() {
    // Get UV coordinates from the mesh
    vec2 uv = UV;

    // Optional animation offset
    float time_offset = TIME * animation_speed * 0.05;

    // --- LAYER 1: PRIMARY (Large patches) ---
    vec2 primary_uv = uv * primary_scale + vec2(time_offset, 0.0);
    float primary_noise = fbm(primary_uv, 3);

    // --- LAYER 2: SECONDARY (Medium splotches) ---
    vec2 secondary_uv = uv * secondary_scale + vec2(0.0, time_offset * 0.7);
    float secondary_noise = fbm(secondary_uv, 2);

    // --- LAYER 3: TERTIARY (Subtle brush strokes) ---
    vec2 tertiary_uv = uv * tertiary_scale + vec2(time_offset * 0.5, time_offset * 0.3);
    float tertiary_noise = noise(tertiary_uv);

    // --- COMBINE LAYERS ---
    // Weighted blend of all noise layers
    float combined_noise = primary_noise * primary_intensity +
                          secondary_noise * secondary_intensity +
                          tertiary_noise * tertiary_intensity;

    // Normalize to [0-1]
    float total_intensity = primary_intensity + secondary_intensity + tertiary_intensity;
    if (total_intensity > 0.0) {
        combined_noise /= total_intensity;
    }

    // --- MAP TO COLORS ---
    vec3 grass_color = map_to_grass_colors(combined_noise);

    // Apply global tint (for biome variations)
    grass_color *= tint_color.rgb;

    // --- LAYER 4: DETAIL SPOTS (Sparse green/tan patches) ---
    vec2 detail_uv = uv * detail_scale;
    float detail_noise = fbm(detail_uv, 2);

    // Apply threshold to make spots sparse
    if (detail_noise > detail_threshold) {
        // Determine if this spot is green or tan based on another noise sample
        vec2 color_select_uv = uv * detail_scale * 0.5 + vec2(100.0, 200.0);  // Offset for variation
        float color_selector = noise(color_select_uv);

        // Mix between dark green and tan
        vec3 spot_color = mix(detail_color_1.rgb, detail_color_2.rgb, color_selector);

        // Fade the spot edges based on how far above threshold
        float spot_strength = smoothstep(detail_threshold, 1.0, detail_noise) * detail_intensity;

        // Blend the spot color into the grass
        grass_color = mix(grass_color, spot_color, spot_strength);
    }

    // Output final color (spatial shader uses ALBEDO for unshaded materials)
    ALBEDO = grass_color;
    ALPHA = 1.0;
}
