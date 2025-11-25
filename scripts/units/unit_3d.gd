extends CharacterBody3D
class_name Unit3D

## 3D version of Unit for 2.5D battlefield
## Uses Character2D5Component for rendering

enum Team { PLAYER, ENEMY }
enum UnitType { MELEE, RANGED }
enum MovementLayer { GROUND, AIR }
enum TargetLayer { GROUND_ONLY, AIR_ONLY, BOTH }  # What layers this unit can target

## Projectile prediction constants
const VELOCITY_STATIONARY_THRESHOLD: float = 0.01  # Squared velocity magnitude (units²/sec²) - below this, target is considered stationary
const MAX_PREDICTION_DISTANCE: float = 50.0  # Max distance projectile can predict ahead (prevents absurd predictions)
const MIN_PROJECTILE_SPEED: float = 1.0  # Minimum projectile speed to prevent division-by-near-zero

## Flying unit constants
const MIN_FLIGHT_ALTITUDE: float = 0.5  ## Minimum altitude for flying units
const MAX_FLIGHT_ALTITUDE: float = 10.0  ## Maximum altitude for flying units (affects shadow scaling)
const SHADOW_SIZE_REDUCTION_FACTOR: float = 0.4  ## At max altitude, shadow is 60% of original size (1.0 - 0.4)
const SHADOW_OPACITY_REDUCTION_FACTOR: float = 0.6  ## At max altitude, shadow is 40% of original opacity (1.0 - 0.6)

## Unit separation constants (prevents stacking during movement)
const SEPARATION_RADIUS: float = 1.5  ## Distance at which separation force kicks in
const SEPARATION_STRENGTH: float = 3.0  ## Strength of separation push

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
@export var can_target: TargetLayer = TargetLayer.BOTH  ## What layers this unit can attack
@export var flight_altitude: float = 2.5  ## Visual height for flying units (AIR layer only)
@export var invert_facing: bool = false  ## Invert default facing logic (for sprites that face right instead of left)

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
@export var sprite_head_offset_pixels: float = 0.0  ## Offset from texture top to actual head (for sprites with empty space above)
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

## Redirect system (forced targeting)
var forced_target: Node3D = null  ## Target assigned by redirect system (overrides normal targeting)
var forced_target_timer: float = 0.0  ## Time remaining for forced target commitment
var original_redirect_point: Vector3 = Vector3.ZERO  ## Original point for fallback targeting

## Rally system (simple movement)
var rally_point: Vector3 = Vector3.ZERO  ## Point to move toward
var rally_mode: bool = false  ## Whether unit is in rally mode

## Guard system (formation hold)
var guard_mode: bool = false  ## Whether unit is in guard mode
var guard_timer: float = 0.0  ## Time remaining for guard mode
var formation_position: Vector3 = Vector3.ZERO  ## Target position in formation

## Proximity tracking (for tutorial/events)
var _has_emitted_proximity_signal: bool = false  ## Track if we've already emitted proximity signal

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
		print("Unit3D: Added to player_units group (team=%d)" % team)
	else:
		add_to_group("enemy_units")
		print("Unit3D: Added to enemy_units group (team=%d)" % team)

	_setup_visuals()
	_setup_shadow()

	# Set physics position and adjust visuals for flying units
	if movement_layer == MovementLayer.AIR:
		# Validate and clamp altitude to valid range
		flight_altitude = clamp(flight_altitude, MIN_FLIGHT_ALTITUDE, MAX_FLIGHT_ALTITUDE)

		# Set physics body position for proper collision at altitude
		position.y = flight_altitude

		# Visual component stays at relative position 0 (already elevated via parent)
		if visual_component:
			visual_component.position.y = 0

		# Adjust shadow for altitude (smaller and more transparent at higher altitudes)
		if shadow_component:
			var altitude_factor: float = flight_altitude / MAX_FLIGHT_ALTITUDE  # Normalize altitude
			altitude_factor = clamp(altitude_factor, 0.0, 1.0)

			# Scale shadow down with altitude
			var size_scale: float = 1.0 - (altitude_factor * SHADOW_SIZE_REDUCTION_FACTOR)
			shadow_component.call("set_shadow_radius", shadow_size * size_scale)

			# Fade shadow opacity with altitude
			var opacity_scale: float = 1.0 - (altitude_factor * SHADOW_OPACITY_REDUCTION_FACTOR)
			shadow_component.call("set_shadow_opacity", shadow_opacity * opacity_scale)

	# Spawn HP bar using HPBarManager (offset calculated automatically based on scale)
	HPBarManager.create_bar_for_unit(self)

	# Setup abilities
	_setup_abilities()

