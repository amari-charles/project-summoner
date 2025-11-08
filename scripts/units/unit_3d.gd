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
var pending_attack_target: Node3D = null  # Target for animation-driven damage
var is_facing_left: bool = false  # Current facing direction

## Visual component (base type - can be Sprite or Skeletal implementation)
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
		# Initialize facing direction based on team
		is_facing_left = (team == Team.ENEMY)
		# Sprites face LEFT by default, so flip PLAYER units to face right
		if team == Team.PLAYER and visual_component.has_method("set_flip_h"):
			visual_component.set_flip_h(true)
		# Play idle animation
		if visual_component.has_method("play_animation"):
			visual_component.play_animation("idle", true)
		return

	# Otherwise, load and instance the standard sprite-based 2.5D character component
	var component_scene = load("res://scenes/units/sprite_character_2d5_component.tscn")
	if component_scene:
		visual_component = component_scene.instantiate()
		add_child(visual_component)

		# Set sprite frames if provided
		if sprite_frames:
			visual_component.set_sprite_frames(sprite_frames)
			# Initialize facing direction based on team
			is_facing_left = (team == Team.ENEMY)
			# Sprites face LEFT by default, so flip PLAYER units to face right
			if team == Team.PLAYER:
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
			# Face opponent when idle in range
			_update_facing(current_target.global_position)
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

	# Face the direction we're moving
	_update_facing_from_direction(direction)

	velocity = direction * move_speed
	move_and_slide()

func _update_facing(target_position: Vector3) -> void:
	# Calculate direction to target and face that direction
	var direction = (target_position - global_position).normalized()
	_update_facing_from_direction(direction)

func _update_facing_from_direction(direction: Vector3) -> void:
	if not visual_component or not visual_component.has_method("set_flip_h"):
		return

	# Face left if direction has negative X component
	var should_face_left = direction.x < 0

	# Only flip if facing changed (avoid redundant calls)
	if should_face_left != is_facing_left:
		is_facing_left = should_face_left
		# Sprites face LEFT by default, so flip when should face RIGHT
		visual_component.set_flip_h(not is_facing_left)

func _perform_attack() -> void:
	if not current_target:
		return

	_update_animation("attack")
	attack_cooldown = 1.0 / attack_speed

	if is_ranged:
		# Ranged attacks spawn projectile immediately (projectile has travel time)
		_spawn_projectile()
	else:
		# Melee attacks store target for animation-driven damage
		pending_attack_target = current_target

		# Fallback: If no animation event fires within 0.5s, deal instant damage
		# (This handles sprite-based units without animation events)
		_start_attack_damage_fallback()

	unit_attacked.emit(current_target)

func _start_attack_damage_fallback() -> void:
	await get_tree().create_timer(0.5).timeout
	# If pending_attack_target still exists, animation event didn't fire
	if pending_attack_target:
		_on_attack_impact()  # Deal damage as fallback

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

## Called by animation event when attack impact occurs
func _on_attack_impact() -> void:
	if pending_attack_target and is_instance_valid(pending_attack_target):
		_deal_damage_to(pending_attack_target)
	pending_attack_target = null

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
	if not visual_component:
		return

	var current_anim = visual_component.get_current_animation()

	# Don't interrupt important animations (attack, hurt, death)
	if current_anim in ["attack", "hurt", "death"]:
		# Only block if animation is still playing
		if visual_component.is_playing() and anim_name not in ["attack", "hurt", "death"]:
			return  # Don't interrupt with idle/walk while animation is playing

	if current_anim != anim_name:
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
