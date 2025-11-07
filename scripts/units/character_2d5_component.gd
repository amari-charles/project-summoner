extends Node3D
class_name Character2D5Component

## 2.5D Character Rendering Component
## Renders 2D sprite animations in 3D space using Sprite3D + SubViewport

@onready var sprite_3d: Sprite3D = $Sprite3D
@onready var viewport: SubViewport = $Sprite3D/SubViewport
@onready var character_sprite: AnimatedSprite2D = $Sprite3D/SubViewport/Model2D/CharacterSprite

func _ready() -> void:
	# Force viewport to render every frame
	viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS

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
