extends Node3D

@export var pulse_speed: float = 9.0
@export var body_orbit_speed: float = 13.0
@export var ring_a_speed: float = 11.0
@export var ring_b_speed: float = -14.0
@export var core_pulse_strength: float = 0.18
@export var shell_pulse_strength: float = 0.24
@export var needle_pulse_strength: float = 0.12
@export var wake_pulse_strength: float = 0.2
@export var core_emission_min: float = 11.0
@export var core_emission_max: float = 18.0
@export var shell_emission_min: float = 3.5
@export var shell_emission_max: float = 8.0
@export var ring_emission_min: float = 4.0
@export var ring_emission_max: float = 9.5
@export var wake_emission_min: float = 3.0
@export var wake_emission_max: float = 7.0
@export var depth_scale_enabled: bool = true
@export var depth_scale_strength: float = 0.012
@export var depth_scale_min: float = 0.78
@export var depth_scale_max: float = 1.28
@export var depth_scale_smoothing: float = 12.0

@onready var body_pivot: Node3D = $BodyPivot
@onready var core_mesh: MeshInstance3D = $BodyPivot/Core
@onready var shell_mesh: MeshInstance3D = $BodyPivot/OuterShell
@onready var needle_mesh: MeshInstance3D = $BodyPivot/Needle
@onready var wake_mesh: MeshInstance3D = $BodyPivot/Wake
@onready var ring_a_pivot: Node3D = $RingAPivot
@onready var ring_b_pivot: Node3D = $RingBPivot
@onready var ring_a_mesh: MeshInstance3D = $RingAPivot/RingA
@onready var ring_b_mesh: MeshInstance3D = $RingBPivot/RingB
@onready var trail_particles: GPUParticles3D = $TrailParticles
@onready var spark_particles: GPUParticles3D = $SparkParticles

var _time: float = 0.0
var _base_root_scale: Vector3 = Vector3.ONE
var _base_body_position: Vector3 = Vector3.ZERO
var _base_core_scale: Vector3 = Vector3.ONE
var _base_shell_scale: Vector3 = Vector3.ONE
var _base_needle_scale: Vector3 = Vector3.ONE
var _base_wake_scale: Vector3 = Vector3.ONE
var _core_material: StandardMaterial3D
var _shell_material: StandardMaterial3D
var _ring_a_material: StandardMaterial3D
var _ring_b_material: StandardMaterial3D
var _needle_material: StandardMaterial3D
var _wake_material: StandardMaterial3D
var _trail_material: ParticleProcessMaterial
var _spark_material: ParticleProcessMaterial
var _active_camera: Camera3D
var _baseline_camera_distance: float = -1.0
var _depth_scale_factor: float = 1.0


func _ready() -> void:
	_base_root_scale = scale
	_base_body_position = body_pivot.position
	_base_core_scale = core_mesh.scale
	_base_shell_scale = shell_mesh.scale
	_base_needle_scale = needle_mesh.scale
	_base_wake_scale = wake_mesh.scale
	_core_material = _duplicate_surface_material(core_mesh)
	_shell_material = _duplicate_surface_material(shell_mesh)
	_needle_material = _duplicate_surface_material(needle_mesh)
	_wake_material = _duplicate_surface_material(wake_mesh)
	_ring_a_material = _duplicate_surface_material(ring_a_mesh)
	_ring_b_material = _duplicate_surface_material(ring_b_mesh)

	if trail_particles.process_material is ParticleProcessMaterial:
		_trail_material = (trail_particles.process_material as ParticleProcessMaterial).duplicate(true)
		trail_particles.process_material = _trail_material
	if spark_particles.process_material is ParticleProcessMaterial:
		_spark_material = (spark_particles.process_material as ParticleProcessMaterial).duplicate(true)
		spark_particles.process_material = _spark_material

	trail_particles.emitting = true
	spark_particles.emitting = true

	_active_camera = get_viewport().get_camera_3d()
	if _active_camera != null:
		_baseline_camera_distance = _active_camera.global_position.distance_to(global_position)


