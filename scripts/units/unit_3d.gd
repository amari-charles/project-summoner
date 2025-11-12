extends CharacterBody3D
class_name Unit3D

## 3D version of Unit for 2.5D battlefield
## Uses Character2D5Component for rendering

enum Team { PLAYER, ENEMY }
enum UnitType { MELEE, RANGED }
enum MovementLayer { GROUND, AIR }  # For future air units

## Projectile prediction constants
const VELOCITY_STATIONARY_THRESHOLD: float = 0.01  # Squared velocity magnitude (units²/sec²) - below this, target is considered stationary
const MAX_PREDICTION_DISTANCE: float = 50.0  # Max distance projectile can predict ahead (prevents absurd predictions)
const MIN_PROJECTILE_SPEED: float = 1.0  # Minimum projectile speed to prevent division-by-near-zero

## Core stats
@export var max_hp: float = 100.0
@export var attack_damage: float = 10.0
@export var attack_speed: float = 1.0
@export var move_speed: float = 3.0
@export var team: Team = Team.PLAYER
@export var aggro_radius: float = 20.0

## Base stats (before modifiers)
var base_max_hp: float
var base_attack_damage: float
var base_attack_speed: float
var base_move_speed: float

## Active modifiers (merged flags from all modifiers)
var active_modifiers: Dictionary = {}

## Unit classification
@export var unit_type: UnitType = UnitType.MELEE
@export var movement_layer: MovementLayer = MovementLayer.GROUND

## Attack range (per-axis for melee, ignored for ranged)
@export var attack_range: float = 2.0              # X-axis (left-right) / base range for ranged
@export var attack_range_depth: float = 1.0        # Z-axis (lane depth) - melee only
@export var attack_range_vertical: float = 0.5     # Y-axis (height tolerance) - melee only

## Ranged attack settings
@export var is_ranged: bool = false  # DEPRECATED: Use unit_type instead
@export var projectile_id: String = "":  # ID for ProjectileManager
	set(value):
		projectile_id = value
		cached_projectile_speed = -1.0  # Invalidate cache when projectile changes

## Targeting settings
@export_group("Targeting")
@export var distance_weight: float = 1.0  ## Weight for distance in target scoring (higher = prefer closer targets)
@export var hp_weight: float = 0.3  ## Weight for HP in target scoring (higher = prefer low HP targets)
@export var target_lock_duration: float = 0.5  ## Duration in seconds to keep current target before re-evaluating

## Visuals
@export var sprite_frames: SpriteFrames = null  # Animation frames for this unit
@export var sprite_feet_offset_pixels: float = 0.0  ## Offset from texture bottom to actual feet (for sprites with empty space below)
@export var sprite_scale: float = 2.5  ## Scale for sprite in viewport (default 2.5 for 100px sprites, use 0.806 for 310px sprites)

## Shadow settings
@export var shadow_enabled: bool = true
@export var shadow_size: float = 0.0  ## Auto-calculated from sprite size (set to 0 for auto)
@export var shadow_opacity: float = 0.6

## Current state
var current_hp: float
var is_alive: bool = true
var current_target: Node3D = null
var attack_cooldown: float = 0.0
var pending_attack_target: Node3D = null  # Target for animation-driven damage
var is_facing_left: bool = false  # Current facing direction
var is_attacking: bool = false  # Track if currently in attack animation
var target_lock_timer: float = 0.0  # Time remaining before re-evaluating target

## Projectile prediction cache
var cached_projectile_speed: float = -1.0  # Cached speed lookup (-1 = not cached)

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
	# Legacy support: If spawned without initialize_with_modifiers(), set up base stats
	# Note: This path does NOT apply modifiers - units should use initialize_with_modifiers()
	if base_max_hp == 0:
		_store_base_stats()
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
		visual_component.name = "Visual"  # Name it so HP bars can find it

		# Configure feet offset if specified
		if sprite_feet_offset_pixels > 0.0 and "feet_offset_pixels" in visual_component:
			visual_component.feet_offset_pixels = sprite_feet_offset_pixels

		# Configure sprite scale if specified
		if "sprite_scale" in visual_component:
			visual_component.sprite_scale = sprite_scale

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

	# Auto-calculate shadow size from visual component if available, otherwise collision shape
	if shadow_size <= 0.0:
		if visual_component and visual_component.has_method("get_sprite_height"):
			# Shadow should be proportional to sprite width (roughly 1/3 of height for human proportions)
			var sprite_height = visual_component.get_sprite_height()
			shadow_size = sprite_height * 0.35  # ~35% of height
		else:
			shadow_size = _calculate_shadow_size_from_collision()

	# Load the ShadowComponent script
	var shadow_script = load("res://scripts/units/shadow_component.gd")
	if not shadow_script:
		push_warning("Unit3D: Failed to load shadow_component.gd")
		return

	# Create shadow instance (MeshInstance3D node)
	shadow_component = MeshInstance3D.new()
	shadow_component.set_script(shadow_script)

	# Add as child (will follow unit automatically)
	add_child(shadow_component)

	# Initialize with proper values (explicit initialization pattern)
	shadow_component.initialize(shadow_size, shadow_opacity)

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

