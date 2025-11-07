extends Node3D
class_name BaseBattlefield3D

## Reusable battlefield scene that can be configured for different visual themes and layouts

# Environment configuration
@export_group("Environment")
@export var sky_color: Color = Color(0.53, 0.81, 0.98, 1.0)
@export var ambient_light_color: Color = Color.WHITE
@export var ambient_light_energy: float = 0.5

# Camera configuration
@export_group("Camera")
@export var camera_position: Vector3 = Vector3(21.18, 14.28, 0)
@export var camera_size: float = 35.62  # Orthographic size

# Battlefield layout
@export_group("Layout")
@export var player_spawn_position: Vector3 = Vector3(0, 2, -29)
@export var enemy_spawn_position: Vector3 = Vector3(0, 2, 29)

@onready var world_environment: WorldEnvironment = $WorldEnvironment
@onready var camera: Camera3D = $Camera3D
@onready var player_spawn_marker: Marker3D = $PlayerSpawnMarker
@onready var enemy_spawn_marker: Marker3D = $EnemySpawnMarker
@onready var gameplay_layer: Node3D = $GameplayLayer
@onready var effects_layer: Node3D = $EffectsLayer

func _ready() -> void:
	_apply_configuration()

func _apply_configuration() -> void:
	# Apply environment settings
	if world_environment and world_environment.environment:
		world_environment.environment.background_color = sky_color
		world_environment.environment.ambient_light_color = ambient_light_color
		world_environment.environment.ambient_light_energy = ambient_light_energy

	# Apply camera settings
	if camera:
		camera.position = camera_position
		camera.size = camera_size

	# Apply spawn positions
	if player_spawn_marker:
		player_spawn_marker.position = player_spawn_position

	if enemy_spawn_marker:
		enemy_spawn_marker.position = enemy_spawn_position

func get_gameplay_layer() -> Node3D:
	return gameplay_layer

func get_effects_layer() -> Node3D:
	return effects_layer

func get_player_spawn_position() -> Vector3:
	return player_spawn_marker.global_position if player_spawn_marker else player_spawn_position

func get_enemy_spawn_position() -> Vector3:
	return enemy_spawn_marker.global_position if enemy_spawn_marker else enemy_spawn_position
