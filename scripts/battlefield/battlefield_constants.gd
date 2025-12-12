extends Node
class_name BattlefieldConstants

## Constants for battlefield coordinate conversion and configuration
## Used by AI, UI, and game systems to convert between screen space and world space

## Screen space reference dimensions (based on default viewport size)
## Used as the center point for coordinate conversion
const SCREEN_CENTER_X: float = 960.0  # Half of 1920 (default width)
const SCREEN_CENTER_Y: float = 540.0  # Half of 1080 (default height)

## World space scale factor
## Converts screen pixels to world units (higher = smaller world scale)
const SCREEN_TO_WORLD_SCALE: float = 100.0

## Ground plane Y-coordinate
## All units walk on this plane, shadows project here, ground VFX spawn here
const GROUND_Y: float = 0.0

## Spawn plane height for 3D units (same as ground for grounded units)
## Y-coordinate where units spawn in 3D battlefield
const SPAWN_PLANE_HEIGHT: float = GROUND_Y

## Raycast distance for screen-to-world conversion
const RAYCAST_DISTANCE: float = 1000.0

## Helper function: Convert 2D screen position to 3D world position
static func screen_to_world_3d(screen_pos: Vector2) -> Vector3:
	return Vector3(
		(screen_pos.x - SCREEN_CENTER_X) / SCREEN_TO_WORLD_SCALE,
		SPAWN_PLANE_HEIGHT,
		(screen_pos.y - SCREEN_CENTER_Y) / SCREEN_TO_WORLD_SCALE
	)

## Helper function: Convert 3D world position to 2D screen position (approximate)
static func world_to_screen_2d(world_pos: Vector3) -> Vector2:
	return Vector2(
		world_pos.x * SCREEN_TO_WORLD_SCALE + SCREEN_CENTER_X,
		world_pos.z * SCREEN_TO_WORLD_SCALE + SCREEN_CENTER_Y
	)

## Battlefield dimensions (world units)
## Used for spawn zone overlays and boundary calculations
const BATTLEFIELD_HALF_WIDTH: float = 50.0  ## Half the X-axis extent (-50 to +50)
const BATTLEFIELD_HALF_DEPTH: float = 40.0  ## Half the Z-axis extent (-40 to +40)

## Spawn zone boundary (halfway mark of battlefield)
## Player (team 0) spawns at X < 0, Enemy (team 1) spawns at X > 0
const SPAWN_BOUNDARY_X: float = 0.0

## Spawn position constants
const MIN_UNIT_SPACING: float = 1.5  ## Minimum distance between unit centers
const SPAWN_SEARCH_ATTEMPTS: int = 8  ## Number of positions to check in each ring
const SPAWN_SEARCH_RINGS: int = 3  ## Number of expanding rings to search

## Find a safe spawn position that doesn't overlap with existing units
## Uses spiral search pattern: checks desired position first, then expands outward
static func find_safe_spawn_position(desired_pos: Vector3, scene_tree: SceneTree, spawning_collision_radius: float = 0.5) -> Vector3:
	if not scene_tree:
		return desired_pos

	# Check if desired position is safe
	if is_spawn_position_safe(desired_pos, scene_tree, spawning_collision_radius):
		return desired_pos

	# Search in expanding rings around desired position
	for ring: int in range(1, SPAWN_SEARCH_RINGS + 1):
		var radius: float = MIN_UNIT_SPACING * ring
		for attempt: int in range(SPAWN_SEARCH_ATTEMPTS):
			var angle: float = (float(attempt) / SPAWN_SEARCH_ATTEMPTS) * TAU
			var offset: Vector3 = Vector3(cos(angle) * radius, 0, sin(angle) * radius)
			var test_pos: Vector3 = desired_pos + offset

			if is_spawn_position_safe(test_pos, scene_tree, spawning_collision_radius):
				return test_pos

	# Fallback: no safe position found, use desired (units will overlap but game continues)
	return desired_pos

## Check if a spawn position is safe (no existing units too close)
static func is_spawn_position_safe(check_pos: Vector3, scene_tree: SceneTree, spawning_collision_radius: float = 0.5) -> bool:
	var all_units: Array[Node] = scene_tree.get_nodes_in_group(GroupIDs.UNITS)

	for node: Node in all_units:
		if not node is Unit3D:
			continue

		var unit: Unit3D = node as Unit3D
		if not unit.is_alive:
			continue

		# Minimum spacing is sum of both collision radii
		var min_spacing: float = spawning_collision_radius + unit.collision_radius
		var spacing_sq: float = min_spacing * min_spacing

		# Check 2D distance (ignore Y-axis)
		var delta: Vector3 = unit.global_position - check_pos
		var distance_sq: float = delta.x * delta.x + delta.z * delta.z

		if distance_sq < spacing_sq:
			return false

	return true

## Check if spawn position is valid for the given team
## Player (team 0) can spawn at X <= 0, Enemy (team 1) can spawn at X > 0
static func is_valid_spawn_position_for_team(pos: Vector3, team: int) -> bool:
	if team == Unit3D.Team.PLAYER:
		return pos.x <= SPAWN_BOUNDARY_X
	else:  # Unit3D.Team.ENEMY
		return pos.x > SPAWN_BOUNDARY_X

## Clamp spawn position to valid zone for the given team
## Snaps X coordinate to boundary if in invalid territory
static func clamp_spawn_position_for_team(pos: Vector3, team: int) -> Vector3:
	var clamped: Vector3 = pos
	if team == Unit3D.Team.PLAYER and pos.x > SPAWN_BOUNDARY_X:
		clamped.x = SPAWN_BOUNDARY_X
	elif team == Unit3D.Team.ENEMY and pos.x < SPAWN_BOUNDARY_X:
		clamped.x = SPAWN_BOUNDARY_X
	return clamped
