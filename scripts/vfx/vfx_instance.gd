extends Node3D
class_name VFXInstance

## Base class for all visual effect instances
## All effect scenes should use this script or inherit from it
## Handles lifetime, pooling, and automatic cleanup
##
## IMPORTANT - Resource Isolation for Pooled VFX:
## When VFX instances are pooled, resources (meshes, materials) from the scene
## file are SHARED between all instances. Modifying a shared resource affects
## ALL instances using it, causing bugs when effects are reused.
##
## Safe patterns for pooled VFX:
## 1. Use node transforms (scale, modulate) instead of resource properties
## 2. Call isolate_mesh_resources() in _ready() for MeshInstance3D nodes you'll modify
## 3. Create resources dynamically in code (they're unique per-instance)
##
## GPUParticles3D are safe - Godot handles particle resources per-instance.

@export var lifetime: float = 1.0  ## How long effect lives (0 = infinite)
@export var auto_free: bool = true  ## Auto-free when lifetime expires?

var time_alive: float = 0.0
var is_pooled: bool = false
var is_playing: bool = false

signal effect_finished()

func _ready() -> void:
	# Don't auto-play for pooled instances - VFXManager will call play() after setup
	# Pooled instances need data (position, custom params) set before playing
	if not is_pooled:
		play()

## Start/restart the effect
func play() -> void:
	is_playing = true
	time_alive = 0.0
	_restart_particles()
	_on_play()

## Restart all GPUParticles3D children (fixes one_shot particles not restarting)
func _restart_particles() -> void:
	for child_node: Node in get_children():
		if child_node is GPUParticles3D:
			var particles: GPUParticles3D = child_node
			# For one_shot particles, we need to restart them
			particles.restart()

## Override in subclasses for custom play logic
func _on_play() -> void:
	# Start particles, animations, etc.
	pass

## Called by VFXManager to pass custom runtime data to VFX
## Override in subclasses to receive and validate custom parameters
## Called AFTER instantiation, BEFORE adding to scene tree
## Safe to modify properties that will be used in _on_play() or later
func receive_data(_data: Dictionary) -> void:
	# Default: do nothing
	# Subclasses override to extract and validate data
	pass

## Reset state for pooling reuse
func reset() -> void:
	time_alive = 0.0
	is_playing = false
	# Stop all particles
	for child_node: Node in get_children():
		if child_node is GPUParticles3D:
			var particles: GPUParticles3D = child_node
			particles.emitting = false
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


# =============================================================================
# RESOURCE ISOLATION HELPERS
# Use these in _ready() to make shared resources unique per-instance
# =============================================================================

## Make a MeshInstance3D's resources unique for safe modification
## Call this in _ready() for any MeshInstance3D whose mesh or material you'll modify
## Parameters:
##   mesh_instance: The MeshInstance3D to isolate
##   isolate_mesh: Duplicate the mesh resource (for modifying mesh properties like QuadMesh.size)
##   isolate_materials: Duplicate all materials (for color/shader changes)
func isolate_mesh_resources(mesh_instance: MeshInstance3D, isolate_mesh: bool = false, isolate_materials: bool = true) -> void:
	if not mesh_instance:
		return

	# Duplicate mesh if requested (needed for modifying mesh properties)
	if isolate_mesh and mesh_instance.mesh:
		mesh_instance.mesh = mesh_instance.mesh.duplicate()

	# Duplicate material_override if set
	if isolate_materials and mesh_instance.material_override:
		mesh_instance.material_override = mesh_instance.material_override.duplicate()

	# Duplicate surface override materials (set via set_surface_override_material)
	if isolate_materials and mesh_instance.mesh:
		for surface_idx: int in range(mesh_instance.get_surface_override_material_count()):
			var material: Material = mesh_instance.get_surface_override_material(surface_idx)
			if material:
				mesh_instance.set_surface_override_material(surface_idx, material.duplicate())

	# Duplicate materials embedded directly in the mesh resource
	# These are set on the mesh itself, not as overrides on the MeshInstance3D
	if isolate_materials and mesh_instance.mesh:
		# Check if mesh has any embedded materials before duplicating
		var has_embedded_materials := false
		for surface_idx: int in range(mesh_instance.mesh.get_surface_count()):
			if mesh_instance.mesh.surface_get_material(surface_idx):
				has_embedded_materials = true
				break

		if has_embedded_materials:
			# Must duplicate mesh first to avoid affecting other instances
			if not isolate_mesh:
				mesh_instance.mesh = mesh_instance.mesh.duplicate()
			for surface_idx: int in range(mesh_instance.mesh.get_surface_count()):
				var surface_mat: Material = mesh_instance.mesh.surface_get_material(surface_idx)
				if surface_mat:
					mesh_instance.mesh.surface_set_material(surface_idx, surface_mat.duplicate())


## Convenience: Isolate all MeshInstance3D descendants (recursive)
## Useful when you have multiple meshes that all need isolation
func isolate_all_mesh_resources(isolate_mesh: bool = false, isolate_materials: bool = true) -> void:
	for child: Node in find_children("*", "MeshInstance3D"):
		isolate_mesh_resources(child as MeshInstance3D, isolate_mesh, isolate_materials)