## =============================================================================
## INITIALIZATION & MODIFIER APPLICATION
## =============================================================================

## Initialize unit with modifiers BEFORE adding to scene tree
## This ensures stats are correct before _ready() fires
func initialize_with_modifiers(modifiers: Array, card_data: Dictionary = {}) -> void:
	# Store base stats from @export values
	_store_base_stats()

	# Apply modifiers to calculate final stats
	apply_modifiers(modifiers, card_data)

## Store base stats from current @export values
func _store_base_stats() -> void:
	base_max_hp = max_hp
	base_attack_damage = attack_damage
	base_attack_speed = attack_speed
	base_move_speed = move_speed

## Apply modifiers to this unit's stats
## Assumes base stats are already stored via _store_base_stats()
##
## @param modifiers: Array of modifier dictionaries
## @param card_data: Card data for context
func apply_modifiers(modifiers: Array, card_data: Dictionary = {}) -> void:
	# Start from base stats
	var stats = {
		"max_hp": base_max_hp,
		"attack_damage": base_attack_damage,
		"attack_speed": base_attack_speed,
		"move_speed": base_move_speed
	}

	# Phase 1: Sum all additive bonuses
	var adds = {
		"max_hp": 0.0,
		"attack_damage": 0.0,
		"attack_speed": 0.0,
		"move_speed": 0.0
	}

	for mod in modifiers:
		var stat_adds = mod.get("stat_adds", {})
		for stat in stat_adds.keys():
			if adds.has(stat):
				adds[stat] += stat_adds[stat]

	# Apply additive bonuses
	for stat in adds.keys():
		stats[stat] += adds[stat]

	# Phase 2: Multiply all multiplicative bonuses (additive within phase)
	var mults = {
		"max_hp": 0.0,  # Start at 0, will add bonuses (e.g., 1.3 becomes 0.3)
		"attack_damage": 0.0,
		"attack_speed": 0.0,
		"move_speed": 0.0
	}

	for mod in modifiers:
		var stat_mults = mod.get("stat_mults", {})
		for stat in stat_mults.keys():
			if mults.has(stat):
				# Convert multiplier to bonus: 1.3 → 0.3
				var bonus = stat_mults[stat] - 1.0
				mults[stat] += bonus

	# Apply multiplicative bonuses
	for stat in mults.keys():
		stats[stat] *= (1.0 + mults[stat])

	# Phase 3: Apply final stats
	max_hp = stats.max_hp
	attack_damage = stats.attack_damage
	attack_speed = stats.attack_speed
	move_speed = stats.move_speed
	current_hp = max_hp  # Start at full HP

	# Phase 4: Merge all flags
	active_modifiers.clear()
	for mod in modifiers:
		var flags = mod.get("flags", {})
		active_modifiers.merge(flags, true)


## =============================================================================
## PHYSICS & BEHAVIOR
## =============================================================================

func _physics_process(delta: float) -> void:
	if not is_alive:
		return

	attack_cooldown = max(attack_cooldown - delta, 0.0)
	target_lock_timer = max(target_lock_timer - delta, 0.0)

	# Re-acquire target if lock expired or current target is invalid
	if target_lock_timer <= 0.0 or not _is_valid_target(current_target):
		current_target = _acquire_target()
		if current_target:
			target_lock_timer = target_lock_duration

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

func _is_valid_target(target: Node3D) -> bool:
	## Check if a target is still valid (alive and in range)
	if not target or not is_instance_valid(target):
		return false
	if target is Unit3D and not target.is_alive:
		return false
	# Check if target is within aggro range (use distance_squared for performance)
	var delta = target.global_position - global_position
	var distance_sq = delta.x * delta.x + delta.z * delta.z
	var max_range = aggro_radius * 1.5  # Allow some leeway
	return distance_sq <= max_range * max_range

