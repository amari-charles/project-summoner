extends Node

## Centralized VFX spawning and pooling system
## Usage: VFXManager.play_effect("fireball_explosion", position)
## Autoload as: /root/VFXManager

var effect_library: Dictionary = {}  ## effect_id -> VFXDefinition
var effect_pools: Dictionary = {}  ## effect_id -> Array[VFXInstance]
var active_effects: Dictionary = {}  ## effect_id -> Array[VFXInstance]

var effects_container: Node3D = null  ## Parent for all effects

func _ready() -> void:
	print("VFXManager: Initializing...")

	# Create container for effects
	effects_container = Node3D.new()
	effects_container.name = "VFXContainer"
	add_child(effects_container)

	_load_effect_library()
	_init_pools()

	print("VFXManager: Loaded %d effects" % effect_library.size())

## Load all VFXDefinition resources from res://resources/vfx/
func _load_effect_library() -> void:
	var vfx_dir: String = "res://resources/vfx/"
	var dir: DirAccess = DirAccess.open(vfx_dir)

	if not dir:
		push_warning("VFXManager: VFX directory not found: " + vfx_dir)
		return

	dir.list_dir_begin()
	var file_name: String = dir.get_next()

	while file_name != "":
		if file_name.ends_with(".tres") or file_name.ends_with(".res"):
			var file_path: String = vfx_dir + file_name
			var resource: Resource = load(file_path)
			if resource is VFXDefinition:
				# Type narrow to VFXDefinition for safe property access
				var vfx_def: VFXDefinition = resource
				if not vfx_def.effect_id.is_empty():
					effect_library[vfx_def.effect_id] = vfx_def
				else:
					push_warning("VFXManager: VFXDefinition has no effect_id: " + file_path)
		file_name = dir.get_next()

	dir.list_dir_end()

## Pre-instantiate pooled effects
func _init_pools() -> void:
	var effect_keys: Array = effect_library.keys()
	for effect_id: Variant in effect_keys:
		var vfx_def_variant: Variant = effect_library[effect_id]
		if not vfx_def_variant is VFXDefinition:
			continue

		# Type narrow to VFXDefinition for safe property access
		var vfx_def: VFXDefinition = vfx_def_variant

		if vfx_def.pooled and vfx_def.effect_scene:
			effect_pools[effect_id] = []
			active_effects[effect_id] = []

			# Pre-instantiate pool
			for i: int in range(vfx_def.pool_size):
				var instance_node: Node = vfx_def.effect_scene.instantiate()
				if not instance_node is VFXInstance:
					continue

				var instance: VFXInstance = instance_node
				if instance:
					instance.is_pooled = true
					instance.reset()
					var pool: Array = effect_pools[effect_id]
					pool.append(instance)

			print("VFXManager: Pre-instantiated pool of %d for '%s'" % [
				vfx_def.pool_size,
				effect_id
			])

## Play an effect at a 3D position
func play_effect(effect_id: String, position: Vector3, data: Dictionary = {}) -> VFXInstance:
	if not effect_library.has(effect_id):
		push_error("VFXManager: Effect '%s' not found in library" % effect_id)
		return null

	var vfx_def_variant: Variant = effect_library[effect_id]
	if not vfx_def_variant is VFXDefinition:
		push_error("VFXManager: Effect '%s' is not a VFXDefinition" % effect_id)
		return null

	# Type narrow to VFXDefinition for safe property access
	var vfx_def: VFXDefinition = vfx_def_variant
	var instance: VFXInstance = null

	# Get from pool or instantiate
	if vfx_def.pooled:
		instance = _get_from_pool(effect_id)
	else:
		if vfx_def.effect_scene:
			var instance_node: Node = vfx_def.effect_scene.instantiate()
			if instance_node is VFXInstance:
				instance = instance_node

	if not instance:
		push_error("VFXManager: Failed to create instance of '%s'" % effect_id)
		return null

	# Configure instance
	instance.global_position = position
	if vfx_def.duration > 0:
		instance.lifetime = vfx_def.duration

	# Apply custom data
	if data.has("scale") and data.scale is float:
		instance.scale = Vector3.ONE * data.scale
	if data.has("rotation") and data.rotation is Vector3:
		instance.rotation = data.rotation

	# Add to scene
	effects_container.add_child(instance)

	# Track active effect
	if vfx_def.pooled:
		if not active_effects.has(effect_id):
			active_effects[effect_id] = []
		var active: Array = active_effects[effect_id]
		active.append(instance)

		# Connect to finished signal for pool return
		if not instance.effect_finished.is_connected(_on_effect_finished):
			instance.effect_finished.connect(_on_effect_finished.bind(effect_id, instance))

	# Play effect
	instance.play()

	# Play sound
	if vfx_def.play_sound:
		_play_sound(vfx_def.play_sound, position, vfx_def.sound_volume)

	# Camera shake
	if vfx_def.camera_shake > 0.0:
		_apply_camera_shake(vfx_def.camera_shake, vfx_def.camera_shake_duration)

	return instance