func _setup_abilities() -> void:
	# Find all BaseAbility components attached as children
	for child: Node in get_children():
		if child is BaseAbility:
			var ability: BaseAbility = child as BaseAbility
			ability.setup(self)

func _setup_visuals() -> void:
	# Check if a visual component already exists (e.g., Skeletal2D5Component added in scene)
	visual_component = get_node_or_null("Visual")
	if visual_component:
		# Initialize facing direction based on team
		is_facing_left = (team == Team.ENEMY)
		# Sprites face LEFT by default, so flip PLAYER units to face right
		# (invert_facing reverses this for sprites that naturally face right)
		var should_flip: bool = (team == Team.PLAYER) != invert_facing
		if should_flip and visual_component.has_method("set_flip_h"):
			visual_component.set_flip_h(true)
		# Play idle animation
		if visual_component.has_method("play_animation"):
			visual_component.play_animation("idle", true)
		return

	# Otherwise, load and instance the standard sprite-based 2.5D character component
	var component_scene: PackedScene = load("res://scenes/units/sprite_character_2d5_component.tscn")
	if component_scene:
		visual_component = component_scene.instantiate()
		visual_component.name = "Visual"  # Name it so HP bars can find it

		# Configure feet offset if specified
		if sprite_feet_offset_pixels > 0.0 and "feet_offset_pixels" in visual_component:
			visual_component.set("feet_offset_pixels", sprite_feet_offset_pixels)

		# Configure head offset if specified
		if sprite_head_offset_pixels > 0.0 and "head_offset_pixels" in visual_component:
			visual_component.set("head_offset_pixels", sprite_head_offset_pixels)

		# Configure sprite scale if specified
		if "sprite_scale" in visual_component:
			visual_component.set("sprite_scale", sprite_scale)

		add_child(visual_component)

		# Set sprite frames if provided
		if sprite_frames and visual_component.has_method("set_sprite_frames"):
			visual_component.call("set_sprite_frames", sprite_frames)
			# Initialize facing direction based on team
			is_facing_left = (team == Team.ENEMY)
			# Sprites face LEFT by default, so flip PLAYER units to face right
			# (invert_facing reverses this for sprites that naturally face right)
			var should_flip_2: bool = (team == Team.PLAYER) != invert_facing
			if should_flip_2:
				visual_component.set_flip_h(true)
			visual_component.play_animation("idle", true)

func _setup_shadow() -> void:
	if not shadow_enabled:
		return

	# Auto-calculate shadow size from visual component if available, otherwise collision shape
	if shadow_size <= 0.0:
		if visual_component and visual_component.has_method("get_sprite_height"):
			# Shadow should be proportional to sprite width (roughly 1/3 of height for human proportions)
			var sprite_height: float = visual_component.get_sprite_height()
			shadow_size = sprite_height * 0.35  # ~35% of height
		else:
			shadow_size = _calculate_shadow_size_from_collision()

	# Load the ShadowComponent script
	var shadow_script: GDScript = load("res://scripts/units/shadow_component.gd")
	if not shadow_script:
		push_warning("Unit3D: Failed to load shadow_component.gd")
		return

	# Create shadow instance (MeshInstance3D node)
	shadow_component = MeshInstance3D.new()
	shadow_component.set_script(shadow_script)

	# Add as child (will follow unit automatically)
	add_child(shadow_component)

	# Initialize with proper values (explicit initialization pattern)
	shadow_component.call("initialize", shadow_size, shadow_opacity)

