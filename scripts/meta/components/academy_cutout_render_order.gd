extends RefCounted

## Painter ordering for camera-facing 2D cutouts in the walkable Academy hub.
## The campus camera looks from positive Z toward negative Z, so cutouts whose
## feet have a greater world Z render later and appear in front as a whole.

const PRIORITY_SCALE: float = 4.0
const MIN_RENDER_PRIORITY: int = -128
const MAX_RENDER_PRIORITY: int = 127


static func apply_from_feet(sprite: GeometryInstance3D, feet_world_z: float) -> void:
	# SpriteBase3D render priority only controls transparent ordering when alpha
	# cutout mode is disabled. Keep the helper's ordering contract explicit.
	if sprite is SpriteBase3D:
		(sprite as SpriteBase3D).alpha_cut = SpriteBase3D.ALPHA_CUT_DISABLED
	sprite.render_priority = priority_for_feet(feet_world_z)


static func priority_for_feet(feet_world_z: float) -> int:
	return clampi(roundi(feet_world_z * PRIORITY_SCALE), MIN_RENDER_PRIORITY, MAX_RENDER_PRIORITY)


static func point_above_visible_art(
	feet_world_position: Vector3,
	camera_basis: Basis,
	visible_art_height: float,
	gap: float,
	depth_offset: float
) -> Vector3:
	var screen_up: Vector3 = camera_basis.y.normalized()
	var toward_camera: Vector3 = camera_basis.z.normalized()
	return feet_world_position + screen_up * (visible_art_height + gap) + toward_camera * depth_offset


static func anchor_visible_bottom(sprite: SpriteBase3D, texture: Texture2D) -> void:
	var image: Image = texture.get_image()
	var bottom_padding: float = 0.0
	if image != null and not image.is_empty():
		var used_rect: Rect2i = image.get_used_rect()
		if used_rect.size != Vector2i.ZERO:
			bottom_padding = texture.get_height() - used_rect.end.y
	# Sprite3D offset Y is inverted relative to texture coordinates: increasing
	# it moves the art upward. This places the lowest visible pixel at the node.
	sprite.offset.y = texture.get_height() * 0.5 - bottom_padding
