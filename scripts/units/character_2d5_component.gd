extends Node3D
class_name Character2D5Component

## Base class for 2.5D character rendering components
## Defines the interface that all character rendering implementations must follow
##
## This is an abstract class - do not use directly!
## Use one of the implementations:
##   - SpriteCharacter2D5Component (for AnimatedSprite2D/sprite frames)
##   - SkeletalCharacter2D5Component (for Skeleton2D/AnimationPlayer)

## Play an animation
## @virtual
func play_animation(anim_name: String, auto_play: bool = false) -> void:
	push_error("Character2D5Component.play_animation() called on base class - must be overridden in child class")

## Stop current animation
## @virtual
func stop_animation() -> void:
	push_error("Character2D5Component.stop_animation() called on base class - must be overridden in child class")

## Get current animation name
## @virtual
func get_current_animation() -> String:
	push_error("Character2D5Component.get_current_animation() called on base class - must be overridden in child class")
	return ""

## Check if animation is playing
## @virtual
func is_playing() -> bool:
	push_error("Character2D5Component.is_playing() called on base class - must be overridden in child class")
	return false

## Flip the sprite horizontally (for enemy units)
## @virtual
func set_flip_h(flip: bool) -> void:
	push_error("Character2D5Component.set_flip_h() called on base class - must be overridden in child class")

## Get the duration of an animation in seconds
## @virtual
func get_animation_duration(anim_name: String) -> float:
	push_error("Character2D5Component.get_animation_duration() called on base class - must be overridden in child class")
	return 1.0  # Fallback duration

## Get the world-space height of this sprite (used for HP bars, projectile spawns, etc.)
## @virtual
func get_sprite_height() -> float:
	push_error("Character2D5Component.get_sprite_height() called on base class - must be overridden in child class")
	return 3.0  # Fallback height