## Play effect in 2D screen space (for UI effects, future)
func play_effect_2d(effect_id: String, position: Vector2, data: Dictionary = {}) -> VFXInstance:
	# Convert 2D position to 3D
	var pos_3d: Vector3 = Vector3(position.x, position.y, 0)
	return play_effect(effect_id, pos_3d, data)

## Get effect from pool or create new
func _get_from_pool(effect_id: String) -> VFXInstance:
	if not effect_pools.has(effect_id):
		return null

	var pool: Array = effect_pools[effect_id]
	if pool.size() > 0:
		var instance_variant: Variant = pool.pop_back()
		if instance_variant is VFXInstance:
			var instance: VFXInstance = instance_variant
			instance.reset()
			return instance

	# Pool exhausted, instantiate new
	var vfx_def_variant: Variant = effect_library[effect_id]
	if not vfx_def_variant is VFXDefinition:
		return null

	# Type narrow to VFXDefinition for safe property access
	var vfx_def: VFXDefinition = vfx_def_variant
	if vfx_def.effect_scene:
		var instance_node: Node = vfx_def.effect_scene.instantiate()
		if instance_node is VFXInstance:
			var instance: VFXInstance = instance_node
			instance.is_pooled = true
			return instance

	return null

## Return effect to pool
func _on_effect_finished(effect_id: String, instance: VFXInstance) -> void:
	# Remove from active
	if active_effects.has(effect_id):
		var active: Array = active_effects[effect_id]
		active.erase(instance)

	# Remove from scene
	if instance.get_parent():
		instance.get_parent().remove_child(instance)

	# Return to pool
	if effect_pools.has(effect_id):
		var pool: Array = effect_pools[effect_id]
		pool.append(instance)

## Play sound at position
func _play_sound(sound: AudioStream, position: Vector3, volume_db: float) -> void:
	var audio_player: AudioStreamPlayer3D = AudioStreamPlayer3D.new()
	audio_player.stream = sound
	audio_player.volume_db = volume_db
	audio_player.global_position = position
	audio_player.autoplay = true

	effects_container.add_child(audio_player)

	# Auto-cleanup when sound finishes
	var cleanup_player: AudioStreamPlayer3D = audio_player
	audio_player.finished.connect(func() -> void:
		if is_instance_valid(cleanup_player):
			cleanup_player.queue_free()
	)

## Apply camera shake (stub for now, will implement with camera system)
func _apply_camera_shake(_intensity: float, _duration: float) -> void:
	# TODO: Implement camera shake
	# Will need reference to main camera
	pass

## Check if effect exists in library
func has_effect(effect_id: String) -> bool:
	return effect_library.has(effect_id)

## Get effect definition
func get_effect_definition(effect_id: String) -> VFXDefinition:
	return effect_library.get(effect_id)

## Debug: Print pool statistics
func print_pool_stats() -> void:
	print("=== VFXManager Pool Statistics ===")
	var pool_keys: Array = effect_pools.keys()
	for effect_id: Variant in pool_keys:
		var pool: Array = effect_pools[effect_id]
		var pool_size: int = pool.size()
		var active_size: int = 0
		if active_effects.has(effect_id):
			var active: Array = active_effects[effect_id]
			active_size = active.size()
		print("  %s: %d in pool, %d active" % [effect_id, pool_size, active_size])
