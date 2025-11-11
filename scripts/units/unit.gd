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
@export var is_ranged: bool = false  # Ranged vs melee
@export var projectile_scene: PackedScene = null  # Projectile for ranged units

## Targeting settings
@export_group("Targeting")
@export var distance_weight: float = 1.0  ## Weight for distance in target scoring (higher = prefer closer targets)
@export var hp_weight: float = 0.3  ## Weight for HP in target scoring (higher = prefer low HP targets)
@export var target_lock_duration: float = 0.5  ## Duration in seconds to keep current target before re-evaluating

## Current state
var current_hp: float
var is_alive: bool = true
var current_target: Node2D = null  # Can be Unit or Summoner
var attack_cooldown: float = 0.0
var target_lock_timer: float = 0.0  # Time remaining before re-evaluating target

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
	target_lock_timer = max(target_lock_timer - delta, 0.0)

	# Re-acquire target if lock expired or current target is invalid
	if target_lock_timer <= 0.0 or not _is_valid_target(current_target):
		current_target = _acquire_target()
		if current_target:
			target_lock_timer = target_lock_duration

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

func _is_valid_target(target: Node2D) -> bool:
	## Check if a target is still valid (alive and in range)
	if not target or not is_instance_valid(target):
		return false
	if target is Unit and not target.is_alive:
		return false
	# Check if target is within aggro range
	var dist = global_position.distance_to(target.global_position)
	return dist <= aggro_radius * 1.5  # Allow some leeway

## Find the best enemy unit using weighted scoring system
func _acquire_target() -> Node2D:
	var target_group = "enemy_units" if team == Team.PLAYER else "player_units"
	var enemies = get_tree().get_nodes_in_group(target_group)

	var best_target: Node2D = null
	var best_score: float = -INF

	for target in enemies:
		if not (target is Unit and target.is_alive):
			continue

		var distance = global_position.distance_to(target.global_position)

		# Skip targets outside aggro range
		if distance > aggro_radius:
			continue

		# Calculate weighted score
		var score = 0.0

		# Distance component (inverse: closer = higher score)
		if distance_weight > 0.0 and distance > 0.01:
			score += distance_weight / distance

		# HP component (inverse: lower HP = higher score)
		if hp_weight > 0.0:
			var hp_percent = target.current_hp / target.max_hp
			# Add small epsilon to avoid division by zero
			score += hp_weight / (hp_percent + 0.1)

		# Track best scoring target
		if score > best_score:
			best_score = score
			best_target = target

	# If no units in range, target the enemy base
	if best_target == null:
		best_target = _get_enemy_base()

	return best_target

## Get the enemy base/summoner
func _get_enemy_base() -> Node2D:
	# First try to find actual bases
	var base_group = "enemy_bases" if team == Team.PLAYER else "player_bases"
	var bases = get_tree().get_nodes_in_group(base_group)

	if bases.size() > 0:
		return bases[0]

	# Fallback to summoners if no bases exist
	var summoner_group = "enemy_summoners" if team == Team.PLAYER else "player_summoners"
	var summoners = get_tree().get_nodes_in_group(summoner_group)

	if summoners.size() > 0:
		return summoners[0]

	return null

## Execute an attack on the target
func _attack(target: Node2D) -> void:
	if target == null:
		return

	if is_ranged:
		# Spawn projectile for ranged attack
		_spawn_projectile(target)
	else:
		# Direct damage for melee attack
		if target.has_method("take_damage"):
			target.take_damage(attack_damage)
			if target is Unit:
				unit_attacked.emit(target)

## Spawn a projectile toward the target
func _spawn_projectile(target: Node2D) -> void:
	if projectile_scene == null:
		push_error("Ranged unit has no projectile_scene set!")
		return

	# Instantiate projectile
	var projectile = projectile_scene.instantiate() as Projectile
	if projectile == null:
		return

	# Add to battlefield (get root node)
	get_tree().root.add_child(projectile)

	# Initialize projectile
	projectile.initialize(global_position, target, attack_damage, team, self)

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
	# Only create default sprite if no custom visual exists in the scene
	if not has_node("Visual"):
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
