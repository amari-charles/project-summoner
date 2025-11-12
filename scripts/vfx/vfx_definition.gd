extends Resource
class_name VFXDefinition

## Defines a reusable visual effect
## Used by VFXManager to spawn and pool effects

@export var effect_id: String = ""
@export var effect_name: String = ""

## Scene
@export_group("Scene")
@export var effect_scene: PackedScene = null  ## The actual effect scene (must have VFXInstance script)

## Behavior
@export_group("Behavior")
@export var duration: float = 1.0  ## Auto-cleanup after this time (0 = manual control)
@export var pooled: bool = true  ## Enable object pooling?
@export var pool_size: int = 10  ## Pre-instantiate this many for pooling

## Audio
@export_group("Audio")
@export var play_sound: AudioStream = null
@export var sound_volume: float = 0.0  ## dB

## Camera Effects
@export_group("Camera Effects")
@export var camera_shake: float = 0.0  ## Screen shake intensity (0-1)
@export var camera_shake_duration: float = 0.2

## Create from dictionary (for JSON loading, future feature)
static func from_dict(data: Dictionary) -> VFXDefinition:
	var vfx: VFXDefinition = VFXDefinition.new()

	vfx.effect_id = data.get("effect_id", "")
	vfx.effect_name = data.get("effect_name", "")

	# Scene path needs to be loaded
	var scene_path_variant: Variant = data.get("effect_scene_path", "")
	if scene_path_variant is String:
		var scene_path: String = scene_path_variant
		if not scene_path.is_empty():
			var loaded_scene: Resource = load(scene_path)
			if loaded_scene is PackedScene:
				vfx.effect_scene = loaded_scene

	vfx.duration = data.get("duration", 1.0)
	vfx.pooled = data.get("pooled", true)
	vfx.pool_size = data.get("pool_size", 10)

	# Audio
	var sound_path_variant: Variant = data.get("sound_path", "")
	if sound_path_variant is String:
		var sound_path: String = sound_path_variant
		if not sound_path.is_empty():
			var loaded_sound: Resource = load(sound_path)
			if loaded_sound is AudioStream:
				vfx.play_sound = loaded_sound
	vfx.sound_volume = data.get("sound_volume", 0.0)

	vfx.camera_shake = data.get("camera_shake", 0.0)
	vfx.camera_shake_duration = data.get("camera_shake_duration", 0.2)

	return vfx
