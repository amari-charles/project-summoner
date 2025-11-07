extends Node
class_name UnitAnimationController

## Animation state machine for units
## Manages animation states, transitions, and frame events
## Usage: Add as child of Unit3D, call play_state("attack")

## Signals
signal state_changed(old_state: String, new_state: String)
signal animation_finished(state_name: String)
signal frame_event_triggered(event_type: String, frame: int)  ## "damage", "projectile_spawn", "footstep"

## Configuration
@export var animation_config: UnitAnimationConfig = null

## State
var current_state: AnimationStateData = null
var current_state_name: String = ""
var state_time: float = 0.0
var is_transitioning: bool = false

## References
var sprite: AnimatedSprite2D = null
var visual_component: Character2D5Component = null
var unit: Node3D = null

## Frame tracking for events
var last_frame: int = -1
var triggered_frames: Dictionary = {}  ## Track which frame events have fired this animation

func _ready() -> void:
	# Find sprite and unit references
	_find_references()

	# Set initial state
	if animation_config:
		play_state(animation_config.default_state)

func _process(delta: float) -> void:
	if not current_state or not sprite:
		return

	state_time += delta

	# Check for frame events
	_check_frame_events()

	# Check for auto transition
	if not current_state.loop and sprite.frame >= sprite.sprite_frames.get_frame_count(sprite.animation) - 1:
		_on_animation_finished()

func _find_references() -> void:
	# Find the Character2D5Component
	visual_component = _find_child_of_type(get_parent(), Character2D5Component)

	# Get sprite from visual component
	if visual_component:
		# Access the AnimatedSprite2D inside the component
		sprite = visual_component.get_node_or_null("Sprite3D/SubViewport/Model2D/CharacterSprite")

	# Get unit reference
	unit = get_parent()

	if not sprite:
		push_error("UnitAnimationController: No AnimatedSprite2D found in Character2D5Component")

func _find_child_of_type(node: Node, type) -> Node:
	for child in node.get_children():
		if is_instance_of(child, type):
			return child
		var result = _find_child_of_type(child, type)
		if result:
			return result
	return null

## Play a new animation state
func play_state(state_name: String, force: bool = false) -> bool:
	if not animation_config or not animation_config.has_state(state_name):
		push_warning("UnitAnimationController: State '%s' not found" % state_name)
		return false

	var new_state = animation_config.get_state(state_name)

	# Check if we can interrupt current state
	if current_state and not force:
		if not current_state.can_be_interrupted and new_state.priority <= current_state.priority:
			return false

	# Store old state
	var old_state_name = current_state_name

	# Transition to new state
	current_state = new_state
	current_state_name = state_name
	state_time = 0.0
	last_frame = -1
	triggered_frames.clear()

	# Update sprite animation
	if sprite and animation_config.sprite_frames:
		sprite.sprite_frames = animation_config.sprite_frames
		if sprite.sprite_frames.has_animation(new_state.animation_name):
			sprite.play(new_state.animation_name)
			sprite.speed_scale = new_state.speed_scale
		else:
			push_warning("UnitAnimationController: Animation '%s' not found in SpriteFrames" % new_state.animation_name)

	# Trigger VFX on start
	if not new_state.vfx_on_start.is_empty() and unit:
		VFXManager.play_effect(new_state.vfx_on_start, unit.global_position)

	# Play sound on start
	if new_state.sound_on_start and unit:
		_play_sound(new_state.sound_on_start, new_state.sound_volume)

	# Emit state changed signal
	if old_state_name != state_name:
		state_changed.emit(old_state_name, state_name)

	return true

## Check current frame for events
func _check_frame_events() -> void:
	if not sprite or not current_state:
		return

	var current_frame = sprite.frame

	# Only process if we're on a new frame
	if current_frame == last_frame:
		return

	last_frame = current_frame

	# Check damage frame
	if current_state.damage_frame == current_frame and not triggered_frames.get("damage", false):
		triggered_frames["damage"] = true
		frame_event_triggered.emit("damage", current_frame)

	# Check projectile spawn frame
	if current_state.projectile_spawn_frame == current_frame and not triggered_frames.get("projectile", false):
		triggered_frames["projectile"] = true
		frame_event_triggered.emit("projectile_spawn", current_frame)

	# Check footstep frames
	if current_frame in current_state.footstep_frames:
		var footstep_key = "footstep_%d" % current_frame
		if not triggered_frames.get(footstep_key, false):
			triggered_frames[footstep_key] = true
			frame_event_triggered.emit("footstep", current_frame)

	# Trigger VFX on damage frame
	if current_state.damage_frame == current_frame and not current_state.vfx_on_damage_frame.is_empty() and unit:
		VFXManager.play_effect(current_state.vfx_on_damage_frame, unit.global_position)

## Called when animation finishes
func _on_animation_finished() -> void:
	if not current_state:
		return

	# Trigger VFX on end
	if not current_state.vfx_on_end.is_empty() and unit:
		VFXManager.play_effect(current_state.vfx_on_end, unit.global_position)

	# Emit signal
	animation_finished.emit(current_state_name)

	# Auto transition
	if not current_state.auto_transition_to.is_empty():
		if current_state.transition_delay > 0.0:
			await get_tree().create_timer(current_state.transition_delay).timeout
		play_state(current_state.auto_transition_to)

## Get current state name
func get_current_state() -> String:
	return current_state_name

## Get current state data
func get_current_state_data() -> AnimationStateData:
	return current_state

## Check if animation is playing
func is_playing(state_name: String) -> bool:
	return current_state_name == state_name

## Get state time
func get_state_time() -> float:
	return state_time

## Set animation config at runtime
func set_animation_config(config: UnitAnimationConfig) -> void:
	animation_config = config
	if animation_config:
		animation_config.build_cache()
		play_state(animation_config.default_state)

## Play sound at unit position
func _play_sound(sound: AudioStream, volume_db: float) -> void:
	if not unit:
		return

	var audio_player = AudioStreamPlayer3D.new()
	audio_player.stream = sound
	audio_player.volume_db = volume_db
	audio_player.global_position = unit.global_position
	audio_player.autoplay = true

	unit.add_child(audio_player)

	# Auto-cleanup when sound finishes
	audio_player.finished.connect(func():
		audio_player.queue_free()
	)
