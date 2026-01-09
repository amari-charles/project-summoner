shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_never, depth_test_disabled, blend_mix;

// Billboard mode - always face camera
void vertex() {
    // Billboard: use camera orientation but keep object's world position
    MODELVIEW_MATRIX = VIEW_MATRIX * mat4(
        vec4(INV_VIEW_MATRIX[0].xyz, 0.0),
        vec4(INV_VIEW_MATRIX[1].xyz, 0.0),
        vec4(INV_VIEW_MATRIX[2].xyz, 0.0),
        MODEL_MATRIX[3]
    );
}

// HP bar fill percentages (0.0 - 1.0)
uniform float hp_percent : hint_range(0.0, 1.0) = 1.0;
uniform float display_percent : hint_range(0.0, 1.0) = 1.0; // Smoothly animated display
uniform float shield_percent : hint_range(0.0, 1.0) = 0.0; // Shield overlay

// Color configuration
uniform vec4 color_full : source_color = vec4(0.0, 1.0, 0.0, 1.0);   // Green
uniform vec4 color_mid : source_color = vec4(1.0, 1.0, 0.0, 1.0);    // Yellow
uniform vec4 color_low : source_color = vec4(1.0, 0.0, 0.0, 1.0);    // Red
uniform vec4 color_background : source_color = vec4(0.2, 0.2, 0.2, 0.8);
uniform vec4 color_shield : source_color = vec4(0.4, 0.8, 1.0, 0.8); // Cyan shield overlay

// Color thresholds (configurable)
uniform float threshold_mid : hint_range(0.0, 1.0) = 0.5;  // Below this = interpolate low->mid
uniform float threshold_low : hint_range(0.0, 1.0) = 0.25; // Below this = pure low color

// Border configuration
uniform float border_width : hint_range(0.0, 0.1) = 0.02;
uniform vec4 border_color : source_color = vec4(0.1, 0.1, 0.1, 1.0);

// Damage flash effect
uniform float damage_flash : hint_range(0.0, 1.0) = 0.0;
uniform vec4 damage_flash_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);

vec3 get_hp_color(float percent) {
    if (percent > threshold_mid) {
        // Interpolate between mid and full
        float t = (percent - threshold_mid) / (1.0 - threshold_mid);
        return mix(color_mid.rgb, color_full.rgb, t);
    } else if (percent > threshold_low) {
        // Interpolate between low and mid
        float t = (percent - threshold_low) / (threshold_mid - threshold_low);
        return mix(color_low.rgb, color_mid.rgb, t);
    } else {
        // Pure low color
        return color_low.rgb;
    }
}

void fragment() {
    vec2 uv = UV;
    vec3 final_color;
    float final_alpha;

    // Check if we're in the border region
    bool in_border = uv.x < border_width || uv.x > (1.0 - border_width) ||
                     uv.y < border_width || uv.y > (1.0 - border_width);

    if (in_border && border_width > 0.0) {
        // Border region
        final_color = border_color.rgb;
        final_alpha = border_color.a;
    } else {
        // Adjust UV to inner region (after border)
        vec2 inner_uv = vec2(
            (uv.x - border_width) / (1.0 - 2.0 * border_width),
            (uv.y - border_width) / (1.0 - 2.0 * border_width)
        );

        // Determine what to render based on horizontal position
        if (inner_uv.x <= display_percent) {
            // HP bar fill region
            vec3 hp_color = get_hp_color(hp_percent);

            // Apply damage flash
            hp_color = mix(hp_color, damage_flash_color.rgb, damage_flash);

            // Shield overlay (renders on top of HP)
            if (shield_percent > 0.0 && inner_uv.x <= shield_percent) {
                // Blend shield color additively
                hp_color = mix(hp_color, color_shield.rgb, 0.5);
            }

            final_color = hp_color;
            final_alpha = 1.0;
        } else {
            // Background region
            final_color = color_background.rgb;
            final_alpha = color_background.a;
        }
    }

    ALBEDO = final_color;
    ALPHA = final_alpha;
}
