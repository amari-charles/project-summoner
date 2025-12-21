shader_type canvas_item;

// Element colors - passed from script based on summoner element
uniform vec4 color_primary : source_color = vec4(1.0, 0.478, 0.165, 1.0); // Brighter
uniform vec4 color_secondary : source_color = vec4(0.722, 0.188, 0.125, 1.0); // Darker

// Gradient control
uniform float gradient_angle : hint_range(0.0, 360.0) = 135.0; // Diagonal by default
uniform float gradient_smoothness : hint_range(0.1, 2.0) = 1.0;

// Vignette
uniform float vignette_strength : hint_range(0.0, 1.0) = 0.4;
uniform float vignette_softness : hint_range(0.1, 1.0) = 0.5;

void fragment() {
    vec2 uv = UV;

    // Calculate gradient based on angle
    float angle_rad = radians(gradient_angle);
    float gradient = uv.x * cos(angle_rad) + uv.y * sin(angle_rad);
    gradient = gradient * gradient_smoothness;
    gradient = clamp(gradient, 0.0, 1.0);

    // Smooth gradient between colors
    vec3 color = mix(color_secondary.rgb, color_primary.rgb, gradient);

    // Vignette - darken edges softly
    vec2 vignette_uv = uv * 2.0 - 1.0;
    float vignette_dist = length(vignette_uv);
    float vignette = 1.0 - smoothstep(1.0 - vignette_softness, 1.0 + vignette_softness, vignette_dist);
    vignette = mix(1.0, vignette, vignette_strength);

    color *= vignette;

    COLOR = vec4(color, 1.0);
}
