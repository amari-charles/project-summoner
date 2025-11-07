extends Resource
class_name AnimationStateData

## Configuration for a single animation state
## Used by UnitAnimationController for state machine

@export var state_name: String = ""  ## "idle", "walk", "attack", "death", etc.
@export var animation_name: String = ""  ## Name in SpriteFrames
@export var loop: bool = true  ## Should animation loop
@export var priority: int = 0  ## Higher priority interrupts lower priority
@export var speed_scale: float = 1.0  ## Animation speed multiplier
@export var can_be_interrupted: bool = true  ## Can be interrupted by higher priority states

## Transition rules
@export var auto_transition_to: String = ""  ## Auto transition when animation finishes
@export var transition_delay: float = 0.0  ## Delay before auto transition

## Event markers (frame numbers where events occur)
@export var damage_frame: int = -1  ## Frame where damage is dealt
@export var projectile_spawn_frame: int = -1  ## Frame where projectile spawns
@export var footstep_frames: Array[int] = []  ## Frames where footstep sounds play

## VFX triggers
@export var vfx_on_start: String = ""  ## VFX to play when state starts
@export var vfx_on_end: String = ""  ## VFX to play when state ends
@export var vfx_on_damage_frame: String = ""  ## VFX to play at damage frame

## Audio
@export var sound_on_start: AudioStream = null  ## Sound to play when state starts
@export var sound_volume: float = 0.0  ## Volume in dB

## Metadata for custom logic
@export var metadata: Dictionary = {}

func _init(
	p_state_name: String = "",
	p_animation_name: String = "",
	p_loop: bool = true,
	p_priority: int = 0
) -> void:
	state_name = p_state_name
	animation_name = p_animation_name
	loop = p_loop
	priority = p_priority
