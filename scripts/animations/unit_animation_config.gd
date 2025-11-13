extends Resource
class_name UnitAnimationConfig

## Complete animation configuration for a unit
## References all AnimationStateData and defines state transitions

@export var config_id: String = ""  ## "warrior_anims", "archer_anims", etc.
@export var sprite_frames: SpriteFrames = null  ## The SpriteFrames resource
@export var default_state: String = "idle"  ## Starting state

## All animation states
@export var states: Array[AnimationStateData] = []

## Quick lookup cache (built at runtime)
var _state_lookup: Dictionary = {}

func _init(p_config_id: String = "") -> void:
	config_id = p_config_id

## Build lookup cache for fast state access
func build_cache() -> void:
	(_state_lookup as Dictionary).clear()
	for state: AnimationStateData in states:
		if state and not (state.state_name as String).is_empty():
			_state_lookup[state.state_name] = state

## Get state by name
func get_state(state_name: String) -> AnimationStateData:
	if (_state_lookup as Dictionary).is_empty():
		build_cache()
	return (_state_lookup as Dictionary).get(state_name)

## Check if state exists
func has_state(state_name: String) -> bool:
	if (_state_lookup as Dictionary).is_empty():
		build_cache()
	return (_state_lookup as Dictionary).has(state_name)

## Add a new state
func add_state(state: AnimationStateData) -> void:
	if state and not (state.state_name as String).is_empty():
		(states as Array[AnimationStateData]).append(state)
		_state_lookup[state.state_name] = state

## Load from dictionary (for JSON loading support)
static func from_dict(data: Dictionary) -> UnitAnimationConfig:
	var config: UnitAnimationConfig = UnitAnimationConfig.new()

	config.config_id = (data as Dictionary).get("config_id", "")
	config.default_state = (data as Dictionary).get("default_state", "idle")

	# Load sprite_frames path
	if (data as Dictionary).has("sprite_frames_path"):
		var sprite_path: String = data["sprite_frames_path"]
		config.sprite_frames = load(sprite_path) as SpriteFrames

	# Load states
	if (data as Dictionary).has("states"):
		for state_dict: Variant in data["states"]:
			var state: AnimationStateData = AnimationStateData.new()
			var default_empty_string: String = ""
			state.state_name = (state_dict as Dictionary).get("state_name", default_empty_string)
			state.animation_name = (state_dict as Dictionary).get("animation_name", state.state_name)
			var default_loop: bool = true
			state.loop = (state_dict as Dictionary).get("loop", default_loop)
			var default_priority: int = 0
			state.priority = (state_dict as Dictionary).get("priority", default_priority)
			var default_speed_scale: float = 1.0
			state.speed_scale = (state_dict as Dictionary).get("speed_scale", default_speed_scale)
			var default_can_be_interrupted: bool = true
			state.can_be_interrupted = (state_dict as Dictionary).get("can_be_interrupted", default_can_be_interrupted)
			state.auto_transition_to = (state_dict as Dictionary).get("auto_transition_to", default_empty_string)
			var default_transition_delay: float = 0.0
			state.transition_delay = (state_dict as Dictionary).get("transition_delay", default_transition_delay)
			var default_frame_index: int = -1
			state.damage_frame = (state_dict as Dictionary).get("damage_frame", default_frame_index)
			state.projectile_spawn_frame = (state_dict as Dictionary).get("projectile_spawn_frame", default_frame_index)
			state.vfx_on_start = (state_dict as Dictionary).get("vfx_on_start", default_empty_string)
			state.vfx_on_end = (state_dict as Dictionary).get("vfx_on_end", default_empty_string)
			state.vfx_on_damage_frame = (state_dict as Dictionary).get("vfx_on_damage_frame", default_empty_string)
			var empty_metadata: Dictionary = {}
			state.metadata = (state_dict as Dictionary).get("metadata", empty_metadata)

			# Load footstep frames
			if (state_dict as Dictionary).has("footstep_frames"):
				(state.footstep_frames as Array[int]).clear()
				for frame: Variant in state_dict["footstep_frames"]:
					if frame is int or frame is float:
						(state.footstep_frames as Array[int]).append(int(frame as float))

			# Load sound
			if (state_dict as Dictionary).has("sound_on_start_path"):
				state.sound_on_start = load(state_dict["sound_on_start_path"]) as AudioStream
			var default_sound_volume: float = 0.0
			state.sound_volume = (state_dict as Dictionary).get("sound_volume", default_sound_volume)

			config.add_state(state)

	config.build_cache()
	return config