## Calculate shadow size based on collision shape
func _calculate_shadow_size_from_collision() -> float:
	# Find CollisionShape3D child
	var children: Array[Node] = get_children()
	for child: Node in children:
		if child is CollisionShape3D:
			# Type narrow to CollisionShape3D for safe property access
			var collision_shape: CollisionShape3D = child
			var shape: Shape3D = collision_shape.shape
			if shape is CapsuleShape3D:
				# Type narrow to CapsuleShape3D for safe property access
				var capsule: CapsuleShape3D = shape
				# Shadow diameter = radius * 2.5 (a bit larger than capsule base)
				return capsule.radius * 2.5
			elif shape is BoxShape3D:
				# Type narrow to BoxShape3D for safe property access
				var box: BoxShape3D = shape
				# Use average of X and Z extents
				var extents: Vector3 = box.size
				return (extents.x + extents.z) / 2.0 * 1.2
			elif shape is SphereShape3D:
				# Type narrow to SphereShape3D for safe property access
				var sphere: SphereShape3D = shape
				return sphere.radius * 2.2

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
func apply_modifiers(modifiers: Array, _card_data: Dictionary = {}) -> void:
	# Start from base stats
	var stats: Dictionary = {
		"max_hp": base_max_hp,
		"attack_damage": base_attack_damage,
		"attack_speed": base_attack_speed,
		"move_speed": base_move_speed
	}

	# Phase 1: Sum all additive bonuses
	var adds: Dictionary = {
		"max_hp": 0.0,
		"attack_damage": 0.0,
		"attack_speed": 0.0,
		"move_speed": 0.0
	}

	for mod: Variant in modifiers:
		if not mod is Dictionary:
			continue
		var mod_dict: Dictionary = mod
		var empty_adds: Dictionary = {}
		var stat_adds: Dictionary = mod_dict.get("stat_adds", empty_adds)
		var stat_keys: Array = stat_adds.keys()
		for stat: Variant in stat_keys:
			if stat is String and adds.has(stat):
				var stat_key: String = stat
				adds[stat_key] += stat_adds[stat_key]

	# Apply additive bonuses
	var add_keys: Array = adds.keys()
	for stat: Variant in add_keys:
		if stat is String:
			var stat_key: String = stat
			stats[stat_key] += adds[stat_key]

	# Phase 2: Multiply all multiplicative bonuses (additive within phase)
	var mults: Dictionary = {
		"max_hp": 0.0,  # Start at 0, will add bonuses (e.g., 1.3 becomes 0.3)
		"attack_damage": 0.0,
		"attack_speed": 0.0,
		"move_speed": 0.0
	}

	for mod: Variant in modifiers:
		if not mod is Dictionary:
			continue
		var mod_dict: Dictionary = mod
		var empty_mults: Dictionary = {}
		var stat_mults: Dictionary = mod_dict.get("stat_mults", empty_mults)
		var mult_keys: Array = stat_mults.keys()
		for stat: Variant in mult_keys:
			if stat is String and mults.has(stat):
				var stat_key: String = stat
				# Convert multiplier to bonus: 1.3 → 0.3
				var bonus: float = stat_mults[stat_key] - 1.0
				mults[stat_key] += bonus

	# Apply multiplicative bonuses
	var mult_result_keys: Array = mults.keys()
	for stat: Variant in mult_result_keys:
		if stat is String:
			var stat_key: String = stat
			stats[stat_key] *= (1.0 + mults[stat_key])

	# Phase 3: Apply final stats
	max_hp = stats.max_hp
	attack_damage = stats.attack_damage
	attack_speed = stats.attack_speed
	move_speed = stats.move_speed
	current_hp = max_hp  # Start at full HP

	# Phase 4: Merge all flags
	active_modifiers.clear()
	for mod: Variant in modifiers:
		if not mod is Dictionary:
			continue
		var mod_dict: Dictionary = mod
		var empty_flags: Dictionary = {}
		var flags: Dictionary = mod_dict.get("flags", empty_flags)
		active_modifiers.merge(flags, true)


## =============================================================================
## PHYSICS & BEHAVIOR
## =============================================================================

