extends RefCounted
class_name CutoutRenderOrder

## Shared painter ordering for camera-facing 2D cutouts in the Academy's 3D world.
## The campus camera looks from positive Z toward negative Z, so cutouts whose
## feet have a greater world Z render later and appear in front as a whole.

const PRIORITY_SCALE: float = 4.0
const MIN_RENDER_PRIORITY: int = -128
const MAX_RENDER_PRIORITY: int = 127


static func apply_from_feet(sprite: GeometryInstance3D, feet_world_z: float) -> void:
	sprite.render_priority = priority_for_feet(feet_world_z)


static func priority_for_feet(feet_world_z: float) -> int:
	return clampi(roundi(feet_world_z * PRIORITY_SCALE), MIN_RENDER_PRIORITY, MAX_RENDER_PRIORITY)
