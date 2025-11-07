extends StaticBody3D
class_name Base3D

## 3D Base structure that units attack
## Each team has one base - destroying it wins the game

enum Team { PLAYER, ENEMY }

@export var max_hp: float = 300.0
@export var team: Team = Team.PLAYER

var current_hp: float
var is_alive: bool = true

## Signals
signal base_destroyed(base: Base3D)
signal base_damaged(base: Base3D, damage: float)

func _ready() -> void:
	current_hp = max_hp

	# Add to groups
	add_to_group("bases")
	if team == Team.PLAYER:
		add_to_group("player_base")
	else:
		add_to_group("enemy_base")

	print("Base3D ready: Team %d, HP %d" % [team, max_hp])

## Take damage from units
func take_damage(damage: float) -> void:
	if not is_alive:
		return

	current_hp -= damage
	base_damaged.emit(self, damage)

	print("Base3D damaged: %d/%d HP" % [current_hp, max_hp])

	if current_hp <= 0:
		current_hp = 0
		_destroy()

## Destroy the base
func _destroy() -> void:
	is_alive = false
	base_destroyed.emit(self)
	print("Base3D destroyed! Team: ", team)
