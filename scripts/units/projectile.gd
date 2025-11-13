extends Area2D
class_name Projectile

## Projectile fired by ranged units
## Travels toward target and deals damage on hit

var damage: float = 10.0
var speed: float = 300.0
var team: Unit.Team = Unit.Team.PLAYER
var target: Node2D = null
var shooter: Unit = null  ## Unit that fired this projectile

func _ready() -> void:
	# Connect to area entered signal
	area_entered.connect(_on_area_entered)
	body_entered.connect(_on_body_entered)

func _physics_process(delta: float) -> void:
	if not is_instance_valid(target):
		queue_free()
		return

	# Move toward target
	var direction = (target.global_position - global_position).normalized()
	global_position += direction * speed * delta

	# Rotate to face direction of travel
	rotation = direction.angle()

	# Check if we've reached the target
	var dist = global_position.distance_to(target.global_position)
	if dist < 10.0:
		_hit_target()

func _on_area_entered(area: Area2D) -> void:
	# Hit another projectile or area
	pass

func _on_body_entered(body: Node) -> void:
	# Check if we hit an enemy
	if body is Unit:
		# Type narrow to Unit for safe property access
		var unit: Unit = body as Unit
		if unit.team != team and unit.get("is_alive"):
			_hit_target()
	elif body is Base:
		# Type narrow to Base for safe property access
		var base: Base = body as Base
		if base.team != team and base.get("is_alive"):
			_hit_target()

func _hit_target() -> void:
	# Deal damage
	if is_instance_valid(target) and target.has_method("take_damage"):
		target.take_damage(damage)

	# Visual hit effect (placeholder)
	# TODO: Add particle effect or animation

	# Destroy projectile
	queue_free()

## Setup the projectile
func initialize(start_pos: Vector2, target_node: Node2D, dmg: float, proj_team: Unit.Team, source: Unit) -> void:
	global_position = start_pos
	target = target_node
	damage = dmg
	team = proj_team
	shooter = source
