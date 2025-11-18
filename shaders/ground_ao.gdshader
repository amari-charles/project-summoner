shader_type spatial;

// Base appearance
uniform vec3 ground_color : source_color = vec3(0.3, 0.5, 0.3); // Grass green placeholder
uniform float ground_roughness : hint_range(0.0, 1.0) = 0.9;

// AO parameters
uniform float ao_strength : hint_range(0.0, 1.0) = 0.4;
uniform float ao_radius = 15.0; // Size of AO zones
uniform float ao_falloff = 2.0; // How quickly AO fades

// Directional shading
uniform vec3 light_direction = vec3(0.866, -0.433, 0.25); // Match DirectionalLight3D
uniform float directional_strength : hint_range(0.0, 1.0) = 0.15;

// Edge darkening
uniform float edge_darkening : hint_range(0.0, 1.0) = 0.2;
uniform vec2 battlefield_size = vec2(100.0, 80.0);

// Pass world position from vertex to fragment shader
varying vec3 world_pos;

void vertex() {
	// Calculate world position in vertex shader
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
	// Use world_pos from varying (calculated in vertex shader)

	// Player spawn AO (-40, 0, -7.5)
	vec2 player_spawn = vec2(-40.0, -7.5);
	float player_dist = distance(world_pos.xz, player_spawn);
	float player_ao = smoothstep(ao_radius, 0.0, player_dist);

	// Enemy spawn AO (40, 0, -7.5)
	vec2 enemy_spawn = vec2(40.0, -7.5);
	float enemy_dist = distance(world_pos.xz, enemy_spawn);
	float enemy_ao = smoothstep(ao_radius, 0.0, enemy_dist);

	// Combine AO zones
	float ao = max(player_ao, enemy_ao);
	ao = pow(ao, ao_falloff); // Soften the falloff

	// Directional gradient (darker opposite to light)
	vec3 to_light = normalize(light_direction);
	float directional = dot(normalize(vec3(world_pos.x, 0.0, world_pos.z)), to_light.xz);
	directional = directional * 0.5 + 0.5; // Remap to 0-1
	float directional_shadow = 1.0 - (directional * directional_strength);

	// Edge darkening
	vec2 edge_dist = abs(world_pos.xz) / (battlefield_size * 0.5);
	float edge_factor = max(edge_dist.x, edge_dist.y);
	edge_factor = smoothstep(0.7, 1.0, edge_factor);
	float edge_shadow = 1.0 - (edge_factor * edge_darkening);

	// Combine all darkening factors
	float total_darkening = ao * ao_strength + (1.0 - directional_shadow) + (1.0 - edge_shadow);
	total_darkening = clamp(total_darkening, 0.0, 1.0);

	// Apply to base color
	vec3 final_color = ground_color * (1.0 - total_darkening);

	ALBEDO = final_color;
	ROUGHNESS = ground_roughness;
}
