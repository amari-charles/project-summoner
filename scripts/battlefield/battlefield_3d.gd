extends Node3D
class_name Battlefield3D

## 3D Battlefield for 2.5D units
## Manages battlefield environment and unit spawning in 3D space

## References to layers
@onready var ground_layer: Node3D = $GroundLayer
@onready var gameplay_layer: Node3D = $GameplayLayer
@onready var effects_layer: Node3D = $EffectsLayer
@onready var player_base: Node3D = $PlayerBase
@onready var enemy_base: Node3D = $EnemyBase

func _ready() -> void:
	print("Battlefield3D: Initializing 2.5D battlefield")

## Get the gameplay layer where units should be spawned
func get_gameplay_layer() -> Node3D:
	return gameplay_layer

## Get the effects layer for particles and visual effects
func get_effects_layer() -> Node3D:
	return effects_layer

## Get player base position for spawning
func get_player_spawn_position() -> Vector3:
	return player_base.global_position

## Get enemy base position for spawning
func get_enemy_spawn_position() -> Vector3:
	return enemy_base.global_position
