extends Character2D5Component
class_name SpriteCharacter2D5Component

## Sprite-based 2.5D Character Rendering Component
## Renders 2D sprite animations in 3D space using AnimatedSprite2D + SubViewport

## Sprite anchor point in viewport (0-1 range)
## X: 0=left, 0.5=center, 1=right
## Y: 0=top, 0.5=center, 1=bottom (feet on ground)
@export var sprite_anchor: Vector2 = Vector2(0.5, 1.0)

@onready var sprite_3d: Sprite3D = $Sprite3D
@onready var viewport: SubViewport = $Sprite3D/SubViewport
@onready var character_sprite: AnimatedSprite2D = $Sprite3D/SubViewport/Model2D/CharacterSprite

func _ready() -> void:
	# Force viewport to render every frame
	viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS

	# Position sprite based on anchor
	_update_sprite_position()

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

## Position the 2D sprite within viewport based on anchor point
## This ensures character feet align with the bottom edge when anchor.y = 1.0
func _update_sprite_position() -> void:
	if not character_sprite or not viewport:
		return

	# Get viewport size
	var viewport_size = viewport.size

	# Calculate position based on anchor
	# anchor (0.5, 1.0) means center-bottom
	var target_pos = Vector2(
		viewport_size.x * sprite_anchor.x,
		viewport_size.y * sprite_anchor.y
	)

	character_sprite.position = target_pos
	print("SpriteCharacter2D5: Positioned sprite at %v (viewport: %v, anchor: %v)" % [target_pos, viewport_size, sprite_anchor])