func _physics_process(delta: float) -> void:
	if not is_alive:
		return

	# Flying units stay at constant altitude (disable gravity)
	if movement_layer == MovementLayer.AIR:
		velocity.y = 0.0  # No vertical movement

	attack_cooldown = max(attack_cooldown - delta, 0.0)
	target_lock_timer = max(target_lock_timer - delta, 0.0)

	# Tick down forced_target timer
	if forced_target_timer > 0.0:
		forced_target_timer -= delta
		if forced_target_timer <= 0.0:
			# Forced target expired - clear redirect state
			forced_target = null
			original_redirect_point = Vector3.ZERO

	# Handle Guard mode (formation + hold position)
	if guard_mode:
		guard_timer -= delta
		_update_guard_behavior(delta)
		return  # Guard overrides normal behavior

	# Handle Rally mode (move to point + defend zone)
	if rally_mode:
		_update_rally_behavior(delta)
		return  # Rally overrides normal behavior

	# Check proximity to enemy base (for tutorial/events)
	_check_proximity_to_enemy_base()

	# Re-acquire target if:
	# - Lock expired
	# - Current target invalid
	# - Forced target exists and differs from current (Charge spell override)
	var should_reacquire: bool = (
		target_lock_timer <= 0.0 or
		not _is_valid_target(current_target) or
		(forced_target and forced_target != current_target and _is_valid_target(forced_target))
	)

	if should_reacquire:
		current_target = _acquire_target()
		if current_target:
			target_lock_timer = target_lock_duration

	if current_target:
		var in_range: bool = _is_in_attack_range(current_target)
		if in_range:
			# Face opponent when idle in range (but not during attack)
			if not is_attacking:
				_update_facing(current_target.global_position)
				_update_animation("idle")
			if attack_cooldown <= 0.0:
				_perform_attack()
			else:
				pass  # On cooldown, wait
		else:
			# Don't move during attack animation
			if not is_attacking:
				_update_animation("walk")
				var old_pos: Vector3 = global_position
				_move_towards_target(delta)
				var moved: float = global_position.distance_to(old_pos)
				# Only warn if move_speed > 0 (intentionally stationary units shouldn't warn)
				if moved < 0.01 and move_speed > 0.01:
					print("Unit3D [team %d]: WARNING - Not moving! pos: %s, target pos: %s" % [team, global_position, current_target.global_position])
	else:
		if not is_attacking:
			_update_animation("idle")

func _check_proximity_to_enemy_base() -> void:
	## Check if player unit is near enemy base and emit signal (once per unit)
	## Used for tutorial/event triggers

	# Only check for player units
	if team != Team.PLAYER:
		return

	# Only emit once per unit
	if _has_emitted_proximity_signal:
		return

	# Find enemy base
	var enemy_bases: Array[Node] = get_tree().get_nodes_in_group("enemy_base")
	if enemy_bases.is_empty():
		return

	var enemy_base: Node3D = enemy_bases[0] as Node3D
	if not enemy_base:
		return

	# Check distance (use 2D distance on XZ plane)
	var distance_2d: float = Vector2(global_position.x, global_position.z).distance_to(
		Vector2(enemy_base.global_position.x, enemy_base.global_position.z)
	)

	# Emit signal when within 10 units
	const PROXIMITY_THRESHOLD: float = 10.0
	if distance_2d <= PROXIMITY_THRESHOLD:
		_has_emitted_proximity_signal = true
		GameStateEvents.unit_near_enemy_base.emit(self, distance_2d)

		# Debug logging (only if EventSequencer has debug_mode enabled)
		var event_sequencer: Node = get_node_or_null("/root/EventSequencer")
		if event_sequencer and event_sequencer.get("debug_mode"):
			print("Unit3D: Player unit '%s' near enemy base (distance: %.2f)" % [name, distance_2d])

func _is_valid_target(target: Node3D) -> bool:
	## Check if a target is still valid (alive and in range)
	if not target or not is_instance_valid(target):
		return false
	if target is Unit3D:
		# Type narrow to Unit3D for safe property access
		var unit_target: Unit3D = target
		if not unit_target.is_alive:
			return false

	# Skip distance check for forced targets from Charge spell
	# Forced targets should be pursued regardless of initial distance
	if target == forced_target and forced_target_timer > 0.0:
		return true

	# Check if target is within aggro range (use distance_squared for performance)
	var delta: Vector3 = target.global_position - global_position
	var distance_sq: float = delta.x * delta.x + delta.z * delta.z
	var max_range: float = aggro_radius * 1.5  # Allow some leeway
	return distance_sq <= max_range * max_range