func _acquire_target() -> Node3D:
	## Find the best target using weighted scoring system
	var target_group = "enemy_units" if team == Team.PLAYER else "player_units"
	var targets = get_tree().get_nodes_in_group(target_group)

	var best_target: Node3D = null
	var best_score: float = -INF
	var aggro_radius_sq = aggro_radius * aggro_radius

	for target in targets:
		if not (target is Unit3D and target.is_alive):
			continue

		# Calculate horizontal distance_squared (ignore Y-axis) - no sqrt yet!
		var delta = target.global_position - global_position
		var distance_sq = delta.x * delta.x + delta.z * delta.z

		# Fast filtering: skip targets outside aggro range (no sqrt needed)
		if distance_sq > aggro_radius_sq:
			continue

		# Only calculate actual distance (sqrt) for targets in range
		var distance = sqrt(distance_sq)

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

	# If no unit found, target the enemy base
	if not best_target:
		var base_group = "enemy_base" if team == Team.PLAYER else "player_base"
		var bases = get_tree().get_nodes_in_group(base_group)
		if bases.size() > 0:
			best_target = bases[0]

	return best_target

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

	# Face left if direction has negative X component (towards player base on left)
	var should_face_left = direction.x < 0

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

		# Apply predictive targeting for moving targets
		target_pos = _calculate_intercept_point(spawn_pos, target_pos, current_target)

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

## Calculate intercept point for projectile targeting
## Uses constant velocity assumption + 1 iteration refinement (industry standard approach)
## NOTE: Constant velocity is acceptable because most units don't accelerate mid-flight
##       and collision hulls are large enough to accommodate minor errors
func _calculate_intercept_point(shooter_pos: Vector3, target_pos: Vector3, target: Node3D) -> Vector3:
	# Get projectile speed from ContentCatalog (cached)
	var projectile_speed = _get_projectile_speed()
	if projectile_speed <= 0:
		return target_pos  # Fallback to current position

	# Get target velocity
	var target_velocity = Vector3.ZERO
	if target is CharacterBody3D:
		target_velocity = target.velocity
	elif "velocity" in target:
		target_velocity = target.velocity

	# If target is stationary, no prediction needed
	if target_velocity.length_squared() < VELOCITY_STATIONARY_THRESHOLD:
		return target_pos

	# Calculate distance for initial time-to-impact estimation
	var distance = (target_pos - shooter_pos).length()

	# Simple time-to-impact estimation
	var time_to_impact = distance / projectile_speed

	# Predict target position at impact time
	var predicted_pos = target_pos + (target_velocity * time_to_impact)

	# Iterative refinement (one iteration is optimal - research shows diminishing returns after this)
	var refined_distance = (predicted_pos - shooter_pos).length()
	var refined_time = refined_distance / projectile_speed
	predicted_pos = target_pos + (target_velocity * refined_time)

	# Bounds validation: ensure prediction isn't absurdly far from current position
	var prediction_offset = (predicted_pos - target_pos).length()
	if prediction_offset > MAX_PREDICTION_DISTANCE:
		# Clamp to max distance in the direction of movement
		var direction = predicted_pos - target_pos
		if direction.length_squared() > 0.001:  # Prevent zero-vector normalization
			direction = direction.normalized()
			predicted_pos = target_pos + (direction * MAX_PREDICTION_DISTANCE)
		else:
			# Prediction collapsed to current position (edge case), use current position
			predicted_pos = target_pos

	return predicted_pos

## Get projectile speed from ContentCatalog (cached for performance)
func _get_projectile_speed() -> float:
	# Return cached value if available
	if cached_projectile_speed >= 0:
		return max(cached_projectile_speed, MIN_PROJECTILE_SPEED)

	# Lazy initialization: lookup once and cache
	if projectile_id.is_empty():
		cached_projectile_speed = 0.0
		return 0.0

	if not ContentCatalog or not ContentCatalog.projectiles.has(projectile_id):
		cached_projectile_speed = 15.0  # Default speed
		return max(15.0, MIN_PROJECTILE_SPEED)

	var proj_data = ContentCatalog.projectiles[projectile_id]
	if proj_data and "speed" in proj_data:
		cached_projectile_speed = proj_data.speed
		return max(proj_data.speed, MIN_PROJECTILE_SPEED)

	cached_projectile_speed = 15.0  # Default speed
	return max(15.0, MIN_PROJECTILE_SPEED)

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
	# Dynamic: query visual component for sprite height
	if visual_component and visual_component.has_method("get_sprite_height"):
		var sprite_height = visual_component.get_sprite_height()
		# Spawn at ~60% of height (chest/hand level for archers)
		return global_position + Vector3(0, sprite_height * 0.6, 0)
	# Fallback for units without visual component
	return global_position + Vector3(0, 1.0, 0)

## Get the world position where projectiles should target
func get_projectile_target_position() -> Vector3:
	if projectile_target_point:
		return projectile_target_point.global_position
	# Dynamic: query visual component for sprite height
	if visual_component and visual_component.has_method("get_sprite_height"):
		var sprite_height = visual_component.get_sprite_height()
		# Target at ~60% of height (chest area)
		return global_position + Vector3(0, sprite_height * 0.6, 0)
	# Fallback for units without visual component
	return global_position + Vector3(0, 1.2, 0)
