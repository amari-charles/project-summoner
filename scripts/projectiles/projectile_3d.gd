extends Area3D
class_name Projectile3D

## Data-driven 3D projectile system
## Supports multiple movement types: straight, homing, arc, ballistic
## Managed by ProjectileManager for pooling

## Movement types
enum MovementType {
	STRAIGHT,   ## Moves in a straight line
	HOMING,     ## Tracks target position
	ARC,        ## Follows an arc trajectory
	BALLISTIC   ## Parabolic arc with gravity
}

## Configuration (set by ProjectileData)
var projectile_id: String = ""
var movement_type: MovementType = MovementType.STRAIGHT
var speed: float = 15.0
var lifetime: float = 5.0
var arc_height: float = 2.0
var homing_strength: float = 5.0
var pierce_count: int = 0  ## 0 = no pierce, -1 = infinite
var damage: float = 10.0
var damage_type: String = "physical"
var visual_scene: PackedScene = null
var hit_vfx: String = ""
var trail_vfx: String = ""
var fade_on_hit: bool = true  ## Fade out when hitting target
var fade_duration: float = 0.5  ## Time to fade out

## State
var source: Node3D = null
var target: Node3D = null
var team: int = -1
var direction: Vector3 = Vector3.FORWARD
var start_position: Vector3 = Vector3.ZERO
var target_position: Vector3 = Vector3.ZERO
var travel_time: float = 0.0
var time_alive: float = 0.0
var hits_remaining: int = 0
var is_pooled: bool = false
var is_active: bool = false
var is_fading: bool = false

## Visual component instance
var visual_instance: Node3D = null

## Signals
signal projectile_hit(target: Node3D, projectile: Projectile3D)
signal projectile_expired(projectile: Projectile3D)

func _ready() -> void:
	# Connect area signals
	if not body_entered.is_connected(_on_body_entered):
		body_entered.connect(_on_body_entered)
	if not area_entered.is_connected(_on_area_entered):
		area_entered.connect(_on_area_entered)

	# Instance visual if available
	if visual_scene and not visual_instance:
		visual_instance = visual_scene.instantiate()
		add_child(visual_instance)

		# Duplicate materials to avoid shared material issues when fading
		_duplicate_materials()

## Duplicate materials for this instance to avoid shared material problems
func _duplicate_materials() -> void:
	if not visual_instance:
		return

	for child in visual_instance.get_children():
		if child is MeshInstance3D:
			var material = child.get_surface_override_material(0)
			if material:
				# Create a unique material instance for this projectile
				child.set_surface_override_material(0, material.duplicate())

func _physics_process(delta: float) -> void:
	if not is_active:
		return

	time_alive += delta
	travel_time += delta

	# Check lifetime expiration - expire without fade (timed out)
	if time_alive >= lifetime:
		_expire_immediate()
		return

	# Check ground collision (explode if hit ground)
	if global_position.y <= BattlefieldConstants.GROUND_Y + 0.2:
		print("GROUND HIT: Projectile at y=%.2f, triggering expire" % global_position.y)
		_trigger_impact_effects(Vector3(global_position.x, BattlefieldConstants.GROUND_Y, global_position.z))
		if fade_on_hit:
			_expire_with_fade()
		else:
			_expire_immediate()
		return

	# Update movement based on type
	match movement_type:
		MovementType.STRAIGHT:
			_move_straight(delta)
		MovementType.HOMING:
			_move_homing(delta)
		MovementType.ARC:
			_move_arc(delta)
		MovementType.BALLISTIC:
			_move_ballistic(delta)

	# Rotate to face movement direction
	if direction.length_squared() > 0.001:
		look_at(global_position + direction, Vector3.UP)

## Straight line movement
func _move_straight(delta: float) -> void:
	global_position += direction * speed * delta

## Homing movement - tracks target
func _move_homing(delta: float) -> void:
	if is_instance_valid(target):
		# Update direction toward target
		var target_dir = (target.global_position - global_position).normalized()
		direction = direction.lerp(target_dir, homing_strength * delta).normalized()

	global_position += direction * speed * delta