func _acquire_target() -> Node3D:
	## Find the best target using weighted scoring system

	# Priority 1: Check forced_target from redirect system
	if forced_target and _is_valid_target(forced_target):
		return forced_target

	# Priority 2: If forced_target died but timer still active, try fallback in original area
	if forced_target_timer > 0.0 and original_redirect_point != Vector3.ZERO:
		var fallback: Node3D = RedirectManager.find_fallback_target(
			original_redirect_point,
			team,
			RedirectManager.TARGET_SEARCH_RADIUS,
			forced_target  # Exclude the dead target
		)
		if fallback:
			forced_target = fallback
			return fallback
		else:
			# No fallback found - clear forced targeting and resume normal behavior
			forced_target = null
			forced_target_timer = 0.0
			original_redirect_point = Vector3.ZERO

	# Priority 3: Normal targeting behavior
	var target_group: String = "enemy_units" if team == Team.PLAYER else "player_units"
	var targets: Array[Node] = get_tree().get_nodes_in_group(target_group)

	var best_target: Node3D = null
	var best_score: float = -INF
	var aggro_radius_sq: float = aggro_radius * aggro_radius

	for target: Node in targets:
		if not target is Unit3D:
			continue

		# Type narrow to Unit3D for safe property access
		var target_unit: Unit3D = target

		if not target_unit.is_alive:
			continue

		# Skip targets we cannot attack based on layer restrictions
		if not _can_attack_layer(target_unit):
			continue

		# Calculate horizontal distance_squared (ignore Y-axis) - no sqrt yet!
		var delta: Vector3 = target_unit.global_position - global_position
		var distance_sq: float = delta.x * delta.x + delta.z * delta.z

		# Fast filtering: skip targets outside aggro range (no sqrt needed)
		if distance_sq > aggro_radius_sq:
			continue

		# Only calculate actual distance (sqrt) for targets in range
		var distance: float = sqrt(distance_sq)

		# Calculate weighted score
		var score: float = 0.0

		# Distance component (inverse: closer = higher score)
		if distance_weight > 0.0 and distance > 0.01:
			score += distance_weight / distance

		# HP component (inverse: lower HP = higher score)
		if hp_weight > 0.0:
			var hp_percent: float = target_unit.current_hp / target_unit.max_hp
			# Add small epsilon to avoid division by zero
			score += hp_weight / (hp_percent + 0.1)

		# Track best scoring target
		if score > best_score:
			best_score = score
			best_target = target_unit

	# If no unit found, target the enemy base
	if not best_target:
		var base_group: String = "enemy_base" if team == Team.PLAYER else "player_base"
		var bases: Array[Node] = get_tree().get_nodes_in_group(base_group)
		if bases.size() > 0:
			best_target = bases[0] as Node3D

	return best_target

func _move_towards_target(_delta: float) -> void:
	if not current_target:
		return

	var direction: Vector3 = (current_target.global_position - global_position).normalized()
	# Only move on X and Z axes (2.5D movement)
	direction.y = 0

	# Apply separation steering to avoid stacking with nearby units
	var separation: Vector3 = _calculate_separation_force()
	var final_direction: Vector3 = (direction * move_speed + separation).normalized()

	# Face the direction we're moving (use base direction, not separation-adjusted)
	_update_facing_from_direction(direction)

	velocity = final_direction * move_speed
	move_and_slide()

## Check if we can attack this target's layer
func _can_attack_layer(target: Node3D) -> bool:
	if not target is Unit3D:
		return true  # Can attack non-units (bases, structures)

	# Type narrow to Unit3D for safe property access
	var target_unit: Unit3D = target
	var target_layer: MovementLayer = target_unit.movement_layer

	# Apply layer-based targeting restrictions
	match can_target:
		TargetLayer.GROUND_ONLY:
			return target_layer == MovementLayer.GROUND
		TargetLayer.AIR_ONLY:
			return target_layer == MovementLayer.AIR
		TargetLayer.BOTH:
			return true
		_:
			return true  # Fallback for any unexpected values

