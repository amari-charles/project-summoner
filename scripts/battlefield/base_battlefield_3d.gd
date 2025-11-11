extends Node3D
class_name BaseBattlefield3D

## Reusable battlefield scene that can be configured for different visual themes and layouts

# Environment configuration
@export_group("Environment")
@export var sky_color: Color = Color(0.53, 0.81, 0.98, 1.0)
@export var ambient_light_color: Color = Color.WHITE
@export var ambient_light_energy: float = 0.5

# Battlefield layout
@export_group("Layout")
## Spawn positions for player and enemy bases/units
##
## The Z offset (-7.5) compensates for the camera's 35° tilt angle.
## Castle sprites are 6 units tall. From the tilted camera view, this height
## appears as "depth" in screen space. The negative Z offset shifts castles
## forward (toward camera) to center them vertically in the viewport.
##
## Formula: Z_offset ≈ -(sprite_height / 2) * (camera_up.z / camera_up.y)
## For 6-unit tall sprite with 35° camera: -7.5 units
##
## TODO: Calculate this dynamically based on sprite height and camera angle
@export var player_spawn_position: Vector3 = Vector3(-40, 0, -7.5)
@export var enemy_spawn_position: Vector3 = Vector3(40, 0, -7.5)

@onready var world_environment: WorldEnvironment = $WorldEnvironment
@onready var camera: Camera3D = $Camera3D
@onready var background: MeshInstance3D = $Background
@onready var player_spawn_marker: Marker3D = $PlayerSpawnMarker
@onready var enemy_spawn_marker: Marker3D = $EnemySpawnMarker
@onready var gameplay_layer: Node3D = $GameplayLayer
@onready var effects_layer: Node3D = $EffectsLayer

func _ready() -> void:
	_apply_configuration()

func _apply_configuration() -> void:
	# Apply environment settings (sky color, ambient lighting)
	if world_environment and world_environment.environment:
		world_environment.environment.background_color = sky_color
		world_environment.environment.ambient_light_color = ambient_light_color
		world_environment.environment.ambient_light_energy = ambient_light_energy

	# Camera position and size are configured directly in the scene file
	# No runtime override needed - the camera controller handles all positioning

	# Apply spawn positions for bases and units
	if player_spawn_marker:
		player_spawn_marker.position = player_spawn_position

	if enemy_spawn_marker:
		enemy_spawn_marker.position = enemy_spawn_position

func _update_ground_position() -> void:
	## Positions the ground plane below the camera's lowest visible Y coordinate
	## This ensures the ground is always below the viewport, preventing blue void
	if not camera or not background:
		return

	# Calculate the lowest Y the camera can see
	var camera_up = camera.transform.basis.y
	var lowest_view_y = camera.position.y - (camera_up.y * camera.size)

	# Position ground below visible area with small margin
	background.position.y = lowest_view_y - 1.0

func get_gameplay_layer() -> Node3D:
	return gameplay_layer

func get_effects_layer() -> Node3D:
	return effects_layer

func get_player_spawn_position() -> Vector3:
	return player_spawn_marker.global_position if player_spawn_marker else player_spawn_position

func get_enemy_spawn_position() -> Vector3:
	return enemy_spawn_marker.global_position if enemy_spawn_marker else enemy_spawn_position
