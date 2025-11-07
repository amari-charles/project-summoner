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
	_state_lookup.clear()
	for state in states:
		if state and not state.state_name.is_empty():
			_state_lookup[state.state_name] = state

## Get state by name
func get_state(state_name: String) -> AnimationStateData:
	if _state_lookup.is_empty():
		build_cache()
	return _state_lookup.get(state_name)

## Check if state exists
func has_state(state_name: String) -> bool:
	if _state_lookup.is_empty():
		build_cache()
	return _state_lookup.has(state_name)

## Add a new state
func add_state(state: AnimationStateData) -> void:
	if state and not state.state_name.is_empty():
		states.append(state)
		_state_lookup[state.state_name] = state

## Load from dictionary (for JSON loading support)
static func from_dict(data: Dictionary) -> UnitAnimationConfig:
	var config = UnitAnimationConfig.new()

	config.config_id = data.get("config_id", "")
	config.default_state = data.get("default_state", "idle")

	# Load sprite_frames path
	if data.has("sprite_frames_path"):
		var sprite_path = data["sprite_frames_path"]
		config.sprite_frames = load(sprite_path)

	# Load states
	if data.has("states"):
		for state_dict in data["states"]:
			var state = AnimationStateData.new()
			state.state_name = state_dict.get("state_name", "")
			state.animation_name = state_dict.get("animation_name", state.state_name)
			state.loop = state_dict.get("loop", true)
			state.priority = state_dict.get("priority", 0)
			state.speed_scale = state_dict.get("speed_scale", 1.0)
			state.can_be_interrupted = state_dict.get("can_be_interrupted", true)
			state.auto_transition_to = state_dict.get("auto_transition_to", "")
			state.transition_delay = state_dict.get("transition_delay", 0.0)
			state.damage_frame = state_dict.get("damage_frame", -1)
			state.projectile_spawn_frame = state_dict.get("projectile_spawn_frame", -1)
			state.vfx_on_start = state_dict.get("vfx_on_start", "")
			state.vfx_on_end = state_dict.get("vfx_on_end", "")
			state.vfx_on_damage_frame = state_dict.get("vfx_on_damage_frame", "")
			state.metadata = state_dict.get("metadata", {})

			# Load footstep frames
			if state_dict.has("footstep_frames"):
				for frame in state_dict["footstep_frames"]:
					state.footstep_frames.append(frame)

			# Load sound
			if state_dict.has("sound_on_start_path"):
				state.sound_on_start = load(state_dict["sound_on_start_path"])
			state.sound_volume = state_dict.get("sound_volume", 0.0)

			config.add_state(state)

	config.build_cache()
	return config