## Check if target is within attack range
## TODO: Alternative approach - weighted/ellipse distance for smooth falloff
## Currently using box-shaped range (per-axis checking) for melee
## NOTE: Layer compatibility is already checked in _acquire_target()
func _is_in_attack_range(target: Node3D) -> bool:
	if not target:
		return false

	var delta: Vector3 = target.global_position - global_position

	# Ranged units use simple 3D distance (sphere)
	if unit_type == UnitType.RANGED or is_ranged:  # Support legacy is_ranged
		var distance: float = global_position.distance_to(target.global_position)
		return distance <= attack_range

	# Melee units use box-shaped range (per-axis checking)
	# This prevents attacking across lanes or at different heights
	if abs(delta.x) > attack_range:  # Left-right
		return false

	# Check Y-axis (height) only for ground vs ground combat
	# Flying units ignore height differences when attacking
	var target_is_unit: bool = target is Unit3D
	var target_is_flying: bool = false
	if target_is_unit:
		# Type narrow to Unit3D for safe property access
		var target_unit: Unit3D = target
		target_is_flying = target_unit.movement_layer == MovementLayer.AIR
	var is_flying: bool = movement_layer == MovementLayer.AIR

	if not is_flying and not target_is_flying:  # Both on ground
		if abs(delta.y) > attack_range_vertical:  # Height tolerance
			return false

	if abs(delta.z) > attack_range_depth:  # Lane/depth
		return false

	return true

func _update_facing(target_position: Vector3) -> void:
	# Calculate direction to target and face that direction
	var direction: Vector3 = (target_position - global_position).normalized()
	_update_facing_from_direction(direction)

func _update_facing_from_direction(direction: Vector3) -> void:
	if not visual_component or not visual_component.has_method("set_flip_h"):
		return

	# Face left if direction has negative X component (towards player base on left)
	var should_face_left: bool = direction.x < 0

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
	var attack_duration: float = 1.0 / attack_speed  # Cooldown duration
	var animation_duration: float = 1.0  # Fallback
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
		var spawn_pos: Vector3 = get_projectile_spawn_position()
		var target_pos: Vector3 = current_target.call("get_projectile_target_position") if current_target.has_method("get_projectile_target_position") else current_target.global_position

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
	var projectile_speed: float = _get_projectile_speed()
	if projectile_speed <= 0:
		return target_pos  # Fallback to current position

	# Get target velocity
	var target_velocity: Vector3 = Vector3.ZERO
	if target is CharacterBody3D:
		# Type narrow to CharacterBody3D for safe property access
		var character_body: CharacterBody3D = target
		target_velocity = character_body.velocity
	elif "velocity" in target:
		target_velocity = target.get("velocity")

	# If target is stationary, no prediction needed
	if target_velocity.length_squared() < VELOCITY_STATIONARY_THRESHOLD:
		return target_pos

	# Calculate distance for initial time-to-impact estimation
	var distance: float = (target_pos - shooter_pos).length()

	# Simple time-to-impact estimation
	var time_to_impact: float = distance / projectile_speed

	# Predict target position at impact time
	var predicted_pos: Vector3 = target_pos + (target_velocity * time_to_impact)

	# Iterative refinement (one iteration is optimal - research shows diminishing returns after this)
	var refined_distance: float = (predicted_pos - shooter_pos).length()
	var refined_time: float = refined_distance / projectile_speed
	predicted_pos = target_pos + (target_velocity * refined_time)

	# Bounds validation: ensure prediction isn't absurdly far from current position
	var prediction_offset: float = (predicted_pos - target_pos).length()
	if prediction_offset > MAX_PREDICTION_DISTANCE:
		# Clamp to max distance in the direction of movement
		var direction: Vector3 = predicted_pos - target_pos
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

	var proj_data_variant: Variant = ContentCatalog.projectiles[projectile_id]
	if not proj_data_variant is Dictionary:
		cached_projectile_speed = 15.0
		return max(15.0, MIN_PROJECTILE_SPEED)

	var proj_data: Dictionary = proj_data_variant
	if proj_data and "speed" in proj_data:
		var default_speed: float = 15.0
		var speed_variant: Variant = proj_data.get("speed", default_speed)
		var speed: float = speed_variant if speed_variant is float else 15.0
		cached_projectile_speed = speed
		return max(speed, MIN_PROJECTILE_SPEED)

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

	# Visual feedback: white flash on hit
	_flash_white()

	# TODO: Play hit sound here - different sounds for melee/ranged/magic damage
	# Future: Integrate with HitFeedbackManager or SoundManager

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

	var current_anim: String = visual_component.get_current_animation()

	# Don't interrupt important animations (attack, hurt, death)
	if current_anim in ["attack", "hurt", "death"]:
		# Only block if animation is still playing
		if visual_component.is_playing() and anim_name not in ["attack", "hurt", "death"]:
			return  # Don't interrupt with idle/walk while animation is playing

	if current_anim != anim_name:
		# SpriteCharacter2D5Component.play_animation() handles missing animations with fallback to "idle"
		visual_component.play_animation(anim_name)

