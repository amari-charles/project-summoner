shader_type canvas_item;

// Element colors - passed from script based on summoner element
uniform vec4 color_primary : source_color = vec4(1.0, 0.478, 0.165, 1.0); // Fire orange
uniform vec4 color_secondary : source_color = vec4(0.722, 0.188, 0.125, 1.0); // Ember red
uniform vec4 color_accent : source_color = vec4(1.0, 0.85, 0.4, 1.0); // Bright highlight

// Animation parameters
uniform float time_scale : hint_range(0.1, 5.0) = 1.5;
uniform float cloud_scale : hint_range(1.0, 10.0) = 4.0;
uniform float flow_speed : hint_range(0.01, 0.2) = 0.05;

// Visual parameters
uniform float cloud_intensity : hint_range(0.0, 1.0) = 0.6;
uniform float glow_strength : hint_range(0.0, 1.0) = 0.4;
uniform float vignette_strength : hint_range(0.0, 0.8) = 0.3;

// Pseudo-random function
float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

// Smooth noise
float noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f); // Smoothstep

    float a = hash(i);
    float b = hash(i + vec2(1.0, 0.0));
    float c = hash(i + vec2(0.0, 1.0));
    float d = hash(i + vec2(1.0, 1.0));

    return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

// Fractional Brownian Motion - layered noise for organic clouds
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

void fragment() {
    vec2 uv = UV;
    float time = TIME * time_scale;

    // Create flowing motion - each layer moves differently
    vec2 flow1 = vec2(time * flow_speed, time * flow_speed * 0.7);
    vec2 flow2 = vec2(-time * flow_speed * 0.8, time * flow_speed * 0.5);
    vec2 flow3 = vec2(time * flow_speed * 0.6, -time * flow_speed * 0.4);

    // Sample FBM at different scales and positions for layered clouds
    float cloud1 = fbm((uv + flow1) * cloud_scale, 6);
    float cloud2 = fbm((uv + flow2) * cloud_scale * 1.5 + 10.0, 5);
    float cloud3 = fbm((uv + flow3) * cloud_scale * 0.8 + 20.0, 4);

    // Create cloud shapes with thresholds
    float shape1 = smoothstep(0.3, 0.7, cloud1);
    float shape2 = smoothstep(0.35, 0.65, cloud2);
    float shape3 = smoothstep(0.4, 0.8, cloud3);

    // Base color - rich element secondary color
    vec3 base_color = color_secondary.rgb * 0.7;

    // Layer the clouds with element colors
    vec3 final_color = base_color;

    // Layer 1: Deep secondary color clouds
    final_color = mix(final_color, color_secondary.rgb, shape1 * cloud_intensity * 0.8);

    // Layer 2: Primary color clouds (brighter)
    final_color = mix(final_color, color_primary.rgb, shape2 * cloud_intensity * 0.6);

    // Layer 3: Accent highlights (brightest spots)
    final_color = mix(final_color, color_accent.rgb, shape3 * cloud_intensity * 0.3);

    // Add subtle glow in bright areas
    float glow = (shape1 + shape2 + shape3) / 3.0;
    final_color += color_primary.rgb * glow * glow_strength * 0.3;

    // Vignette - subtle darkening at edges
    vec2 vignette_uv = uv * 2.0 - 1.0;
    float vignette = 1.0 - dot(vignette_uv, vignette_uv) * vignette_strength;
    vignette = clamp(vignette, 0.0, 1.0);
    final_color *= vignette;

    // Keep readable for UI - don't go too bright
    final_color = clamp(final_color, vec3(0.0), vec3(0.95));

    COLOR = vec4(final_color, 1.0);
}
