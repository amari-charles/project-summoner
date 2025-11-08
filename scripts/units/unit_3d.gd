extends CharacterBody3D
class_name Unit3D

## 3D version of Unit for 2.5D battlefield
## Uses Character2D5Component for rendering

enum Team { PLAYER, ENEMY }

## Core stats
@export var max_hp: float = 100.0
@export var attack_damage: float = 10.0
@export var attack_range: float = 2.0
@export var attack_speed: float = 1.0
@export var move_speed: float = 3.0
@export var team: Team = Team.PLAYER
@export var aggro_radius: float = 20.0
@export var is_ranged: bool = false
@export var projectile_id: String = ""  # ID for ProjectileManager
@export var sprite_frames: SpriteFrames = null  # Animation frames for this unit

## Current state
var current_hp: float
var is_alive: bool = true
var current_target: Node3D = null
var attack_cooldown: float = 0.0

## Visual component
var visual_component: Character2D5Component = null

## Attachment points for projectiles and effects
@onready var projectile_spawn_point: Marker3D = $ProjectileSpawnPoint if has_node("ProjectileSpawnPoint") else null
@onready var projectile_target_point: Marker3D = $ProjectileTargetPoint if has_node("ProjectileTargetPoint") else null

## Signals
signal unit_died(unit: Unit3D)
signal unit_attacked(target: Node3D)
signal hp_changed(new_hp: float, new_max_hp: float)

func _ready() -> void:
	current_hp = max_hp
	add_to_group("units")

	if team == Team.PLAYER:
		add_to_group("player_units")
	else:
		add_to_group("enemy_units")

	_setup_visuals()

	# Spawn HP bar using HPBarManager
	HPBarManager.create_bar_for_unit(self)

func _setup_visuals() -> void:
	# Check if a visual component already exists (e.g., Skeletal2D5Component added in scene)
	visual_component = get_node_or_null("Visual")
	if visual_component:
		print("Unit3D: Using existing visual component: %s" % visual_component.name)
		# Flip enemy sprites to face left
		if team == Team.ENEMY and visual_component.has_method("set_flip_h"):
			visual_component.set_flip_h(true)
		# Play idle animation
		if visual_component.has_method("play_animation"):
			visual_component.play_animation("idle", true)
		return

	# Otherwise, load and instance the standard 2.5D character component
	var component_scene = load("res://scenes/units/character_2d5_component.tscn")
	if component_scene:
		visual_component = component_scene.instantiate()
		add_child(visual_component)

		# Set sprite frames if provided
		if sprite_frames:
			visual_component.set_sprite_frames(sprite_frames)
			# Flip enemy sprites to face left
			if team == Team.ENEMY:
				visual_component.set_flip_h(true)
			visual_component.play_animation("idle", true)

func _physics_process(delta: float) -> void:
	if not is_alive:
		return

	attack_cooldown = max(attack_cooldown - delta, 0.0)
	current_target = _acquire_target()

	if current_target:
		var distance = global_position.distance_to(current_target.global_position)

		if distance <= attack_range:
			_update_animation("idle")
			if attack_cooldown <= 0.0:
				_perform_attack()
		else:
			_update_animation("walk")
			_move_towards_target(delta)
	else:
		_update_animation("idle")

func _acquire_target() -> Node3D:
	var target_group = "enemy_units" if team == Team.PLAYER else "player_units"
	var targets = get_tree().get_nodes_in_group(target_group)

	var closest_target: Node3D = null
	var closest_distance: float = aggro_radius

	for target in targets:
		if target is Unit3D and target.is_alive:
			var dist = global_position.distance_to(target.global_position)
			if dist < closest_distance:
				closest_distance = dist
				closest_target = target

	# If no unit found, target the enemy base
	if not closest_target:
		var base_group = "enemy_base" if team == Team.PLAYER else "player_base"
		var bases = get_tree().get_nodes_in_group(base_group)
		if bases.size() > 0:
			closest_target = bases[0]

	return closest_target

func _move_towards_target(delta: float) -> void:
	if not current_target:
		return

	var direction = (current_target.global_position - global_position).normalized()
	# Only move on X and Z axes (2.5D movement)
	direction.y = 0
	velocity = direction * move_speed
	move_and_slide()

func _perform_attack() -> void:
	if not current_target:
		return

	_update_animation("attack")
	attack_cooldown = 1.0 / attack_speed

	if is_ranged:
		_spawn_projectile()
	else:
		_deal_damage_to(current_target)

	unit_attacked.emit(current_target)

func _spawn_projectile() -> void:
	if not current_target:
		return

	if not projectile_id.is_empty():
		# Use attachment points for proper spawn/target positions
		var spawn_pos = get_projectile_spawn_position()
		var target_pos = current_target.get_projectile_target_position() if current_target.has_method("get_projectile_target_position") else current_target.global_position

		ProjectileManager.spawn_projectile(
			projectile_id,
			self,
			current_target,
			attack_damage,
			"physical",
			{
				"start_position": spawn_pos,
				"target_position": target_pos
			}
		)

func _deal_damage_to(target: Node3D) -> void:
	# Use DamageSystem for centralized damage calculation
	DamageSystem.apply_damage(self, target, attack_damage, "physical")

func take_damage(amount: float) -> void:
	if not is_alive:
		return

	current_hp -= amount
	current_hp = max(current_hp, 0.0)

	# Emit signal for HP bars
	hp_changed.emit(current_hp, max_hp)

	_update_animation("hurt")

	if current_hp <= 0:
		_die()

func _die() -> void:
	is_alive = false
	_update_animation("death")
	unit_died.emit(self)

	# Remove HP bar
	HPBarManager.remove_bar_from_unit(self)

	# Wait for death animation then remove
	await get_tree().create_timer(1.0).timeout
	queue_free()

func _update_animation(anim_name: String) -> void:
	if visual_component and visual_component.get_current_animation() != anim_name:
		visual_component.play_animation(anim_name)

## Get the world position where projectiles should spawn from
func get_projectile_spawn_position() -> Vector3:
	if projectile_spawn_point:
		return projectile_spawn_point.global_position
	# Fallback: use visual component position or unit position + offset
	if visual_component:
		return global_position + Vector3(0, 1.0, 0)
	return global_position

## Get the world position where projectiles should target
func get_projectile_target_position() -> Vector3:
	if projectile_target_point:
		return projectile_target_point.global_position
	# Fallback: aim slightly higher than unit position (chest height)
	return global_position + Vector3(0, 1.2, 0)
