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

## Height offset for ground overlays to prevent z-fighting
const GROUND_OVERLAY_OFFSET: float = 0.02

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
const BATTLEFIELD_HALF_DEPTH: float = 25.0  ## Half the Z-axis extent (-25 to +25)

## Spawn zone boundary (halfway mark of battlefield)
## Player (team 0) spawns at X ≤ 0, Enemy (team 1) spawns at X > 0
const SPAWN_BOUNDARY_X: float = 0.0
const SPAWN_BOUNDARY_EPSILON: float = 0.001  ## Small offset to ensure enemy clamps to valid position

## Spawn-side validation/clamping ownership lives in C# BattlefieldBounds.
## Keep this script focused on visual/layout constants and screen/world conversion helpers.
