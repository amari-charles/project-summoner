extends CharacterBody3D
class_name Unit3D

## 3D version of Unit for 2.5D battlefield
## Uses Character2D5Component for rendering

enum Team { PLAYER, ENEMY }
enum UnitType { MELEE, RANGED }
enum MovementLayer { GROUND, AIR }  # For future air units

## Core stats
@export var max_hp: float = 100.0
@export var attack_damage: float = 10.0
@export var attack_speed: float = 1.0
@export var move_speed: float = 3.0
@export var team: Team = Team.PLAYER
@export var aggro_radius: float = 20.0

## Unit classification
@export var unit_type: UnitType = UnitType.MELEE
@export var movement_layer: MovementLayer = MovementLayer.GROUND

## Attack range (per-axis for melee, ignored for ranged)
@export var attack_range: float = 2.0              # X-axis (left-right) / base range for ranged
@export var attack_range_depth: float = 1.0        # Z-axis (lane depth) - melee only
@export var attack_range_vertical: float = 0.5     # Y-axis (height tolerance) - melee only

## Ranged attack settings
@export var is_ranged: bool = false  # DEPRECATED: Use unit_type instead
@export var projectile_id: String = ""  # ID for ProjectileManager

## Visuals
@export var sprite_frames: SpriteFrames = null  # Animation frames for this unit

## Shadow settings
@export var shadow_enabled: bool = true
@export var shadow_size: float = 1.0
@export var shadow_opacity: float = 0.6

## Current state
var current_hp: float
var is_alive: bool = true
var current_target: Node3D = null
var attack_cooldown: float = 0.0
var pending_attack_target: Node3D = null  # Target for animation-driven damage
var is_facing_left: bool = false  # Current facing direction
var is_attacking: bool = false  # Track if currently in attack animation

## Visual component (base type - can be Sprite or Skeletal implementation)
var visual_component: Character2D5Component = null
var shadow_component: MeshInstance3D = null

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
	_setup_shadow()

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

func _setup_shadow() -> void:
	if not shadow_enabled:
		return

	# Auto-calculate shadow size from collision shape if not manually set
	if shadow_size <= 0.0:
		shadow_size = _calculate_shadow_size_from_collision()

	# Load the ShadowComponent script
	var shadow_script = load("res://scripts/units/shadow_component.gd")
	if not shadow_script:
		push_warning("Unit3D: Failed to load shadow_component.gd")
		return

	# Create shadow instance
	shadow_component = MeshInstance3D.new()
	shadow_component.set_script(shadow_script)
	shadow_component.shadow_size = shadow_size
	shadow_component.shadow_opacity = shadow_opacity

	# Add as child (will follow unit automatically)
	add_child(shadow_component)

## Calculate shadow size based on collision shape
func _calculate_shadow_size_from_collision() -> float:
	# Find CollisionShape3D child
	for child in get_children():
		if child is CollisionShape3D:
			var shape = child.shape
			if shape is CapsuleShape3D:
				# Shadow diameter = radius * 2.5 (a bit larger than capsule base)
				return shape.radius * 2.5
			elif shape is BoxShape3D:
				# Use average of X and Z extents
				var extents = shape.size
				return (extents.x + extents.z) / 2.0 * 1.2
			elif shape is SphereShape3D:
				return shape.radius * 2.2

	# Fallback to default
	return 1.0

func _physics_process(delta: float) -> void:
	if not is_alive:
		return

	attack_cooldown = max(attack_cooldown - delta, 0.0)
	current_target = _acquire_target()

	if current_target:
		if _is_in_attack_range(current_target):
			# Face opponent when idle in range (but not during attack)
			if not is_attacking:
				_update_facing(current_target.global_position)
				_update_animation("idle")
			if attack_cooldown <= 0.0:
				_perform_attack()
		else:
			# Don't move during attack animation
			if not is_attacking:
				_update_animation("walk")
				_move_towards_target(delta)
	else:
		if not is_attacking:
			_update_animation("idle")

func _acquire_target() -> Node3D:
	var target_group = "enemy_units" if team == Team.PLAYER else "player_units"
	var targets = get_tree().get_nodes_in_group(target_group)

	var closest_target: Node3D = null
	var closest_distance: float = aggro_radius

	for target in targets:
		if target is Unit3D and target.is_alive:
			# Use horizontal distance for aggro (ignore Y-axis height)
			var delta = target.global_position - global_position
			var horizontal_dist = Vector2(delta.x, delta.z).length()
			if horizontal_dist < closest_distance:
				closest_distance = horizontal_dist
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

## Check if target is within attack range
## TODO: Alternative approach - weighted/ellipse distance for smooth falloff
## Currently using box-shaped range (per-axis checking) for melee
func _is_in_attack_range(target: Node3D) -> bool:
	if not target:
		return false

	var delta = target.global_position - global_position

	# Ranged units use simple 3D distance (sphere)
	if unit_type == UnitType.RANGED or is_ranged:  # Support legacy is_ranged
		var distance = global_position.distance_to(target.global_position)
		return distance <= attack_range

	# Melee units use box-shaped range (per-axis checking)
	# This prevents attacking across lanes or at different heights
	if abs(delta.x) > attack_range:  # Left-right
		return false
	if abs(delta.y) > attack_range_vertical:  # Height tolerance
		return false
	if abs(delta.z) > attack_range_depth:  # Lane/depth
		return false

	return true

func _update_facing(target_position: Vector3) -> void:
	# Calculate direction to target and face that direction
	var direction = (target_position - global_position).normalized()
	_update_facing_from_direction(direction)

func _update_facing_from_direction(direction: Vector3) -> void:
	if not visual_component or not visual_component.has_method("set_flip_h"):
		return

	# Face left if direction has negative Z component (towards player base)
	var should_face_left = direction.z < 0

	# Only flip if facing changed (avoid redundant calls)
	if should_face_left != is_facing_left:
		is_facing_left = should_face_left
		# Sprites face LEFT by default, so flip when should face RIGHT
		visual_component.set_flip_h(not is_facing_left)

func _perform_attack() -> void:
	if not current_target:
		return

	is_attacking = true
	_update_animation("attack")

	# Query actual animation duration from visual component
	var attack_duration = 1.0 / attack_speed  # Cooldown duration
	var animation_duration = 1.0  # Fallback
	if visual_component and visual_component.has_method("get_animation_duration"):
		animation_duration = visual_component.get_animation_duration("attack")

	attack_cooldown = attack_duration

	if is_ranged:
		# Ranged attacks spawn projectile immediately (projectile has travel time)
		_spawn_projectile()
	else:
		# Melee attacks store target for animation-driven damage
		pending_attack_target = current_target

		# Fallback: If no animation event fires within 0.5s, deal instant damage
		# (This handles sprite-based units without animation events)
		_start_attack_damage_fallback()

	# Clear attacking state after animation completes (not cooldown!)
	_clear_attacking_state(animation_duration)

	unit_attacked.emit(current_target)

func _start_attack_damage_fallback() -> void:
	await get_tree().create_timer(0.5).timeout
	# If pending_attack_target still exists, animation event didn't fire
	if pending_attack_target:
		_on_attack_impact()  # Deal damage as fallback

func _clear_attacking_state(duration: float) -> void:
	await get_tree().create_timer(duration).timeout
	is_attacking = false

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

	# TODO: Hurt animations disabled for now to prevent interrupting attacks
	# _update_animation("hurt")

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