## Arc movement - follows arc to target position
func _move_arc(delta: float) -> void:
	var progress = travel_time * speed / start_position.distance_to(target_position)
	progress = clamp(progress, 0.0, 1.0)

	# Horizontal movement (linear interpolation)
	var horizontal_pos = start_position.lerp(target_position, progress)

	# Vertical offset (parabolic arc)
	var arc_offset = arc_height * sin(progress * PI)
	horizontal_pos.y += arc_offset

	# Update position and direction
	direction = (horizontal_pos - global_position).normalized()
	global_position = horizontal_pos

	# Check if reached target - trigger impact effects and expire
	if progress >= 1.0:
		_trigger_impact_effects(global_position)
		if fade_on_hit:
			_expire_with_fade()
		else:
			_expire_immediate()

## Ballistic movement - parabolic arc with gravity
func _move_ballistic(delta: float) -> void:
	# Calculate initial velocity to reach target
	var displacement = target_position - start_position
	var horizontal_dist = Vector2(displacement.x, displacement.z).length()
	var vertical_dist = displacement.y

	# Ballistic trajectory
	var gravity = 9.8
	var time_to_target = horizontal_dist / speed
	var initial_velocity_y = (vertical_dist + 0.5 * gravity * time_to_target * time_to_target) / time_to_target

	# Apply velocity
	var horizontal_dir = Vector3(displacement.x, 0, displacement.z).normalized()
	var velocity = horizontal_dir * speed
	velocity.y = initial_velocity_y - gravity * travel_time

	direction = velocity.normalized()
	global_position += velocity * delta

	# Check if passed target - trigger impact effects and expire
	if global_position.distance_to(target_position) < 0.5:
		_trigger_impact_effects(global_position)
		if fade_on_hit:
			_expire_with_fade()
		else:
			_expire_immediate()

## Initialize projectile
func initialize(data: Dictionary) -> void:
	# Required fields
	source = data.get("source")
	target = data.get("target")
	team = data.get("team", -1)
	damage = data.get("damage", 10.0)
	damage_type = data.get("damage_type", "physical")

	# Set position
	if data.has("start_position"):
		start_position = data.start_position
		global_position = start_position
	elif source:
		start_position = source.global_position
		global_position = start_position

	# Set target position (prioritize explicit target_position over target.global_position)
	if data.has("target_position"):
		target_position = data.target_position
	elif target and is_instance_valid(target):
		target_position = target.global_position

	# Set direction
	if data.has("direction"):
		direction = data.direction.normalized()
	elif target_position != Vector3.ZERO:
		direction = (target_position - start_position).normalized()

	# Reset state
	travel_time = 0.0
	time_alive = 0.0
	hits_remaining = pierce_count
	is_active = true

	# Make projectile and visual visible when spawning
	visible = true
	if visual_instance:
		visual_instance.visible = true

	# Spawn trail VFX
	if not trail_vfx.is_empty():
		VFXManager.play_effect(trail_vfx, global_position)

## Handle body collision
func _on_body_entered(body: Node3D) -> void:
	if not is_active:
		return

	# Check if it's a valid target
	if not _is_valid_target(body):
		return

	_hit_target(body)

## Handle area collision
func _on_area_entered(area: Area3D) -> void:
	if not is_active:
		return

	# Check if area belongs to a unit
	var body = area.get_parent()
	if body and _is_valid_target(body):
		_hit_target(body)

## Check if body is a valid target
func _is_valid_target(body: Node3D) -> bool:
	# Don't hit the source
	if body == source:
		return false

	# Check team
	if "team" in body and body.team == team:
		return false

	# Check if alive
	if "is_alive" in body and not body.is_alive:
		return false

	return true

## Hit a target
func _hit_target(target_node: Node3D) -> void:
	# Apply damage using DamageSystem
	if target_node.has_method("take_damage"):
		DamageSystem.apply_damage(source, target_node, damage, damage_type)

	# Emit signal
	projectile_hit.emit(target_node, self)

	# Spawn hit VFX and apply AOE damage if applicable
	_trigger_impact_effects(global_position)

	# Handle pierce
	if pierce_count == -1:
		# Infinite pierce, keep going
		return
	elif hits_remaining > 0:
		hits_remaining -= 1
		return
	else:
		# No pierce remaining, destroy with fade
		if fade_on_hit:
			_expire_with_fade()
		else:
			_expire_immediate()

