extends Character2D5Component
class_name SpriteCharacter2D5Component

## Sprite-based 2.5D Character Rendering Component
## Renders 2D sprite animations in 3D space using AnimatedSprite2D + SubViewport

## Sprite scaling constants
const VIEWPORT_SIZE: int = 250  ## Fixed viewport height in pixels
const LEGACY_SPRITE_SIZE: int = 100  ## Original sprite height (soldier, archer, etc.)
const DEFAULT_SPRITE_SCALE: float = 2.5  ## Default scale for 100px sprites (250 / 100 = 2.5)

## Offset in pixels from texture bottom to actual character feet
## Use this when sprite artwork has empty space below the character's feet
## Example: 100px texture with feet at 70px from top = 30px offset
@export var feet_offset_pixels: float = 0.0
@export var hp_bar_offset_x: float = 0.0  ## Horizontal offset for HP bar in world units (negative = left, positive = right)
## Scale for sprite within viewport
## Use calculate_sprite_scale() helper or calculate manually: VIEWPORT_SIZE / sprite_height
@export var sprite_scale: float = DEFAULT_SPRITE_SCALE

@onready var sprite_3d: Sprite3D = $Sprite3D
@onready var viewport: SubViewport = $Sprite3D/SubViewport
@onready var character_sprite: AnimatedSprite2D = $Sprite3D/SubViewport/Model2D/CharacterSprite

func _ready() -> void:
	# Force viewport to render every frame
	viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS

	# Apply sprite scale
	character_sprite.scale = Vector2(sprite_scale, sprite_scale)

	# Bottom-align sprite using offset (feet at origin)
	_setup_sprite_alignment()

## Set the SpriteFrames resource for this character
func set_sprite_frames(frames: SpriteFrames) -> void:
	if character_sprite:
		character_sprite.sprite_frames = frames
		# Recalculate alignment now that we have actual texture data
		_setup_sprite_alignment()

## Flip the sprite horizontally
func set_flip_h(_flip: bool) -> void:
	if character_sprite:
		character_sprite.flip_h = _flip

## Play an animation
func play_animation(_anim_name: String, _auto_play: bool = false) -> void:
	if character_sprite and character_sprite.sprite_frames:
		# Check if animation exists before trying to play it
		if character_sprite.sprite_frames.has_animation(_anim_name):
			character_sprite.animation = _anim_name
			if _auto_play:
				character_sprite.autoplay = _anim_name
			character_sprite.play()
		else:
			push_warning("Animation '%s' not found in sprite_frames, falling back to 'idle'" % _anim_name)
			if character_sprite.sprite_frames.has_animation("idle"):
				character_sprite.animation = "idle"
				character_sprite.play()

## Stop current animation
func stop_animation() -> void:
	if character_sprite:
		character_sprite.stop()

## Get current animation name
func get_current_animation() -> String:
	if character_sprite:
		return character_sprite.animation
	return ""

## Check if animation is playing
func is_playing() -> bool:
	if character_sprite:
		return character_sprite.is_playing()
	return false

## Get the duration of an animation in seconds
func get_animation_duration(_anim_name: String) -> float:
	if character_sprite and character_sprite.sprite_frames:
		var frames: SpriteFrames = character_sprite.sprite_frames
		if frames.has_animation(_anim_name):
			var frame_count: int = frames.get_frame_count(_anim_name)
			var fps: float = frames.get_animation_speed(_anim_name)
			if fps > 0:
				return frame_count / fps
	return 1.0  # Fallback duration

## Setup sprite alignment so character feet are at origin (Y=0)
## Positions BOTH the Sprite3D and the 2D sprite content within viewport
func _setup_sprite_alignment() -> void:
	if not sprite_3d or not viewport or not character_sprite:
		return

	# Calculate actual sprite height in world units
	var world_height: float = viewport.size.y * sprite_3d.pixel_size  # VIEWPORT_SIZE * pixel_size

	# Position Sprite3D so viewport bottom is at Y=0
	sprite_3d.position.y = world_height / 2.0  # 3.125 for standard sprites

	# Get actual texture size for precise feet positioning
	var texture_size: Vector2 = _get_current_frame_size()

	if texture_size.y > 0:
		# PRECISE: Calculate position so sprite's actual feet align with viewport bottom
		# With centered=true: feet_y = sprite.position.y + ((texture_height / 2) - feet_offset) * sprite.scale.y
		# We want: feet at viewport bottom, accounting for empty space below feet
		# Therefore: sprite.position.y = viewport.size.y - ((texture_height / 2) - feet_offset) * sprite.scale.y
		character_sprite.position.y = viewport.size.y - ((texture_size.y / 2.0) - feet_offset_pixels) * character_sprite.scale.y
	else:
		# FALLBACK: No texture data available yet, use approximate positioning
		character_sprite.position.y = viewport.size.y * 0.8

## Get the world-space height of this sprite
## Used by HP bars, projectile spawns, etc.
func get_sprite_height() -> float:
	assert(viewport != null, "SpriteChar2D5: viewport is null")
	assert(sprite_3d != null, "SpriteChar2D5: sprite_3d is null")

	# Get actual texture size to calculate real character height
	var texture_size: Vector2 = _get_current_frame_size()

	if texture_size.y > 0:
		# Actual sprite height in world units, accounting for feet offset
		return (texture_size.y - feet_offset_pixels) * sprite_3d.pixel_size

	# Fallback: use viewport height (sprite frames not loaded yet)
	push_warning("SpriteChar2D5: No texture data available, using viewport fallback. Check sprite_frames configuration.")
	return viewport.size.y * sprite_3d.pixel_size

## Get the horizontal offset for HP bar positioning
func get_hp_bar_offset_x() -> float:
	return hp_bar_offset_x

## Get the size of the current sprite frame texture
## Returns Vector2.ZERO if no texture available
func _get_current_frame_size() -> Vector2:
	if not character_sprite or not character_sprite.sprite_frames:
		return Vector2.ZERO

	# Get current animation name (default to "idle" if not set)
	var anim: String = character_sprite.animation
	if anim == "":
		anim = "idle"

	# Check if animation exists
	if not character_sprite.sprite_frames.has_animation(anim):
		return Vector2.ZERO

	# Get frame count
	var frame_count: int = character_sprite.sprite_frames.get_frame_count(anim)
	if frame_count == 0:
		return Vector2.ZERO

	# Get first frame texture (assume all frames same size)
	var frame_texture: Texture2D = character_sprite.sprite_frames.get_frame_texture(anim, 0)
	if not frame_texture:
		return Vector2.ZERO

	return frame_texture.get_size()

## =============================================================================
## STATIC HELPER METHODS
## =============================================================================

## Calculate appropriate sprite scale for a given sprite height
## This ensures sprites of different sizes render at consistent world scale
static func calculate_sprite_scale(sprite_height_pixels: int) -> float:
	if sprite_height_pixels <= 0:
		push_warning("SpriteChar2D5: Invalid sprite height %d, using default scale" % sprite_height_pixels)
		return DEFAULT_SPRITE_SCALE
	return float(VIEWPORT_SIZE) / float(sprite_height_pixels)
