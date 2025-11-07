extends Node3D
class_name VFXInstance

## Base class for all visual effect instances
## All effect scenes should use this script or inherit from it
## Handles lifetime, pooling, and automatic cleanup

@export var lifetime: float = 1.0  ## How long effect lives (0 = infinite)
@export var auto_free: bool = true  ## Auto-free when lifetime expires?

var time_alive: float = 0.0
var is_pooled: bool = false
var is_playing: bool = false

signal effect_finished()

func _ready() -> void:
	play()

## Start/restart the effect
func play() -> void:
	is_playing = true
	time_alive = 0.0
	_restart_particles()
	_on_play()

## Restart all GPUParticles3D children (fixes one_shot particles not restarting)
func _restart_particles() -> void:
	for child in get_children():
		if child is GPUParticles3D:
			# For one_shot particles, we need to restart them
			child.restart()

## Override in subclasses for custom play logic
func _on_play() -> void:
	# Start particles, animations, etc.
	pass

## Reset state for pooling reuse
func reset() -> void:
	time_alive = 0.0
	is_playing = false
	# Stop all particles
	for child in get_children():
		if child is GPUParticles3D:
			child.emitting = false
	_on_reset()

## Override in subclasses for custom reset logic
func _on_reset() -> void:
	# Reset particles, animations, etc.
	pass

func _physics_process(delta: float) -> void:
	if not is_playing:
		return

	if lifetime > 0.0:
		time_alive += delta

		if time_alive >= lifetime:
			_finish()

## Effect has finished playing
func _finish() -> void:
	is_playing = false
	effect_finished.emit()

	if is_pooled:
		# Return to pool via VFXManager
		if get_parent():
			get_parent().remove_child(self)
		# VFXManager will handle pool return
	elif auto_free:
		queue_free()

## Stop the effect early
func stop() -> void:
	if is_playing:
		_finish()