## Flash the unit white briefly when taking damage
func _flash_white() -> void:
	if not visual_component:
		return

	# Delegate to the visual component's flash implementation
	if visual_component.has_method("flash_white"):
		visual_component.flash_white()

## =============================================================================
## RALLY & GUARD SPELL BEHAVIOR
## =============================================================================

## Update Rally behavior (simple movement to rally point)
func _update_rally_behavior(_delta: float) -> void:
	# Check if we're at the rally point
	var distance_to_rally: float = global_position.distance_to(rally_point)

	if distance_to_rally > 1.0:
		# Move toward rally point
		_move_towards_position(rally_point)
		_update_animation("walk")
	else:
		# Reached rally point - clear rally mode and resume normal AI
		_clear_rally_mode()

## Clear rally mode and resume normal AI
func _clear_rally_mode() -> void:
	rally_mode = false
	rally_point = Vector3.ZERO
	print("Unit %s: Reached rally point, resuming normal AI" % name)

## Update Guard behavior (formation + hold)
func _update_guard_behavior(_delta: float) -> void:
	# Check if we've reached formation position
	var distance_to_formation: float = global_position.distance_to(formation_position)

	if distance_to_formation > 0.5:
		# Still moving to formation
		_move_towards_position(formation_position)
		_update_animation("walk")
	else:
		# At formation - hold and defend
		# Acquire nearby enemies
		if target_lock_timer <= 0.0 or not _is_valid_target(current_target):
			current_target = _acquire_guard_target()
			if current_target:
				target_lock_timer = target_lock_duration

		if current_target and _is_in_attack_range(current_target):
			if not is_attacking:
				_update_facing(current_target.global_position)
				_update_animation("idle")
			if attack_cooldown <= 0.0:
				_perform_attack()
		else:
			if not is_attacking:
				_update_animation("idle")

	# Check guard expiration (10 seconds or no enemies for 5 seconds)
	if guard_timer <= 0.0:
		_clear_guard_mode()

## Acquire target for guard mode (only nearby enemies, no chasing)
func _acquire_guard_target() -> Node3D:
	var target_group: String = "enemy_units" if team == Team.PLAYER else "player_units"
	var all_units: Array[Node] = get_tree().get_nodes_in_group(target_group)

	var closest_enemy: Unit3D = null
	var closest_dist: float = INF
	var guard_radius: float = aggro_radius * 0.5  # Smaller radius for guard

	for node: Node in all_units:
		if not node is Unit3D:
			continue
		var enemy: Unit3D = node as Unit3D
		if not enemy.is_alive:
			continue

		var dist: float = enemy.global_position.distance_to(global_position)
		if dist <= guard_radius and dist < closest_dist:
			closest_enemy = enemy
			closest_dist = dist

	return closest_enemy

## Clear guard mode and resume normal AI
func _clear_guard_mode() -> void:
	guard_mode = false
	guard_timer = 0.0
	formation_position = Vector3.ZERO
	print("Unit %s: Guard mode ended, resuming normal AI" % name)

## Move toward a specific position (used by rally/guard)
func _move_towards_position(target_position: Vector3) -> void:
	var direction: Vector3 = (target_position - global_position).normalized()
	direction.y = 0  # 2.5D movement

	# Apply separation steering to avoid stacking with nearby units
	var separation: Vector3 = _calculate_separation_force()
	var final_direction: Vector3 = (direction * move_speed + separation).normalized()

	_update_facing_from_direction(direction)
	velocity = final_direction * move_speed
	move_and_slide()

