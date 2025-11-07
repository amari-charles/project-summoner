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

func _physics_process(delta: float) -> void:
	if not is_active:
		return

	time_alive += delta
	travel_time += delta

	# Check lifetime expiration
	if time_alive >= lifetime:
		_expire()
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

	# Check if reached target
	if progress >= 1.0:
		_expire()

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

	# Check if passed target
	if global_position.distance_to(target_position) < 0.5:
		_expire()

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

	# Set target position
	if target and is_instance_valid(target):
		target_position = target.global_position
	elif data.has("target_position"):
		target_position = data.target_position

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

	# Spawn hit VFX
	if not hit_vfx.is_empty():
		VFXManager.play_effect(hit_vfx, global_position)

	# Handle pierce
	if pierce_count == -1:
		# Infinite pierce, keep going
		return
	elif hits_remaining > 0:
		hits_remaining -= 1
		return
	else:
		# No pierce remaining, destroy
		_expire()

## Expire projectile
func _expire() -> void:
	is_active = false
	projectile_expired.emit(self)

	if is_pooled:
		# Return to pool (ProjectileManager handles this via signal)
		pass
	else:
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
