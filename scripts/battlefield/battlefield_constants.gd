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

## Spawn plane height for 3D units
## Y-coordinate where units spawn in 3D battlefield
const SPAWN_PLANE_HEIGHT: float = 1.0

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