func _process(delta: float) -> void:
	_time += delta
	_update_depth_scale(delta)

	ring_a_pivot.rotate_y(ring_a_speed * delta)
	ring_b_pivot.rotate_x(ring_b_speed * delta)
	ring_b_pivot.rotate_z((ring_b_speed * -0.45) * delta)

	var pulse: float = 0.5 + (0.5 * sin(_time * pulse_speed))
	var inverse_pulse: float = 1.0 - pulse
	var core_scale: float = 1.0 + ((pulse - 0.5) * 2.0 * core_pulse_strength)
	var shell_scale: float = 1.0 + ((inverse_pulse - 0.5) * 2.0 * shell_pulse_strength)
	var needle_scale: float = 1.0 + ((pulse - 0.5) * 2.0 * needle_pulse_strength)
	var wake_scale: float = 1.0 + ((inverse_pulse - 0.5) * 2.0 * wake_pulse_strength)

	var orbit_x: float = sin(_time * body_orbit_speed) * 0.03
	var orbit_y: float = cos(_time * body_orbit_speed * 0.77) * 0.018
	body_pivot.position = _base_body_position + Vector3(orbit_x, orbit_y, 0.0)
	core_mesh.scale = _base_core_scale * core_scale
	shell_mesh.scale = _base_shell_scale * shell_scale
	needle_mesh.scale = _base_needle_scale * needle_scale
	wake_mesh.scale = _base_wake_scale * wake_scale

	if _core_material != null:
		_core_material.emission_energy_multiplier = lerp(core_emission_min, core_emission_max, pulse)
	if _shell_material != null:
		_shell_material.emission_energy_multiplier = lerp(shell_emission_min, shell_emission_max, inverse_pulse)
	if _ring_a_material != null:
		_ring_a_material.emission_energy_multiplier = lerp(ring_emission_min, ring_emission_max, pulse)
	if _ring_b_material != null:
		_ring_b_material.emission_energy_multiplier = lerp(ring_emission_min, ring_emission_max, inverse_pulse)
	if _needle_material != null:
		_needle_material.emission_energy_multiplier = lerp(core_emission_min * 0.75, core_emission_max * 0.9, pulse)
	if _wake_material != null:
		_wake_material.emission_energy_multiplier = lerp(wake_emission_min, wake_emission_max, inverse_pulse)

	if _trail_material != null:
		_trail_material.initial_velocity_min = lerp(2.8, 6.5, pulse)
		_trail_material.initial_velocity_max = lerp(5.2, 11.0, pulse)
		_trail_material.scale_min = lerp(0.1, 0.24, pulse)
		_trail_material.scale_max = lerp(0.22, 0.46, pulse)

	if _spark_material != null:
		_spark_material.initial_velocity_min = lerp(0.8, 2.1, inverse_pulse)
		_spark_material.initial_velocity_max = lerp(1.8, 4.2, inverse_pulse)
		_spark_material.scale_min = lerp(0.05, 0.14, pulse)
		_spark_material.scale_max = lerp(0.08, 0.22, pulse)


func _update_depth_scale(delta: float) -> void:
	if not depth_scale_enabled:
		scale = _base_root_scale
		return

	if _active_camera == null or not is_instance_valid(_active_camera):
		_active_camera = get_viewport().get_camera_3d()
		if _active_camera != null and _baseline_camera_distance <= 0.0:
			_baseline_camera_distance = _active_camera.global_position.distance_to(global_position)

	if _active_camera == null:
		scale = _base_root_scale
		return

	var current_distance: float = _active_camera.global_position.distance_to(global_position)
	if _baseline_camera_distance <= 0.0:
		_baseline_camera_distance = current_distance

	var distance_delta: float = _baseline_camera_distance - current_distance
	var target_scale: float = clamp(1.0 + (distance_delta * depth_scale_strength), depth_scale_min, depth_scale_max)
	var blend: float = clamp(delta * depth_scale_smoothing, 0.0, 1.0)
	_depth_scale_factor = lerp(_depth_scale_factor, target_scale, blend)
	scale = _base_root_scale * _depth_scale_factor


func _duplicate_surface_material(mesh: MeshInstance3D) -> StandardMaterial3D:
	var source_material: StandardMaterial3D = mesh.get_surface_override_material(0) as StandardMaterial3D
	if source_material == null:
		source_material = mesh.material_override as StandardMaterial3D
	if source_material == null:
		return null

	var duplicate_material: StandardMaterial3D = source_material.duplicate(true) as StandardMaterial3D
	mesh.set_surface_override_material(0, duplicate_material)
	mesh.material_override = duplicate_material
	return duplicate_material