## Calculate separation steering force to avoid overlapping with nearby units
func _calculate_separation_force() -> Vector3:
	var separation: Vector3 = Vector3.ZERO
	var neighbors: int = 0
	var radius_sq: float = SEPARATION_RADIUS * SEPARATION_RADIUS

	# Check all units (both teams - we don't want to overlap with anyone)
	var all_units: Array[Node] = get_tree().get_nodes_in_group("units")

	for node: Node in all_units:
		if node == self:
			continue

		if not node is Unit3D:
			continue

		var other: Unit3D = node as Unit3D
		if not other.is_alive:
			continue

		# Calculate 2D distance (ignore Y-axis)
		var delta: Vector3 = global_position - other.global_position
		delta.y = 0
		var distance_sq: float = delta.x * delta.x + delta.z * delta.z

		# Skip if outside separation radius
		if distance_sq >= radius_sq or distance_sq < 0.001:
			continue

		# Calculate push direction (away from other unit)
		var distance: float = sqrt(distance_sq)
		var push_dir: Vector3 = delta.normalized()

		# Stronger push when closer (inverse relationship)
		var strength: float = (1.0 - distance / SEPARATION_RADIUS) * SEPARATION_STRENGTH
		separation += push_dir * strength
		neighbors += 1

	return separation

## Calculate formation positions for a group of units (static helper)
static func calculate_formation_positions(units: Array[Unit3D], center: Vector3) -> void:
	if units.is_empty():
		return

	# Separate by unit type
	var melee_units: Array[Unit3D] = []
	var ranged_units: Array[Unit3D] = []

	for unit: Unit3D in units:
		if unit.unit_type == UnitType.MELEE:
			melee_units.append(unit)
		else:
			ranged_units.append(unit)

	# Dynamic radius scaling based on unit count
	# For a 180-degree arc (PI radians), calculate radius to fit units with proper spacing
	const UNIT_SPACING: float = 1.5  # Minimum spacing between units
	const MIN_RADIUS: float = 2.0    # Minimum formation radius
	const FORMATION_ANGLE: float = PI  # 180 degrees

	# Calculate radius for melee arc (arc_length = num_units × spacing, radius = arc_length / angle)
	var front_radius: float = MIN_RADIUS
	if melee_units.size() > 1:
		var arc_length: float = melee_units.size() * UNIT_SPACING
		front_radius = max(MIN_RADIUS, arc_length / FORMATION_ANGLE)

	# Calculate radius for ranged arc (always behind melee, add 2.0 offset)
	var back_radius: float = front_radius + 2.0
	if ranged_units.size() > 1:
		var arc_length: float = ranged_units.size() * UNIT_SPACING
		back_radius = max(front_radius + 2.0, arc_length / FORMATION_ANGLE)

	# Position melee units in front arc
	for i: int in range(melee_units.size()):
		var angle: float = (float(i) / max(melee_units.size(), 1)) * PI - PI/2  # Spread across 180 degrees in front
		var offset: Vector3 = Vector3(cos(angle) * front_radius, 0, sin(angle) * front_radius)
		melee_units[i].formation_position = center + offset
		melee_units[i].guard_mode = true
		melee_units[i].guard_timer = 25.0  # 25 second guard duration

	# Position ranged units in back arc
	for i: int in range(ranged_units.size()):
		var angle: float = (float(i) / max(ranged_units.size(), 1)) * PI - PI/2  # Spread across 180 degrees behind
		var offset: Vector3 = Vector3(cos(angle) * back_radius, 0, sin(angle) * back_radius)
		ranged_units[i].formation_position = center + offset
		ranged_units[i].guard_mode = true
		ranged_units[i].guard_timer = 25.0  # 25 second guard duration

## Get the world position where projectiles should spawn from
func get_projectile_spawn_position() -> Vector3:
	if projectile_spawn_point:
		return projectile_spawn_point.global_position
	# Dynamic: query visual component for sprite height
	if visual_component and visual_component.has_method("get_sprite_height"):
		var sprite_height: float = visual_component.get_sprite_height()
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
		var sprite_height: float = visual_component.get_sprite_height()
		# Target at ~60% of height (chest area)
		return global_position + Vector3(0, sprite_height * 0.6, 0)
	# Fallback for units without visual component
	return global_position + Vector3(0, 1.2, 0)
