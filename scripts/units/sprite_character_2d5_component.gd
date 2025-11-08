extends Character2D5Component
class_name SpriteCharacter2D5Component

## Sprite-based 2.5D Character Rendering Component
## Renders 2D sprite animations in 3D space using AnimatedSprite2D + SubViewport

@onready var sprite_3d: Sprite3D = $Sprite3D
@onready var viewport: SubViewport = $Sprite3D/SubViewport
@onready var character_sprite: AnimatedSprite2D = $Sprite3D/SubViewport/Model2D/CharacterSprite

func _ready() -> void:
	# Force viewport to render every frame
	viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS

	# Bottom-align sprite using offset (feet at origin)
	_setup_sprite_alignment()

## Set the SpriteFrames resource for this character
func set_sprite_frames(frames: SpriteFrames) -> void:
	if character_sprite:
		character_sprite.sprite_frames = frames

## Flip the sprite horizontally
func set_flip_h(flip: bool) -> void:
	if character_sprite:
		character_sprite.flip_h = flip

## Play an animation
func play_animation(anim_name: String, auto_play: bool = false) -> void:
	if character_sprite and character_sprite.sprite_frames:
		character_sprite.animation = anim_name
		if auto_play:
			character_sprite.autoplay = anim_name
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
func get_animation_duration(anim_name: String) -> float:
	if character_sprite and character_sprite.sprite_frames:
		var frames = character_sprite.sprite_frames
		if frames.has_animation(anim_name):
			var frame_count = frames.get_frame_count(anim_name)
			var fps = frames.get_animation_speed(anim_name)
			if fps > 0:
				return frame_count / fps
	return 1.0  # Fallback duration

## Setup sprite alignment so character feet are at origin (Y=0)
## Uses simple Y positioning - proven to work
func _setup_sprite_alignment() -> void:
	if not sprite_3d or not viewport:
		return

	# Calculate actual sprite height in world units
	var world_height = viewport.size.y * sprite_3d.pixel_size  # 250 * 0.025 = 6.25

	# Position sprite so bottom is at Y=0
	# With centered=true, center needs to be at half-height
	sprite_3d.position.y = world_height / 2.0  # 3.125 for standard sprites

## Get the world-space height of this sprite
## Used by HP bars, projectile spawns, etc.
func get_sprite_height() -> float:
	if not viewport or not sprite_3d:
		return 3.0  # Fallback

	# Total height = viewport pixels × pixel_size
	return viewport.size.y * sprite_3d.pixel_size
