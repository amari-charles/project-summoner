extends CharacterBody2D
class_name Unit

## Base class for all units in Project Summoner
## Units have HP, attack damage, range, and team affiliation

enum Team { PLAYER, ENEMY }

## Core stats
@export var max_hp: float = 100.0
@export var attack_damage: float = 10.0
@export var attack_range: float = 80.0
@export var attack_speed: float = 1.0  # Attacks per second
@export var move_speed: float = 60.0
@export var team: Team = Team.PLAYER
@export var aggro_radius: float = 180.0  # Range to detect enemies
@export var is_ranged: bool = false  # Ranged vs melee (for future)

## Current state
var current_hp: float
var is_alive: bool = true
var current_target: Node2D = null  # Can be Unit or Summoner
var attack_cooldown: float = 0.0

## Signals
signal unit_died(unit: Unit)
signal unit_attacked(target: Unit)

func _ready() -> void:
	current_hp = max_hp
	add_to_group("units")

	# Add team-specific group membership
	if team == Team.PLAYER:
		add_to_group("player_units")
	else:
		add_to_group("enemy_units")

	_setup_visuals()

func _physics_process(delta: float) -> void:
	if not is_alive:
		return

	# Update attack cooldown
	attack_cooldown = max(attack_cooldown - delta, 0.0)

	# Always try to find target (enemies or base)
	current_target = _acquire_target()

	if current_target != null:
		var dist = global_position.distance_to(current_target.global_position)

		# Attack if in range and cooldown ready
		if dist <= attack_range:
			if attack_cooldown <= 0.0:
				_attack(current_target)
				attack_cooldown = 1.0 / attack_speed
			velocity = Vector2.ZERO
		else:
			# Move toward target
			var direction = (current_target.global_position - global_position).normalized()
			velocity = direction * move_speed
	else:
		# No target - advance toward enemy base
		var base = _get_enemy_base()
		if base:
			var direction = (base.global_position - global_position).normalized()
			velocity = direction * move_speed
		else:
			velocity = Vector2.ZERO

	move_and_slide()

## Find the nearest enemy unit within aggro range
func _acquire_target() -> Node2D:
	var target_group = "enemy_units" if team == Team.PLAYER else "player_units"
	var enemies = get_tree().get_nodes_in_group(target_group)

	var best: Node2D = null
	var best_dist := INF

	for u in enemies:
		if u is Unit and u.is_alive:
			var dist = global_position.distance_to(u.global_position)
			if dist < best_dist and dist <= aggro_radius:
				best = u
				best_dist = dist

	# If no units in range, target the enemy base
	if best == null:
		best = _get_enemy_base()

	return best

## Get the enemy base/summoner
func _get_enemy_base() -> Node2D:
	var base_group = "enemy_summoners" if team == Team.PLAYER else "player_summoners"
	var bases = get_tree().get_nodes_in_group(base_group)

	if bases.size() > 0:
		return bases[0]
	return null

## Execute an attack on the target
func _attack(target: Node2D) -> void:
	if target == null:
		return

	# Target can be Unit or Summoner - both have take_damage
	if target.has_method("take_damage"):
		target.take_damage(attack_damage)
		if target is Unit:
			unit_attacked.emit(target)

## Take damage from an attack
func take_damage(damage: float) -> void:
	if not is_alive:
		return

	current_hp -= damage

	if current_hp <= 0:
		current_hp = 0
		_die()

## Handle unit death
func _die() -> void:
	is_alive = false
	unit_died.emit(self)

	# Play death animation/effect here
	await get_tree().create_timer(0.5).timeout
	queue_free()

## Setup visual representation (override in derived classes)
func _setup_visuals() -> void:
	# Default: create a simple colored rectangle
	var sprite = ColorRect.new()
	sprite.size = Vector2(32, 32)
	sprite.position = Vector2(-16, -16)
	sprite.color = Color.BLUE if team == Team.PLAYER else Color.RED
	add_child(sprite)

	# Add HP bar
	var hp_bar_bg = ColorRect.new()
	hp_bar_bg.size = Vector2(40, 6)
	hp_bar_bg.position = Vector2(-20, -30)
	hp_bar_bg.color = Color.BLACK
	add_child(hp_bar_bg)

	var hp_bar = ColorRect.new()
	hp_bar.name = "HPBar"
	hp_bar.size = Vector2(40, 6)
	hp_bar.position = Vector2(-20, -30)
	hp_bar.color = Color.GREEN
	add_child(hp_bar)

## Update HP bar visual
func _process(_delta: float) -> void:
	if has_node("HPBar"):
		var hp_bar = get_node("HPBar") as ColorRect
		var hp_percent = current_hp / max_hp
		hp_bar.size.x = 40 * hp_percent

		# Change color based on HP
		if hp_percent > 0.5:
			hp_bar.color = Color.GREEN
		elif hp_percent > 0.25:
			hp_bar.color = Color.YELLOW
		else:
			hp_bar.color = Color.RED