## Trigger impact effects (VFX and AOE damage)
func _trigger_impact_effects(impact_position: Vector3) -> void:
	print("\n=== PROJECTILE IMPACT ===")
	print("  Position: %v" % impact_position)
	print("  Projectile ID: '%s'" % projectile_id)
	print("  Team: %d" % team)
	print("  Damage: %.1f (%s)" % [damage, damage_type])
	print("  Source valid: %s" % is_instance_valid(source))

	# Spawn hit VFX
	if not hit_vfx.is_empty():
		VFXManager.play_effect(hit_vfx, impact_position)

	# Apply AOE damage if radius is set
	var proj_data = _get_projectile_data()
	print("  Has ProjectileData: %s" % (proj_data != null))
	if proj_data:
		print("  AOE radius: %.1f" % proj_data.aoe_radius)
		if proj_data.aoe_radius > 0.0:
			_apply_aoe_damage(impact_position, proj_data.aoe_radius)
		else:
			print("  AOE radius is 0, skipping AOE damage")
	else:
		print("  No ProjectileData found, skipping AOE damage")

## Apply AOE damage to all enemies in radius
func _apply_aoe_damage(center: Vector3, radius: float) -> void:
	print("\n--- AOE DAMAGE CALCULATION ---")
	print("  Center: %v, Radius: %.1f" % [center, radius])
	print("  Projectile damage: %.1f, type: %s" % [damage, damage_type])

	if not is_instance_valid(source):
		print("  ERROR: Source not valid!")
		return

	print("  Source: %s (team %d)" % [source.name, team])

	var scene_tree = get_tree()
	if not scene_tree:
		print("  ERROR: No scene tree!")
		return

	# Spawn debug visualization sphere
	_spawn_debug_aoe_sphere(center, radius)

	# Determine target group based on team
	var target_group = "enemy_units" if team == Unit3D.Team.PLAYER else "player_units"
	print("  Target group: '%s'" % target_group)

	var enemies = scene_tree.get_nodes_in_group(target_group)
	print("  Found %d potential targets in group" % enemies.size())

	# Also check all units to see if grouping is the issue
	var all_units = scene_tree.get_nodes_in_group("units")
	print("  Total units in scene: %d" % all_units.size())

	if enemies.size() == 0 and all_units.size() > 0:
		print("  WARNING: No enemies in target group but units exist - group assignment issue?")
		for unit in all_units:
			if unit is Unit3D:
				print("    Unit '%s': team=%d, alive=%s, pos=%v" % [unit.name, unit.team, unit.is_alive, unit.global_position])

	var hit_count = 0
	for enemy in enemies:
		if enemy is Unit3D:
			var distance = enemy.global_position.distance_to(center)
			var alive_str = "alive" if enemy.is_alive else "DEAD"
			print("    %s: distance=%.1f, %s, team=%d" % [enemy.name, distance, alive_str, enemy.team])

			if enemy.is_alive and distance <= radius:
				print("      -> APPLYING DAMAGE: %.1f" % damage)
				DamageSystem.apply_damage(source, enemy, damage, damage_type)
				hit_count += 1
			elif not enemy.is_alive:
				print("      -> Skipped (dead)")
			elif distance > radius:
				print("      -> Skipped (too far)")

	print("  RESULT: Hit %d enemies with %.1f damage each" % [hit_count, damage])
	print("--- END AOE CALCULATION ---\n")

## Spawn a debug visualization sphere to show AOE radius
func _spawn_debug_aoe_sphere(center: Vector3, radius: float) -> void:
	var sphere = MeshInstance3D.new()
	var mesh = SphereMesh.new()
	mesh.radius = radius
	mesh.height = radius * 2
	sphere.mesh = mesh

	# Semi-transparent red material
	var material = StandardMaterial3D.new()
	material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	material.albedo_color = Color(1, 0, 0, 0.2)
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	sphere.material_override = material

	sphere.global_position = center
	get_tree().root.add_child(sphere)

	# Auto-remove after 1 second
	await get_tree().create_timer(1.0).timeout
	sphere.queue_free()

## Get projectile data from ContentCatalog
func _get_projectile_data() -> ProjectileData:
	if projectile_id.is_empty():
		return null
	return ContentCatalog.get_projectile(projectile_id)

## Expire projectile with fade-out animation (used on hit)
func _expire_with_fade() -> void:
	if is_fading:
		return  # Already fading

	is_fading = true
	is_active = false  # Stop movement

	# If no visual or can't fade, just hide immediately
	if not visual_instance:
		visible = false
		_expire_immediate()
		return

	# Create tween for fade animation
	var tween = create_tween()
	tween.set_parallel(true)

	# Fade all materials on visual children
	for child in visual_instance.get_children():
		if child is MeshInstance3D:
			var material = child.get_surface_override_material(0)
			if material and material is StandardMaterial3D:
				# Tween alpha from current to 0
				tween.tween_property(material, "albedo_color:a", 0.0, fade_duration)
		elif child is GPUParticles3D:
			# Stop emitting new particles
			child.emitting = false

	# When tween finishes, hide and cleanup
	tween.finished.connect(func():
		visible = false
		_expire_immediate()
	)

## Expire projectile immediately (no animation)
func _expire_immediate() -> void:
	print("EXPIRE_IMMEDIATE: is_pooled=%s, is_active=%s" % [is_pooled, is_active])
	is_active = false
	is_fading = false
	projectile_expired.emit(self)

	if is_pooled:
		# Return to pool (ProjectileManager handles this via signal)
		print("EXPIRE_IMMEDIATE: Emitted signal, waiting for pool return")
	else:
		print("EXPIRE_IMMEDIATE: Not pooled, calling queue_free()")
		queue_free()

## Reset for pooling
func reset() -> void:
	source = null
	target = null
	team = -1
	direction = Vector3.FORWARD
	start_position = Vector3.ZERO
	target_position = Vector3.ZERO
	travel_time = 0.0
	time_alive = 0.0
	hits_remaining = 0
	is_active = false
	is_fading = false

	# Cancel any running tweens (only if in tree)
	if is_inside_tree():
		var tweens = get_tree().get_processed_tweens()
		for tween in tweens:
			if tween.is_valid():
				tween.kill()

	# Reset visual alpha and visibility (but keep hidden until reused)
	if visual_instance:
		visual_instance.visible = false  # Keep hidden until next spawn
		for child in visual_instance.get_children():
			if child is MeshInstance3D:
				child.visible = true
				var material = child.get_surface_override_material(0)
				if material and material is StandardMaterial3D:
					# Reset transparency mode
					material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
					var color = material.albedo_color
					color.a = 1.0
					material.albedo_color = color

	# Only reset transform if node is in tree
	if is_inside_tree():
		global_position = Vector3.ZERO
		rotation = Vector3.ZERO

## Load configuration from ProjectileData
func load_from_data(data: ProjectileData) -> void:
	projectile_id = data.projectile_id
	movement_type = _string_to_movement_type(data.movement_type)
	speed = data.speed
	lifetime = data.lifetime
	arc_height = data.arc_height
	homing_strength = data.homing_strength
	pierce_count = data.pierce_count
	visual_scene = data.visual_scene
	hit_vfx = data.hit_vfx
	trail_vfx = data.trail_vfx

## Convert string to MovementType enum
func _string_to_movement_type(type_str: String) -> MovementType:
	match type_str.to_lower():
		"straight":
			return MovementType.STRAIGHT
		"homing":
			return MovementType.HOMING
		"arc":
			return MovementType.ARC
		"ballistic":
			return MovementType.BALLISTIC
		_:
			push_warning("Projectile3D: Unknown movement type '%s', defaulting to STRAIGHT" % type_str)
			return MovementType.STRAIGHT
